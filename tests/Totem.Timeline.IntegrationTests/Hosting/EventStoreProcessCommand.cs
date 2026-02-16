using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Totem.Threading;

namespace Totem.Timeline.IntegrationTests.Hosting
{
  /// <summary>
	/// The command line used to start an external EventStore process
	/// </summary>
	internal sealed class EventStoreProcessCommand
  {
    static readonly TaskSource _killExistingProcesses = new TaskSource();
    static int _firstCommandFlag;

    readonly string _exeFile;
    readonly int _port;
    
    internal EventStoreProcessCommand(string exeFile, int port)
    {
      _exeFile = exeFile;
      _port = port;
    }

    internal async Task<Process> StartProcess()
    {
      if(IsFirstCommand())
      {
        KillExistingProcesses();
      }

      await _killExistingProcesses.Task;
      
      return Process.Start(CreateStartInfo());
    }

    bool IsFirstCommand() =>
      Interlocked.CompareExchange(ref _firstCommandFlag, 1, 0) == 0;

    void KillExistingProcesses()
    {
      foreach(var existingProcess in GetExistingProcesses())
      {
        try
        {
          existingProcess.Kill();
          existingProcess.WaitForExit();
        }
        catch(Exception error)
        {
          Debug.WriteLine("Failed to kill existing EventStore process", error);
        }
      }

      _killExistingProcesses.SetResult();
    }

    IEnumerable<Process> GetExistingProcesses() =>
      Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_exeFile));

    ProcessStartInfo CreateStartInfo() =>
      new ProcessStartInfo
      {
        FileName = _exeFile,
        UseShellExecute = false,
        CreateNoWindow = true,
        Arguments = Text.None
          .Write("--mem-db")
          .Write(" --stats-period-sec=60")
          .Write(" --run-projections=all")
          .Write(" --insecure")
          .Write(" --http-port=").Write(_port),
      };
  }
}
