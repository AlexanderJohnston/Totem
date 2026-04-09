using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class BoxAlreadyExists : Event
  {
    public string BoxName { get; set; }
    public Id ClientId { get; set; }

    public BoxAlreadyExists(string boxName, Id clientId)
    {
      BoxName = boxName;
      ClientId = clientId;
    }
  }

  public class BoxCreated : Event
  {
    public KnownBox Box { get; set; }

    public BoxCreated(KnownBox box)
    {
      Box = box;
    }
  }

  public class ClientAlreadyExists : Event
  {
    public string JobName { get; set; }
    public string JobNumber { get; set; }
    public Id ServerId { get; set; }

    public ClientAlreadyExists(string jobName, string jobNumber, Id serverId)
    {
      JobName = jobName;
      JobNumber = jobNumber;
      ServerId = serverId;
    }
  }

  public class ClientCreated : Event
  {
    public KnownClient Client { get; set; }

    public ClientCreated(KnownClient client)
    {
      Client = client;
    }
  }

  public class OperatorAlreadyExists : Event
  {
    public string OperatorName { get; set; }
    public Id OperatorId { get; set; }

    public OperatorAlreadyExists(string operatorName, Id operatorId)
    {
      OperatorName = operatorName;
      OperatorId = operatorId;
    }
  }

  public class OperatorAssigned : Event
  {
    public KnownOperator Operator { get; set; }
    public List<Id> Rolls { get; set; }
    public Id BoxId { get; set; }

    public OperatorAssigned(KnownOperator @operator, List<Id> rolls, Id boxId)
    {
      Operator = @operator;
      Rolls = rolls;
      BoxId = boxId;
    }
  }

  public class OperatorCreated : Event
  {
    public KnownOperator Operator { get; set; }

    public OperatorCreated(KnownOperator @operator)
    {
      Operator = @operator;
    }
  }

  public class OperatorNotRecognized : Event
  {
    public string OperatorName { get; set; }

    public OperatorNotRecognized(string operatorName)
    {
      OperatorName = operatorName;
    }
  }

  public class RollAlreadyExists : Event
  {
    public string RollName { get; set; }
    public Id BoxId { get; set; }

    public RollAlreadyExists(string rollName, Id boxId)
    {
      RollName = rollName;
      BoxId = boxId;
    }
  }

  public class RollCreated : Event
  {
    public KnownRoll Roll { get; set; }

    public RollCreated(KnownRoll roll)
    {
      Roll = roll;
    }
  }

  public class ServerAlreadyExists : Event
  {
    public string ServerName { get; set; }

    public ServerAlreadyExists(string serverName)
    {
      ServerName = serverName;
    }
  }

  public class ServerCreated : Event
  {
    public KnownServer Server { get; set; }

    public ServerCreated(KnownServer server)
    {
      Server = server;
    }
  }

  public class ClientAlreadyAssignedToServer : Event
  {
    public KnownClient Client { get; set; }
    public Id ServerId { get; set; }

    public ClientAlreadyAssignedToServer(KnownClient client, Id serverId)
    {
      Client = client;
      ServerId = serverId;
    }
  }

  public class ClientNotRecognized : Event
  {
    public Id ClientId { get; set; }

    public ClientNotRecognized(Id clientId)
    {
      ClientId = clientId;
    }
  }

  public class ClientReassigned : Event
  {
    public KnownClient Client { get; set; }
    public Id PreviousServerId { get; set; }

    public ClientReassigned(KnownClient client, Id previousServerId)
    {
      Client = client;
      PreviousServerId = previousServerId;
    }
  }

  public class HourlyWaspImportEvent : Event
  {
    public bool ImportEnabled { get; set; }
    public string Trigger { get; set; }

    public HourlyWaspImportEvent(bool importEnabled, string trigger)
    {
      ImportEnabled = importEnabled;
      Trigger = trigger;
    }
  }

  public class ManualWaspImportEvent : Event
  {
    public string Trigger { get; set; }

    public ManualWaspImportEvent(string trigger)
    {
      Trigger = trigger;
    }
  }

  public class ServerNotRecognized : Event
  {
    public Id ServerId { get; set; }

    public ServerNotRecognized(Id serverId)
    {
      ServerId = serverId;
    }
  }

  public class WaspAssetAlreadyImported : Event
  {
    public string AssetId { get; set; }
    public string Reason { get; set; }

    public WaspAssetAlreadyImported(string assetId, string reason)
    {
      AssetId = assetId;
      Reason = reason;
    }
  }

  public class WaspAssetDeferred : Event
  {
    public KnownWaspAsset Asset { get; set; }
    public string Reason { get; set; }

    public WaspAssetDeferred(KnownWaspAsset asset, string reason)
    {
      Asset = asset;
      Reason = reason;
    }
  }

  public class WaspBoxIdentified : Event
  {
    public string AssetId { get; set; }
    public string JobNumber { get; set; }
    public string BoxName { get; set; }
    public Id ClientId { get; set; }

    public WaspBoxIdentified(string assetId, string jobNumber, string boxName, Id clientId)
    {
      AssetId = assetId;
      JobNumber = jobNumber;
      BoxName = boxName;
      ClientId = clientId;
    }
  }

  public class WaspImportAlreadyInRequestedState : Event
  {
    public bool ImportEnabled { get; set; }
    public string Reason { get; set; }

    public WaspImportAlreadyInRequestedState(bool importEnabled, string reason)
    {
      ImportEnabled = importEnabled;
      Reason = reason;
    }
  }

  public class WaspImportCompleted : Event
  {
    public int ImportedAssetCount { get; set; }
    public int DeferredAssetCount { get; set; }
    public int IgnoredAssetCount { get; set; }
    public List<string> ImportedAssetIds { get; set; }

    public WaspImportCompleted(int importedAssetCount, int deferredAssetCount, int ignoredAssetCount, List<string> importedAssetIds)
    {
      ImportedAssetCount = importedAssetCount;
      DeferredAssetCount = deferredAssetCount;
      IgnoredAssetCount = ignoredAssetCount;
      ImportedAssetIds = importedAssetIds;
    }
  }

  public class WaspImportAssetRequeued : Event
  {
    public string AssetId { get; set; }

    public WaspImportAssetRequeued(string assetId)
    {
      AssetId = assetId;
    }
  }

  public class WaspImportBatchLoaded : Event
  {
    public List<string> AssetIds { get; set; }

    public WaspImportBatchLoaded(List<string> assetIds)
    {
      AssetIds = assetIds ?? new List<string>();
    }
  }

  public class WaspImportEnabledSet : Event
  {
    public bool ImportEnabled { get; set; }
    public string Reason { get; set; }

    public WaspImportEnabledSet(bool importEnabled, string reason)
    {
      ImportEnabled = importEnabled;
      Reason = reason;
    }
  }

  public class WaspImportFailed : Event
  {
    public string Error { get; set; }
    public string Step { get; set; }

    public WaspImportFailed(string error, string step)
    {
      Error = error;
      Step = step;
    }
  }

  public class WaspLegacyAssetIgnored : Event
  {
    public string AssetId { get; set; }
    public string Reason { get; set; }

    public WaspLegacyAssetIgnored(string assetId, string reason)
    {
      AssetId = assetId;
      Reason = reason;
    }
  }

  public class WaspRollIdentified : Event
  {
    public string AssetId { get; set; }
    public string JobNumber { get; set; }
    public string RollName { get; set; }
    public Id BoxId { get; set; }
    public Id ClientId { get; set; }

    public WaspRollIdentified(string assetId, string jobNumber, string rollName, Id boxId, Id clientId)
    {
      AssetId = assetId;
      JobNumber = jobNumber;
      RollName = rollName;
      BoxId = boxId;
      ClientId = clientId;
    }
  }
}
