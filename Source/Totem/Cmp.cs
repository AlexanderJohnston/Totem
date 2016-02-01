using System;
using System.Collections.Generic;
using System.Linq;

namespace Totem
{
	/// <summary>
	/// Standard implementations of equality and comparison operations
	/// </summary>
	public static class Cmp
	{
		public static Comparable<T> Values<T>(T x, T y)
		{
			return new Comparable<T>(x, y);
		}

		public static int Op<T>(T x, T y)
		{
			return Comparer<T>.Default.Compare(x, y);
		}

		/// <summary>
		/// A value compared with another value of the same type. Supports drilldown.
		/// </summary>
		/// <typeparam name="T">The type of comparable value</typeparam>
		public sealed class Comparable<T>
		{
			private readonly T _x;
			private readonly T _y;
			private bool _checked;
			private int _result;

			internal Comparable(T x, T y)
			{
				_x = x;
				_y = y;
			}

			public Comparable<T> Check<TValue>(Func<T, TValue> get)
			{
				if(_result == 0)
				{
					_result = Comparer<TValue>.Default.Compare(get(_x), get(_y));
				}

				_checked = true;

				return this;
			}

			public static implicit operator int(Comparable<T> comparable)
			{
				if(!comparable._checked)
				{
					throw new InvalidOperationException("Check at least one value for comparison");
				}

				return comparable._result;
			}
		}
	}
}