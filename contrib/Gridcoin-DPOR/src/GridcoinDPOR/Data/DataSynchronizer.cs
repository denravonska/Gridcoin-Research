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
        public const double NEURAL_NETWORK_MULTIPLIER = 115000;

        private ILogger _logger = new NullLogger();
        public ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value;}
        }

        private readonly GridcoinContext _db;
        private readonly FileDownloader _fileDownloader;

         public DataSynchronizer(
             ILogger logger,
             GridcoinContext dbContext,
             FileDownloader fileDownloader)
        {
            _logger = logger;
            _db = dbContext;
            _fileDownloader = fileDownloader;
        }

        public DataSynchronizer(
            GridcoinContext dbContext,
            FileDownloader fileDownloader)
        {
            _db = dbContext;
            _fileDownloader = fileDownloader;
        }

        public async Task SyncAsync(string dataDirectory, string syncDataXml, bool teamOption)
        {
            var dporDir = Path.Combine(dataDirectory, "DPOR");
            var statsDir = Path.Combine(dporDir, "stats");

            if (!Directory.Exists(statsDir))
            {
                Directory.CreateDirectory(statsDir);
            }

            try
            {
                await SyncResearchersAsync(syncDataXml);
                await SyncProjectsAsync(syncDataXml);
                await DownloadProjectXmlFilesAsync(statsDir);
                await AssignGridcoinTeamIdsAsync(statsDir);
                await SyncUserStatsAsync(statsDir);
                await CalculateMagnitudes();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Failed to Sync data");
            }
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
                string teamGzip = Path.Combine(statsDir, project.GetTeamGzipFilename());
                foreach (var teamUrl in project.GetTeamUrls())
                {
                    bool result = await _fileDownloader.DownloadFileAsync(teamUrl, teamGzip);
                    if (result)
                    {
                        break;
                    }
                }
            }

            _logger.Information("Downloading User XML files that are newer than local files in stats");
            foreach (var project in projects)
            {
                string userGzip = Path.Combine(statsDir, project.GetUserGzipFilename());
                foreach (var userUrl in project.GetUserUrls())
                {
                    bool result = await _fileDownloader.DownloadFileAsync(userUrl, userGzip);
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

            _logger.Information("Found: {0} projects without a TeamID. Searching Team XML files" , projects.Count());

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
        
        private async Task SyncUserStatsAsync(string statsDir)
        {
            var projects = await _db.Projects.ToListAsync();
            var researchers = await _db.Researchers.ToListAsync();

            _logger.Information("Updating researcher stats for: {0} projects" , projects.Count());

            var readerSettings = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                Async = true
            };

            foreach (var project in projects)
            {
                var userGzipPath = Path.Combine(statsDir, project.GetUserGzipFilename());
                if (!File.Exists(userGzipPath))
                {
                    _logger.Error("Can't update stats for project: {0} because the user XML file is missing from: {1}", project.Name, userGzipPath);
                    continue;
                }

                using (var fileStream = File.OpenRead(userGzipPath))
                using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = XmlReader.Create(gzipStream, readerSettings))
                {
                    _logger.Debug("Opened file: {0} for parsing", userGzipPath);
                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
                        {
                            string xml = await reader.ReadInnerXmlAsync();
                            string xmlCpid = XmlUtil.ExtractXml(xml, "cpid");

                            // dont bother writing timestamps older than 32 days since we base mag off of RAC
                            double xmlExpAvgTime = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_time"));
                            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(xmlExpAvgTime);
                            double minutes = (DateTime.UtcNow - dateTime).TotalMinutes;
                            if (minutes > (60 * 24 * 32))
                            {
                                continue;
                            }

                            var researcher = researchers.SingleOrDefault(x => x.CPID == xmlCpid);
                            if (researcher == null)
                            {
                                continue;
                            }

                            // parse fields and store record
                            double xmlTotalCredit = Convert.ToDouble(XmlUtil.ExtractXml(xml, "total_credit"));
                            double xmlRAC = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_credit"));
                            int xmlProjectUserId = Convert.ToInt32(XmlUtil.ExtractXml(xml, "id"));

                            bool inTeam = false;
                            string xmlTeamId = XmlUtil.ExtractXml(xml, "teamid");
                            if (project.TeamId.ToString().Equals(xmlTeamId))
                            {
                                inTeam = true;
                            }

                            var projectResearcher = await _db.ProjectResearcher.SingleOrDefaultAsync(x => x.ProjectId == project.Id && x.ResearcherId == researcher.Id);
                            if (projectResearcher == null)
                            {
                                projectResearcher = new ProjectResearcher()
                                {
                                    ProjectId = project.Id,
                                    ResearcherId = researcher.Id,
                                    InTeam = inTeam,
                                    Credit = xmlTotalCredit,
                                    RAC = xmlRAC,
                                    WebUserId = xmlProjectUserId,
                                };

                                _db.ProjectResearcher.Add(projectResearcher);
                                if (await _db.SaveChangesAsync() > 0)
                                {
                                    _logger.Debug("Added stats to project: {0} for researcher with CPID: {1}", project.Name, xmlCpid);
                                }
                                else
                                {
                                    _logger.Error("Failed to add stats to project: {0} for researcher with CPID: {1}", project.Name, xmlCpid);
                                }
                            }
                            else
                            {
                                projectResearcher.InTeam = inTeam;
                                projectResearcher.Credit = xmlTotalCredit;
                                projectResearcher.RAC = xmlRAC;

                                if (await _db.SaveChangesAsync() > 0)
                                {
                                    _logger.Debug("Updated stats for project: {0} and researcher with CPID: {1}", project.Name, xmlCpid);
                                }
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
          
        private async Task CalculateMagnitudes()
        {
            _logger.Information("Start calculating magnitudes. Clearing previous calculated fields");
            await _db.Database.ExecuteSqlCommandAsync(string.Format("UPDATE Projects SET {0}=0", nameof(Project.TotalRAC)));
            await _db.Database.ExecuteSqlCommandAsync(string.Format("UPDATE Projects SET {0}=0", nameof(Project.TeamRAC)));
            await _db.Database.ExecuteSqlCommandAsync(string.Format("UPDATE Researchers SET {0}=0", nameof(Researcher.TotalMag)));
            await _db.Database.ExecuteSqlCommandAsync(string.Format("UPDATE Researchers SET {0}=0", nameof(Researcher.TotalMagNTR)));
            await _db.Database.ExecuteSqlCommandAsync(string.Format("UPDATE ProjectResearcher SET {0}=0", nameof(ProjectResearcher.ProjectMag)));
            await _db.Database.ExecuteSqlCommandAsync(string.Format("UPDATE ProjectResearcher SET {0}=0", nameof(ProjectResearcher.ProjectMagNTR)));
            _logger.Information("Finished clearing previous calculated fields");

            _logger.Information("Start calculating Project Total RAC");

            var projects = await _db.Projects.ToListAsync();
            foreach (var project in projects)
            {
                project.TotalRAC = await _db.ProjectResearcher.Where(x => x.ProjectId == project.Id).SumAsync(x => x.RAC);
                project.TeamRAC = await _db.ProjectResearcher.Where(x => x.ProjectId == project.Id && x.InTeam == true).SumAsync(x => x.RAC);
            }

            if (await _db.SaveChangesAsync() > 0)
            {
                _logger.Information("Finished calculating Project Total RAC and Team RAC");
            }
            else
            {
                _logger.Error("Failed to calculate Project Total RAC and Team RAC");
            }

            var researchers = await _db.Researchers
                                       .Include(x => x.ProjectResearchers)
                                       .ToListAsync();

            
            foreach(var p in projects)
            {
                _logger.Information("Calculating individual magnitudes for researchers on project: {0}", p.Name);
                foreach(var researcher in researchers)
                {
                    var projectResearcher = researcher.ProjectResearchers.SingleOrDefault(x => x.ProjectId == p.Id && x.ResearcherId == researcher.Id && x.RAC > 0);
                    if (projectResearcher != null)
                    {
                        double mag = Math.Round(((projectResearcher.RAC / (p.TeamRAC + 0.01)) / (projects.Count + 0.01)) *  NEURAL_NETWORK_MULTIPLIER, 2);
                        double magNTR = Math.Round(((projectResearcher.RAC / (p.TotalRAC + 0.01)) / (projects.Count + 0.01)) *  NEURAL_NETWORK_MULTIPLIER, 2);
                        projectResearcher.ProjectMag = mag;
                        projectResearcher.ProjectMagNTR = magNTR;

                        researcher.TotalMag += mag;
                        researcher.TotalMagNTR += magNTR;
                    }

                    if (researcher.TotalMag < 1 && researcher.TotalMag > 0.25)
                    {
                        researcher.TotalMag = 1;
                    }
                    else
                    {
                        researcher.TotalMag = Math.Round(researcher.TotalMag, 2);
                    }

                    if (researcher.TotalMagNTR < 1 && researcher.TotalMagNTR > 0.25)
                    {
                        researcher.TotalMagNTR = 1;
                    }
                    else
                    {
                        researcher.TotalMagNTR = Math.Round(researcher.TotalMagNTR, 2);
                    }
                }
                _logger.Information("Finished calculating individual magnitudes for researchers on project: {0}", p.Name);
            }
            
            _logger.Information("Finished calculating magnitudes. Researchers: {0} Projects: {1}", researchers.Count, projects.Count);
            await _db.SaveChangesAsync();
        }
    }
}