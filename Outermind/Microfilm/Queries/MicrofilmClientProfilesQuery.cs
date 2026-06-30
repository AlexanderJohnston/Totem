using System;
using System.Collections.Generic;
using System.Linq;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks the global catalog of reusable microfilm client table profiles.
  /// </summary>
  public class MicrofilmClientProfilesQuery : Query
  {
    public List<MicrofilmClientProfile> Profiles { get; set; } = new();

    void Given(MicrofilmClientProfileCreated e)
    {
      Upsert(e.Profile);
    }

    void Given(MicrofilmClientProfileReplaced e)
    {
      Upsert(e.Profile);
    }

    void Given(MicrofilmClientProfileDeleted e)
    {
      Profiles.RemoveAll(profile => profile.Id == e.ProfileId);
    }

    void Upsert(MicrofilmClientProfile profile)
    {
      Profiles.RemoveAll(existing => existing.Id == profile.Id);
      Profiles.Add(profile.Clone());
      SortProfiles();
    }

    void SortProfiles()
    {
      Profiles = Profiles
        .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }
  }
}
