using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    public class SqlServerBulkOptionsExtension : IDbContextOptionsExtension
    {
        public bool BulkDeleteEnabled { get; private set; }

        public bool BulkInsertEnabled { get; private set; }

        public bool BulkUpdateEnabled { get; private set; }

        public string LogFragment => "SqlServerBulk";
#if (NETSTANDARD2_0)
        public bool ApplyServices(IServiceCollection services)
        {
#else

        public void ApplyServices(IServiceCollection services)
        {
#endif
            services.AddScoped<IModificationCommandBatchFactory, BulkModificationCommantBatchFactory>();
#if (NETSTANDARD2_0)

            return true;
#endif
        }

        public virtual long GetServiceProviderHashCode()
        {
            return BulkInsertEnabled.GetHashCode() ^ BulkDeleteEnabled.GetHashCode() ^ BulkUpdateEnabled.GetHashCode();
        }

        public virtual void Validate(IDbContextOptions options)
        {
        }

        internal void ApplyOptions(SqlServerBulkOptions options)
        {
            BulkInsertEnabled = options.InsertEnabled;
            BulkUpdateEnabled = options.UpdateEnabled;
            BulkDeleteEnabled = options.DeleteEnabled;
        }
    }
}