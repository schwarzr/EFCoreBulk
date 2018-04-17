using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Bulk.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bulk.Test
{
    public class BulkInsertOperationsTest : DatabaseTest
    {
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
            Assert.Single(changes);

            await ctx.SaveChangesAsync();

            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Detached);

            var updated = await ctx.SimpleTableWithIdentity.FirstAsync(p => p.Id == oldItem.Id);

            Assert.Equal("newTitle", updated.Title);

            Assert.NotEqual(0, item1.Id);
            Assert.NotEqual(0, item2.Id);
            Assert.NotEqual(0, item3.Id);
            Assert.NotEqual(item1.Id, item2.Id);
            Assert.NotEqual(item2.Id, item3.Id);
            Assert.NotEqual(item1.Id, item3.Id);
        }

        [Fact]
        public async Task BulkInsertShadowPropertyEntityIgnoreShadowPropertyAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            var items = Enumerable.Range(1, 100)
                .Select(p =>
                {
                    var result = new SimpleTableWithShadowProperty
                    {
                        Title = $"Title {p}"
                    };
                    result.StoreValue("Description_de", $"Description Value {0}");
                    return result;
                })
                .ToList();

            await ctx.BulkInsertAsync(items);

            var allItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();
            Assert.All(allItems, p => Assert.Equal("DEFAULT", ctx.Entry(p).Property("Description_de").CurrentValue));
        }

        [Fact]
        public async Task BulkInsertShadowPropertyEntityIncludeShadowPropertyAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            var items = Enumerable.Range(1, 100)
                .Select(p =>
                {
                    var result = new SimpleTableWithShadowProperty
                    {
                        Title = $"Title {p}"
                    };
                    result.StoreValue("Description_de", $"Description Value {0}");
                    return result;
                })
                .ToList();

            await ctx.BulkInsertAsync(items, shadowPropertyAccessor: ShadowPropertyAccessor.Current);

            var allItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();
            Assert.All(allItems, p => Assert.StartsWith("Description Value ", (string)ctx.Entry(p).Property("Description_de").CurrentValue));
        }

        [Fact]
        public async Task BulkInsertTpHEntitiesAsync()
        {
            var itemsOne = Enumerable.Range(0, 200).Select(p => new TpHChildTableOne
            {
                Name = "TpHBulkInsertTestOne",
                ChildOneProperty = p + 1
            }).ToList();

            var itemsTwo = Enumerable.Range(0, 200).Select(p => new TpHChildTableTwo
            {
                Name = "TpHBulkInsertTestTwo",
                ChildTwoProperty = p + 2
            }).ToList();

            var prov = GetServiceProvider();

            var context = prov.GetService<TestContext>();

            await context.BulkInsertAsync(itemsOne);
            await context.BulkInsertAsync(itemsTwo);

            Assert.All(itemsOne, p => Assert.True(p.Id > 0));
            Assert.All(itemsTwo, p => Assert.True(p.Id > 0));

            var itemOne = context.BaseTphTable.First(p => p.Id == itemsOne.First().Id);
            var itemTwo = context.BaseTphTable.First(p => p.Id == itemsTwo.First().Id);

            Assert.IsType<TpHChildTableOne>(itemOne);
            Assert.IsType<TpHChildTableTwo>(itemTwo);

            Assert.Equal("TpHBulkInsertTestOne", itemOne.Name);
            Assert.Equal("TpHBulkInsertTestTwo", itemTwo.Name);

            Assert.Equal(1, ((TpHChildTableOne)itemOne).ChildOneProperty);
            Assert.Equal(2, ((TpHChildTableTwo)itemTwo).ChildTwoProperty);
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

                Assert.NotEqual(0, item1.Id);
                Assert.NotEqual(0, item2.Id);
                Assert.NotEqual(0, item3.Id);
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
    }
}