using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore;
using DiagnosticCore.Oop;
using DiagnosticCore.Statistics;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryLeak
{
    public class Program
    {
        public static void Main(string[] args)
        {
            EnableTracker();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());

        private static void EnableTracker()
        {
            //ThreadPool.SetMinThreads(Environment.ProcessorCount * 1000, 1000);

            // OutofProcess tracker
            AllocationTracker<GcStats>.Current.Start();
            ThreadingTracker<ThreadingStats>.Current.Start();

            // InProcess tracker
            ProfilerTracker.Options = new ProfilerTrackerOptions
            {
                CancellationToken = default,
                GCProfilerCallback = GCProfilerCallback,
                ThreadProfilerCallback = ThreadProfilerCallback,
                ContentionProfilerCallback = ContentionProfilerCallback,
                TimerThreadInfoCallback = TimerThreadInfoCallback,
            };
            ProfilerTracker.Current.Value.Start();
        }

        // TODO: Heap memory

        /// <summary>
        /// GC
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task GCProfilerCallback(EtwGCStatistics arg)
        {
            // send metrics to datadog or any favor you like.
            Console.WriteLine($"GC Index {arg.Index}; Gen {arg.Generation}; Type {arg.Type}; Duration {arg.DurationMillsec}ms; Reason {arg.Reason};");
            return Task.CompletedTask;
        }
        /// <summary>
        /// Thread
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ThreadProfilerCallback(EtwThreadPoolStatistics arg)
        {
            if (arg.Type == ThreadPoolStatisticType.ThreadWorker)
            {
                Console.WriteLine($"Thread ActiveWrokerThreads {arg.ThreadWorker.ActiveWrokerThreads}; RetiredWrokerThreads {arg.ThreadWorker.RetiredWrokerThreads};");
            }
            else if (arg.Type == ThreadPoolStatisticType.ThreadAdjustment)
            {
                Console.WriteLine($"ThreadAdjustment Reason {arg.ThreadAdjustment.Reason}; AverageThrouput {arg.ThreadAdjustment.AverageThrouput};");
            }
            else if (arg.Type == ThreadPoolStatisticType.IOThread)
            {
                Console.WriteLine($"IOThreads {arg.IOThread.Count}; RetiredIOThreads {arg.IOThread.RetiredIOThreads};");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Contention
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ContentionProfilerCallback(EtwContentionStatistics arg)
        {
            Console.WriteLine($"Contention Flag {arg.Flag}; DurationNs {arg.DurationNs}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// ThreadInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task TimerThreadInfoCallback(TimerThreadInfoStatistics arg)
        {
            Console.WriteLine($"ThreadInfo AvailableWorkerThreads {arg.AvailableWorkerThreads}; MaxWorkerThreads {arg.MaxWorkerThreads};");
            return Task.CompletedTask;
        }
    }
}
