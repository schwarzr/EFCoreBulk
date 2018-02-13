using System;
using System.Linq;
using System.Threading.Tasks;
using Bulk.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bulk.Test
{
    public class BulkOperationsTest
    {
        [Fact]
        public async Task SimpleInsertAsync()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();
                await ctx.Database.MigrateAsync();

                var item1 = new SimpleTableWithIdentity { Title = "Bla1" };
                var item2 = new SimpleTableWithIdentity { Title = "Bla2" };
                var item3 = new SimpleTableWithIdentity { Title = "Bla3" };
                ctx.SimpleTableWithIdentity.Add(item1);
                ctx.SimpleTableWithIdentity.Add(item2);
                ctx.SimpleTableWithIdentity.Add(item3);

                await ctx.SaveChangesAsync();

                Assert.NotEqual(item1.Id, 0);
                Assert.NotEqual(item2.Id, 0);
                Assert.NotEqual(item3.Id, 0);
                Assert.NotEqual(item1.Id, item2.Id);
                Assert.NotEqual(item2.Id, item3.Id);
                Assert.NotEqual(item1.Id, item3.Id);
            }
        }

        [Fact]
        public async Task SimpleInsertMassAsync()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();
                await ctx.Database.MigrateAsync();

                var items = Enumerable.Range(0, 100000).Select(p => new SimpleTableWithIdentity { Title = $"Bla{p}" }).ToList();
                await ctx.SimpleTableWithIdentity.AddRangeAsync(items);

                await ctx.SaveChangesAsync();

                Assert.All(items, p => Assert.True(p.Id > 0));
            }
        }

        private static IServiceProvider GetNonBulkServiceProvider()
        {
            var coll = new ServiceCollection();
            coll
                .AddDbContext<TestContext>(p => p.UseSqlServer("Data Source=.;Initial Catalog=BulkTestContext;Integrated Security=True;")
            );

            return coll.BuildServiceProvider();
        }

        private static IServiceProvider GetServiceProvider()
        {
            var coll = new ServiceCollection();
            coll
                .AddDbContext<TestContext>(p => p.UseSqlServer("Data Source=.;Initial Catalog=BulkTestContext;Integrated Security=True;")
                .AddBulk()
            );

            return coll.BuildServiceProvider();
        }
    }
}