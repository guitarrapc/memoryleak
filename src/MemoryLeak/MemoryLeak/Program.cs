﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ProfilerTracker.Options = new ProfilerTrackerOptions
            {
                CancellationToken = default,
                GCProfilerCallback = GCProfilerCallback,
                ThreadProfilerCallback = ThreadProfilerCallback,
            };

            // start tracker
            AllocationTracker<GcStats>.Current.Start();
            ThreadingTracker<ThreadingStats>.Current.Start();
            ProfilerTracker.Current.Value.Start();
        }

        private static async Task GCProfilerCallback(GCStatistics value)
        {
            // send metrics to datadog or any favor you like.
            Console.WriteLine($"GC Index {value.Index}; Gen {value.Generation}; Type {value.Type}; Duration {value.DurationMillsec}ms; Reason {value.Reason};");
        }
        private static async Task ThreadProfilerCallback(ThreadStatistics value)
        {
            Console.WriteLine($"Thread ActiveWrokerThreadCount {value.ActiveWrokerThreadCount}; RetiredWrokerThreadCount {value.RetiredWrokerThreadCount};");
        }
    }
}
