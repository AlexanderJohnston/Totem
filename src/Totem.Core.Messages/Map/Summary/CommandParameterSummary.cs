namespace Totem.Map.Summary;

public sealed class CommandParameterSummary
{
    public CommandParameterSummary(string name, Id parameterTypeId, Id commandTypeId, bool hasContext)
    {
        Name = name;
        ParameterTypeId = parameterTypeId;
        CommandTypeId = commandTypeId;
        HasContext = hasContext;
    }

    public string Name { get; }
    public Id ParameterTypeId { get; }
    public Id CommandTypeId { get; }
    public bool HasContext { get; }
}
