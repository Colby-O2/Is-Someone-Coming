using System;
using System.Collections;
using System.Collections.Generic;

namespace PlazmaGames.Core.Utils
{
	public static class ArrayUtilities
	{
		/// <summary>
		/// Replaces elements in the array with another.
		/// </summary>
		public static void Replace<T>(this T[,] arr, T val, T newVal)
		{
			for (int i = 0; i < arr.GetLength(0); i++)
			{
				for (int j = 0; j < arr.GetLength(1); j++)
				{
					if (arr[i, j].Equals(val)) arr[i, j] = newVal;
				}
			}
		}

		/// <summary>
		/// Count the number of elements equal to val in the 2D array.
		/// </summary>
		public static int Count<T>(this T[,] arr, T val)
		{
			int count = 0;

			for (int i = 0; i < arr.GetLength(0); i++)
			{
				for (int j = 0; j < arr.GetLength(1); j++)
				{
					if (arr[i, j].Equals(val)) count++;
				}
			}

			return count;
		}

		/// <summary>
		/// Count the number of elements equal to val in the 2D array.
		/// </summary>
		public static int Count<T>(this T[,] arr, T val, int offset)
		{
			int count = 0;

			for (int i = offset; i < arr.GetLength(0) - offset; i++)
			{
				for (int j = offset; j < arr.GetLength(1) - offset; j++)
				{
					if (arr[i, j].Equals(val)) count++;
				}
			}

			return count;
		}

		/// <summary>
		/// 
		/// </summary>
		public static T[] Concat<T>(this T[] x, T[] y)
		{
			if (x == null) throw new ArgumentNullException("x");
			if (y == null) throw new ArgumentNullException("y");
			int oldLen = x.Length;
			Array.Resize<T>(ref x, x.Length + y.Length);
			Array.Copy(y, 0, x, oldLen, y.Length);
			return x;
		}

		/// <summary>
		/// Fill an array with a single value
		/// </summary>
		public static void Fill<T>(this T[] arr, T val)
		{
			for (int i = 0; i < arr.Length; i++) arr[i] = val;
		}

		/// <summary>
		/// Fill an array with a single value
		/// </summary>
		public static void Fill<T>(this T[] arr, Func<T> val)
		{
			for (int i = 0; i < arr.Length; i++) arr[i] = val();
		}


		/// <summary>
		/// Fill a 2D array with a single value
		/// </summary>
		public static void Fill<T>(this T[,] arr, T val)
		{
			for (int i = 0; i < arr.GetLength(0); i++)
			{
				for (int j = 0; j < arr.GetLength(1); j++)
				{
					arr[i, j] = val;
				}
			}
		}

		public static void Fill<T>(this T[,] arr, Func<T> val)
		{
			for (int i = 0; i < arr.GetLength(0); i++)
			{
				for (int j = 0; j < arr.GetLength(1); j++)
				{
					arr[i, j] = val();
				}
			}
		}

		/// <summary>
		/// Creates an array and fills it with a default value 
		/// </summary>
		public static T[] CreateAndFill<T>(int size, T val)
		{
			T[] arr = new T[size];
			Fill(arr, val);
			return arr;
		}

				/// <summary>
		/// Creates an array and fills it with a default value 
		/// </summary>
		public static T[] CreateAndFill<T>(int size, Func<T> val)
		{
			T[] arr = new T[size];
			Fill(arr, val);
			return arr;
		}

		/// <summary>
		/// Creates a 2D array and fills it with a default value 
		/// </summary>
		public static T[,] CreateAndFill<T>(int sizeX, int sizeY, T val)
		{
			T[,] arr = new T[sizeX, sizeY];
			Fill(arr, val);
			return arr;
		}

		/// <summary>
		/// Creates a 2D array and fills it with a default value 
		/// </summary>
		public static T[,] CreateAndFill<T>(int sizeX, int sizeY, Func<T> val)
		{
			T[,] arr = new T[sizeX, sizeY];
			Fill(arr, val);
			return arr;
		}

		/// <summary>
		/// Shuffles the array in-place
		/// </summary>
		public static T[] Shuffle<T>(this T[] arr)
		{
			int i = arr.Length;
			Random random = new Random();
			while (i != 0) {
				int ri = (int)System.Math.Floor(random.NextDouble() * i--);
				(arr[i], arr[ri]) = (arr[ri], arr[i]);
			}

			return arr;
		}
		
		/// <summary>
		/// Shuffles the list in-place
		/// </summary>
		public static List<T> Shuffle<T>(this List<T> arr)
		{
			int i = arr.Count;
			Random random = new Random();
			while (i != 0) {
				int ri = (int)System.Math.Floor(random.NextDouble() * i--);
				(arr[i], arr[ri]) = (arr[ri], arr[i]);
			}

			return arr;
		}
	}
}
