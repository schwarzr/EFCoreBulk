using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class EntityMetadataColumnSetupProvider : IColumnSetupProvider
    {
        private readonly ImmutableList<IColumnSetup> _columns;
        private readonly bool _propagateValues;

        public EntityMetadataColumnSetupProvider(IEntityType entity, bool propagateValues, EntityState state)
        {
            _propagateValues = propagateValues;
            var relational = entity.Relational();
            if (relational == null)
            {
                throw new NotSupportedException($"The Entity {entity} does not have a relational mapping.");
            }

            SchemaName = relational.Schema;
            TableName = relational.TableName;

            var properties = entity.GetProperties();

            if (state == EntityState.Deleted)
            {
                properties = properties.Where(p => p.IsPrimaryKey());
            }

            if (!propagateValues)
            {
                properties = properties.Where(p => !p.IsStoreGeneratedAlways);
                if (state == EntityState.Added)
                {
                    properties = properties.Where(p => !p.ValueGenerated.HasFlag(ValueGenerated.OnAdd));
                }
            }

            _columns = properties.Select((p, i) => CreateColumnSetup(entity, p, i, propagateValues, state)).ToImmutableList();
        }

        public string SchemaName { get; }

        public string TableName { get; }

        public IEnumerable<IColumnSetup> Build()
        {
            return _columns;
        }

        public void PropagateValues(object entity, IDictionary<IColumnSetup, object> values)
        {
            foreach (var item in values)
            {
                item.Key.SetValue(entity, item.Value);
            }
        }

        private IColumnSetup CreateColumnSetup(IEntityType entity, IProperty property, int index, bool propagateValues, EntityState state)
        {
            var relational = entity.Relational();

            if (relational.DiscriminatorProperty == property)
            {
                var discriminatorValue = relational.DiscriminatorValue;
                return new DelegateColumnSetup(index, property.Relational().ColumnName, property.ClrType, p => discriminatorValue, (p, q) => { }, ValueDirection.Write);
            }

            var param = Expression.Parameter(typeof(object), "p");
            var param2 = Expression.Parameter(typeof(object), "q");

            var cast = Expression.Convert(param, property.DeclaringEntityType.ClrType);

            var getValue = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Property(cast, property.PropertyInfo), typeof(object)), param);
            var setValue = Expression.Lambda<Action<object, object>>(Expression.Assign(Expression.Property(cast, property.PropertyInfo), Expression.Convert(param2, property.ClrType)), param, param2);

            return new DelegateColumnSetup(index, property.Relational().ColumnName, property.ClrType, getValue.Compile(), setValue.Compile(), propagateValues ? GetValueDirection(property, state) : ValueDirection.Write);
        }

        private ValueDirection GetValueDirection(IProperty property, EntityState state)
        {
            if (property.IsStoreGeneratedAlways || (state == EntityState.Added && property.ValueGenerated.HasFlag(ValueGenerated.OnAdd)))
            {
                return ValueDirection.Read;
            }

            if (property.ValueGenerated.HasFlag(ValueGenerated.OnAddOrUpdate) && (state == EntityState.Added || state == EntityState.Modified))
            {
                return ValueDirection.Both;
            }

            return ValueDirection.Write;
        }
    }
}