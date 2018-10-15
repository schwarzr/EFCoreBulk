using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class DeleteBulkProcessor<TEntity> : SqlServerBulkProcessor<TEntity>
    {
        private readonly string _bulkTable;
        private readonly string _targetTableName;

        public DeleteBulkProcessor(IColumnSetupProvider columnSetupProvider) : base(EntityState.Deleted, columnSetupProvider)
        {
            _targetTableName = $"[{columnSetupProvider.TableName}]";
            _bulkTable = $"[#{columnSetupProvider.TableName}_{State}]";

            if (!string.IsNullOrWhiteSpace(columnSetupProvider.SchemaName))
            {
                _targetTableName = $"[{columnSetupProvider.SchemaName}].{_targetTableName}";
                _bulkTable = $"[{columnSetupProvider.SchemaName}].{_bulkTable}";
            }
        }

        protected override string TempTableName => _bulkTable;

        protected override SqlCommand CommitStatement(IRelationalConnection connection)
        {
            var writeColumns = string.Join(" AND ", InboundColumns.Select(p => $"t.[{p.ColumnName}] = tmp.[{p.ColumnName}]"));

            var commandText = $"DELETE t FROM {_targetTableName} t " +
                              $"INNER JOIN {_bulkTable} tmp ON {writeColumns}";

            var command = connection.DbConnection.CreateCommand();
            command.CommandText = commandText;
            command.Transaction = connection.CurrentTransaction.GetDbTransaction();
            command.CommandTimeout = connection.CommandTimeout ?? 60;
            return (SqlCommand)command;
        }

        protected override SqlBulkCopy CreateBulkCopy(IRelationalConnection connection)
        {
            var bulk = new SqlBulkCopy((SqlConnection)connection.DbConnection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.KeepIdentity, (SqlTransaction)connection.CurrentTransaction.GetDbTransaction());
            bulk.BulkCopyTimeout = connection.CommandTimeout ?? 60;
            bulk.DestinationTableName = _bulkTable;
            InboundColumns.ForEach(p => bulk.ColumnMappings.Add(p.ColumnName, p.ColumnName));

            return bulk;
        }

        protected override SqlCommand PrepareStatement(IRelationalConnection connection)
        {
            var columnNames = InboundColumns.Select(p => $"[{p.ColumnName}]");

            var commandText = $"Select Top 0 {string.Join(", ", columnNames)} into {_bulkTable} FROM {_targetTableName}";

            var command = connection.DbConnection.CreateCommand();
            command.Transaction = connection.CurrentTransaction.GetDbTransaction();
            command.CommandText = commandText;
            command.CommandTimeout = connection.CommandTimeout ?? 60;

            return (SqlCommand)command;
        }
    }
}