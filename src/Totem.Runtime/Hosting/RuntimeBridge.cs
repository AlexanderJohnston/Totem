﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Topshelf;
using Totem.Runtime.Map;

namespace Totem.Runtime.Hosting
{
	/// <summary>
	/// The point of contact between the host and runtime app domains
	/// </summary>
	internal sealed class RuntimeBridge<THost> : MarshalByRefObject, ITaggable, IRuntimeBridge where THost : IRuntimeHost, new()
	{
		public RuntimeBridge()
		{
			Tags = new Tags();

			RuntimeHost.Bridge = this;
		}

		Tags ITaggable.Tags { get { return Tags; } }
		private Tags Tags { get; set; }
		private IClock Clock { get { return Notion.Traits.Clock.Get(this); } }
		private ILog Log { get { return Notion.Traits.Log.Get(this); } }
		private RuntimeMap Runtime { get { return Notion.Traits.Runtime.Get(this); } }

		//
		// Run
		//

		internal TopshelfExitCode Run(TextWriter consoleOut)
		{
			Console.SetOut(consoleOut);

			return HostFactory.Run(new THost().Configure);
		}

		//
		// Restarts
		//

		public event EventHandler Restarting;

		internal bool RestartRequested { get; private set; }

		public void RequestRestart(string reason)
		{
			Log.Info("[runtime] Restarting runtime in 5 seconds: {Reason}", reason);

			RestartRequested = true;

			RestartAfterDelay();
		}

		private void RestartAfterDelay()
		{
			// This is the simple way to ensure all flows are done. The advanced way is to track whether work is
			// in progress and wait/block until it is all done.

			IDisposable subscription = null;

			subscription = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
			{
				try
				{
					if(Restarting != null)
					{
						Restarting(this, EventArgs.Empty);
					}
				}
				finally
				{
					subscription.Dispose();
				}
			});
		}

		public override object InitializeLifetimeService()
		{
			// Null tells .NET Remoting that this object has an infinite lease. See Modifying Lease Properties:
			//
			// http://msdn.microsoft.com/en-us/library/23bk23zc%28v=vs.71%29.aspx

			return null;
		}
	}
}