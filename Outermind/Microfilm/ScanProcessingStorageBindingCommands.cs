using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public sealed class CreateScanProcessingStorageBinding : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public Id ClientId { get; set; }
    public string BindingId { get; set; }
    public string Label { get; set; }
    public List<ScanProcessingStorageCapability> Capabilities { get; set; }

    public CreateScanProcessingStorageBinding(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      Id clientId,
      string bindingId,
      string label,
      IEnumerable<ScanProcessingStorageCapability> capabilities)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      ClientId = clientId;
      BindingId = bindingId;
      Label = label;
      Capabilities = capabilities == null
        ? new List<ScanProcessingStorageCapability>()
        : new List<ScanProcessingStorageCapability>(capabilities);
    }
  }

  public sealed class ActivateScanProcessingStorageBinding : Command
  {
    public string RequestId { get; set; }
    public ScanProcessingActorIdentity ActingActor { get; set; }
    public Id ClientId { get; set; }
    public string BindingId { get; set; }
    public long ConfigurationGeneration { get; set; }
    public long ExpectedStorageRevision { get; set; }

    public ActivateScanProcessingStorageBinding(
      string requestId,
      ScanProcessingActorIdentity actingActor,
      Id clientId,
      string bindingId,
      long configurationGeneration,
      long expectedStorageRevision)
    {
      RequestId = requestId;
      ActingActor = actingActor;
      ClientId = clientId;
      BindingId = bindingId;
      ConfigurationGeneration = configurationGeneration;
      ExpectedStorageRevision = expectedStorageRevision;
    }
  }
}
