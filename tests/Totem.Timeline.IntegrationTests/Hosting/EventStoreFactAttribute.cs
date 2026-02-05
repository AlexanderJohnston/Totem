using System;
using Xunit;

namespace Totem.Timeline.IntegrationTests.Hosting
{
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  internal sealed class EventStoreFactAttribute : FactAttribute
  {
    const string SkipMessage = "Integration tests require eventStoreProcess:exeFile to be set to an existing EventStoreDB executable.";

    public EventStoreFactAttribute()
    {
      if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EventStoreExeFile")))
      {
        Skip = SkipMessage;
      }
    }
  }
}
