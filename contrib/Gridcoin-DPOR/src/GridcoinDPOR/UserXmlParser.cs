// // Copyright (c) 2017 The Gridcoin Developers
// // Distributed under the MIT/X11 software license, see the accompanying
// // file COPYING or http://www.opensource.org/licenses/mit-license.php.

// using System.Collections.Generic;
// using System.IO;
// using System.Xml;
// using System.Xml.Linq;
// using System.Linq;
// using XmlTextReader = System.Xml.XmlReader;
// using GridcoinDPOR.Models;
// using System;
// using Serilog;
// using GridcoinDPOR.Logging;
// using System.Threading.Tasks;
// using System.Text;
// using GridcoinDPOR.Util;
// using System.Globalization;

// namespace GridcoinDPOR
// {
//     public static class UserXmlParser
//     {
//         private static ILogger _logger = new NullLogger();
//         public static ILogger Logger 
//         { 
//             get { return _logger; } 
//             set { _logger = value;}
//         }

//         public static async Task<IEnumerable<User>> GetUsersWithBeaconAsync(string filePath, IEnumerable<CpidData> cpids)
//         {
//             var filename = Path.GetFileName(filePath);
//             var users = new List<User>();
//             var readerSettings = new XmlReaderSettings()
//             {
//                 DtdProcessing = DtdProcessing.Prohibit,
//                 IgnoreProcessingInstructions = true,
//                 IgnoreWhitespace = true,
//                 IgnoreComments = true,
//                 Async = true
//             };

//             using (var fileStream = File.OpenRead(filePath))
//             using (var reader = XmlReader.Create(fileStream, readerSettings))
//             {
//                 _logger.ForContext(nameof(UserXmlParser)).Information("Started parsing {0} for CPID's and Credit", filename);
//                 while(!reader.EOF)
//                 {
//                     if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
//                     {
//                         var xml = await reader.ReadInnerXmlAsync();
//                         var recordCpid = XmlUtil.ExtractXml(xml, "cpid");
//                         if (cpids.Any(a => a.CPID == recordCpid))
//                         {
//                             users.Add(new User()
//                             {
//                                 CPID = recordCpid,
//                                 TotalCredit = Convert.ToDouble(XmlUtil.ExtractXml(xml, "total_credit")),
//                                 RAC = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_credit")),
//                                 ProjectUserID = Convert.ToInt32(XmlUtil.ExtractXml(xml, "id")),
//                             });
//                         }
//                     }
//                     else
//                     {
//                         await reader.ReadAsync();
//                     }
//                 }
//             }
//             _logger.ForContext(nameof(UserXmlParser)).Information("Finished parsing {0} and found {1} CPID's", filename, users.Count);
//             return users;
//         }
//         public static async Task<IEnumerable<User>> GetUsersInTeamWithBeaconAsync(string filePath, int teamId, IEnumerable<CpidData> cpids)
//         {
//             var filename = Path.GetFileName(filePath);
//             var users = new List<User>();
//             var readerSettings = new XmlReaderSettings()
//             {
//                 DtdProcessing = DtdProcessing.Prohibit,
//                 IgnoreProcessingInstructions = true,
//                 IgnoreWhitespace = true,
//                 IgnoreComments = true,
//                 Async = true
//             };

//             using (var fileStream = File.OpenRead(filePath))
//             using (var reader = XmlReader.Create(fileStream, readerSettings))
//             {
//                 _logger.ForContext(nameof(UserXmlParser)).Information("Started parsing {0} for CPID's and Credit with TeamID: {1}", filename, teamId);
//                 while(!reader.EOF)
//                 {
//                     if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
//                     {
//                         var xml = await reader.ReadInnerXmlAsync();
//                         var recordCpid = XmlUtil.ExtractXml(xml, "cpid");
//                         if (cpids.Any(a => a.CPID == recordCpid))
//                         {
//                             var recordTeamId = XmlUtil.ExtractXml(xml, "teamid");
//                             if (teamId.ToString().Equals(recordTeamId))
//                             {
//                                 users.Add(new User()
//                                 {
//                                     CPID = recordCpid,
//                                     TotalCredit = Convert.ToDouble(XmlUtil.ExtractXml(xml, "total_credit")),
//                                     RAC = Convert.ToDouble(XmlUtil.ExtractXml(xml, "expavg_credit")),
//                                     ProjectUserID = Convert.ToInt32(XmlUtil.ExtractXml(xml, "id")),
//                                 });
//                             }
//                         }
//                     }
//                     else
//                     {
//                         await reader.ReadAsync();
//                     }
//                 }
//             }
//             _logger.ForContext(nameof(UserXmlParser)).Information("Finished parsing {0} and found {1} CPID's", filename, users.Count);
//             return users;
//         }
//     }
// }