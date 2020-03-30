using System;
using System.Collections.Generic;
using System.Text;
using Bulk.Test.Model;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;

namespace Bulk.Test
{
    public class ShadowPropertyAccessor : IShadowPropertyAccessor
    {
        private static readonly ShadowPropertyAccessor _current;

        static ShadowPropertyAccessor()
        {
            _current = new ShadowPropertyAccessor();
        }

        private ShadowPropertyAccessor()
        {
        }

        public static ShadowPropertyAccessor Current => _current;

        public object GetValue(object entity, string property)
        {
            var shadowEntity = entity as IShadowPropertyEntity;
            if (shadowEntity == null)
            {
                throw new NotSupportedException("Only IShadowPropertyEntities are allowed");
            }
            return shadowEntity.GetValue(property);
        }

        public void StoreValue(object entity, string property, object value)
        {
            var shadowEntity = entity as IShadowPropertyEntity;
            if (shadowEntity == null)
            {
                throw new NotSupportedException("Only IShadowPropertyEntities are allowed");
            }
            shadowEntity.StoreValue(property, value);
        }
    }
}