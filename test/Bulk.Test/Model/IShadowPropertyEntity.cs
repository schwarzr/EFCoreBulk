using System;
using System.Collections.Generic;
using System.Text;

namespace Bulk.Test.Model
{
    public interface IShadowPropertyEntity
    {
        object GetValue(string property);

        void StoreValue(string property, object value);
    }
}