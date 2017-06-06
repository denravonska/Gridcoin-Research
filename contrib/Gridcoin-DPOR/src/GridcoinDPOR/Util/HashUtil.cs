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
        public static string GenerateMD5Hash(string data)
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