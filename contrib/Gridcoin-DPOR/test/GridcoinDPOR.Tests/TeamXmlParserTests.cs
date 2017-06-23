// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GridcoinDPOR.Models;
using Xunit;

namespace GridcoinDPOR.Tests
{
    public class TeamXmlParserTests
    {
        [Fact]
        public async void GetUsersInTeamWithBeaconAsync_Success()
        {
            // ARRANGE
            var location = typeof(TeamXmlParserTests).GetTypeInfo().Assembly.Location;
            var dirPath = Path.GetDirectoryName(location);
            var path = Path.Combine(dirPath, "../../../TestData/tn-grid_team.gz");

            // ACT
            var teamId = await TeamXmlParser.GetGridcoinTeamIdAsync(path);

            // ASSERT
            Assert.Equal(expected: 61, actual: teamId);
        }
    }
}
