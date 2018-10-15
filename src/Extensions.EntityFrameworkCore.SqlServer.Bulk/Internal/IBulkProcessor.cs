using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public interface IBulkProcessor<TItem>
    {
        IColumnSetupProvider ColumnSetupProvider { get; }

        EntityState State { get; }

        int Process(IRelationalConnection connection, IEnumerable<TItem> items);

        Task<int> ProcessAsync(IRelationalConnection connection, IEnumerable<TItem> items, CancellationToken cancellation = default(CancellationToken));
    }
}