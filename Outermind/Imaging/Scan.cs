using System;
using System.Collections.Generic;
using Totem;

namespace Quantum.Imaging
{
  public enum ScanStatus
  {
    InProgress,
    Finished,
    Error
  }

  public record ScanComment
  {
    public Id Operator;
    public DateTime Timestamp;
    public string Comment;

    public ScanComment(Id @operator, DateTime timestamp, string comment)
    {
      Operator = @operator;
      Timestamp = timestamp;
      Comment = comment;
    }
  }

  public class Scan
  {
    public Id Id;
    public Id Operator;
    public string Path;
    public int ImageCount;
    public DateTime Started;
    public DateTime Stopped;
    public List<ScanComment> Comments = new();
    public ScanStatus Status;

    public override bool Equals(object obj)
    {
      return obj is Scan scan && Id.Equals(scan.Id);
    }

    public override int GetHashCode()
    {
      var hashCode = -239686454;
      hashCode = hashCode * -1521134295 + Id.GetHashCode();
      return hashCode;
    }
  }
}