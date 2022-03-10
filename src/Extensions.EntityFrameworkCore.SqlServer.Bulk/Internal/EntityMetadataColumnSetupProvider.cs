using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class EntityMetadataColumnSetupProvider : IColumnSetupProvider
    {
        private readonly BulkOptions _bulkOptions;
        private readonly ImmutableList<IColumnSetup> _columns;

        public EntityMetadataColumnSetupProvider(IEntityType entity, EntityState state, BulkOptions bulkOptions)
        {
            _bulkOptions = bulkOptions;

            SchemaName = entity.GetSchema();
            TableName = entity.GetTableName();

            var properties = entity.GetProperties();

            if (state == EntityState.Deleted)
            {
                properties = properties.Where(p => p.IsPrimaryKey());
            }

            if (_bulkOptions.ShadowPropertyAccessor == null)
            {
                properties = properties.Where(p => entity.FindDiscriminatorProperty() == p || !p.IsShadowProperty());
            }

            var columns = properties.Select((p, i) => CreateColumnSetup(entity, p, i, state, _bulkOptions)).Where(p => p.ValueDirection != ValueDirection.None);

            if (!bulkOptions.PropagateValues)
            {
                columns = columns.Where(p => p.ValueDirection.HasFlag(ValueDirection.Write));
            }

            _columns = columns.ToImmutableList();
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

        private static ValueDirection GetValudDirection(IProperty property, PropertySaveBehavior saveBehavior, ValueGenerated generatorFlag)
        {
            if (saveBehavior != PropertySaveBehavior.Save || (property.IsPrimaryKey() && property.ValueGenerated.HasFlag(generatorFlag)))
            {
                return ValueDirection.Read;
            }
            else
            {
                var result = ValueDirection.Write;
                if (property.ValueGenerated.HasFlag(generatorFlag))
                {
                    result = result | ValueDirection.Read;
                }

                return result;
            }
        }

        private IColumnSetup CreateColumnSetup(IEntityType entity, IProperty property, int index, EntityState state, BulkOptions bulkOptions)
        {
            var storeIdentifier = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);

            var direction = GetValueDirection(property, state);

            if (property.IsPrimaryKey() && _bulkOptions.IdentityInsert)
            {
                direction = ValueDirection.Write;
            }

            if (!bulkOptions.PropagateValues)
            {
                direction = direction & ~ValueDirection.Read;
            }

            if (entity.FindDiscriminatorProperty() == property)
            {
                var discriminatorValue = entity.GetDiscriminatorValue();
                return new DelegateColumnSetup(index, property.GetColumnName(storeIdentifier.Value), property.ClrType, p => discriminatorValue, (p, q) => { }, ValueDirection.Write);
            }

            Expression<Func<object, object>> getValue = null;
            Expression<Action<object, object>> setValue = null;

            var valueConverter = property.GetValueConverter();

            if (property.IsShadowProperty())
            {
                var accessorType = typeof(IShadowPropertyAccessor);

                var param = Expression.Parameter(typeof(object), "p");
                var param2 = Expression.Parameter(typeof(object), "q");

                Expression getValueBody = Expression.Convert(Expression.Call(
                        Expression.Constant(bulkOptions.ShadowPropertyAccessor, accessorType),
                        accessorType.GetRuntimeMethod("GetValue", new[] { typeof(object), typeof(string) }),
                        param,
                        Expression.Constant(property.Name)),
                        property.ClrType);

                Expression setValueBody = param2;

                if (!bulkOptions.IgnoreDefaultValues)
                {
                    getValueBody = ProcessDefaultValue(getValueBody, property);
                }

                if (valueConverter != null)
                {
                    getValueBody = valueConverter.ConvertToProviderExpression.Body.Replace(valueConverter.ConvertToProviderExpression.Parameters[0], getValueBody);
                    setValueBody = Expression.Convert(valueConverter.ConvertFromProviderExpression.Body.Replace(valueConverter.ConvertFromProviderExpression.Parameters[0], Expression.Convert(setValueBody, valueConverter.ProviderClrType)), typeof(object));
                }

                getValue = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(getValueBody, typeof(object)),
                    param);
                setValue = Expression.Lambda<Action<object, object>>(
                    Expression.Call(
                        Expression.Constant(bulkOptions.ShadowPropertyAccessor, accessorType),
                        accessorType.GetRuntimeMethod("StoreValue", new[] { typeof(object), typeof(string), typeof(object) }),
                        param,
                        Expression.Constant(property.Name), setValueBody),
                    param,
                    param2);
            }
            else
            {
                var param = Expression.Parameter(typeof(object), "p");
                var param2 = Expression.Parameter(typeof(object), "q");

                var cast = Expression.Convert(param, property.DeclaringEntityType.ClrType);

                Expression getValueBody = Expression.Property(cast, property.PropertyInfo);
                Expression setValueBody = Expression.Convert(param2, property.ClrType);

                if (!bulkOptions.IgnoreDefaultValues)
                {
                    getValueBody = ProcessDefaultValue(getValueBody, property);
                }

                if (valueConverter != null)
                {
                    getValueBody = valueConverter.ConvertToProviderExpression.Body.Replace(valueConverter.ConvertToProviderExpression.Parameters[0], getValueBody);
                    setValueBody = valueConverter.ConvertFromProviderExpression.Body.Replace(valueConverter.ConvertFromProviderExpression.Parameters[0], Expression.Convert(param2, valueConverter.ProviderClrType));
                }

                getValue = Expression.Lambda<Func<object, object>>(Expression.Convert(getValueBody, typeof(object)), param);
                setValue = Expression.Lambda<Action<object, object>>(Expression.Assign(Expression.Property(cast, property.PropertyInfo), setValueBody), param, param2);
            }

            var targetType = property.ClrType;

            if (valueConverter != null)
            {
                targetType = valueConverter.ProviderClrType;
            }

            return new DelegateColumnSetup(index, property.GetColumnName(storeIdentifier.Value), targetType, getValue.Compile(), setValue.Compile(), direction);
        }

        private ValueDirection GetValueDirection(IProperty property, EntityState state)
        {
            if (state == EntityState.Added)
            {
                return GetValudDirection(property, property.GetBeforeSaveBehavior(), ValueGenerated.OnAdd);
            }
            else if (state == EntityState.Modified)
            {
                return GetValudDirection(property, property.GetAfterSaveBehavior(), ValueGenerated.OnUpdate);
            }
            else if (state == EntityState.Deleted)
            {
                return property.IsPrimaryKey() ? ValueDirection.Write : ValueDirection.None;
            }
            throw new NotSupportedException($"The entity state {state} can not be processed!");
        }

        private Expression ProcessDefaultValue(Expression getValueBody, IProperty property)
        {
            Expression defaultValueExpression = null;
            var defaultValue = property.GetDefaultValue();
            if (defaultValue != null)
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type.GetTypeInfo().IsEnum)
                {
                    defaultValue = Enum.ToObject(type, defaultValue);
                }
                defaultValueExpression = Expression.Constant(defaultValue, property.ClrType);
            }
            else
            {
                var factory = property.GetValueGeneratorFactory();

                if (factory != null)
                {
                    throw new NotSupportedException("Using ValueGeneratorFactories is currently not supported when using the DbContext bulk extension methods.");
                }
            }

            if (defaultValueExpression != null)
            {
                return Expression.Condition(
                    Expression.Equal(getValueBody, Expression.Default(property.ClrType)),
                    defaultValueExpression,
                    getValueBody);
            }
            return getValueBody;
        }
    }
}