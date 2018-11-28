using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public abstract class SqlServerBulkProcessor<TEntity> : BulkProcessor<TEntity>
    {
        public SqlServerBulkProcessor(EntityState state, IColumnSetupProvider columnSetupProvider, SqlBulkCopyOptions options)
            : base(state, columnSetupProvider)
        {
            SqlBulkCopyOptions = options;

            InboundColumns = ImmutableList.CreateRange(columnSetupProvider.Build().Where(p => p.ValueDirection.HasFlag(ValueDirection.Write)));
            OutboundColumns = ImmutableList.CreateRange(columnSetupProvider.Build().Where(p => p.ValueDirection.HasFlag(ValueDirection.Read)));
        }

        public ImmutableList<IColumnSetup> InboundColumns { get; }

        public ImmutableList<IColumnSetup> OutboundColumns { get; }

        public SqlBulkCopyOptions SqlBulkCopyOptions { get; }

        protected abstract string TempTableName { get; }

        public override int Process(IRelationalConnection connection, IEnumerable<TEntity> items)
        {
            int result = 0;

            using (var prepare = PrepareStatement(connection).NullDisposable())
            {
                if (prepare.Target != null)
                {
                    prepare.Target.ExecuteNonQuery();
                }
            }

            using (var bulk = CreateBulkCopy(connection))
            using (var enumerableReader = new EnumerableDataReader<TEntity>(items, InboundColumns, OutboundColumns.Any()))
            {
                bulk.WriteToServer(enumerableReader);

                using (var commit = CommitStatement(connection).NullDisposable())
                {
                    if (commit.Target != null)
                    {
                        if (OutboundColumns.Any())
                        {
                            using (var reader = commit.Target.ExecuteReader())
                            {
                                Dictionary<IColumnSetup, object> values = OutboundColumns.ToDictionary(p => p, p => (object)null);
                                var run = 0;
                                while (reader.Read())
                                {
                                    GetResultValues(reader, values);
                                    ColumnSetupProvider.PropagateValues(enumerableReader.TrackedItems[run], values);
                                    run++;
                                }

                                result = run;
                            }
                        }
                        else
                        {
                            result = commit.Target.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        result = enumerableReader.Position;
                    }
                }
            }

            using (var commit = CleanupStatement(connection).NullDisposable())
            {
                if (commit.Target != null)
                {
                    commit.Target.ExecuteNonQuery();
                }
            }

            return result;
        }

        public override async Task<int> ProcessAsync(IRelationalConnection connection, IEnumerable<TEntity> items, CancellationToken cancellation = default(CancellationToken))
        {
            int result = 0;

            using (var prepare = PrepareStatement(connection).NullDisposable())
            {
                if (prepare.Target != null)
                {
                    await prepare.Target.ExecuteNonQueryAsync(cancellation);
                }
            }

            using (var bulk = CreateBulkCopy(connection))
            using (var enumerableReader = new EnumerableDataReader<TEntity>(items, InboundColumns, OutboundColumns.Any()))
            {
                await bulk.WriteToServerAsync(enumerableReader, cancellation);

                using (var commit = CommitStatement(connection).NullDisposable())
                {
                    if (commit.Target != null)
                    {
                        if (OutboundColumns.Any())
                        {
                            using (var reader = await commit.Target.ExecuteReaderAsync(cancellation))
                            {
                                Dictionary<IColumnSetup, object> values = OutboundColumns.ToDictionary(p => p, p => (object)null);
                                var run = 0;
                                while (await reader.ReadAsync(cancellation))
                                {
                                    GetResultValues(reader, values);
                                    ColumnSetupProvider.PropagateValues(enumerableReader.TrackedItems[run], values);
                                    run++;
                                }

                                result = run;
                            }
                        }
                        else
                        {
                            result = await commit.Target.ExecuteNonQueryAsync(cancellation);
                        }
                    }
                    else
                    {
                        result = enumerableReader.Position;
                    }
                }
            }

            using (var commit = CleanupStatement(connection).NullDisposable())
            {
                if (commit.Target != null)
                {
                    await commit.Target.ExecuteNonQueryAsync(cancellation);
                }
            }

            return result;
        }

        protected virtual SqlCommand CleanupStatement(IRelationalConnection connection)
        {
            var tempTableName = TempTableName;
            if (tempTableName != null)
            {
                var commandText = $"DROP TABLE {tempTableName}";

                var command = connection.DbConnection.CreateCommand();
                command.CommandText = commandText;
                command.Transaction = connection.CurrentTransaction.GetDbTransaction();
                command.CommandTimeout = connection.CommandTimeout ?? 60;
                return (SqlCommand)command;
            }

            return null;
        }

        protected abstract SqlCommand CommitStatement(IRelationalConnection connection);

        protected abstract SqlBulkCopy CreateBulkCopy(IRelationalConnection connection);

        protected abstract SqlCommand PrepareStatement(IRelationalConnection connection);

        private void GetResultValues(SqlDataReader reader, IDictionary<IColumnSetup, object> values)
        {
            for (int i = 0; i < OutboundColumns.Count; i++)
            {
                var value = reader.GetValue(i);
                if (value == DBNull.Value)
                {
                    value = null;
                }
                values[OutboundColumns[i]] = value;
            }
        }
    }
}