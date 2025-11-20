using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Intentional")]
    public class BulkModificationCommantBatchFactory : SqlServerModificationCommandBatchFactory
    {
        private readonly SqlServerBulkOptions _bulkOptions;
        private readonly SqlServerBulkConfiguration _sqlServerBulkConfiguration;

        public BulkModificationCommantBatchFactory(
            ModificationCommandBatchFactoryDependencies dependencies,
            IDbContextOptions options,
            SqlServerBulkOptions bulkOptions,
            SqlServerBulkConfiguration sqlServerBulkConfiguration)
            : base(dependencies, options)
        {
            _bulkOptions = bulkOptions;
            _sqlServerBulkConfiguration = sqlServerBulkConfiguration;
        }

        public override ModificationCommandBatch Create()
        {
            return new SqlServerBulkModificationCommandBatch(base.Create(), _bulkOptions, _sqlServerBulkConfiguration);
        }
    }
}