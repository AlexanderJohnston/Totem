namespace Totem.Commands;

public interface ICommandPipelineBuilder
{
    ICommandPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ICommandMiddleware;

    ICommandPipeline Build();
}
