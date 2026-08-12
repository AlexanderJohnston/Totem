using System;
using System.Collections.Generic;
using System.Linq;

namespace Outermind.Microfilm
{
  /// <summary>
  /// The fixed Version 1 permission catalog. Role definitions may contain only these values.
  /// </summary>
  public static class ScanProcessingPermissions
  {
    public const string ScanDiscover = "scan.discover";
    public const string ScanStart = "scan.start";
    public const string ScanFinishOwn = "scan.finish-own";
    public const string ScanFinishAny = "scan.finish-any";
    public const string ScanAbandon = "scan.abandon";
    public const string ProcessingDiscover = "processing.discover";
    public const string ProcessingPreviewQpfSettings = "processing.preview.qpf-settings";
    public const string ProcessingPreviewFramesPaths = "processing.preview.frames-paths";
    public const string ProcessingApplyQpfSettings = "processing.apply.qpf-settings";
    public const string ProcessingApplyFramesPaths = "processing.apply.frames-paths";
    public const string JobCancelOwn = "job.cancel-own";
    public const string JobCancelAny = "job.cancel-any";
    public const string HistoryViewContext = "history.view-context";
    public const string HistoryViewWorkspace = "history.view-workspace";
    public const string SensitiveErrorView = "sensitive-error.view";
    public const string BackupCleanup = "backup.cleanup";

    static readonly string[] Catalog =
    {
      BackupCleanup,
      HistoryViewContext,
      HistoryViewWorkspace,
      JobCancelAny,
      JobCancelOwn,
      ProcessingApplyFramesPaths,
      ProcessingApplyQpfSettings,
      ProcessingDiscover,
      ProcessingPreviewFramesPaths,
      ProcessingPreviewQpfSettings,
      ScanAbandon,
      ScanDiscover,
      ScanFinishAny,
      ScanFinishOwn,
      ScanStart,
      SensitiveErrorView
    };

    static readonly Dictionary<string, string> CanonicalByValue = Catalog.ToDictionary(
      permission => permission,
      permission => permission,
      StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> All => Catalog;

    public static bool TryNormalize(string permission, out string canonical)
    {
      var candidate = string.IsNullOrWhiteSpace(permission) ? null : permission.Trim();

      return CanonicalByValue.TryGetValue(candidate ?? string.Empty, out canonical);
    }

    public static bool TryNormalize(
      IEnumerable<string> permissions,
      out List<string> canonical,
      out string invalidPermission)
    {
      canonical = new List<string>();
      invalidPermission = null;

      foreach(var permission in permissions ?? Enumerable.Empty<string>())
      {
        if(!TryNormalize(permission, out var normalized))
        {
          invalidPermission = permission;
          return false;
        }

        if(!canonical.Contains(normalized, StringComparer.Ordinal))
        {
          canonical.Add(normalized);
        }
      }

      canonical.Sort(StringComparer.Ordinal);
      return true;
    }
  }

  public static class ScanProcessingAccessIssueCodes
  {
    public const string AuthenticatedActorRequired = "AUTHENTICATED_ACTOR_REQUIRED";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string InvalidRoleName = "INVALID_ROLE_NAME";
    public const string InvalidPermission = "INVALID_PERMISSION";
    public const string RoleNameConflict = "ROLE_NAME_CONFLICT";
    public const string RoleNotFound = "ROLE_NOT_FOUND";
    public const string RoleVersionStale = "ROLE_VERSION_STALE";
    public const string RegisteredUserNotFound = "REGISTERED_USER_NOT_FOUND";
    public const string BootstrapDenied = "BOOTSTRAP_DENIED";
    public const string BootstrapNotConfigured = "BOOTSTRAP_NOT_CONFIGURED";
    public const string ForbiddenOperation = "FORBIDDEN_OPERATION";
  }

