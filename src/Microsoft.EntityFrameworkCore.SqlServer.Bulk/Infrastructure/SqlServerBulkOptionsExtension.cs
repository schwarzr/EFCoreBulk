using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    internal class SqlServerBulkOptionsExtension : IDbContextOptionsExtension
    {
        public string LogFragment => "SqlServerBulk";
#if (NETSTANDARD2_0)
        public bool ApplyServices(IServiceCollection services)
#else

        public void ApplyServices(IServiceCollection services)
#endif
        {
            services.AddScoped<IModificationCommandBatchFactory, BulkModificationCommantBatchFactory>();
#if (NETSTANDARD2_0)

            return true;
#endif
        }

        public virtual long GetServiceProviderHashCode()
        {
            return 65432;
        }

        public virtual void Validate(IDbContextOptions options)
        {
        }
    }
}