using System;
using System.Security.Claims;
using Outermind.Microfilm;
using Quantum.Web.Identity;
using Quantum.Web.IdentityTracking;

namespace Quantum.Web.ScanProcessing
{
  public interface IRegisteredScanProcessingActorResolver
  {
    bool TryResolve(ClaimsPrincipal principal, out ScanProcessingActorIdentity actor);
  }

  /// <summary>
  /// Resolves only server-issued registered-user cookie claims. Windows tracking identities
  /// without a registered application user ID are not operation actors.
  /// </summary>
  public sealed class RegisteredScanProcessingActorResolver : IRegisteredScanProcessingActorResolver
  {
    public bool TryResolve(ClaimsPrincipal principal, out ScanProcessingActorIdentity actor)
    {
      actor = null;

      if(principal?.Identity?.IsAuthenticated != true
        || !string.Equals(
          principal.FindFirstValue(InteractionAuthClaims.TrackingSource),
          InteractionTrackingSources.BackendCookie,
          StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }

      var resolved = new ScanProcessingActorIdentity(
        principal.FindFirstValue(ClaimTypes.NameIdentifier),
        principal.FindFirstValue(InteractionAuthClaims.UserName),
        principal.FindFirstValue(ClaimTypes.Name),
        principal.FindFirstValue(InteractionAuthClaims.ProcessUserId));

      if(!resolved.IsValid)
      {
        return false;
      }

      actor = resolved;
      return true;
    }

    public static ScanProcessingActorIdentity FromApplicationUser(ApplicationUser user) =>
      user == null
        ? null
        : new ScanProcessingActorIdentity(user.Id, user.UserName, user.DisplayName, user.ProcessUserId);
  }
}
