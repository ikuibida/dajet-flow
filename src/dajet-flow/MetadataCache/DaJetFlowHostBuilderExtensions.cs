using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DaJet.Flow
{
    public static class DaJetFlowHostBuilderExtensions
    {
        public static IHostBuilder UseDaJetMetadataCache(this IHostBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            return builder.ConfigureServices(ConfigureServices);
        }
        private static void ConfigureServices(HostBuilderContext _, IServiceCollection services)
        {
            services.AddHostedService<MetadataCacheService>();
            services.AddSingleton<IMetadataCache, MetadataCache>();
        }
    }
}