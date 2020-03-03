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

    public class GCEventStat : IProfilerStat
    {
        private GCEventListener listener;
        public Action<ChannelReader<GCStatistics>> ProfilerCallback { get; set; }

        public GCEventStat(Func<GCStatistics, Task> onGCDurationEvent)
        {
            listener = new GCEventListener(onGCDurationEvent);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(GCEventStat)}");
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

    public class ThreadEventStat : IProfilerStat
    {
        private ThreadEventListener listener;
        public Action<ChannelReader<ThreadStatistics>> ProfilerCallback { get; set; }

        public ThreadEventStat(Func<ThreadStatistics, Task> onGCDurationEvent)
        {
            listener = new ThreadEventListener(onGCDurationEvent);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(ThreadEventStat)}");
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
