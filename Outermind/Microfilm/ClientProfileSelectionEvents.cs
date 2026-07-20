using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class MicrofilmClientProfileSelectionChanged : Event
  {
    public Id ClientId { get; set; }
    public string ProfileId { get; set; }

    public MicrofilmClientProfileSelectionChanged(Id clientId, string profileId)
    {
      ClientId = clientId;
      ProfileId = profileId;
    }
  }
}
