using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability.Cpu;
using DiagnosticCore.Portability.Helpers;

namespace DiagnosticCore.Portability.Cpu
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/2ba30330ec5ec8a9abe284c927277a6fe0fc5a0c/src/BenchmarkDotNet/Portability/Cpu/ProcCpuInfoProvider.cs
    internal static class ProcCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> ProcCpuInfo = new Lazy<CpuInfo>(Load);

        private static CpuInfo Load()
        {
            if (RuntimeInformation.IsLinux())
            {
                string content = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo");
                string output = GetCpuSpeed();
                content = content + output;
                return ProcCpuInfoParser.ParseOutput(content);
            }
            return null;
        }

        private static string GetCpuSpeed()
        {
            var output = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu | grep MHz\"")?
                                      .Split('\n')
                                      .SelectMany(x => x.Split(':'))
                                      .ToArray();

            return ParseCpuFrequencies(output);
        }

        private static string ParseCpuFrequencies(string[] input)
        {
            // Example of output we trying to parse:
            //
            // CPU MHz: 949.154
            // CPU max MHz: 3200,0000
            // CPU min MHz: 800,0000
            //
            // And we don't need "CPU MHz" line
            if (input == null || input.Length < 6)
                return null;

            Frequency.TryParseMHz(input[3].Trim().Replace(',', '.'), out var minFrequency);
            Frequency.TryParseMHz(input[5].Trim().Replace(',', '.'), out var maxFrequency);

            return $"\n{ProcCpuInfoKeyNames.MinFrequency}\t:{minFrequency.ToMHz()}\n{ProcCpuInfoKeyNames.MaxFrequency}\t:{maxFrequency.ToMHz()}\n";
        }
    }
}
