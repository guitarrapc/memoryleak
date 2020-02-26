using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability.Cpu;
using DiagnosticCore.Portability.Helpers;

namespace DiagnosticCore.Portability.Cpu
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/2ba30330ec5ec8a9abe284c927277a6fe0fc5a0c/src/BenchmarkDotNet/Portability/Cpu/WmicCpuInfoParser.cs
    internal static class WmicCpuInfoParser
    {
        internal static CpuInfo ParseOutput(string content)
        {
            var processors = SectionsHelper.ParseSections(content, '=');

            var processorModelNames = new HashSet<string>();
            int physicalCoreCount = 0;
            int logicalCoreCount = 0;
            int processorsCount = 0;

            var currentClockSpeed = Frequency.Zero;
            var maxClockSpeed = Frequency.Zero;
            var minClockSpeed = Frequency.Zero;

            foreach (var processor in processors)
            {
                var numberOfCores = 0;
                if (processor.TryGetValue(WmicCpuInfoKeyNames.NumberOfCores, out string numberOfCoresValue) &&
                    int.TryParse(numberOfCoresValue, out numberOfCores) &&
                    numberOfCores > 0)
                    physicalCoreCount += numberOfCores;

                var numberOfLogical = 0;
                if (processor.TryGetValue(WmicCpuInfoKeyNames.NumberOfLogicalProcessors, out string numberOfLogicalValue) &&
                    int.TryParse(numberOfLogicalValue, out numberOfLogical) &&
                    numberOfLogical > 0)
                    logicalCoreCount += numberOfLogical;

                if (processor.TryGetValue(WmicCpuInfoKeyNames.Name, out string name))
                {
                    processorModelNames.Add(name);
                    processorsCount++;
                }

                var frequency = 0;
                if (processor.TryGetValue(WmicCpuInfoKeyNames.MaxClockSpeed, out string frequencyValue)
                    && int.TryParse(frequencyValue, out frequency)
                    && frequency > 0)
                {
                    maxClockSpeed += frequency;
                }
            }

            return new CpuInfo(
                processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null,
                processorsCount > 0 ? processorsCount : (int?)null,
                physicalCoreCount > 0 ? physicalCoreCount : (int?)null,
                logicalCoreCount > 0 ? logicalCoreCount : (int?)null,
                currentClockSpeed > 0 && processorsCount > 0 ? Frequency.FromMHz(currentClockSpeed / processorsCount) : (Frequency?)null,
                minClockSpeed > 0 && processorsCount > 0 ? Frequency.FromMHz(minClockSpeed / processorsCount) : (Frequency?)null,
                maxClockSpeed > 0 && processorsCount > 0 ? Frequency.FromMHz(maxClockSpeed / processorsCount) : (Frequency?)null);
        }
    }
}
