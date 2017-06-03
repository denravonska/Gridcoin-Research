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
    public static class TeamXmlParser
    {
        public static string GetTeamIdByTeamName(string filePath, string teamName)
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                using (var xmlReader = XmlReader.Create(fileStream, new XmlReaderSettings() { DtdProcessing = DtdProcessing.Prohibit, IgnoreWhitespace = true }))
                {
                    var doc = XDocument.Load(xmlReader);
                    var nonamespace = XNamespace.None;
                    var teams = (from team in doc.Descendants(nonamespace + "team")
                    where team.Element("name").Value.Equals("gridcoin", StringComparison.CurrentCultureIgnoreCase)
                    select new 
                    {
                        Id = team.Element("id").Value,
                        Name = team.Element("name").Value,
                    }).ToList();

                    return teams.Single().Id;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex);
                return "";
            }
        }
    }
}