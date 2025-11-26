using Totem.Reports;

namespace Totem;

public interface IHttpReportQuery : IHttpQuery, IReportQuery
{

}

public interface IHttpReportQuery<TRow> : IHttpReportQuery, IReportQuery<TRow>
    where TRow : IReportRow
{

}
