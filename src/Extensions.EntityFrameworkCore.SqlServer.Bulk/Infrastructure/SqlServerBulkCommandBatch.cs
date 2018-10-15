using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
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

        public override IReadOnlyList<ModificationCommand> ModificationCommands => _bulkMode ? _commands : _modificationCommandBatch.ModificationCommands;

        public override bool AddCommand(ModificationCommand modificationCommand)
        {
            var state = modificationCommand.EntityState;
            var bulkMode = false;

            if ((state == EntityState.Added && _bulkOptions.BulkInsertEnabled) ||
                    (state == EntityState.Modified && _bulkOptions.BulkUpdateEnabled) ||
                    (state == EntityState.Deleted && _bulkOptions.BulkDeleteEnabled))
            {
                bulkMode = true;
            }

            if (!_state.HasValue)
            {
                _state = state;
                _table = modificationCommand.TableName;
                _schema = modificationCommand.Schema;

                _bulkMode = bulkMode;
            }

            if (bulkMode != _bulkMode)
            {
                return false;
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
                var result = processor.Process(connection, _commands);
                if (result != _commands.Count)
                {
                    ThrowAggregateUpdateConcurrencyException(_commands.Count, result);
                }
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
                var result = await processor.ProcessAsync(connection, _commands, cancellationToken);
                if (result != _commands.Count)
                {
                    ThrowAggregateUpdateConcurrencyException(_commands.Count, result);
                }
            }
            else
            {
                await _modificationCommandBatch.ExecuteAsync(connection, cancellationToken);
            }
        }

        protected virtual void ThrowAggregateUpdateConcurrencyException(
            int expectedRowsAffected,
            int rowsAffected)
        {
            var entries = _commands.SelectMany(p => p.Entries).ToImmutableList();

            throw new DbUpdateConcurrencyException(
                RelationalStrings.UpdateConcurrencyException(expectedRowsAffected, rowsAffected),
                entries);
        }

        private IBulkProcessor<ModificationCommand> GetBulkProcessor()
        {
            switch (_state)
            {
                case EntityState.Deleted:
                    return new DeleteBulkProcessor<ModificationCommand>(new ModificationCommandSetupProvider(_commands));

                case EntityState.Modified:
                    break;

                case EntityState.Added:
                    return new InsertBulkProcessor<ModificationCommand>(new ModificationCommandSetupProvider(_commands));
            }

            return null;
        }
    }
}