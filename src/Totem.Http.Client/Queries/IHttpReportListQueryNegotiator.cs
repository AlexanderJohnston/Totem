namespace Totem.Queries;

public interface IHttpReportListQueryNegotiator
{
    HttpRequestMessage Negotiate(IHttpReportListQueryContext<IHttpReportListQuery> context);
    void NegotiateResult(IHttpReportListQueryContext<IHttpReportListQuery> context);
}
