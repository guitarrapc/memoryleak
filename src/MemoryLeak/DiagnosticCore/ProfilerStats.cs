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

    internal class EventListenerStat : IProfilerStat
    {
        private SimpleEventListener listener;
        private readonly ClrRuntimeEventKeywords keyword;

        public EventListenerStat(ClrRuntimeEventKeywords clrRuntimeEventKeyword)
        {
            keyword = clrRuntimeEventKeyword;
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            SimpleEventListener.keyword = (int)keyword;
            listener = new SimpleEventListener();
        }

        public void Stop()
        {
            listener.Stop();
        }
    }

    internal class ProfilerEventListenerStat : IProfilerStat
    {
        private GCEventListener listener;

        public ProfilerEventListenerStat(string targetSourceName, ClrRuntimeEventKeywords clrRuntimeEventKeyword)
        {
            listener = new GCEventListener(targetSourceName, EventLevel.Informational, ClrRuntimeEventKeywords.GC);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.MeastureGcElapsedTime(eventData), () =>
            {
                Console.WriteLine("Start: EventListerStat");
            });            
        }

        public void Stop()
        {
            listener.Stop();
        }
    }
}
