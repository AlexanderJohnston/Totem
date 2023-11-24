namespace Totem.Commands;

public sealed class CommandPipelineBuilder : ICommandPipelineBuilder
{
    readonly List<ICommandMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public CommandPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public ICommandPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ICommandMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new CommandMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public ICommandPipeline Build() =>
        new CommandPipeline(_loggerFactory.CreateLogger<CommandPipeline>(), _steps, _map);
}
