using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class InsertBulkProcessor<TEntity> : SqlServerBulkProcessor<TEntity>
    {
        private readonly string bulkTable;
        private readonly string targetTableName;

        public InsertBulkProcessor(IColumnSetupProvider columnSetupProvider) : base(EntityState.Added, columnSetupProvider)
        {
            targetTableName = $"[{columnSetupProvider.TableName}]";
            bulkTable = targetTableName;

            if (OutboundColumns.Any())
            {
                bulkTable = $"[#{columnSetupProvider.TableName}_{State}]";
            }

            if (!string.IsNullOrWhiteSpace(columnSetupProvider.SchemaName))
            {
                targetTableName = $"[{columnSetupProvider.SchemaName}].{targetTableName}";
                bulkTable = $"[{columnSetupProvider.SchemaName}].{bulkTable}";
            }
        }

        protected override SqlCommand CommitStatement(IRelationalConnection connection)
        {
            if (!OutboundColumns.Any())
            {
                return null;
            }

            var writeColumns = string.Join(", ", InboundColumns.Select(p => $"[{p.ColumnName}]"));

            var commandText = $"INSERT INTO {targetTableName} ({writeColumns}) " +
                                $"OUTPUT {string.Join(", ", OutboundColumns.Select(p => $"inserted.[{p.ColumnName}]"))} " +
                                $"SELECT {writeColumns} FROM {bulkTable}";

            var command = connection.DbConnection.CreateCommand();
            command.CommandText = commandText;
            command.Transaction = connection.CurrentTransaction.GetDbTransaction();
            command.CommandTimeout = connection.CommandTimeout ?? 60;
            return (SqlCommand)command;
        }

        protected override SqlBulkCopy CreateBulkCopy(IRelationalConnection connection)
        {
            var bulk = new SqlBulkCopy((SqlConnection)connection.DbConnection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, (SqlTransaction)connection.CurrentTransaction.GetDbTransaction());
            bulk.BulkCopyTimeout = connection.CommandTimeout ?? 60;
            bulk.DestinationTableName = bulkTable;
            InboundColumns.ForEach(p => bulk.ColumnMappings.Add(p.ColumnName, p.ColumnName));

            return bulk;
        }

        protected override SqlCommand PrepareStatement(IRelationalConnection connection)
        {
            if (!OutboundColumns.Any())
            {
                return null;
            }

            var columnNames = InboundColumns.Select(p => $"[{p.ColumnName}]");

            var commandText = $"Select Top 0 {string.Join(", ", columnNames)} into {bulkTable} FROM {targetTableName}";

            var command = connection.DbConnection.CreateCommand();
            command.Transaction = connection.CurrentTransaction.GetDbTransaction();
            command.CommandText = commandText;
            command.CommandTimeout = connection.CommandTimeout ?? 60;

            return (SqlCommand)command;
        }
    }
}