using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Outermind.SmartScanning
{
  public static class FileOwnerResolver
  {
    public static string TryResolveDirectoryOwner(string directoryPath)
    {
      if(string.IsNullOrWhiteSpace(directoryPath))
      {
        return null;
      }

      try
      {
        var directory = new DirectoryInfo(directoryPath);
        if(!directory.Exists)
        {
          return null;
        }

        FileInfo latestFile = null;
        DateTime latestWrite = DateTime.MinValue;

        try
        {
          foreach(var file in directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
          {
            var lastWrite = file.LastWriteTimeUtc;
            if(latestFile == null || lastWrite > latestWrite)
            {
              latestFile = file;
              latestWrite = lastWrite;
            }
          }
        }
        catch
        {
          latestFile = null;
        }

        if(latestFile != null)
        {
          var owner = TryGetOwner(() => latestFile.GetAccessControl());
          if(!string.IsNullOrEmpty(owner))
          {
            return owner;
          }
        }

        return TryGetOwner(() => directory.GetAccessControl());
      }
      catch
      {
        return null;
      }
    }

    static string TryGetOwner(Func<FileSystemSecurity> accessor)
    {
      if(accessor == null)
      {
        return null;
      }

      try
      {
        var security = accessor();
        if(security == null)
        {
          return null;
        }

        IdentityReference owner;
        try
        {
          owner = security.GetOwner(typeof(SecurityIdentifier));
        }
        catch
        {
          owner = security.GetOwner(typeof(NTAccount));
        }

        if(owner == null)
        {
          return null;
        }

        if(owner is SecurityIdentifier sid)
        {
          try
          {
            var account = sid.Translate(typeof(NTAccount)) as NTAccount;
            return account?.Value ?? sid.Value;
          }
          catch
          {
            return sid.Value;
          }
        }

        if(owner is NTAccount accountOwner)
        {
          return accountOwner.Value;
        }

        return owner.Value;
      }
      catch
      {
        return null;
      }
    }
  }
}
