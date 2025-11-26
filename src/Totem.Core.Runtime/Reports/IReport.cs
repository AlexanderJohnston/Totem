namespace Totem.Reports;

public interface IReport : IEventObserver
{
    Type RowType { get; }
    IReportRow Row { get; }
}

public interface IReport<out TRow> : IReport
    where TRow : IReportRow
{
    new TRow Row { get; }
}
