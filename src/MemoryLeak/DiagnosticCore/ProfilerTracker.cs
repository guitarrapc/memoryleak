﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore.Statistics;

namespace DiagnosticCore
{
    /// <summary>
    /// Register Options for tracker callbacks and Cancelltion token
    /// </summary>
    public class ProfilerTrackerOptions
    {
        /// <summary>
        /// CancellcationToken to cancel reading event channel.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        /// <summary>
        /// Callback invoke when Contention Event emitted (generally lock event) and error.
        /// </summary>
        public (Func<ContentionEventStatistics, Task> OnSuccess, Action<Exception> OnError) ContentionEventCallback { get; set; }
        /// <summary>
        /// Callback invoke when GC Event emitted and error.
        /// </summary>
        public (Func<GCEventStatistics, Task> OnSuccess, Action<Exception> OnError) GCEventCallback { get; set; }
        /// <summary>
        /// Callback invoke when ThreadPool Event emitted and error.
        /// </summary>
        public (Func<ThreadPoolEventStatistics, Task> OnSuccess, Action<Exception> OnError) ThreadPoolEventCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer GCInfo Event emitted and error.
        /// </summary>
        public (Func<GCInfoStatistics, Task> OnSuccess, Action<Exception> OnError) GCInfoTimerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer ProcessInfo Event emitted and error.
        /// </summary>
        public (Func<ProcessInfoStatistics, Task> OnSuccess, Action<Exception> OnError) ProcessInfoTimerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer ThreadInfo Event emitted and error.
        /// </summary>
        public (Func<ThreadInfoStatistics, Task> OnSuccess, Action<Exception> OnError) ThreadInfoTimerCallback { get; set; }

        /// <summary>
        /// Timer dueTime/interval Options.
        /// </summary>
        public (TimeSpan dueTime, TimeSpan intervalPeriod) TimerOption { get; set; } = (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public class ProfilerTracker
    {
        /// <summary>
        /// Singleton instance access.
        /// </summary>
        public static Lazy<ProfilerTracker> Current = new Lazy<ProfilerTracker>(() => new ProfilerTracker());

        /// <summary>
        /// Options for the tracker
        /// </summary>
        public static ProfilerTrackerOptions Options { get; set; } = new ProfilerTrackerOptions();

        private readonly IProfiler[] profilerStats;
        private int initializedNum;

        private ProfilerTracker()
        {
            // list Stats
            profilerStats = new IProfiler[] {
                // event
                new GCEventProfiler(Options.GCEventCallback.OnSuccess, Options.GCEventCallback.OnError),
                new ThreadPoolEventProfiler(Options.ThreadPoolEventCallback.OnSuccess, Options.ThreadPoolEventCallback.OnError),
                new ContentionEventProfiler(Options.ContentionEventCallback.OnSuccess, Options.ContentionEventCallback.OnError),
                // timer
                new ThreadInfoTimerProfiler(Options.ThreadInfoTimerCallback.OnSuccess, Options.ThreadInfoTimerCallback.OnError, Options.TimerOption),
                new GCInfoTimerProfiler(Options.GCInfoTimerCallback.OnSuccess, Options.GCInfoTimerCallback.OnError, Options.TimerOption),
                new ProcessInfoTimerProfiler(Options.ProcessInfoTimerCallback.OnSuccess, Options.ProcessInfoTimerCallback.OnError, Options.TimerOption),
            };
        }

        /// <summary>
        /// Start tracking.
        /// </summary>
        public void Start()
        {
            // offer thread safe single access
            Interlocked.Increment(ref initializedNum);
            if (initializedNum != 1) return;

            // reset initialization status if cancelled.
            Options.CancellationTokenSource.Token.Register(() => initializedNum = 0);

            foreach (var profile in profilerStats)
            {
                profile.Start();
                // FireAndForget
                profile.ReadResultAsync(Options.CancellationTokenSource.Token);
            }
        }
        /// <summary>
        /// Restart tracking.
        /// </summary>
        public void Restart()
        {
            if (initializedNum != 1) return;

            foreach (var stat in profilerStats)
            {
                stat.Restart();
            }
        }
        /// <summary>
        /// Stop tracking.
        /// </summary>
        public void Stop()
        {
            if (initializedNum != 1) return;

            foreach (var stat in profilerStats)
            {
                stat.Stop();
            }
        }

        /// <summary>
        /// Cancel tracking.
        /// </summary>
        public void Cancel()
        {
            Options.CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Reset tracking.
        /// Available when existing cancellation token source is cancelled.
        /// </summary>
        /// <param name="cts"></param>
        public bool Reset(CancellationTokenSource cts)
        {
            if (Options.CancellationTokenSource.IsCancellationRequested)
            {
                Options.CancellationTokenSource = cts;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Show profiler status.
        /// </summary>
        /// <param name="action"></param>
        public void Status(Action<(string Name, bool Enabled)> action)
        {
            foreach (var profiler in profilerStats)
            {
                action((profiler.Name, profiler.Enabled));
            }
        }
    }
}
