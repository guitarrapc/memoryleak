using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace DiagnosticCore.EventPipeEventSources
{
    // https://github.com/dotnet/diagnostics/blob/b1f65150bb22ec96f56aaf3f0c6bb24c0b356a01/documentation/tutorial/src/triggerdump/Program.cs
    internal class CpuEventPipeEventSource
    {
        private readonly List<EventPipeProvider> providers;
        private readonly DiagnosticsClient client;

        private EventPipeSession _session;
        private EventPipeEventSource _source;

        public CpuEventPipeEventSource(int processId)
        {
            providers = new List<EventPipeProvider>()
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

            client = new DiagnosticsClient(processId);
        }

        public void Start()
        {
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
}
