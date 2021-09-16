using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    public class SqlServerBulkOptionsExtension : IDbContextOptionsExtension
    {
        private readonly SqlServerBulkOptions _bulkOptions;

        public SqlServerBulkOptionsExtension(SqlServerBulkOptions bulkOptions)
        {
            _bulkOptions = bulkOptions;
        }

        public SqlServerBulkOptions BulkOptions => _bulkOptions;


        public DbContextOptionsExtensionInfo Info => new SqlServerBulkOptionsExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.AddScoped<IModificationCommandBatchFactory, BulkModificationCommantBatchFactory>();
            services.AddSingleton(_bulkOptions);
            services.AddScoped<SqlServerBulkConfiguration>(sp => new SqlServerBulkConfiguration { Disabled = sp.GetRequiredService<SqlServerBulkOptions>().DisableByDefault });
        }

        public virtual void Validate(IDbContextOptions options)
        {
        }

        private class SqlServerBulkOptionsExtensionInfo : DbContextOptionsExtensionInfo
        {
            private readonly SqlServerBulkOptionsExtension _extension;

            public SqlServerBulkOptionsExtensionInfo(SqlServerBulkOptionsExtension extension)
                : base(extension)
            {
                _extension = extension;
            }

            public override bool IsDatabaseProvider => false;

            public override string LogFragment => "SqlServerBulk";

            public override long GetServiceProviderHashCode()
            {
                return _extension.BulkOptions.GetHashCode() * 999;
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo.Add("SqlServerBulkExtensions", _extension.BulkOptions.ToString());
            }
        }
    }
}