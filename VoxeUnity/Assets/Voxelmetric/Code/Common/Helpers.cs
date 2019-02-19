﻿using System.Runtime.CompilerServices;
using System.Threading;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Common
{
	public static class Helpers
	{
		public static readonly int MainThreadID = Thread.CurrentThread.ManagedThreadId;

		public static bool IsMainThread
		{
			get { return Thread.CurrentThread.ManagedThreadId==MainThreadID; }
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetIndex1DFrom2D(int x, int z, int sizeX)
		{
			return x + z * sizeX;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetIndex1DFrom3D(int x, int y, int z, int sizeX, int sizeZ)
		{
			return x + sizeX * (z + y * sizeZ);
		}

		public static readonly int ZeroChunkIndex = Env.ChunkPadding+(Env.ChunkPadding<<Env.ChunkPow)+(Env.ChunkPadding<<Env.ChunkPow2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetChunkIndex1DFrom3D(int x, int y, int z)
		{
			int xx = x+Env.ChunkPadding;
			int yy = y+Env.ChunkPadding;
			int zz = z+Env.ChunkPadding;
			return xx+(zz<<Env.ChunkPow)+(yy<<Env.ChunkPow2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetChunkIndex1DFrom3D(int x, int y, int z, int pow)
		{
			int xx = x+Env.ChunkPadding;
			int yy = y+Env.ChunkPadding;
			int zz = z+Env.ChunkPadding;
			return xx + (zz << pow) + (yy << (pow << 1));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GetIndex2DFrom1D(int index, out int x, out int z, int sizeX)
		{
			x = index % sizeX;
			z = index / sizeX;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GetIndex3DFrom1D(int index, out int x, out int y, out int z, int sizeX, int sizeZ)
		{
			x = index % sizeX;
			y = index / (sizeX * sizeZ);
			z = (index / sizeX) % sizeZ;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GetChunkIndex3DFrom1D(int index, out int x, out int y, out int z)
		{
			x = index & Env.ChunkMask;
			y = index >> Env.ChunkPow2;
			z = (index >> Env.ChunkPow) & Env.ChunkMask;

			x -= Env.ChunkPadding;
			y -= Env.ChunkPadding;
			z -= Env.ChunkPadding;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GetChunkIndex3DFrom1D(int index, out int x, out int y, out int z, int pow)
		{
			x = index & Env.ChunkMask;
			y = index >> (pow<<1);
			z = (index >> pow) & ((1<<pow)-1);

			x -= Env.ChunkPadding;
			y -= Env.ChunkPadding;
			z -= Env.ChunkPadding;
		}

		// Returns a coordinate at the beggining of the chunk
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MakeChunkCoordinate(int x)
		{
			return ((x>=0 ? x : x-Env.ChunkSize1)/Env.ChunkSize)*Env.ChunkSize;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MakeChunkCoordinate(int x, int size)
		{
	        return ((x >= 0 ? x : x - (size-1)) / size) * size;
		}

	    /// <summary>
	    /// Returns the position of the chunk containing this block
	    /// </summary>
	    /// <returns>The position of the chunk containing this block</returns>
	    public static Vector3Int ContainingChunkPos(ref Vector3Int pos)
	    {
	        Vector3Int v;
	        v.x = MakeChunkCoordinate(pos.x);
	        v.y = MakeChunkCoordinate(pos.y);
	        v.z = MakeChunkCoordinate(pos.z);
	        return v;
	    }

        public static T[] CreateArray1D<T>(int size)
		{
			return new T[size];
		}

		public static T[] CreateAndInitArray1D<T>(int size)
		{
			var arr = new T[size];
			for (int i = 0; i < arr.Length; i++)
				arr[i] = default(T);

			return arr;
		}

		public static T[][] CreateArray2D<T>(int sizeX, int sizeY)
		{
			var arr = new T[sizeX][];

			for (int i = 0; i < arr.Length; i++)
				arr[i] = new T[sizeY];

			return arr;
		}

		public static T[][] CreateAndInitArray2D<T>(int sizeX, int sizeY)
		{
			var arr = new T[sizeX][];

			for (int i = 0; i < arr.Length; i++)
			{
				arr[i] = new T[sizeY];
				for (int j = 0; j < arr[i].Length; j++)
					arr[i][j] = default(T);
			}

			return arr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Interpolate(float x0, float x1, float alpha)
		{
			return x0 + (x1 - x0) * alpha;
		}

		// Finds the smallest positive t such that s+t*ds is an integer
		public static float IntBound(float s, float ds)
		{
			/* Recursive version
				if (ds < 0)
				{
					return IntBound(-s, -ds);
				}
				else
				{
					s = Mod(s, 1);
					// Problem is now s+t*ds = 1
					return (1 - s) / ds;
				}
			 */
			while (true)
			{
				if (ds < 0)
				{
					s = -s;
					ds = -ds;
					continue;
				}

				s = Mod(s, 1);
				return (1 - s) / ds;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int FastFloor(float val)
		{
			return (val > 0) ? (int)val : (int)val - 1;
		}

		// Custom modulo. Handles negative numbers.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Mod(int value, int modulus)
		{
			int r = value % modulus;
			return (r < 0) ? (r + modulus) : r;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Mod(float value, int modulus)
		{
			return (value % modulus + modulus) % modulus;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Mod3(uint value)
		{
			value = (value >> 16) + (value & 0xFFFF); // sum base 2**16 digits value <= 0x1FFFE
			value = (value >> 8) + (value & 0xFF); // sum base 2**8 digits value <= 0x2FD
			value = (value >> 4) + (value & 0xF); // sum base 2**4 digits value <= 0x3C; worst case 0x3B
			value = (value >> 2) + (value & 0x3); // sum base 2**2 digits value <= 0x1D; worst case 0x1B
			value = (value >> 2) + (value & 0x3); // sum base 2**2 digits value <= 0x9; worst case 0x7
			// Following line can be omitted at the cost of slightly more performance but less precision (would go down from 4 to 1 billion)
			value = (value >> 2) + (value & 0x3); // sum base 2**2 digits value <= 0x4
			if (value > 2) value = value - 3;
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(this int val, int min, int max)
		{
			if (val < min)
				return min;

			return val > max ? max : val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(this float val, float min, float max)
		{
			if (val < min)
				return min;

			return val > max ? max : val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Abs(int val)
		{
			return (val + (val >> 31)) ^ (val >> 31);
		}
	}
}
