// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using GridcoinDPOR.Data;
using GridcoinDPOR.Logging;
using GridcoinDPOR.Util;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GridcoinDPOR
{
    public class Contract
    {
        private ILogger _logger = new NullLogger();
        public ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value;}
        }

        private readonly Paths _paths;
        private readonly QuorumHashingAlgorithm _quorumHashingAlg;

        public Contract(
            ILogger logger,
            Paths paths)
        {
            _logger = logger;
            _paths = paths;
            _quorumHashingAlg = new QuorumHashingAlgorithm();
        }

        public async Task<string> GetContract(bool noTeam)
        {
            string contractFilePath = Path.Combine(_paths.RootFolder, "contract.dat");
            string contractNoTeamFilePath = Path.Combine(_paths.RootFolder, "contract-noteam.dat");

            if (noTeam)
            {
                if (File.Exists(contractNoTeamFilePath))
                {
                    string contract = await FileUtil.ReadAllTextAsync(contractNoTeamFilePath);
                    return contract;
                }
            }
            else
            {
                if (File.Exists(contractFilePath))
                {
                    string contract = await FileUtil.ReadAllTextAsync(contractFilePath);
                    return contract;
                }
            }

            return "";
        }

        public async Task<string> GetNeuralHash(bool noTeam)
        {
            var contract = await GetContract(noTeam);
            if (contract == "")
            {
                return "d41d8cd98f00b204e9800998ecf8427e";
            }

            var hash = _quorumHashingAlg.GetNeuralHash(contract);
            return hash;
        }
    }
}
