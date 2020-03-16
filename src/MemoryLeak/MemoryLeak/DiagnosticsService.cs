using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryLeak
{
    public class DiagnosticsService : IHostedService
    {
        private readonly ILogger<DiagnosticsService> _logger;
        private readonly ProfilerDiagnostics _diagnostics;

        public DiagnosticsService(ILogger<DiagnosticsService> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _diagnostics = new ProfilerDiagnostics(loggerFactory);
            _diagnostics.EnableTracker();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _diagnostics.StartTracker();
            return Task.CompletedTask;   
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _diagnostics.StopTracker();
            return Task.CompletedTask;
        }
    }
}
