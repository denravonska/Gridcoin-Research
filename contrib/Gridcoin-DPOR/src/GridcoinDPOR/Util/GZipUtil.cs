// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace GridcoinDPOR.Util
{
    public static class GZipUtil
    {
        public static async Task<bool> DecompressGZipFile(string inputFile, string outputFile)
        {
            try
            {
                using (FileStream inputFileStream = File.OpenRead(inputFile))
                using (FileStream outputFileStream = File.Create(outputFile))
                using (GZipStream gzipStream = new GZipStream(inputFileStream, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(outputFileStream);
                    return true;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}