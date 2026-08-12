using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public sealed class ScanProcessingStorageBindingCreated : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public ScanProcessingStorageBindingDefinition Binding { get; set; }
    public long StorageRevision { get; set; }

    public ScanProcessingStorageBindingCreated(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      string clientId,
      string workspaceId,
      ScanProcessingStorageBindingDefinition binding,
      long storageRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      ClientId = clientId;
      WorkspaceId = workspaceId;
      Binding = binding;
      StorageRevision = storageRevision;
    }
  }

  public sealed class ScanProcessingStorageBindingActivated : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public string BindingId { get; set; }
    public long ConfigurationGeneration { get; set; }
    public string PreviousBindingId { get; set; }
    public long? PreviousConfigurationGeneration { get; set; }
    public long StorageRevision { get; set; }

    public ScanProcessingStorageBindingActivated(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      string clientId,
      string workspaceId,
      string bindingId,
      long configurationGeneration,
      string previousBindingId,
      long? previousConfigurationGeneration,
      long storageRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      ClientId = clientId;
      WorkspaceId = workspaceId;
      BindingId = bindingId;
      ConfigurationGeneration = configurationGeneration;
      PreviousBindingId = previousBindingId;
      PreviousConfigurationGeneration = previousConfigurationGeneration;
      StorageRevision = storageRevision;
    }
  }

  public sealed class ScanProcessingStorageBindingActivationUnchanged : Event
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public string BindingId { get; set; }
    public long ConfigurationGeneration { get; set; }
    public long StorageRevision { get; set; }

    public ScanProcessingStorageBindingActivationUnchanged(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      string clientId,
      string workspaceId,
      string bindingId,
      long configurationGeneration,
      long storageRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      ClientId = clientId;
      WorkspaceId = workspaceId;
      BindingId = bindingId;
      ConfigurationGeneration = configurationGeneration;
      StorageRevision = storageRevision;
    }
  }

  public sealed class ScanProcessingStorageBindingRequestRejected : Event
  {
    public string RequestId { get; set; }
    public string Action { get; set; }
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string Field { get; set; }
    public long StorageRevision { get; set; }

    public ScanProcessingStorageBindingRequestRejected(
      string requestId,
      string action,
      string clientId,
      string workspaceId,
      string code,
      string message,
      string field,
      long storageRevision)
    {
      RequestId = requestId;
      Action = action;
      ClientId = clientId;
      WorkspaceId = workspaceId;
      Code = code;
      Message = message;
      Field = field;
      StorageRevision = storageRevision;
    }
  }

  /// <summary>
  /// Redacted durable audit fact. It contains logical identity and generation only, never labels,
  /// physical roots, paths, Server identifiers, credentials, or rejected raw values.
  /// </summary>
  public sealed class ScanProcessingStorageBindingAuditRecorded : Event
  {
    public string AuditId { get; set; }
    public string RequestId { get; set; }
    public string Action { get; set; }
    public string Outcome { get; set; }
    public string Code { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public string ClientId { get; set; }
    public string WorkspaceId { get; set; }
    public string BindingId { get; set; }
    public long? ConfigurationGeneration { get; set; }
    public long StorageRevision { get; set; }

    public ScanProcessingStorageBindingAuditRecorded(
      string auditId,
      string requestId,
      string action,
      string outcome,
      string code,
      ScanProcessingActorIdentity actingActor,
      string clientId,
      string workspaceId,
      string bindingId,
      long? configurationGeneration,
      long storageRevision)
    {
      AuditId = auditId;
      RequestId = requestId;
      Action = action;
      Outcome = outcome;
      Code = code;
      ActingActor = actingActor;
      ClientId = clientId;
      WorkspaceId = workspaceId;
      BindingId = bindingId;
      ConfigurationGeneration = configurationGeneration;
      StorageRevision = storageRevision;
    }

    public static Id NewAuditId() => Id.FromGuid();
  }
}
