//using System;
//using System.Collections.Generic;
//using System.Text;
//using Quantum.Imaging;
//using Totem;
//using Totem.Timeline;

//namespace Outermind.Topics
//{
//  /// <summary>
//  ///  Implement all commands and events from _Events.cs.
//  /// </summary>
//  public class ScanManager : Topic
//  {
//    // store all known scans with their status
//    Dictionary<Id, Scan> _scans = new();

//    void Given(ScanStarted e)
//    {
//      _scans[e.Roll].Status = ScanStatus.InProgress;
//    }

//    void Given(ScanFinished e)
//    {
//      _scans[e.Roll].Status = ScanStatus.Finished;
//    }

//    void When(StartScan c)
//    {
//      if(_scans.ContainsKey(c.Roll) && _scans[c.Roll].Status == ScanStatus.InProgress)
//      {
//        Then(new ScanAlreadyInProgress(c.Roll));
//      }
//      else
//      {
//        Then(new ScanStarted(c.Roll, c.Operator, c.StartTime));
//      }
//    }

//    void When(FinishScan c)
//    {
//      if(_scans.ContainsKey(c.Roll) && _scans[c.Roll].Status == ScanStatus.InProgress)
//      {
//        Then(new ScanFinished(c.Roll, c.EndTime));
//      }
//      else
//      {
//        Then(new ScanNotFound(c.Roll, "No scan in progress to finish."));
//      }
//    }

//    void When(DeleteScan c)
//    {
//      if(_scans.ContainsKey(c.Roll))
//      {
//        _scans.Remove(c.Roll);
//        Then(new ScanDeleted(c.Roll));
//      }
//      else
//      {
//        Then(new NothingToDelete(c.Roll, "No scan found to delete."));
//      }
//    }

//    void When(MoveScan e)
//    {
//      // Don't actually move it, just fake it after checking if it exists.
//      if(!_scans.ContainsKey(e.Roll))
//      {
//        Then(new CannotMoveScan(e.Roll, "No scan found to move."));
//      }
//      else
//      {
//        Then(new ScanMoved(e.Roll, e.Operator, e.Source, e.Destination));
//      }
//    }

//    void When(OperatorComment e)
//    {
//      // look up and add comments to the roll
//      if(_scans.ContainsKey(e.Roll))
//      {
//        _scans[e.Roll].Comments.Add(new ScanComment(e.Operator, DateTime.Now, e.Notes));
//        Then(new CommentAdded(e.Roll, e.Operator, e.Notes));
//      }
//      else
//      {
//        Then(new CommentRefused(e.Roll, e.Operator, e.Notes, "No scan found to add comment to."));
//      }
//    }

//    void When(UpdateSettings e)
//    {
//      // look up and update settings for the roll
//      if(_scans.ContainsKey(e.Roll))
//      {
//        Then(new SettingsUpdated(e.Roll, e.Settings));
//      }
//      else
//      {
//        Then(new SettingsRefused(e.Roll, e.Settings, "No scan found to update settings for."));
//      }
//    }
//  }
//}
