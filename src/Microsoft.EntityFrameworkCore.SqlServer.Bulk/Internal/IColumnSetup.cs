using System;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public interface IColumnSetup
    {
        string ColumnName { get; }

        Type ColumnType { get; }

        int Ordinal { get; }

        ValueDirection ValueDirection { get; }

        object GetValue(object parameter);

        void SetValue(object parameter, object value);
    }
}