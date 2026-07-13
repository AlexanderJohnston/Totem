using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Quantum.Web.Identity
{
  public sealed class FileApplicationUserStore : IApplicationUserStore
  {
    static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
    };

    readonly SemaphoreSlim _gate = new(1, 1);
    readonly string _path;

    public FileApplicationUserStore(IOptions<InteractionAuthOptions> options, IWebHostEnvironment environment)
    {
      if(environment == null)
      {
        throw new ArgumentNullException(nameof(environment));
      }

      var configuredPath = options?.Value?.UserStorePath;
      _path = Path.IsPathRooted(configuredPath ?? "")
        ? configuredPath
        : Path.Combine(environment.ContentRootPath, string.IsNullOrWhiteSpace(configuredPath) ? "App_Data\\interaction-users.json" : configuredPath);
    }

    public async Task<ApplicationUser> FindByNormalizedUserNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
      if(string.IsNullOrWhiteSpace(normalizedUserName))
      {
        return null;
      }

      await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

      try
      {
        var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false);

        return file.Users.FirstOrDefault(user =>
          string.Equals(user.NormalizedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase));
      }
      finally
      {
        _gate.Release();
      }
    }

    public async Task<bool> TryCreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
      if(user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }

      await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

      try
      {
        var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false);

        if(file.Users.Any(existing =>
          string.Equals(existing.NormalizedUserName, user.NormalizedUserName, StringComparison.OrdinalIgnoreCase)))
        {
          return false;
        }

        file.Users.Add(user);
        await WriteFileAsync(file, cancellationToken).ConfigureAwait(false);

        return true;
      }
      finally
      {
        _gate.Release();
      }
    }

    public async Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
      if(user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }

      await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

      try
      {
        var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false);
        var index = file.Users.FindIndex(existing => string.Equals(existing.Id, user.Id, StringComparison.Ordinal));

        if(index < 0)
        {
          throw new InvalidOperationException("Cannot update an application user that does not exist in the user store.");
        }

        file.Users[index] = user;
        await WriteFileAsync(file, cancellationToken).ConfigureAwait(false);
      }
      finally
      {
        _gate.Release();
      }
    }

    async Task<ApplicationUserFile> ReadFileAsync(CancellationToken cancellationToken)
    {
      if(!File.Exists(_path))
      {
        return new ApplicationUserFile();
      }

      await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
      var file = await JsonSerializer.DeserializeAsync<ApplicationUserFile>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);

      return file == null
        ? new ApplicationUserFile()
        : new ApplicationUserFile { Users = file.Users ?? new List<ApplicationUser>() };
    }

    async Task WriteFileAsync(ApplicationUserFile file, CancellationToken cancellationToken)
    {
      var directory = Path.GetDirectoryName(_path);

      if(!string.IsNullOrWhiteSpace(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var tempPath = $"{_path}.{Guid.NewGuid():N}.tmp";

      await using(var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
      {
        await JsonSerializer.SerializeAsync(stream, file, JsonOptions, cancellationToken).ConfigureAwait(false);
      }

      File.Move(tempPath, _path, overwrite: true);
    }

    sealed class ApplicationUserFile
    {
      public List<ApplicationUser> Users { get; set; } = new();
    }
  }
}
