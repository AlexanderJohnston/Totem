namespace Totem.Queries;

public interface IHttpReportQueryNegotiator
{
    HttpRequestMessage Negotiate(IHttpReportQueryContext<IHttpReportQuery> context);
    void NegotiateResult(IHttpReportQueryContext<IHttpReportQuery> context);
}
