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
        public DateTime LastSyncUtc { get; set; }
        public ICollection<Researcher> Researchers { get; set; }

        public Project()
        {
            Researchers = new List<Researcher>();
        }
    }
}