namespace Totem.Events;

public interface IEventPipelineBuilder
{
    IEventPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IEventMiddleware;

    IEventPipeline Build();
}
