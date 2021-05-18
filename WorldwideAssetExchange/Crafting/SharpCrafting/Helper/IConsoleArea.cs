using System.Threading;
using System.Threading.Tasks;

namespace SharpCrafting.Helper
{
    public interface IConsoleArea
    {
        Task NavigateAsync(CancellationToken cancellationToken);
    }
}