// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GridcoinDPOR.Logging;
using GridcoinDPOR.Models;
using GridcoinDPOR.Util;
using Serilog;

namespace GridcoinDPOR
{
    public static class Service
    {
        private static ILogger _logger = new NullLogger();
        public static ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value;}
        }
        
        public static async Task<bool> SyncDPOR2(string dataDirectory, string syncDataXml)
        {
            var dporDir = Path.Combine(dataDirectory, "DPOR");
            var statsDir = Path.Combine(dporDir, "stats");

            var syncFile = Path.Combine(dporDir, "syncing.lck");
            
            if (!Directory.Exists(statsDir)) 
            {
                Directory.CreateDirectory(statsDir);
            }
            
            try
            {
                // EXTRACT LIST OF WHITELISTED PROJECTS FROM XML DATA
                var syncData = SyncData.Parse(syncDataXml);
                var users = new List<User>();
                
                // DOWNLOAD AND EXTRACT FILES
                foreach(var project in syncData.Whitelist)
                {
                    // get team file
                    foreach(var teamUrl in project.GetTeamUrls())
                    {
                        var teamGzip = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_team.gz");
                        var teamXml = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_team.xml");
                        var teamGzipDownloadResult = await WebUtil.DownloadFile(teamUrl, teamGzip);
                        if (teamGzipDownloadResult)
                        {
                            if (await GZipUtil.DecompressGZipFile(teamGzip, teamXml))
                            {
                                project.TeamId = await TeamXmlParser.GetGridcoinTeamIdAsync(teamXml);
                            }
                            break;
                        }
                        else
                        {
                            _logger.ForContext(nameof(Service)).Warning("Failed to download team file from URL: {0}", teamUrl);
                        }
                    }

                    // get the user file
                    foreach(var userUrl in project.GetUserUrls())
                    {
                        var userGzip = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_user.gz");
                        var userXml = Path.Combine(statsDir, project.Name.ToLower().Replace(" ", "_") + "_user.xml");
                        var userGzipDownloadResult = await WebUtil.DownloadFile(userUrl, userGzip);
                        if (userGzipDownloadResult)
                        {
                            if (await GZipUtil.DecompressGZipFile(userGzip, userXml))
                            {
                                var usersInProject = await UserXmlParser.GetUsersInTeamWithBeaconAsync(userXml, project.TeamId, syncData.CpidData);
                            }
                            break;
                        }
                        else
                        {
                            _logger.ForContext(nameof(Service)).Warning("Failed to download user file from URL: {0}", userUrl);
                        }
                    }
                }

                // CALCULATE MAGNITUDES


                return false;
            }
            catch(Exception ex)
            {
                _logger.ForContext(nameof(Service)).Fatal("SyncDPOR2 command failed with exception {0}", ex);
                return false;
            }
        }
    }
}