using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Projects the optional presentation profile selected by one microfilm client.
  /// </summary>
  public class MicrofilmClientProfileSelectionQuery : Query
  {
    public Id ClientId { get; set; }
    public string ProfileId { get; set; }

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id Route(MicrofilmClientProfileSelectionChanged e) => e.ClientId;

    void Given(ClientCreated e)
    {
      ClientId = e.Client.ClientId;
    }

    void Given(MicrofilmClientProfileSelectionChanged e)
    {
      ClientId = e.ClientId;
      ProfileId = e.ProfileId;
    }
  }
}
