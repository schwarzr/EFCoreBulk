using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class ReflectionColumnSetupProvider : IColumnSetupProvider
    {
        private readonly Type _entityType;

        public ReflectionColumnSetupProvider(Type entityType)
        {
            _entityType = entityType;
        }

        public IEnumerable<IColumnSetup> Build()
        {
            int run = 0;
            foreach (var item in _entityType.GetRuntimeProperties().Where(p => p.CanRead).OrderBy(p => p.Name))
            {
                var param = Expression.Parameter(typeof(object));
                var param2 = Expression.Parameter(typeof(object));
                var lambdaGet = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Property(Expression.Convert(param, _entityType), item), typeof(object)), param);

                var lambdaSet = Expression.Lambda<Action<object, object>>(Expression.Assign(Expression.Property(Expression.Convert(param, _entityType), item), Expression.Convert(param2, item.PropertyType)), param, param2);

                yield return new DelegateColumnSetup(run, item.Name, item.PropertyType, lambdaGet.Compile(), lambdaSet.Compile());
                run++;
            }
        }
    }
}