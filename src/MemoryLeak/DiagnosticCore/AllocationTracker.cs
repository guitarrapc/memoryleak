using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore
{
    public class AllocationTracker<TStats> : ITracker<GcStats>
    {
        public static ITracker<GcStats> Current { get; } = new AllocationTracker<GcStats>();
        public GcStats PreviousStat { get; private set; }
        public GcStats CurrentStat { get; private set; }
        public GcStats DiffStat { get; private set; }

        public void Start()
        {
            CurrentStat = PreviousStat = GcStats.ReadInitial();
        }

        public void Track()
        {
            PreviousStat = CurrentStat;
            CurrentStat = GcStats.ReadCurrent();
            DiffStat = CurrentStat - PreviousStat;
        }

        public void Final()
        {
            PreviousStat = CurrentStat;
            CurrentStat = GcStats.ReadFinal();
            DiffStat = CurrentStat - PreviousStat;
        }
    }
}
