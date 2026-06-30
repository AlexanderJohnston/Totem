using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages the global catalog of reusable microfilm client table profiles.
  /// </summary>
  public class MicrofilmClientProfileTopic : Topic
  {
    readonly Dictionary<string, MicrofilmClientProfile> _profilesById = new();
    readonly Dictionary<string, string> _profileIdsByName = new(StringComparer.OrdinalIgnoreCase);

    void Given(MicrofilmClientProfileCreated e)
    {
      Store(e.Profile);
    }

    void Given(MicrofilmClientProfileReplaced e)
    {
      if(e.Profile != null && _profilesById.TryGetValue(e.Profile.Id, out var existing))
      {
        _profileIdsByName.Remove(existing.Name);
      }

      Store(e.Profile);
    }

    void Given(MicrofilmClientProfileDeleted e)
    {
      if(e.ProfileId != null && _profilesById.TryGetValue(e.ProfileId, out var existing))
      {
        _profileIdsByName.Remove(existing.Name);
        _profilesById.Remove(e.ProfileId);
      }
    }

    void When(CreateMicrofilmClientProfile command)
    {
      if(!TryNormalizeName(command.Name, out var name))
      {
        return;
      }

      if(_profileIdsByName.ContainsKey(name))
      {
        Then(new MicrofilmClientProfileNameDuplicated(name));
        return;
      }

      if(!TryNormalizeColumns(command.Columns, out var columns))
      {
        return;
      }

      var now = Clock.Now;
      Then(new MicrofilmClientProfileCreated(new MicrofilmClientProfile(
        Id.FromGuid().ToString(),
        name,
        NormalizeDescription(command.Description),
        columns,
        now,
        now)));
    }

    void When(ReplaceMicrofilmClientProfile command)
    {
      var profileId = NormalizeProfileId(command.ProfileId);
      if(!_profilesById.TryGetValue(profileId, out var existing))
      {
        Then(new MicrofilmClientProfileNotRecognized(profileId));
        return;
      }

      if(!TryNormalizeName(command.Name, out var name))
      {
        return;
      }

      if(_profileIdsByName.TryGetValue(name, out var nameOwnerId) && nameOwnerId != profileId)
      {
        Then(new MicrofilmClientProfileNameDuplicated(name));
        return;
      }

      if(!TryNormalizeColumns(command.Columns, out var columns))
      {
        return;
      }

      Then(new MicrofilmClientProfileReplaced(new MicrofilmClientProfile(
        profileId,
        name,
        NormalizeDescription(command.Description),
        columns,
        existing.CreatedAt,
        Clock.Now)));
    }

    void When(DeleteMicrofilmClientProfile command)
    {
      var profileId = NormalizeProfileId(command.ProfileId);
      if(!_profilesById.ContainsKey(profileId))
      {
        Then(new MicrofilmClientProfileNotRecognized(profileId));
        return;
      }

      Then(new MicrofilmClientProfileDeleted(profileId));
    }

    void Store(MicrofilmClientProfile profile)
    {
      if(profile == null || string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
      {
        return;
      }

      var copy = profile.Clone();
      _profilesById[copy.Id] = copy;
      _profileIdsByName[copy.Name] = copy.Id;
    }

    bool TryNormalizeName(string input, out string name)
    {
      name = null;

      if(string.IsNullOrWhiteSpace(input))
      {
        Then(new MicrofilmClientProfileNameRejected("INVALID_PROFILE_NAME", "Profile name is required."));
        return false;
      }

      name = input.Trim();
      return true;
    }

    bool TryNormalizeColumns(List<MicrofilmTableColumn> input, out List<MicrofilmTableColumn> columns)
    {
      if(MicrofilmTableRules.TryNormalizeColumns(input, out columns, out var code, out var message, out var columnId))
      {
        return true;
      }

      Then(new MicrofilmClientProfileColumnsRejected(code, message, columnId));
      return false;
    }

    static string NormalizeProfileId(string profileId) =>
      (profileId ?? "").Trim();

    static string NormalizeDescription(string description)
    {
      var normalized = description?.Trim();
      return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
  }
}
