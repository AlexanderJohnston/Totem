namespace Totem.Map.Summary;

public sealed class ObserverMethodSummary
{
    public ObserverMethodSummary(EventParameterSummary parameter) =>
        Parameter = parameter;

    public EventParameterSummary Parameter { get; }
}
