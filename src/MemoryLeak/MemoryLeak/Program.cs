using System;
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
                GCDurationProfilerCallback = GCDurationProfilerCallback,
            };

            // start tracker
            AllocationTracker<GcStats>.Current.Start();
            ThreadingTracker<ThreadingStats>.Current.Start();
            ProfilerTracker.Current.Value.Start();
        }

        private static async Task GCDurationProfilerCallback(GCDurationResult value)
        {
            // send metrics to datadog or any favor you like.
            Console.WriteLine($"GC Gen {value.Generation}; Index {value.Index}; Duration {value.DurationMillsec}ms; Reason {value.Reason};");
        }
    }
}
