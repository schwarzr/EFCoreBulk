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
        private readonly ImmutableList<IColumnSetup> _columns;
        private readonly bool _propagateValues;

        public EntityMetadataColumnSetupProvider(IEntityType entity, bool propagateValues, EntityState state, IShadowPropertyAccessor shadowPropertyAccessor)
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

            if (shadowPropertyAccessor == null)
            {
                properties = properties.Where(p => entity.Relational().DiscriminatorProperty == p || !p.IsShadowProperty);
            }

            var columns = properties.Select((p, i) => CreateColumnSetup(entity, p, i, propagateValues, state, shadowPropertyAccessor)).Where(p => p.ValueDirection != ValueDirection.None);

            if (!propagateValues)
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

#if(NETSTANDARD2_0)
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
#else

        private static ValueDirection GetValudDirection(IProperty property, ValueGenerated generatorFlag)
        {
            if (property.IsStoreGeneratedAlways || (property.IsPrimaryKey() && property.ValueGenerated == generatorFlag))
            {
                return ValueDirection.Read;
            }
            else
            {
                var result = ValueDirection.Write;
                if (property.ValueGenerated == generatorFlag)
                {
                    result = result | ValueDirection.Read;
                }

                return result;
            }
        }

#endif

        private IColumnSetup CreateColumnSetup(IEntityType entity, IProperty property, int index, bool propagateValues, EntityState state, IShadowPropertyAccessor shadowPropertyAccessor)
        {
            var relational = entity.Relational();

            var direction = GetValueDirection(property, state);

            if (!propagateValues)
            {
                direction = direction & ~ValueDirection.Read;
            }

            if (relational.DiscriminatorProperty == property)
            {
                var discriminatorValue = relational.DiscriminatorValue;
                return new DelegateColumnSetup(index, property.Relational().ColumnName, property.ClrType, p => discriminatorValue, (p, q) => { }, ValueDirection.Write);
            }

            Expression<Func<object, object>> getValue = null;
            Expression<Action<object, object>> setValue = null;

            if (property.IsShadowProperty)
            {
                var accessorType = typeof(IShadowPropertyAccessor);

                var param = Expression.Parameter(typeof(object), "p");
                var param2 = Expression.Parameter(typeof(object), "q");

                Expression getValueBody = Expression.Convert(Expression.Call(
                        Expression.Constant(shadowPropertyAccessor, accessorType),
                        accessorType.GetRuntimeMethod("GetValue", new[] { typeof(object), typeof(string) }),
                        param,
                        Expression.Constant(property.Name)),
                        property.ClrType);

                getValueBody = ProcessDefaultValue(getValueBody, property);

                getValue = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(getValueBody, typeof(object)),
                    param);
                setValue = Expression.Lambda<Action<object, object>>(
                    Expression.Call(
                        Expression.Constant(shadowPropertyAccessor, accessorType),
                        accessorType.GetRuntimeMethod("StoreValue", new[] { typeof(object), typeof(string), typeof(object) }),
                        param,
                        Expression.Constant(property.Name), param2),
                    param,
                    param2);
            }
            else
            {
                var param = Expression.Parameter(typeof(object), "p");
                var param2 = Expression.Parameter(typeof(object), "q");

                var cast = Expression.Convert(param, property.DeclaringEntityType.ClrType);

                Expression getValueBody = Expression.Property(cast, property.PropertyInfo);
                getValueBody = ProcessDefaultValue(getValueBody, property);

                getValue = Expression.Lambda<Func<object, object>>(Expression.Convert(getValueBody, typeof(object)), param);
                setValue = Expression.Lambda<Action<object, object>>(Expression.Assign(Expression.Property(cast, property.PropertyInfo), Expression.Convert(param2, property.ClrType)), param, param2);
            }
            return new DelegateColumnSetup(index, property.Relational().ColumnName, property.ClrType, getValue.Compile(), setValue.Compile(), direction);
        }

        private ValueDirection GetValueDirection(IProperty property, EntityState state)
        {
#if (NETSTANDARD2_0)
            if (state == EntityState.Added)
            {
                return GetValudDirection(property, property.BeforeSaveBehavior, ValueGenerated.OnAdd);
            }
            else if (state == EntityState.Modified)
            {
                return GetValudDirection(property, property.AfterSaveBehavior, ValueGenerated.OnUpdate);
            }
            else if (state == EntityState.Deleted)
            {
                return property.IsPrimaryKey() ? ValueDirection.Write : ValueDirection.None;
            }
#else
            if (state == EntityState.Added)
            {
                return GetValudDirection(property, ValueGenerated.OnAdd);
            }
            else if (state == EntityState.Modified)
            {
                return GetValudDirection(property, ValueGenerated.OnAddOrUpdate);
            }
            else if (state == EntityState.Deleted)
            {
                return property.IsPrimaryKey() ? ValueDirection.Write : ValueDirection.None;
            }
#endif
            throw new NotSupportedException($"The entity state {state} can not be processed!");
        }

        private Expression ProcessDefaultValue(Expression getValueBody, IProperty property)
        {
            Expression defaultValueExpression = null;
            var defaultValue = property.Relational().DefaultValue;
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