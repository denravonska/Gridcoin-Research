// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using GridcoinDPOR.Util;

namespace GridcoinDPOR.Models
{   
    public class SyncData
    {
        public IEnumerable<Whitelist> Whitelist { get; set; }
        public IEnumerable<CpidData> CpidData { get; set; }
        public string Age { get; set; }
        public string QuorumHash { get; set; }
        public string TimeStamp { get; set; }
        public string BlockNumber { get; set; }
        public string PrimaryCPID { get; set; }

        public static SyncData Parse(string syndDataXml)
        {
            // GET WHITELIST DATA
            var whitelist = new List<Whitelist>();
            var whitelistXml = XmlUtil.ExtractXml(syndDataXml, "WHITELIST");
            var whitelistRows = whitelistXml.Split(new string[] {"<ROW>"}, StringSplitOptions.RemoveEmptyEntries);

            foreach(var row in whitelistRows)
            {
                var columns = row.Split(new string[] {"<COL>"}, StringSplitOptions.RemoveEmptyEntries);
                whitelist.Add(new Whitelist()
                {
                    Name = columns[0],
                    Url = columns[1]
                });
            }

            // GET CPID DATA
            var cpidData = new List<CpidData>();
            var cpidDataXml = XmlUtil.ExtractXml(syndDataXml, "CPIDDATA");
            var cpidDataRows = cpidDataXml.Split(new string[] {"<ROW>"}, StringSplitOptions.RemoveEmptyEntries);

            // TODO: Not sure what this data is?
            var testNet = cpidDataRows[0].Split(new string[] {"<COL>"}, StringSplitOptions.RemoveEmptyEntries);
            
            foreach(var row in cpidDataRows)
            {
                if (row.Length > 20)
                {
                    var columns = row.Split(new string[] {"<COL>"}, StringSplitOptions.RemoveEmptyEntries);
                    var cpidExtraData = Encoding.UTF8.GetString(Convert.FromBase64String(columns[1]));
                    var cpidExtraFields = cpidExtraData.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);

                    cpidData.Add(new CpidData()
                    {
                        CPID = columns[0],
                        CPIDv2 = cpidExtraFields[0],
                        BlockHash = cpidExtraFields[1],
                        Address = cpidExtraFields[2],
                    });
                }
            }

            var syncData = new SyncData()
            {
                Whitelist = whitelist,
                CpidData = cpidData,
                Age = XmlUtil.ExtractXml(syndDataXml, "AGE"),
                QuorumHash = XmlUtil.ExtractXml(syndDataXml, "HASH"),
                TimeStamp = XmlUtil.ExtractXml(syndDataXml, "TIMESTAMP"),
                BlockNumber = XmlUtil.ExtractXml(syndDataXml, "BLOCKNUMBER"),
                PrimaryCPID = XmlUtil.ExtractXml(syndDataXml, "PRIMARYCPID"),
            };

            return syncData;
        }
    }

    public class Whitelist
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public int TeamId { get; set; }

        public IEnumerable<string> GetUserUrls()
        {
            var urls = new List<string>();
            if (Url.EndsWith("stats/@"))
            {
                urls.Add(Url.Replace("@", "user.gz"));
                urls.Add(Url.Replace("@", "user.xml.gz"));
                urls.Add(Url.Replace("@", "user_id.gz"));
                return urls;
            }
            if (Url.EndsWith("@"))
            {
                urls.Add(Url.Replace("@", "stats/user.gz"));
                urls.Add(Url.Replace("@", "stats/user.xml.gz"));
                urls.Add(Url.Replace("@", "stats/user_id.gz"));
                return urls;
            }
            if (Url.EndsWith(".gz"))
            {
                urls.Add(Url);
                return urls;
            }
            return urls;
        }

        public IEnumerable<string> GetTeamUrls()
        {
            var urls = new List<string>();
            if (Url.EndsWith("stats/@"))
            {
                urls.Add(Url.Replace("@", "team.gz"));
                urls.Add(Url.Replace("@", "team.xml.gz"));
                urls.Add(Url.Replace("@", "team_id.gz"));
                return urls;
            }
            if (Url.EndsWith("@"))
            {
                urls.Add(Url.Replace("@", "stats/team.gz"));
                urls.Add(Url.Replace("@", "stats/team.xml.gz"));
                urls.Add(Url.Replace("@", "stats/team_id.gz"));
                return urls;
            }
            return urls;
        }
    }

    public class CpidData
    {
        public string CPID { get; set; }
        public string CPIDv2 { get; set; }
        public string BlockHash { get; set; }
        public string Address { get; set; }
        public bool IsValid { get {return CheckIsValid();} }

        private bool CheckIsValid()
        {
            //TODO: Port this
            // FUNCTIONS IN VB CODE ARE:
            // clsMD5.CompareCPID(sCPID, cpidv2, BlockHash)
            // UpdateMD5()
            // HashHex()
            return false;
        }
    }
}