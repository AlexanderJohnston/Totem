using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Quantum.Web.Identity
{
  public interface IApplicationUserManager
  {
    Task<ApplicationUserResult> RegisterAsync(string userName, string displayName, string password, CancellationToken cancellationToken);
    Task<ApplicationUserResult> LoginAsync(string userName, string password, CancellationToken cancellationToken);
  }

  public sealed class ApplicationUserManager : IApplicationUserManager
  {
    readonly IApplicationUserStore _store;
    readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public ApplicationUserManager(IApplicationUserStore store, IPasswordHasher<ApplicationUser> passwordHasher)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<ApplicationUserResult> RegisterAsync(string userName, string displayName, string password, CancellationToken cancellationToken)
    {
      var errors = ValidateRegistration(userName, password);

      if(errors.Any())
      {
        return ApplicationUserResult.ValidationFailed(errors);
      }

      var normalizedUserName = NormalizeUserName(userName);
      var user = new ApplicationUser
      {
        Id = Guid.NewGuid().ToString("N"),
        UserName = userName.Trim(),
        NormalizedUserName = normalizedUserName,
        DisplayName = NormalizeOptionalText(displayName) ?? userName.Trim(),
        ProcessUserId = CreateProcessUserId(normalizedUserName),
        CreatedAtUtc = DateTimeOffset.UtcNow
      };

      user.PasswordHash = _passwordHasher.HashPassword(user, password);

      if(!await _store.TryCreateAsync(user, cancellationToken).ConfigureAwait(false))
      {
        return ApplicationUserResult.DuplicateUser();
      }

      return ApplicationUserResult.Success(user);
    }

    public async Task<ApplicationUserResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
      if(string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
      {
        return ApplicationUserResult.InvalidCredentials();
      }

      var normalizedUserName = NormalizeUserName(userName);
      var user = await _store.FindByNormalizedUserNameAsync(normalizedUserName, cancellationToken).ConfigureAwait(false);

      if(user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
      {
        return ApplicationUserResult.InvalidCredentials();
      }

      var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

      if(verification == PasswordVerificationResult.Failed)
      {
        return ApplicationUserResult.InvalidCredentials();
      }

      if(verification == PasswordVerificationResult.SuccessRehashNeeded)
      {
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        await _store.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
      }

      return ApplicationUserResult.Success(user);
    }

    static IReadOnlyList<string> ValidateRegistration(string userName, string password)
    {
      var errors = new List<string>();
      var normalizedUserName = NormalizeOptionalText(userName);

      if(string.IsNullOrWhiteSpace(normalizedUserName))
      {
        errors.Add("Username is required.");
      }
      else
      {
        if(normalizedUserName.Length < 3 || normalizedUserName.Length > 64)
        {
          errors.Add("Username must be between 3 and 64 characters.");
        }

        if(normalizedUserName.Any(character => !IsAllowedUserNameCharacter(character)))
        {
          errors.Add("Username may contain only letters, numbers, dots, underscores, hyphens, or @.");
        }

        if(!normalizedUserName.Any(char.IsLetterOrDigit))
        {
          errors.Add("Username must contain at least one letter or number.");
        }
      }

      if(string.IsNullOrEmpty(password) || password.Length < 8)
      {
        errors.Add("Password must be at least 8 characters.");
      }

      if(password?.Length > 256)
      {
        errors.Add("Password is too long.");
      }

      return errors;
    }

    static bool IsAllowedUserNameCharacter(char character) =>
      char.IsLetterOrDigit(character) || character == '.' || character == '_' || character == '-' || character == '@';

    static string NormalizeUserName(string userName) =>
      NormalizeOptionalText(userName)?.ToUpperInvariant();

    static string NormalizeOptionalText(string value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static string CreateProcessUserId(string normalizedUserName)
    {
      var builder = new StringBuilder(normalizedUserName.Length);
      var lastWasSeparator = false;

      foreach(var character in normalizedUserName)
      {
        if(char.IsLetterOrDigit(character))
        {
          builder.Append(character);
          lastWasSeparator = false;
        }
        else if(!lastWasSeparator)
        {
          builder.Append('_');
          lastWasSeparator = true;
        }
      }

      return builder.ToString().Trim('_');
    }
  }

  public sealed class ApplicationUserResult
  {
    ApplicationUserResult(ApplicationUser user, ApplicationUserResultStatus status, IReadOnlyList<string> errors)
    {
      User = user;
      Status = status;
      Errors = errors ?? Array.Empty<string>();
    }

    public ApplicationUser User { get; }
    public ApplicationUserResultStatus Status { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool Succeeded => Status == ApplicationUserResultStatus.Succeeded;

    public static ApplicationUserResult Success(ApplicationUser user) =>
      new(user, ApplicationUserResultStatus.Succeeded, Array.Empty<string>());

    public static ApplicationUserResult ValidationFailed(IReadOnlyList<string> errors) =>
      new(null, ApplicationUserResultStatus.ValidationFailed, errors);

    public static ApplicationUserResult DuplicateUser() =>
      new(null, ApplicationUserResultStatus.DuplicateUser, new[] { "Registration could not be completed for that username." });

    public static ApplicationUserResult InvalidCredentials() =>
      new(null, ApplicationUserResultStatus.InvalidCredentials, new[] { "Invalid username or password." });
  }

  public enum ApplicationUserResultStatus
  {
    Succeeded,
    ValidationFailed,
    DuplicateUser,
    InvalidCredentials
  }
}
