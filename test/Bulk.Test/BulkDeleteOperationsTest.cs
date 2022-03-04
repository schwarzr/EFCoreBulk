using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bulk.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Bulk.Test
{
    public class BulkDeleteOperationsTest : DatabaseTest
    {
        [Test]
        public async Task BulkDeleteNormalInsertAndUpdateWithSaveChangesAsync()
        {
            var prov = GetServiceProvider(p => p.EnableBulkDelete().EnableBulkInsert(false));

            var ctx = prov.GetService<TestContext>();

            var items = Enumerable.Range(1, 11)
                            .Select(p => new SimpleTableWithShadowProperty
                            {
                                Title = $"Title {p}"
                            })
                            .ToList();
            var updateItem = items.Last();

            await ctx.BulkInsertAsync(items);

            await ctx.SimpleTableWithShadowProperty.AddRangeAsync(items.Take(10));
            Assert.AreEqual(10, ctx.ChangeTracker.Entries().Count());
            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Deleted);

            ctx.Entry(updateItem).State = EntityState.Unchanged;

            var newItem = new SimpleTableWithIdentity() { Title = "Normal Insert" };
            ctx.Add(newItem);
            updateItem.Title = "modified " + updateItem.Title;
            Assert.AreEqual(EntityState.Modified, ctx.Entry(updateItem).State);

            var result = await ctx.SaveChangesAsync();
            Assert.AreEqual(12, result);

            var dbItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();
            Assert.AreEqual(1, dbItems.Count);
        }

        [Test]
        public async Task BulkDeleteWithDbContextExtensionMethodAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            var items = Enumerable.Range(1, 100)
                            .Select(p => new SimpleTableWithShadowProperty
                            {
                                Title = $"Title {p}"
                            })
                            .ToList();

            await ctx.BulkInsertAsync(items);

            Assert.AreEqual(100, await ctx.SimpleTableWithShadowProperty.CountAsync());

            await ctx.BulkDeleteAsync(items.Take(50));

            var dbItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();

            Assert.AreEqual(50, dbItems.Count);

            var toCheck = items.Skip(50).ToList();
            var comparer = new SimpleTablePrimaryKeyComparer();

            for (int i = 0; i < dbItems.Count; i++)
            {
                Assert.True(comparer.Equals(toCheck[i], dbItems[i]));
            }
        }

        [Test]
        public async Task BulkDeleteWithErrorOnEmptyDeleteSaveChangesAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            var items = Enumerable.Range(1, 100)
                            .Select(p => new SimpleTableWithShadowProperty
                            {
                                Title = $"Title {p}"
                            })
                            .ToList();

            await ctx.BulkInsertAsync(items);

            items.Add(new SimpleTableWithShadowProperty { Id = 999, Title = "Title 999" });

            await ctx.SimpleTableWithShadowProperty.AddRangeAsync(items);
            Assert.AreEqual(101, ctx.ChangeTracker.Entries().Count());

            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Deleted);
            Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await ctx.SaveChangesAsync());
        }

        [Test]
        public async Task BulkDeleteWithSaveChangesAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            var items = Enumerable.Range(1, 100)
                            .Select(p => new SimpleTableWithShadowProperty
                            {
                                Title = $"Title {p}"
                            })
                            .ToList();

            await ctx.BulkInsertAsync(items);

            await ctx.SimpleTableWithShadowProperty.AddRangeAsync(items);
            Assert.AreEqual(100, ctx.ChangeTracker.Entries().Count());

            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Deleted);
            var result = await ctx.SaveChangesAsync();
            Assert.AreEqual(100, result);

            var dbItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();
            Assert.IsEmpty(dbItems);
        }

        private class SimpleTablePrimaryKeyComparer : IEqualityComparer<SimpleTableWithShadowProperty>
        {
            public bool Equals(SimpleTableWithShadowProperty x, SimpleTableWithShadowProperty y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(SimpleTableWithShadowProperty obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}