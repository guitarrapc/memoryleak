using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DiagnosticCore.Extensions
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/0b83c934e5f983ff2aa43c82e63d242216d330bf/src/BenchmarkDotNet/Extensions/AssemblyExtensions.cs
    internal static class AssemblyExtensions
    {
        internal static bool? IsJitOptimizationDisabled(this Assembly assembly)
            => GetDebuggableAttribute(assembly).IsJitOptimizerDisabled();

        internal static bool? IsDebug(this Assembly assembly)
            => GetDebuggableAttribute(assembly).IsJitTrackingEnabled();

        internal static bool IsTrue(this bool? valueOrNothing) => valueOrNothing.HasValue && valueOrNothing.Value;

        private static DebuggableAttribute GetDebuggableAttribute(Assembly assembly)
            => assembly?.GetCustomAttributes()
                .OfType<DebuggableAttribute>()
                .SingleOrDefault();

        private static bool? IsJitOptimizerDisabled(this DebuggableAttribute attribute) => attribute?.IsJITOptimizerDisabled;

        private static bool? IsJitTrackingEnabled(this DebuggableAttribute attribute) => attribute?.IsJITTrackingEnabled;
    }
}
