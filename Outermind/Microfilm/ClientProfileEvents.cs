using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class MicrofilmClientProfileCreated : Event
  {
    public MicrofilmClientProfile Profile { get; set; }

    public MicrofilmClientProfileCreated(MicrofilmClientProfile profile)
    {
      Profile = profile;
    }
  }

  public class MicrofilmClientProfileReplaced : Event
  {
    public MicrofilmClientProfile Profile { get; set; }

    public MicrofilmClientProfileReplaced(MicrofilmClientProfile profile)
    {
      Profile = profile;
    }
  }

  public class MicrofilmClientProfileDeleted : Event
  {
    public string ProfileId { get; set; }

    public MicrofilmClientProfileDeleted(string profileId)
    {
      ProfileId = profileId;
    }
  }

  public class MicrofilmClientProfileNotRecognized : Event
  {
    public string ProfileId { get; set; }

    public MicrofilmClientProfileNotRecognized(string profileId)
    {
      ProfileId = profileId;
    }
  }

  public class MicrofilmClientProfileNameRejected : Event
  {
    public string Code { get; set; }
    public string Message { get; set; }

    public MicrofilmClientProfileNameRejected(string code, string message)
    {
      Code = code;
      Message = message;
    }
  }

  public class MicrofilmClientProfileNameDuplicated : Event
  {
    public string Name { get; set; }

    public MicrofilmClientProfileNameDuplicated(string name)
    {
      Name = name;
    }
  }

  public class MicrofilmClientProfileColumnsRejected : Event
  {
    public string Code { get; set; }
    public string Message { get; set; }
    public string ColumnId { get; set; }

    public MicrofilmClientProfileColumnsRejected(string code, string message, string columnId = null)
    {
      Code = code;
      Message = message;
      ColumnId = columnId;
    }
  }
}
