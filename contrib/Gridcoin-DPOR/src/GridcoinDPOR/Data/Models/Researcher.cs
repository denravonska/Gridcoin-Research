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
        public string CPIDv2 { get; set; }
        public string BlockHash { get; set; }
        public string Address { get; set; }
        public bool IsValid { get; set; }
        public double TotalMag { get; set; }
        public double TotalMagNTR { get; set; }
        public ICollection<ProjectResearcher> ProjectResearchers { get; set; }

        //TODO: Port this
        // FUNCTIONS IN VB CODE ARE:
        // clsMD5.CompareCPID(sCPID, cpidv2, BlockHash)
        // UpdateMD5()
        // HashHex()
        
    }
}