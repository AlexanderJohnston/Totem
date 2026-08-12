using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Quantum.Web.ScanProcessing
{
  public sealed class ScanProcessingAccessOptions
  {
    /// <summary>
    /// Temporary fixed demo value supplied at runtime through environment configuration or user secrets.
    /// There is deliberately no source-controlled default.
    /// </summary>
    public string DemoBootstrapSecret { get; set; }
  }

  public enum DemoBootstrapSecretValidation
  {
    NotConfigured,
    Invalid,
    Valid
  }

  public interface IDemoBootstrapSecretValidator
  {
    DemoBootstrapSecretValidation Validate(string candidate);
  }

  public sealed class DemoBootstrapSecretValidator : IDemoBootstrapSecretValidator
  {
    readonly IOptions<ScanProcessingAccessOptions> _options;

    public DemoBootstrapSecretValidator(IOptions<ScanProcessingAccessOptions> options)
    {
      _options = options;
    }

    public DemoBootstrapSecretValidation Validate(string candidate)
    {
      var configured = _options?.Value?.DemoBootstrapSecret;

      if(string.IsNullOrEmpty(configured))
      {
        return DemoBootstrapSecretValidation.NotConfigured;
      }

      if(string.IsNullOrEmpty(candidate))
      {
        return DemoBootstrapSecretValidation.Invalid;
      }

      var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
      var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(candidate));

      return CryptographicOperations.FixedTimeEquals(expectedHash, candidateHash)
        ? DemoBootstrapSecretValidation.Valid
        : DemoBootstrapSecretValidation.Invalid;
    }
  }
}
