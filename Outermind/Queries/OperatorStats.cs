using Outermind;
using Totem;
using Totem.Timeline;

namespace Quantum.Queries
{
  public class OperatorStats : Query
  {
    static Id RouteFirst(ScanDetected e) => e.UserId;

    public int FilesTouched;

    void Given(ScanDetected e)
    {
      FilesTouched += e.Scan.FileCount;
    }
  }
}
