﻿using System;
using System.Collections.Generic;
using System.Linq;
using Totem.Runtime;

namespace Totem
{
	/// <summary>
	/// The state of a timeline query at a specific point
	/// </summary>
	[Durable]
	public abstract class View : Notion
	{
		protected View(ViewKey key)
		{
			Key = key;
			WhenCreated = Clock.Now;
			WhenModified = WhenCreated;
		}

		public ViewKey Key { get; private set; }
		public DateTime WhenCreated { get; private set; }
		public DateTime WhenModified { get; private set; }

		public override Text ToText()
		{
			return Key.ToString();
		}

		public void OnModified()
		{
			WhenModified = Clock.Now;
		}
	}
}