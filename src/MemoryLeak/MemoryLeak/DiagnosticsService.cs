using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore;
using DiagnosticCore.Statistics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryLeak
{
    public class DiagnosticsService : IHostedService
    {
        private readonly ILogger<DiagnosticsService> _logger;
        public DiagnosticsService(ILogger<DiagnosticsService> logger)
        {
            _logger = logger;
            ProfilerTracker.Options = new ProfilerTrackerOptions
            {
                GCEventCallback = (GCEventProfilerCallback, OnException),
                ThreadPoolEventCallback = (ThreadPoolEventProfilerCallback, OnException),
                ContentionEventCallback = (ContentionEventProfilerCallback, OnException),
                ThreadInfoTimerCallback = (ThreadInfoTimerCallback, OnException),
                GCInfoTimerCallback = (GCInfoTimerCallback, OnException),
                ProcessInfoTimerCallback = (ProcessInfoTimerCallback, OnException),
            };
        }
        private void OnException(Exception exception) => _logger.LogError(exception, "Error while diagnostics.");

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ProfilerTracker.Current.Value.Start();
            return Task.CompletedTask;   
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ProfilerTracker.Current.Value.Stop();
            return Task.CompletedTask;
        }

        /// <summary>
        /// GC Event
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task GCEventProfilerCallback(GCEventStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            if (arg.Type == GCEventType.GCStartEnd)
            {
                _logger.LogWarning($"GC StartEnd Reason {arg.GCStartEndStatistics.Reason}; Duration {arg.GCStartEndStatistics.DurationMillsec}ms; Index {arg.GCStartEndStatistics.Index}; Gen {arg.GCStartEndStatistics.Generation}; Type {arg.GCStartEndStatistics.Type};");
            }
            else if (arg.Type == GCEventType.GCSuspend)
            {
                _logger.LogWarning($"GC Suspend Reason {arg.GCSuspendStatistics.Reason}; Duration {arg.GCSuspendStatistics.DurationMillisec}ms; Count {arg.GCSuspendStatistics.Count};");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// ThreadPool Event
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task ThreadPoolEventProfilerCallback(ThreadPoolEventStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            if (arg.Type == ThreadPoolStatisticType.ThreadWorker)
            {
                _logger.LogWarning($"Thread ActiveWrokerThreads {arg.ThreadWorker.ActiveWrokerThreads};");
            }
            else if (arg.Type == ThreadPoolStatisticType.ThreadAdjustment)
            {
                _logger.LogWarning($"ThreadAdjustment Reason {arg.ThreadAdjustment.Reason}; NewWorkerThread {arg.ThreadAdjustment.NewWorkerThreads}; AverageThrouput {arg.ThreadAdjustment.AverageThrouput};");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Contention Event
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task ContentionEventProfilerCallback(ContentionEventStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            _logger.LogWarning($"Contention Flag {arg.Flag}; DurationNs {arg.DurationNs}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// ThreadInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task ThreadInfoTimerCallback(ThreadInfoStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            var usingWorkerThreads = arg.MaxWorkerThreads - arg.AvailableWorkerThreads;
            _logger.LogWarning($"ThreadInfo AvailableWorkerThreads {arg.AvailableWorkerThreads}; MaxWorkerThreads {arg.MaxWorkerThreads}; UsingWorkerThreads {usingWorkerThreads}; ThreadCount {arg.ThreadCount}; QueueLength {arg.QueueLength}; LockContentionCount {arg.LockContentionCount}; CompletedItemsCount {arg.CompletedItemsCount}; AvailableCompletionPortThreads {arg.AvailableCompletionPortThreads}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// GCInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task GCInfoTimerCallback(GCInfoStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            _logger.LogWarning($"GCInfo HeapSize {arg.HeapSize}; Gen0Count {arg.Gen0Count}; Gen1Count {arg.Gen1Count}; Gen2Count {arg.Gen2Count}; Gen0Size {arg.Gen0Size}; Gen1Size {arg.Gen1Size}; Gen2Size {arg.Gen2Size}; LohSize {arg.LohSize}; TimeInGc {arg.TimeInGc}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// ProcessInfo
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task ProcessInfoTimerCallback(ProcessInfoStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            _logger.LogWarning($"ProcessInfo Cpu {arg.Cpu}; PrivateBytes {arg.PrivateBytes}; WorkingSet {arg.WorkingSet};");
            return Task.CompletedTask;
        }
    }
}
