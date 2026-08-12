using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Quantum.Web.Identity;
using Quantum.Web.ScanProcessing;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;

namespace Quantum.Web.Controllers
{
  /// <summary>
  /// Demo frontend contracts for durable Scan and Processing access management.
  /// Registered authentication is required, but manager-only enforcement is deliberately deferred.
  /// </summary>
  [ApiController]
  [Route("api/scan-processing/access")]
  public sealed class ScanProcessingAccessController : ControllerBase
  {
    readonly ICommandServer _commands;
    readonly IQueryDb _queryDb;
    readonly IApplicationUserManager _users;
    readonly IRegisteredScanProcessingActorResolver _actors;
    readonly IDemoBootstrapSecretValidator _bootstrapSecret;

    public ScanProcessingAccessController(
      ICommandServer commands,
      IQueryDb queryDb,
      IApplicationUserManager users,
      IRegisteredScanProcessingActorResolver actors,
      IDemoBootstrapSecretValidator bootstrapSecret)
    {
      _commands = commands;
      _queryDb = queryDb;
      _users = users;
      _actors = actors;
      _bootstrapSecret = bootstrapSecret;
    }

    [HttpGet("permissions")]
    public ActionResult<ScanProcessingPermissionCatalogResponse> GetPermissions() =>
      Ok(new ScanProcessingPermissionCatalogResponse(ScanProcessingPermissions.All));

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
      if(!TryResolveActor(out _))
      {
        return RegisteredActorRequired();
      }

      var query = await _queryDb.ReadQuery<ScanProcessingAccessQuery>();
      var roles = query.State.RolesById.Values
        .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(role => role.RoleId, StringComparer.Ordinal)
        .Select(role => role.Clone())
        .ToList();

      return Ok(new ScanProcessingRolesResponse(query.State.AuthorizationRevision, roles));
    }

