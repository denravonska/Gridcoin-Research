// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System.Text.RegularExpressions;

namespace GridcoinDPOR.Util
{
    public static class XmlUtil
    {
        public static string ExtractXml(string xml, string tag)
        {
            var startTag = string.Concat("<", tag, ">");
            var endTag = string.Concat("</", tag, ">");
            var regex = new Regex(string.Format("{0}(.*?){1}", startTag, endTag));
            var match = regex.Match(xml);

            if (match.Groups.Count > 1)
            {
                return match.Groups[1].ToString();
            }

            return "";
        }
    }
}