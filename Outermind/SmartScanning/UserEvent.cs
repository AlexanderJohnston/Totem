using System;
using System.Collections.Generic;
using System.Text;

namespace Quantum.SmartScanning
{
  public class UserEvent
  {
    public string User;
    public string Action;
    public DateTime Timestamp;
    public string Source;
    public string Machine;
    public string Path;
    public List<string> Details = new List<string>();
  }
}
