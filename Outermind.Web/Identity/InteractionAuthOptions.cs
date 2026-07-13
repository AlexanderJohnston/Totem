namespace Quantum.Web.Identity
{
  public sealed class InteractionAuthOptions
  {
    public string UserStorePath { get; set; } = "App_Data\\interaction-users.json";
    public string CookieName { get; set; } = "Totem.Auth";
    public int CookieLifetimeHours { get; set; } = 8;
    public bool SlidingExpiration { get; set; } = true;
    public bool RequireSecureCookies { get; set; }
    public string SameSite { get; set; } = "Lax";
  }
}
