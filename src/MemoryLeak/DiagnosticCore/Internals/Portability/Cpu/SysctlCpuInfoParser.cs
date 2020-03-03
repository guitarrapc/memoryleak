using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Portability.Cpu;
using DiagnosticCore.Internals.Helpers;

namespace DiagnosticCore.Internals.Portability.Cpu
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/2ba30330ec5ec8a9abe284c927277a6fe0fc5a0c/src/BenchmarkDotNet/Portability/Cpu/SysctlCpuInfoParser.cs
    internal static class SysctlCpuInfoParser
    {
        internal static CpuInfo ParseOutput(string content)
        {
            var sysctl = SectionsHelper.ParseSection(content, ':');
            string processorName = sysctl.GetValueOrDefault("machdep.cpu.brand_string");
            var physicalProcessorCount = GetPositiveIntValue(sysctl, "hw.packages");
            var physicalCoreCount = GetPositiveIntValue(sysctl, "hw.physicalcpu");
            var logicalCoreCount = GetPositiveIntValue(sysctl, "hw.logicalcpu");
            var nominalFrequency = GetPositiveLongValue(sysctl, "hw.cpufrequency");
            var minFrequency = GetPositiveLongValue(sysctl, "hw.cpufrequency_min");
            var maxFrequency = GetPositiveLongValue(sysctl, "hw.cpufrequency_max");
            return new CpuInfo(processorName, physicalProcessorCount, physicalCoreCount, logicalCoreCount, nominalFrequency, minFrequency, maxFrequency);
        }

        private static int? GetPositiveIntValue(Dictionary<string, string> sysctl, string keyName)
        {
            if (sysctl.TryGetValue(keyName, out string value) &&
                int.TryParse(value, out int result) &&
                result > 0)
                return result;
            return null;
        }

        private static long? GetPositiveLongValue(Dictionary<string, string> sysctl, string keyName)
        {
            if (sysctl.TryGetValue(keyName, out string value) &&
                long.TryParse(value, out long result) &&
                result > 0)
                return result;
            return null;
        }
    }
}
