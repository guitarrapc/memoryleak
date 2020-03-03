using System.Linq;
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
            profilerStats = new IProfilerStat[] {
                //new CpuProfilerStat(_processId),
                //new GCEventProfilerStat(_processId),
                new GCEventListenerStat(),
            };
        }

        public void Start()
        {
            foreach (var profile in profilerStats)
            {
                var t = new Task(() => profile.Start());
                t.Start();
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
