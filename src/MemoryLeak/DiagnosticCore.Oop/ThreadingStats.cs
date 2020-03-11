using System;
using System.Threading;

namespace DiagnosticCore.Oop
{
    public struct ThreadingStats : IEquatable<ThreadingStats>
    {
        internal const string ResultsLinePrefix = "Threading: ";

        // BDN targets .NET Standard 2.0, these properties are not part of .NET Standard 2.0, were added in .NET Core 3.0
        private static readonly Func<long> GetCompletedWorkItemCountDelegate = CreateLongGetterDelegate(typeof(ThreadPool), "CompletedWorkItemCount");
        private static readonly Func<long> GetLockContentionCountDelegate = CreateLongGetterDelegate(typeof(Monitor), "LockContentionCount");
        private static readonly Func<int> GetThreadCountDelegate = CreateIntGetterDelegate(typeof(ThreadPool), "ThreadCount");

        public static ThreadingStats Empty => new ThreadingStats(0, 0, 0, 0);

        public long CompletedWorkItemCount { get; }
        public long LockContentionCount { get; }
        public long TotalOperations { get; }
        public int ThreadCount { get; }

        public ThreadingStats(long completedWorkItemCount, long lockContentionCount, long totalOperations, int threadCount)
        {
            CompletedWorkItemCount = completedWorkItemCount;
            LockContentionCount = lockContentionCount;
            TotalOperations = totalOperations;
            ThreadCount = threadCount;
        }

        public static ThreadingStats ReadInitial()
        {
            long lockContentionCount = GetLockContentionCountDelegate(); // Monitor.LockContentionCount can schedule a work item and needs to be called before ThreadPool.CompletedWorkItemCount
            long completedWorkItemCount = GetCompletedWorkItemCountDelegate();
            int threadCount = GetThreadCountDelegate();

            return new ThreadingStats(completedWorkItemCount, lockContentionCount, 0, threadCount);
        }

        public static ThreadingStats ReadCurrent()
        {
            long lockContentionCount = GetLockContentionCountDelegate(); // Monitor.LockContentionCount can schedule a work item and needs to be called before ThreadPool.CompletedWorkItemCount
            long completedWorkItemCount = GetCompletedWorkItemCountDelegate();
            int threadCount = GetThreadCountDelegate();

            return new ThreadingStats(completedWorkItemCount, lockContentionCount, 0, threadCount);
        }

        public static ThreadingStats ReadFinal()
        {
            long completedWorkItemCount = GetCompletedWorkItemCountDelegate();
            long lockContentionCount = GetLockContentionCountDelegate(); // Monitor.LockContentionCount can schedule a work item and needs to be called after ThreadPool.CompletedWorkItemCount
            int threadCount = GetThreadCountDelegate();

            return new ThreadingStats(completedWorkItemCount, lockContentionCount, 0, threadCount);
        }

        public string ToOutputLine() => $"{ResultsLinePrefix} {CompletedWorkItemCount} {LockContentionCount} {TotalOperations}";

        public static ThreadingStats Parse(string line)
        {
            if (!line.StartsWith(ResultsLinePrefix))
                throw new NotSupportedException($"Line must start with {ResultsLinePrefix}");

            var measurementSplit = line.Remove(0, ResultsLinePrefix.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!long.TryParse(measurementSplit[0], out long completedWorkItemCount)
                || !long.TryParse(measurementSplit[1], out long lockContentionCount)
                || !long.TryParse(measurementSplit[2], out long totalOperationsCount)
                || !int.TryParse(measurementSplit[3], out int threadCount))
            {
                throw new NotSupportedException("Invalid string");
            }

            return new ThreadingStats(completedWorkItemCount, lockContentionCount, totalOperationsCount, threadCount);
        }

        public static ThreadingStats operator +(ThreadingStats left, ThreadingStats right)
            => new ThreadingStats(
                left.CompletedWorkItemCount + right.CompletedWorkItemCount,
                left.LockContentionCount + right.LockContentionCount,
                left.TotalOperations + right.TotalOperations,
                left.ThreadCount + right.ThreadCount);

        public static ThreadingStats operator -(ThreadingStats left, ThreadingStats right)
            => new ThreadingStats(
                left.CompletedWorkItemCount - right.CompletedWorkItemCount,
                left.LockContentionCount - right.LockContentionCount,
                left.TotalOperations - right.TotalOperations,
                left.ThreadCount - right.ThreadCount);

        public ThreadingStats WithTotalOperations(long totalOperationsCount) => new ThreadingStats(CompletedWorkItemCount, LockContentionCount, totalOperationsCount, ThreadCount);

        public override string ToString() => ToOutputLine();

        private static Func<long> CreateLongGetterDelegate(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);

            // we create delegate to avoid boxing, IMPORTANT!
            return property != null ? (Func<long>)property.GetGetMethod().CreateDelegate(typeof(Func<long>)) : () => 0;
        }
        private static Func<int> CreateIntGetterDelegate(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);

            // we create delegate to avoid boxing, IMPORTANT!
            return property != null ? (Func<int>)property.GetGetMethod().CreateDelegate(typeof(Func<int>)) : () => 0;
        }

        public bool Equals(ThreadingStats other) => CompletedWorkItemCount == other.CompletedWorkItemCount && LockContentionCount == other.LockContentionCount && TotalOperations == other.TotalOperations;

        public override bool Equals(object obj) => obj is ThreadingStats other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = CompletedWorkItemCount.GetHashCode();
                hashCode = (hashCode * 397) ^ LockContentionCount.GetHashCode();
                hashCode = (hashCode * 397) ^ TotalOperations.GetHashCode();
                return hashCode;
            }
        }
    }
}