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
using Microsoft.EntityFrameworkCore.Metadata;

namespace GridcoinDPOR.Data
{
    public class GridcoinContext : DbContext
    {
        public DbSet<Researcher> Researchers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectResearcher> ProjectResearcher { get; set; }

        public GridcoinContext(DbContextOptions options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
             modelBuilder.Entity<Project>()
                        .HasIndex(x => x.Name)
                        .IsUnique();

            modelBuilder.Entity<Researcher>()
                        .HasIndex(x => x.CPID)
                        .IsUnique();

            modelBuilder.Entity<ProjectResearcher>()
                        .HasOne(x => x.Project)
                        .WithMany(x => x.ProjectResearchers)
                        .HasForeignKey(x => x.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectResearcher>()
                        .HasOne(x => x.Researcher)
                        .WithMany(x => x.ProjectResearchers)
                        .HasForeignKey(x => x.ResearcherId)
                        .OnDelete(DeleteBehavior.Cascade);
        }

        public static GridcoinContext Create(Paths paths)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GridcoinContext>();
            optionsBuilder.UseSqlite(string.Format("Filename={0}", paths.DatabasePath));
            var db = new GridcoinContext(optionsBuilder.Options);
            db.Database.EnsureCreated();
            return db;
        }
    }
}