namespace Totem.Map.Builder;

internal sealed class EventBuilder : RuntimeTypeBuilder<EventType>
{
    internal EventBuilder(Type declaredType) : base(declaredType)
    { }

    internal EventInfo Info { get; private set; } = null!;
    internal EventHandlerBuilder? Handler { get; set; }

    internal override void Reflect(RuntimeMapBuilder map)
    {
        if(!EventInfo.TryFrom(DeclaredType, out var info))
        {
            SetError(BuildErrors.MessageInfoNotFound);
            return;
        }

        Info = info;
    }

    protected override EventType BuildValue() =>
        new(Info);
}
