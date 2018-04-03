using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    public class BulkModificationCommantBatchFactory : SqlServerModificationCommandBatchFactory
    {
        private readonly SqlServerBulkOptionsExtension _bulkOptions;

        public BulkModificationCommantBatchFactory(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, ISqlServerUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory, IDbContextOptions options)
            : base(commandBuilderFactory, sqlGenerationHelper, updateSqlGenerator, valueBufferFactoryFactory, options)
        {
            _bulkOptions = options.FindExtension<SqlServerBulkOptionsExtension>();
        }

        public override ModificationCommandBatch Create()
        {
            return new SqlServerBulkModificationCommandBatch(base.Create(), _bulkOptions);
        }
    }
}