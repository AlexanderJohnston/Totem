namespace Totem.Map.Summary;

public sealed class EventParameterSummary
{
    public EventParameterSummary(string name, Id parameterTypeId, Id eventTypeId, bool hasContext)
    {
        Name = name;
        ParameterTypeId = parameterTypeId;
        EventTypeId = eventTypeId;
        HasContext = hasContext;
    }

    public string Name { get; }
    public Id ParameterTypeId { get; }
    public Id EventTypeId { get; }
    public bool HasContext { get; }
}
