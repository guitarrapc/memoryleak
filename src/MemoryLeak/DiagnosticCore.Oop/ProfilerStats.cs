using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore.Oop.EventPipeEventSources;

namespace DiagnosticCore.Oop
{
    public interface IProfilerStat
    {
        void Start();
        void Restart();
        void Stop();
        Task ReadResultAsync(CancellationToken cancellationToken);
    }
    public class CpuEventPipeProfilerStat : IProfilerStat
    {
        private readonly CpuEventPipeEventSource source;
        public CpuEventPipeProfilerStat(int processId) => source= new CpuEventPipeEventSource(processId);

        public void Start()
        {
            source.Start();
        }
        public void Restart()
        {
            source.Restart();
        }
        public void Stop()
        {
            source.Stop();
        }
        public Task ReadResultAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class GCEventPipeProfilerStat : IProfilerStat
    {
        private readonly GCEventPipeEventSource source;
        public GCEventPipeProfilerStat(int processId) => source = new GCEventPipeEventSource(processId);

        public void Start()
        {
            source.Start();
        }
        public void Restart()
        {
            source.Restart();
        }
        public void Stop()
        {
            source.Stop();
        }
        public Task ReadResultAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
