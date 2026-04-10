using Totem;

namespace Outermind.Microfilm
{
  public static class RollIds
  {
    public static Id From(Id clientId, Id boxId, string rollName) =>
      Id.From($"{clientId}:{boxId}:{Id.From(rollName)}");
  }
}
