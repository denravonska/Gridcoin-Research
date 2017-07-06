// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GridcoinDPOR;
using Xunit;
using GridcoinDPOR.Util;

namespace GridcoinDPOR.Tests.Util
{
    public class HashUtilTests
    {
        [Fact]
        public void GetNeuralHash_Success()
        {
            // ARRANGE
            var location = typeof(HashUtilTests).GetTypeInfo().Assembly.Location;
            var dirPath = Path.GetDirectoryName(location);
            var path = Path.Combine(dirPath, "../../../TestData/contract.txt");
            var contactData = File.ReadAllText(path);

            // ACT
            var hash = HashUtil.NeuralHash(contactData);

            // ASSERT
            Assert.Equal(expected: "0f099cab261bb562ff553f3b9c7bf942", actual: hash);
        }

        [Fact]
        public void HashCPID_WholeNumber70_Success()
        {
            // ARRANGE
            double magIn = 70;
            string cpid = "1963a6f109ea770c195a0e1afacd2eba";
            

            // ACT
            var hash = HashUtil.HashCPID(magIn, cpid);

            // ASSERT
            Assert.Equal(expected: "1963a6f109ea770c195a0e1afacd2eba264", actual: hash);
        }

        [Fact]
        public void HashCPID_WholeNumber820_Success()
        {
            // ARRANGE
            double magIn = 820;
            string cpid = "285ff8d5014ef73cc83580338a9c0345";
            

            // ACT
            var hash = HashUtil.HashCPID(magIn, cpid);

            // ASSERT
            Assert.Equal(expected: "285ff8d5014ef73cc83580338a9c03453729", actual: hash);
        }

        [Fact]
        public void HashCPID_WholeNumber0_Success()
        {
            // ARRANGE
            double magIn = 0;
            string cpid = "9b67756a05f76842de1e88226b79deb9";
            

            // ACT
            var hash = HashUtil.HashCPID(magIn, cpid);

            // ASSERT
            Assert.Equal(expected: "9b67756a05f76842de1e88226b79deb901", actual: hash);
        }
    }
}
