using System;
using System.Collections.Generic;
using System.Text;
using Quantum.SmartScanning;

namespace Quantum.ServiceContracts
{
  public interface IUsersLogParser
  {
    List<UserEvent> ParseFile(string filePath);
    List<UserEvent> ParseLines(IEnumerable<string> lines);
  }
}
