using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class BulkOptions
    {
        public BulkOptions(bool ignoreDefaultValues, bool propagateValues, IShadowPropertyAccessor shadowPropertyAccessor)
        {
            IgnoreDefaultValues = ignoreDefaultValues;
            PropagateValues = propagateValues;
            ShadowPropertyAccessor = shadowPropertyAccessor;
        }

        public bool IgnoreDefaultValues { get; }

        public bool PropagateValues { get; }

        public IShadowPropertyAccessor ShadowPropertyAccessor { get; }
    }
}