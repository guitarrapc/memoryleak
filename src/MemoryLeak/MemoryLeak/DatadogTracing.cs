﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Text;
using DiagnosticCore.Statistics;
using StatsdClient;

namespace MemoryLeak
{
    public static class DatadogTracing
    {
        private static readonly ConcurrentDictionary<string, string[]> tagCache = new ConcurrentDictionary<string, string[]>();
        private static readonly string[] commonTags;

        /// <summary>
        /// static constructor
        /// </summary>
        static DatadogTracing()
        {
            // generate base tag for Kubernetes
            if (Pripod.Pod.Current.IsRunningOnKubernetes)
            {
                var kubeNs = Pripod.Pod.Current.Namespace; // kube_namespace
                var kubePod = Pripod.Pod.Current.Name; // pod_name
                var kubeNode = Pripod.Pod.Current.NodeName; // node
                var kubeStatefulset = Pripod.Pod.Current.StatefulSet.Name; // kube_stateful_set
                Pripod.Pod.Current.Labels.TryGetValue("app", out var kubeApp); // kube_app

                // get your annotation values
                var annotationsKey = "myapp.io";
                Pripod.Pod.Current.Annotations.TryGetValue($"{annotationsKey}/build-id", out var kubeBuildId); // build_id
                Pripod.Pod.Current.Annotations.TryGetValue($"{annotationsKey}/git-branch", out var kubeGitBranch); // git_branch
                Pripod.Pod.Current.Annotations.TryGetValue($"{annotationsKey}/git-hash", out var kubeGitHash); // git_hash

                commonTags = new[]
                {
                    ZString.Concat("is_kubernetes:", Pripod.Pod.Current.IsRunningOnKubernetes),
                    ZString.Concat("kube_namespace:", kubeNs),
                    ZString.Concat("pod_name:", kubePod),
                    ZString.Concat("node:", kubeNode),
                    ZString.Concat("kube_stateful_set:", kubeStatefulset),
                    ZString.Concat("kube_app:", kubeApp),
                    ZString.Concat("build_id:", kubeBuildId),
                    ZString.Concat("git_branch:", kubeGitBranch),
                    ZString.Concat("git_hash:", kubeGitHash),
                };
            }
            else
            {
                commonTags = new[]
                {
                    ZString.Concat("is_kubernetes:", Pripod.Pod.Current.IsRunningOnKubernetes),
                };
            }
        }
        public static void EnableDatadog(string statsdServerAddress, int statsdServerPort)
        {
            // always use udp port
            var dogstatsdConfig = new StatsdConfig
            {
                StatsdServerName = statsdServerAddress,
                StatsdPort = statsdServerPort,
            };
            DogStatsd.Configure(dogstatsdConfig);
        }

        private static string[] AddCommonTags(this IEnumerable<string> source)
        {
            return source.Concat(commonTags).ToArray();
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
            var key = ZString.Concat("contention_type:", statistics.Flag);
            var tags = tagCache.GetOrAdd(key, key => new[] { key }.AddCommonTags());
            DogStatsd.Increment("clr_diagnostics_event.contention.startend_count", tags: tags);
            DogStatsd.Gauge("clr_diagnostics_event.contention.startend_duration_ns", statistics.DurationNs, tags: tags);
        }

        // GCEvent
        public static void GcEventStartEnd(in GCStartEndStatistics statistics)
        {
            var key = ZString.Concat("gc_gen:", statistics.Generation, statistics.Type, statistics.Reason);
            var tags = tagCache.GetOrAdd(key, (key, stat) => new[] { ZString.Concat($"gc_gen:", stat.Generation), ZString.Concat("gc_type:", stat.Type), ZString.Concat("gc_reason:", stat.GetReasonString()) }.AddCommonTags(), statistics);
            DogStatsd.Increment("clr_diagnostics_event.gc.startend_count", tags: tags);
            DogStatsd.Gauge("clr_diagnostics_event.gc.startend_duration_ms", statistics.DurationMillsec, tags: tags);
        }

        public static void GcEventSuspend(in GCSuspendStatistics statistics)
        {
            var key = ZString.Concat("gc_suspend:", statistics.Reason);
            var tags = tagCache.GetOrAdd(key, (key, stat) => new[] { ZString.Concat("gc_suspend_reason:", stat.GetReasonString()) }.AddCommonTags(), statistics);
            DogStatsd.Counter("clr_diagnostics_event.gc.suspend_object_count", statistics.Count, tags: tags);
            DogStatsd.Gauge("clr_diagnostics_event.gc.suspend_duration_ms", statistics.DurationMillisec, tags: tags);
        }

