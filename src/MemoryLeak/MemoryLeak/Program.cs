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
            };
            ProfilerTracker.Current.Value.Start();
        }

        private static async Task GCProfilerCallback(GCStatistics value)
        {
            // send metrics to datadog or any favor you like.
            Console.WriteLine($"GC Index {value.Index}; Gen {value.Generation}; Type {value.Type}; Duration {value.DurationMillsec}ms; Reason {value.Reason};");
        }
        private static async Task ThreadProfilerCallback(ThreadStatistics value)
        {
            if (value.Type == ThreadStatisticType.ThreadWorker)
            {
                Console.WriteLine($"Thread ActiveWrokerThreads {value.ThreadWorker.ActiveWrokerThreads}; RetiredWrokerThreads {value.ThreadWorker.RetiredWrokerThreads}; WorkerThreads {value.ThreadWorker.WorkerThreads}; CompletionPortThreads {value.ThreadWorker.CompletionPortThreads}");
                //Console.WriteLine($"ThreadInfo ThreadCount {ThreadPool.ThreadCount}; CompletedWorkItemCount {ThreadPool.CompletedWorkItemCount}; PendingWorkItemCount {ThreadPool.PendingWorkItemCount}");
            }
            else if (value.Type == ThreadStatisticType.ThreadAdjustment)
            {
                Console.WriteLine($"ThreadAdjustment Reason {value.ThreadAdjustment.Reason}; AverageThrouput {value.ThreadAdjustment.AverageThrouput};");
            }
            else if (value.Type == ThreadStatisticType.IOThread)
            {
                Console.WriteLine($"IOThreads {value.IOThread.Count}; RetiredIOThreads {value.IOThread.RetiredIOThreads};");
            }
        }
    }
}
