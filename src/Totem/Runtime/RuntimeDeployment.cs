﻿using System;
using System.Collections.Generic;
using System.Linq;
using Totem.IO;

namespace Totem.Runtime
{
	/// <summary>
	/// A context-bound instance of the Totem runtime
	/// </summary>
	public sealed class RuntimeDeployment
	{
		public RuntimeDeployment(IFolder folder, IFolder hostFolder, IFolder dataFolder, IFolder logFolder, string solutionConfiguration = "")
		{
			Folder = folder;
			HostFolder = hostFolder;
			DataFolder = dataFolder;
			LogFolder = logFolder;
			SolutionConfiguration = solutionConfiguration;
			InSolution = solutionConfiguration != "";
		}

		public readonly IFolder Folder;
		public readonly IFolder HostFolder;
		public readonly IFolder DataFolder;
		public readonly IFolder LogFolder;
		public readonly string SolutionConfiguration;
		public readonly bool InSolution;

		public override string ToString()
		{
			return Folder.ToString();
		}

		public FolderLink Expand(FolderResource folder)
		{
			return Folder.Link.Then(folder);
		}

		public FileLink Expand(FileResource file)
		{
			return Folder.Link.Then(file);
		}

		public FolderLink ExpandInData(FolderResource folder)
		{
			return DataFolder.Link.Then(folder);
		}

		public FileLink ExpandInData(FileResource file)
		{
			return DataFolder.Link.Then(file);
		}
	}
}