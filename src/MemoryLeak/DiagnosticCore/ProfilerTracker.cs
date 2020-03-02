using System.Threading.Tasks;

namespace DiagnosticCore
{
    public class ProfilerTracker
    {
        public static ProfilerTracker Current = new ProfilerTracker(System.Diagnostics.Process.GetCurrentProcess().Id);
        
        private readonly int _processId;
        private readonly IProfilerStat[] profilerStats;
        private bool initialized;

        public ProfilerTracker(int processId)
        {
            _processId = processId;
            profilerStats = new[] {
                new CpuProfilerStat(_processId),
            };
        }

        public void Start()
        {
            if (initialized) return;

            Task monitorTask = new Task(() =>
            {
                foreach (var stat in profilerStats)
                {
                    stat.Start();
                }
            });
            monitorTask.Start();
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
