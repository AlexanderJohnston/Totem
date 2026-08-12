using System;
using System.Collections.Generic;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Projects current Version 1 roles, global assignments, manager status, and access revision.
  /// </summary>
  public sealed class ScanProcessingAccessQuery : Query
  {
    public ScanProcessingAccessState State { get; set; } = new();

    void Given(ScanProcessingRoleDefined e) => State.Apply(e);
    void Given(ScanProcessingRoleDefinitionChanged e) => State.Apply(e);
    void Given(ScanProcessingRoleAssigned e) => State.Apply(e);
    void Given(ScanProcessingRoleRevoked e) => State.Apply(e);
    void Given(ScanProcessingManagerGranted e) => State.Apply(e);
  }

  /// <summary>
  /// Append-only access audit projection. This is not exposed as a general public route.
  /// </summary>
  public sealed class ScanProcessingAccessAuditQuery : Query
  {
    public List<ScanProcessingAccessAuditEntry> Entries { get; set; } = new();

    void Given(ScanProcessingAccessAuditRecorded e)
    {
      Entries.Add(new ScanProcessingAccessAuditEntry
      {
        AuditId = e.AuditId,
        RequestId = e.RequestId,
        Action = e.Action,
        Outcome = e.Outcome,
        Code = e.Code,
        ActingActor = e.ActingActor?.Clone(),
        TargetActor = e.TargetActor?.Clone(),
        RoleId = e.RoleId,
        PreviousRoleIds = new List<string>(e.PreviousRoleIds ?? new List<string>()),
        ResultingRoleIds = new List<string>(e.ResultingRoleIds ?? new List<string>()),
        PreviousPermissions = new List<string>(e.PreviousPermissions ?? new List<string>()),
        ResultingPermissions = new List<string>(e.ResultingPermissions ?? new List<string>()),
        Permission = e.Permission,
        ResolvedScope = e.ResolvedScope?.Clone(),
        AuthorizationRevision = e.AuthorizationRevision,
        OccurredAt = e.When
      });
    }
  }

  public sealed class ScanProcessingAccessAuditEntry
  {
    public string AuditId { get; set; }
    public string RequestId { get; set; }
    public string Action { get; set; }
    public string Outcome { get; set; }
    public string Code { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public ScanProcessingActorIdentity TargetActor { get; set; }
    public string RoleId { get; set; }
    public List<string> PreviousRoleIds { get; set; } = new();
    public List<string> ResultingRoleIds { get; set; } = new();
    public List<string> PreviousPermissions { get; set; } = new();
    public List<string> ResultingPermissions { get; set; } = new();
    public string Permission { get; set; }
    public ScanProcessingResolvedScope ResolvedScope { get; set; }
    public long AuthorizationRevision { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
  }
}
