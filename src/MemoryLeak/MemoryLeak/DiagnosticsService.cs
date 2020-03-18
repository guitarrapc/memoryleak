using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemoryLeak
{
    public class DiagnosticsService : IHostedService
    {
        private readonly ILogger<DiagnosticsService> _logger;
        private readonly Diagnostics _diagnostics;

        public DiagnosticsService(IConfiguration config, ILogger<DiagnosticsService> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            var datadogHostAddress = config.GetValue("DD_NODE_HOST", "127.0.0.1");
            _diagnostics = new Diagnostics(datadogHostAddress, loggerFactory);
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
