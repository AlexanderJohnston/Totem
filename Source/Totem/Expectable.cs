using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Totem
{
	/// <summary>
	/// Extends <see cref="Expect{T}"/> with expectations
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class Expectable
	{
		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> IsTrue<T>(
			this Expect<T> expect,
			Func<T, bool> check,
			Text issue = null,
			Text expected = null,
			Func<T, Text> received = null)
		{
			return expect.IsTrue(check, WriteMessage(check, issue, expected, received));
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> IsFalse<T>(
			this Expect<T> expect,
			Func<T, bool> check,
			Text issue = null,
			Text expected = null,
			Func<T, Text> received = null)
		{
			return expect.IsFalse(check, WriteMessage(check, issue, expected, received));
		}

		private static Func<T, string> WriteMessage<T>(
			Func<T, bool> check,
			Text issue,
			Text expected,
			Func<T, Text> received)
		{
			if(issue == null)
			{
				issue = "Unexpected value";
			}

			if(received == null)
			{
				received = t => Text.Of(t);
			}

			return t =>
			{
				var message = issue.WriteTwoLines();

				if(expected != null)
				{
					message = message
						.WriteLine("Expected:")
						.WriteLine(expected.Indent())
						.WriteLine()
						.WriteLine("Received:");
				}
				else
				{
					message = message
						.WriteLine("Check:")
						.WriteLine(Text.Of("{0}.{1}", check.Method.DeclaringType, check.Method).Indent())
						.WriteLine()
						.WriteLine("Value:");
				}

				return message.WriteLine(received(t).Indent());
			};
		}

		//
		// Boolean
		//

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<bool> IsTrue(this Expect<bool> expect, Text issue = null)
		{
			return expect.IsTrue(t => t, issue, Text.Of(true));
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<bool> IsFalse(this Expect<bool> expect, Text issue = null)
		{
			return expect.IsFalse(t => t, issue, Text.Of(false));
		}

		//
		// Null
		//

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> IsNull<T>(this Expect<T> expect, Text issue = null) where T : class
		{
			return expect.IsTrue(t => t == null, issue, "null");
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> IsNotNull<T>(this Expect<T> expect, Text issue = null) where T : class
		{
			return expect.IsTrue(t => t != null, issue, "not null", t => "null");
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T?> IsNull<T>(this Expect<T?> expect, Text issue = null) where T : struct
		{
			return expect.IsTrue(t => t == null, issue, "null");
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T?> IsNotNull<T>(this Expect<T?> expect, Text issue = null) where T : struct
		{
			return expect.IsTrue(t => t != null, issue, "not null", t => "null");
		}

		//
		// Equality
		//

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> Is<T>(this Expect<T> expect, T other, IEqualityComparer<T> comparer, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).Is(other, comparer), issue ?? "Values are not equal", Text.Of("{0} (comparer = {1})", other, comparer));
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> Is<T>(this Expect<T> expect, T other, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).Is(other), issue ?? "Values are not equal", Text.Of(other));
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> IsNot<T>(this Expect<T> expect, T other, IEqualityComparer<T> comparer, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).IsNot(other, comparer), issue ?? "Values are equal", Text.Of("not {0} (comparer = {1})", other, comparer));
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> IsNot<T>(this Expect<T> expect, T other, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).IsNot(other), issue ?? "Values are equal", "not " + Text.Of(other));
		}

		//
		// String
		//

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<string> IsEmpty(this Expect<string> expect, Text issue = null)
		{
			return expect.IsTrue(t => t == "", issue, "empty");
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<string> IsNotEmpty(this Expect<string> expect, Text issue = null)
		{
			return expect.IsTrue(t => t != "", issue, "not empty", t => "empty");
		}

		//
		// Types
		//

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<T> IsAssignableTo<T>(this Expect<T> expect, Type type, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).IsAssignableTo(type), issue ?? "Value is not assignable", "assignable to " + Text.Of(type));
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<Type> IsAssignableTo(this Expect<Type> expect, Type type, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).IsAssignableTo(type), issue ?? "Value is not assignable", "assignable to " + Text.Of(type));
		}

		//
		// Sequences
		//

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<Many<T>> Has<T>(this Expect<Many<T>> expect, int count, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).Has(count), issue ?? "Value has the wrong number of items", Text.Count(count, "item"), t => Text.Count(t.Count, "item"));
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<Many<T>> Has0<T>(this Expect<Many<T>> expect, Text issue = null)
		{
			return expect.Has(0, issue);
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<Many<T>> Has1<T>(this Expect<Many<T>> expect, Action<T> expectItem = null, Text issue = null)
		{
			expect = expect.Has(1, issue);

			if(expectItem != null)
			{
				expectItem(expect.Target[0]);
			}

			return expect;
		}

		//
		// Tags
		//

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<Tag<T>> IsUnset<T>(this Expect<Tag<T>> expect, ITaggable target, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).IsUnset(target), issue, "tag not set", t => $"tag set: {t}");
		}

		[DebuggerHidden, DebuggerStepThrough, DebuggerNonUserCode]
		public static Expect<Tag<T>> IsSet<T>(this Expect<Tag<T>> expect, ITaggable target, Text issue = null)
		{
			return expect.IsTrue(t => Check.True(t).IsSet(target), issue, "tag set", t => $"tag set: {t}");
		}
	}
}