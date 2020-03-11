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
                GCEventProfilerCallback = GCEventProfilerCallback,
                ThreadPoolEventProfilerCallback = ThreadPoolEventProfilerCallback,
                ContentionEventProfilerCallback = ContentionEventProfilerCallback,
                ThreadInfoTimerCallback = ThreadInfoTimerCallback,
                GCInfoTimerCallback = GCInfoTimerCallback,
                ProcessInfoTimerCallback = ProcessInfoTimerCallback,
            };
            ProfilerTracker.Current.Value.Start();
        }

        /// <summary>
        /// GC
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task GCEventProfilerCallback(GCEventStatistics arg)
        {
            // send metrics to datadog or any favor you like.
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
        /// Thread
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ThreadPoolEventProfilerCallback(ThreadPoolEventStatistics arg)
        {
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
        /// Contention
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ContentionEventProfilerCallback(ContentionEventStatistics arg)
        {
            //Console.WriteLine($"Contention Flag {arg.Flag}; DurationNs {arg.DurationNs}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// ThreadInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static Task ThreadInfoTimerCallback(ThreadInfoStatistics arg)
        {
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
            Console.WriteLine($"ProcessInfo Cpu {arg.Cpu}; PrivateBytes {arg.PrivateBytes}; WorkingSet {arg.WorkingSet};");
            return Task.CompletedTask;
        }
    }
}
