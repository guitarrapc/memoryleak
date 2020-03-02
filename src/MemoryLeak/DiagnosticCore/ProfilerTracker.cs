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
        public void TriggerDumpOnCpuUsage(int processId, int threshold)
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
            var client = new DiagnosticsClient(processId);
            using (var session = client.StartEventPipeSession(providers))
            {
                var source = new EventPipeEventSource(session.EventStream);
                source.Dynamic.All += (TraceEvent obj) =>
                {
                    if (obj.EventName.Equals("EventCounters"))
                    {
                        var names = obj.PayloadNames;
                        IDictionary<string, object> payloadVal = (IDictionary<string, object>)(obj.PayloadValue(0));
                        IDictionary<string, object> payloadFields = (IDictionary<string, object>)(payloadVal["Payload"]);
                        if (payloadFields["Name"].ToString().Equals("cpu-usage"))
                        {
                            double cpuUsage = Double.Parse(payloadFields["Mean"]?.ToString());
                            Console.WriteLine("cpu usage: " + cpuUsage);
                            if (cpuUsage > (double)threshold)
                            {
                                //client.WriteDump(DumpType.Normal, "/tmp/minidump.dmp");
                            }
                        }
                    }
                };
                try
                {
                    source.Process();
                }
                catch (DiagnosticsClientException) { }
            }
        }
    }
}
