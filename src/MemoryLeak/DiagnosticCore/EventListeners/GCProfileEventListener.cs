using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public struct GCDurationResult
    {
        public long Index { get; set; }
        public double DurationMillsec { get; set; }
    }
    internal class GCDurationEventListener : ProfileEventListenerBase
    {
        public ulong countTotalEvents = 0;
        long timeGCStart = 0;
        public Channel<GCDurationResult> Result { get; }

        public GCDurationEventListener(string targetSourceName, EventLevel level, ClrRuntimeEventKeywords keywords) : base(targetSourceName, level, keywords)
        {
            Result = new Channel<GCDurationResult>();
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
                var duration = (double)(timeGCEnd - timeGCStart) / 10.0 / 1000.0;
                
                // note: need handle when not write?
                var stat = new GCDurationResult
                {
                    Index = gcIndex,
                    DurationMillsec = duration
                };                
                Result.Writer.TryWrite(stat);

                Console.WriteLine("GC#{0} took {1:f3}ms", gcIndex, duration);
            }

            countTotalEvents++;
        }
    }
}
