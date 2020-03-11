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
        /// Callback invoke when GC Event emitted and error.
        /// </summary>
        public (Func<GCEventStatistics, Task> OnSuccess, Action<Exception> OnError) GCEventCallback { get; set; }
        /// <summary>
        /// Callback invoke when ThreadPool Event emitted and error.
        /// </summary>
        public (Func<ThreadPoolEventStatistics, Task> OnSuccess, Action<Exception> OnError) ThreadPoolEventCallback { get; set; }
        /// <summary>
        /// Callback invoke when Contention Event emitted (generally lock event) and error.
        /// </summary>
        public (Func<ContentionEventStatistics, Task> OnSuccess, Action<Exception> OnError) ContentionEventCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer ThreadInfo Event emitted and error.
        /// </summary>
        public (Func<ThreadInfoStatistics, Task> OnSuccess, Action<Exception> OnError) ThreadInfoTimerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer GCInfo Event emitted and error.
        /// </summary>
        public (Func<GCInfoStatistics, Task> OnSuccess, Action<Exception> OnError) GCInfoTimerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Timer ProcessInfo Event emitted and error.
        /// </summary>
        public (Func<ProcessInfoStatistics, Task> OnSuccess, Action<Exception> OnError) ProcessInfoTimerCallback { get; set; }

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
                new GCEventProfiler(Options?.GCEventCallback.OnSuccess, Options?.GCEventCallback.OnError),
                new ThreadPoolEventProfiler(Options?.ThreadPoolEventCallback.OnSuccess, Options?.ThreadPoolEventCallback.OnError),
                new ContentionEventProfiler(Options?.ContentionEventCallback.OnSuccess, Options?.ContentionEventCallback.OnError),
                // timer
                new ThreadInfoTimerProfiler(Options?.ThreadInfoTimerCallback.OnSuccess, Options?.ThreadInfoTimerCallback.OnError, Options.TimerOption),
                new GCInfoTimerProfiler(Options?.GCInfoTimerCallback.OnSuccess, Options?.GCInfoTimerCallback.OnError, Options.TimerOption),
                new ProcessInfoTimerProfiler(Options?.ProcessInfoTimerCallback.OnSuccess, Options?.ProcessInfoTimerCallback.OnError, Options.TimerOption),
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
