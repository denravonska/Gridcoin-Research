// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using GridcoinDPOR.Data;
using GridcoinDPOR.Data.Models;
using GridcoinDPOR.Logging;
using GridcoinDPOR.Models;
using GridcoinDPOR.Util;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GridcoinDPOR.Data
{
    public class DataSynchronizer
    {
        private ILogger _logger = new NullLogger();
        public ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value;}
        }

        private readonly GridcoinContext _db;
        public DataSynchronizer(GridcoinContext dbContext)
        {
            _db = dbContext;
        }
        
        public async Task<bool> SyncDPOR2(string dataDirectory, string syncDataXml, bool teamOption)
        {
            var dporDir = Path.Combine(dataDirectory, "DPOR");
            var statsDir = Path.Combine(dporDir, "stats");
            
            if (!Directory.Exists(statsDir)) 
            {
                Directory.CreateDirectory(statsDir);
            }
            
            try
            {
                // EXTRACT LIST OF WHITELISTED PROJECTS FROM XML DATA
                var syncData = SyncData.Parse(syncDataXml);

                //TODO: Delete any projects from the DB that are no longer white-listed
                
                // DOWNLOAD AND EXTRACT FILES
                foreach(var project in syncData.Whitelist)
                {
                    // only search for the Team ID when -noteam is not set in args
                    if(teamOption)
                    {
                        foreach(var teamUrl in project.GetTeamUrls())
                        {
                            string teamGzip = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_team.gz");
                            bool teamGzipDownloadResult = await WebUtil.DownloadFile(teamUrl, teamGzip);
                            if (teamGzipDownloadResult)
                            {
                                project.TeamId = await TeamXmlParser.GetGridcoinTeamIdAsync(teamGzip);
                                break;
                            }
                            else
                            {
                                _logger.Warning("Failed to download team file from URL: {0}", teamUrl);
                            }
                        }
                    }

                    // get the user file
                    foreach(var userUrl in project.GetUserUrls())
                    {
                        string userGzip = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_user.gz");
                        bool userGzipDownloadResult = await WebUtil.DownloadFile(userUrl, userGzip);
                        if (userGzipDownloadResult)
                        {
                            _logger.Information("Syncing stats for project: {0}", project.Name);
                            if(await ParseAndStoreUserStats(userGzip, project.Name, project.TeamId, syncData.CpidData))
                            {
                                _logger.Information("Successfully Synchronized stats for project: {0}", project.Name);
                            }
                            break;
                        }
                        else
                        {
                            _logger.Warning("Failed to download user file from URL: {0}", userUrl);
                        }
                    }
                }

                // not sure if it's worth returning anything?
                return true;
            }
            catch(Exception ex)
            {
                _logger.Fatal("SyncDPOR2 command failed with exception {0}", ex);
                return false;
            }
        }

        private async Task<bool> ParseAndStoreUserStats(string filePath, string projectName, int teamId, IEnumerable<CpidData> beaconCpids)
        {
            string filename = Path.GetFileName(filePath);
            var localFileLastModified = File.GetLastAccessTimeUtc(filePath);

            // get project from DB
            var project = _db.Projects.Include(x => x.Researchers).SingleOrDefault(x => x.Name.Equals(projectName));
            if (project != null)
            {
                // TODO: delete any researchers from the project that have expired beacons


                if (project.LastSyncUtc == localFileLastModified)
                {
                    _logger.Information("Data for project already up to date. Skipping project {0}", project.Name);
                    return true;
                }
            }
            else 
            {
                project = new Project()
                {
                    Name = projectName,
                };

                _db.Projects.Add(project);

                int changes = await _db.SaveChangesAsync();
            }
            
            var readerSettings = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                Async = true
            };

            using (var fileStream = File.OpenRead(filePath))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = XmlReader.Create(gzipStream, readerSettings))
            {
                _logger.Information("Started parsing {0} for CPID's and Credit with TeamID: {1}", filename, teamId);
                while(!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
                    {
                        string xml = await reader.ReadInnerXmlAsync();
                        string xmlCpid = XmlUtil.ExtractXml(xml, "cpid");
                        double xmlTotalCredit = Convert.ToDouble(XmlUtil.ExtractXml(xml, "total_credit"));
                        double xmlRAC = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_credit"));
                        int xmlProjectUserId = Convert.ToInt32(XmlUtil.ExtractXml(xml, "id"));

                        // only extract if this CPID is in the list of beacons
                        if (beaconCpids.Any(a => a.CPID == xmlCpid))
                        {
                            bool inTeam = false;
                            string xmlTeamId = XmlUtil.ExtractXml(xml, "teamid");
                            if (teamId.ToString().Equals(xmlTeamId))
                            {
                                inTeam = true;
                            }

                            var researcher = project.Researchers.SingleOrDefault(x => x.CPID == xmlCpid);
                            if (researcher != null)
                            {
                                // edit Credit and RAC
                                researcher.TotalCredit = xmlTotalCredit;
                                researcher.RAC = xmlRAC;
                                researcher.InTeam = inTeam;
                            }
                            else
                            {
                                // create researcher
                                project.Researchers.Add(new Researcher()
                                {
                                    CPID = xmlCpid,
                                    TotalCredit = xmlTotalCredit,
                                    RAC = xmlRAC,
                                    InTeam = inTeam,
                                    UserId = xmlProjectUserId,
                                });
                            }
                            
                            int changes = await _db.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        await reader.ReadAsync();
                    }
                }
            }
        
            project.LastSyncUtc = localFileLastModified;

            if (await _db.SaveChangesAsync() > 0)
            {
                _logger.Information("Finished parsing file: {0} Found: {1} researchers", filename, project.Researchers.Count);
                return true;
            }

            return false;
        }
    }
}