using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore
{
    public static class EntityFrameworkCoreSqlServerBulkPropertyExtensions
    {
        private static readonly ConcurrentDictionary<Type, ValueConverter> _spatialConverterCache = new ConcurrentDictionary<Type, ValueConverter>();

        public static bool IsSpatial(this IProperty property, out ValueConverter spatialConverter)
        {
            spatialConverter = _spatialConverterCache.GetOrAdd(property.ClrType, p => GetSpatialConverter(p, property));

            return spatialConverter != null;
        }

        private static ValueConverter GetSpatialConverter(Type p, IProperty property)
        {
            var expectedType = typeof(RelationalGeometryTypeMapping<,>).MakeGenericType(p, typeof(SqlBytes));
            var mapping = property.GetRelationalTypeMapping();

            if (expectedType.IsInstanceOfType(mapping))
            {
                var propertyInfo = mapping.GetType().GetProperty("SpatialConverter", BindingFlags.Instance | BindingFlags.NonPublic);

                if (propertyInfo != null)
                {
                    return propertyInfo.GetValue(mapping, null) as ValueConverter;
                }
            }

            return null;
        }
    }
}