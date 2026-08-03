using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  /// <summary>
  /// Durable facts consumed by the roll operation projection. No command or HTTP mutation emits them yet.
  /// </summary>
  public class ScanStartAccepted : Event
  {
    public Id RollId { get; set; }
    public string RowId { get; set; }
    public string ScanId { get; set; }

    public ScanStartAccepted(Id rollId, string rowId, string scanId)
    {
      RollId = rollId;
      RowId = rowId;
      ScanId = scanId;
    }
  }

  public class ScanStarted : Event
  {
    public Id RollId { get; set; }
    public string RowId { get; set; }
    public string ScanId { get; set; }

    public ScanStarted(Id rollId, string rowId, string scanId)
    {
      RollId = rollId;
      RowId = rowId;
      ScanId = scanId;
    }
  }

  public class ScanFinishAccepted : Event
  {
    public Id RollId { get; set; }
    public string RowId { get; set; }
    public string ScanId { get; set; }

    public ScanFinishAccepted(Id rollId, string rowId, string scanId)
    {
      RollId = rollId;
      RowId = rowId;
      ScanId = scanId;
    }
  }

  public class ScanFinished : Event
  {
    public Id RollId { get; set; }
    public string RowId { get; set; }
    public string ScanId { get; set; }

    public ScanFinished(Id rollId, string rowId, string scanId)
    {
      RollId = rollId;
      RowId = rowId;
      ScanId = scanId;
    }
  }

  public class ScanStartFailed : Event
  {
    public Id RollId { get; set; }
    public string RowId { get; set; }
    public string ScanId { get; set; }

    public ScanStartFailed(Id rollId, string rowId, string scanId)
    {
      RollId = rollId;
      RowId = rowId;
      ScanId = scanId;
    }
  }
}
