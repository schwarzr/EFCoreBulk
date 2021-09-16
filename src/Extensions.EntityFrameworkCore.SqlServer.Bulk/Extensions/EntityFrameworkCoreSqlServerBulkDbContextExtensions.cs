using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore
{
    public static class EntityFrameworkCoreSqlServerBulkDbContextExtensions
    {
        public static async Task BulkDeleteAsync<TEntity>(this DbContext context, IEnumerable<TEntity> items, Action<BulkOptionsBuilder> bulkOptions = null, CancellationToken token = default(CancellationToken))
        {
            GetBulkInfrstructure<TEntity>(context, out var sp, out var entity, out var relationalConnection);

            IDbContextTransaction target = null;
            if (relationalConnection.CurrentTransaction == null)
            {
                target = await relationalConnection.BeginTransactionAsync(token);
            }

            using (var transaction = target.NullDisposable())
            {
                var builder = new BulkOptionsBuilder();
                bulkOptions?.Invoke(builder);

                var options = builder.Options;

                var processor = new DeleteBulkProcessor<TEntity>(new EntityMetadataColumnSetupProvider(entity, EntityState.Deleted, options), options.GetSqlBulkOptions(EntityState.Deleted), options.Setup);
                await processor.ProcessAsync(relationalConnection, items, token);
                transaction.Target?.Commit();
            }
        }

        public static void EnableBulk(this DbContext context)
        {
            var config = GetConfig(context);

            config.Disabled = false;
        }

        public static void DisableBulk(this DbContext context)
        {
            var config = GetConfig(context);

            config.Disabled = true;
        }

        private static SqlServerBulkConfiguration GetConfig(DbContext context)
        {
            var config = ((IInfrastructure<IServiceProvider>)context).Instance.GetService<SqlServerBulkConfiguration>();
            if (config == null)
            {
                throw new NotSupportedException("Bulk extensions are not eanbled on this instance of DbContext");
            }

            return config;
        }

        public static async Task BulkInsertAsync<TEntity>(this DbContext context, IEnumerable<TEntity> items, Action<BulkOptionsBuilder> bulkOptions = null, CancellationToken token = default(CancellationToken))
        {
            GetBulkInfrstructure<TEntity>(context, out var sp, out var entity, out var relationalConnection);

            IDbContextTransaction target = null;
            if (relationalConnection.CurrentTransaction == null)
            {
                target = await relationalConnection.BeginTransactionAsync(token);
            }

            using (var transaction = target.NullDisposable())
            {
                var builder = new BulkOptionsBuilder();
                bulkOptions?.Invoke(builder);

                var options = builder.Options;

                var insertProcessor = new InsertBulkProcessor<TEntity>(new EntityMetadataColumnSetupProvider(entity, EntityState.Added, options), options.GetSqlBulkOptions(EntityState.Added), options.Setup);
                await insertProcessor.ProcessAsync(relationalConnection, items, token);
                transaction.Target?.Commit();
            }
        }

        private static void GetBulkInfrstructure<TEntity>(DbContext context, out IInfrastructure<IServiceProvider> sp, out IEntityType entity, out IRelationalConnection relationalConnection)
        {
            sp = (IInfrastructure<IServiceProvider>)context;
            var options = sp.GetService<IDbContextOptions>();
            entity = context.Model.FindEntityType(typeof(TEntity));

            if (entity == null)
            {
                throw new NotSupportedException($"The type {typeof(TEntity)} is not part of the EntityFramework metadata model. Only mapped entities are supported.");
            }

            relationalConnection = sp.GetService<IRelationalConnection>();
        }
    }
}