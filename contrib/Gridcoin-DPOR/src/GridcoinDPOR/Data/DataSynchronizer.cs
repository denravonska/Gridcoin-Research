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

        private string _dataDirectory;
        private string _downloadsDirectory;
        private string _syncDataXml;
        private bool _beaconDataChanged;

        private readonly GridcoinContext _db;
        private readonly FileDownloader _fileDownloader;
        private readonly QuorumHashingAlgorithm _hashAlgo;

         public DataSynchronizer(
             ILogger logger,
             GridcoinContext dbContext,
             FileDownloader fileDownloader)
        {
            _logger = logger;
            _db = dbContext;
            _fileDownloader = fileDownloader;
            _hashAlgo = new QuorumHashingAlgorithm();
        }

        public DataSynchronizer(
            GridcoinContext dbContext,
            FileDownloader fileDownloader)
        {
            _db = dbContext;
            _fileDownloader = fileDownloader;
            _hashAlgo = new QuorumHashingAlgorithm();
        }

        public async Task SyncAsync(string dataDirectory)
        {
            _dataDirectory = Path.Combine(dataDirectory, "DPOR");
            _downloadsDirectory = Path.Combine(_dataDirectory, "stats");

            if (!Directory.Exists(_downloadsDirectory))
            {
                Directory.CreateDirectory(_downloadsDirectory);
            }

            string syncDataFile = Path.Combine(_dataDirectory, "syncdpor.dat");
            if (!File.Exists(syncDataFile))
            {
                _logger.Fatal("Failed to locate the syncdpor.dat file in the DPOR directory");
                throw new Exception("Failed to locate syncdpor.dat in the DPOR directory");
            }

            _syncDataXml = await FileUtil.ReadAllTextAsync(syncDataFile);

            try
            {
                await SyncResearchersAsync();
                await SyncProjectsAsync();
                await DownloadProjectXmlFilesAsync();
                await AssignGridcoinTeamIdsAsync();
                await SyncUserStatsAsync();
                await CalculateMagnitudes();
                await GenerateContract();
                await GenerateContractNoTeam();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Failed to Sync data");
            }
        }

        private async Task SyncResearchersAsync()
        {
            _logger.Information("Syncronizing researchers with local database");
            var cpidDataXml = XmlUtil.ExtractXml(_syncDataXml, "CPIDDATA");
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
                    }
                    else
                    {
                        researcher.CPIDv2 = cpid2;
                        researcher.BlockHash = blockHash;
                        researcher.Address = address;
                    }
                }
            }

            _logger.Information("Found {0} CPIDS in the sync data XML", cpids.Count);

            var cpidsToDelete = _db.Researchers.Where(x => !cpids.Contains(x.CPID)).ToList();
            if (cpidsToDelete.Any())
            {
                _logger.Information("Found {0} researchers with expired beacons to delete", cpidsToDelete.Count);
                _db.Researchers.RemoveRange(cpidsToDelete);
            }

            int changes = await _db.SaveChangesAsync();
            if (changes > 0)
            {
                _beaconDataChanged = true;
            }

            _logger.Debug("{0} changes made to the Researchers table", changes);
        }

        private async Task SyncProjectsAsync()
        {
            _logger.Information("Syncronizing projects with local database");
            string whitelistXml = XmlUtil.ExtractXml(_syncDataXml, "WHITELIST");
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
                }
                else
                {
                    project.Url = projectUrl;
                }
            }

            _logger.Information("Found {0} white-listed Projects in the sync data XML", whitelistRows.Count());

            var projectsToDelete = _db.Projects.Where(x => !projectNames.Contains(x.Name)).ToList();
            if (projectsToDelete.Any())
            {
                _logger.Information("Found {0} projects that need deleted", projectsToDelete.Count);
                _db.Projects.RemoveRange(projectsToDelete);
            }

            int changes = await _db.SaveChangesAsync();
            if (changes > 0)
            {
                _beaconDataChanged = true;
            }

            _logger.Debug("{0} changes made to the Projects table", changes);
        }

        private async Task DownloadProjectXmlFilesAsync()
        {
            var projects = await _db.Projects.ToListAsync();

            _logger.Information("Downloading Team XML files for projects without a Gridcoin Team ID");
            foreach (var project in projects.Where(x => x.TeamId == 0).ToList())
            {
                string teamGzip = Path.Combine(_downloadsDirectory, project.GetTeamGzipFilename());
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
                string userGzip = Path.Combine(_downloadsDirectory, project.GetUserGzipFilename());
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

        private async Task AssignGridcoinTeamIdsAsync()
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
                var teamGzipPath = Path.Combine(_downloadsDirectory, project.GetTeamGzipFilename());

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
        
        private async Task SyncUserStatsAsync()
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
                var userGzipPath = Path.Combine(_downloadsDirectory, project.GetUserGzipFilename());
                if (!File.Exists(userGzipPath))
                {
                    _logger.Error("Can't update stats for project: {0} because the user XML file is missing from: {1}", project.Name, userGzipPath);
                    continue;
                }

                var lastModified = File.GetLastAccessTimeUtc(userGzipPath);
                if (lastModified == project.LastSyncUtc && _beaconDataChanged == false)
                {
                    _logger.Information("Skipping parsing of stats for project: {0} because new XML file is the same age and the beacon data has not changed", project.Name);
                    continue;
                }

                _logger.Information("Syncing stats for project: {0} with XML file Last-Modified: {1}", project.Name, lastModified);

                project.LastSyncUtc = lastModified;

                using (var fileStream = File.OpenRead(userGzipPath))
                using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = XmlReader.Create(gzipStream, readerSettings))
                {
                    _logger.Debug("Opened the file: {0} for parsing and storing stats inside the database", Path.GetFileName(userGzipPath));

                    int skipped = 0;

                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
                        {
                            string xml = await reader.ReadInnerXmlAsync();
                            string xmlCpid = XmlUtil.ExtractXml(xml, "cpid");

                            var researcher = researchers.SingleOrDefault(x => x.CPID == xmlCpid);
                            if (researcher == null)
                            {
                                continue;
                            }
                            else
                            {
                                // dont bother writing timestamps older than 32 days since we base mag off of RAC
                                double xmlExpAvgTime = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_time"));
                                var dateTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(xmlExpAvgTime);
                                double minutes = (project.LastSyncUtc - dateTime).TotalMinutes;
                                if (minutes > (60 * 24 * 32))
                                {
                                    skipped++;

                                    // if there is already a stat logged for this researcher on this project remove it
                                    var existingProjectResearcher = await _db.ProjectResearcher
                                                                             .Include(x => x.Researcher)
                                                                             .SingleOrDefaultAsync(x => x.ProjectId == project.Id && x.ResearcherId == researcher.Id);
                                    if (existingProjectResearcher != null)
                                    {
                                        _db.ProjectResearcher.Remove(existingProjectResearcher);
                                        _logger.Debug("Removed existing stats for researcher with CPID: {0} on the project: {1}", existingProjectResearcher.Researcher.CPID, project.Name);
                                    }

                                    continue;
                                }
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
                            }
                            else
                            {
                                projectResearcher.InTeam = inTeam;
                                projectResearcher.Credit = xmlTotalCredit;
                                projectResearcher.RAC = xmlRAC;
                            }
                        }
                        else
                        {
                            await reader.ReadAsync();
                        }
                    }

                    _logger.Debug("Skipped storing stats of {0} CPIDS for project: {1} because of expavg_time > 32 days from Last-Modified time", skipped, project.Name);
                    int changes = await _db.SaveChangesAsync();
                    _logger.Debug("Made {0} changes to the ProjectResearchers table", changes);
                }
            }
        }
          
        private async Task CalculateMagnitudes()
        {
            _logger.Information("Calculating Project Averages");
            if (_beaconDataChanged == false)
            {
                _logger.Information("Skipping calculation of averages and magnitudes because beacon data has not changed");
            }

            var projects = await _db.Projects.ToListAsync();
            foreach (var project in projects)
            {
                int projectResearchersCount = await _db.ProjectResearcher.CountAsync(x => x.ProjectId == project.Id);

                double noTeamTotalRAC = await _db.ProjectResearcher.Where(x => x.ProjectId == project.Id).SumAsync(x => x.RAC);
                double noTeamAvgRAC = (noTeamTotalRAC / (projectResearchersCount + 0.01));

                double teamTotalRAC = await _db.ProjectResearcher.Where(x => x.ProjectId == project.Id && x.InTeam == true).SumAsync(x => x.RAC);
                double teamAvgRAC = (teamTotalRAC / (projectResearchersCount + 0.01));

                project.NoTeamTotalRAC = noTeamTotalRAC;
                project.NoTeamAvgRAC = noTeamAvgRAC;
                project.TeamTotalRAC = teamTotalRAC;
                project.TeamAvgRAC = teamAvgRAC;
            }

            await _db.SaveChangesAsync();
            _logger.Information("Finished calculating Averages");

            var researchers = await _db.Researchers
                                       .Include(x => x.ProjectResearchers)
                                       .ToListAsync();

            foreach(var researcher in researchers)
            {
                researcher.TotalMag = 0;
                researcher.TotalMagNTR = 0;
            }

            _logger.Information("Calculating Magnitudes");
            int projectsCount = projects.Count();

            foreach(var p in projects)
            {
                _logger.Debug("Calculating individual magnitudes for researchers on project: {0}", p.Name);
                foreach(var r in researchers)
                {
                    var projectResearcher = r.ProjectResearchers.SingleOrDefault(x => x.ProjectId == p.Id && x.ResearcherId == r.Id && x.RAC > 0);
                    if (projectResearcher != null)
                    {
                        if (projectResearcher.RAC > 0)
                        {
                            double mag = Math.Round(((projectResearcher.RAC / (p.TeamTotalRAC + 0.01)) / (projectsCount + 0.01)) *  NEURAL_NETWORK_MULTIPLIER, 2);
                            double magNTR = Math.Round(((projectResearcher.RAC / (p.NoTeamTotalRAC + 0.01)) / (projectsCount + 0.01)) *  NEURAL_NETWORK_MULTIPLIER, 2);
                            projectResearcher.ProjectMag = mag;
                            projectResearcher.ProjectMagNTR = magNTR;

                            r.TotalMag += mag;
                            r.TotalMagNTR += magNTR;
                        }
                    }

                    if (r.TotalMag < 1 && r.TotalMag > 0.25)
                    {
                        r.TotalMag = 1;
                    }
                    else
                    {
                        r.TotalMag = Math.Round(r.TotalMag, 2);
                    }

                    if (r.TotalMagNTR < 1 && r.TotalMagNTR > 0.25)
                    {
                        r.TotalMagNTR = 1;
                    }
                    else
                    {
                        r.TotalMagNTR = Math.Round(r.TotalMagNTR, 2);
                    }
                }
            }
            
            await _db.SaveChangesAsync();
            _logger.Information("Finished calculating magnitudes. Researchers: {0} Projects: {1}", researchers.Count, projects.Count);
        }

        private async Task GenerateContract()
        {
            string contractPath = Path.Combine(Path.Combine(_dataDirectory, "contract.dat"));
            if (File.Exists(contractPath) && _beaconDataChanged == false)
            {
                _logger.Information("Skipping generation of the contract.dat file because it already exists and the beacon data has not changed");
                return;
            }

            var researchers = await _db.Researchers.AsNoTracking().ToListAsync();
            _logger.Information("Generating contract.dat for {0} researchers", researchers.Count);

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<MAGNITUDES>");

            foreach(var researcher in researchers)
            {
                researcher.IsValid = true;
                if (researcher.IsValid)
                {
                    string cpid = researcher.CPID + "," + Num(researcher.TotalMag) + ";";
                    if (researcher.TotalMag == 0)
                    {
                        cpid = "0,15;";
                    }
                    stringBuilder.Append(cpid);
                }
                else
                {
                    stringBuilder.Append(researcher.CPID + ",00;");
                }
            }

            stringBuilder.Append("</MAGNITUDES><QUOTES>btc,0;grc,0;</QUOTES><AVERAGES>");

            var projects = await _db.Projects.OrderBy(x => x.Name).AsNoTracking().ToListAsync();

            foreach(var project in projects)
            {
                if (project.TeamAvgRAC > 0)
                {
                    stringBuilder.Append(project.GetNameForContract() + "," + Num(project.TeamAvgRAC) + "," + Num(project.TeamTotalRAC) + ";");
                }
            }

            stringBuilder.Append("NeuralNetwork,2000000,20000000;</AVERAGES>");

            string contract = stringBuilder.ToString();
            File.WriteAllText(contractPath, contract);
        }

        private async Task GenerateContractNoTeam()
        {
            string contractPath = Path.Combine(Path.Combine(_dataDirectory, "contract-noteam.dat"));
            if (File.Exists(contractPath) && _beaconDataChanged == false)
            {
                _logger.Information("Skipping generation of the contract-noteam.dat file because it already exists and the beacon data has not changed");
                return;
            }

            var researchers = await _db.Researchers.AsNoTracking().ToListAsync();
            _logger.Information("Generating contract-noteam.dat for {0} researchers", researchers.Count);

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<MAGNITUDES>");

            foreach(var researcher in researchers)
            {
                researcher.IsValid = true;
                if (researcher.IsValid)
                {
                    string cpid = researcher.CPID + "," + Num(researcher.TotalMagNTR) + ";";
                    if (researcher.TotalMagNTR == 0)
                    {
                        cpid = "0,15;";
                    }
                    stringBuilder.Append(cpid);
                }
                else
                {
                    stringBuilder.Append(researcher.CPID + ",00;");
                }
            }

            stringBuilder.Append("</MAGNITUDES><AVERAGES>");

            var projects = await _db.Projects.OrderBy(x => x.Name).AsNoTracking().ToListAsync();

            foreach(var project in projects)
            {
                if (project.TeamAvgRAC > 0)
                {
                    stringBuilder.Append(project.GetNameForContract() + "," + Num(project.NoTeamAvgRAC) + "," + Num(project.NoTeamTotalRAC) + ";");
                }
            }

            stringBuilder.Append("NeuralNetwork,2000000,20000000;</AVERAGES>");

            string contract = stringBuilder.ToString();
            File.WriteAllText(contractPath, contract);
        }

        private string Num(double magnitude)
        {
            double dither = _hashAlgo.RoundWithDither(magnitude);
            string noSep = dither.ToString().Replace(",", ".");
            return noSep;
        }
    }
}