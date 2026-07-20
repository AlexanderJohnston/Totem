using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Stores the optional presentation profile selected by an individual client.
  /// </summary>
  public class MicrofilmClientProfileSelectionTopic : Topic
  {
    bool _clientRecognized;
    string _profileId;

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id RouteFirst(SetMicrofilmClientProfileSelection e) => e.ClientId;
    static Id Route(SetMicrofilmClientProfileSelection e) => e.ClientId;
    static Id Route(MicrofilmClientProfileSelectionChanged e) => e.ClientId;

    void Given(ClientCreated e)
    {
      _clientRecognized = true;
    }

    void Given(MicrofilmClientProfileSelectionChanged e)
    {
      _profileId = e.ProfileId;
    }

    void When(SetMicrofilmClientProfileSelection command)
    {
      if(!_clientRecognized)
      {
        Then(new MicrofilmTableClientNotRecognized(command.ClientId));
        return;
      }

      if(command.ProfileId != null && string.IsNullOrWhiteSpace(command.ProfileId))
      {
        return;
      }

      var profileId = command.ProfileId?.Trim();
      Then(new MicrofilmClientProfileSelectionChanged(command.ClientId, profileId));
    }
  }
}
