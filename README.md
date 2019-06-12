# EntityFramework Core Bulk Extension for SQL Server

This is an extension for the EntityFramework Core SqlServer provider, that extends the DbContext with the use of the ado.net SqlBulkCopy class.

The extension supports two operation modes.

* replace the internal batch command processor
```csharp
var services = new ServiceCollection();
services
    .AddDbContext<TestContext>(p => 
        p.UseSqlServer(
            $"Data Source=.\\sqlexpress;Initial Catalog={_databaseName};Integrated Security=True;", 
            options => options.AddBulk()
        )
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
In this mode the default IModificationCommandBatchFactory is overwritten with a customized version that redirects all insert or delete commands (based on the configuration of .AddBulk) to a SqlBulkCopy operation.

Bulk Insert and Delete are enabled by default. This can be changed by using the configuration parameter for the AddBulk method.

```csharp
services.AddDbContext<TestContext>(
    p => p.UseSqlServer(
            "connectionstring",
            options => options.AddBulk(x => x.EnableBulkInsert().EnableBulkDelete(false))
    )
);
```


* use the BulkInsertAsync and BulkDeleteAsync extension methods.
This mode can be used for high performance bulk inserts and deletes, bypassing the context ChangeTracker and therefore streaming directly to the DB.
```csharp
var items = Enumerable.Range(0, 100000).Select(p => new SimpleTableWithIdentity { Title = $"Bla{p}" });

// with value propagation --> item.ids are updated after the insert ist completed
await ctx.BulkInsertAsync(items, p => p.PropagateValues(true)); 

// without value propagation --> ids are not updated with the identity values from the db.
// --> faster and less memory usage
await ctx.BulkInsertAsync(items, p => p.PropagateValues(false)); 

// enable identity insert 
var items = Enumerable.Range(0, 100000)
                .Select(p => new SimpleTableWithIdentity { Id = p + 1,  Title = $"Bla{p}" });
await ctx.BulkInsertAsync(items, p => p.IdentityInsert(true)); 

```
