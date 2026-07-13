using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quantum.Web.Identity;
using Quantum.Web.IdentityTracking;

namespace Quantum.Web.Controllers
{
  [ApiController]
  [Route("api/auth")]
  public sealed class AuthController : ControllerBase
  {
    readonly IApplicationUserManager _users;
    readonly IInteractionIdentityResolver _identity;
    readonly ICsrfTokenService _csrfTokens;
    readonly IOptions<CsrfOptions> _csrfOptions;

    public AuthController(
      IApplicationUserManager users,
      IInteractionIdentityResolver identity,
      ICsrfTokenService csrfTokens,
      IOptions<CsrfOptions> csrfOptions)
    {
      _users = users;
      _identity = identity;
      _csrfTokens = csrfTokens;
      _csrfOptions = csrfOptions;
    }

    [HttpGet("csrf")]
    public ActionResult<CsrfTokenResponse> Csrf()
    {
      var token = _csrfTokens.CreateToken();

      _csrfTokens.SetTokenCookie(Response, token);
      Response.Headers["Cache-Control"] = "no-store";

      return Ok(new CsrfTokenResponse(token, _csrfOptions.Value.HeaderName));
    }

    [HttpPost("register")]
    public async Task<ActionResult<InteractionSession>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
      var result = await _users.RegisterAsync(request?.UserName, request?.DisplayName, request?.Password, cancellationToken).ConfigureAwait(false);

      if(!result.Succeeded)
      {
        return AuthFailure(result);
      }

      await SignInAsync(result.User).ConfigureAwait(false);
      Response.Headers["Cache-Control"] = "no-store";

      return Ok(_identity.Resolve(HttpContext.User));
    }

    [HttpPost("login")]
    public async Task<ActionResult<InteractionSession>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
      var result = await _users.LoginAsync(request?.UserName, request?.Password, cancellationToken).ConfigureAwait(false);

      if(!result.Succeeded)
      {
        return AuthFailure(result);
      }

      await SignInAsync(result.User).ConfigureAwait(false);
      Response.Headers["Cache-Control"] = "no-store";

      return Ok(_identity.Resolve(HttpContext.User));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<InteractionSession>> Logout()
    {
      await HttpContext.SignOutAsync(InteractionAuthDefaults.AuthenticationScheme).ConfigureAwait(false);
      Response.Headers["Cache-Control"] = "no-store";

      return Ok(_identity.Resolve(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    async Task SignInAsync(ApplicationUser user)
    {
      var principal = CreatePrincipal(user);
      var properties = new AuthenticationProperties
      {
        AllowRefresh = true,
        IsPersistent = false
      };

      await HttpContext.SignInAsync(InteractionAuthDefaults.AuthenticationScheme, principal, properties).ConfigureAwait(false);
      HttpContext.User = principal;
    }

    static ClaimsPrincipal CreatePrincipal(ApplicationUser user)
    {
      var claims = new List<Claim>
      {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.DisplayName),
        new(InteractionAuthClaims.UserName, user.UserName),
        new(InteractionAuthClaims.ProcessUserId, user.ProcessUserId),
        new(InteractionAuthClaims.TrackingSource, InteractionTrackingSources.BackendCookie)
      };

      return new ClaimsPrincipal(new ClaimsIdentity(claims, InteractionAuthDefaults.AuthenticationScheme));
    }

    ActionResult<InteractionSession> AuthFailure(ApplicationUserResult result) =>
      result.Status switch
      {
        ApplicationUserResultStatus.ValidationFailed => BadRequest(new AuthErrorResponse("Validation failed.", result.Errors)),
        ApplicationUserResultStatus.DuplicateUser => Conflict(new AuthErrorResponse("Registration could not be completed for that username.")),
        ApplicationUserResultStatus.InvalidCredentials => Unauthorized(new AuthErrorResponse("Invalid username or password.")),
        _ => BadRequest(new AuthErrorResponse("Authentication request could not be completed."))
      };
  }

  public sealed class RegisterRequest
  {
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string Password { get; set; }
  }

  public sealed class LoginRequest
  {
    public string UserName { get; set; }
    public string Password { get; set; }
  }

  public sealed record CsrfTokenResponse(string Token, string HeaderName);

  public sealed class AuthErrorResponse
  {
    public AuthErrorResponse(string message, IReadOnlyList<string> errors = null)
    {
      Message = message;
      Errors = errors ?? System.Array.Empty<string>();
    }

    public string Message { get; }
    public IReadOnlyList<string> Errors { get; }
  }
}
