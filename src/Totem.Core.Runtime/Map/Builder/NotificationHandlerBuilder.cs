namespace Totem.Map.Builder;

internal sealed class NotificationHandlerBuilder : RuntimeTypeBuilder<NotificationHandlerType>
{
    internal NotificationHandlerBuilder(Type declaredType) : base(declaredType)
    { }

    internal NotificationBuilder Notification { get; private set; } = null!;
    internal Type ServiceType { get; private set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var notificationType = DeclaredType.GetImplementedInterfaceGenericArguments(typeof(INotificationHandler<>)).Single();

        if(!map.Messages.Notifications.TryGet(notificationType, out var notification))
        {
            SetError(BuildErrors.HandlerMessageNotFound, new { notificationType });
            return;
        }

        if(notification.Handler is not null)
        {
            SetError(BuildErrors.HandlerDuplicated, new { notification.Handler });
            return;
        }

        Notification = notification;
        Notification.Handler = this;
        ServiceType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
    }

    internal override void Validate()
    {
        if(Notification is null)
        {
            SetError(BuildErrors.HandlerMessageHasError);
        }
    }

    protected override NotificationHandlerType BuildValue()
    {
        var handler = new NotificationHandlerType(DeclaredType, ServiceType, Notification.Value);

        Notification.Value.Handler = handler;

        return handler;
    }
        
}
