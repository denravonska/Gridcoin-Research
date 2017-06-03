// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

namespace GridcoinDPOR.Models
{
    public class User
    {
        public string CPID { get; set; }
        public string Name { get; set; }
        public string Project { get; set; }
        public double TotalCredit {get; set; }
        public double RAC { get; set; }
    }
}