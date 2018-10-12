using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class BulkOptionsBuilder
    {
        private bool _ignoreDefaultValue = false;
        private bool _propagateValues = true;
        private IShadowPropertyAccessor _shadowPropertyAccessor;

        public BulkOptions Options => new BulkOptions(_ignoreDefaultValue, _propagateValues, _shadowPropertyAccessor);

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

        public BulkOptionsBuilder ShadowPropertyAccessor(IShadowPropertyAccessor accessor)
        {
            _shadowPropertyAccessor = accessor;
            return this;
        }
    }
}