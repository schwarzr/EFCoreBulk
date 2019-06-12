using System;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class SqlServerBulkOptions : IEquatable<SqlServerBulkOptions>
    {
        public SqlServerBulkOptions()
        {
            InsertEnabled = true;
            DeleteEnabled = true;
        }

        public bool DeleteEnabled { get; set; }

        public bool InsertEnabled { get; set; }

        public SqlServerBulkOptions EnableBulkInsert(bool enabled = true)
        {
            InsertEnabled = enabled;
            return this;
        }

        public SqlServerBulkOptions EnableBulkDelete(bool enabled = true)
        {
            DeleteEnabled = enabled;
            return this;
        }

        public override int GetHashCode()
        {
            return this.InsertEnabled.GetHashCode() ^ this.DeleteEnabled.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is SqlServerBulkOptions value)
            {
                return Equals(value);
            }
            return base.Equals(obj);
        }

        public bool Equals(SqlServerBulkOptions other)
        {
            return this.InsertEnabled == other.InsertEnabled && this.DeleteEnabled == other.DeleteEnabled;
        }
    }
}