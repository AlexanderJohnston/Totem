﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Totem.IO;
using Totem.Runtime.Configuration.Deployment;
using Totem.Runtime.Configuration.Service;

namespace Totem.Runtime.Configuration
{
	/// <summary>
	/// Configures the current Totem runtime
	/// </summary>
	public class RuntimeSection : ConfigurationSection
	{
		[ConfigurationProperty("mode", DefaultValue = RuntimeMode.Console)]
		public RuntimeMode Mode
		{
			get { return (RuntimeMode) this["mode"]; }
			set { this["mode"] = value; }
		}

		[ConfigurationProperty("dataFolder", IsRequired = true)]
		public string DataFolder
		{
			get { return (string) this["dataFolder"]; }
			set { this["dataFolder"] = value; }
		}

		[ConfigurationProperty("log", IsRequired = true)]
		public LogElement Log
		{
			get { return (LogElement) this["log"]; }
			set { this["log"] = value; }
		}

		[ConfigurationProperty("service", IsRequired = true)]
		public ServiceElement Service
		{
			get { return (ServiceElement) this["service"]; }
			set { this["service"] = value; }
		}

		[ConfigurationProperty("deployment", IsRequired = true)]
		public DeploymentElement Deployment
		{
			get { return (DeploymentElement) this["deployment"]; }
			set { this["deployment"] = value; }
		}

		//
		// Map
		//

		public RuntimeDeployment ReadDeployment()
		{
			var hostFolder = GetHostFolder();

			var inSolution = InSolution(hostFolder);

			var deploymentFolder = GetDeploymentFolder(hostFolder, inSolution);

			var dataFolder = GetDataFolder(deploymentFolder);

			var log = GetLog(dataFolder);

			return new RuntimeDeployment(Mode, inSolution, deploymentFolder, dataFolder, log, Deployment.Packages.GetNames());
		}

		private static IFolder GetHostFolder()
		{
			return new LocalFolder(FolderLink.From(Directory.GetCurrentDirectory()));
		}

		private static bool InSolution(IFolder hostFolder)
		{
			var segments = hostFolder.Link.Resource.Path.Segments;

			return segments.Count > 2
				&& segments[segments.Count - 1].ToString().Equals(RuntimeDeployment.BuildType, StringComparison.OrdinalIgnoreCase)
				&& segments[segments.Count - 2].ToString().Equals("bin", StringComparison.OrdinalIgnoreCase);
		}

		private static IFolder GetDeploymentFolder(IFolder hostFolder, bool inSolution)
		{
			// Omit bin/%BuildType% if not deployed

			return new LocalFolder(hostFolder.Link.Up(inSolution ? 3 : 1));
		}

		private IFolder GetDataFolder(IFolder deploymentFolder)
		{
			var dataFolder = Path.IsPathRooted(DataFolder)
				? DataFolder
				: Path.GetFullPath(Path.Combine(deploymentFolder.Link.ToString(), DataFolder));

			return new LocalFolder(FolderLink.From(dataFolder));
		}

		private RuntimeDeploymentLog GetLog(IFolder dataFolder)
		{
			var dataFolderLink = dataFolder.Link.Then(FolderResource.From(Log.DataFolder));

			return new RuntimeDeploymentLog(Log.Level, new LocalFolder(dataFolderLink), Log.ServerUrl);
		}

		//
		// Factory
		//

		public static RuntimeSection Read()
		{
			var instance = (RuntimeSection) ConfigurationManager.GetSection("totem.runtime");

			Expect.That(instance).IsNotNull("Runtime is not configured - specify the 'totem.runtime' section in the configuration file");

			return instance;
		}
	}
}