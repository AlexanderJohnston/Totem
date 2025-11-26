namespace Totem.Subscriptions;

public interface ITspSubscriptionPipelineBuilder
{
    ITspSubscriptionPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ITspSubscriptionMiddleware;

    ITspSubscriptionPipeline Build();
}