  public static class ScanProcessingAccessActions
  {
    public const string RoleDefine = "role.define";
    public const string RoleReplace = "role.replace";
    public const string RoleAssign = "role.assign";
    public const string RoleRevoke = "role.revoke";
    public const string ManagerBootstrap = "manager.bootstrap";
    public const string OperationAuthorize = "operation.authorize";
  }

  public sealed class ScanProcessingActorIdentity : IEquatable<ScanProcessingActorIdentity>
  {
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string ProcessUserId { get; set; }

    public ScanProcessingActorIdentity()
    {
    }

    public ScanProcessingActorIdentity(string userId, string userName, string displayName, string processUserId)
    {
      UserId = Normalize(userId);
      UserName = Normalize(userName);
      DisplayName = Normalize(displayName);
      ProcessUserId = Normalize(processUserId);
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrWhiteSpace(UserName);

    public ScanProcessingActorIdentity Clone() =>
      new(UserId, UserName, DisplayName, ProcessUserId);

    public bool Equals(ScanProcessingActorIdentity other) =>
      other != null && string.Equals(UserId, other.UserId, StringComparison.Ordinal);

    public override bool Equals(object obj) => Equals(obj as ScanProcessingActorIdentity);
    public override int GetHashCode() => UserId?.GetHashCode(StringComparison.Ordinal) ?? 0;

    static string Normalize(string value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  /// <summary>
  /// Server-resolved operation scope. Future HTTP operation DTOs must not bind this type directly.
  /// </summary>
  public sealed class ScanProcessingResolvedScope : IEquatable<ScanProcessingResolvedScope>
  {
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public string RollId { get; set; }
    public string RowId { get; set; }
    public string ResourceId { get; set; }

    public ScanProcessingResolvedScope()
    {
    }

    public ScanProcessingResolvedScope(
      string clientId,
      string workspaceId,
      string rollId,
      string rowId,
      string resourceId = null)
    {
      ClientId = Normalize(clientId);
      WorkspaceId = Normalize(workspaceId);
      RollId = Normalize(rollId);
      RowId = Normalize(rowId);
      ResourceId = Normalize(resourceId);
    }

    public bool IsValid =>
      !string.IsNullOrWhiteSpace(ClientId)
      && !string.IsNullOrWhiteSpace(WorkspaceId)
      && string.Equals(ClientId, WorkspaceId, StringComparison.Ordinal)
      && !string.IsNullOrWhiteSpace(RollId)
      && !string.IsNullOrWhiteSpace(RowId);

    public ScanProcessingResolvedScope Clone() =>
      new(ClientId, WorkspaceId, RollId, RowId, ResourceId);

    public bool Equals(ScanProcessingResolvedScope other) =>
      other != null
      && string.Equals(ClientId, other.ClientId, StringComparison.Ordinal)
      && string.Equals(WorkspaceId, other.WorkspaceId, StringComparison.Ordinal)
      && string.Equals(RollId, other.RollId, StringComparison.Ordinal)
      && string.Equals(RowId, other.RowId, StringComparison.Ordinal)
      && string.Equals(ResourceId, other.ResourceId, StringComparison.Ordinal);

    public override bool Equals(object obj) => Equals(obj as ScanProcessingResolvedScope);
    public override int GetHashCode() => HashCode.Combine(ClientId, WorkspaceId, RollId, RowId, ResourceId);

    static string Normalize(string value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  public sealed class ScanProcessingRoleDefinition
  {
    public string RoleId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public int Version { get; set; }

    public ScanProcessingRoleDefinition()
    {
    }

    public ScanProcessingRoleDefinition(
      string roleId,
      string name,
      string description,
      IEnumerable<string> permissions,
      int version)
    {
      RoleId = roleId;
      Name = name;
      Description = description;
      Permissions = permissions?.ToList() ?? new List<string>();
      Version = version;
    }

    public ScanProcessingRoleDefinition Clone() =>
      new(RoleId, Name, Description, Permissions, Version);
  }

  public sealed class ScanProcessingActorAccess
  {
    public ScanProcessingActorIdentity Actor { get; set; }
    public List<string> RoleIds { get; set; } = new();
    public bool IsManager { get; set; }

    public ScanProcessingActorAccess()
    {
    }

    public ScanProcessingActorAccess(
      ScanProcessingActorIdentity actor,
      IEnumerable<string> roleIds,
      bool isManager)
    {
      Actor = actor?.Clone();
      RoleIds = roleIds?.OrderBy(roleId => roleId, StringComparer.Ordinal).ToList() ?? new List<string>();
      IsManager = isManager;
    }

    public ScanProcessingActorAccess Clone() =>
      new(Actor, RoleIds, IsManager);
  }

  /// <summary>
  /// Shared deterministic reducer used by the authority topic and its read projection.
  /// </summary>
  public sealed class ScanProcessingAccessState
  {
    public Dictionary<string, ScanProcessingRoleDefinition> RolesById { get; set; } = new();
    public Dictionary<string, ScanProcessingActorAccess> ActorsById { get; set; } = new();
    public long AuthorizationRevision { get; set; }

    public void Apply(ScanProcessingRoleDefined e) => ApplyRole(e.Role, e.AuthorizationRevision);
    public void Apply(ScanProcessingRoleDefinitionChanged e) => ApplyRole(e.Role, e.AuthorizationRevision);

    public void Apply(ScanProcessingRoleAssigned e) =>
      ApplyActor(e.TargetActor, e.ResultingRoleIds, IsManager(e.TargetActor?.UserId), e.AuthorizationRevision);

    public void Apply(ScanProcessingRoleRevoked e) =>
      ApplyActor(e.TargetActor, e.ResultingRoleIds, IsManager(e.TargetActor?.UserId), e.AuthorizationRevision);

    public void Apply(ScanProcessingManagerGranted e)
    {
      var existing = GetActor(e.TargetActor?.UserId);
      ApplyActor(e.TargetActor, existing?.RoleIds, true, e.AuthorizationRevision);
    }

    public bool TryGetRole(string roleId, out ScanProcessingRoleDefinition role) =>
      RolesById.TryGetValue(roleId ?? string.Empty, out role);

    public bool TryGetRoleByName(string name, out ScanProcessingRoleDefinition role)
    {
      role = RolesById.Values.FirstOrDefault(candidate =>
        string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

      return role != null;
    }

    public ScanProcessingActorAccess GetActor(string userId) =>
      string.IsNullOrWhiteSpace(userId) || !ActorsById.TryGetValue(userId, out var actor)
        ? null
        : actor;

    public bool IsManager(string userId) => GetActor(userId)?.IsManager == true;

    public List<string> ResolveEffectiveRoles(string userId, string permission)
    {
      var access = GetActor(userId);

      if(access == null)
      {
        return new List<string>();
      }

      return access.RoleIds
        .Where(roleId => RolesById.TryGetValue(roleId, out var role)
          && role.Permissions.Contains(permission, StringComparer.Ordinal))
        .OrderBy(roleId => roleId, StringComparer.Ordinal)
        .ToList();
    }

    void ApplyRole(ScanProcessingRoleDefinition role, long revision)
    {
      if(role == null || string.IsNullOrWhiteSpace(role.RoleId))
      {
        return;
      }

      RolesById[role.RoleId] = role.Clone();
      AuthorizationRevision = Math.Max(AuthorizationRevision, revision);
    }

    void ApplyActor(
      ScanProcessingActorIdentity actor,
      IEnumerable<string> roleIds,
      bool isManager,
      long revision)
    {
      if(actor?.IsValid != true)
      {
        return;
      }

      ActorsById[actor.UserId] = new ScanProcessingActorAccess(actor, roleIds, isManager);
      AuthorizationRevision = Math.Max(AuthorizationRevision, revision);
    }
  }
}
