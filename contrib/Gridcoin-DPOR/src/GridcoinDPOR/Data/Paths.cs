// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GridcoinDPOR.Data
{
    public class Paths
    {
        public string RootFolder { get; private set; }
        public string DownloadsFolder { get; private set; }
        public string LogFilePath { get; private set; }
        public string DatabasePath { get; private set; }

        public Paths()
        {
        }

        public void SetDataPaths(string gridcoinDataDir, bool isTestnet)
        {
            if (isTestnet)
            {
                RootFolder = Path.Combine(gridcoinDataDir, "testnet", "DPOR");
                DownloadsFolder = Path.Combine(RootFolder, "stats");
                LogFilePath = Path.Combine(RootFolder, "logs", "debug-{Date}.log");
                DatabasePath = Path.Combine(RootFolder, "db.sqlite");
            }
            else
            {
                RootFolder = Path.Combine(gridcoinDataDir, "DPOR");
                DownloadsFolder = Path.Combine(RootFolder, "stats");
                LogFilePath = Path.Combine(RootFolder, "logs", "debug-{Date}.log");
                DatabasePath = Path.Combine(RootFolder, "db.sqlite");
            }

            if (!Directory.Exists(RootFolder))
            {
                Directory.CreateDirectory(RootFolder);
            }

            if (!Directory.Exists(DownloadsFolder))
            {
                Directory.CreateDirectory(DownloadsFolder);
            }
        }
    }
}
