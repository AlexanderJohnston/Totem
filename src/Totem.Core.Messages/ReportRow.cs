namespace Totem;

public abstract class ReportRow : IReportRow, IReportRowInit
{
    public Id Id { get; set; } = null!;

    Id IReportRowInit.Id { set => Id = value; }
}
