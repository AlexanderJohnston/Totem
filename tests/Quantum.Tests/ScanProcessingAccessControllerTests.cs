using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Outermind.Microfilm;
using Quantum.Web.Controllers;
using Quantum.Web.Identity;
using Quantum.Web.IdentityTracking;
using Quantum.Web.ScanProcessing;
using Totem;
using Totem.Timeline;
using Totem.Timeline.Area;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;
using Xunit;

namespace Quantum.Tests
{
  public sealed class ScanProcessingAccessControllerTests
  {
    const string TestOnlySecret = "test-only-bootstrap-value";

    [Fact]
    public async Task Bootstrap_InvalidSecret_DoesNotLookupUserOrAppendCommand()
    {
      var commands = new RecordingCommandServer();
      var users = new RecordingUserManager();
      var controller = Controller(commands, users, new FixedSecretValidator(DemoBootstrapSecretValidation.Invalid));

      var result = await controller.BootstrapManager(new BootstrapScanProcessingManagerRequest
      {
        UserName = "registered-user",
        Secret = TestOnlySecret
      }, CancellationToken.None);

      var denied = Assert.IsType<ObjectResult>(result);
      var issue = Assert.Single(Assert.IsType<ProcessingIssueEnvelope>(denied.Value).Issues);

      Assert.Equal(StatusCodes.Status403Forbidden, denied.StatusCode);
      Assert.Equal(ScanProcessingAccessIssueCodes.BootstrapDenied, issue.Code);
      Assert.Equal(0, users.FindCalls);
      Assert.Null(commands.Command);
      Assert.DoesNotContain("registered-user", JsonSerializer.Serialize(denied.Value));
      Assert.DoesNotContain(TestOnlySecret, JsonSerializer.Serialize(denied.Value));
    }

    [Fact]
    public async Task Bootstrap_ValidSecretForUnknownUser_ReturnsStableNotFoundWithoutAppend()
    {
      var commands = new RecordingCommandServer();
      var users = new RecordingUserManager();
      var controller = Controller(commands, users, new FixedSecretValidator(DemoBootstrapSecretValidation.Valid));

      var result = await controller.BootstrapManager(new BootstrapScanProcessingManagerRequest
      {
        UserName = "missing-user",
        Secret = TestOnlySecret
      }, CancellationToken.None);

      var notFound = Assert.IsType<NotFoundObjectResult>(result);
      var issue = Assert.Single(Assert.IsType<ProcessingIssueEnvelope>(notFound.Value).Issues);

      Assert.Equal(ScanProcessingAccessIssueCodes.RegisteredUserNotFound, issue.Code);
      Assert.Equal(1, users.FindCalls);
      Assert.Null(commands.Command);
      Assert.DoesNotContain(TestOnlySecret, JsonSerializer.Serialize(notFound.Value));
    }

