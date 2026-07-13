using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Quantum.Web.Identity
{
  public sealed class CsrfTokenService : ICsrfTokenService
  {
    readonly IOptions<CsrfOptions> _csrfOptions;
    readonly IOptions<InteractionAuthOptions> _authOptions;

    public CsrfTokenService(IOptions<CsrfOptions> csrfOptions, IOptions<InteractionAuthOptions> authOptions)
    {
      _csrfOptions = csrfOptions ?? throw new ArgumentNullException(nameof(csrfOptions));
      _authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
    }

    public string CreateToken() =>
      WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public void SetTokenCookie(HttpResponse response, string token)
    {
      if(response == null)
      {
        throw new ArgumentNullException(nameof(response));
      }

      response.Cookies.Append(
        _csrfOptions.Value.CookieName,
        token,
        new CookieOptions
        {
          HttpOnly = false,
          IsEssential = true,
          Path = "/",
          SameSite = ParseSameSite(_authOptions.Value.SameSite),
          Secure = _authOptions.Value.RequireSecureCookies || response.HttpContext.Request.IsHttps
        });
    }

    public bool IsRequestTokenValid(HttpRequest request)
    {
      if(request == null)
      {
        return false;
      }

      var cookieToken = request.Cookies[_csrfOptions.Value.CookieName];
      var headerToken = request.Headers[_csrfOptions.Value.HeaderName].ToString();

      if(string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(headerToken))
      {
        return false;
      }

      return CryptographicOperations.FixedTimeEquals(
        System.Text.Encoding.UTF8.GetBytes(cookieToken),
        System.Text.Encoding.UTF8.GetBytes(headerToken));
    }

    static SameSiteMode ParseSameSite(string value) =>
      string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase)
        ? SameSiteMode.Strict
        : string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)
          ? SameSiteMode.None
          : SameSiteMode.Lax;
  }
}
