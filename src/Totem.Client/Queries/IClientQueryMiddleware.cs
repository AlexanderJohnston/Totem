using System;
using System.Threading;
using System.Threading.Tasks;

namespace Totem.Queries
{
    public interface IClientQueryMiddleware
    {
        Task InvokeAsync(IClientQueryContext<IQuery> context, Func<Task> next, CancellationToken cancellationToken);
    }
}