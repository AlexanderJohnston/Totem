using System.Collections.Generic;

namespace Quantum.Web.IdentityTracking
{
  public sealed class InteractionIdentityOptions
  {
    public string UnidentifiedDisplayLabel { get; set; } = "Unidentified user";
    public string TrackingSourceWhenPrincipalPresent { get; set; } = InteractionTrackingSources.WindowsIntegratedAuth;
    public List<InteractionIdentityMappingOptions> AccountMappings { get; set; } = new();
  }

  public sealed class InteractionIdentityMappingOptions
  {
    public List<string> Accounts { get; set; } = new();
    public string ProcessUserId { get; set; }
    public string DisplayLabel { get; set; }
  }
}
