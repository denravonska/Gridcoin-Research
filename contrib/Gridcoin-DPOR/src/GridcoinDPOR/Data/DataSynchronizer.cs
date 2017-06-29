// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using GridcoinDPOR.Data.Models;
using GridcoinDPOR.Logging;
using GridcoinDPOR.Util;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

        public async Task<bool> Sync(string dataDirectory, string syncDataXml, bool teamOption)
        {
            var dporDir = Path.Combine(dataDirectory, "DPOR");
            var statsDir = Path.Combine(dporDir, "stats");

            if (!Directory.Exists(statsDir))
            {
                Directory.CreateDirectory(statsDir);
            }

            await SyncResearchersAsync(syncDataXml);
            await SyncProjectsAsync(syncDataXml);
            await DownloadProjectXmlFilesAsync(statsDir);
            await AssignGridcoinTeamIdsAsync(statsDir);

            return false;
        }

        private async Task SyncResearchersAsync(string syncDataXml)
        {
            _logger.Information("Syncronizing researchers with local database");
            var cpidDataXml = XmlUtil.ExtractXml(syncDataXml, "CPIDDATA");
            var cpidDataRows = cpidDataXml.Split(new string[] { "<ROW>" }, StringSplitOptions.RemoveEmptyEntries);

            // TODO: Not sure what this data is?
            var testNet = cpidDataRows[0].Split(new string[] { "<COL>" }, StringSplitOptions.RemoveEmptyEntries);

            var cpids = new List<string>();

            foreach (var row in cpidDataRows)
            {
                if (row.Length > 20)
                {
                    var columns = row.Split(new string[] { "<COL>" }, StringSplitOptions.RemoveEmptyEntries);
                    string cpidExtraData = Encoding.UTF8.GetString(Convert.FromBase64String(columns[1]));
                    var cpidExtraFields = cpidExtraData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    string cpid = columns[0];
                    string cpid2 = cpidExtraFields[0];
                    string blockHash = cpidExtraFields[1];
                    string address = cpidExtraFields[2];

                    cpids.Add(cpid);

                    var researcher = await _db.Researchers.SingleOrDefaultAsync(x => x.CPID == cpid);
                    if (researcher == null)
                    {
                        researcher = new Researcher()
                        {
                            CPID = cpid,
                            CPIDv2 = cpid2,
                            BlockHash = blockHash,
                            Address = address,
                        };

                        _db.Researchers.Add(researcher);
                        if (await _db.SaveChangesAsync() > 0)
                        {
                            _logger.Debug("Added CPID: {0} to db", researcher.CPID);
                        }
                    }
                    else
                    {
                        researcher.CPIDv2 = cpid2;
                        researcher.BlockHash = blockHash;
                        researcher.Address = address;
                        if (await _db.SaveChangesAsync() > 0)
                        {
                            _logger.Debug("Updated CPID: {0} in db", researcher.CPID);
                        }
                    }
                }
            }

            _logger.Information("Successfully Stored: {0} CPIDS", cpids.Count);

            _logger.Information("Searching for researchers with expired beacons to delete");
            var cpidsToDelete = _db.Researchers.Where(x => !cpids.Contains(x.CPID)).ToList();

            if (cpidsToDelete.Any())
            {
                foreach(var cpidToDelete in cpidsToDelete)
                {
                    _db.Researchers.Remove(cpidToDelete);
                    if (await _db.SaveChangesAsync() > 0)
                    {
                        _logger.Debug("Deleted CPID: {0} and its related data as beacon has expired", cpidToDelete.CPID);
                    }
                    else
                    {
                        _logger.Error("Failed to delete CPID: {0} and its releated data", cpidToDelete.CPID);
                    }
                }
                _logger.Information("Finished removing expired researchers with expired beacons");
            }
            else
            {
                _logger.Information("No expired researcher beacons to delete");
            }
        }

        private async Task SyncProjectsAsync(string syncDataXml)
        {
            _logger.Information("Syncronizing projects with local database");
            string whitelistXml = XmlUtil.ExtractXml(syncDataXml, "WHITELIST");
            var whitelistRows = whitelistXml.Split(new string[] {"<ROW>"}, StringSplitOptions.RemoveEmptyEntries);
            var projectNames = new List<string>();

            foreach (var row in whitelistRows)
            {
                var columns = row.Split(new string[] { "<COL>" }, StringSplitOptions.RemoveEmptyEntries);

                string projectName = columns[0];
                string projectUrl = columns[1];
                projectNames.Add(projectName);

                var project = await _db.Projects.SingleOrDefaultAsync(x => x.Name == projectName);
                if (project == null)
                {
                    project = new Project()
                    {
                        Name = projectName,
                        Url = projectUrl,
                    };

                    _db.Projects.Add(project);
                    if (await _db.SaveChangesAsync() > 0)
                    {
                        _logger.Debug("Added new project: {0} to db", projectName);
                    }
                }
                else
                {
                    project.Url = projectUrl;
                    if (await _db.SaveChangesAsync() > 0)
                    {
                        _logger.Debug("Updated existing project: {0} in db", projectName);
                    }
                }
            }
            _logger.Information("Successfully Stored: {0} Projects", projectNames.Count);
            _logger.Information("Searching for none-whitlisted projects to delete");
            var projectsToDelete = _db.Projects.Where(x => !projectNames.Contains(x.Name)).ToList();
            if (projectsToDelete.Any())
            {
                _logger.Information("Removing: {0} projects no longer whitelisted from the local database", projectsToDelete.Count);
                foreach(var projectToDelete in projectsToDelete)
                {
                    _db.Projects.Remove(projectToDelete);
                    if (await _db.SaveChangesAsync() > 0)
                    {
                        _logger.Debug("Deleted project: {0} and its related data as no longer in the whitelist", projectToDelete.Name);
                    }
                    else
                    {
                        _logger.Error("Failed to delete project: {0} and its related data", projectToDelete.Name);
                    }
                }
                _logger.Information("Finished removing projects");
            }
            else
            {
                _logger.Information("No none-whitlisted projects to delete");
            }
        }

        private async Task DownloadProjectXmlFilesAsync(string statsDir)
        {
            var projects = await _db.Projects.ToListAsync();

            _logger.Information("Downloading Team XML files for projects without a Gridcoin Team ID");
            foreach (var project in projects.Where(x => x.TeamId == 0).ToList())
            {
                foreach (var teamUrl in project.GetTeamUrls())
                {
                    string teamGzip = Path.Combine(statsDir, project.GetUserGzipFilename());
                    bool result = await WebUtil.DownloadFileAsync(teamUrl, teamGzip);
                    if (result)
                    {
                        break;
                    }
                }
            }

            _logger.Information("Downloading User XML files that are newer than local files in stats");
            foreach (var project in projects)
            {
                foreach (var userUrl in project.GetUserUrls())
                {
                    string userGzip = Path.Combine(statsDir, project.GetUserGzipFilename());
                    bool result = await WebUtil.DownloadFileAsync(userUrl, userGzip);
                    if (result)
                    {
                        break;
                    }
                }
            }
        }

        private async Task AssignGridcoinTeamIdsAsync(string statsDir)
        {
            var projects = _db.Projects.Where(x => x.TeamId == 0);

            _logger.Information("Searching for Gridcoin TeamID for {0} projects without a TeamID" , projects.Count());

            var readerSettings = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                Async = true
            };

            foreach(var project in projects)
            {
                var teamGzipPath = Path.Combine(statsDir, project.GetTeamGzipFilename());

                if (!File.Exists(teamGzipPath))
                {
                    _logger.Error("Failed to load TeamID from missing Team file {0}", teamGzipPath);
                    continue;
                }
      
                using (var inputFileStream = File.OpenRead(teamGzipPath))
                using (var gzipStream = new GZipStream(inputFileStream, CompressionMode.Decompress))
                using (var reader = XmlReader.Create(gzipStream, readerSettings))
                {
                    _logger.Debug("Opened file {0} for parsing", project.GetTeamGzipFilename());
                    while(!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "team")
                        {
                            var xml = await reader.ReadInnerXmlAsync();
                            var teamName = XmlUtil.ExtractXml(xml, "name");
                            if (teamName.Equals("gridcoin", StringComparison.CurrentCultureIgnoreCase))
                            {
                                var teamId = Convert.ToInt32(XmlUtil.ExtractXml(xml, "id"));
                                _logger.Information("Found TeamID: {0} with the name Gridcoin in the file {1}", teamId, project.GetTeamGzipFilename());
                                project.TeamId = teamId;

                                await _db.SaveChangesAsync();
                                break;
                            }
                        }
                        else
                        {
                            await reader.ReadAsync();
                        }
                    }
                }
            }
        }
        

        // public async Task<bool> SyncDPOR2(string dataDirectory, string syncDataXml, bool teamOption)
        // {
        //     var dporDir = Path.Combine(dataDirectory, "DPOR");
        //     var statsDir = Path.Combine(dporDir, "stats");
            
        //     if (!Directory.Exists(statsDir)) 
        //     {
        //         Directory.CreateDirectory(statsDir);
        //     }
            
        //     try
        //     {
        //         // EXTRACT LIST OF WHITELISTED PROJECTS FROM XML DATA
        //         var syncData = SyncData.Parse(syncDataXml);

        //         //TODO: Delete any projects from the DB that are no longer white-listed
                
        //         // DOWNLOAD AND EXTRACT FILES
        //         foreach(var project in syncData.Whitelist)
        //         {
        //             // only search for the Team ID when -noteam is not set in args
        //             if(teamOption)
        //             {
        //                 foreach(var teamUrl in project.GetTeamUrls())
        //                 {
        //                     string teamGzip = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_team.gz");
        //                     bool teamGzipDownloadResult = await WebUtil.DownloadFile(teamUrl, teamGzip);
        //                     if (teamGzipDownloadResult)
        //                     {
        //                         project.TeamId = await TeamXmlParser.GetGridcoinTeamIdAsync(teamGzip);
        //                         break;
        //                     }
        //                     else
        //                     {
        //                         _logger.Warning("Failed to download team file from URL: {0}", teamUrl);
        //                     }
        //                 }
        //             }

        //             // get the user file
        //             foreach(var userUrl in project.GetUserUrls())
        //             {
        //                 string userGzip = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_user.gz");
        //                 bool userGzipDownloadResult = await WebUtil.DownloadFile(userUrl, userGzip);
        //                 if (userGzipDownloadResult)
        //                 {
        //                     _logger.Information("Syncing stats for project: {0}", project.Name);
        //                     if(await ParseAndStoreUserStats(userGzip, project.Name, project.TeamId, syncData.CpidData))
        //                     {
        //                         _logger.Information("Successfully Synchronized stats for project: {0}", project.Name);
        //                     }
        //                     break;
        //                 }
        //                 else
        //                 {
        //                     _logger.Warning("Failed to download user file from URL: {0}", userUrl);
        //                 }
        //             }
        //         }

        //         // not sure if it's worth returning anything?
        //         return true;
        //     }
        //     catch(Exception ex)
        //     {
        //         _logger.Fatal("SyncDPOR2 command failed with exception {0}", ex);
        //         return false;
        //     }
        // }

        // private async Task<bool> ParseAndStoreUserStats(string filePath, string projectName, int teamId, IEnumerable<CpidData> beaconCpids)
        // {
        //     string filename = Path.GetFileName(filePath);
        //     var localFileLastModified = File.GetLastAccessTimeUtc(filePath);

        //     // get project from DB
        //     var project = _db.Projects.Include(x => x.Researchers).SingleOrDefault(x => x.Name.Equals(projectName));
        //     if (project != null)
        //     {
        //         // TODO: delete any researchers from the project that have expired beacons
        //         // TODO: delete any researchers with RAC time older than 32 days


        //         if (project.LastSyncUtc == localFileLastModified)
        //         {
        //             _logger.Information("Data for project already up to date. Skipping project {0}", project.Name);
        //             return true;
        //         }
        //     }
        //     else 
        //     {
        //         project = new Project()
        //         {
        //             Name = projectName,
        //         };

        //         _db.Projects.Add(project);

        //         int changes = await _db.SaveChangesAsync();
        //     }
            
        //     var readerSettings = new XmlReaderSettings()
        //     {
        //         DtdProcessing = DtdProcessing.Prohibit,
        //         IgnoreProcessingInstructions = true,
        //         IgnoreWhitespace = true,
        //         IgnoreComments = true,
        //         Async = true
        //     };
            
        //     int researchersFound = 0;

        //     using (var fileStream = File.OpenRead(filePath))
        //     using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
        //     using (var reader = XmlReader.Create(gzipStream, readerSettings))
        //     {
        //         _logger.Information("Opened file {0} for parsing", filename);
        //         while(!reader.EOF)
        //         {
        //             if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
        //             {
        //                 string xml = await reader.ReadInnerXmlAsync();
        //                 string xmlCpid = XmlUtil.ExtractXml(xml, "cpid");
                        
        //                 // only extract if this CPID is in the list of beacons
        //                 if (!beaconCpids.Any(a => a.CPID == xmlCpid))
        //                 {
        //                     await reader.ReadAsync();
        //                     continue;
        //                 }

        //                 // dont bother writing timestamps older than 32 days since we base mag off of RAC
        //                 double xmlExpAvgTime = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_time"));
        //                 var dateTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(xmlExpAvgTime);
        //                 double minutes = (DateTime.UtcNow - dateTime).TotalMinutes;
        //                 if (minutes > (60 * 24 * 32))
        //                 {
        //                     await reader.ReadAsync();
        //                     continue;
        //                 }

        //                 researchersFound++;

        //                 // parse fields and store record
        //                 double xmlTotalCredit = Convert.ToDouble(XmlUtil.ExtractXml(xml, "total_credit"));
        //                 double xmlRAC = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_credit"));
        //                 int xmlProjectUserId = Convert.ToInt32(XmlUtil.ExtractXml(xml, "id"));

        //                 bool inTeam = false;
        //                 string xmlTeamId = XmlUtil.ExtractXml(xml, "teamid");
        //                 if (teamId.ToString().Equals(xmlTeamId))
        //                 {
        //                     inTeam = true;
        //                 }

        //                 var researcher = project.Researchers.SingleOrDefault(x => x.CPID == xmlCpid);
        //                 if (researcher != null)
        //                 {
        //                     // edit Credit and RAC
        //                     researcher.TotalCredit = xmlTotalCredit;
        //                     researcher.RAC = xmlRAC;
        //                     researcher.InTeam = inTeam;
        //                 }
        //                 else
        //                 {
        //                     // create researcher
        //                     project.Researchers.Add(new Researcher()
        //                     {
        //                         CPID = xmlCpid,
        //                         TotalCredit = xmlTotalCredit,
        //                         RAC = xmlRAC,
        //                         InTeam = inTeam,
        //                         UserId = xmlProjectUserId,
        //                     });
        //                 }
                        
        //                 int changes = await _db.SaveChangesAsync();
        //             }
        //             else
        //             {
        //                 await reader.ReadAsync();
        //             }
        //         }
        //     }
        
        //     project.LastSyncUtc = localFileLastModified;

        //     if (await _db.SaveChangesAsync() > 0)
        //     {
        //         _logger.Information("Finished parsing file and found: {0} researchers", researchersFound);
        //         return true;
        //     }

        //     return false;
        // }
    }
}