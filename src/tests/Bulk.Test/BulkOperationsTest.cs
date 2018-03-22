using System;
using System.Linq;
using System.Threading.Tasks;
using Bulk.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.Data.SqlClient;
using System.Collections.Concurrent;

namespace Bulk.Test
{
    public class BulkOperationsTest : IDisposable
    {
        private readonly string _databaseName;
        private readonly ServiceProvider _nonBulkServiceProvider;

        private ConcurrentBag<ServiceProvider> _bulkServiceProviders = new ConcurrentBag<ServiceProvider>();
        private ConcurrentBag<IServiceScope> _bulkServiceScopes = new ConcurrentBag<IServiceScope>();

        private bool disposedValue = false;

        public BulkOperationsTest()
        {
            _databaseName = Guid.NewGuid().ToString("N");
            using (var connection = new SqlConnection($"Data Source=.\\sqlexpress;Initial Catalog=master;Integrated Security=True;"))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = $"CREATE DATABASE [{_databaseName}]";
                command.ExecuteNonQuery();
            }

            var coll = new ServiceCollection();
            coll
                .AddDbContext<TestContext>(p => p.UseSqlServer($"Data Source=.\\sqlexpress;Initial Catalog={_databaseName};Integrated Security=True;"));

            _nonBulkServiceProvider = coll.BuildServiceProvider();
            using (var scope = _nonBulkServiceProvider.CreateScope())
            {
                scope.ServiceProvider.GetService<TestContext>().Database.Migrate();
            }
        }

        [Fact]
        public async Task BulkInsertNormalUpdateAsync()
        {
            var prov = GetServiceProvider();

            var ctx = prov.GetService<TestContext>();
            var oldItem = new SimpleTableWithIdentity { Title = "oldTitle" };

            ctx.SimpleTableWithIdentity.Add(oldItem);
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Detached);

            var itemToUpdate = ctx.SimpleTableWithIdentity.Find(oldItem.Id);
            itemToUpdate.Title = "newTitle";

            var item1 = new SimpleTableWithIdentity { Title = "Bla1" };
            var item2 = new SimpleTableWithIdentity { Title = "Bla2" };
            var item3 = new SimpleTableWithIdentity { Title = "Bla3" };
            ctx.SimpleTableWithIdentity.Add(item1);
            ctx.SimpleTableWithIdentity.Add(item2);
            ctx.SimpleTableWithIdentity.Add(item3);

            var changes = ctx.ChangeTracker.Entries().Where(p => p.State == EntityState.Modified).ToList();
            Assert.Equal(1, changes.Count);

            await ctx.SaveChangesAsync();

            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Detached);

            var updated = await ctx.SimpleTableWithIdentity.FirstAsync(p => p.Id == oldItem.Id);

            Assert.Equal("newTitle", updated.Title);

            Assert.NotEqual(item1.Id, 0);
            Assert.NotEqual(item2.Id, 0);
            Assert.NotEqual(item3.Id, 0);
            Assert.NotEqual(item1.Id, item2.Id);
            Assert.NotEqual(item2.Id, item3.Id);
            Assert.NotEqual(item1.Id, item3.Id);
        }

        [Fact]
        public async Task BulkInsertWithDbContextExtensionMethodWithoutValuePropagationAsync()
        {
            var prov = GetServiceProvider();

            var ctx = prov.GetService<TestContext>();

            var items = new[] {
                    new SimpleTableWithIdentity { Title = "Extension1" },
                    new SimpleTableWithIdentity { Title = "Extension2" },
                    new SimpleTableWithIdentity { Title = "Extension3" }
                };

            await ctx.BulkInsertAsync(items, false);

            Assert.Equal(0, items[0].Id);
            Assert.Equal(0, items[1].Id);
            Assert.Equal(0, items[2].Id);
        }

        [Fact]
        public async Task BulkInsertWithDbContextExtensionMethodWithValuePropagationAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            var items = new[] {
                    new SimpleTableWithIdentity { Title = "Extension1" },
                    new SimpleTableWithIdentity { Title = "Extension2" },
                    new SimpleTableWithIdentity { Title = "Extension3" }
                };

            await ctx.BulkInsertAsync(items);

            Assert.NotEqual(0, items[0].Id);
            Assert.NotEqual(0, items[1].Id);
            Assert.NotEqual(0, items[2].Id);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [Fact]
        public async Task SimpleInsertAsync()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();

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
            //var prov = GetNonBulkServiceProvider();
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();
            await ctx.Database.MigrateAsync();

            var items = Enumerable.Range(0, 100000).Select(p => new SimpleTableWithIdentity { Title = $"Bla{p}" }).ToList();
            await ctx.SimpleTableWithIdentity.AddRangeAsync(items);

            await ctx.SaveChangesAsync();

            Assert.All(items, p => Assert.True(p.Id > 0));
        }

        [Fact]
        public async Task SimpleInsertMassWithDbContextExtensionAsync()
        {
            //var prov = GetNonBulkServiceProvider();
            var prov = GetServiceProvider();

            var ctx = prov.GetService<TestContext>();
            var items = Enumerable.Range(0, 100000).Select(p => new SimpleTableWithIdentity { Title = $"Bla{p}" });
            await ctx.BulkInsertAsync(items, false);
        }

        // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var item in _bulkServiceScopes)
                    {
                        item.Dispose();
                    }

                    foreach (var item in _bulkServiceProviders)
                    {
                        item.Dispose();
                    }

                    using (var scope = _nonBulkServiceProvider.CreateScope())
                    {
                        var ctx = scope.ServiceProvider.GetService<TestContext>();
                        ctx.Database.EnsureDeleted();
                    }

                    _nonBulkServiceProvider.Dispose();
                }

                disposedValue = true;
            }
        }

        private IServiceProvider GetNonBulkServiceProvider()
        {
            return _nonBulkServiceProvider;
        }

        private IServiceProvider GetServiceProvider(Action<SqlServerBulkOptions> config = null)
        {
            var coll = new ServiceCollection();
            coll
                .AddDbContext<TestContext>(p => p.UseSqlServer($"Data Source=.\\sqlexpress;Initial Catalog={_databaseName};Integrated Security=True;")
                .AddBulk(config)
            );

            var result = coll.BuildServiceProvider();
            _bulkServiceProviders.Add(result);
            var scope = result.CreateScope();
            _bulkServiceScopes.Add(scope);
            return scope.ServiceProvider;
        }
    }
}