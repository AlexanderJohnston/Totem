﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Totem.Runtime.Json;
using Totem.Runtime.Map.Timeline;

namespace Totem.Runtime.Timeline
{
	/// <summary>
	/// A database persisting timeline data in the local runtime
	/// </summary>
	public sealed class LocalTimelineDb : Notion, ITimelineDb, IViewDb
	{
		private readonly ConcurrentDictionary<FlowKey, Flow> _flowsByKey = new ConcurrentDictionary<FlowKey, Flow>();
		private long _position = -1;

		public ResumeInfo ReadResumeInfo()
		{
			return new ResumeInfo(new TimelinePosition(0));
		}

		public Flow ReadFlow(TimelineRoute route)
		{
			var key = route.Key;
			var type = key.Type;

			Flow flow;

			if(_flowsByKey.TryGetValue(key, out flow))
			{
				return flow;
			}

			if(type.IsRouted && !route.IsFirst)
			{
				throw new ArgumentException($"Unknown flow {key}", nameof(route));
			}

			flow = type.New();

			Flow.Initialize(flow, key);

			_flowsByKey[key] = flow;

			return flow;
		}

		public Many<TimelineMessage> Push(Many<Event> events)
		{
			return events.ToMany(e => PushLocal(TimelinePosition.None, e));
		}

		public Many<TimelineMessage> PushCall(WhenCall call)
		{
			if(call.Flow.Done)
			{
				Flow removedFlow;

				_flowsByKey.TryRemove(call.Flow.Key, out removedFlow);
			}

			var topicCall = call as TopicWhenCall;

			if(topicCall == null)
			{
				return new Many<TimelineMessage>();
			}

			return topicCall
				.RetrieveNewEvents()
				.ToMany(e => PushLocal(topicCall.Point.Position, e));
		}

		public TimelineMessage PushFromSchedule(TimelineMessage message)
		{
			return PushLocal(message.Point.Position, message.Point.Event);
		}

		public TimelineMessage PushFlowStopped(FlowKey key, TimelinePoint point, Exception error)
		{
			var stopped = new FlowStopped(key.Type.Key, key.Id, error.ToString());

			Flow.Traits.ForwardRequestId(point.Event, stopped);

			return PushLocal(point.Position, stopped);
		}

		//
		// Push local
		//

		private TimelineMessage PushLocal(TimelinePosition cause, Event e)
		{
			var type = Runtime.GetEvent(e.GetType());

			var scheduled = e as EventScheduled;

			if(scheduled != null)
			{
				type = Runtime.GetEvent(scheduled.Event.GetType());

				e = scheduled.Event;
			}

			return PushLocal(cause, type, e, scheduled != null);
		}

		private TimelineMessage PushLocal(TimelinePosition cause, EventType type, Event e, bool scheduled)
		{
			var newPoint = new TimelinePoint(cause, NextPosition(), type, e, scheduled);

			if(scheduled)
			{
				Log.Info("[timeline] {Cause:l} ++ {Position:l} >> {EventType:l} @ {When}", cause, newPoint.Position, type, e.When);
			}
			else
			{
				Log.Info("[timeline] {Cause:l} ++ {Point:l}", cause, newPoint);
			}

			return new TimelineMessage(newPoint, type.CallRoute(e).ToMany());
		}

		private TimelinePosition NextPosition()
		{
			return new TimelinePosition(Interlocked.Increment(ref _position));
		}

		//
		// Views
		//

		public ViewSnapshot<View> ReadSnapshot(Type type, Id id, TimelinePosition checkpoint)
		{
			return ReadView(type, id, checkpoint, view => (View) view);
		}

		public ViewSnapshot<T> ReadSnapshot<T>(Id id, TimelinePosition checkpoint) where T : View
		{
			return ReadView(typeof(T), id, checkpoint, view => (T) view);
		}

		public ViewSnapshot<string> ReadJsonSnapshot(Type type, Id id, TimelinePosition checkpoint)
		{
			return ReadView(type, id, checkpoint, view => JsonFormat.Text.Serialize(view).ToString());
		}

		private ViewSnapshot<T> ReadView<T>(Type type, Id id, TimelinePosition checkpoint, Func<Flow, T> selectContent)
		{
			var key = Runtime.GetView(type).CreateKey(id);

			Flow view;

			if(!_flowsByKey.TryGetValue(key, out view))
			{
				return ViewSnapshot<T>.OfNotFound(key);
			}

			ExpectNot(view.Done, "View is done and marked for removal: " + key.ToText());

			return view.Checkpoint == checkpoint
				? ViewSnapshot<T>.OfNotModified(key, checkpoint)
				: ViewSnapshot<T>.OfContent(key, view.Checkpoint, selectContent(view));
		}
	}
}