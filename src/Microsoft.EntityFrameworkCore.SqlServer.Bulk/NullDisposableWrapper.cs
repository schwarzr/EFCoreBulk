using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public class NullDisposableWrapper<TTarget> : IDisposable
        where TTarget : IDisposable
    {
        public NullDisposableWrapper(TTarget target)
        {
            Target = target;
        }

        public TTarget Target { get; }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Target?.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}