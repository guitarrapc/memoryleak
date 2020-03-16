using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DiagnosticCore.Statistics;
using StatsdClient;

namespace MemoryLeak
{
    public static class DatadogTracing
    {
        public static void EnableDatadog()
        {
            var dogstatsdConfig = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1", // udp for Windows Host
                    StatsdPort = 8125,
                }
                : new StatsdConfig
                {
                    StatsdServerName = "unix:///tmp/dsd.socket", // unix domain socket only work on Linux (Windows missing SocketType.Dgram)
                };
            DogStatsd.Configure(dogstatsdConfig);
        }

        /// list of event tags
        /// - clr_diagnostics_event.contention.startend_count"
        /// - clr_diagnostics_event.contention.startend_duration_ns"
        ///     contention_flag|managed|native
        /// - clr_diagnostics_event.gc.startend_count            // count
        /// - clr_diagnostics_event.gc.startend_duration_ms      // gauge
        ///     gc_gen:0|1|2
        ///     gc_type:blocking_outsite|background|blocking_during_gc
        ///     gc_reason:soh|induced|low_memory|empty|loh|oos_soh|oos_loh|incuded_non_forceblock
        /// - clr_diagnostics_event.gc.suspend_count             // count
        /// - clr_diagnostics_event.gc.suspend_duration_ms       // gauge
        ///     gc_suspend_reason:other|gc|appdomain_shudown|code_pitch|shutdown|debugger|prep_gc
        /// - clr_diagnostics_event.threadpool.available_workerthread_count // gauge
        /// - clr_diagnostics_event.threadpool.adjustment_avg_throughput // gauge
        /// - clr_diagnostics_event.threadpool.adjustment_new_workerthreads_count // gauge
        ///     threadpool_adjustment_reason:warmup|initializing|random_move|climbing_move|change_point|stabilizing|starvation|timeout

        // ContentionEvent
        public static void ContentionEventStartEnd(in ContentionEventStatistics statistics)
        {
            DogStatsd.Increment("clr_diagnostics_event.contention.startend_count", tags: new[] { $"contention_type:{statistics.Flag}" });
            DogStatsd.Gauge("clr_diagnostics_event.contention.startend_duration_ns", statistics.DurationNs, tags: new[] { $"contention_type:{statistics.Flag}" });
        }

        // GCEvent
        public static void GcEventStartEnd(in GCStartEndStatistics statistics)
        { 
            DogStatsd.Increment("clr_diagnostics_event.gc.startend_count", tags: new[] { $"gc_gen:{statistics.Generation}", $"gc_type:{statistics.Type}", $"gc_reason:{statistics.GetReasonString()}" });
            DogStatsd.Gauge("clr_diagnostics_event.gc.startend_duration_ms", statistics.DurationMillsec, tags: new[] { $"gc_gen:{statistics.Generation}", $"gc_type:{statistics.Type}", $"gc_reason:{statistics.GetReasonString()}" });
        }
        
        public static void GcEventSuspend(in GCSuspendStatistics statistics)
        {
            DogStatsd.Counter("clr_diagnostics_event.gc.suspend_object_count", statistics.Count, tags: new[] { $"gc_suspend_reason:{statistics.GetReasonString()}" });
            DogStatsd.Gauge("clr_diagnostics_event.gc.suspend_duration_ms", statistics.DurationMillisec, tags: new[] { $"gc_suspend_reason:{statistics.GetReasonString()}" });
        }

        // ThreadPoolEvent
        public static void ThreadPoolEventWorker(in ThreadPoolWorkerStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_event.threadpool.available_workerthread_count", statistics.ActiveWrokerThreads);
        }
        public static void ThreadPoolEventAdjustment(in ThreadPoolAdjustmentStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_event.threadpool.adjustment_avg_throughput", statistics.AverageThrouput, tags: new[] { $"thread_adjust_reason:{statistics.GetReasonString()}" });
            DogStatsd.Gauge("clr_diagnostics_event.threadpool.adjustment_new_workerthreads_count", statistics.NewWorkerThreads, tags: new[] { $"thread_adjust_reason:{statistics.GetReasonString()}" });
        }


        /// list of timer tags
        /// - clr_diagnostics_timer.gc.heap_size // gauge
        /// - clr_diagnostics_timer.gc.gen0_count // gauge
        /// - clr_diagnostics_timer.gc.gen1_count // gauge
        /// - clr_diagnostics_timer.gc.gen2_count // gauge
        /// - clr_diagnostics_timer.gc.gen0_size // gauge
        /// - clr_diagnostics_timer.gc.gen1_size // gauge
        /// - clr_diagnostics_timer.gc.gen2_size // gauge
        /// - clr_diagnostics_timer.gc.loh_size // gauge
        /// - clr_diagnostics_timer.gc.time_in_gc_percent // gauge
        /// - clr_diagnostics_timer.process.cpu // gauge
        /// - clr_diagnostics_timer.process.private_bytes // gauge
        /// - clr_diagnostics_timer.process.working_sets // gauge
        /// - clr_diagnostics_timer.thread.available_worker_threads // gauge
        /// - clr_diagnostics_timer.thread.max_worker_threads // gauge
        /// - clr_diagnostics_timer.thread.thread_count // gauge
        /// - clr_diagnostics_timer.thread.queue_length // gauge
        /// - clr_diagnostics_timer.thread.lock_contention_count // gauge
        /// - clr_diagnostics_timer.thread.completed_items_count // gauge
        /// - clr_diagnostics_timer.thread.available_completion_port_threads // gauge

        // GC
        public static void GcInfoTimerGauge(in GCInfoStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_timer.gc.heap_size_bytes", statistics.HeapSize);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_count", statistics.Gen0Count, tags: new[] { $"gc_gen:0" });
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_count", statistics.Gen1Count, tags: new[] { $"gc_gen:1" });
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_count", statistics.Gen2Count, tags: new[] { $"gc_gen:2" });
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.Gen0Size, tags: new[] { $"gc_gen:0" });
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.Gen1Size, tags: new[] { $"gc_gen:1" });
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.Gen2Size, tags: new[] { $"gc_gen:2" });
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.LohSize, tags: new[] { $"gc_gen:loh" });
            DogStatsd.Gauge("clr_diagnostics_timer.gc.time_in_gc_percent", statistics.TimeInGc);
        }

        // Process
        public static void ProcessInfoTimerGauge(in ProcessInfoStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_timer.process.cpu", statistics.Cpu);
            DogStatsd.Gauge("clr_diagnostics_timer.process.private_bytes", statistics.PrivateBytes);
            DogStatsd.Gauge("clr_diagnostics_timer.process.working_sets", statistics.WorkingSet);
        }

        // Thread
        public static void ThreadInfoTimerGauge(in ThreadInfoStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_timer.thread.available_worker_threads", statistics.AvailableWorkerThreads);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.max_worker_threads", statistics.MaxWorkerThreads);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.thread_count", statistics.ThreadCount);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.queue_length", statistics.QueueLength);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.lock_contention_count", statistics.LockContentionCount);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.completed_items_count", statistics.CompletedItemsCount);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.available_completion_port_threads", statistics.AvailableWorkerThreads);
        }

    }
}
