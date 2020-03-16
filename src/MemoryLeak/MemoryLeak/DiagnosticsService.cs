using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryLeak
{
    public class DiagnosticsService : IHostedService
    {
        private readonly ILogger<DiagnosticsService> _logger;
        private readonly Diagnostics _diagnostics;

        public DiagnosticsService(ILogger<DiagnosticsService> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _diagnostics = new Diagnostics(loggerFactory);
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
