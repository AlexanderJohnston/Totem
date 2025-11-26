namespace Totem.Map;

public sealed class EventParameter : RuntimeMethodParameter
{
    internal EventParameter(ParameterInfo info, EventType message, bool hasContext) : base(info, message) =>
        HasContext = hasContext;

    public new EventType Message => (EventType) base.Message;
    public bool HasContext { get; }

    internal Expression ToArgument(ParameterExpression contextParameter)
    {
        if(HasContext)
        {
            // (TContext) context

            return Expression.Convert(contextParameter, Info.ParameterType);
        }

        // ((IEventContext<TCommand>) context).Event

        var contextType = typeof(IEventContext<>).MakeGenericType(Info.ParameterType);
        var castContext = Expression.Convert(contextParameter, contextType);

        return Expression.Property(castContext, nameof(IEventContext<IEvent>.Event));
    }
}
