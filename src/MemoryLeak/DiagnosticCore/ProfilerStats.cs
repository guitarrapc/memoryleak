using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;
using DiagnosticCore.EventPipeEventSources;

namespace DiagnosticCore
{
    public interface IProfilerStat
    {
        void Start();
        void Restart();
        void Stop();
        Task ReadResultAsync(CancellationToken cancellationToken);
    }
    public class CpuProfilerStat : IProfilerStat
    {
        private readonly CpuEventPipeEventSource source;
        public CpuProfilerStat(int processId) => source= new CpuEventPipeEventSource(processId);

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

    public class GCEventProfilerStat : IProfilerStat
    {
        private readonly GCEventPipeEventSource source;
        public GCEventProfilerStat(int processId) => source = new GCEventPipeEventSource(processId);

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

    public class GCEventListenerStat : IProfilerStat
    {
        private GCEventListener listener;
        public Action<ChannelReader<GCStatistics>> ProfilerCallback { get; set; }

        public GCEventListenerStat(Func<GCStatistics, Task> onGCDurationEvent)
        {
            listener = new GCEventListener(onGCDurationEvent);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.DefaultHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(GCEventListenerStat)}");
            });
        }

        public void Stop()
        {
            listener.Stop();
        }

        public async Task ReadResultAsync(CancellationToken cancellationToken)
        {
            await listener.OnReadResultAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
