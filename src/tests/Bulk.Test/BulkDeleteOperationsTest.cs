using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class BulkDeleteOperationsTest : DatabaseTest
    {
        [Fact]
        public async Task BulkDeleteNormalInsertAndUpdateWithSaveChangesAsync()
        {
            var prov = GetServiceProvider(p =>
            {
                p.DeleteEnabled = true;
                p.InsertEnabled = false;
                p.UpdateEnabled = false;
            });

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
            Assert.Equal(10, ctx.ChangeTracker.Entries().Count());
            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Deleted);

            ctx.Entry(updateItem).State = EntityState.Unchanged;

            var newItem = new SimpleTableWithIdentity() { Title = "Normal Insert" };
            ctx.Add(newItem);
            updateItem.Title = "modified " + updateItem.Title;
            Assert.Equal(EntityState.Modified, ctx.Entry(updateItem).State);

            var result = await ctx.SaveChangesAsync();
            Assert.Equal(12, result);

            var dbItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();
            Assert.Single(dbItems);
        }

        [Fact]
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

            Assert.Equal(100, await ctx.SimpleTableWithShadowProperty.CountAsync());

            await ctx.BulkDeleteAsync(items.Take(50));

            var dbItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();

            Assert.Equal(50, dbItems.Count);

            Assert.Equal(items.Skip(50), dbItems, new SimpleTablePrimaryKeyComparer());
        }

        [Fact]
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
            Assert.Equal(101, ctx.ChangeTracker.Entries().Count());

            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Deleted);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await ctx.SaveChangesAsync());
        }

        [Fact]
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
            Assert.Equal(100, ctx.ChangeTracker.Entries().Count());

            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Deleted);
            var result = await ctx.SaveChangesAsync();
            Assert.Equal(100, result);

            var dbItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();
            Assert.Empty(dbItems);
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