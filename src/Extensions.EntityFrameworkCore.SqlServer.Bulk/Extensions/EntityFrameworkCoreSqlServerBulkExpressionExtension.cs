using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq.Expressions
{
    public static class EntityFrameworkCoreSqlServerBulkExpressionExtension
    {
        public static Expression Replace(this Expression baseExpression, Expression search, Expression replace)
        {
            var visitor = new ReplaceExpressionVisitor(search, replace);

            return visitor.Visit(baseExpression);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private Expression _search;
            private Expression _replace;

            public ReplaceExpressionVisitor(Expression search, Expression replace)
            {
                _search = search;
                _replace = replace;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _search)
                {
                    return _replace;
                }
                return base.Visit(node);
            }
        }
    }
}
