namespace Totem.Queries;

public interface IHttpReportQueryMiddleware
{
    Task InvokeAsync(IHttpReportQueryContext<IHttpReportQuery> context, Func<Task> next, CancellationToken cancellationToken);
}
