using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Portability.Cpu;
using DiagnosticCore.Helpers;

namespace DiagnosticCore.Portability.Cpu
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/a78b38b0e89d04ad3fe8934162c7adb42f81eabe/src/BenchmarkDotNet/Portability/Cpu/WmicCpuInfoProvider.cs
    internal static class WmicCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> WmicCpuInfo = new Lazy<CpuInfo>(Load);

        private static CpuInfo Load()
        {
            if (RuntimeInformation.IsWindows())
            {
                string argList = $"{WmicCpuInfoKeyNames.Name}, {WmicCpuInfoKeyNames.NumberOfCores}, {WmicCpuInfoKeyNames.NumberOfLogicalProcessors}, {WmicCpuInfoKeyNames.MaxClockSpeed}";
                string content = ProcessHelper.RunAndReadOutput("wmic", $"cpu get {argList} /Format:List");
                return WmicCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}
