using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Internals.Portability.Cpu
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/2ba30330ec5ec8a9abe284c927277a6fe0fc5a0c/src/BenchmarkDotNet/Portability/Cpu/ProcCpuInfoKeyNames.cs
    internal static class ProcCpuInfoKeyNames
    {
        internal const string PhysicalId = "physical id";
        internal const string CpuCores = "cpu cores";
        internal const string ModelName = "model name";
        internal const string MaxFrequency = "max freq";
        internal const string MinFrequency = "min freq";
    }
}
