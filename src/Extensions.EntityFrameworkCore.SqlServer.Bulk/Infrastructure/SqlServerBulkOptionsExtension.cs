using System;
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

        public string LogFragment => "SqlServerBulk";
        public bool ApplyServices(IServiceCollection services)
        {
            services.AddScoped<IModificationCommandBatchFactory, BulkModificationCommantBatchFactory>();
            services.AddSingleton(_bulkOptions);
            return true;
        }

        public virtual long GetServiceProviderHashCode()
        {
            return _bulkOptions.GetHashCode() * 999;
        }

        public virtual void Validate(IDbContextOptions options)
        {
        }
    }
}