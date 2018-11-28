using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class BulkOptions
    {
        private readonly Func<SqlBulkCopyOptions, SqlBulkCopyOptions> _bulkOptionsFactory;

        public BulkOptions(bool ignoreDefaultValues, bool propagateValues, bool identityInsert, IShadowPropertyAccessor shadowPropertyAccessor, Func<SqlBulkCopyOptions, SqlBulkCopyOptions> bulkOptions, Action<SqlBulkCopy> setup)
        {
            IdentityInsert = identityInsert;
            _bulkOptionsFactory = bulkOptions;
            Setup = setup;
            IgnoreDefaultValues = ignoreDefaultValues;
            PropagateValues = propagateValues;
            ShadowPropertyAccessor = shadowPropertyAccessor;
        }

        public bool IdentityInsert { get; }

        public bool IgnoreDefaultValues { get; }

        public bool PropagateValues { get; }

        public Action<SqlBulkCopy> Setup { get; }

        public IShadowPropertyAccessor ShadowPropertyAccessor { get; }

        public static SqlBulkCopyOptions DefaultBulkOptions(EntityState state)
        {
            if (state == EntityState.Added)
            {
                return SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers;
            }
            return SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.KeepIdentity;
        }

        public SqlBulkCopyOptions GetSqlBulkOptions(EntityState state)
        {
            var result = DefaultBulkOptions(state);

            if (IdentityInsert)
            {
                result = result | SqlBulkCopyOptions.KeepIdentity;
            }

            return _bulkOptionsFactory?.Invoke(result) ?? result;
        }
    }
}