using System.Collections.Generic;
using System.Linq;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public sealed class ScanProcessingRoleDefined : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingRoleDefinition Role { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingRoleDefined(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingRoleDefinition role,
      long authorizationRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      Role = role;
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingRoleDefinitionChanged : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingRoleDefinition Role { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingRoleDefinitionChanged(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingRoleDefinition role,
      long authorizationRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      Role = role;
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingRoleAssigned : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public string RoleId { get; set; }
    public List<string> PreviousRoleIds { get; set; }
    public List<string> ResultingRoleIds { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingRoleAssigned(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor,
      string roleId,
      IEnumerable<string> previousRoleIds,
      IEnumerable<string> resultingRoleIds,
      long authorizationRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      TargetActor = targetActor;
      RoleId = roleId;
      PreviousRoleIds = Copy(previousRoleIds);
      ResultingRoleIds = Copy(resultingRoleIds);
      AuthorizationRevision = authorizationRevision;
    }

    static List<string> Copy(IEnumerable<string> values) =>
      values?.OrderBy(value => value, System.StringComparer.Ordinal).ToList() ?? new List<string>();
  }

  public sealed class ScanProcessingRoleAssignmentUnchanged : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public string RoleId { get; set; }
    public List<string> ResultingRoleIds { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingRoleAssignmentUnchanged(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor,
      string roleId,
      IEnumerable<string> resultingRoleIds,
      long authorizationRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      TargetActor = targetActor;
      RoleId = roleId;
      ResultingRoleIds = resultingRoleIds?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingRoleRevoked : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public string RoleId { get; set; }
    public List<string> PreviousRoleIds { get; set; }
    public List<string> ResultingRoleIds { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingRoleRevoked(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      ScanProcessingActorIdentity targetActor,
      string roleId,
      IEnumerable<string> previousRoleIds,
      IEnumerable<string> resultingRoleIds,
      long authorizationRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      TargetActor = targetActor;
      RoleId = roleId;
      PreviousRoleIds = previousRoleIds?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      ResultingRoleIds = resultingRoleIds?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingManagerGranted : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingManagerGranted(
      string requestId,
      ScanProcessingActorIdentity targetActor,
      long authorizationRevision)
    {
      RequestId = requestId;
      TargetActor = targetActor;
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingManagerGrantUnchanged : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingManagerGrantUnchanged(
      string requestId,
      ScanProcessingActorIdentity targetActor,
      long authorizationRevision)
    {
      RequestId = requestId;
      TargetActor = targetActor;
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingAccessRequestRejected : Event
  {
    public string RequestId { get; set; }
    public string Action { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string Field { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingAccessRequestRejected(
      string requestId,
      string action,
      string code,
      string message,
      string field,
      long authorizationRevision)
    {
      RequestId = requestId;
      Action = action;
      Code = code;
      Message = message;
      Field = field;
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingOperationAuthorized : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity Actor { get; set; }
    public string Permission { get; set; }
    public ScanProcessingResolvedScope ResolvedScope { get; set; }
    public List<string> EffectiveRoleIds { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingOperationAuthorized(
      string requestId,
      ScanProcessingActorIdentity actor,
      string permission,
      ScanProcessingResolvedScope resolvedScope,
      IEnumerable<string> effectiveRoleIds,
      long authorizationRevision)
    {
      RequestId = requestId;
      Actor = actor;
      Permission = permission;
      ResolvedScope = resolvedScope;
      EffectiveRoleIds = effectiveRoleIds?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      AuthorizationRevision = authorizationRevision;
    }
  }

  public sealed class ScanProcessingOperationAuthorizationRejected : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity Actor { get; set; }
    public string Permission { get; set; }
    public ScanProcessingResolvedScope ResolvedScope { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingOperationAuthorizationRejected(
      string requestId,
      ScanProcessingActorIdentity actor,
      string permission,
      ScanProcessingResolvedScope resolvedScope,
      string code,
      string message,
      long authorizationRevision)
    {
      RequestId = requestId;
      Actor = actor;
      Permission = permission;
      ResolvedScope = resolvedScope;
      Code = code;
      Message = message;
      AuthorizationRevision = authorizationRevision;
    }
  }

  /// <summary>
  /// Redacted durable audit fact. It deliberately has no secret, credential, or physical-path field.
  /// </summary>
  public sealed class ScanProcessingAccessAuditRecorded : Event
  {
    public string AuditId { get; set; }
    public string RequestId { get; set; }
    public string Action { get; set; }
    public string Outcome { get; set; }
    public string Code { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public string RoleId { get; set; }
    public List<string> PreviousRoleIds { get; set; }
    public List<string> ResultingRoleIds { get; set; }
    public List<string> PreviousPermissions { get; set; }
    public List<string> ResultingPermissions { get; set; }
    public string Permission { get; set; }
    public ScanProcessingResolvedScope ResolvedScope { get; set; }
    public long AuthorizationRevision { get; set; }

    public ScanProcessingAccessAuditRecorded(
      string auditId,
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
      ScanProcessingResolvedScope resolvedScope,
      long authorizationRevision)
    {
      AuditId = auditId;
      RequestId = requestId;
      Action = action;
      Outcome = outcome;
      Code = code;
      ActingActor = actingActor;
      TargetActor = targetActor;
      RoleId = roleId;
      PreviousRoleIds = previousRoleIds?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      ResultingRoleIds = resultingRoleIds?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      PreviousPermissions = previousPermissions?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      ResultingPermissions = resultingPermissions?.OrderBy(value => value, System.StringComparer.Ordinal).ToList()
        ?? new List<string>();
      Permission = permission;
      ResolvedScope = resolvedScope;
      AuthorizationRevision = authorizationRevision;
    }
  }
}
