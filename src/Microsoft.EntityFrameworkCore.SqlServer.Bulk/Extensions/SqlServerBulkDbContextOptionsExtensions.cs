using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{
    public static class SqlServerBulkDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder AddBulk(this DbContextOptionsBuilder optionsBuilder)
        {
            var extension = GetOrCreateExtension(optionsBuilder);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        private static SqlServerBulkOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.Options.FindExtension<SqlServerBulkOptionsExtension>() ?? new SqlServerBulkOptionsExtension();
    }
}