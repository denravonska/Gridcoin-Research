// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Linq;
using GridcoinDPOR.Data;

namespace GridcoinDPOR
{
    public class MagnitudeCalculator
    {
        private readonly GridcoinContext _dbContext;
        public MagnitudeCalculator(GridcoinContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string GenerateContract()
        {
            // var test = _dbContext.Researchers.Where(x => x.RAC > 0)
            //                                  .GroupBy(g => )
            //                                  .Select(r => new 
            //                                  { 
            //                                      CPID = r,
            //                                      Mag = r.RAC / r.Select(pr => pr.Sum())
            //                                  })
            //                                  .ToList();

            return "";
        }
    }
}