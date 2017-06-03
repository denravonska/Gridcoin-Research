// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GridcoinDPOR.Models;
using GridcoinDPOR.Util;

namespace GridcoinDPOR
{
    public static class Service
    {
        public static async Task<bool> SyncDPOR2(string dataDirectory)
        {
            var dporDir = Path.Combine(dataDirectory, "NeuralNetwork2");
            var syncFile = Path.Combine(dporDir, "syncing.lck");
            
            if (!Directory.Exists(dporDir)) 
            {
                Directory.CreateDirectory(dporDir);
            }
            
            try
            {
                // EXTRACT LIST OF WHITELISTED PROJECTS FROM XML DATA
                var syncDataFile = Path.Combine(dataDirectory, "syncdpor.dat");
                if (!File.Exists(syncDataFile))
                {
                    throw new Exception("Could not load syncdpor.dat");
                }

                var syncData = await SyncData.LoadAsync(syncDataFile);
                var users = new List<User>();
                
                // DOWNLOAD AND EXTRACT FILES
                foreach(var project in syncData.Whitelist)
                {
                    // get team file
                    foreach(var teamUrl in project.GetTeamUrls())
                    {
                        var teamGzip = Path.Combine(dporDir, project.Name.ToLower().Replace(" ", "_") + "_team.gz");
                        var teamXml = Path.Combine(dporDir, project.Name.ToLower().Replace(" ", "_") + "_team.xml");
                        var teamGzipDownloadResult = await WebUtil.DownloadFile(teamUrl, teamGzip);
                        if (teamGzipDownloadResult)
                        {
                            if (await GZipUtil.DecompressGZipFile(teamGzip, teamXml))
                            {
                                // get the team id from the file
                                project.TeamId = TeamXmlParser.GetTeamIdByTeamName(teamXml, "Gridcoin");
                            }
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Failed to download team file from URL: {0}", teamUrl);
                        }
                    }

                    // get the user file
                    foreach(var userUrl in project.GetUserUrls())
                    {
                        var userGzip = Path.Combine(dporDir, project.Name.ToLower().Replace(" ", "_") + "_user.gz");
                        var userXml = Path.Combine(dporDir, project.Name.ToLower().Replace(" ", "_") + "_user.xml");
                        var userGzipDownloadResult = await WebUtil.DownloadFile(userUrl, userGzip);
                        if (userGzipDownloadResult)
                        {
                            if (await GZipUtil.DecompressGZipFile(userGzip, userXml))
                            {
                                // get user data from xml
                                var usersInProject = UserXmlParser.GetUsersInTeamWithBeacon(userXml, project.TeamId, syncData.CpidData);
                            }
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Failed to download team file from URL: {0}", userUrl);
                        }
                    }
                }

                // CALCULATE MAGNITUDES


                return false;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}