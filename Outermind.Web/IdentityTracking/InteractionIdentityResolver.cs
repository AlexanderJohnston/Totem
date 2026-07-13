using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Quantum.Web.IdentityTracking
{
  public sealed class InteractionIdentityResolver : IInteractionIdentityResolver
  {
    readonly IOptions<InteractionIdentityOptions> _options;

    public InteractionIdentityResolver(IOptions<InteractionIdentityOptions> options)
    {
      _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public InteractionSession Resolve(ClaimsPrincipal principal)
    {
      var options = _options.Value ?? new InteractionIdentityOptions();

      if(principal?.Identity?.IsAuthenticated != true)
      {
        return Unidentified(options);
      }

      if(IsBackendCookiePrincipal(principal))
      {
        return ResolveBackendCookiePrincipal(principal, options);
      }

      var observed = ObservedIdentity.From(principal);

      if(!observed.Candidates.Any())
      {
        return Unidentified(options);
      }

      var mappings = BuildMappings(options);

      foreach(var candidate in observed.Candidates)
      {
        if(mappings.TryGetValue(candidate.LookupKey, out var mapping))
        {
          return new InteractionSession
          {
            Status = InteractionTrackingStatus.Identified,
            DisplayLabel = FirstNonBlank(mapping.DisplayLabel, mapping.ProcessUserId, observed.DisplayLabel),
            WindowsAccount = observed.WindowsAccount,
            UserPrincipalName = observed.UserPrincipalName,
            ProcessUserId = mapping.ProcessUserId,
            TrackingSource = PrincipalTrackingSource(options)
          };
        }
      }

      return new InteractionSession
      {
        Status = InteractionTrackingStatus.Unmapped,
        DisplayLabel = FirstNonBlank(observed.DisplayLabel, "Unmapped user"),
        WindowsAccount = observed.WindowsAccount,
        UserPrincipalName = observed.UserPrincipalName,
        ProcessUserId = null,
        TrackingSource = PrincipalTrackingSource(options)
      };
    }

    static bool IsBackendCookiePrincipal(ClaimsPrincipal principal) =>
      string.Equals(
        principal.FindFirstValue(InteractionAuthClaims.TrackingSource),
        InteractionTrackingSources.BackendCookie,
        StringComparison.OrdinalIgnoreCase);

    static InteractionSession ResolveBackendCookiePrincipal(ClaimsPrincipal principal, InteractionIdentityOptions options)
    {
      var userId = NormalizeText(principal.FindFirstValue(ClaimTypes.NameIdentifier));
      var userName = NormalizeText(principal.FindFirstValue(InteractionAuthClaims.UserName));
      var processUserId = NormalizeText(principal.FindFirstValue(InteractionAuthClaims.ProcessUserId));
      var displayLabel = FirstNonBlank(
        principal.FindFirstValue(ClaimTypes.Name),
        userName,
        principal.Identity?.Name,
        userId,
        "Backend user");

      if(string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(processUserId))
      {
        return new InteractionSession
        {
          Status = InteractionTrackingStatus.Unmapped,
          DisplayLabel = displayLabel,
          WindowsAccount = null,
          UserPrincipalName = null,
          ProcessUserId = null,
          TrackingSource = InteractionTrackingSources.BackendCookie
        };
      }

      if(string.IsNullOrWhiteSpace(processUserId))
      {
        return new InteractionSession
        {
          Status = InteractionTrackingStatus.Unmapped,
          DisplayLabel = displayLabel,
          WindowsAccount = null,
          UserPrincipalName = null,
          ProcessUserId = null,
          TrackingSource = InteractionTrackingSources.BackendCookie
        };
      }

      return new InteractionSession
      {
        Status = InteractionTrackingStatus.Identified,
        DisplayLabel = displayLabel,
        WindowsAccount = null,
        UserPrincipalName = null,
        ProcessUserId = processUserId,
        TrackingSource = InteractionTrackingSources.BackendCookie
      };
    }

    static InteractionSession Unidentified(InteractionIdentityOptions options) =>
      new()
      {
        Status = InteractionTrackingStatus.Unidentified,
        DisplayLabel = FirstNonBlank(options.UnidentifiedDisplayLabel, "Unidentified user"),
        WindowsAccount = null,
        UserPrincipalName = null,
        ProcessUserId = null,
        TrackingSource = InteractionTrackingSources.None
      };

    static string PrincipalTrackingSource(InteractionIdentityOptions options) =>
      FirstNonBlank(options.TrackingSourceWhenPrincipalPresent, InteractionTrackingSources.WindowsIntegratedAuth);

    static Dictionary<string, ConfiguredIdentityMapping> BuildMappings(InteractionIdentityOptions options)
    {
      var mappings = new Dictionary<string, ConfiguredIdentityMapping>(StringComparer.OrdinalIgnoreCase);

      foreach(var configured in options.AccountMappings ?? Enumerable.Empty<InteractionIdentityMappingOptions>())
      {
        var processUserId = NormalizeText(configured?.ProcessUserId);

        if(string.IsNullOrWhiteSpace(processUserId))
        {
          continue;
        }

        foreach(var account in configured.Accounts ?? Enumerable.Empty<string>())
        {
          var key = NormalizeLookupKey(account);

          if(string.IsNullOrWhiteSpace(key))
          {
            continue;
          }

          var mapping = new ConfiguredIdentityMapping(processUserId, NormalizeText(configured.DisplayLabel));

          if(mappings.TryGetValue(key, out var existing))
          {
            if(!string.Equals(existing.ProcessUserId, mapping.ProcessUserId, StringComparison.OrdinalIgnoreCase))
            {
              throw new InvalidOperationException("InteractionIdentity account mapping is configured with conflicting process user ids.");
            }

            continue;
          }

          mappings.Add(key, mapping);
        }
      }

      return mappings;
    }

    static string NormalizeLookupKey(string value)
    {
      var normalized = NormalizeText(value);

      return string.IsNullOrWhiteSpace(normalized)
        ? null
        : normalized.Replace('/', '\\');
    }

    static string NormalizeText(string value) =>
      string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static string FirstNonBlank(params string[] values) =>
      values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    sealed class ConfiguredIdentityMapping
    {
      public ConfiguredIdentityMapping(string processUserId, string displayLabel)
      {
        ProcessUserId = processUserId;
        DisplayLabel = displayLabel;
      }

      public string ProcessUserId { get; }
      public string DisplayLabel { get; }
    }

    sealed class ObservedIdentity
    {
      readonly List<ObservedIdentityCandidate> _candidates = new();
      readonly HashSet<string> _candidateKeys = new(StringComparer.OrdinalIgnoreCase);

      public IReadOnlyList<ObservedIdentityCandidate> Candidates => _candidates;
      public string WindowsAccount { get; private set; }
      public string UserPrincipalName { get; private set; }
      public string DisplayLabel { get; private set; }

      public static ObservedIdentity From(ClaimsPrincipal principal)
      {
        var observed = new ObservedIdentity();

        observed.AddClaimValues(principal, ClaimTypes.WindowsAccountName);
        observed.AddClaimValues(principal, ClaimTypes.Upn);
        observed.AddClaimValues(principal, "upn");
        observed.AddClaimValues(principal, ClaimTypes.Name);
        observed.AddClaimValues(principal, ClaimTypes.NameIdentifier);
        observed.AddCandidate(principal?.Identity?.Name);

        return observed;
      }

      void AddClaimValues(ClaimsPrincipal principal, string claimType)
      {
        foreach(var claim in principal?.FindAll(claimType) ?? Enumerable.Empty<Claim>())
        {
          AddCandidate(claim.Value);
        }
      }

      void AddCandidate(string value)
      {
        var key = NormalizeLookupKey(value);

        if(string.IsNullOrWhiteSpace(key) || !_candidateKeys.Add(key))
        {
          return;
        }

        var canonicalWindowsAccount = TryCanonicalWindowsAccount(value);
        var canonicalUpn = TryCanonicalUpn(value);
        var displayValue = FirstNonBlank(canonicalWindowsAccount, canonicalUpn, NormalizeText(value));

        _candidates.Add(new ObservedIdentityCandidate(key));

        WindowsAccount ??= canonicalWindowsAccount;
        UserPrincipalName ??= canonicalUpn;
        DisplayLabel ??= displayValue;
      }

      static string TryCanonicalWindowsAccount(string value)
      {
        var normalized = NormalizeLookupKey(value);

        if(string.IsNullOrWhiteSpace(normalized))
        {
          return null;
        }

        var separator = normalized.IndexOf('\\');

        if(separator <= 0 || separator >= normalized.Length - 1)
        {
          return null;
        }

        var domain = normalized.Substring(0, separator).Trim().ToUpperInvariant();
        var user = normalized.Substring(separator + 1).Trim();

        return string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(user)
          ? null
          : $"{domain}\\{user}";
      }

      static string TryCanonicalUpn(string value)
      {
        var normalized = NormalizeText(value);

        if(string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@'))
        {
          return null;
        }

        return normalized;
      }
    }

    sealed class ObservedIdentityCandidate
    {
      public ObservedIdentityCandidate(string lookupKey)
      {
        LookupKey = lookupKey;
      }

      public string LookupKey { get; }
    }
  }
}
