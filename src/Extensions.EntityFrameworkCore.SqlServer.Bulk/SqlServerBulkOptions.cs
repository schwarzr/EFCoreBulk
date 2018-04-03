namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class SqlServerBulkOptions
    {
        public SqlServerBulkOptions()
        {
            InsertEnabled = true;
        }

        public bool DeleteEnabled { get; set; }

        public bool InsertEnabled { get; set; }

        public bool UpdateEnabled { get; set; }
    }
}