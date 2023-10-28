﻿using Anevo.Models.Tables.Groups;
using Anevo.Models.Tables.User;
using Microsoft.EntityFrameworkCore;

namespace Anevo.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
        {
            Database.EnsureCreated();   // создаем базу данных при первом обращении
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SG_001>().HasKey(u => new { u.SG001_Id });
            modelBuilder.Entity<SG_010>().HasKey(u => new { u.SG010_Id });
        }

        public DbSet<SU_001> SU_001 { get; set; }
        public DbSet<SG_001> SG_001 { get; set; }

        public DbSet<SG_010> SG_010 { get; set; }

        ~ApplicationContext()
        {
        }
    }
}