    [Fact]
    public async Task Bootstrap_SuccessAppendsSecretFreeCommandForResolvedRegisteredUser()
    {
      var commands = new RecordingCommandServer();
      var users = new RecordingUserManager
      {
        FoundUser = new ApplicationUser
        {
          Id = "user-1",
          UserName = "registered-user",
          DisplayName = "Registered User",
          ProcessUserId = "REGISTERED_USER"
        }
      };
      var controller = Controller(commands, users, new FixedSecretValidator(DemoBootstrapSecretValidation.Valid));

      var result = await controller.BootstrapManager(new BootstrapScanProcessingManagerRequest
      {
        UserName = users.FoundUser.UserName,
        Secret = TestOnlySecret
      }, CancellationToken.None);

      Assert.IsType<OkObjectResult>(result);
      var command = Assert.IsType<GrantScanProcessingManager>(commands.Command);

      Assert.Equal(users.FoundUser.Id, command.TargetActor.UserId);
      Assert.Equal(users.FoundUser.UserName, command.TargetActor.UserName);
      Assert.DoesNotContain(TestOnlySecret, JsonSerializer.Serialize(command));
      Assert.DoesNotContain(
        typeof(GrantScanProcessingManager).GetProperties(),
        property => property.Name.Contains("secret", StringComparison.OrdinalIgnoreCase));
      Assert.DoesNotContain(
        typeof(ScanProcessingAccessAuditRecorded).GetProperties(),
        property => property.Name.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DefineRole_UsesServerResolvedActorAndRequestHasNoActorField()
    {
      var commands = new RecordingCommandServer();
      var actor = new ScanProcessingActorIdentity("user-1", "registered-user", "Registered User", "REGISTERED_USER");
      var controller = Controller(
        commands,
        new RecordingUserManager(),
        new FixedSecretValidator(DemoBootstrapSecretValidation.Invalid),
        new FixedActorResolver(actor));

      var result = await controller.DefineRole(new SaveScanProcessingRoleRequest
      {
        Name = "Scan operator",
        Permissions = new List<string> { ScanProcessingPermissions.ScanStart }
      });

      Assert.IsType<OkObjectResult>(result);
      var command = Assert.IsType<DefineScanProcessingRole>(commands.Command);

      Assert.Equal(actor.UserId, command.ActingActor.UserId);
      Assert.DoesNotContain(
        typeof(SaveScanProcessingRoleRequest).GetProperties(),
        property => property.Name.Contains("actor", StringComparison.OrdinalIgnoreCase)
          || property.Name.Contains("manager", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Assignment_ResolvesTargetUserAndDoesNotRequireManagerStatusForDemo()
    {
      var commands = new RecordingCommandServer();
      var actingActor = new ScanProcessingActorIdentity("user-caller", "caller", "Caller", "CALLER");
      var users = new RecordingUserManager
      {
        FoundUser = new ApplicationUser
        {
          Id = "user-target",
          UserName = "target",
          DisplayName = "Target User",
          ProcessUserId = "TARGET"
        }
      };
      var controller = Controller(
        commands,
        users,
        new FixedSecretValidator(DemoBootstrapSecretValidation.Invalid),
        new FixedActorResolver(actingActor));

      var result = await controller.AssignRole(new ChangeScanProcessingRoleAssignmentRequest
      {
        UserName = users.FoundUser.UserName,
        RoleId = "role-1"
      }, CancellationToken.None);

      Assert.IsType<OkObjectResult>(result);
      var command = Assert.IsType<AssignScanProcessingRole>(commands.Command);
      Assert.Equal(actingActor.UserId, command.ActingActor.UserId);
      Assert.Equal(users.FoundUser.Id, command.TargetActor.UserId);
      Assert.Equal("role-1", command.RoleId);
    }

    [Fact]
    public void RegisteredActorResolver_AcceptsOnlyServerIssuedRegisteredCookieIdentity()
    {
      var resolver = new RegisteredScanProcessingActorResolver();
      var cookiePrincipal = Principal("stable-user-id", "registered-user");

      Assert.True(resolver.TryResolve(cookiePrincipal, out var actor));
      Assert.Equal("stable-user-id", actor.UserId);
      Assert.Equal("registered-user", actor.UserName);

      var trackingOnly = new ClaimsPrincipal(new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.Name, "CMGX\\ajohnston"),
        new Claim(InteractionAuthClaims.TrackingSource, InteractionTrackingSources.WindowsIntegratedAuth)
      }, "Negotiate"));

      Assert.False(resolver.TryResolve(trackingOnly, out _));
    }

    [Fact]
    public void SecretValidator_HasNoDefaultAndComparesConfiguredValue()
    {
      var notConfigured = new DemoBootstrapSecretValidator(Options.Create(new ScanProcessingAccessOptions()));
      var configured = new DemoBootstrapSecretValidator(Options.Create(new ScanProcessingAccessOptions
      {
        DemoBootstrapSecret = TestOnlySecret
      }));

      Assert.Equal(DemoBootstrapSecretValidation.NotConfigured, notConfigured.Validate(TestOnlySecret));
      Assert.Equal(DemoBootstrapSecretValidation.Invalid, configured.Validate("wrong-test-value"));
      Assert.Equal(DemoBootstrapSecretValidation.Valid, configured.Validate(TestOnlySecret));
    }

    [Fact]
    public void RoutesExposeManagementContractsButNoOperationMutationEndpoint()
    {
      var routes = typeof(ScanProcessingAccessController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
          .Select(attribute => $"{attribute.HttpMethods.Single()} {attribute.Template}"))
        .ToHashSet(StringComparer.Ordinal);

      Assert.Contains("GET permissions", routes);
      Assert.Contains("GET roles", routes);
      Assert.Contains("POST roles", routes);
      Assert.Contains("PUT roles/{roleId}", routes);
      Assert.Contains("GET assignments", routes);
      Assert.Contains("POST assignments", routes);
      Assert.Contains("POST assignments/revoke", routes);
      Assert.Contains("POST admin/bootstrap", routes);
      Assert.DoesNotContain(routes, route =>
        route.Contains("start", StringComparison.OrdinalIgnoreCase)
        || route.Contains("finish", StringComparison.OrdinalIgnoreCase)
        || route.Contains("apply", StringComparison.OrdinalIgnoreCase));
    }

    static ScanProcessingAccessController Controller(
      RecordingCommandServer commands,
      RecordingUserManager users,
      IDemoBootstrapSecretValidator validator,
      IRegisteredScanProcessingActorResolver actorResolver = null)
    {
      var controller = new ScanProcessingAccessController(
        commands,
        new UnusedQueryDb(),
        users,
        actorResolver ?? new FixedActorResolver(null),
        validator)
      {
        ControllerContext = new ControllerContext
        {
          HttpContext = new DefaultHttpContext
          {
            User = Principal("stable-user-id", "registered-user"),
            TraceIdentifier = "test-correlation"
          }
        }
      };

      return controller;
    }

    static ClaimsPrincipal Principal(string userId, string userName) =>
      new(new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Name, "Registered User"),
        new Claim(InteractionAuthClaims.UserName, userName),
        new Claim(InteractionAuthClaims.ProcessUserId, "REGISTERED_USER"),
        new Claim(InteractionAuthClaims.TrackingSource, InteractionTrackingSources.BackendCookie)
      }, InteractionAuthDefaults.AuthenticationScheme));