    [HttpPost("roles")]
    public Task<IActionResult> DefineRole([FromBody] SaveScanProcessingRoleRequest request)
    {
      if(!TryResolveActor(out var actor))
      {
        return Task.FromResult<IActionResult>(RegisteredActorRequired());
      }

      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(Issue(
          ScanProcessingAccessIssueCodes.InvalidRequest,
          "Role request body is required.",
          "request")));
      }

      return _commands.Execute(
        new DefineScanProcessingRole(NewRequestId(), actor, request.Name, request.Description, request.Permissions),
        When<ScanProcessingRoleDefined>.Then(e => Created(
          $"/api/scan-processing/access/roles/{e.Role.RoleId}",
          new ScanProcessingRoleMutationResponse(e.AuthorizationRevision, e.Role))),
        When<ScanProcessingAccessRequestRejected>.Then(MapRejected));
    }

    [HttpPut("roles/{roleId}")]
    public Task<IActionResult> ReplaceRole(string roleId, [FromBody] SaveScanProcessingRoleRequest request)
    {
      if(!TryResolveActor(out var actor))
      {
        return Task.FromResult<IActionResult>(RegisteredActorRequired());
      }

      if(request == null)
      {
        return Task.FromResult<IActionResult>(BadRequest(Issue(
          ScanProcessingAccessIssueCodes.InvalidRequest,
          "Role request body is required.",
          "request")));
      }

      return _commands.Execute(
        new ReplaceScanProcessingRole(
          NewRequestId(),
          actor,
          roleId,
          request.ExpectedVersion,
          request.Name,
          request.Description,
          request.Permissions),
        When<ScanProcessingRoleDefinitionChanged>.Then(e => Ok(
          new ScanProcessingRoleMutationResponse(e.AuthorizationRevision, e.Role))),
        When<ScanProcessingAccessRequestRejected>.Then(MapRejected));
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments()
    {
      if(!TryResolveActor(out _))
      {
        return RegisteredActorRequired();
      }

      var query = await _queryDb.ReadQuery<ScanProcessingAccessQuery>();
      var assignments = query.State.ActorsById.Values
        .OrderBy(access => access.Actor.UserName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(access => access.Actor.UserId, StringComparer.Ordinal)
        .Select(access => access.Clone())
        .ToList();

      return Ok(new ScanProcessingAssignmentsResponse(query.State.AuthorizationRevision, assignments));
    }

    [HttpPost("assignments")]
    public Task<IActionResult> AssignRole(
      [FromBody] ChangeScanProcessingRoleAssignmentRequest request,
      CancellationToken cancellationToken) =>
      ChangeAssignment(request, revoke: false, cancellationToken);

    [HttpPost("assignments/revoke")]
    public Task<IActionResult> RevokeRole(
      [FromBody] ChangeScanProcessingRoleAssignmentRequest request,
      CancellationToken cancellationToken) =>
      ChangeAssignment(request, revoke: true, cancellationToken);

    [HttpPost("admin/bootstrap")]
    public async Task<IActionResult> BootstrapManager(
      [FromBody] BootstrapScanProcessingManagerRequest request,
      CancellationToken cancellationToken)
    {
      if(request == null)
      {
        return BadRequest(Issue(
          ScanProcessingAccessIssueCodes.InvalidRequest,
          "Bootstrap request body is required.",
          "request"));
      }

      var validation = _bootstrapSecret.Validate(request.Secret);

      if(validation == DemoBootstrapSecretValidation.NotConfigured)
      {
        return StatusCode(StatusCodes.Status503ServiceUnavailable, Issue(
          ScanProcessingAccessIssueCodes.BootstrapNotConfigured,
          "Temporary manager bootstrap is not configured.",
          "secret"));
      }

      // Validate before the username lookup so an invalid secret cannot enumerate registered users.
      if(validation != DemoBootstrapSecretValidation.Valid)
      {
        return StatusCode(StatusCodes.Status403Forbidden, Issue(
          ScanProcessingAccessIssueCodes.BootstrapDenied,
          "Manager bootstrap was denied.",
          "secret"));
      }

      var user = await _users.FindByUserNameAsync(request.UserName, cancellationToken).ConfigureAwait(false);

      if(user == null)
      {
        return NotFound(Issue(
          ScanProcessingAccessIssueCodes.RegisteredUserNotFound,
          "The registered user was not found.",
          "userName"));
      }

      var target = RegisteredScanProcessingActorResolver.FromApplicationUser(user);

      return await _commands.Execute(
        new GrantScanProcessingManager(NewRequestId(), target),
        When<ScanProcessingManagerGranted>.Then(e => Ok(
          new BootstrapScanProcessingManagerResponse(
            e.TargetActor.UserId,
            e.TargetActor.UserName,
            true,
            e.AuthorizationRevision))),
        When<ScanProcessingManagerGrantUnchanged>.Then(e => Ok(
          new BootstrapScanProcessingManagerResponse(
            e.TargetActor.UserId,
            e.TargetActor.UserName,
            true,
            e.AuthorizationRevision))),
        When<ScanProcessingAccessRequestRejected>.Then(MapRejected));
    }

    async Task<IActionResult> ChangeAssignment(
      ChangeScanProcessingRoleAssignmentRequest request,
      bool revoke,
      CancellationToken cancellationToken)
    {
      if(!TryResolveActor(out var actor))
      {
        return RegisteredActorRequired();
      }

      if(request == null)
      {
        return BadRequest(Issue(
          ScanProcessingAccessIssueCodes.InvalidRequest,
          "Assignment request body is required.",
          "request"));
      }

      var user = await _users.FindByUserNameAsync(request.UserName, cancellationToken).ConfigureAwait(false);

      if(user == null)
      {
        return NotFound(Issue(
          ScanProcessingAccessIssueCodes.RegisteredUserNotFound,
          "The registered user was not found.",
          "userName"));
      }

      var target = RegisteredScanProcessingActorResolver.FromApplicationUser(user);
      var command = revoke
        ? (Totem.Timeline.Command) new RevokeScanProcessingRole(NewRequestId(), actor, target, request.RoleId)
        : new AssignScanProcessingRole(NewRequestId(), actor, target, request.RoleId);

      return await _commands.Execute(
        command,
        When<ScanProcessingRoleAssigned>.Then(e => Ok(AssignmentResponse(e.TargetActor, e.ResultingRoleIds, e.AuthorizationRevision))),
        When<ScanProcessingRoleRevoked>.Then(e => Ok(AssignmentResponse(e.TargetActor, e.ResultingRoleIds, e.AuthorizationRevision))),
        When<ScanProcessingRoleAssignmentUnchanged>.Then(e => Ok(AssignmentResponse(e.TargetActor, e.ResultingRoleIds, e.AuthorizationRevision))),
        When<ScanProcessingAccessRequestRejected>.Then(MapRejected));
    }

    bool TryResolveActor(out ScanProcessingActorIdentity actor) =>
      _actors.TryResolve(HttpContext.User, out actor);

    IActionResult RegisteredActorRequired() =>
      Unauthorized(Issue(
        ScanProcessingAccessIssueCodes.AuthenticatedActorRequired,
        "A registered authenticated actor is required.",
        "actor"));

    IActionResult MapRejected(ScanProcessingAccessRequestRejected rejected)
    {
      var envelope = Issue(rejected.Code, rejected.Message, rejected.Field);

      return rejected.Code switch
      {
        ScanProcessingAccessIssueCodes.AuthenticatedActorRequired => Unauthorized(envelope),
        ScanProcessingAccessIssueCodes.RegisteredUserNotFound => NotFound(envelope),
        ScanProcessingAccessIssueCodes.RoleNotFound => NotFound(envelope),
        ScanProcessingAccessIssueCodes.RoleNameConflict => Conflict(envelope),
        ScanProcessingAccessIssueCodes.RoleVersionStale => Conflict(envelope),
        _ => BadRequest(envelope)
      };
    }

    ProcessingIssueEnvelope Issue(string code, string message, string field) =>
      new(new ProcessingIssue
      {
        Code = code,
        Severity = ProcessingIssueSeverity.Error,
        Message = message,
        Field = field,
        CorrelationId = HttpContext?.TraceIdentifier
      });

    static ScanProcessingRoleAssignmentResponse AssignmentResponse(
      ScanProcessingActorIdentity target,
      IEnumerable<string> roleIds,
      long authorizationRevision) =>
      new(
        authorizationRevision,
        target?.Clone(),
        roleIds?.OrderBy(roleId => roleId, StringComparer.Ordinal).ToList() ?? new List<string>());

    static string NewRequestId() => Guid.NewGuid().ToString("N");
  }

  public sealed class SaveScanProcessingRoleRequest
  {
    public int ExpectedVersion { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Permissions { get; set; } = new();
  }

  public sealed class ChangeScanProcessingRoleAssignmentRequest
  {
    public string UserName { get; set; }
    public string RoleId { get; set; }
  }

  public sealed class BootstrapScanProcessingManagerRequest
  {
    public string UserName { get; set; }
    public string Secret { get; set; }
  }

  public sealed record ScanProcessingPermissionCatalogResponse(IReadOnlyList<string> Permissions);
  public sealed record ScanProcessingRolesResponse(long AuthorizationRevision, IReadOnlyList<ScanProcessingRoleDefinition> Roles);
  public sealed record ScanProcessingAssignmentsResponse(long AuthorizationRevision, IReadOnlyList<ScanProcessingActorAccess> Assignments);
  public sealed record ScanProcessingRoleMutationResponse(long AuthorizationRevision, ScanProcessingRoleDefinition Role);
  public sealed record ScanProcessingRoleAssignmentResponse(
    long AuthorizationRevision,
    ScanProcessingActorIdentity Actor,
    IReadOnlyList<string> RoleIds);
  public sealed record BootstrapScanProcessingManagerResponse(
    string UserId,
    string UserName,
    bool IsManager,
    long AuthorizationRevision);
}
