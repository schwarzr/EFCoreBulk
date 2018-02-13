using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public interface IColumnSetupProvider
    {
        IEnumerable<IColumnSetup> Build();
    }
}