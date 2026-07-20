using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class SetMicrofilmClientProfileSelection : Command
  {
    public Id ClientId { get; set; }
    public string ProfileId { get; set; }

    public SetMicrofilmClientProfileSelection(Id clientId, string profileId)
    {
      ClientId = clientId;
      ProfileId = profileId;
    }
  }
}
