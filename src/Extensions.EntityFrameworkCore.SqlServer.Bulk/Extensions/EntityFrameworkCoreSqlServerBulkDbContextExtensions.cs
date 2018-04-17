using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore
{
    public static class EntityFrameworkCoreSqlServerBulkDbContextExtensions
    {
        public static async Task BulkInsertAsync<TEntity>(this DbContext context, IEnumerable<TEntity> items, bool propatateValues = true, CancellationToken token = default(CancellationToken), IShadowPropertyAccessor shadowPropertyAccessor = null)
        {
            var sp = ((IInfrastructure<IServiceProvider>)context);
            var options = sp.GetService<IDbContextOptions>();
            var entity = context.Model.FindEntityType(typeof(TEntity));

            if (entity == null)
            {
                throw new NotSupportedException($"The type {typeof(TEntity)} is not part of the EntityFramework metadata model. Only mapped entities are supported.");
            }

            using (var relationalConnection = sp.GetService<IRelationalConnection>())
            {
                IDbContextTransaction target = null;

                if (relationalConnection.CurrentTransaction == null)
                {
                    target = await relationalConnection.BeginTransactionAsync(token);
                }

                using (var transaction = target.NullDisposable())
                {
                    var insertProcessor = new InsertBulkProcessor<TEntity>(new EntityMetadataColumnSetupProvider(entity, propatateValues, EntityState.Added, shadowPropertyAccessor));
                    await insertProcessor.ProcessAsync(relationalConnection, items, token);
                    transaction.Target?.Commit();
                }
            }
        }
    }
}