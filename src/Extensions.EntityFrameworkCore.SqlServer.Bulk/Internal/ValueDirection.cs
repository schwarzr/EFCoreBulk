using System;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    [Flags]
    public enum ValueDirection
    {
        None = 0x00,
        Write = 0x01,
        Read = 0x02,
        Both = Read | Write
    }
}