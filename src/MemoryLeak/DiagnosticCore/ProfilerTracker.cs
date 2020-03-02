using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace DiagnosticCore
{
    // https://github.com/dotnet/diagnostics/blob/b1f65150bb22ec96f56aaf3f0c6bb24c0b356a01/documentation/tutorial/src/triggerdump/Program.cs
    public class ProfilerTracker
    {
        public static ProfilerTracker Current = new ProfilerTracker(System.Diagnostics.Process.GetCurrentProcess().Id, 500);
        
        private readonly int _processId;
        private readonly double _cpuUsageThreshold;

        private EventPipeSession _session;
        private EventPipeEventSource _source;

        public ProfilerTracker(int processId, double cpuUsageThreshold)
        {
            _processId = processId;
            _cpuUsageThreshold = cpuUsageThreshold;
        }

        public void StartTriggerDumpOnCpuUsage()
        {
            if (_source != null && _session != null)
            {
                // already register eventpipeeventsource, let's restart
                _source.Dynamic.All += TriggerCpuDump;
                return;
            }
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
                _source.Dynamic.All += TriggerCpuDump;

                try
                {
                    _source?.Process();
                }
                catch (DiagnosticsClientException) { }
            }
        }
        public void StopTriggerDumpOnCpuUsage()
        {
            // do not dispose source and session.
            _source.Dynamic.All -= TriggerCpuDump;
        }
        private void TriggerCpuDump(TraceEvent evt)
        {
            if (evt.EventName.Equals("EventCounters"))
            {
                var names = evt.PayloadNames;
                // todo: get type to avoid boxing.
                IDictionary<string, object> payloadVal = (IDictionary<string, object>)(evt.PayloadValue(0));
                IDictionary<string, object> payloadFields = (IDictionary<string, object>)(payloadVal["Payload"]);
                if (payloadFields["Name"].ToString().Equals("cpu-usage"))
                {
                    double cpuUsage = Double.Parse(payloadFields["Mean"]?.ToString());
                    Console.WriteLine("cpu usage: " + cpuUsage);
                    if (cpuUsage > _cpuUsageThreshold)
                    {
                        //client.WriteDump(DumpType.Normal, "/tmp/minidump.dmp");
                    }
                }
            }
        }
    }
}
