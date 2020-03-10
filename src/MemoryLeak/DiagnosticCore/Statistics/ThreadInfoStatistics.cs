using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public struct TimerThreadInfoStatistics
    {
        public DateTime Date { get; set; }
        public int AvailableWorkerThreads { get; set; }
        public int AvailableCompletionPortThreads { get; set; }
        public int MaxWorkerThreads { get; set; }
        public int MaxCompletionPortThreads { get; set; }
    }
}
