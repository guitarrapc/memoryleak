using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    internal class GCProfileEventListener : ProfileEventListenerBase
    {
        public ulong countTotalEvents = 0;
        long timeGCStart = 0;

        public GCProfileEventListener(string targetSourceName, EventLevel level, ClrRuntimeEventKeywords keywords) : base(targetSourceName, level, keywords)
        {
        }

        public GCProfileEventListener(string targetSourceName, EventLevel level, long keywords) : base(targetSourceName, level, keywords)
        {
        }

        public GCProfileEventListener(Guid targetSourceGuid, EventLevel level, ClrRuntimeEventKeywords keywords) : base(targetSourceGuid, level, keywords)
        {
        }

        public override void DefaultHandler(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "EventCounters") return;

            // Write the contents of the event to the console.
            if (eventData.EventName.Contains("GCStart"))
            {
                timeGCStart = eventData.TimeStamp.Ticks;
            }
            else if (eventData.EventName.Contains("GCEnd"))
            {
                long timeGCEnd = eventData.TimeStamp.Ticks;
                long gcIndex = long.Parse(eventData.Payload[0].ToString());
                Console.WriteLine("GC#{0} took {1:f3}ms",
                    gcIndex, (double)(timeGCEnd - timeGCStart) / 10.0 / 1000.0);
            }

            countTotalEvents++;
        }
    }
}
