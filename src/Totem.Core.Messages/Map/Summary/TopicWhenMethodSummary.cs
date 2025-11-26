namespace Totem.Map.Summary;

public sealed class TopicWhenMethodSummary
{
    public TopicWhenMethodSummary(CommandParameterSummary parameter, bool isAsync, bool hasCancellationToken)
    {
        Parameter = parameter;
        IsAsync = isAsync;
        HasCancellationToken = hasCancellationToken;
    }

    public CommandParameterSummary Parameter { get; }
    public bool IsAsync { get; }
    public bool HasCancellationToken { get; }
}
