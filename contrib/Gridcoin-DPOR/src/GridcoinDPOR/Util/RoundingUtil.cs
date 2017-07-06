// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.Text;

namespace GridcoinDPOR.Util
{
    public static class RoundingUtil
    {
        private static string[] _magBreaks = "0-25,25-500,500-1000,1000-10000,10000-50000,50000-100000,100000-999999,1000000-Inf".Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        private static string[] _ditherConstants = ".8,.2,.1,.025,.006,.003,.0015,.0007".Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);


        public static double RoundSnap(double number)
        {
            double dither = Dither(number);
            double rounded = Math.Round(Math.Round(number * dither, 0) / dither, 2);
            return rounded;
        }

        public static double Dither(double magnitude)
        {
            double dither = 0.1;
            for (int i = 0; i < _magBreaks.Length; i++)
            {
                string[] breaks = _magBreaks[i].Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                double lowBreak = Convert.ToDouble(breaks[0]);
                double highBreak = 0;
                if (breaks[1] == "Inf") 
                {
                    highBreak = lowBreak * 10;
                }
                else
                {
                    highBreak = Convert.ToDouble(breaks[1]);
                }
                if (magnitude >= lowBreak && magnitude <= highBreak)
                {
                    dither = Convert.ToDouble(_ditherConstants[i]);
                }
            }
            return dither;
        }
    }
}
