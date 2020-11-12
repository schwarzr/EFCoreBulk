using System;
using Microsoft.Data.SqlClient;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class BulkOptionsBuilder
    {
        private Func<SqlBulkCopyOptions, SqlBulkCopyOptions> _bulkOptionsFactory;
        private bool _identityInsert = false;
        private bool _ignoreDefaultValue = false;
        private bool _propagateValues = true;
        private Action<SqlBulkCopy> _setup;
        private IShadowPropertyAccessor _shadowPropertyAccessor;

        public BulkOptions Options => new BulkOptions(_ignoreDefaultValue, _propagateValues, _identityInsert, _shadowPropertyAccessor, _bulkOptionsFactory, _setup);

        public BulkOptionsBuilder IdentityInsert(bool enabled = true)
        {
            _identityInsert = enabled;
            return this;
        }

        public BulkOptionsBuilder IgnoreDefaultValues(bool ignore = true)
        {
            _ignoreDefaultValue = ignore;
            return this;
        }

        public BulkOptionsBuilder PropagateValues(bool propagate = true)
        {
            _propagateValues = propagate;
            return this;
        }

        public BulkOptionsBuilder Setup(Action<SqlBulkCopy> setup)
        {
            _setup = setup;
            return this;
        }

        public BulkOptionsBuilder ShadowPropertyAccessor(IShadowPropertyAccessor accessor)
        {
            _shadowPropertyAccessor = accessor;
            return this;
        }

        public BulkOptionsBuilder SqlBulkOptions(Func<SqlBulkCopyOptions, SqlBulkCopyOptions> bulkOptionsFactory)
        {
            _bulkOptionsFactory = bulkOptionsFactory;
            return this;
        }
    }
}