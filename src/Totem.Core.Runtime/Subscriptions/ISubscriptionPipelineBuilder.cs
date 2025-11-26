namespace Totem.Subscriptions;

public interface ISubscriptionPipelineBuilder
{
    ISubscriptionPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ISubscriptionMiddleware;

    ISubscriptionPipeline Build();
}
