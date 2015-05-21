﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Totem.IO;

namespace Totem.Runtime
{
	/// <summary>
	/// A set of related aseemblies in a region of a Totem runtime
	/// </summary>
	public sealed class RuntimePackage : Notion
	{
		public RuntimePackage(FolderLink folder, RuntimeRegionKey regionKey, AssemblyCatalog catalog)
		{
			Folder = folder;
			RegionKey = regionKey;
			Catalog = catalog;
			Areas = new AreaTypeSet();
		}

		public FolderLink Folder { get; private set; }
		public RuntimeRegionKey RegionKey { get; private set; }
		public AssemblyCatalog Catalog { get; private set; }
		public Assembly Assembly { get { return Catalog.Assembly; } }
		public AreaTypeSet Areas { get; private set; }

		public override Text ToText()
		{
			return Assembly.GetName().Name;
		}

		public AreaType GetArea(RuntimeTypeKey key, bool strict = true)
		{
			return Areas.Get(key, strict);
		}

		public AreaType GetArea(Type declaredType, bool strict = true)
		{
			return Areas.Get(declaredType, strict);
		}
	}
}