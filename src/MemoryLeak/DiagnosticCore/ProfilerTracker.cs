using System.Threading.Tasks;

namespace DiagnosticCore
{
    public class ProfilerTracker
    {
        public static ProfilerTracker Current = new ProfilerTracker(System.Diagnostics.Process.GetCurrentProcess().Id);
        
        private readonly int _processId;
        private readonly CpuProfilerStats cpuProfilerStats;
        private bool initialized;

        public ProfilerTracker(int processId)
        {
            _processId = processId;
            cpuProfilerStats = new CpuProfilerStats(_processId);
        }

        public void Start()
        {
            if (initialized) return;

            Task monitorTask = new Task(() =>
            {
                cpuProfilerStats.Start();
            });
            monitorTask.Start();
            initialized = true;
        }
        public void Restart()
        {
            if (!initialized) return;
            
            cpuProfilerStats.Restart();
        }
        public void Stop()
        {
            if (!initialized) return;

            cpuProfilerStats.Stop();
        }
    }
}
