using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public abstract class BulkProcessor<TItem> : IBulkProcessor<TItem>
    {
        public BulkProcessor(EntityState state, IColumnSetupProvider columnSetupProvider)
        {
            State = state;
            ColumnSetupProvider = columnSetupProvider;
        }

        public IColumnSetupProvider ColumnSetupProvider { get; }

        public EntityState State { get; }

        public abstract int Process(IRelationalConnection connection, IEnumerable<TItem> items);

        public abstract Task<int> ProcessAsync(IRelationalConnection connection, IEnumerable<TItem> items, CancellationToken cancellation = default(CancellationToken));
    }
}