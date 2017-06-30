// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.Text;

namespace GridcoinDPOR.Data.Models
{
    public class ProjectResearcher
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }
        public int ResearcherId { get; set; }
        public Researcher Researcher { get; set; }
        public bool InTeam { get; set; }
        public double Credit { get; set; }
        public double RAC { get; set; }
        public double ProjectMag { get; set; }
        public double ProjectMagNTR { get; set; }
        public int WebUserId { get; set; }
    }
}
