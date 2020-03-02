using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace DiagnosticCore.EventListeners
{
    // wrapper of ClrTraceEventParser.Keywords
    // https://github.com/microsoft/perfview/blob/101984515958750f83063c117084eeec0866a19f/src/TraceEvent/Parsers/ClrTraceEventParser.cs#L36
    enum ClrRuntimeEventKeywords : long
    {
        None = 0,
        /// <summary>
        /// Logging when garbage collections and finalization happen.
        /// </summary>
        GC = 0x1,
        /// <summary>
        /// Events when GC handles are set or destroyed.
        /// </summary>
        GCHandle = 0x2,
        /// <summary>
        /// Logging when modules actually get loaded and unloaded. 
        /// </summary>
        Loader = 0x8,
        /// <summary>
        /// Logging when Just in time (JIT) compilation occurs. 
        /// </summary>
        Jit = 0x10,
        /// <summary>
        /// Log when lock contention occurs.  (Monitor.Enters actually blocks)
        /// </summary>
        Contention = 0x4000,
        /// <summary>
        /// Log exception processing.
        /// </summary>
        Exceptions = 0x8000,
        /// <summary>
        /// Log events associated with the threadpool, and other threading events.  
        /// </summary>
        Threading = 0x10000,
        /// <summary>
        /// Recommend default flags (good compromise on verbosity).  
        /// </summary>
        Default = GC | Loader | Jit | Contention | Exceptions | Threading,
    }

    internal class SimpleEventListener : EventListener
    {
        public ulong countTotalEvents = 0;
        public static long keyword;

        long timeGCStart = 0;

        private List<EventSource> enabledEvents = new List<EventSource>();
        EventSource eventSourceDotNet; // avoid GC.

        // .ctor call after OnEventSourceCreated. https://github.com/Microsoft/ApplicationInsights-dotnet/issues/1106
        public SimpleEventListener() { }

        // Called whenever an EventSource is created.
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            Console.WriteLine(eventSource.Name);
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                //EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)keyword);
                EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x0000001);
                eventSourceDotNet = eventSource;
                enabledEvents.Add(eventSourceDotNet);
            }
        }
        // Called whenever an event is written.
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
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

        private void ShowEventDataDetail(EventWrittenEventArgs eventData)
        {
            Console.WriteLine($"ThreadID = {eventData.OSThreadId} ID = {eventData.EventId} Name = {eventData.EventName}");
            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                string payloadString = eventData.Payload[i] != null ? eventData.Payload[i].ToString() : string.Empty;
                Console.WriteLine($"    Name = \"{eventData.PayloadNames[i]}\" Value = \"{payloadString}\"");
            }
            Console.WriteLine("\n");
        }

        public virtual void Restart()
        {
            foreach (var enabled in enabledEvents)
                EnableEvents(enabled, EventLevel.Informational, (EventKeywords)keyword);
        }

        public virtual void Stop()
        {
            foreach (var enabled in enabledEvents)
                DisableEvents(enabled);
        }
    }
}
