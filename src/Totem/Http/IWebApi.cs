﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Totem.Http
{
	/// <summary>
	/// Describes an instance of a Totem web API bound to an HTTP request
	/// </summary>
	public interface IWebApi : IWritable, ITaggable
	{
		WebApiCall Call { get; }
	}
}