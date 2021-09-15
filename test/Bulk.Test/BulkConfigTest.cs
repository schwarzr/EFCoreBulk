using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Bulk.Test.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk;
using Microsoft.EntityFrameworkCore.SqlServer.Bulk.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bulk.Test
{
    public class BulkConfigTest : DatabaseTest
    {
        [Fact]
        public async Task TestDisabledByDeafultAsync()
        {
            var prov = GetServiceProvider(p => p.DefaultDisabled());
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();

                var config = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetRequiredService<SqlServerBulkConfiguration>();

                Assert.True(config.Disabled);
            }
        }

        [Fact]
        public async Task TestEnabledByDeafultAsync()
        {
            var prov = GetServiceProvider(p => p.DefaultDisabled(false));
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();

                var config = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetRequiredService<SqlServerBulkConfiguration>();

                Assert.False(config.Disabled);
            }
        }

        [Fact]
        public async Task TestEnabledByDeafultNoconfigAsync()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();

                var config = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetRequiredService<SqlServerBulkConfiguration>();

                Assert.False(config.Disabled);
            }
        }


        [Fact]
        public async Task TestDisableAndEnableMethod()
        {
            var prov = GetServiceProvider();
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();
                var config = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetRequiredService<SqlServerBulkConfiguration>();
                Assert.False(config.Disabled);

                ctx.DisableBulk();

                Assert.True(config.Disabled);

                ctx.EnableBulk();

                Assert.False(config.Disabled);
            }
        }

        [Fact]
        public async Task TestEnableAndDisableMethod()
        {
            var prov = GetServiceProvider(p => p.DefaultDisabled(true));
            using ((IDisposable)prov)
            {
                var ctx = prov.GetService<TestContext>();
                var config = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetRequiredService<SqlServerBulkConfiguration>();
                Assert.True(config.Disabled);

                ctx.EnableBulk();

                Assert.False(config.Disabled);

                ctx.DisableBulk();

                Assert.True(config.Disabled);
            }
        }

        [Fact]
        public async Task TestEnableAndDisableWithMultipleInstancesMethod()
        {
            var prov = GetServiceProvider(p => p.DefaultDisabled(true));
            using ((IDisposable)prov)
            using (var scope = prov.CreateScope())
            {
                var ctx = prov.GetService<TestContext>();
                var config = ((IInfrastructure<IServiceProvider>)ctx).Instance.GetRequiredService<SqlServerBulkConfiguration>();

                var ctx2 = scope.ServiceProvider.GetService<TestContext>();
                var config2 = ((IInfrastructure<IServiceProvider>)ctx2).Instance.GetRequiredService<SqlServerBulkConfiguration>();

                Assert.True(config.Disabled);
                Assert.True(config2.Disabled);

                ctx.EnableBulk();

                Assert.False(config.Disabled);
                Assert.True(config2.Disabled);

                ctx.DisableBulk();

                Assert.True(config.Disabled);
                Assert.True(config2.Disabled);
            }
        }
    }
}