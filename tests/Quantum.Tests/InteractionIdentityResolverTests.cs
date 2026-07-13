using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Quantum.Web.IdentityTracking;
using Xunit;

namespace Quantum.Tests
{
  public class InteractionIdentityResolverTests
  {
    [Fact]
    public void UnauthenticatedPrincipal_ReturnsUnidentified()
    {
      var session = Resolve(new ClaimsPrincipal(new ClaimsIdentity()));

      Assert.Equal(InteractionTrackingStatus.Unidentified, session.Status);
      Assert.Equal("Unidentified user", session.DisplayLabel);
      Assert.Null(session.WindowsAccount);
      Assert.Null(session.UserPrincipalName);
      Assert.Null(session.ProcessUserId);
      Assert.Equal(InteractionTrackingSources.None, session.TrackingSource);
    }

    [Fact]
    public void AuthenticatedWindowsAccountWithMapping_ReturnsIdentifiedSession()
    {
      var session = Resolve(
        Principal(new Claim(ClaimTypes.WindowsAccountName, @"domain\ajohnston")),
        new InteractionIdentityMappingOptions
        {
          Accounts = new List<string> { @"DOMAIN\ajohnston" },
          ProcessUserId = "AJOHNSTON",
          DisplayLabel = "Alex Johnston"
        });

      Assert.Equal(InteractionTrackingStatus.Identified, session.Status);
      Assert.Equal("Alex Johnston", session.DisplayLabel);
      Assert.Equal(@"DOMAIN\ajohnston", session.WindowsAccount);
      Assert.Null(session.UserPrincipalName);
      Assert.Equal("AJOHNSTON", session.ProcessUserId);
      Assert.Equal(InteractionTrackingSources.WindowsIntegratedAuth, session.TrackingSource);
    }

    [Fact]
    public void AuthenticatedUpnWithMapping_ReturnsIdentifiedSession()
    {
      var session = Resolve(
        Principal(new Claim(ClaimTypes.Upn, "Alex.Johnston@example.com")),
        new InteractionIdentityMappingOptions
        {
          Accounts = new List<string> { "alex.johnston@example.com" },
          ProcessUserId = "AJOHNSTON"
        });

      Assert.Equal(InteractionTrackingStatus.Identified, session.Status);
      Assert.Equal("AJOHNSTON", session.DisplayLabel);
      Assert.Null(session.WindowsAccount);
      Assert.Equal("Alex.Johnston@example.com", session.UserPrincipalName);
      Assert.Equal("AJOHNSTON", session.ProcessUserId);
    }

    [Fact]
    public void AuthenticatedPrincipalWithoutMapping_ReturnsUnmappedSession()
    {
      var session = Resolve(Principal(new Claim(ClaimTypes.Name, @"DOMAIN\unmapped")));

      Assert.Equal(InteractionTrackingStatus.Unmapped, session.Status);
      Assert.Equal(@"DOMAIN\unmapped", session.DisplayLabel);
      Assert.Equal(@"DOMAIN\unmapped", session.WindowsAccount);
      Assert.Null(session.UserPrincipalName);
      Assert.Null(session.ProcessUserId);
      Assert.Equal(InteractionTrackingSources.WindowsIntegratedAuth, session.TrackingSource);
    }

    [Fact]
    public void AuthenticatedPrincipalWithNoObservedIdentity_ReturnsUnidentifiedSession()
    {
      var session = Resolve(new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "test")));

      Assert.Equal(InteractionTrackingStatus.Unidentified, session.Status);
      Assert.Equal("Unidentified user", session.DisplayLabel);
      Assert.Null(session.WindowsAccount);
      Assert.Null(session.UserPrincipalName);
      Assert.Null(session.ProcessUserId);
      Assert.Equal(InteractionTrackingSources.None, session.TrackingSource);
    }

    [Fact]
    public void BackendCookiePrincipalWithProcessUserId_ReturnsIdentifiedSession()
    {
      var session = Resolve(Principal(
        new Claim(ClaimTypes.NameIdentifier, "user-1"),
        new Claim(ClaimTypes.Name, "Alex Johnston"),
        new Claim(InteractionAuthClaims.UserName, "ajohnston"),
        new Claim(InteractionAuthClaims.ProcessUserId, "AJOHNSTON"),
        new Claim(InteractionAuthClaims.TrackingSource, InteractionTrackingSources.BackendCookie)));

      Assert.Equal(InteractionTrackingStatus.Identified, session.Status);
      Assert.Equal("Alex Johnston", session.DisplayLabel);
      Assert.Null(session.WindowsAccount);
      Assert.Null(session.UserPrincipalName);
      Assert.Equal("AJOHNSTON", session.ProcessUserId);
      Assert.Equal(InteractionTrackingSources.BackendCookie, session.TrackingSource);
    }

    [Fact]
    public void BackendCookiePrincipalWithoutProcessUserId_ReturnsUnmappedSession()
    {
      var session = Resolve(Principal(
        new Claim(ClaimTypes.NameIdentifier, "user-1"),
        new Claim(ClaimTypes.Name, "Alex Johnston"),
        new Claim(InteractionAuthClaims.UserName, "ajohnston"),
        new Claim(InteractionAuthClaims.TrackingSource, InteractionTrackingSources.BackendCookie)));

      Assert.Equal(InteractionTrackingStatus.Unmapped, session.Status);
      Assert.Equal("Alex Johnston", session.DisplayLabel);
      Assert.Null(session.ProcessUserId);
      Assert.Equal(InteractionTrackingSources.BackendCookie, session.TrackingSource);
    }

    [Fact]
    public void DuplicateAccountMappingsWithDifferentProcessUsers_ThrowConfigurationError()
    {
      var resolver = Resolver(
        new InteractionIdentityMappingOptions
        {
          Accounts = new List<string> { @"DOMAIN\ajohnston" },
          ProcessUserId = "AJOHNSTON"
        },
        new InteractionIdentityMappingOptions
        {
          Accounts = new List<string> { @"domain/ajohnston" },
          ProcessUserId = "OTHER"
        });

      Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Principal(new Claim(ClaimTypes.Name, @"DOMAIN\ajohnston"))));
    }

    static InteractionSession Resolve(ClaimsPrincipal principal, params InteractionIdentityMappingOptions[] mappings) =>
      Resolver(mappings).Resolve(principal);

    static InteractionIdentityResolver Resolver(params InteractionIdentityMappingOptions[] mappings) =>
      new(Options.Create(new InteractionIdentityOptions
      {
        AccountMappings = new List<InteractionIdentityMappingOptions>(mappings)
      }));

    static ClaimsPrincipal Principal(params Claim[] claims) =>
      new(new ClaimsIdentity(claims, "test"));
  }
}
