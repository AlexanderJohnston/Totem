namespace Totem.Queries;

public interface IReportQueryMiddleware
{
    Task InvokeAsync(IReportQueryContext<IReportQuery> context, Func<Task> next, CancellationToken cancellationToken);
}
