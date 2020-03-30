using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    public class BulkModificationCommantBatchFactory : SqlServerModificationCommandBatchFactory
    {
        private readonly SqlServerBulkOptions _bulkOptions;

        public BulkModificationCommantBatchFactory(ModificationCommandBatchFactoryDependencies dependencies,
            IDbContextOptions options,
            SqlServerBulkOptions bulkOptions) 
            : base(dependencies, options)
        {
            _bulkOptions = bulkOptions;
        }

        public override ModificationCommandBatch Create()
        {
            return new SqlServerBulkModificationCommandBatch(base.Create(), _bulkOptions);
        }
    }
}