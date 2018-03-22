using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public interface IColumnSetupProvider
    {
        string SchemaName { get; }

        string TableName { get; }

        IEnumerable<IColumnSetup> Build();

        void PropagateValues(object entity, IDictionary<IColumnSetup, object> values);
    }
}