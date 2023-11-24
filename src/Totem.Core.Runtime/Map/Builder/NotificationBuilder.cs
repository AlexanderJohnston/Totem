namespace Totem.Map.Builder;

internal sealed class NotificationBuilder : RuntimeTypeBuilder<NotificationType>
{
    internal NotificationBuilder(Type declaredType) : base(declaredType)
    { }

    internal NotificationInfo Info { get; private set; } = null!;
    internal NotificationHandlerBuilder Handler { get; set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        if(!NotificationInfo.TryFrom(DeclaredType, out var info))
        {
            SetError(BuildErrors.MessageInfoNotFound);
            return;
        }

        Info = info;
    }

    internal override void Validate()
    {
        if(Handler is null)
        {
            SetError(BuildErrors.HandlerNotFound);
        }
    }

    protected override NotificationType BuildValue() =>
        new(Info);
}
