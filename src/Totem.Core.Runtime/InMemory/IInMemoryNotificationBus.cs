namespace Totem.InMemory;

public interface IInMemoryNotificationBus
{
    void Publish(NotificationEnvelope notification);
}
