using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Model;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<SimpleTableWithIdentity> SimpleTableWithIdentity { get; set; }
    }
}