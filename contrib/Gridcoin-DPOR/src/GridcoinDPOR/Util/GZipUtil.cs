// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using GridcoinDPOR.Logging;
using Serilog;

namespace GridcoinDPOR.Util
{
    public static class GZipUtil
    {
        private static ILogger _logger = new NullLogger();
        public static ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value;}
        }
        
        public static async Task<bool> DecompressGZipFile(string inputFile, string outputFile)
        {
            try
            {
                using (FileStream inputFileStream = File.OpenRead(inputFile))
                using (FileStream outputFileStream = File.Create(outputFile))
                using (GZipStream gzipStream = new GZipStream(inputFileStream, CompressionMode.Decompress))
                {
                    _logger.ForContext(nameof(GZipUtil)).Information("Started decompressing file {0} to {1}", Path.GetFileName(inputFile), Path.GetFileName(outputFile));
                    await gzipStream.CopyToAsync(outputFileStream);
                    _logger.ForContext(nameof(GZipUtil)).Information("Finished decompressing file {0} to {1}", Path.GetFileName(inputFile), Path.GetFileName(outputFile));
                }

                if (File.Exists(outputFile))
                {
                    _logger.ForContext(nameof(GZipUtil)).Information("Successfully decompressing file {0} to {1}", Path.GetFileName(inputFile), Path.GetFileName(outputFile));
                    return true;
                }
                else
                {
                    _logger.ForContext(nameof(GZipUtil)).Error("Failed to decompressing file {0} to {1}", Path.GetFileName(inputFile), Path.GetFileName(outputFile));
                    return false;
                }
            }
            catch(Exception ex)
            {
                _logger.ForContext(nameof(GZipUtil)).Error("Failed to decompressing file. {0}", ex);
                return false;
            }
        }
    }
}