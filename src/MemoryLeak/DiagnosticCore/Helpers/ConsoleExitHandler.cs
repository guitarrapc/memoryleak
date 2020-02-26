using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace DiagnosticCore.Helpers
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/cf3f8c382a6aab8fbc5c7ce804ef7451a4fdfac7/src/BenchmarkDotNet/Helpers/ConsoleExitHandler.cs
    internal class ConsoleExitHandler : IDisposable
    {
        private readonly Process process;
        private readonly ILogger logger;

        internal ConsoleExitHandler(Process process, ILogger logger)
        {
            this.process = process;
            this.logger = logger;

            Attach();
        }

        public void Dispose() => Detach();

        private void Attach()
        {
            process.Exited += ProcessOnExited;
            Console.CancelKeyPress += CancelKeyPressHandlerCallback;
            AppDomain.CurrentDomain.ProcessExit += ProcessExitEventHandlerHandlerCallback;
        }

        private void Detach()
        {
            process.Exited -= ProcessOnExited;
            Console.CancelKeyPress -= CancelKeyPressHandlerCallback;
            AppDomain.CurrentDomain.ProcessExit -= ProcessExitEventHandlerHandlerCallback;
        }

        // the process has exited, so we detach the events
        private void ProcessOnExited(object sender, EventArgs e) => Detach();

        // the user has clicked Ctrl+C so we kill the entire process tree
        private void CancelKeyPressHandlerCallback(object sender, ConsoleCancelEventArgs e) => KillProcessTree();

        // the user has closed the console window so we kill the entire process tree
        private void ProcessExitEventHandlerHandlerCallback(object sender, EventArgs e) => KillProcessTree();

        private void KillProcessTree()
        {
            try
            {
                logger.Flush(); // Save log to file as soon as possible. Without it, the file log will be empty if the process has already died.

                process.KillTree(); // we need to kill entire process tree, not just the process itself
            }
            catch
            {
                // we don't care about exceptions here, it's shutdown and we just try to cleanup whatever we can
            }
        }
    }
}
