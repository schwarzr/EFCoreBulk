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

        public DbSet<SimpleTableWithIdentity> SimpleTableWithIdentity { get; set; }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}