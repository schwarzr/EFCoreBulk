using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class EnumerableDataReader<TItem> : DbDataReader
    {
        private readonly Dictionary<int, IColumnSetup> _columnMappings;
        private readonly Dictionary<string, int> _columnNameMappings;
        private readonly IEnumerable<TItem> _items;
        private readonly IList<TItem> _trackedItems;
        private readonly bool _trackItems;
        private IEnumerator<TItem> _enumerator;
        private object _enumeratorLocker = new object();
        private bool _isClosed = false;

        public EnumerableDataReader(IEnumerable<TItem> items, IEnumerable<IColumnSetup> columns = null, bool trackItems = false)
        {
            _trackItems = trackItems;
            _items = items;
            _trackedItems = new List<TItem>();
            TrackedItems = new ReadOnlyCollection<TItem>(this._trackedItems);

            if (columns == null)
            {
                columns = new ReflectionColumnSetupProvider(typeof(TItem)).Build().Where(p => p.ValueDirection.HasFlag(ValueDirection.Write)).ToList();
            }

            _columnMappings = columns.ToDictionary(p => p.Ordinal, p => p);
            _columnNameMappings = _columnMappings.Values.ToDictionary(p => p.ColumnName, p => p.Ordinal);
        }

        public override int Depth => 0;

        public override int FieldCount => _columnMappings.Count;

        public override bool HasRows => true;

        public override bool IsClosed => _isClosed;

        public override int RecordsAffected => throw new NotImplementedException();

        public ReadOnlyCollection<TItem> TrackedItems { get; }

        public override object this[int ordinal] => GetValue(ordinal);

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool GetBoolean(int ordinal)
        {
            return (bool)GetValue(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            return (byte)GetValue(ordinal);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            var value = (byte[])GetValue(ordinal);

            Buffer.BlockCopy(value, (int)dataOffset, buffer, bufferOffset, length);
            return length;
        }

        public override char GetChar(int ordinal)
        {
            return (char)GetValue(ordinal);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            var value = (char[])GetValue(ordinal);

            Buffer.BlockCopy(value, (int)dataOffset, buffer, bufferOffset, length);
            return length;
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return (DateTime)GetValue(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return (decimal)GetValue(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return (double)GetValue(ordinal);
        }

        public override IEnumerator GetEnumerator()
        {
            return _enumerator;
        }

        public override Type GetFieldType(int ordinal)
        {
            return _columnMappings[ordinal].ColumnType;
        }

        public override float GetFloat(int ordinal)
        {
            return (float)GetValue(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return (Guid)GetValue(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return (short)GetValue(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return (int)GetValue(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return (long)GetValue(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _columnMappings[ordinal].ColumnName;
        }

        public override int GetOrdinal(string name)
        {
            return _columnNameMappings[name];
        }

        public override string GetString(int ordinal)
        {
            return (string)GetValue(ordinal);
        }

        public override object GetValue(int ordinal)
        {
            return _columnMappings[ordinal].GetValue(_enumerator.Current);
        }

        public override int GetValues(object[] values)
        {
            foreach (var item in _columnMappings.Values)
            {
                values[item.Ordinal] = item.GetValue(_enumerator.Current);
            }
            return values.Length;
        }

        public override bool IsDBNull(int ordinal)
        {
            return GetValue(ordinal) == null;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            if (_enumerator == null)
            {
                lock (_enumeratorLocker)
                {
                    if (_enumerator == null)
                    {
                        _enumerator = _items.GetEnumerator();
                    }
                }
            }

            var result = _enumerator?.MoveNext() ?? false;
            if (_trackItems && result)
            {
                _trackedItems.Add(_enumerator.Current);
            }

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _enumerator?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}