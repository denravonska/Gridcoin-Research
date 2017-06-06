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
    public class UserXmlParserTests
    {
        [Fact]
        public async void GetUsersInTeamWithBeaconAsync_Success()
        {
            // ARRANGE
            var location = typeof(UserXmlParserTests).GetTypeInfo().Assembly.Location;
            var dirPath = Path.GetDirectoryName(location);
            var path = Path.Combine(dirPath, "../../../TestData/tn-grid_user.xml");
            var teamId = 61;

            var cpids = new List<CpidData>();
            cpids.Add(new CpidData() { CPID = "7d0d73fe026d66fd4ab8d5d8da32a611" });
            cpids.Add(new CpidData() { CPID = "e7f90818e3e87c0bbefe83ad3cfe27e1" });
            cpids.Add(new CpidData() { CPID = "96c18bb4a02d15c90224a7138a540cf7" });

            // ACT
            var users = await UserXmlParser.GetUsersInTeamWithBeaconAsync(path, teamId, cpids);

            // ASSERT
            Assert.NotEmpty(users);
            Assert.Collection(users,
                item => Assert.Equal("e7f90818e3e87c0bbefe83ad3cfe27e1", item.CPID),
                item => Assert.Equal("7d0d73fe026d66fd4ab8d5d8da32a611", item.CPID),
                item => Assert.Equal("96c18bb4a02d15c90224a7138a540cf7", item.CPID)
            );
            Assert.Equal(12181278.132404d, users.Where(w => w.CPID == "e7f90818e3e87c0bbefe83ad3cfe27e1").First().TotalCredit);
            Assert.Equal(105342.196023d, users.Where(w => w.CPID == "e7f90818e3e87c0bbefe83ad3cfe27e1").First().RAC);
        }

        [Fact]
        public async void GetUsersInTeamWithBeaconAsync_WrongTeamId_ReturnEmpty()
        {
            // ARRANGE
            var location = typeof(UserXmlParserTests).GetTypeInfo().Assembly.Location;
            var dirPath = Path.GetDirectoryName(location);
            var path = Path.Combine(dirPath, "../../../TestData/tn-grid_user.xml");
            var teamId = 0;

            var cpids = new List<CpidData>();
            cpids.Add(new CpidData() { CPID = "7d0d73fe026d66fd4ab8d5d8da32a611" });
            cpids.Add(new CpidData() { CPID = "e7f90818e3e87c0bbefe83ad3cfe27e1" });
            cpids.Add(new CpidData() { CPID = "96c18bb4a02d15c90224a7138a540cf7" });

            // ACT
            var users = await UserXmlParser.GetUsersInTeamWithBeaconAsync(path, teamId, cpids);

            // ASSERT
            Assert.Empty(users);
        }
    }
}
