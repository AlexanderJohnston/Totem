namespace Totem;

public abstract class Report<TRow> : Timeline, IReport<TRow>
    where TRow : IReportRow, new()
{
    public Type RowType => typeof(TRow);
    public TRow Row { get; } = new();

    IReportRow IReport.Row => Row;
}
