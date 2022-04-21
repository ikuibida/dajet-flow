using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Runtime.Loader;

namespace DaJet.Flow.App
{
    public static class Program
    {
        public static void Main()
        {
            //AssemblyLoadContext.Default.Resolving += ResolveAssembly;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.File("C:\\temp\\dajet-flow\\dajet-flow.log", fileSizeLimitBytes: 1048576, rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Host is running");

                CreateHostBuilder().Build().Run();

                Log.Information("Host is stopped");
            }
            catch (Exception error)
            {
                Log.Fatal(error, "Failed to start host");
            }
            finally
            {
                Log.CloseAndFlush();
            }
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
                .ConfigureServices(ConfigureServices)
                .UseSerilog();
                // TODO: .UseDaJetMetadataCache();
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

            services.AddTransient(typeof(RabbitMQ.Consumer));
            services.AddTransient(typeof(RabbitMQ.Producer));
            services.AddTransient(typeof(SqlServer.Consumer<>));
            services.AddTransient(typeof(SqlServer.Producer<>));
            services.AddTransient(typeof(PostgreSQL.Consumer<>));
            services.AddTransient(typeof(PostgreSQL.Producer<>));

            services.AddSingleton<RabbitMQ.DbToRmqTransformer>();
            services.AddSingleton<RabbitMQ.RmqToDbTransformer>();
            services.AddSingleton<Transformers.OutgoingIncomingTransformer>();

            foreach (PipelineOptions options in settings.Pipelines)
            {
                if (!options.IsActive)
                {
                    continue;
                }

                services.AddSingleton<IHostedService>(serviceProvider =>
                {
                    ILogger<DaJetFlowService> logger = serviceProvider.GetRequiredService<ILogger<DaJetFlowService>>();

                    PipelineBuilder builder = serviceProvider.GetRequiredService<PipelineBuilder>();

                    IPipeline pipeline = builder.Build(options, out List<string> errors);

                    if (pipeline == null)
                    {
                        foreach (string error in errors)
                        {
                            logger.LogError(error);
                        }
                    }
                    else
                    {
                        logger.LogInformation($"Pipeline [{pipeline.Name}] is built.");
                    }

                    return new DaJetFlowService(pipeline, logger);
                });
            }
        }
    }
}