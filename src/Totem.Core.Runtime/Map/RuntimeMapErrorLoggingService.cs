namespace Totem.Map;

public sealed class RuntimeMapErrorLoggingService : IHostedService
{
    readonly ILogger _logger;
    readonly RuntimeMap _map;

    public RuntimeMapErrorLoggingService(ILogger<RuntimeMapErrorLoggingService> logger, RuntimeMap map)
    {
        _logger = logger;
        _map = map;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach(var error in _map.Errors)
        {
            if(error.Details is null)
            {
                _logger.LogError("{ErrorName:l} in runtime map at {Input:l}", error.Info.Name, error.Input);
            }
            else
            {
                _logger.LogError("{ErrorName:l} in runtime map at {Input:l} (details: {@Details:l})", error.Info.Name, error.Input, error.Details);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
