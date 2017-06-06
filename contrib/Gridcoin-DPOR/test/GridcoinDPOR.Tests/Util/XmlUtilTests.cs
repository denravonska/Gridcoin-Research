// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using GridcoinDPOR.Util;
using Xunit;

namespace GridcoinDPOR.Tests.Util
{
    public class XmlUtilTests
    {
        [Fact]
        public void ExtractXML_SyncDataCPID_Success()
        {
            // ARRANGE
            var xml = "<PRIMARYCPID>96c18bb4a02d15c90224a7138a540cf7</PRIMARYCPID>";

            // ACT
            var cpid = XmlUtil.ExtractXml(xml, "PRIMARYCPID");

            // ASSERT
            Assert.Equal(
                expected: "96c18bb4a02d15c90224a7138a540cf7",
                actual: cpid
            );
        }
    }
}

