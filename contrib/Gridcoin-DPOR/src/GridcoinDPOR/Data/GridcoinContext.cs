// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using GridcoinDPOR.Data.Models;
using System.IO;

namespace GridcoinDPOR.Data
{
    public class GridcoinContext : DbContext
    {
        public DbSet<Researcher> Researchers { get; set; }
        public DbSet<Project> Projects { get; set; }

        public GridcoinContext(DbContextOptions options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Researcher>()
                        .HasIndex(x => x.CPID);
        }

        public static GridcoinContext Create(string dataDirectory)
        {
            //TODO: Handle testnet DIR
            var dporDir = Path.Combine(dataDirectory, "DPOR");
            var dbPath = Path.Combine(dporDir, "db.sqlite");
            var optionsBuilder = new DbContextOptionsBuilder<GridcoinContext>();
            optionsBuilder.UseSqlite(string.Format("Filename={0}", dbPath));
            var db = new GridcoinContext(optionsBuilder.Options);
            db.Database.EnsureCreated();
            return db;
        }
    }
}