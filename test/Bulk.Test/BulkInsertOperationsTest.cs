using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Bulk.Test.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Bulk.Test
{
    public class BulkInsertOperationsTest : DatabaseTest
    {
        [Test]
        public async Task BulkInsertCustomOptionsTest()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();

                var item1 = new SimpleTableWithIdentity { Id = 11, Title = "Bla1" };
                var item2 = new SimpleTableWithIdentity { Id = 12, Title = "Bla2" };
                var item3 = new SimpleTableWithIdentity { Id = 13, Title = "Bla3" };

                await ctx.BulkInsertAsync(new[] { item1, item2, item3 },
                    builder => builder.IdentityInsert().SqlBulkOptions(p => SqlBulkCopyOptions.TableLock));

                var items = await ctx.SimpleTableWithIdentity.ToListAsync();

                Assert.AreEqual(3, items.Count);
                Assert.False(items.Any(p => p.Id == 11));
                Assert.False(items.Any(p => p.Id == 12));
                Assert.False(items.Any(p => p.Id == 13));
            }
        }

        [Test]
        public async Task BulkInsertDefaultValuesForPropertiesAsync()
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
                    if (p % 2 == 0)
                    {
                        result.StoreValue("Description_de", $"Description Value {0}");
                        result.ModificationDate = DateTime.Now;
                    }
                    return result;
                })
                .ToList();

            await ctx.BulkInsertAsync(items, p => p.ShadowPropertyAccessor(ShadowPropertyAccessor.Current));

            var defaultItems = await ctx.SimpleTableWithShadowProperty.Where(p => EF.Property<string>(p, "Description_de") == "Default").ToListAsync();
            var otherItems = await ctx.SimpleTableWithShadowProperty.Where(p => EF.Property<string>(p, "Description_de") != "Default").ToListAsync();
            Assert.AreEqual(50, defaultItems.Count);
            Assert.AreEqual(50, otherItems.Count);

            otherItems.ForEach(p => ((string)ctx.Entry(p).Property("Description_de").CurrentValue).StartsWith("Description Value "));
            defaultItems.ForEach(p => Assert.AreEqual(p.ModificationDate, DateTime.MinValue));
            otherItems.ForEach(p => Assert.True(p.ModificationDate > DateTime.Now.AddHours(-1)));
        }

        [Test]
        public async Task SaveChanges_InsertWithDefaultValuesAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            for (int i = 1; i <= 100; i++)
            {
                var result = new SimpleTableWithShadowProperty
                {
                    Title = $"Title {i}"
                };

                await ctx.SimpleTableWithShadowProperty.AddAsync(result);
                var entry = ctx.Entry(result);

                if (i % 2 == 0)
                {
                    entry.Property("Description_de").CurrentValue = $"Description Value {i}";
                    result.ModificationDate = DateTime.Now;
                }
            }

            await ctx.SaveChangesAsync();

            var defaultItems = await ctx.SimpleTableWithShadowProperty.Where(p => EF.Property<string>(p, "Description_de") == "Default").ToListAsync();
            var otherItems = await ctx.SimpleTableWithShadowProperty.Where(p => EF.Property<string>(p, "Description_de") != "Default").ToListAsync();
            Assert.AreEqual(50, defaultItems.Count);
            Assert.AreEqual(50, otherItems.Count);

            otherItems.ForEach(p => Assert.True(((string)ctx.Entry(p).Property("Description_de").CurrentValue).StartsWith("Description Value ")));
            defaultItems.ForEach(p => Assert.AreEqual(p.ModificationDate, DateTime.MinValue));
            otherItems.ForEach(p => Assert.True(p.ModificationDate > DateTime.Now.AddHours(-1)));
        }

        [Test]
        public async Task BulkInsertIdentityInsertTest()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();

                var item1 = new SimpleTableWithIdentity { Id = 11, Title = "Bla1" };
                var item2 = new SimpleTableWithIdentity { Id = 12, Title = "Bla2" };
                var item3 = new SimpleTableWithIdentity { Id = 13, Title = "Bla3" };

                await ctx.BulkInsertAsync(new[] { item1, item2, item3 }, p => p.IdentityInsert(true).PropagateValues(true));

                var items = await ctx.SimpleTableWithIdentity.ToListAsync();

                Assert.AreEqual(3, items.Count);
                Assert.True(items.Any(p => p.Id == 11));
                Assert.True(items.Any(p => p.Id == 12));
                Assert.True(items.Any(p => p.Id == 13));
            }
        }

        [Test]
        public async Task BulkInsertNoDefaultValueHandlingAsync()
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
                    if (p % 2 == 0)
                    {
                        result.ModificationDate = DateTime.Now;
                    }
                    return result;
                })
                .ToList();

            await ctx.BulkInsertAsync(items, p => p.IgnoreDefaultValues());

            var defaultItems = await ctx.SimpleTableWithShadowProperty.Where(p => p.ModificationDate == null).ToListAsync();
            var otherItems = await ctx.SimpleTableWithShadowProperty.Where(p => p.ModificationDate != null).ToListAsync();
            Assert.AreEqual(50, defaultItems.Count);
            Assert.AreEqual(50, otherItems.Count);

            defaultItems.ForEach(p => Assert.False(p.ModificationDate.HasValue));
            otherItems.ForEach(p => Assert.True(p.ModificationDate > DateTime.Now.AddHours(-1)));
        }

        [Test]
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
            Assert.AreEqual(1, changes.Count);

            await ctx.SaveChangesAsync();

            ctx.ChangeTracker.Entries().ToList().ForEach(p => p.State = EntityState.Detached);

            var updated = await ctx.SimpleTableWithIdentity.FirstAsync(p => p.Id == oldItem.Id);

            Assert.AreEqual("newTitle", updated.Title);

            Assert.AreNotEqual(0, item1.Id);
            Assert.AreNotEqual(0, item2.Id);
            Assert.AreNotEqual(0, item3.Id);
            Assert.AreNotEqual(item1.Id, item2.Id);
            Assert.AreNotEqual(item2.Id, item3.Id);
            Assert.AreNotEqual(item1.Id, item3.Id);
        }

        [Test]
        public async Task BulkInsertSetupTest()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();

                var item1 = new SimpleTableWithIdentity { Id = 11, Title = "Bla1" };
                var item2 = new SimpleTableWithIdentity { Id = 12, Title = "Bla2" };
                var item3 = new SimpleTableWithIdentity { Id = 13, Title = "Bla3" };

                bool wasCalled = false;

                await ctx.BulkInsertAsync(new[] { item1, item2, item3 }, p => p.Setup(x => wasCalled = true));

                var items = await ctx.SimpleTableWithIdentity.ToListAsync();

                Assert.AreEqual(3, items.Count);
                Assert.True(wasCalled);
            }
        }

        [Test]
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
            allItems.ForEach(p => Assert.AreEqual("DEFAULT", ctx.Entry(p).Property("Description_de").CurrentValue));
        }

        [Test]
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

            await ctx.BulkInsertAsync(items, p => p.ShadowPropertyAccessor(ShadowPropertyAccessor.Current));

            var allItems = await ctx.SimpleTableWithShadowProperty.ToListAsync();
            allItems.ForEach(p => ((string)ctx.Entry(p).Property("Description_de").CurrentValue).StartsWith("Description Value "));
        }

        [Test]
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

            itemsOne.ForEach(p => Assert.True(p.Id > 0));
            itemsTwo.ForEach(p => Assert.True(p.Id > 0));

            var itemOne = context.BaseTphTable.First(p => p.Id == itemsOne.First().Id);
            var itemTwo = context.BaseTphTable.First(p => p.Id == itemsTwo.First().Id);

            Assert.IsInstanceOf<TpHChildTableOne>(itemOne);
            Assert.IsInstanceOf<TpHChildTableTwo>(itemTwo);

            Assert.AreEqual("TpHBulkInsertTestOne", itemOne.Name);
            Assert.AreEqual("TpHBulkInsertTestTwo", itemTwo.Name);

            Assert.AreEqual(1, ((TpHChildTableOne)itemOne).ChildOneProperty);
            Assert.AreEqual(2, ((TpHChildTableTwo)itemTwo).ChildTwoProperty);
        }

        [Test]
        public async Task BulkInsertWithDbContextExtensionMethodWithoutValuePropagationAsync()
        {
            var prov = GetServiceProvider();

            var ctx = prov.GetService<TestContext>();

            var items = new[] {
                    new SimpleTableWithIdentity { Title = "Extension1" },
                    new SimpleTableWithIdentity { Title = "Extension2" },
                    new SimpleTableWithIdentity { Title = "Extension3" }
                };

            await ctx.BulkInsertAsync(items, p => p.PropagateValues(false));

            Assert.AreEqual(0, items[0].Id);
            Assert.AreEqual(0, items[1].Id);
            Assert.AreEqual(0, items[2].Id);
        }

        [Test]
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

            Assert.AreNotEqual(0, items[0].Id);
            Assert.AreNotEqual(0, items[1].Id);
            Assert.AreNotEqual(0, items[2].Id);
        }

        [Test]
        public async Task BulkInsertWithSurroundingTransactionAsync()
        {
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();

            var items = new[] {
                new SimpleTableWithIdentity { Title = "Bla1" },
                new SimpleTableWithIdentity { Title = "Bla2" },
                new SimpleTableWithIdentity { Title = "Bla3" }
            };

            var items2 = new[] {
                new SimpleTableWithIdentity { Title = "Bla4" },
                new SimpleTableWithIdentity { Title = "Bla5" },
                new SimpleTableWithIdentity { Title = "Bla6" }
            };

            using (var transaction = await ctx.Database.BeginTransactionAsync())
            {
                await ctx.BulkInsertAsync(items, p => p.PropagateValues(false));
                await ctx.BulkInsertAsync(items2, p => p.PropagateValues(false));

                transaction.Commit();
            }

            var result = ctx.SimpleTableWithIdentity.ToList();

            Assert.AreEqual(6, result.Count);
            Assert.True(result.Any(p => p.Title.Equals("Bla1")));
            Assert.True(result.Any(p => p.Title.Equals("Bla2")));
            Assert.True(result.Any(p => p.Title.Equals("Bla3")));
            Assert.True(result.Any(p => p.Title.Equals("Bla4")));
            Assert.True(result.Any(p => p.Title.Equals("Bla5")));
            Assert.True(result.Any(p => p.Title.Equals("Bla6")));
        }

        [Test]
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

                Assert.AreNotEqual(0, item1.Id);
                Assert.AreNotEqual(0, item2.Id);
                Assert.AreNotEqual(0, item3.Id);
                Assert.AreNotEqual(item1.Id, item2.Id);
                Assert.AreNotEqual(item2.Id, item3.Id);
                Assert.AreNotEqual(item1.Id, item3.Id);
            }
        }

        [Test]
        public async Task SimpleInsertMassAsync()
        {
            //var prov = GetNonBulkServiceProvider();
            var prov = GetServiceProvider();
            var ctx = prov.GetService<TestContext>();
            await ctx.Database.MigrateAsync();

            var items = Enumerable.Range(0, 100000).Select(p => new SimpleTableWithIdentity { Title = $"Bla{p}" }).ToList();
            await ctx.SimpleTableWithIdentity.AddRangeAsync(items);

            await ctx.SaveChangesAsync();

            items.ForEach(p => Assert.True(p.Id > 0));
        }

        [Test]
        public async Task SimpleInsertMassWithDbContextExtensionAsync()
        {
            //var prov = GetNonBulkServiceProvider();
            var prov = GetServiceProvider();

            var ctx = prov.GetService<TestContext>();
            var items = Enumerable.Range(0, 100000).Select(p => new SimpleTableWithIdentity { Title = $"Bla{p}" });
            await ctx.BulkInsertAsync(items, p => p.PropagateValues(false));
        }

        [Test]
        public async Task BulkInsertUriWithStringConverter()
        {
            var provider = GetServiceProvider();
            var context = provider.GetService<TestContext>();

            Func<int, SimpleTableWithUri> mapStringToEntity =
                x => new SimpleTableWithUri
                {
                    Id = x,
                    Uri = new Uri($"https://loclahost:8080/test-uri{x}")
                };

            var entities = Enumerable.Range(0, 10)
                .Select(mapStringToEntity);

            await context.BulkInsertAsync(entities);

            var items = await context.SimpleTableWithUri
                .ToListAsync();

            Assert.AreEqual(10, items.Count);
        }


        [Test]
        public async Task BulkInsertUriWithStringConverter_SaveChanges()
        {
            var provider = GetServiceProvider();
            var context = provider.GetService<TestContext>();

            Func<int, SimpleTableWithUri> mapStringToEntity =
                x => new SimpleTableWithUri
                {
                    Uri = new Uri($"https://loclahost:8080/test-uri{x}")
                };

            var entities = Enumerable.Range(0, 10)
                .Select(mapStringToEntity);

            await context.AddRangeAsync(entities);

            await context.SaveChangesAsync();

            var items = await context.SimpleTableWithUri
                .ToListAsync();

            Assert.AreEqual(10, items.Count);
        }

    }
}