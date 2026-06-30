using System.Security.Claims;

namespace Quantum.Web.IdentityTracking
{
  public interface IInteractionIdentityResolver
  {
    InteractionSession Resolve(ClaimsPrincipal principal);
  }
}
