using System;
using System.Collections.Generic;
using System.Text;
using Bulk.Test.Model;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Bulk.Test
{
    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<BaseTphTable> BaseTphTable { get; set; }

        public DbSet<SimpleTableWithIdentity> SimpleTableWithIdentity { get; set; }

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BaseTphTable>()
                .HasDiscriminator<byte>("Type")
                    .HasValue<TpHChildTableOne>(1)
                    .HasValue<TpHChildTableTwo>(2);
        }
    }
}