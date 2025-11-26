using Totem.Reports;

namespace Totem;

public interface IHttpReportListQuery : IHttpQuery, IReportListQuery
{

}

public interface IHttpReportListQuery<TRow> : IHttpReportListQuery, IReportListQuery<TRow>
    where TRow : IReportRow
{

}
