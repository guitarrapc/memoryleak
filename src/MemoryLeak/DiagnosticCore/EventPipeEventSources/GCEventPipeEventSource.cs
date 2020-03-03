using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace DiagnosticCore.EventPipeEventSources
{
    public class GCEventPipeEventSource
    {
        private readonly List<EventPipeProvider> providers;
        private readonly DiagnosticsClient client;

        private EventPipeSession _session;
        private EventPipeEventSource _source;

        public GCEventPipeEventSource(int processId)
        {
            providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider(
                    "Microsoft-Windows-DotNETRuntime",
                    EventLevel.Informational,
                    (long)ClrTraceEventParser.Keywords.GC
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
            // source not yet triggered from session. GC not happen yet.
            if (_source == null) return;
            // do not dispose source and session.
            _source.Dynamic.All -= Trigger;
        }

        private void Trigger(TraceEvent evt)
        {
            //Console.WriteLine(evt);
            if (evt.EventName.Equals("GC/Triggered"))
            {
                var gcEventData = (GCTriggeredTraceData)evt;
                Console.WriteLine(gcEventData.Reason);
            }
        }
    }
}
