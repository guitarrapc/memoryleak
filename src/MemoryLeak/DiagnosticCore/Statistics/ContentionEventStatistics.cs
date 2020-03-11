﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public struct ContentionEventStatistics
    {
        public long Time { get; set; }
        /// <summary>
        /// 0 : managed.
        /// 1 : native
        /// </summary>
        public byte Flag { get; set; }
        public double DurationNs { get; set; }
    }
}