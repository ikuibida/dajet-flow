using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Runtime.Loader;

namespace DaJet.Flow.App
{
    public static class Program
    {
        public static void Main()
        {
            AssemblyLoadContext.Default.Resolving += ResolveAssembly;

            CreateHostBuilder().Build().Run();
        }
        private static Assembly ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string probeSetting = "bin;bin\\datamappers";
            
            foreach (string subdirectory in probeSetting.Split(';'))
            {
                string pathMaybe = Path.Combine(AppContext.BaseDirectory, subdirectory, $"{assemblyName.Name}.dll");
                
                if (File.Exists(pathMaybe))
                {
                    return context.LoadFromAssemblyPath(pathMaybe);
                }
            }

            return null; // assembly is not found
        }
        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseSystemd()
                .UseWindowsService()
                .ConfigureAppConfiguration(config =>
                {
                    config.Sources.Clear();
                    config.AddJsonFile("appsettings.json", optional: false);
                })
                .ConfigureServices(ConfigureServices);
        }
        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            AppSettings settings = new AppSettings();
            context.Configuration.Bind(settings);

            services
                .AddOptions()
                .AddSingleton(Options.Create(settings))
                .Configure<HostOptions>(context.Configuration.GetSection(nameof(HostOptions)));

            services.AddSingleton<PipelineBuilder>();
            services.AddTransient(typeof(Pipeline<>));
            services.AddTransient(typeof(SqlServer.Consumer<>));
            services.AddTransient(typeof(SqlServer.Producer<>));
            services.AddTransient(typeof(PostgreSQL.Consumer<>));
            services.AddTransient(typeof(PostgreSQL.Producer<>));

            services.AddSingleton<Contracts.Transformers.V1.OutgoingIncomingTransformer>();

            foreach (PipelineOptions options in settings.Pipelines)
            {
                services.AddSingleton<IHostedService>(serviceProvider =>
                {
                    ILogger<DaJetFlowService> logger = serviceProvider.GetRequiredService<ILogger<DaJetFlowService>>();

                    PipelineBuilder builder = serviceProvider.GetRequiredService<PipelineBuilder>();

                    IPipeline pipeline = builder.Build(options);

                    return new DaJetFlowService(pipeline, logger);
                });
            }
        }
    }
}