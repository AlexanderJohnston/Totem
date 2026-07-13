using System.Threading;
using System.Threading.Tasks;

namespace Quantum.Web.Identity
{
  public interface IApplicationUserStore
  {
    Task<ApplicationUser> FindByNormalizedUserNameAsync(string normalizedUserName, CancellationToken cancellationToken);
    Task<bool> TryCreateAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken);
  }
}
