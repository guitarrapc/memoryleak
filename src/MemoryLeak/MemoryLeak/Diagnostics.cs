﻿using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DiagnosticCore;
using DiagnosticCore.Statistics;
using Microsoft.Extensions.Logging;
using StatsdClient;

namespace MemoryLeak
{
    public class Diagnostics
    {
        private readonly ILogger<Diagnostics> _logger;
        public Diagnostics(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Diagnostics>();

            // Enable Datadog before tracker.
            DatadogTracing.EnableDatadog();

            // Enable Inprocess Tracker
            EnableTracker();
        }

        private void EnableTracker()
        {
            // InProcess tracker
            ProfilerTracker.Options = new ProfilerTrackerOptions
            {
                ContentionEventCallback = (ContentionEventProfilerCallback, OnException),
                GCEventCallback = (GCEventProfilerCallback, OnException),
                ThreadPoolEventCallback = (ThreadPoolEventProfilerCallback, OnException),
                GCInfoTimerCallback = (GCInfoTimerCallback, OnException),
                ProcessInfoTimerCallback = (ProcessInfoTimerCallback, OnException),
                ThreadInfoTimerCallback = (ThreadInfoTimerCallback, OnException),
            };
        }
        public void StartTracker()
        {
            ProfilerTracker.Current.Value.Start();
        }
        public void StopTracker()
        {
            ProfilerTracker.Current.Value.Stop();
        }
        private void OnException(Exception exception) => _logger.LogError(exception, "Error while diagnostics.");

        /// <summary>
        /// Contention Event
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task ContentionEventProfilerCallback(ContentionEventStatistics arg)
        {
            // memo: send metrics to datadog or any favor you like.
            DatadogTracing.ContentionEventStartEnd(arg);
            _logger.LogInformation($"Contention Flag {arg.Flag}; DurationNs {arg.DurationNs}");
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
                DatadogTracing.GcEventStartEnd(arg.GCStartEndStatistics);
                _logger.LogInformation($"GC StartEnd Reason {arg.GCStartEndStatistics.Reason}; Duration {arg.GCStartEndStatistics.DurationMillsec}ms; Index {arg.GCStartEndStatistics.Index}; Gen {arg.GCStartEndStatistics.Generation}; Type {arg.GCStartEndStatistics.Type};");
            }
            else if (arg.Type == GCEventType.GCSuspend)
            {
                DatadogTracing.GcEventSuspend(arg.GCSuspendStatistics);
                _logger.LogInformation($"GC Suspend Reason {arg.GCSuspendStatistics.Reason}; Duration {arg.GCSuspendStatistics.DurationMillisec}ms; Count {arg.GCSuspendStatistics.Count};");
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
            if (arg.Type == ThreadPoolStatisticType.ThreadPoolWorkerStartStop)
            {
                DatadogTracing.ThreadPoolEventWorker(arg.ThreadPoolWorker);
                _logger.LogInformation($"ThreadPoolStartStop ActiveWrokerThreads {arg.ThreadPoolWorker.ActiveWrokerThreads};");
            }
            else if (arg.Type == ThreadPoolStatisticType.ThreadPoolAdjustment)
            {
                DatadogTracing.ThreadPoolEventAdjustment(arg.ThreadPoolAdjustment);
                _logger.LogInformation($"ThreadAdjustment Reason {arg.ThreadPoolAdjustment.Reason}; NewWorkerThread {arg.ThreadPoolAdjustment.NewWorkerThreads}; AverageThrouput {arg.ThreadPoolAdjustment.AverageThrouput};");
            }
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
            DatadogTracing.GcInfoTimerGauge(arg);
            _logger.LogInformation($"GCInfo HeapSize {arg.HeapSize}; Gen0Count {arg.Gen0Count}; Gen1Count {arg.Gen1Count}; Gen2Count {arg.Gen2Count}; Gen0Size {arg.Gen0Size}; Gen1Size {arg.Gen1Size}; Gen2Size {arg.Gen2Size}; LohSize {arg.LohSize}; TimeInGc {arg.TimeInGc}");
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
            DatadogTracing.ProcessInfoTimerGauge(arg);
            _logger.LogInformation($"ProcessInfo Cpu {arg.Cpu}; PrivateBytes {arg.PrivateBytes}; WorkingSet {arg.WorkingSet};");
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
            DatadogTracing.ThreadInfoTimerGauge(arg);
            var usingWorkerThreads = arg.MaxWorkerThreads - arg.AvailableWorkerThreads;
            _logger.LogInformation($"ThreadInfo AvailableWorkerThreads {arg.AvailableWorkerThreads}; MaxWorkerThreads {arg.MaxWorkerThreads}; UsingWorkerThreads {usingWorkerThreads}; ThreadCount {arg.ThreadCount}; QueueLength {arg.QueueLength}; LockContentionCount {arg.LockContentionCount}; CompletedItemsCount {arg.CompletedItemsCount}; AvailableCompletionPortThreads {arg.AvailableCompletionPortThreads}");
            return Task.CompletedTask;
        }
    }
}
