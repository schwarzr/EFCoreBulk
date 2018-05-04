using System;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class DelegateColumnSetup : IColumnSetup
    {
        private readonly Func<object, object> _getValue;
        private readonly Action<object, object> _setValue;

        public DelegateColumnSetup(int ordinal, string columnName, Type columnType, Func<object, object> getValue, Action<object, object> setValue, ValueDirection direction = ValueDirection.Write)
        {
            Ordinal = ordinal;
            ValueDirection = direction;
            ColumnType = columnType;
            ColumnName = columnName;

            _getValue = getValue;
            _setValue = setValue;
        }

        public string ColumnName { get; }

        public Type ColumnType { get; }

        public int Ordinal { get; }

        public ValueDirection ValueDirection { get; }

        public object GetValue(object parameter)
        {
            return _getValue(parameter);
        }

        public void SetValue(object parameter, object value)
        {
            _setValue(parameter, value);
        }
    }
}