using System.Collections.Generic;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class CreateMicrofilmClientProfile : Command
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }

    public CreateMicrofilmClientProfile(string name, string description, List<MicrofilmTableColumn> columns)
    {
      Name = name;
      Description = description;
      Columns = columns ?? new List<MicrofilmTableColumn>();
    }
  }

  public class ReplaceMicrofilmClientProfile : Command
  {
    public string ProfileId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }

    public ReplaceMicrofilmClientProfile(string profileId, string name, string description, List<MicrofilmTableColumn> columns)
    {
      ProfileId = profileId;
      Name = name;
      Description = description;
      Columns = columns ?? new List<MicrofilmTableColumn>();
    }
  }

  public class DeleteMicrofilmClientProfile : Command
  {
    public string ProfileId { get; set; }

    public DeleteMicrofilmClientProfile(string profileId)
    {
      ProfileId = profileId;
    }
  }
}
