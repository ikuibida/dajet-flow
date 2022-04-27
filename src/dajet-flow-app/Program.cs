using DaJet.Flow.Data;
using DaJet.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            // TODO: IHostBuilder.UseDaJetMetadataCache();
        }
        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            AppSettings settings = new AppSettings();
            context.Configuration.Bind(settings);

            services
                .AddOptions()
                .AddSingleton(Options.Create(settings))
                .Configure<HostOptions>(context.Configuration.GetSection(nameof(HostOptions)));

            services.AddTransient<IMetadataService, MetadataService>(); // TODO: IHostBuilder.UseDaJetMetadataCache();

            services.AddTransient<PipelineServiceProvider>();
            services.AddTransient<IPipelineBuilder, PipelineBuilder>();

            services.AddTransient(typeof(Pipeline<>));
            services.AddSingleton<DataMapperOptionsBuilder>();

            services.AddTransient(typeof(RabbitMQ.Consumer));
            services.AddTransient(typeof(RabbitMQ.Producer));
            services.AddTransient(typeof(SqlServer.Consumer<>));
            services.AddTransient(typeof(SqlServer.Producer<>));
            services.AddTransient(typeof(PostgreSQL.Consumer<>));
            services.AddTransient(typeof(PostgreSQL.Producer<>));

            services.AddTransient<SqlServer.DataMapperFactory>();
            services.AddTransient<SqlServer.DataMappers.OutgoingMessageDataMapper>();
            services.AddTransient<SqlServer.DataMappers.IncomingMessageDataMapper>();
            services.AddTransient<PostgreSQL.DataMapperFactory>();
            services.AddTransient<PostgreSQL.DataMappers.OutgoingMessageDataMapper>();
            services.AddTransient<PostgreSQL.DataMappers.IncomingMessageDataMapper>();

            services.AddSingleton<RabbitMQ.DbToRmqTransformer>();
            services.AddSingleton<RabbitMQ.RmqToDbTransformer>();
            services.AddSingleton<Transformers.OutgoingIncomingTransformer>();

            foreach (PipelineOptions options in settings.Pipelines)
            {
                if (!options.IsActive)
                {
                    continue;
                }

                services.AddSingleton(serviceProvider =>
                {
                    IPipeline pipeline = serviceProvider.GetRequiredService<IPipelineBuilder>().Configure(options).Build();

                    object service = ActivatorUtilities.CreateInstance(serviceProvider, typeof(DaJetFlowService), pipeline);
                    
                    return (service as IHostedService)!;
                });
            }
        }
    }
}