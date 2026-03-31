using System;
using Totem;

namespace Outermind.Microfilm
{
  public class KnownBox : IEquatable<KnownBox>
  {
    public string BoxName { get; set; }
    public Id BoxId { get; set; }
    public Id ClientId { get; set; }

    public KnownBox(string boxName, Id boxId, Id clientId)
    {
      BoxName = boxName;
      BoxId = boxId;
      ClientId = clientId;
    }

    public bool Equals(KnownBox other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return BoxId == other.BoxId;
    }

    public override bool Equals(object obj) => Equals(obj as KnownBox);
    public override int GetHashCode() => BoxId.GetHashCode();
  }

  public class KnownClient : IEquatable<KnownClient>
  {
    public string JobName { get; set; }
    public string JobNumber { get; set; }
    public Id ClientId { get; set; }
    public string ServerName { get; set; }

    public KnownClient(string jobName, string jobNumber, Id clientId, string serverName)
    {
      JobName = jobName;
      JobNumber = jobNumber;
      ClientId = clientId;
      ServerName = serverName;
    }

    public bool Equals(KnownClient other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return ClientId == other.ClientId;
    }

    public override bool Equals(object obj) => Equals(obj as KnownClient);
    public override int GetHashCode() => ClientId.GetHashCode();
  }

  public class KnownOperator : IEquatable<KnownOperator>
  {
    public string OperatorName { get; set; }
    public Id OperatorId { get; set; }

    public KnownOperator(string operatorName, Id operatorId)
    {
      OperatorName = operatorName;
      OperatorId = operatorId;
    }

    public bool Equals(KnownOperator other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return OperatorId == other.OperatorId;
    }

    public override bool Equals(object obj) => Equals(obj as KnownOperator);
    public override int GetHashCode() => OperatorId.GetHashCode();
  }

  public class KnownRoll : IEquatable<KnownRoll>
  {
    public string RollName { get; set; }
    public Id RollId { get; set; }
    public Id BoxId { get; set; }

    public KnownRoll(string rollName, Id rollId, Id boxId)
    {
      RollName = rollName;
      RollId = rollId;
      BoxId = boxId;
    }

    public bool Equals(KnownRoll other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return RollId == other.RollId;
    }

    public override bool Equals(object obj) => Equals(obj as KnownRoll);
    public override int GetHashCode() => RollId.GetHashCode();
  }

  public class KnownServer : IEquatable<KnownServer>
  {
    public string ServerName { get; set; }
    public Id ServerId { get; set; }

    public KnownServer(string serverName, Id serverId)
    {
      ServerName = serverName;
      ServerId = serverId;
    }

    public bool Equals(KnownServer other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return ServerId == other.ServerId;
    }

    public override bool Equals(object obj) => Equals(obj as KnownServer);
    public override int GetHashCode() => ServerId.GetHashCode();
  }
}
