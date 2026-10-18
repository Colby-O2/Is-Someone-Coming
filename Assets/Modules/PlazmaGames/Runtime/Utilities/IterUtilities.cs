using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlazmaGames.Core.Utils
{
	public static class IterUtilities
	{
		public static void ForEach<T>(this IEnumerable<T> itbl, Action<T> func)
		{
			foreach (T t in itbl) func(t);
		}

		public static void ForEach<T>(this IEnumerable<T> itbl, Action<T, int> func)
		{
			int i = 0;
			foreach (T t in itbl) func(t, i++);
		}
		
		/// <summary>
		/// returns enumerable of each index where func returns true.
		/// </summary>
		public static IEnumerable<int> IndexWhere<T>(this IEnumerable<T> itbl, Func<T, bool> func)
		{
			int i = 0;
			foreach (T t in itbl)
			{
				if (func(t)) yield return i;
				i += 1;
			}
		}
		
		/// <summary>
		/// returns enumerable of each index where func returns true.
		/// </summary>
		public static IEnumerable<int> IndexWhere<T>(this IEnumerable<T> itbl, Func<T, int, bool> func)
		{
			int i = 0;
			foreach (T t in itbl)
			{
				if (func(t, i)) yield return i;
				i += 1;
			}
		}

		/// <summary>
		/// returns true if every element in baseSet is also in the enumerable
		/// along with having the same number of duplicates.
		/// </summary>
		public static bool ContainsSet<T>(this IEnumerable<T> itbl, IEnumerable<T> baseSet)
		{
			List<T> items = itbl.ToList();
			return baseSet.All(t => items.Remove(t));
		}

		public static void Loop2D(Vector2Int range, Func<Vector2Int, bool> func)
		{
			for (int y = 0; y < range.y; y++)
			{
				for (int x = 0; x < range.x; x++)
				{
					if (!func(new Vector2Int(x, y))) return;
				}
			}
		}

		public static void Loop3D(Vector3Int range, Func<Vector3Int, bool> func)
		{
			for (int z = 0; z < range.z; z++)
			{
				for (int y = 0; y < range.y; y++)
				{
					for (int x = 0; x < range.x; x++)
					{
						if (!func(new Vector3Int(x, y, z))) return;
					}
				}
			}
		}

		public static void Loop3D(Vector3Int range, Vector3Int stride, Func<Vector3Int, bool> func)
		{
			Vector3Int vec = new Vector3Int();
			for (int z = 0; z < range.z; z += stride.z)
			{
				for (int y = 0; y < range.y; y += stride.y)
				{
					for (int x = 0; x < range.x; x += stride.x)
					{
						vec.Set(x, y, z);
						if (!func(vec)) return;
					}
				}
			}
		}
	}
}
