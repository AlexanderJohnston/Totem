using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PostSharp.Patterns.Diagnostics;
using PostSharp.Patterns.Model;
using PostSharp.Patterns.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PostSharp.Patterns.Diagnostics.FormattedMessageBuilder;

namespace SharpCrafting
{
    [Actor]
    public class EventSatteliteService : IHostedService
    {
        [Reference] private GenericPlatform _platform;
        [Reference] private readonly LogSource _log = LogSource.Get()
            .WithLevels(PostSharp.Patterns.Diagnostics.LogLevel.Trace,
            PostSharp.Patterns.Diagnostics.LogLevel.Warning);

        [Child]
        private INativeClass _sharedMemory { get; set; }

        public EventSatteliteService (IOptions<AppConfig> options)
        {
            _platform = options.Value.Platform;
        }

        [Reentrant]
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _log.Info.Write ( Formatted( "[Event Sattelite Service]: Calling out to the platform for event sattelite to receive launchpad events." ) ) ;
            _sharedMemory = (INativeClass)_platform.GetNativeClass("SharpCrafting", "EventSattelite");
            await _sharedMemory.Initialize(this);
        }

        [Reentrant]
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _log.Warning.Write(Formatted("[Event Sattelite Service]: Terminating this service."));
            await _sharedMemory.Terminate("The launchpad event service is being shut down by the host.");
        }
    }
}
