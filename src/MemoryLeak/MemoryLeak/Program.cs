using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryLeak
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //ThreadPool.SetMinThreads(Environment.ProcessorCount * 1000, 1000);

            // if you want run Diagnostics without DI.
            //WithoutHostedService(args).Build().Run();

            // if you want run Diagnostics with IHostedService
            WithHostedService(args).Build().Run();
        }

        public static IHostBuilder WithHostedService(string[] args)
        {
            return CreateBuilderCustom(args);
        }

        public static IHostBuilder WithoutHostedService(string[] args)
        {
            // profiler
            var loggerFactory = LoggerFactory.Create(logging => logging.AddMyConsoleLogger());
            var diagnostics = new Diagnostics(loggerFactory);
            diagnostics.StartTracker();

            return CreateBuilderDefault(args);
        }

        public static IHostBuilder CreateBuilderCustom(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddMyConsoleLogger();
                })
                .ConfigureServices((context, services) => services.AddHostedService<DiagnosticsService>())
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());

        public static IHostBuilder CreateBuilderDefault(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