    sealed class RecordingCommandServer : ICommandServer
    {
      public Command Command { get; private set; }

      public Task<IActionResult> Execute(Command command, IEnumerable<CommandWhen> whens)
      {
        Command = command;
        return Task.FromResult<IActionResult>(new OkObjectResult(new { accepted = true }));
      }

      public Task<IActionResult> Execute(Command command, params CommandWhen[] whens) =>
        Execute(command, (IEnumerable<CommandWhen>) whens);
    }

    sealed class RecordingUserManager : IApplicationUserManager
    {
      public ApplicationUser FoundUser { get; set; }
      public int FindCalls { get; private set; }

      public Task<ApplicationUserResult> RegisterAsync(string userName, string displayName, string password, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

      public Task<ApplicationUserResult> LoginAsync(string userName, string password, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

      public Task<ApplicationUser> FindByUserNameAsync(string userName, CancellationToken cancellationToken)
      {
        FindCalls++;
        return Task.FromResult(FoundUser);
      }
    }

    sealed class FixedActorResolver : IRegisteredScanProcessingActorResolver
    {
      readonly ScanProcessingActorIdentity _actor;

      public FixedActorResolver(ScanProcessingActorIdentity actor)
      {
        _actor = actor;
      }

      public bool TryResolve(ClaimsPrincipal principal, out ScanProcessingActorIdentity actor)
      {
        actor = _actor?.Clone();
        return actor != null;
      }
    }

    sealed class FixedSecretValidator : IDemoBootstrapSecretValidator
    {
      readonly DemoBootstrapSecretValidation _result;

      public FixedSecretValidator(DemoBootstrapSecretValidation result)
      {
        _result = result;
      }

      public DemoBootstrapSecretValidation Validate(string candidate) => _result;
    }

    sealed class UnusedQueryDb : IQueryDb
    {
      public Task<Query> ReadQuery(Func<AreaMap, FlowKey> getKey) => throw new NotSupportedException();
      public Task<QueryContent> ReadQueryContent(Func<AreaMap, QueryETag> getETag) => throw new NotSupportedException();
    }
  }
}
