using Microsoft.EntityFrameworkCore.SqlServer.Bulk;

namespace System
{
    public static class EntityFrameworkCoreSqlServerBulkIDisposableExtensions
    {
        public static NullDisposableWrapper<TTarget> NullDisposable<TTarget>(this TTarget target)
            where TTarget : IDisposable
        {
            return new NullDisposableWrapper<TTarget>(target);
        }
    }
}