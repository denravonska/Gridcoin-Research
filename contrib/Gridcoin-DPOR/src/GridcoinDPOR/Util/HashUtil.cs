// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Security.Cryptography;
using System.Text;

namespace GridcoinDPOR.Util
{
    public static class HashUtil
    { 
        public static string NeuralHash(string contract)
        {
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
                            hashIn += HashUtil.HashCPID(mag, cpid) + "<COL>";
                        }
                    }
                }
            }
           
            string hash = HashUtil.HashMD5(hashIn);
            return hash;
        }

        public static string HashCPID(double magIn, string cpid)
        {
            string mag = (Math.Round(magIn, 0)).ToString().Trim();
            double magLength = mag.Length;
            double exponent = Math.Pow(magLength, 5);
            string magComponent = (Math.Round(magIn / (exponent + 0.01), 0)).ToString().Trim();
            string suffix = (Math.Round(magLength * exponent, 0)).ToString().Trim();
            string hash = cpid + magComponent + suffix;
            return hash;
        }

        public static string HashMD5(string data)
        {
            try
            {
                using(var md5 = MD5.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(data);
                    var hashBytes = md5.ComputeHash(inputBytes);
                    var hash = ByteArrayToString(hashBytes);
                    return hash;
                }
            }
            catch
            {
                // TODO: Log?
                return "MD5Error";
            }
        }

        public static string ByteArrayToString(byte[] data)   
        {   
            var hex = new StringBuilder(data.Length * 2);   

            for(int i=0; i < data.Length; i++) 
            {   
                hex.Append(data[i].ToString("X2"));   
            }

            return hex.ToString().ToLower();   
        }
    }
}