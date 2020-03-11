using System;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;
using DiagnosticCore.Statistics;

namespace DiagnosticCore
{
    public class ProfilerTrackerOptions
    {
        public CancellationToken CancellationToken { get; set; }
        /// <summary>
        /// Callback invoke when GC Event emitted
        /// </summary>
        public Func<GCEventStatistics, Task> GCEventProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when ThreadPool Event emitted
        /// </summary>
        public Func<ThreadPoolEventStatistics, Task> ThreadPoolEventProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Contention Event emitted (generally lock event)
        /// </summary>
        public Func<ContentionEventStatistics, Task> ContentionEventProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer ThreadInfo Event emitted.
        /// </summary>
        public Func<ThreadInfoStatistics, Task> ThreadInfoTimerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer GCInfo Event emitted.
        /// </summary>
        public Func<GCInfoStatistics, Task> GCInfoTimerCallback { get; set; }
        /// <summary>
        /// Timer dueTime/interval Options.
        /// </summary>
        public (TimeSpan dueTime, TimeSpan intervalPeriod) TimerOption { get; set; } = (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public class ProfilerTracker
    {
        /// <summary>
        /// ProfilerTracker.Options にコールバック登録してトラッキングしているイベントごとに自分の処理を実行
        /// ProfilerTracker.Current.Value.Start() でトラッキング開始
        /// ProfilerTracker.Current.Value.Stop()  でトラッキングを停止
        /// </summary>
        public static Lazy<ProfilerTracker> Current = new Lazy<ProfilerTracker>(() => new ProfilerTracker());

        public static ProfilerTrackerOptions Options { get; set; } = new ProfilerTrackerOptions();

        private readonly IProfiler[] profilerStats;
        private bool initialized;

        public ProfilerTracker() =>
            // list Stat
            profilerStats = new IProfiler[] {
                // event
                new GCEventProfiler(Options?.GCEventProfilerCallback),
                new ThreadPoolEventProfiler(Options?.ThreadPoolEventProfilerCallback),
                new ContentionEventProfiler(Options?.ContentionEventProfilerCallback),
                // timer
                new ThreadInfoTimerProfiler(Options?.ThreadInfoTimerCallback, Options.TimerOption),
                new GCInfoTimerProfiler(Options?.GCInfoTimerCallback, Options.TimerOption),
            };

        public void Start()
        {
            foreach (var profile in profilerStats)
            {
                profile.Start();
                // FireAndForget
                profile.ReadResultAsync(Options.CancellationToken);
            }
            initialized = true;
        }
        public void Restart()
        {
            if (!initialized) return;

            foreach (var stat in profilerStats)
            {
                stat.Restart();
            }
        }
        public void Stop()
        {
            if (!initialized) return;

            foreach (var stat in profilerStats)
            {
                stat.Stop();
            }
        }
    }
}
