using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Portability.Cpu;
using DiagnosticCore.Internals.Helpers;
using DiagnosticCore.Portability;

namespace DiagnosticCore.Internals.Portability.Cpu
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/2ba30330ec5ec8a9abe284c927277a6fe0fc5a0c/src/BenchmarkDotNet/Portability/Cpu/SysctlCpuInfoProvider.cs
    internal static class SysctlCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> SysctlCpuInfo = new Lazy<CpuInfo>(Load);

        private static CpuInfo Load()
        {
            if (RuntimeInformation.IsMacOSX())
            {
                string content = ProcessHelper.RunAndReadOutput("sysctl", "-a");
                return SysctlCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}
