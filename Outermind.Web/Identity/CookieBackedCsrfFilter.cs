using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Quantum.Web.Controllers;
using Quantum.Web.IdentityTracking;

namespace Quantum.Web.Identity
{
  public sealed class CookieBackedCsrfFilter : IAsyncActionFilter
  {
    readonly ICsrfTokenService _tokens;
    readonly HashSet<string> _allowedOrigins;

    public CookieBackedCsrfFilter(ICsrfTokenService tokens, IConfiguration configuration)
    {
      _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
      _allowedOrigins = GetAllowedOrigins(configuration);
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
      var request = context.HttpContext.Request;

      if(!IsUnsafeMethod(request.Method))
      {
        await next().ConfigureAwait(false);
        return;
      }

      if(!IsAcceptedOrigin(request.Scheme, request.Host.Value, request.Headers.Origin, request.Headers.Referer))
      {
        context.Result = new StatusCodeResult(403);
        return;
      }

      if(IsBackendCookiePrincipal(context.HttpContext.User) && !_tokens.IsRequestTokenValid(request))
      {
        context.Result = new BadRequestObjectResult(new AuthErrorResponse("CSRF token is required for this request."));
        return;
      }

      await next().ConfigureAwait(false);
    }

    bool IsAcceptedOrigin(string requestScheme, string requestHost, StringValues originHeader, StringValues refererHeader)
    {
      var origin = FirstNonBlank(originHeader) ?? OriginFromReferer(FirstNonBlank(refererHeader));

      if(string.IsNullOrWhiteSpace(origin))
      {
        return true;
      }

      if(!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
      {
        return false;
      }

      var requestOrigin = $"{requestScheme}://{requestHost}";

      return string.Equals(originUri.GetLeftPart(UriPartial.Authority), requestOrigin, StringComparison.OrdinalIgnoreCase)
        || _allowedOrigins.Contains(originUri.GetLeftPart(UriPartial.Authority));
    }

    static bool IsUnsafeMethod(string method) =>
      string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
      || string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase)
      || string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase)
      || string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase);

    static bool IsBackendCookiePrincipal(ClaimsPrincipal principal) =>
      principal?.Identity?.IsAuthenticated == true
      && string.Equals(
        principal.FindFirstValue(InteractionAuthClaims.TrackingSource),
        InteractionTrackingSources.BackendCookie,
        StringComparison.OrdinalIgnoreCase);

    static string FirstNonBlank(StringValues values) =>
      values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    static string OriginFromReferer(string referer)
    {
      if(!Uri.TryCreate(referer, UriKind.Absolute, out var uri))
      {
        return null;
      }

      return uri.GetLeftPart(UriPartial.Authority);
    }

    static HashSet<string> GetAllowedOrigins(IConfiguration configuration)
    {
      var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? SplitOriginList(configuration["Cors:AllowedOrigins"]);

      return new HashSet<string>(
        origins.Where(origin => !string.IsNullOrWhiteSpace(origin)).Select(origin => origin.Trim().TrimEnd('/')),
        StringComparer.OrdinalIgnoreCase);
    }

    static string[] SplitOriginList(string configuredOriginList) =>
      string.IsNullOrWhiteSpace(configuredOriginList)
        ? Array.Empty<string>()
        : configuredOriginList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }
}
