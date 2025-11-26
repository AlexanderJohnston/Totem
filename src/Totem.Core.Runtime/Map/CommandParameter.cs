namespace Totem.Map;

public sealed class CommandParameter : RuntimeMethodParameter
{
    internal CommandParameter(ParameterInfo info, CommandType message, bool hasContext) : base(info, message) =>
        HasContext = hasContext;

    public new CommandType Message => (CommandType) base.Message;
    public bool HasContext { get; }

    internal Expression ToArgument(ParameterExpression contextParameter)
    {
        if(HasContext)
        {
            // (TContext) context

            return Expression.Convert(contextParameter, Info.ParameterType);
        }

        // ((ICommandContext<TCommand>) context).Command

        var contextType = typeof(ICommandContext<>).MakeGenericType(Info.ParameterType);
        var castContext = Expression.Convert(contextParameter, contextType);

        return Expression.Property(castContext, nameof(ICommandContext<ICommand>.Command));
    }
}
