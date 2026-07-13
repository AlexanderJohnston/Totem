using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quantum.Web.Controllers;
using Quantum.Web.Identity;
using Quantum.Web.IdentityTracking;
using Xunit;

namespace Quantum.Tests
{
  public sealed class AuthControllerTests
  {
    [Fact]
    public async Task Register_SignsInAndReturnsIdentifiedSession()
    {
      var authentication = new RecordingAuthenticationService();
      var controller = CreateController(authentication);

      var result = await controller.Register(new RegisterRequest
      {
        UserName = "ajohnston",
        DisplayName = "Alex Johnston",
        Password = "CorrectHorse1"
      }, CancellationToken.None);

      var session = SessionFromOk(result);
      Assert.Equal(InteractionTrackingStatus.Identified, session.Status);
      Assert.Equal("Alex Johnston", session.DisplayLabel);
      Assert.Equal("AJOHNSTON", session.ProcessUserId);
      Assert.Equal(InteractionTrackingSources.BackendCookie, session.TrackingSource);
      Assert.Equal(InteractionAuthDefaults.AuthenticationScheme, authentication.SignedInScheme);
      Assert.NotNull(authentication.SignedInPrincipal);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorizedWithoutSignIn()
    {
      var authentication = new RecordingAuthenticationService();
      var controller = CreateController(authentication);

      await controller.Register(new RegisterRequest
      {
        UserName = "ajohnston",
        Password = "CorrectHorse1"
      }, CancellationToken.None);

      authentication.Clear();

      var result = await controller.Login(new LoginRequest
      {
        UserName = "ajohnston",
        Password = "wrong-password"
      }, CancellationToken.None);

      Assert.IsType<UnauthorizedObjectResult>(result.Result);
      Assert.Null(authentication.SignedInPrincipal);
    }

    [Fact]
    public async Task Logout_SignsOutAndReturnsUnidentifiedSession()
    {
      var authentication = new RecordingAuthenticationService();
      var controller = CreateController(authentication);

      var result = await controller.Logout();

      var session = SessionFromOk(result);
      Assert.Equal(InteractionTrackingStatus.Unidentified, session.Status);
      Assert.Equal(InteractionAuthDefaults.AuthenticationScheme, authentication.SignedOutScheme);
    }

    static AuthController CreateController(RecordingAuthenticationService authentication)
    {
      var services = new ServiceCollection();
      services.AddSingleton<IAuthenticationService>(authentication);
      var serviceProvider = services.BuildServiceProvider();
      var store = new ApplicationUserManagerTests.InMemoryApplicationUserStore();
      var manager = new ApplicationUserManager(store, new PasswordHasher<ApplicationUser>());
      var resolver = new InteractionIdentityResolver(Options.Create(new InteractionIdentityOptions()));
      var csrf = new CsrfTokenService(
        Options.Create(new CsrfOptions()),
        Options.Create(new InteractionAuthOptions()));

      var controller = new AuthController(manager, resolver, csrf, Options.Create(new CsrfOptions()))
      {
        ControllerContext = new ControllerContext
        {
          HttpContext = new DefaultHttpContext
          {
            RequestServices = serviceProvider
          }
        }
      };

      return controller;
    }

    static InteractionSession SessionFromOk(ActionResult<InteractionSession> result)
    {
      var ok = Assert.IsType<OkObjectResult>(result.Result);
      return Assert.IsType<InteractionSession>(ok.Value);
    }

    sealed class RecordingAuthenticationService : IAuthenticationService
    {
      public string SignedInScheme { get; private set; }
      public ClaimsPrincipal SignedInPrincipal { get; private set; }
      public string SignedOutScheme { get; private set; }

      public void Clear()
      {
        SignedInScheme = null;
        SignedInPrincipal = null;
        SignedOutScheme = null;
      }

      public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) =>
        Task.FromResult(AuthenticateResult.NoResult());

      public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
        Task.CompletedTask;

      public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
        Task.CompletedTask;

      public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
      {
        SignedInScheme = scheme;
        SignedInPrincipal = principal;
        return Task.CompletedTask;
      }

      public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
      {
        SignedOutScheme = scheme;
        return Task.CompletedTask;
      }
    }
  }
}
