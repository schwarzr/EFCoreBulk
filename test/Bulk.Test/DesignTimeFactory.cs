using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bulk.Test
{
    public class DesignTimeFactory : IDesignTimeDbContextFactory<TestContext>
    {
        public TestContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TestContext>();
            builder.UseSqlServer("Data Source=.;Initial Catalog=5B61D5D3-EF17-4F03-BA0C-7F4B4B45A889;Integrated Security=True;", p => p.UseNetTopologySuite());

            return new TestContext(builder.Options);
        }
    }
}