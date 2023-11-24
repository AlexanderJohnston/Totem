namespace Totem.Map;

public sealed class TopicWhenMethod : RuntimeMethod
{
    delegate void CompiledCall(ITopic topic, ITopicContext<ICommand> context);
    delegate Task CompiledCallAsync(ITopic topic, ITopicContext<ICommand> context, CancellationToken cancellationToken);

    readonly CompiledCall _call = null!;
    readonly CompiledCallAsync _callAsync = null!;

    internal TopicWhenMethod(MethodInfo info, CommandParameter parameter, bool isAsync, bool hasCancellationToken) : base(info, parameter)
    {
        IsAsync = isAsync;
        HasCancellationToken = hasCancellationToken;

        // (topic, context) => ((TTopic) topic).When(<argument>[, cancellationToken])

        var topicParameter = Expression.Parameter(typeof(ITopic), "topic");
        var contextParameter = Expression.Parameter(typeof(ITopicContext<ICommand>), "context");

        var topicCast = Expression.Convert(topicParameter, info.DeclaringType!);
        var contextArgument = parameter.ToArgument(contextParameter);
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        if(!isAsync)
        {
            var callWhen = Expression.Call(topicCast, info, contextArgument);

            _call = Expression.Lambda<CompiledCall>(callWhen, topicParameter, contextParameter).Compile();
        }
        else
        {
            var callWhen = hasCancellationToken
                ? Expression.Call(topicCast, info, contextArgument, cancellationTokenParameter)
                : Expression.Call(topicCast, info, contextArgument);

            _callAsync = Expression.Lambda<CompiledCallAsync>(callWhen, topicParameter, contextParameter, cancellationTokenParameter).Compile();
        }
    }

    public new CommandParameter Parameter => (CommandParameter) base.Parameter;
    public bool IsAsync { get; }
    public bool HasCancellationToken { get; }

    internal async Task CallAsync(ITopic topic, ITopicContext<ICommand> context, CancellationToken cancellationToken)
    {
        if(_callAsync is not null)
        {
            await _callAsync(topic, context, cancellationToken);
        }
        else
        {
            _call!(topic, context);
        }

        foreach(var error in topic.Errors)
        {
            context.Errors.Add(error);
        }
    }
}
