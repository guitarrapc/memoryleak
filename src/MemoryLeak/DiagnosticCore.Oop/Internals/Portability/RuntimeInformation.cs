using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Portability.Cpu;
using Microsoft.Win32;
using static System.Runtime.InteropServices.RuntimeInformation;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;
using DiagnosticCore.Internals.Extensions;
using DiagnosticCore.Internals.Helpers;
using DiagnosticCore.Internals.Portability.Cpu;

namespace DiagnosticCore.Portability
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/2ba30330ec5ec8a9abe284c927277a6fe0fc5a0c/src/BenchmarkDotNet/Portability/RuntimeInformation.cs
    internal static class RuntimeInformation
    {
        private const string DebugConfigurationName = "DEBUG";
        internal const string ReleaseConfigurationName = "RELEASE";
        internal const string Unknown = "?";

        public static bool IsMono { get; } = Type.GetType("Mono.Runtime") != null; // it allocates a lot of memory, we need to check it once in order to keep Engine non-allocating!

        public static bool IsFullFramework => FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);

        public static bool IsNetNative => FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase);

        public static bool IsNetCore => FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(typeof(object).Assembly.Location);

        /// <summary>
        /// "The north star for CoreRT is to be a flavor of .NET Core" -> CoreRT reports .NET Core everywhere
        /// </summary>
        public static bool IsCoreRT
            => FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase)
               && string.IsNullOrEmpty(typeof(object).Assembly.Location); // but it's merged to a single .exe and .Location returns null here ;)

        public static bool IsRunningInContainer => string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");

        internal static string ExecutableExtension => IsWindows() ? ".exe" : string.Empty;

        internal static string ScriptFileExtension => IsWindows() ? ".bat" : ".sh";

        internal static string GetArchitecture() => GetCurrentPlatform().ToString();

        internal static bool IsWindows() => IsOSPlatform(OSPlatform.Windows);

        internal static bool IsLinux() => IsOSPlatform(OSPlatform.Linux);

        internal static bool IsMacOSX() => IsOSPlatform(OSPlatform.OSX);

        public static string GetOsVersion()
        {
            if (IsMacOSX())
            {
                string systemVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("System Version") ?? "";
                string kernelVersion = ExternalToolsHelper.MacSystemProfilerData.Value.GetValueOrDefault("Kernel Version") ?? "";
                if (!string.IsNullOrEmpty(systemVersion) && !string.IsNullOrEmpty(kernelVersion))
                    return OsBrandStringHelper.PrettifyMacOSX(systemVersion, kernelVersion);
            }

            return OsBrandStringHelper.Prettify(
                RuntimeEnvironment.OperatingSystem,
                RuntimeEnvironment.OperatingSystemVersion,
                GetWindowsUbr());
        }

        // TODO: Introduce a common util API for registry calls, use it also in BenchmarkDotNet.Toolchains.CsProj.GetCurrentVersionBasedOnWindowsRegistry
        /// <summary>
        /// On Windows, this method returns UBR (Update Build Revision) based on Registry.
        /// Returns null if the value is not available
        /// </summary>
        /// <returns></returns>
        private static int? GetWindowsUbr()
        {
            if (IsWindows())
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                    using (var ndpKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (ndpKey == null)
                            return null;

                        return Convert.ToInt32(ndpKey.GetValue("UBR"));
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }

        internal static CpuInfo GetCpuInfo()
        {
            if (IsWindows() && IsFullFramework && !IsMono)
                return MosCpuInfoProvider.MosCpuInfo.Value;
            if (IsWindows())
                return WmicCpuInfoProvider.WmicCpuInfo.Value;
            if (IsLinux())
                return ProcCpuInfoProvider.ProcCpuInfo.Value;
            if (IsMacOSX())
                return SysctlCpuInfoProvider.SysctlCpuInfo.Value;

            return null;
        }

        internal static string GetRuntimeVersion()
        {
            if (IsMono)
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                {
                    string version = monoDisplayName.Invoke(null, null)?.ToString();
                    if (version != null)
                    {
                        int bracket1 = version.IndexOf('('), bracket2 = version.IndexOf(')');
                        if (bracket1 != -1 && bracket2 != -1)
                        {
                            string comment = version.Substring(bracket1 + 1, bracket2 - bracket1 - 1);
                            var commentParts = comment.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (commentParts.Length > 2)
                                version = version.Substring(0, bracket1) + "(" + commentParts[0] + " " + commentParts[1] + ")";
                        }
                    }

                    return "Mono " + version;
                }
            }
            else if (IsFullFramework)
            {
                return FrameworkVersionHelper.GetFrameworkDescription();
            }
            else if (IsNetCore)
            {
                string runtimeVersion = Internals.Environments.Runtimes.CoreRuntime.TryGetVersion(out var version) ? version.ToString() : "?";

                var coreclrAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).GetTypeInfo().Assembly.Location);
                var corefxAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(Regex).GetTypeInfo().Assembly.Location);

                return $".NET Core {runtimeVersion} (CoreCLR {coreclrAssemblyInfo.FileVersion}, CoreFX {corefxAssemblyInfo.FileVersion})";
            }
            else if (IsCoreRT)
            {
                return FrameworkDescription.Replace("Core ", "CoreRT ");
            }

            return Unknown;
        }

        internal static Runtime GetCurrentRuntime()
        {
            //do not change the order of conditions because it may cause incorrect determination of runtime
            if (IsMono)
                return MonoRuntime.Default;
            if (IsFullFramework)
                return Internals.Environments.Runtimes.ClrRuntime.GetCurrentVersion();
            if (IsNetCore)
                return Internals.Environments.Runtimes.CoreRuntime.GetCurrentVersion();
            if (IsCoreRT)
                return Internals.Environments.Runtimes.CoreRtRuntime.GetCurrentVersion();

            throw new NotSupportedException("Unknown .NET Runtime");
        }

        public static Platform GetCurrentPlatform()
        {
            switch (ProcessArchitecture)
            {
                case Architecture.Arm:
                    return Platform.Arm;
                case Architecture.Arm64:
                    return Platform.Arm64;
                case Architecture.X64:
                    return Platform.X64;
                case Architecture.X86:
                    return Platform.X86;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool Is64BitPlatform() => IntPtr.Size == 8;

        internal static bool HasRyuJit()
        {
            if (IsMono)
                return false;
            if (IsNetCore)
                return true;

            return Is64BitPlatform()
                   && GetConfiguration() != DebugConfigurationName
                   && !new JitHelper().IsMsX64();
        }

        internal static Jit GetCurrentJit() => HasRyuJit() ? Jit.RyuJit : Jit.LegacyJit;

        internal static string GetJitInfo()
        {
            if (IsCoreRT || IsNetNative)
                return "AOT";
            if (IsMono)
                return ""; // There is no helpful information about JIT on Mono
            if (IsNetCore || HasRyuJit()) // CoreCLR supports only RyuJIT
                return "RyuJIT";
            if (IsFullFramework)
                return "LegacyJIT";

            return Unknown;
        }

        internal static IntPtr GetCurrentAffinity() => Process.GetCurrentProcess().TryGetAffinity() ?? default;

        internal static string GetConfiguration()
        {
            var isDebug = Assembly.GetEntryAssembly().IsDebug();
            if (isDebug.HasValue == false)
            {
                return Unknown;
            }
            return isDebug.Value ? DebugConfigurationName : ReleaseConfigurationName;
        }

        // See http://aakinshin.net/en/blog/dotnet/jit-version-determining-in-runtime/
        private class JitHelper
        {
            private int bar;

            public bool IsMsX64(int step = 1)
            {
                int value = 0;
                for (int i = 0; i < step; i++)
                {
                    bar = i + 10;
                    for (int j = 0; j < 2 * step; j += step)
                        value = j + 10;
                }
                return value == 20 + step;
            }
        }

        private class JitModule
        {
            public string Name { get; }
            public string Version { get; }

            public JitModule(string name, string version)
            {
                Name = name;
                Version = version;
            }
        }

        internal static ICollection<Antivirus> GetAntivirusProducts()
        {
            var products = new List<Antivirus>();
            if (IsWindows())
            {
                try
                {
                    using (var wmi = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
                    using (var data = wmi.Get())
                        foreach (var o in data)
                        {
                            var av = (ManagementObject)o;
                            if (av != null)
                            {
                                string name = av["displayName"].ToString();
                                string path = av["pathToSignedProductExe"].ToString();
                                products.Add(new Antivirus(name, path));
                            }
                        }
                }
                catch
                {
                    // Never mind
                }
            }

            return products;
        }

        internal static VirtualMachineHypervisor GetVirtualMachineHypervisor()
        {
            VirtualMachineHypervisor[] hypervisors = { HyperV.Default, VirtualBox.Default, VMware.Default };

            if (IsWindows())
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                    {
                        using (var items = searcher.Get())
                        {
                            foreach (var item in items)
                            {
                                string manufacturer = item["Manufacturer"]?.ToString();
                                string model = item["Model"]?.ToString();
                                return hypervisors.FirstOrDefault(x => x.IsVirtualMachine(manufacturer, model));
                            }
                        }
                    }
                }
                catch
                {
                    // Never mind
                }
            }

            return null;
        }
    }
}
