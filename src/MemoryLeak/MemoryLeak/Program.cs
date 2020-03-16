using System;
using System.Threading.Tasks;
using DiagnosticCore;
using DiagnosticCore.Statistics;
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
            EnableTracker();
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

        private static void EnableTracker()
        {
            //ThreadPool.SetMinThreads(Environment.ProcessorCount * 1000, 1000);

            // InProcess tracker
            ProfilerTracker.Options = new ProfilerTrackerOptions
            {
                GCEventCallback = (GCEventProfilerCallback, ex => { }),
                ThreadPoolEventCallback = (ThreadPoolEventProfilerCallback, ex => { }),
                ContentionEventCallback = (ContentionEventProfilerCallback, ex => { }),
                ThreadInfoTimerCallback = (ThreadInfoTimerCallback, ex => { }),
                GCInfoTimerCallback = (GCInfoTimerCallback, ex => { }),
                ProcessInfoTimerCallback = (ProcessInfoTimerCallback, ex => { }),
            };
            ProfilerTracker.Current.Value.Start();
        }

        /// <summary>
        /// GC Event
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task GCEventProfilerCallback(GCEventStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            if (arg.Type == GCEventType.GCStartEnd)
            {
                Console.WriteLine($"GC StartEnd Reason {arg.GCStartEndStatistics.Reason}; Duration {arg.GCStartEndStatistics.DurationMillsec}ms; Index {arg.GCStartEndStatistics.Index}; Gen {arg.GCStartEndStatistics.Generation}; Type {arg.GCStartEndStatistics.Type};");
            }
            else if (arg.Type == GCEventType.GCSuspend)
            {
                Console.WriteLine($"GC Suspend Reason {arg.GCSuspendStatistics.Reason}; Duration {arg.GCSuspendStatistics.DurationMillisec}ms; Count {arg.GCSuspendStatistics.Count};");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// ThreadPool Event
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ThreadPoolEventProfilerCallback(ThreadPoolEventStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            if (arg.Type == ThreadPoolStatisticType.ThreadWorker)
            {
                Console.WriteLine($"Thread ActiveWrokerThreads {arg.ThreadWorker.ActiveWrokerThreads};");
            }
            else if (arg.Type == ThreadPoolStatisticType.ThreadAdjustment)
            {
                Console.WriteLine($"ThreadAdjustment Reason {arg.ThreadAdjustment.Reason}; NewWorkerThread {arg.ThreadAdjustment.NewWorkerThreads}; AverageThrouput {arg.ThreadAdjustment.AverageThrouput};");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Contention Event
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ContentionEventProfilerCallback(ContentionEventStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            Console.WriteLine($"Contention Flag {arg.Flag}; DurationNs {arg.DurationNs}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// ThreadInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ThreadInfoTimerCallback(ThreadInfoStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            var usingWorkerThreads = arg.MaxWorkerThreads - arg.AvailableWorkerThreads;
            Console.WriteLine($"ThreadInfo AvailableWorkerThreads {arg.AvailableWorkerThreads}; MaxWorkerThreads {arg.MaxWorkerThreads}; UsingWorkerThreads {usingWorkerThreads}; ThreadCount {arg.ThreadCount}; QueueLength {arg.QueueLength}; LockContentionCount {arg.LockContentionCount}; CompletedItemsCount {arg.CompletedItemsCount}; AvailableCompletionPortThreads {arg.AvailableCompletionPortThreads}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// GCInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task GCInfoTimerCallback(GCInfoStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            Console.WriteLine($"GCInfo HeapSize {arg.HeapSize}; Gen0Count {arg.Gen0Count}; Gen1Count {arg.Gen1Count}; Gen2Count {arg.Gen2Count}; Gen0Size {arg.Gen0Size}; Gen1Size {arg.Gen1Size}; Gen2Size {arg.Gen2Size}; LohSize {arg.LohSize}; TimeInGc {arg.TimeInGc}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// ProcessInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ProcessInfoTimerCallback(ProcessInfoStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            Console.WriteLine($"ProcessInfo Cpu {arg.Cpu}; PrivateBytes {arg.PrivateBytes}; WorkingSet {arg.WorkingSet};");
            return Task.CompletedTask;
        }
    }
}
