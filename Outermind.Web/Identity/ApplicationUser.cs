using System;

namespace Quantum.Web.Identity
{
  public sealed class ApplicationUser
  {
    public string Id { get; set; }
    public string UserName { get; set; }
    public string NormalizedUserName { get; set; }
    public string DisplayName { get; set; }
    public string PasswordHash { get; set; }
    public string ProcessUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
  }
}
