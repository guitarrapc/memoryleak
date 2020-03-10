using System;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;

namespace DiagnosticCore
{
    public class ProfilerTrackerOptions
    {
        public CancellationToken CancellationToken { get; set; }
        /// <summary>
        /// Callback invoke when GC Event emitted
        /// </summary>
        public Func<GCStatistics, Task> GCProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when ThreadPool Event emitted
        /// </summary>
        public Func<ThreadPoolStatistics, Task> ThreadProfilerCallback { get; set; }
        /// <summary>
        /// Callback invoke when Contention Event emitted (generally lock event)
        /// </summary>
        public Func<ContentionStatistics, Task> ContentionProfilerCallback { get; set; }
    }
    public class ProfilerTracker
    {
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
            };

        public void Start()
        {
            // FireAndForget
            foreach (var profile in profilerStats)
            {
                var t = Task.Run(() => profile.Start());
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
