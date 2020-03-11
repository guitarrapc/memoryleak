using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Oop
{
    public class ThreadingTracker<TStats> : ITracker<ThreadingStats>
    {
        public static ITracker<ThreadingStats> Current { get; } = new ThreadingTracker<ThreadingStats>();
        public ThreadingStats PreviousStat { get; private set; }
        public ThreadingStats CurrentStat { get; private set; }
        public ThreadingStats DiffStat { get; private set; }

        public void Start()
        {
            CurrentStat = PreviousStat = ThreadingStats.ReadInitial();
        }

        public void Track()
        {
            PreviousStat = CurrentStat;
            CurrentStat = ThreadingStats.ReadCurrent();
            DiffStat = CurrentStat - PreviousStat;
        }

        public void Final()
        {
            PreviousStat = CurrentStat;
            CurrentStat = ThreadingStats.ReadFinal();
            DiffStat = CurrentStat - PreviousStat;
        }
    }
}
