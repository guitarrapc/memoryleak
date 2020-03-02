namespace DiagnosticCore
{
    public interface ITracker<TStats> where TStats : struct
    {
        TStats PreviousStat { get; }
        TStats CurrentStat { get; }
        TStats DiffStat { get; }

        void Start();
        void Track();
        void Final();
    }
}
