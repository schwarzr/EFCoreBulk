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

        public DbSet<SimpleTableWithShadowProperty> SimpleTableWithShadowProperty { get; set; }

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

            var entity = modelBuilder.Entity<SimpleTableWithShadowProperty>();
            var prop = entity.Property<string>("Description_de").HasMaxLength(200).HasDefaultValue("DEFAULT").IsRequired();
            //entity.Property(p => p.ModificationDate).HasDefaultValue(DateTime.MinValue);
            entity.Property(p => p.ModificationDate).HasDefaultValue(DateTime.MinValue);
            entity.Property(p => p.State).HasDefaultValue(State.Completed);
            entity.Property<string>("Description_en").HasMaxLength(200);

            entity.Property(p => p.BoolFlag).HasDefaultValue(false);
        }
    }
}