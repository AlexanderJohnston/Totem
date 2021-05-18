using System;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Patterns.Diagnostics;
using SharpCrafting.Helper;
using Totem;
using static PostSharp.Patterns.Diagnostics.FormattedMessageBuilder;

namespace SharpCrafting.Areas.Home
{
    public class HomeArea : IConsoleArea
    {
        private readonly LogSource _log = LogSource.Get ().WithLevels ( LogLevel.Trace, LogLevel.Info );

        readonly ITotemClient _totemClient;

        public HomeArea(ITotemClient totemClient)
        {
            _totemClient = totemClient ?? throw new ArgumentNullException(nameof(totemClient));
        }

        public async Task NavigateAsync(CancellationToken cancellationToken)
        {
            //await _totemClient.SendAsync(new InstallVersion { ZipUrl = zipUrl }, cancellationToken);
            _log.Info.Write(Formatted("Totem reached the Home Area."));
        }
    }
}