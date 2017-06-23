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
using Serilog;
using GridcoinDPOR.Logging;
using System.Threading.Tasks;
using GridcoinDPOR.Util;
using System.IO.Compression;

namespace GridcoinDPOR
{
    public static class TeamXmlParser
    {
        private static ILogger _logger = new NullLogger();
        public static ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value;}
        }

        public static async Task<int> GetGridcoinTeamIdAsync(string filePath)
        {
            var filename = Path.GetFileName(filePath);
            var readerSettings = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                Async = true
            };

            using (var inputFileStream = File.OpenRead(filePath))
            using (var gzipStream = new GZipStream(inputFileStream, CompressionMode.Decompress))
            using (var reader = XmlReader.Create(gzipStream, readerSettings))
            {
                _logger.ForContext(nameof(TeamXmlParser)).Information("Started parsing {0} for the Gridcoin  TeamID", filename);
                while(!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "team")
                    {
                        var xml = await reader.ReadInnerXmlAsync();
                        var teamName = XmlUtil.ExtractXml(xml, "name");
                        if (teamName.Equals("gridcoin", StringComparison.CurrentCultureIgnoreCase))
                        {
                            var teamId = Convert.ToInt32(XmlUtil.ExtractXml(xml, "id"));
                            _logger.ForContext(nameof(TeamXmlParser)).Information("Found TeamID: {0} with the name Gridcoin in the file {1}", teamId, filename);
                            return teamId;
                        }
                    }
                    else
                    {
                        await reader.ReadAsync();
                    }
                }
            }
            _logger.ForContext(nameof(TeamXmlParser)).Warning("Could not find a team with the name Gridcoin in the file {0}", filename);
            return 0;
        }
    }
}
