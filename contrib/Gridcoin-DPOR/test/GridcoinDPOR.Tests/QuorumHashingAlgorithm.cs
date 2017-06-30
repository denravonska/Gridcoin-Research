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

namespace GridcoinDPOR.Tests
{
    public class QuorumHashingAlgorithmTests
    {
        [Fact]
        public void GetNeuralHash_Success()
        {
            // ARRANGE
            var location = typeof(QuorumHashingAlgorithmTests).GetTypeInfo().Assembly.Location;
            var dirPath = Path.GetDirectoryName(location);
            var path = Path.Combine(dirPath, "../../../TestData/contract.txt");
            var contactData = File.ReadAllText(path);
            var quorumHashingAlgo = new QuorumHashingAlgorithm();

            // ACT
            var hash = quorumHashingAlgo.GetNeuralHash(contactData);

            // ASSERT
            Assert.Equal(expected: "81211a4d534a68d715fcafe9e628c2d3", actual: hash);
        }
    }
}
