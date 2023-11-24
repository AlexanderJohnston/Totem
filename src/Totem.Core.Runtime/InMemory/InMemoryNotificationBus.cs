namespace Totem.InMemory;

public sealed class InMemoryNotificationBus : IInMemoryNotificationBus, IDisposable
{
    readonly ConcurrentDictionary<Type, InMemoryNotificationQueue> _queuesByType = new();
    readonly ILoggerFactory _loggerFactory;
    readonly INotificationPipeline _pipeline;

    public InMemoryNotificationBus(ILoggerFactory loggerFactory, INotificationPipeline pipeline)
    {
        _loggerFactory = loggerFactory;
        _pipeline = pipeline;
    }

    public void Publish(NotificationEnvelope notification) =>
        _queuesByType.AddOrUpdate(
            notification.NotificationType,
            type =>
            {
                var queue = new InMemoryNotificationQueue(_loggerFactory.CreateLogger<InMemoryNotificationQueue>(), type, _pipeline);

                queue.Enqueue(notification);

                return queue;
            },
            (type, queue) =>
            {
                queue.Enqueue(notification);

                return queue;
            });

    public void Dispose()
    {
        foreach(var queue in _queuesByType.Values)
        {
            queue.Dispose();
        }
    }
}
