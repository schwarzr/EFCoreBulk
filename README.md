# EntityFramework Core Bulk Extension for SQL Server

This package is currently under development and only prerelease.
Currently only Bulk Inserts are working and there is no documentation.

The extension currently supports two operation modes.

* replace the internal bulk processor
```csharp
var services = new ServiceCollection();
services
    .AddDbContext<TestContext>(p => p.UseSqlServer($"Data Source=.\\sqlexpress;Initial Catalog={_databaseName};Integrated Security=True;")
    .AddBulk()
);

// use the default SaveChanges operations
var item1 = new SimpleTableWithIdentity { Title = "Bla1" };
var item2 = new SimpleTableWithIdentity { Title = "Bla2" };
var item3 = new SimpleTableWithIdentity { Title = "Bla3" };
ctx.SimpleTableWithIdentity.Add(item1);
ctx.SimpleTableWithIdentity.Add(item2);
ctx.SimpleTableWithIdentity.Add(item3);

await ctx.SaveChangesAsync();
```
* use the BulkInsertAsync extension method for high performance bulk inserts

```csharp
var items = Enumerable.Range(0, 100000).Select(p => new SimpleTableWithIdentity { Title = $"Bla{p}" });
await ctx.BulkInsertAsync(items, false); // with or without value propagation
```
