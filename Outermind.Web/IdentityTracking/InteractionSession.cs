namespace Quantum.Web.IdentityTracking
{
  public sealed class InteractionSession
  {
    public string Status { get; init; }
    public string DisplayLabel { get; init; }
    public string WindowsAccount { get; init; }
    public string UserPrincipalName { get; init; }
    public string ProcessUserId { get; init; }
    public string TrackingSource { get; init; }
  }
}
