using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace DiagnosticCore.Internals.Environments.Runtimes
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/a08062fa473e3426c7d5f1a1cf2ad5cbbc46f7d1/src/BenchmarkDotNet/Environments/Runtimes/MonoRuntime.cs
    public class MonoRuntime : Runtime, IEquatable<MonoRuntime>
    {
        public static readonly MonoRuntime Default = new MonoRuntime("Mono");

        public string CustomPath { get; }

        public string AotArgs { get; }

        public string MonoBclPath { get; }

        private MonoRuntime(string name) : base(RuntimeMoniker.Mono, "mono", name)
        {
        }

        public MonoRuntime(string name, string customPath) : this(name) => CustomPath = customPath;

        public MonoRuntime(string name, string customPath, string aotArgs, string monoBclPath) : this(name)
        {
            CustomPath = customPath;
            AotArgs = aotArgs;
            MonoBclPath = monoBclPath;
        }

        public override bool Equals(object obj) => obj is MonoRuntime other && Equals(other);

        public bool Equals(MonoRuntime other)
            => base.Equals(other) && Name == other?.Name && CustomPath == other?.CustomPath && AotArgs == other?.AotArgs && MonoBclPath == other?.MonoBclPath;

        public override int GetHashCode()
            => base.GetHashCode() ^ Name.GetHashCode() ^ (CustomPath?.GetHashCode() ?? 0) ^ (AotArgs?.GetHashCode() ?? 0) ^ (MonoBclPath?.GetHashCode() ?? 0);
    }
}
