using Totem;
using Totem.Timeline;
using System.Text.Json.Serialization;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Projects roll-owned operation state and a business revision from durable roll facts.
  /// The resource version is deliberately independent of QueryHub and query ETags.
  /// </summary>
  public class RollOperationQuery : Query
  {
    public Id RollId { get; set; }
    public RollScanState ScanState { get; set; } = RollScanState.Idle;
    public string ActiveScanId { get; set; }
    public long ResourceRevision { get; set; }

    [JsonIgnore]
    public bool IsScanning => ScanState != RollScanState.Idle;

    [JsonIgnore]
    public string ResourceVersion => RollResourceVersion.FromRevision(ResourceRevision);

    static Id RouteFirst(RollCreated e) => e.Roll.RollId;
    static Id Route(RollMicrofilmTableColumnsChanged e) => e.RollId;
    static Id Route(RollMicrofilmRowCreated e) => e.RollId;
    static Id Route(RollMicrofilmRowCellChanged e) => e.RollId;
    static Id Route(ScanStartAccepted e) => e.RollId;
    static Id Route(ScanStarted e) => e.RollId;
    static Id Route(ScanFinishAccepted e) => e.RollId;
    static Id Route(ScanFinished e) => e.RollId;
    static Id Route(ScanStartFailed e) => e.RollId;

    void Given(RollCreated e)
    {
      RollId = e.Roll.RollId;
      AdvanceRevision();
    }

    void Given(RollMicrofilmTableColumnsChanged e) => AdvanceRevision();
    void Given(RollMicrofilmRowCreated e) => AdvanceRevision();
    void Given(RollMicrofilmRowCellChanged e) => AdvanceRevision();

    void Given(ScanStartAccepted e)
    {
      ScanState = RollScanState.Starting;
      ActiveScanId = e.ScanId;
      AdvanceRevision();
    }

    void Given(ScanStarted e)
    {
      ScanState = RollScanState.Active;
      ActiveScanId = e.ScanId;
      AdvanceRevision();
    }

    void Given(ScanFinishAccepted e)
    {
      ScanState = RollScanState.Finishing;
      ActiveScanId = e.ScanId;
      AdvanceRevision();
    }

    void Given(ScanFinished e)
    {
      ScanState = RollScanState.Idle;
      ActiveScanId = null;
      AdvanceRevision();
    }

    void Given(ScanStartFailed e)
    {
      ScanState = RollScanState.Idle;
      ActiveScanId = null;
      AdvanceRevision();
    }

    void AdvanceRevision() => ResourceRevision++;
  }
}
