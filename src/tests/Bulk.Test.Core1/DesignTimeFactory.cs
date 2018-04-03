using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bulk.Test
{
    public class DesignTimeFactory : IDbContextFactory<TestContext>
    {
        public TestContext Create(DbContextFactoryOptions options)
        {
            var builder = new DbContextOptionsBuilder<TestContext>();
            builder.UseSqlServer("Data Source=.;Initial Catalog=9F88862F-0D92-49C3-A9CF-5C5D9B568FCA;Integrated Security=True;");

            return new TestContext(builder.Options);
        }
    }
}