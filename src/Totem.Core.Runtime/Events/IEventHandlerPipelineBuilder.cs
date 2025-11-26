namespace Totem.Events;

public interface IEventHandlerPipelineBuilder
{
    IEventHandlerPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IEventHandlerMiddleware;

    IEventHandlerPipeline Build();
}
