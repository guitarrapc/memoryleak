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
        public Func<EtwGCStatistics, Task> GCProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when ThreadPool Event emitted
        /// </summary>
        public Func<EtwThreadPoolStatistics, Task> ThreadProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Contention Event emitted (generally lock event)
        /// </summary>
        public Func<EtwContentionStatistics, Task> ContentionProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer ThreadInfo Event emitted.
        /// </summary>
        public Func<TimerThreadInfoStatistics, Task> TimerThreadInfoCallback { get; set; }
        /// <summary>
        /// ThreadInfo dueTime/interval Options.
        /// </summary>
        public (TimeSpan dueTime, TimeSpan intervalPeriod) TimerThreadInfoOption { get; set; } = (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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

        private readonly IProfilerStat[] profilerStats;
        private bool initialized;

        public ProfilerTracker() =>
            // list Stat
            profilerStats = new IProfilerStat[] {
                new GCEventStat(Options?.GCProfilerCallback),
                new ThreadPoolEventStat(Options?.ThreadProfilerCallback),
                new ContentionEventStat(Options?.ContentionProfilerCallback),
                new TimerThreadInfoStat(Options?.TimerThreadInfoCallback, Options.TimerThreadInfoOption)
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
