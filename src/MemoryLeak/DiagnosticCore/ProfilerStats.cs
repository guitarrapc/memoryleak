using System;
using System.Diagnostics.Tracing;
using DiagnosticCore.EventListeners;
using DiagnosticCore.EventPipeEventSources;

namespace DiagnosticCore
{
    public interface IProfilerStat
    {
        void Start();
        void Restart();
        void Stop();
    }
    internal class CpuProfilerStat : IProfilerStat
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
    }

    internal class GCEventProfilerStat : IProfilerStat
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
    }

    internal class GCEventListenerStat : IProfilerStat
    {
        private GCDurationEventListener listener;

        public GCEventListenerStat()
        {
            listener = new GCDurationEventListener("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.GC);
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
    }
}
