using System;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;

namespace DiagnosticCore
{
    public class ProfilerTrackerOptions
    {
        public CancellationToken CancellationToken { get; set; }
        public Func<GCStatistics, Task> GCDurationProfilerCallback { get; set; }
    }
    public class ProfilerTracker
    {
        public static Lazy<ProfilerTracker> Current = new Lazy<ProfilerTracker>(() => new ProfilerTracker());

        public static ProfilerTrackerOptions Options { get; set; } = new ProfilerTrackerOptions();

        private readonly int _processId;
        private readonly IProfilerStat[] profilerStats;
        private bool initialized;

        public ProfilerTracker()
        {
            // list Stat
            profilerStats = new IProfilerStat[] {
                new GCEventListenerStat(Options?.GCDurationProfilerCallback),
            };
        }

        public void Start()
        {
            // TODO: Task.Run....
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
