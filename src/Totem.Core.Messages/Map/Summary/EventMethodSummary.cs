namespace Totem.Map.Summary;

public sealed class EventMethodSummary
{
    public EventMethodSummary(EventParameterSummary parameter) =>
        Parameter = parameter;

    public EventParameterSummary Parameter { get; }
}
