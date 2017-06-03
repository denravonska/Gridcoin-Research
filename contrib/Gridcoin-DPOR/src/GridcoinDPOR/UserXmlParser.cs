// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using XmlTextReader = System.Xml.XmlReader;
using GridcoinDPOR.Models;
using System;

namespace GridcoinDPOR
{
    public static class UserXmlParser
    {
        public static IEnumerable<User> GetUsersInTeamWithBeacon(string filePath, string teamId, IEnumerable<CpidData> cpids)
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                using (var xmlReader = XmlReader.Create(fileStream, new XmlReaderSettings() { DtdProcessing = DtdProcessing.Prohibit, IgnoreWhitespace = true }))
                {
                    var doc = XDocument.Load(xmlReader);
                    var nonamespace = XNamespace.None;
                    var users = (from user in doc.Descendants(nonamespace + "user")
                    where cpids.Any(x => x.CPID == user.Element("cpid").Value)
                    select new User
                    {
                        CPID = user.Element("cpid").Value,
                        Name = user.Element("name").Value,
                        TotalCredit = Convert.ToDouble(user.Element("total_credit").Value),
                        RAC = Convert.ToDouble(user.Element("expavg_credit").Value), 
                    }).ToList();

                    return users;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex);
                return new List<User>();
            }
        }
    }
}