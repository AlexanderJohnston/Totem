﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;

namespace Totem.Runtime.Timeline
{
  /// <summary>
  /// The scope of timeline activity in a runtime
  /// </summary>
  public sealed class TimelineScope : PushScope, ITimelineScope
	{
    private readonly ILifetimeScope _scope;
    private readonly ITimelineDb _timelineDb;
    private readonly IFlowDb _flowDb;
    private TimelineSchedule _schedule;
    private TimelineFlowSet _flows;
    private TimelineRequestSet _requests;

    public TimelineScope(ILifetimeScope scope, ITimelineDb timelineDb, IFlowDb flowDb)
		{
      _scope = scope;
      _timelineDb = timelineDb;
      _flowDb = flowDb;

      _schedule = new TimelineSchedule(this);
      _flows = new TimelineFlowSet(this);
      _requests = new TimelineRequestSet(this);
    }

    protected override void Open()
		{
      Track(_schedule);
      Track(_flows);
      Track(_requests);

      base.Open();

      ResumeTimeline();
    }

    private void ResumeTimeline()
    {
      var resumeInfo = _timelineDb.ReadResumeInfo();

      _schedule.ResumeWith(resumeInfo);

      resumeInfo.Push(this);
    }

    //
    // Runtime
    //

    protected override void Push()
    {
      _schedule.Push(Point);
      _flows.Push(Point);
      _requests.Push(Point);
    }

    internal void PushScheduled(TimelinePoint point)
    {
      Push(_timelineDb.WriteScheduled(point));
    }

    public Task<T> MakeRequest<T>(Id id) where T : Request
    {
      return _requests.MakeRequest<T>(id);
    }

    public IFlowScope OpenFlowScope(FlowKey key)
    {
      return new FlowScope(key, _scope, _flowDb);
    }
	}
}