        // ThreadPoolEvent
        public static void ThreadPoolEventWorker(in ThreadPoolWorkerStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_event.threadpool.available_workerthread_count", statistics.ActiveWrokerThreads, tags: commonTags);
        }
        public static void ThreadPoolEventAdjustment(in ThreadPoolAdjustmentStatistics statistics)
        {
            var key = ZString.Concat("thread_adjust_reason", statistics.Reason);
            var tags = tagCache.GetOrAdd(key, (key, stat) => new[] { ZString.Concat("thread_adjust_reason:", stat.GetReasonString()) }.AddCommonTags(), statistics);
            DogStatsd.Gauge("clr_diagnostics_event.threadpool.adjustment_avg_throughput", statistics.AverageThrouput, tags: tags);
            DogStatsd.Gauge("clr_diagnostics_event.threadpool.adjustment_new_workerthreads_count", statistics.NewWorkerThreads, tags: tags);
        }
        public static void ThreadPoolStarvationEventAdjustment(in ThreadPoolAdjustmentStatistics statistics)
        {
            DogStatsd.Event("ThreadPool Starvation detected", ".NET CLR automatically expanding ThreadPool, but this results slow down system. Watch out for error increase and take action to expan thread pool in advance.", alertType: "warning", aggregationKey: "host", tags: commonTags);
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
            var baseTagkey = ZString.Concat("gc_mode", statistics.GCMode, statistics.LatencyMode, statistics.CompactionMode);
            var baseTag = tagCache.GetOrAdd(baseTagkey, (key, stat) => new[] {
                ZString.Concat("gc_mode:", stat.GetGCModeString()),
                ZString.Concat("latency_mode:", stat.GetLatencyModeString()),
                ZString.Concat("compaction_mode:", stat.GetCompactionModeString())
            }.AddCommonTags(), statistics);
            var gen0Tags = tagCache.GetOrAdd(ZString.Concat("gen0", baseTagkey), key => baseTag.Prepend($"gc_gen:0").ToArray());
            var gen1Tags = tagCache.GetOrAdd(ZString.Concat("gen1", baseTagkey), key => baseTag.Prepend($"gc_gen:1").ToArray());
            var gen2Tags = tagCache.GetOrAdd(ZString.Concat("gen2", baseTagkey), key => baseTag.Prepend($"gc_gen:2").ToArray());
            var genLohTags = tagCache.GetOrAdd(ZString.Concat("genLoh", baseTagkey), key => baseTag.Prepend($"gc_gen:loh").ToArray());

            DogStatsd.Gauge("clr_diagnostics_timer.gc.heap_size_bytes", statistics.HeapSize, tags: baseTag);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_count", statistics.Gen0Count, tags: gen0Tags);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_count", statistics.Gen1Count, tags: gen1Tags);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_count", statistics.Gen2Count, tags: gen2Tags);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.Gen0Size, tags: gen0Tags);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.Gen1Size, tags: gen1Tags);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.Gen2Size, tags: gen2Tags);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.gc_size", statistics.LohSize, tags: genLohTags);
            DogStatsd.Gauge("clr_diagnostics_timer.gc.time_in_gc_percent", statistics.TimeInGc, tags: baseTag);
        }

        // Process
        public static void ProcessInfoTimerGauge(in ProcessInfoStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_timer.process.cpu", statistics.Cpu, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.process.private_bytes", statistics.PrivateBytes, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.process.working_sets", statistics.WorkingSet, tags: commonTags);
        }

        // Thread
        public static void ThreadInfoTimerGauge(in ThreadInfoStatistics statistics)
        {
            DogStatsd.Gauge("clr_diagnostics_timer.thread.available_worker_threads", statistics.AvailableWorkerThreads, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.max_worker_threads", statistics.MaxWorkerThreads, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.thread_count", statistics.ThreadCount, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.queue_length", statistics.QueueLength, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.lock_contention_count", statistics.LockContentionCount, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.completed_items_count", statistics.CompletedItemsCount, tags: commonTags);
            DogStatsd.Gauge("clr_diagnostics_timer.thread.available_completion_port_threads", statistics.AvailableWorkerThreads, tags: commonTags);
        }
    }
}
