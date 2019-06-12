using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{
    public static class SqlServerBulkDbContextOptionsExtensions
    {

        public static SqlServerDbContextOptionsBuilder AddBulk(this SqlServerDbContextOptionsBuilder builder, Action<SqlServerBulkOptions> configure = null)
        {

            ((IRelationalDbContextOptionsBuilderInfrastructure)builder).OptionsBuilder.AddBulk(configure);
            return builder;
        }

        private static DbContextOptionsBuilder AddBulk(this DbContextOptionsBuilder optionsBuilder, Action<SqlServerBulkOptions> configure = null)
        {
            var extension = GetOrCreateExtension(optionsBuilder, p => { configure?.Invoke(p); return p; });
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        private static SqlServerBulkOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder, Func<SqlServerBulkOptions, SqlServerBulkOptions> factory)
            => optionsBuilder.Options.FindExtension<SqlServerBulkOptionsExtension>() ?? new SqlServerBulkOptionsExtension(factory(new SqlServerBulkOptions()));
    }
}