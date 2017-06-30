// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;

namespace GridcoinDPOR.Data.Models
{
   public class Project 
   {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTime LastSyncUtc { get; set; }
        public int TeamId { get; set; }
        public ICollection<ProjectResearcher> ProjectResearchers { get; set; }
        public double TotalRAC { get; set; }
        public double TeamRAC { get; set; }

        public Project()
        {
        //    Researchers = new List<Researcher>();
        }

        public string GetTeamGzipFilename()
        {
            string fileName = Name.ToLower().Replace(" ", "_") + "_team.gz";
            return fileName;
        }

        public string GetUserGzipFilename()
        {
            string fileName = Name.ToLower().Replace(" ", "_") + "_user.gz";
            return fileName;
        }

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
}