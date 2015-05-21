﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using Autofac;
using Autofac.Core;
using Totem.IO;

namespace Totem.Runtime
{
	/// <summary>
	/// A set of related objects in a Totem runtime
	/// </summary>
	public abstract class RuntimeArea : BuilderModule, IRuntimeArea, IPartImportsSatisfiedNotification
	{
		Tags ITaggable.Tags { get { return Tags; } }
		protected Tags Tags { get; private set; }
		protected IClock Clock { get { return Notion.Traits.Clock.Get(this); } }
		protected RuntimeMap Runtime { get { return Notion.Traits.Runtime.Get(this); } }

		public AreaType Type { get; private set; }

		public virtual bool TryResolveConnection(ILifetimeScope scope, out IConnectable connection)
		{
			connection = null;

			return false;
		}

		public sealed override string ToString()
		{
			return ToText();
		}

		public virtual Text ToText()
		{
			return base.ToString();
		}

		protected static IExpect<T> Expect<T>(T value)
		{
			return Totem.Expect.That(value);
		}

		//
		// Registration
		//

		protected sealed override void Configure(IComponentRegistry componentRegistry)
		{
			base.Configure(componentRegistry);
		}

		public sealed override void RegisterCallback(Action<IComponentRegistry> configurationCallback)
		{
			base.RegisterCallback(configurationCallback);
		}

		void IPartImportsSatisfiedNotification.OnImportsSatisfied()
		{
			Tags = new Tags();

			ReadType();

			ReadSection();

			RegisterArea();
		}

		private void ReadType()
		{
			Type = Runtime.GetArea(GetType());
		}

		protected virtual void ReadSection()
		{}

		protected abstract void RegisterArea();

		//
		// Path expansion
		//

		public FolderLink Expand(FolderResource folder)
		{
			return Runtime.Deployment.Expand(folder);
		}

		public FileLink Expand(FileResource file)
		{
			return Runtime.Deployment.Expand(file);
		}

		public FolderLink ExpandInData(FolderResource folder)
		{
			return Runtime.Deployment.ExpandInData(folder);
		}

		public FileLink ExpandInData(FileResource file)
		{
			return Runtime.Deployment.ExpandInData(file);
		}
	}

	/// <summary>
	/// A set of related objects and settings available for hosting by a runtime
	/// </summary>
	/// <typeparam name="TSection">The type of configuration section providing settings</typeparam>
	public abstract class RuntimeArea<TSection> : RuntimeArea where TSection : ConfigurationSection
	{
		protected TSection Section { get; private set; }

		protected override void ReadSection()
		{
			Section = ReadSection(this);
		}

		public static TSection ReadSection(RuntimeArea area, bool strict = true)
		{
			var name = area.Type.SectionName;
			var areaType = area.Type.DeclaredType;

			if(name == "")
			{
				Expect(strict).IsFalse(
					issue: Text
						.Of("Area does not define a configuration section")
						.WriteTwoLines()
						.Write("Add [ConfigurationSection(\"...\")] to " + Text.Of(areaType)),
					expected: "Area specifies a configuration section");

				return null;
			}

			var section = ConfigurationManager.GetSection(name);

			Expect(section).IsNotNull(
				issue: Text
					.Of("Area '{0}' is not configured. Add the following:", area)
					.WriteTwoLines()
					.Write(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile)
					.WriteTwoLines()
					.WriteLine("<configSections>")
					.WriteLine("    <section name=\"{0}\" type=\"{1}, {2}\" />", name, areaType.FullName, areaType.Assembly.FullName)
					.WriteLine("</configSections>"),
				expected: "Defined configuration section");

			var typedSection = section as TSection;

			Expect(typedSection).IsNotNull(
				issue: Text.Of("Area section is not the expected type"),
				expected: Text.OfType<TSection>(),
				actual: t => Text.OfType(section));

			return typedSection;
		}
	}
}