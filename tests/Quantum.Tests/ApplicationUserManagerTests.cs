using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Quantum.Web.Identity;
using Xunit;

namespace Quantum.Tests
{
  public sealed class ApplicationUserManagerTests
  {
    [Fact]
    public async Task RegisterAsync_CreatesPasswordHashedUserWithServerProcessUserId()
    {
      var store = new InMemoryApplicationUserStore();
      var manager = Manager(store);

      var result = await manager.RegisterAsync("ajohnston", "Alex Johnston", "CorrectHorse1", CancellationToken.None);

      Assert.True(result.Succeeded);
      Assert.Equal("AJOHNSTON", result.User.ProcessUserId);
      Assert.Equal("AJOHNSTON", result.User.NormalizedUserName);
      Assert.NotEqual("CorrectHorse1", result.User.PasswordHash);
      Assert.NotNull(await store.FindByNormalizedUserNameAsync("AJOHNSTON", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ReturnsDuplicateUser()
    {
      var manager = Manager(new InMemoryApplicationUserStore());

      Assert.True((await manager.RegisterAsync("ajohnston", null, "CorrectHorse1", CancellationToken.None)).Succeeded);
      var duplicate = await manager.RegisterAsync("AJohnston", null, "CorrectHorse1", CancellationToken.None);

      Assert.Equal(ApplicationUserResultStatus.DuplicateUser, duplicate.Status);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsUser()
    {
      var manager = Manager(new InMemoryApplicationUserStore());
      var registered = await manager.RegisterAsync("ajohnston", "Alex Johnston", "CorrectHorse1", CancellationToken.None);

      var login = await manager.LoginAsync("AJOHNSTON", "CorrectHorse1", CancellationToken.None);

      Assert.True(login.Succeeded);
      Assert.Equal(registered.User.Id, login.User.Id);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsGenericFailure()
    {
      var manager = Manager(new InMemoryApplicationUserStore());
      await manager.RegisterAsync("ajohnston", "Alex Johnston", "CorrectHorse1", CancellationToken.None);

      var login = await manager.LoginAsync("ajohnston", "wrong-password", CancellationToken.None);

      Assert.Equal(ApplicationUserResultStatus.InvalidCredentials, login.Status);
      Assert.Equal("Invalid username or password.", Assert.Single(login.Errors));
    }

    static ApplicationUserManager Manager(IApplicationUserStore store) =>
      new(store, new PasswordHasher<ApplicationUser>());

    public sealed class InMemoryApplicationUserStore : IApplicationUserStore
    {
      readonly List<ApplicationUser> _users = new();

      public Task<ApplicationUser> FindByNormalizedUserNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(user =>
          string.Equals(user.NormalizedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase)));

      public Task<bool> TryCreateAsync(ApplicationUser user, CancellationToken cancellationToken)
      {
        if(_users.Any(existing =>
          string.Equals(existing.NormalizedUserName, user.NormalizedUserName, StringComparison.OrdinalIgnoreCase)))
        {
          return Task.FromResult(false);
        }

        _users.Add(user);
        return Task.FromResult(true);
      }

      public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
      {
        var index = _users.FindIndex(existing => existing.Id == user.Id);

        if(index < 0)
        {
          throw new InvalidOperationException("User not found.");
        }

        _users[index] = user;
        return Task.CompletedTask;
      }
    }
  }
}
