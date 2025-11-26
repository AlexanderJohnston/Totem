namespace Totem;

public interface IReportQuery : IMessage
{

}

public interface IReportQuery<TRow> : IReportQuery where TRow : IReportRow
{

}
