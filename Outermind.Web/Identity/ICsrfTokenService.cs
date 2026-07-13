using Microsoft.AspNetCore.Http;

namespace Quantum.Web.Identity
{
  public interface ICsrfTokenService
  {
    string CreateToken();
    void SetTokenCookie(HttpResponse response, string token);
    bool IsRequestTokenValid(HttpRequest request);
  }
}
