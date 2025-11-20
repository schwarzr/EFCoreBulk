using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class NullDisposableWrapper<TTarget> : IDisposable
        where TTarget : IDisposable
    {
        private bool _disposedValue = false;

        public NullDisposableWrapper(TTarget target)
        {
            Target = target;
        }

        public TTarget Target { get; }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Target?.Dispose();
                }

                _disposedValue = true;
            }
        }
    }
}