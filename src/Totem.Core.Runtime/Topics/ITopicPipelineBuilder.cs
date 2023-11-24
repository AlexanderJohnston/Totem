namespace Totem.Topics;

public interface ITopicPipelineBuilder
{
    ITopicPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ITopicMiddleware;

    ITopicPipeline Build();
}
