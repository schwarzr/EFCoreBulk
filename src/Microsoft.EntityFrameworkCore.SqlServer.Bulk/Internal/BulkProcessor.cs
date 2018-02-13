using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public abstract class BulkProcessor<TItem> : IBulkProcessor<TItem>
    {
        public BulkProcessor(EntityState state, string schema, string table, IColumnSetupProvider columnSetupProvider)
        {
            State = state;
        }

        public EntityState State { get; }

        public void Process(IEnumerable<TItem> items)
        {
            throw new NotImplementedException();
        }

        public Task ProcessAsync(IEnumerable<TItem> items)
        {
            throw new NotImplementedException();
        }
    }
}