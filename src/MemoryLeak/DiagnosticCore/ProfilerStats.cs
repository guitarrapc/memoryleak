using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using DiagnosticCore.EventListeners;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace DiagnosticCore
{
    public interface IProfilerStat
    {
        void Start();
        void Restart();
        void Stop();
    }
    // https://github.com/dotnet/diagnostics/blob/b1f65150bb22ec96f56aaf3f0c6bb24c0b356a01/documentation/tutorial/src/triggerdump/Program.cs
    internal class CpuProfilerStat : IProfilerStat
    {
        private EventPipeSession _session;
        private EventPipeEventSource _source;
        private readonly int _processId;

        public CpuProfilerStat(int processId) => _processId = processId;

        public void Start()
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider(
                    "System.Runtime",
                    EventLevel.Informational,
                    (long)ClrTraceEventParser.Keywords.None,
                    new Dictionary<string, string>() {
                        { "EventCounterIntervalSec", "1" }
                    }
                )
            };
            var client = new DiagnosticsClient(_processId);
            using (var session = client.StartEventPipeSession(providers))
            using (var source = new EventPipeEventSource(session.EventStream))
            {
                _session = session;
                _source = source;
                _source.Dynamic.All += Trigger;

                try
                {
                    _source?.Process();
                }
                catch (DiagnosticsClientException) { }
            }
        }
        public void Restart()
        {
            if (_source != null && _session != null)
            {
                // already register eventpipeeventsource, let's restart
                _source.Dynamic.All += Trigger;
                return;
            }
        }
        public void Stop()
        {
            // do not dispose source and session.
            _source.Dynamic.All -= Trigger;
        }

        private void Trigger(TraceEvent evt)
        {
            if (evt.EventName.Equals("EventCounters"))
            {
                // todo: get type to avoid boxing.
                IDictionary<string, object> payloadVal = (IDictionary<string, object>)(evt.PayloadValue(0));
                IDictionary<string, object> payloadFields = (IDictionary<string, object>)(payloadVal["Payload"]);
                if (payloadFields["Name"].ToString().Equals("cpu-usage"))
                {
                    double cpuUsage = Double.Parse(payloadFields["Mean"]?.ToString());
                    Console.WriteLine("cpu usage: " + cpuUsage);
                }
            }
        }
    }

    internal class GCEventProfilerStat : IProfilerStat
    {
        private EventPipeSession _session;
        private EventPipeEventSource _source;
        private readonly int _processId;

        public GCEventProfilerStat(int processId) => _processId = processId;

        public void Start()
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider(
                    "Microsoft-Windows-DotNETRuntime",
                    EventLevel.Informational,
                    (long)ClrTraceEventParser.Keywords.GC
                )
            };
            var client = new DiagnosticsClient(_processId);
            using (var session = client.StartEventPipeSession(providers, false))
            using (var source = new EventPipeEventSource(session.EventStream))
            {
                _session = session;
                _source = source;
                _source.Clr.All += Trigger;

                try
                {
                    _source?.Process();
                }
                catch (DiagnosticsClientException) { }
            }
        }
        public void Restart()
        {
            if (_source != null && _session != null)
            {
                // already register eventpipeeventsource, let's restart
                _source.Dynamic.All += Trigger;
                return;
            }
        }
        public void Stop()
        {
            // source not yet triggered from session. GC not happen yet.
            if (_source == null) return;
            // do not dispose source and session.
            _source.Dynamic.All -= Trigger;
        }

        internal void Trigger(TraceEvent evt)
        {
            //Console.WriteLine(evt);
            if (evt.EventName.Equals("GC/Triggered"))
            {
                var gcEventData = (GCTriggeredTraceData)evt;
                Console.WriteLine(gcEventData.Reason);
            }
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
}
