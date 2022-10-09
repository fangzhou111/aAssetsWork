using System.Linq;
namespace UniRx
{
	using UnityEngine;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	public static class UniRxExtensions
	{
		//public static IEnumerable<T> CopyAsSafeEnumerable<T>(this IEnumerable<T> source)
		//{
		//	var e = ((IEnumerable)source).GetEnumerator();
		//	using (e as IDisposable)
		//	{
		//		while (e.MoveNext())
		//		{
		//			yield return (T)e.Current;
		//		}
		//	}
		//}

		public static IEnumerable<T> CopyAsSafeEnumerable<T>(this ICollection<T> source)
		{
			T[] temp = new T[source != null ? source.Count : 0];
			if (source != null)
			{
				source.CopyTo(temp, 0);
			}
			var e = temp.GetEnumerator();
			using (e as IDisposable)
			{
				while (e.MoveNext())
				{
					yield return (T)e.Current;
				}
			}
		}

		#region Collection

		public static void AddSafe<T>(this List<T> source, ICollection<T> value)
		{
			if (value != null)
			{
				foreach (var item in value)
				{
					source.AddSafe(item);
				}
			}
		}

		public static void AddSafe<T>(this List<T> source, T value)
		{
			if (!source.Contains(value))
				source.Add(value);
		}

		public static void AddUnSafe<T>(this List<T> source, ICollection<T> value)
		{
			if (value != null)
			{
				foreach (var item in value)
				{
					source.AddUnSafe(item);
				}
			}
		}

		public static void AddUnSafe<T>(this List<T> source, T value)
		{
			source.Add(value);
		}

		public static void AddSafe<T>(this IReactiveCollection<T> source, T value)
		{
			if (!source.Contains(value))
				source.Add(value);
		}

		public static void AddSafe<T>(this IReactiveCollection<T> source, ICollection<T> value)
		{
			if (value != null)
			{
				foreach (var item in value)
				{
					source.AddSafe(item);
				}
			}
		}

		#endregion

		#region array

		public static T[] AddSafe<T>(this T[] source, ICollection<T> value)
		{
			source = source ?? new T[0];

			if (value != null)
			{
				foreach (var item in value)
				{
					source = source.AddSafe(item);
				}
			}

			return source;
		}

		public static T[] AddSafe<T>(this T[] source, T value)
		{
			source = source ?? new T[0];

			HashSet<T> list = new HashSet<T>(source);
			if (!list.Contains(value))
				list.Add(value);
			return list.ToArray();
		}

		public static T[] AddUnSafe<T>(this T[] source, ICollection<T> value)
		{
			source = source ?? new T[0];

			if (value != null)
			{
				foreach (var item in value)
				{
					source = source.AddUnSafe(item);
				}
			}

			return source;
		}

		public static T[] AddUnSafe<T>(this T[] source, T value)
		{
			source = source ?? new T[0];
			List<T> list = new List<T>(source);
			list.Add(value);
			return list.ToArray();
		}

		#endregion

		public static string ToArrayString<T>(this ICollection<T> source)
		{
			if (source == null) return string.Empty;

			string str = "";
			foreach (var item in source)
			{
				str += item.ToString() + "\n";
			}
			str += ("> count = " + source.Count);
			return str;
		}
	}
}

