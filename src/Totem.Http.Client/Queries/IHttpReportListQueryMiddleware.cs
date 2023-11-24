namespace Totem.Queries;

public interface IHttpReportListQueryMiddleware
{
    Task InvokeAsync(IHttpReportListQueryContext<IHttpReportListQuery> context, Func<Task> next, CancellationToken cancellationToken);
}
