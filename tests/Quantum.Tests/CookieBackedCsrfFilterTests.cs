using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Quantum.Web.Controllers;
using Quantum.Web.Identity;
using Quantum.Web.IdentityTracking;
using Xunit;

namespace Quantum.Tests
{
  public sealed class CookieBackedCsrfFilterTests
  {
    [Fact]
    public async Task AuthenticatedBackendCookieUnsafeRequest_RequiresCsrfHeader()
    {
      var filter = CreateFilter();
      var context = CreateActionContext();
      context.HttpContext.Request.Method = "POST";
      context.HttpContext.User = BackendCookiePrincipal();

      await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

      var result = Assert.IsType<BadRequestObjectResult>(context.Result);
      var error = Assert.IsType<AuthErrorResponse>(result.Value);
      Assert.Equal("CSRF token is required for this request.", error.Message);
    }

    [Fact]
    public async Task AuthenticatedBackendCookieUnsafeRequest_WithMatchingToken_ExecutesAction()
    {
      var filter = CreateFilter();
      var context = CreateActionContext();
      var executed = false;
      context.HttpContext.Request.Method = "POST";
      context.HttpContext.Request.Headers["X-CSRF-TOKEN"] = "token";
      context.HttpContext.Request.Headers.Cookie = "Totem.Csrf=token";
      context.HttpContext.User = BackendCookiePrincipal();

      await filter.OnActionExecutionAsync(context, () =>
      {
        executed = true;
        return Task.FromResult(CreateExecutedContext(context));
      });

      Assert.True(executed);
      Assert.Null(context.Result);
    }

    static CookieBackedCsrfFilter CreateFilter() =>
      new(
        new CsrfTokenService(Options.Create(new CsrfOptions()), Options.Create(new InteractionAuthOptions())),
        new ConfigurationBuilder().Build());

    static ActionExecutingContext CreateActionContext()
    {
      var httpContext = new DefaultHttpContext();
      httpContext.Request.Scheme = "https";
      httpContext.Request.Host = new HostString("localhost");

      return new ActionExecutingContext(
        new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
        new List<IFilterMetadata>(),
        new Dictionary<string, object>(),
        controller: null);
    }

    static ActionExecutedContext CreateExecutedContext(ActionExecutingContext context) =>
      new(context, new List<IFilterMetadata>(), controller: null);

    static ClaimsPrincipal BackendCookiePrincipal() =>
      new(new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.NameIdentifier, "user-1"),
        new Claim(InteractionAuthClaims.TrackingSource, InteractionTrackingSources.BackendCookie)
      }, InteractionAuthDefaults.AuthenticationScheme));
  }
}
