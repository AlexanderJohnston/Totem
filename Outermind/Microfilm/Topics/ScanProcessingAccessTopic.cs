using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Single durable authority for Version 1 role policy and globally ordered authorization decisions.
  /// </summary>
  public sealed class ScanProcessingAccessTopic : Topic
  {
    readonly ScanProcessingAccessState _state = new();

    void Given(ScanProcessingRoleDefined e) => _state.Apply(e);
    void Given(ScanProcessingRoleDefinitionChanged e) => _state.Apply(e);
    void Given(ScanProcessingRoleAssigned e) => _state.Apply(e);
    void Given(ScanProcessingRoleRevoked e) => _state.Apply(e);
    void Given(ScanProcessingManagerGranted e) => _state.Apply(e);

    void When(DefineScanProcessingRole command)
    {
      if(!TryValidateManagementRequest(
        command.RequestId,
        command.ActingActor,
        ScanProcessingAccessActions.RoleDefine,
        out var requestId))
      {
        return;
      }

      if(!TryNormalizeRole(
        requestId,
        command.ActingActor,
        ScanProcessingAccessActions.RoleDefine,
        command.Name,
        command.Description,
        command.Permissions,
        out var name,
        out var description,
        out var permissions))
      {
        return;
      }

      if(_state.TryGetRoleByName(name, out _))
      {
        Reject(
          requestId,
          ScanProcessingAccessActions.RoleDefine,
          ScanProcessingAccessIssueCodes.RoleNameConflict,
          "A Scan and Processing role already uses that name.",
          "name",
          command.ActingActor);
        return;
      }

      var revision = NextRevision;
      var role = new ScanProcessingRoleDefinition(
        $"role-{Guid.NewGuid():N}",
        name,
        description,
        permissions,
        1);

      Then(new ScanProcessingRoleDefined(requestId, command.ActingActor.Clone(), role, revision));
      Audit(
        requestId,
        ScanProcessingAccessActions.RoleDefine,
        "accepted",
        null,
        command.ActingActor,
        null,
        role.RoleId,
        null,
        null,
        null,
        role.Permissions,
        null,
        null,
        revision);
    }

    void When(ReplaceScanProcessingRole command)
    {
      if(!TryValidateManagementRequest(
        command.RequestId,
        command.ActingActor,
        ScanProcessingAccessActions.RoleReplace,
        out var requestId))
      {
        return;
      }

      var roleId = Normalize(command.RoleId);

      if(string.IsNullOrWhiteSpace(roleId) || !_state.TryGetRole(roleId, out var existing))
      {
        Reject(
          requestId,
          ScanProcessingAccessActions.RoleReplace,
          ScanProcessingAccessIssueCodes.RoleNotFound,
          "The Scan and Processing role was not found.",
          "roleId",
          command.ActingActor,
          roleId: roleId);
        return;
      }

      if(command.ExpectedVersion != existing.Version)
      {
        Reject(
          requestId,
          ScanProcessingAccessActions.RoleReplace,
          ScanProcessingAccessIssueCodes.RoleVersionStale,
          "The role changed after it was read. Refresh it and try again.",
          "expectedVersion",
          command.ActingActor,
          roleId: roleId);
        return;
      }

      if(!TryNormalizeRole(
        requestId,
        command.ActingActor,
        ScanProcessingAccessActions.RoleReplace,
        command.Name,
        command.Description,
        command.Permissions,
        out var name,
        out var description,
        out var permissions))
      {
        return;
      }

      if(_state.TryGetRoleByName(name, out var duplicate) && duplicate.RoleId != roleId)
      {
        Reject(
          requestId,
          ScanProcessingAccessActions.RoleReplace,
          ScanProcessingAccessIssueCodes.RoleNameConflict,
          "A Scan and Processing role already uses that name.",
          "name",
          command.ActingActor,
          roleId: roleId);
        return;
      }

      var revision = NextRevision;
      var role = new ScanProcessingRoleDefinition(
        roleId,
        name,
        description,
        permissions,
        existing.Version + 1);

      Then(new ScanProcessingRoleDefinitionChanged(requestId, command.ActingActor.Clone(), role, revision));
      Audit(
        requestId,
        ScanProcessingAccessActions.RoleReplace,
        "accepted",
        null,
        command.ActingActor,
        null,
        roleId,
        null,
        null,
        existing.Permissions,
        role.Permissions,
        null,
        null,
        revision);
    }

    void When(AssignScanProcessingRole command)
    {
      if(!TryValidateAssignmentRequest(
        command.RequestId,
        command.ActingActor,
        command.TargetActor,
        command.RoleId,
        ScanProcessingAccessActions.RoleAssign,
        out var requestId,
        out var roleId))
      {
        return;
      }

      var previous = CurrentRoles(command.TargetActor.UserId);

      if(previous.Contains(roleId, StringComparer.Ordinal))
      {
        Then(new ScanProcessingRoleAssignmentUnchanged(
          requestId,
          command.ActingActor.Clone(),
          command.TargetActor.Clone(),
          roleId,
          previous,
          _state.AuthorizationRevision));
        Audit(
          requestId,
          ScanProcessingAccessActions.RoleAssign,
          "unchanged",
          null,
          command.ActingActor,
          command.TargetActor,
          roleId,
          previous,
          previous,
          null,
          null,
          null,
          null,
          _state.AuthorizationRevision);
        return;
      }

      var resulting = previous.Append(roleId).OrderBy(value => value, StringComparer.Ordinal).ToList();
      var revision = NextRevision;

      Then(new ScanProcessingRoleAssigned(
        requestId,
        command.ActingActor.Clone(),
        command.TargetActor.Clone(),
        roleId,
        previous,
        resulting,
        revision));
      Audit(
        requestId,
        ScanProcessingAccessActions.RoleAssign,
        "accepted",
        null,
        command.ActingActor,
        command.TargetActor,
        roleId,
        previous,
        resulting,
        null,
        null,
        null,
        null,
        revision);
    }

    void When(RevokeScanProcessingRole command)
    {
      if(!TryValidateAssignmentRequest(
        command.RequestId,
        command.ActingActor,
        command.TargetActor,
        command.RoleId,
        ScanProcessingAccessActions.RoleRevoke,
        out var requestId,
        out var roleId))
      {
        return;
      }

      var previous = CurrentRoles(command.TargetActor.UserId);

      if(!previous.Contains(roleId, StringComparer.Ordinal))
      {
        Then(new ScanProcessingRoleAssignmentUnchanged(
          requestId,
          command.ActingActor.Clone(),
          command.TargetActor.Clone(),
          roleId,
          previous,
          _state.AuthorizationRevision));
        Audit(
          requestId,
          ScanProcessingAccessActions.RoleRevoke,
          "unchanged",
          null,
          command.ActingActor,
          command.TargetActor,
          roleId,
          previous,
          previous,
          null,
          null,
          null,
          null,
          _state.AuthorizationRevision);
        return;
      }

      var resulting = previous.Where(value => value != roleId).ToList();
      var revision = NextRevision;

      Then(new ScanProcessingRoleRevoked(
        requestId,
        command.ActingActor.Clone(),
        command.TargetActor.Clone(),
        roleId,
        previous,
        resulting,
        revision));
      Audit(
        requestId,
        ScanProcessingAccessActions.RoleRevoke,
        "accepted",
        null,
        command.ActingActor,
        command.TargetActor,
        roleId,
        previous,
        resulting,
        null,
        null,
        null,
        null,
        revision);
    }

    void When(GrantScanProcessingManager command)
    {
      var requestId = Normalize(command.RequestId);

      if(string.IsNullOrWhiteSpace(requestId) || command.TargetActor?.IsValid != true)
      {
        Reject(
          requestId,
          ScanProcessingAccessActions.ManagerBootstrap,
          ScanProcessingAccessIssueCodes.InvalidRequest,
          "The manager bootstrap request was invalid.",
          "userName",
          null,
          command.TargetActor);
        return;
      }

      if(_state.IsManager(command.TargetActor.UserId))
      {
        Then(new ScanProcessingManagerGrantUnchanged(
          requestId,
          command.TargetActor.Clone(),
          _state.AuthorizationRevision));
        Audit(
          requestId,
          ScanProcessingAccessActions.ManagerBootstrap,
          "unchanged",
          null,
          null,
          command.TargetActor,
          null,
          CurrentRoles(command.TargetActor.UserId),
          CurrentRoles(command.TargetActor.UserId),
          null,
          null,
          null,
          null,
          _state.AuthorizationRevision);
        return;
      }

      var revision = NextRevision;
      Then(new ScanProcessingManagerGranted(requestId, command.TargetActor.Clone(), revision));
      Audit(
        requestId,
        ScanProcessingAccessActions.ManagerBootstrap,
        "accepted",
        null,
        null,
        command.TargetActor,
        null,
        CurrentRoles(command.TargetActor.UserId),
        CurrentRoles(command.TargetActor.UserId),
        null,
        null,
        null,
        null,
        revision);
    }

    void When(RequestScanProcessingAuthorization command)
    {
      var requestId = Normalize(command.RequestId);
      var actor = command.Actor;
      var scope = command.ResolvedScope;

      if(string.IsNullOrWhiteSpace(requestId)
        || actor?.IsValid != true
        || scope?.IsValid != true
        || !ScanProcessingPermissions.TryNormalize(command.Permission, out var permission))
      {
        RejectOperation(
          requestId,
          actor,
          command.Permission,
          scope,
          ScanProcessingAccessIssueCodes.ForbiddenOperation,
          "The actor is not authorized for the requested operation.");
        return;
      }

      var effectiveRoleIds = _state.ResolveEffectiveRoles(actor.UserId, permission);

      if(effectiveRoleIds.Count == 0)
      {
        RejectOperation(
          requestId,
          actor,
          permission,
          scope,
          ScanProcessingAccessIssueCodes.ForbiddenOperation,
          "The actor is not authorized for the requested operation.");
        return;
      }

      Then(new ScanProcessingOperationAuthorized(
        requestId,
        actor.Clone(),
        permission,
        scope.Clone(),
        effectiveRoleIds,
        _state.AuthorizationRevision));
      Audit(
        requestId,
        ScanProcessingAccessActions.OperationAuthorize,
        "authorized",
        null,
        actor,
        actor,
        null,
        effectiveRoleIds,
        effectiveRoleIds,
        null,
        null,
        permission,
        scope,
        _state.AuthorizationRevision);
    }

    bool TryValidateManagementRequest(
      string rawRequestId,
      ScanProcessingActorIdentity actor,
      string action,
      out string requestId)
    {
      requestId = Normalize(rawRequestId);

      if(string.IsNullOrWhiteSpace(requestId))
      {
        Reject(
          requestId,
          action,
          ScanProcessingAccessIssueCodes.InvalidRequest,
          "A request ID is required.",
          "requestId",
          actor);
        return false;
      }

      if(actor?.IsValid != true)
      {
        Reject(
          requestId,
          action,
          ScanProcessingAccessIssueCodes.AuthenticatedActorRequired,
          "A registered authenticated actor is required.",
          "actor",
          actor);
        return false;
      }

      return true;
    }

    bool TryValidateAssignmentRequest(
      string rawRequestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor,
      string rawRoleId,
      string action,
      out string requestId,
      out string roleId)
    {
      roleId = Normalize(rawRoleId);

      if(!TryValidateManagementRequest(rawRequestId, actingActor, action, out requestId))
      {
        return false;
      }

      if(targetActor?.IsValid != true)
      {
        Reject(
          requestId,
          action,
          ScanProcessingAccessIssueCodes.RegisteredUserNotFound,
          "The registered user was not found.",
          "userName",
          actingActor,
          targetActor,
          roleId);
        return false;
      }

      if(string.IsNullOrWhiteSpace(roleId) || !_state.TryGetRole(roleId, out _))
      {
        Reject(
          requestId,
          action,
          ScanProcessingAccessIssueCodes.RoleNotFound,
          "The Scan and Processing role was not found.",
          "roleId",
          actingActor,
          targetActor,
          roleId);
        return false;
      }

      return true;
    }

    bool TryNormalizeRole(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      string action,
      string rawName,
      string rawDescription,
      IEnumerable<string> rawPermissions,
      out string name,
      out string description,
      out List<string> permissions)
    {
      name = Normalize(rawName);
      description = Normalize(rawDescription);
      permissions = null;

      if(string.IsNullOrWhiteSpace(name) || name.Length > 80)
      {
        Reject(
          requestId,
          action,
          ScanProcessingAccessIssueCodes.InvalidRoleName,
          "Role name is required and must be at most 80 characters.",
          "name",
          actingActor);
        return false;
      }

      if(description?.Length > 500)
      {
        Reject(
          requestId,
          action,
          ScanProcessingAccessIssueCodes.InvalidRequest,
          "Role description must be at most 500 characters.",
          "description",
          actingActor);
        return false;
      }

      if(!ScanProcessingPermissions.TryNormalize(rawPermissions, out permissions, out _))
      {
        Reject(
          requestId,
          action,
          ScanProcessingAccessIssueCodes.InvalidPermission,
          "The role contains a permission outside the Version 1 catalog.",
          "permissions",
          actingActor);
        return false;
      }

      return true;
    }

    void RejectOperation(
      string requestId,
      ScanProcessingActorIdentity actor,
      string permission,
      ScanProcessingResolvedScope scope,
      string code,
      string message)
    {
      Then(new ScanProcessingOperationAuthorizationRejected(
        requestId,
        actor?.Clone(),
        permission,
        scope?.Clone(),
        code,
        message,
        _state.AuthorizationRevision));
      Audit(
        requestId,
        ScanProcessingAccessActions.OperationAuthorize,
        "rejected",
        code,
        actor,
        actor,
        null,
        null,
        null,
        null,
        null,
        permission,
        scope,
        _state.AuthorizationRevision);
    }

    void Reject(
      string requestId,
      string action,
      string code,
      string message,
      string field,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor = null,
      string roleId = null)
    {
      Then(new ScanProcessingAccessRequestRejected(
        requestId,
        action,
        code,
        message,
        field,
        _state.AuthorizationRevision));
      Audit(
        requestId,
        action,
        "rejected",
        code,
        actingActor,
        targetActor,
        roleId,
        CurrentRoles(targetActor?.UserId),
        CurrentRoles(targetActor?.UserId),
        null,
        null,
        null,
        null,
        _state.AuthorizationRevision);
    }

    void Audit(
      string requestId,
      string action,
      string outcome,
      string code,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor,
      string roleId,
      IEnumerable<string> previousRoleIds,
      IEnumerable<string> resultingRoleIds,
      IEnumerable<string> previousPermissions,
      IEnumerable<string> resultingPermissions,
      string permission,
      ScanProcessingResolvedScope scope,
      long revision) =>
      Then(new ScanProcessingAccessAuditRecorded(
        Id.FromGuid().ToString(),
        requestId,
        action,
        outcome,
        code,
        actingActor?.Clone(),
        targetActor?.Clone(),
        roleId,
        previousRoleIds,
        resultingRoleIds,
        previousPermissions,
        resultingPermissions,
        permission,
        scope?.Clone(),
        revision));

    List<string> CurrentRoles(string userId) =>
      _state.GetActor(userId)?.RoleIds.ToList() ?? new List<string>();

    long NextRevision => checked(_state.AuthorizationRevision + 1);

    static string Normalize(string value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}
