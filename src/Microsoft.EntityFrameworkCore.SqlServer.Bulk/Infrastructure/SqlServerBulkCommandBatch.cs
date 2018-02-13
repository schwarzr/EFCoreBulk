using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    public class SqlServerBulkModificationCommandBatch : ModificationCommandBatch
    {
        private ImmutableList<ModificationCommand> _commands;
        private string _schema;
        private EntityState? _state;
        private string _table;
        private ModificationCommandBatch modificationCommandBatch;

        public SqlServerBulkModificationCommandBatch(ModificationCommandBatch modificationCommandBatch)
        {
            this.modificationCommandBatch = modificationCommandBatch;
            _commands = ImmutableList.Create<ModificationCommand>();
        }

        public override IReadOnlyList<ModificationCommand> ModificationCommands => _commands;

        public override bool AddCommand(ModificationCommand modificationCommand)
        {
            if (!_state.HasValue)
            {
                _state = modificationCommand.EntityState;
            }

            if (_table == null)
            {
                _table = modificationCommand.TableName;
            }

            if (_schema == null)
            {
                _schema = modificationCommand.Schema;
            }

            if (_state != modificationCommand.EntityState || _table != modificationCommand.TableName || _schema != modificationCommand.Schema)
            {
                return false;
            }

            _commands = _commands.Add(modificationCommand);

            return true;
        }

        public override void Execute(IRelationalConnection connection)
        {
        }

        public override async Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken))
        {
            string targetTableName;
            string destinationTable;

            if (_commands.Any())
            {
                var cmd = _commands.First();
                var columnList = cmd.ColumnModifications.Where(p => p.IsWrite).ToList();

                if (cmd.RequiresResultPropagation)
                {
                    targetTableName = $"[#{_table}_{_state}]";
                    destinationTable = $"[{_table}]";

                    if (!string.IsNullOrWhiteSpace(_schema))
                    {
                        targetTableName = $"[{_schema}].{targetTableName}";
                        destinationTable = $"[{_schema}].{destinationTable}";
                    }

                    var columnNames = columnList.Select(p => $"[{p.ColumnName}]");

                    var commandText = $"Select Top 0 {string.Join(", ", columnNames)} into {targetTableName} FROM {destinationTable}";

                    var command = connection.DbConnection.CreateCommand();
                    command.Transaction = connection.CurrentTransaction.GetDbTransaction();
                    command.CommandText = commandText;
                    command.CommandTimeout = connection.CommandTimeout.GetValueOrDefault();

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                else
                {
                    targetTableName = $"[{_table}]";

                    if (!string.IsNullOrWhiteSpace(_schema))
                    {
                        targetTableName = $"[{_schema}].{targetTableName}";
                    }
                    destinationTable = targetTableName;
                }

                var bulk = new SqlBulkCopy((SqlConnection)connection.DbConnection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, (SqlTransaction)connection.CurrentTransaction.GetDbTransaction());
                bulk.DestinationTableName = targetTableName;
                columnList.ForEach(p => bulk.ColumnMappings.Add(p.ColumnName, p.ColumnName));

                await bulk.WriteToServerAsync(new EnumerableDataReader<ModificationCommand>(_commands, new ModificationCommandSetupProvider(_commands)), cancellationToken);

                if (cmd.RequiresResultPropagation)
                {
                    var readColumnList = cmd.ColumnModifications.Where(p => p.IsRead).ToList();
                    var writeColumns = string.Join(", ", columnList.Select(p => $"[{p.ColumnName}]"));

                    var commandText = $"INSERT INTO {destinationTable} ({writeColumns}) " +
                                        $"OUTPUT {string.Join(", ", readColumnList.Select(p => $"inserted.[{p.ColumnName}]"))} " +
                                        $"SELECT {writeColumns} FROM {targetTableName}";

                    var command = connection.DbConnection.CreateCommand();
                    command.CommandText = commandText;
                    command.Transaction = connection.CurrentTransaction.GetDbTransaction();
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        var values = new object[readColumnList.Count];
                        var run = 0;
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = reader.GetValue(i);
                            }

                            _commands[run].PropagateResults(new ValueBuffer(values));
                            run++;
                        }
                    }
                }
            }
        }
    }
}