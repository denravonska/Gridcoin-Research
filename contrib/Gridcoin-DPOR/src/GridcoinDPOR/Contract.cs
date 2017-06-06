// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using GridcoinDPOR.Util;

namespace GridcoinDPOR
{
    public static class Contract
    {
        public static string GetNeuralHash(string contract)
        {
            var magBreaks = "0-25,25-500,500-1000,1000-10000,10000-50000,50000-100000,100000-999999,1000000-Inf".Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var ditherConstants = ".8,.2,.1,.025,.006,.003,.0015,.0007".Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            string magsXml = XmlUtil.ExtractXml(contract, "MAGNITUDES");
            var mags = magsXml.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string hashIn = "";

            for (int i = 0; i < mags.Length; i++)
            {
                if(mags[i].Length > 10)
                {
                    var row = mags[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (row.Length > 0)
                    {
                        if (row[0].Length > 5)
                        {
                            string cpid = row[0];
                            double mag = Math.Round(Convert.ToDouble(row[1]), 0);
                            hashIn += HashCPID(mag, cpid) + "<COL>";
                        }
                    }
                }
            }

            string hash = HashUtil.GenerateMD5Hash(hashIn);
            return hash;
        }

        private static string HashCPID(double magIn, string cpid)
        {
            string mag = (Math.Round(magIn, 0)).ToString().Trim();
            double magLength = mag.Length;
            double exponent = Math.Pow(magLength, 5);
            string magComponent = (Math.Round(magIn / (exponent + 0.01), 0)).ToString().Trim();
            string suffix = (Math.Round(magLength * exponent, 0)).ToString().Trim();
            string hash = cpid + magComponent + suffix;
            return hash;
        }
    }
}