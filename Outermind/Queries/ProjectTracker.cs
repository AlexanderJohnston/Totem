using System;
using System.Collections.Generic;
using Outermind;
using Totem;
using Totem.Timeline;

namespace Quantum.Queries
{
  public class ProjectTracker : Query
  {
    static Id RouteFirst(ProjectClassified e) => e.Project;

    public Dictionary<string, DateTime> KnownFolders { get; set; } = new();

    void Given(ProjectClassified e)
    {
      KnownFolders[e.FolderPath] = e.Scan.CompletedAtUtc;
    }

  }
}