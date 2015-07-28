﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Totem.Runtime.Map.Timeline
{
	/// <summary>
	/// A .NET type representing a request on the timeline
	/// </summary>
	public sealed class RequestFlowType : FlowType
	{
		internal RequestFlowType(RuntimeTypeRef type, FlowConstructor constructor) : base(type, constructor)
		{}
	}
}