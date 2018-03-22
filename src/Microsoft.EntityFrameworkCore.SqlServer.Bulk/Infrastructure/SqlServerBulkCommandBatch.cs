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
        private bool _bulkMode;
        private SqlServerBulkOptionsExtension _bulkOptions;
        private ImmutableList<ModificationCommand> _commands;
        private ModificationCommandBatch _modificationCommandBatch;
        private string _schema;
        private EntityState? _state;
        private string _table;

        public SqlServerBulkModificationCommandBatch(ModificationCommandBatch modificationCommandBatch, SqlServerBulkOptionsExtension bulkOptions)
        {
            this._modificationCommandBatch = modificationCommandBatch;
            _commands = ImmutableList.Create<ModificationCommand>();
            _bulkOptions = bulkOptions;
        }

        public override IReadOnlyList<ModificationCommand> ModificationCommands => _commands;

        public override bool AddCommand(ModificationCommand modificationCommand)
        {
            if (!_state.HasValue)
            {
                _state = modificationCommand.EntityState;
                _table = modificationCommand.TableName;
                _schema = modificationCommand.Schema;

                if ((_state == EntityState.Added && _bulkOptions.BulkInsertEnabled) ||
                    (_state == EntityState.Modified && _bulkOptions.BulkUpdateEnabled) ||
                    (_state == EntityState.Deleted && _bulkOptions.BulkDeleteEnabled))
                {
                    _bulkMode = true;
                }
            }

            if (!_bulkMode)
            {
                return _modificationCommandBatch.AddCommand(modificationCommand);
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
            if (_bulkMode)
            {
                var processor = GetBulkProcessor();
                processor.Process(connection, _commands);
            }
            else
            {
                _modificationCommandBatch.Execute(connection);
            }
        }

        public override async Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_bulkMode)
            {
                var processor = GetBulkProcessor();
                await processor.ProcessAsync(connection, _commands, cancellationToken);
            }
            else
            {
                await _modificationCommandBatch.ExecuteAsync(connection, cancellationToken);
            }
        }

        private IBulkProcessor<ModificationCommand> GetBulkProcessor()
        {
            switch (_state)
            {
                case EntityState.Deleted:
                    break;

                case EntityState.Modified:
                    break;

                case EntityState.Added:
                    return new InsertBulkProcessor<ModificationCommand>(new ModificationCommandSetupProvider(_commands));
            }

            return null;
        }
    }
}