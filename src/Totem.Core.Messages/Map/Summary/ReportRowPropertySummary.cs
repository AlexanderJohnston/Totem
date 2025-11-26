namespace Totem.Map.Summary;

public sealed class ReportRowPropertySummary
{
    public ReportRowPropertySummary(string name, Id valueTypeId)
    {
        Name = name;
        ValueTypeId = valueTypeId;
    }

    public string Name { get; }
    public Id ValueTypeId { get; }
}
