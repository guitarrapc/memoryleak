using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Portability.Cpu
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/a78b38b0e89d04ad3fe8934162c7adb42f81eabe/src/BenchmarkDotNet/Portability/Cpu/WmicCpuInfoKeyNames.cs
    internal static class WmicCpuInfoKeyNames
    {
        internal const string NumberOfLogicalProcessors = "NumberOfLogicalProcessors";
        internal const string NumberOfCores = "NumberOfCores";
        internal const string Name = "Name";
        internal const string MaxClockSpeed = "MaxClockSpeed";
    }
}
