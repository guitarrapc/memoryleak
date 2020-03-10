using DiagnosticCore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryLeak.Controllers
{
//[System.Runtime]
//    # of Assemblies Loaded                           162
//    % Time in GC (since last GC)                       0
//    Allocation Rate (Bytes / sec)                 16,304
//    CPU Usage (%)                                      0
//    Exceptions / sec                                   0
//    GC Heap Size (MB)                                358
//    Gen 0 GC / sec                                     0
//    Gen 0 Size (B)                                    24
//    Gen 1 GC / sec                                     0
//    Gen 1 Size (B)                               245,608
//    Gen 2 GC / sec                                     0
//    Gen 2 Size (B)                           480,561,360
//    LOH Size (B)                             204,070,320
//    Monitor Lock Contention Count / sec                0
//    Number of Active Timers                            3
//    Number of Completed Work Items / sec               2
//    ThreadPool Queue Length                            0
//    ThreadPool Threads Count                           3
//    Working Set (MB)                                  79

    [Route("api/[controller]")]
    [ApiController]
    public class CounterController : ControllerBase
    {
        private static Process _process = Process.GetCurrentProcess();
        private static TimeSpan _oldCPUTime = TimeSpan.Zero;
        private static DateTime _lastMonitorTime = DateTime.UtcNow;
        private static DateTime _lastRpsTime = DateTime.UtcNow;
        private static double _cpu = 0, _rps = 0;
        private static readonly double RefreshRate = TimeSpan.FromSeconds(1).TotalMilliseconds;
        public static long Requests = 0;

        [HttpGet("collect")]
        public ActionResult GetCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return Ok();
        }

        [HttpGet("current")]
        public ActionResult GetCurrent()
        {
            AllocationTracker<GcStats>.Current.Track();
            ThreadingTracker<ThreadingStats>.Current.Track();
            return Ok(new
            {
                GC = new
                {
                    AllocationTracker<GcStats>.Current.CurrentStat,
                    AllocationTracker<GcStats>.Current.DiffStat,
                },
                Threading = new
                {
                    ThreadingTracker<ThreadingStats>.Current.CurrentStat,
                    ThreadingTracker<ThreadingStats>.Current.DiffStat,
                },
            });
        }

        [HttpGet("startdumptracker")]
        public ActionResult StartDumpTracker()
        {
            ProfilerTracker.Current.Value.Restart();
            return Ok();
        }
        [HttpGet("stopdumptracker")]
        public ActionResult StopDumpTracker()
        {
            ProfilerTracker.Current.Value.Stop();
            return Ok();
        }

        [HttpGet("thread/{count}")]
        public async Task<ActionResult> StartThread(int count = 100_000)
        {
            // start Threads in threadpool
            var tasks = Enumerable.Range(1, count)
                        .Select(x => Task.Run(() => 1))
                        .ToArray();
            await Task.WhenAll(tasks); 
            return Ok();
        }

        [HttpGet("final")]
        public ActionResult GetFinal()
        {
            AllocationTracker<GcStats>.Current.Final();
            AllocationTracker<ThreadingStats>.Current.Final();
            return Ok();
        }

        [HttpGet("diagnostics")]
        public ActionResult<CounterMetrics> GetDiagnostics()
        {
            var now = DateTime.UtcNow;
            _process.Refresh();

            var cpuElapsedTime = now.Subtract(_lastMonitorTime).TotalMilliseconds;

            if (cpuElapsedTime > RefreshRate)
            {
                var newCPUTime = _process.TotalProcessorTime;
                var elapsedCPU = (newCPUTime - _oldCPUTime).TotalMilliseconds;
                _cpu = elapsedCPU * 100 / Environment.ProcessorCount / cpuElapsedTime;

                _lastMonitorTime = now;
                _oldCPUTime = newCPUTime;
            }

            var rpsElapsedTime = now.Subtract(_lastRpsTime).TotalMilliseconds;
            if (rpsElapsedTime > RefreshRate)
            {
                _rps = Requests * 1000 / rpsElapsedTime;
                Interlocked.Exchange(ref Requests, 0);
                _lastRpsTime = now;
            }

            var diagnostics = new CounterMetrics
            {
                PID = _process.Id,

                // The memory occupied by objects.
                Allocated = GC.GetTotalMemory(false),
                TotalAllocated = GC.GetTotalAllocatedBytes(false),

                // The working set includes both shared and private data. The shared data includes the pages that contain all the 
                // instructions that the process executes, including instructions in the process modules and the system libraries.
                WorkingSet = _process.WorkingSet64,

                // The value returned by this property represents the current size of memory used by the process, in bytes, that 
                // cannot be shared with other processes.
                PrivateBytes = _process.PrivateMemorySize64,

                // The number of generation 0 collections
                Gen0 = GC.CollectionCount(0),

                // The number of generation 1 collections
                Gen1 = GC.CollectionCount(1),

                // The number of generation 2 collections
                Gen2 = GC.CollectionCount(2),

                CPU = _cpu,

                RPS = _rps
            };

            return diagnostics;
        }

        public struct CounterMetrics
        {
            public int? PID { get; set; }
            public long? Allocated { get; set; }
            public long? TotalAllocated { get; set; }
            public long? WorkingSet { get; set; }
            public long? PrivateBytes { get; set; }
            public int? Gen0 { get; set; }
            public int? Gen1 { get; set; }
            public int? Gen2 { get; set; }
            public double? CPU { get; set; }
            public double? RPS { get; set; }

            public override bool Equals(object obj)
            {
                return obj is CounterMetrics other &&
                       PID == other.PID &&
                       Allocated == other.Allocated &&
                       WorkingSet == other.WorkingSet &&
                       PrivateBytes == other.PrivateBytes &&
                       Gen0 == other.Gen0 &&
                       Gen1 == other.Gen1 &&
                       Gen2 == other.Gen2 &&
                       CPU == other.CPU &&
                       RPS == other.RPS;
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(PID);
                hash.Add(Allocated);
                hash.Add(WorkingSet);
                hash.Add(PrivateBytes);
                hash.Add(Gen0);
                hash.Add(Gen1);
                hash.Add(Gen2);
                hash.Add(CPU);
                hash.Add(RPS);
                return hash.ToHashCode();
            }

            public static bool operator ==(CounterMetrics left, CounterMetrics right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(CounterMetrics left, CounterMetrics right)
            {
                return !(left == right);
            }
        }
    }
}
