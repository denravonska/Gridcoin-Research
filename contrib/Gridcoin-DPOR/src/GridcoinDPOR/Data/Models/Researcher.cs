// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;

namespace GridcoinDPOR.Data.Models
{
    public class Researcher
    {
        public int Id { get; set; }
        public string CPID { get; set; }
        public double TotalCredit { get; set; }
        public double RAC { get; set; }
        public bool InTeam { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }
    }
}