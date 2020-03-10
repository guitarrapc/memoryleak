using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore;
using DiagnosticCore.EventListeners;
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

            // start tracker
            AllocationTracker<GcStats>.Current.Start();
            ThreadingTracker<ThreadingStats>.Current.Start();

            ProfilerTracker.Options = new ProfilerTrackerOptions
            {
                CancellationToken = default,
                GCProfilerCallback = GCProfilerCallback,
                ThreadProfilerCallback = ThreadProfilerCallback,
                ContentionProfilerCallback = ContentionProfilerCallback,
            };
            ProfilerTracker.Current.Value.Start();
        }

        private static Task GCProfilerCallback(GCStatistics arg)
        {
            // send metrics to datadog or any favor you like.
            Console.WriteLine($"GC Index {arg.Index}; Gen {arg.Generation}; Type {arg.Type}; Duration {arg.DurationMillsec}ms; Reason {arg.Reason};");

            return Task.CompletedTask;
        }
        private static Task ThreadProfilerCallback(ThreadPoolStatistics arg)
        {
            if (arg.Type == ThreadPoolStatisticType.ThreadWorker)
            {
                Console.WriteLine($"Thread ActiveWrokerThreads {arg.ThreadWorker.ActiveWrokerThreads}; RetiredWrokerThreads {arg.ThreadWorker.RetiredWrokerThreads}; WorkerThreads {arg.ThreadWorker.WorkerThreads}; CompletionPortThreads {arg.ThreadWorker.CompletionPortThreads}");
                //Console.WriteLine($"ThreadInfo ThreadCount {ThreadPool.ThreadCount}; CompletedWorkItemCount {ThreadPool.CompletedWorkItemCount}; PendingWorkItemCount {ThreadPool.PendingWorkItemCount}");
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

        private static Task ContentionProfilerCallback(ContentionStatistics arg)
        {
            Console.WriteLine($"Contention Flag {arg.Flag}; DurationNs {arg.DurationNs}");

            return Task.CompletedTask;
        }
    }
}
