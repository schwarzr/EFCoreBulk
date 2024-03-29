﻿using System;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Bulk.Test
{
    public class DatabaseTest
    {
        private string _databaseName;
        private IServiceProvider _nonBulkServiceProvider;

        private ConcurrentBag<IServiceProvider> _bulkServiceProviders = new ConcurrentBag<IServiceProvider>();
        private ConcurrentBag<IServiceScope> _bulkServiceScopes = new ConcurrentBag<IServiceScope>();

        public DatabaseTest()
        {

        }

        [SetUp]
        public void Setup()
        {
            _databaseName = Guid.NewGuid().ToString("N");
            using (var connection = new SqlConnection($"Data Source=(localdb)\\mssqllocaldb;Initial Catalog=master;Integrated Security=True;"))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = $"CREATE DATABASE [{_databaseName}]";
                command.ExecuteNonQuery();
            }

            var coll = new ServiceCollection();
            coll
                .AddDbContext<TestContext>(p => p.UseSqlServer($"Data Source=(localdb)\\mssqllocaldb;Initial Catalog={_databaseName};Integrated Security=True;"));

            _nonBulkServiceProvider = coll.BuildServiceProvider();
            using (var scope = _nonBulkServiceProvider.CreateScope())
            {
                scope.ServiceProvider.GetService<TestContext>().Database.Migrate();
            }
        }

        [TearDown]
        protected virtual void TearDown()
        {
            foreach (var item in _bulkServiceScopes)
            {
                item.Dispose();
            }

            foreach (var item in _bulkServiceProviders)
            {
                ((IDisposable)item).Dispose();
            }

            using (var scope = _nonBulkServiceProvider.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetService<TestContext>();
                ctx.Database.EnsureDeleted();
            }

                    ((IDisposable)_nonBulkServiceProvider).Dispose();
        }

        protected IServiceProvider GetNonBulkServiceProvider()
        {
            return _nonBulkServiceProvider;
        }

        protected IServiceProvider GetServiceProvider(Action<SqlServerBulkOptions> config = null)
        {
            var coll = new ServiceCollection();
            coll
                .AddDbContext<TestContext>(p => p.UseSqlServer($"Data Source=(localdb)\\mssqllocaldb;Initial Catalog={_databaseName};Integrated Security=True;", options => options.AddBulk(config))
            );

            var result = coll.BuildServiceProvider();
            _bulkServiceProviders.Add(result);
            var scope = result.CreateScope();
            _bulkServiceScopes.Add(scope);
            return scope.ServiceProvider;
        }
    }
}