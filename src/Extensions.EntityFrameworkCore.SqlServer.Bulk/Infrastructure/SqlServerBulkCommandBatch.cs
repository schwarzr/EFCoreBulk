﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure
{
    public class SqlServerBulkModificationCommandBatch : ModificationCommandBatch
    {
        private bool _bulkMode;
        private SqlServerBulkOptions _bulkOptions;
        private readonly SqlServerBulkConfiguration _sqlServerBulkConfiguration;
        private ImmutableList<IReadOnlyModificationCommand> _commands;
        private ModificationCommandBatch _modificationCommandBatch;
        private string _schema;
        private EntityState? _state;
        private string _table;
        private bool _areMoreBatchesExpected;

        public override bool RequiresTransaction => _bulkMode ? true : _modificationCommandBatch.RequiresTransaction;

        public SqlServerBulkModificationCommandBatch(ModificationCommandBatch modificationCommandBatch, SqlServerBulkOptions bulkOptions, SqlServerBulkConfiguration _sqlServerBulkConfiguration)
        {
            this._modificationCommandBatch = modificationCommandBatch;
            _commands = ImmutableList.Create<IReadOnlyModificationCommand>();
            _bulkOptions = bulkOptions;
            this._sqlServerBulkConfiguration = _sqlServerBulkConfiguration;
        }

        public override bool AreMoreBatchesExpected => _areMoreBatchesExpected;

        public override void Complete(bool moreBatchesExpected)
        {
            _areMoreBatchesExpected = moreBatchesExpected;
            if (!_bulkMode)
            {
                _modificationCommandBatch.Complete(moreBatchesExpected);
            }
        }

        public override IReadOnlyList<IReadOnlyModificationCommand> ModificationCommands => _bulkMode ? _commands : _modificationCommandBatch.ModificationCommands;

        public override bool TryAddCommand(IReadOnlyModificationCommand modificationCommand)
        {
            if (_sqlServerBulkConfiguration.Disabled)
            {
                _bulkMode = false;
                return _modificationCommandBatch.TryAddCommand(modificationCommand);
            }

            var state = modificationCommand.EntityState;
            var bulkMode = false;

            if ((state == EntityState.Added && _bulkOptions.InsertEnabled) ||
                    (state == EntityState.Deleted && _bulkOptions.DeleteEnabled))
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
                return _modificationCommandBatch.TryAddCommand(modificationCommand);
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

        private IBulkProcessor<IReadOnlyModificationCommand> GetBulkProcessor()
        {
            switch (_state)
            {
                case EntityState.Deleted:
                    return new DeleteBulkProcessor<IReadOnlyModificationCommand>(new ModificationCommandSetupProvider(_commands), BulkOptions.DefaultBulkOptions(EntityState.Deleted));

                case EntityState.Added:
                    return new InsertBulkProcessor<IReadOnlyModificationCommand>(new ModificationCommandSetupProvider(_commands), BulkOptions.DefaultBulkOptions(EntityState.Added));
            }

            return null;
        }
    }
}