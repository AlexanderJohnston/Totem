﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog.Extras.Topshelf;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Totem.Runtime
{
	/// <summary>
	/// Hosts an instance of the Totem runtime
	/// </summary>
	public static class RuntimeHost
	{
		public static void UseRuntime(this HostConfigurator host, Assembly assembly)
		{
			var service = new RuntimeService(assembly);

			service.Initialize();

			host.UseSerilog(service.Log.Logger);

			host.Service(() => service);
		}
	}
}