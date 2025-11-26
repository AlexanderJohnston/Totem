namespace Totem;

public interface IReportListQuery : IMessage
{

}

public interface IReportListQuery<TRow> : IReportListQuery where TRow : IReportRow
{

}
