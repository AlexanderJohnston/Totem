namespace Totem.Queries;

public interface IReportListQueryMiddleware
{
    Task InvokeAsync(IReportListQueryContext<IReportListQuery> context, Func<Task> next, CancellationToken cancellationToken);
}
