using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class AssignOperator : Command
  {
    public Id OperatorId { get; set; }
    public Id ClientId { get; set; }
    public Id BoxId { get; set; }
    public Id[] RollIds { get; set; }

    public AssignOperator(Id operatorId, Id clientId, Id boxId, Id[] rollIds)
    {
      OperatorId = operatorId;
      ClientId = clientId;
      BoxId = boxId;
      RollIds = rollIds;
    }
  }

  public class CreateBox : Command
  {
    public string BoxName { get; set; }
    public Id ClientId { get; set; }

    public CreateBox(string boxName, Id clientId)
    {
      BoxName = boxName;
      ClientId = clientId;
    }
  }

  public class CreateOperator : Command
  {
    public string OperatorName { get; set; }

    public CreateOperator(string operatorName)
    {
      OperatorName = operatorName;
    }
  }

  public class CreateRoll : Command
  {
    public string RollName { get; set; }
    public Id BoxId { get; set; }
    public Id ClientId { get; set; }

    public CreateRoll(string rollName, Id boxId, Id clientId)
    {
      RollName = rollName;
      BoxId = boxId;
      ClientId = clientId;
    }
  }

  public class NewClient : Command
  {
    public string JobName { get; set; }
    public string JobNumber { get; set; }
    public Id ServerId { get; set; }

    public NewClient(string jobName, string jobNumber, Id serverId)
    {
      JobName = jobName;
      JobNumber = jobNumber;
      ServerId = serverId;
    }
  }

  public class NewServer : Command
  {
    public string ServerName { get; set; }

    public NewServer(string serverName)
    {
      ServerName = serverName;
    }
  }

  public class ChangeClientAssignment : Command
  {
    public Id ClientId { get; set; }
    public Id ServerId { get; set; }

    public ChangeClientAssignment(Id clientId, Id serverId)
    {
      ClientId = clientId;
      ServerId = serverId;
    }
  }

  public class SetWaspImportEnabled : Command
  {
    public bool ImportEnabled { get; set; }

    public SetWaspImportEnabled(bool importEnabled)
    {
      ImportEnabled = importEnabled;
    }
  }
}
