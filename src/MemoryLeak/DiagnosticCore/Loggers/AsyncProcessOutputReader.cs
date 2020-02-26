using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiagnosticCore.Loggers
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/1c431a9fb71141fa63479a53713029e3c3661b0b/src/BenchmarkDotNet/Loggers/AsyncProcessOutputReader.cs
    internal class AsyncProcessOutputReader : IDisposable
    {
        private readonly Process process;
        private readonly ConcurrentStack<string> output, error;

        private long status;

        internal AsyncProcessOutputReader(Process process)
        {
            if (!process.StartInfo.RedirectStandardOutput)
                throw new NotSupportedException("set RedirectStandardOutput to true first");
            if (!process.StartInfo.RedirectStandardError)
                throw new NotSupportedException("set RedirectStandardError to true first");

            this.process = process;
            output = new ConcurrentStack<string>();
            error = new ConcurrentStack<string>();
            status = (long)Status.Created;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref status, (long)Status.Disposed);

            Detach();
        }

        internal void BeginRead()
        {
            if (Interlocked.CompareExchange(ref status, (long)Status.Started, (long)Status.Created) != (long)Status.Created)
                throw new InvalidOperationException("Reader can be started only once");

            Attach();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        internal void CancelRead()
        {
            if (Interlocked.CompareExchange(ref status, (long)Status.Stopped, (long)Status.Started) != (long)Status.Started)
                throw new InvalidOperationException("Only a started reader can be stopped");

            process.CancelOutputRead();
            process.CancelErrorRead();

            Detach();
        }

        internal void StopRead()
        {
            if (Interlocked.CompareExchange(ref status, (long)Status.Stopped, (long)Status.Started) != (long)Status.Started)
                throw new InvalidOperationException("Only a started reader can be stopped");

            Detach();
        }

        internal ImmutableArray<string> GetOutputLines() => ReturnIfStopped(() => output.ToImmutableArray());

        internal ImmutableArray<string> GetErrorLines() => ReturnIfStopped(() => error.ToImmutableArray());

        internal ImmutableArray<string> GetOutputAndErrorLines() => ReturnIfStopped(() => output.Concat(error).ToImmutableArray());

        internal string GetOutputText() => ReturnIfStopped(() => string.Join(Environment.NewLine, output));

        internal string GetErrorText() => ReturnIfStopped(() => string.Join(Environment.NewLine, error));

        private void Attach()
        {
            process.OutputDataReceived += ProcessOnOutputDataReceived;
            process.ErrorDataReceived += ProcessOnErrorDataReceived;
        }

        private void Detach()
        {
            process.OutputDataReceived -= ProcessOnOutputDataReceived;
            process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                output.Push(e.Data);
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                error.Push(e.Data);
        }

        private T ReturnIfStopped<T>(Func<T> getter)
            => Interlocked.Read(ref status) == (long)Status.Stopped
                ? getter.Invoke()
                : throw new InvalidOperationException("The reader must be stopped first");

        private enum Status : long
        {
            Created,
            Started,
            Stopped,
            Disposed
        }
    }
}
