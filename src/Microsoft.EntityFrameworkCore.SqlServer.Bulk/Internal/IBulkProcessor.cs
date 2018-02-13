using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public interface IBulkProcessor<TItem>
    {
        EntityState State { get; }

        void Process(IEnumerable<TItem> items);

        Task ProcessAsync(IEnumerable<TItem> items);
    }
}