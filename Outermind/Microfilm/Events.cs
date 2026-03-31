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
    public string ServerName { get; set; }

    public ClientAlreadyExists(string jobName, string jobNumber, string serverName)
    {
      JobName = jobName;
      JobNumber = jobNumber;
      ServerName = serverName;
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

    public OperatorAssigned(KnownOperator @operator, List<Id> rolls)
    {
      Operator = @operator;
      Rolls = rolls;
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
}
