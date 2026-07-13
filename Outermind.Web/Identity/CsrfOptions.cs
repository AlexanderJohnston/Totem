namespace Quantum.Web.Identity
{
  public sealed class CsrfOptions
  {
    public string CookieName { get; set; } = "Totem.Csrf";
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";
  }
}
