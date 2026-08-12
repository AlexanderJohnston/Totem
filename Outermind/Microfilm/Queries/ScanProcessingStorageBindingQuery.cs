using System;
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Projects logical storage binding state for one Version 1 client/workspace.
  /// </summary>
  public sealed class ScanProcessingStorageBindingQuery : Query
  {
    public ScanProcessingStorageBindingState State { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id Route(ScanProcessingStorageBindingCreated e) => Id.From(e.ClientId);
    static Id Route(ScanProcessingStorageBindingActivated e) => Id.From(e.ClientId);

    void Given(ClientCreated e) => State.Apply(e);
    void Given(ScanProcessingStorageBindingCreated e) => State.Apply(e);
    void Given(ScanProcessingStorageBindingActivated e) => State.Apply(e);
  }

  /// <summary>
  /// Append-only, workspace-routed logical storage audit projection.
  /// </summary>
  public sealed class ScanProcessingStorageBindingAuditQuery : Query
  {
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public List<ScanProcessingStorageBindingAuditEntry> Entries { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id Route(ScanProcessingStorageBindingAuditRecorded e) => Id.From(e.ClientId);

    void Given(ClientCreated e)
    {
      ClientId = e.Client.ClientId.ToString();
      WorkspaceId = ClientId;
    }

    void Given(ScanProcessingStorageBindingAuditRecorded e)
    {
      ClientId = e.ClientId;
      WorkspaceId = e.WorkspaceId;
      Entries.Add(new ScanProcessingStorageBindingAuditEntry
      {
        AuditId = e.AuditId,
        RequestId = e.RequestId,
        Action = e.Action,
        Outcome = e.Outcome,
        Code = e.Code,
        ActingActor = e.ActingActor?.Clone(),
        BindingId = e.BindingId,
        ConfigurationGeneration = e.ConfigurationGeneration,
        StorageRevision = e.StorageRevision,
        OccurredAt = e.When
      });
    }
  }

  public sealed class ScanProcessingStorageBindingAuditEntry
  {
    public string AuditId { get; set; }
    public string RequestId { get; set; }
    public string Action { get; set; }
    public string Outcome { get; set; }
    public string Code { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public string BindingId { get; set; }
    public long? ConfigurationGeneration { get; set; }
    public long StorageRevision { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
  }
}
