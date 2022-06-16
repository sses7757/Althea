using System.Runtime.CompilerServices;
using System.Security.Cryptography;

using Althea.Backend.Storage;
using Althea.NativeTypes;
using Althea.Random;

using LAD = Althea.Backend.CSharp.LinearAlgebra.Api;


namespace Althea.Backend.CSharp.Random
{
	/// <summary>
	/// The C# back-end of <see cref="Althea.Random.IAbstractApi"/> that utilizes <see cref="System.Random"/>.
	/// </summary>
	/// <remarks>Only supports storages on CPU memory of primitive and pre-defined real types.<br/>
	/// Only supports the <see cref="UniformDistribution{T}"/> and <see cref="RandomBitsDistribution{T}"/>.</remarks>
	public class Api : Althea.Random.IAbstractApi
	{
		#region basic
		void IDisposable.Dispose()
		{
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public bool Disposed { get; set; } = false;

		/// <summary>
		/// Get the default <see cref="Api"/>.
		/// </summary>
		internal protected static readonly Api Default = new();
		#endregion

		#region operations
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool Check<T, TS>(TS storage!!, IRandomDistribution distribution!!, out IntPtr pointer, out int length, out T offset, out T scale)
			where T : unmanaged, INumber<T>
			where TS : class, IStorage<T, TS>
		{
			pointer = default; length = 0; offset = default; scale = default;
			if (distribution.RandomSeed.HasValue)
				return false; // not support
			if (!NumberType<T>.IsPrimitive)
				return false; // not support
			if (storage is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			if (distribution is not (UniformDistribution<T> or RandomBitsDistribution<T>))
				return false; // not support

			pointer = (IntPtr)(ps.Pointer.Pointer.Pointer.ToInt64() + ps.Pointer.OffsetInBytes); length = (int)ps.Pointer.LengthInBytes;
			if (distribution is UniformDistribution<T> u)
			{
				offset = u.LowerBound; scale = u.UpperBound - offset;
				if (!Unmanaged<T>.DataType.IsInteger())
				{
					T s = scale;
					if (typeof(T) == typeof(double))
					{
						double r = ReciprocalD * (*(double*)&s);
						scale = *(T*)&r;
					}
					else if (typeof(T) == typeof(float))
					{
						float r = (float)(ReciprocalS * (*(float*)&s));
						scale = *(T*)&r;
					}
					else if (typeof(T) == typeof(Half))
					{
						Half r = (Half)(ReciprocalH * ((double)*(Half*)&s));
						scale = *(T*)&r;
					}
				}
			}
			return true;
		}

		private const double ReciprocalD = 1.0 / (ulong.MaxValue + 1.0);
		private const double ReciprocalS = 1.0 / (uint.MaxValue + 1.0);
		private const double ReciprocalH = 1.0 / (ushort.MaxValue + 1.0);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void Generate<T>(IntPtr pointer, int length, T offset, T scale) where T : unmanaged, INumber<T>
		{
			void* p = pointer.ToPointer();
			RandomNumberGenerator.Fill(new(p, length));
			if (scale == T.Zero) // random bits
				return;
			// else, random range
			int len = length / Unmanaged<T>.Size;
			DataType type = Unmanaged<T>.DataType;
			switch (type)
			{
				case DataType.RealSingle:
					LAD.PointWiseCast((uint*)p, 1, (float*)p, 1, len);
					LAD.VectorModify<float, float, LAD.U_MultiplyScalar>((float*)p, 1, (float*)p, 1, len, *(float*)&scale);
					break;
				case DataType.RealDouble:
					LAD.PointWiseCast((ulong*)p, 1, (double*)p, 1, len);
					LAD.VectorModify<double, double, LAD.U_MultiplyScalar>((double*)p, 1, (double*)p, 1, len, *(double*)&scale);
					break;
				case DataType.RealInt8:
				case DataType.RealInt16:
				case DataType.RealInt32:
				case DataType.RealInt64:
				case DataType.RealUInt8:
				case DataType.RealUInt16:
				case DataType.RealUInt32:
				case DataType.RealUInt64:
					LAD.VectorModify<T, T, LAD.U_Modulo>((T*)p, 1, (T*)p, 1, len, scale);
					break;
			}
			LAD.VectorModify<T, T, LAD.U_AddScalar>((T*)p, 1, (T*)p, 1, len, offset);
		}

		/// <inheritdoc/>
		public bool FillWithRandom<T, TS>(TS storage, IRandomDistribution distribution) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (!Check(storage, distribution, out var ptr, out int len, out T offset, out T scale))
				return false;
			Generate(ptr, len, offset, scale);
			return true;
		}

		bool Althea.Random.IAbstractApi.FillWithRandom(IRandomDistribution distribution, params IStorage[] storages) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, TS1, TS2>(TS1 storage1, TS2 storage2, Rank2RandomDistribution<T1, T2> distribution) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, TS1, TS2, TS3>(TS1 storage1, TS2 storage2, TS3 storage3, Rank3RandomDistribution<T1, T2, T3> distribution) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, T4, TS1, TS2, TS3, TS4>(TS1 storage1, TS2 storage2, TS3 storage3, TS4 storage4, Rank4RandomDistribution<T1, T2, T3, T4> distribution) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, T4, T5, TS1, TS2, TS3, TS4, TS5>(TS1 storage1, TS2 storage2, TS3 storage3, TS4 storage4, TS5 storage5, Rank5RandomDistribution<T1, T2, T3, T4, T5> distribution) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, T4, T5, T6, TS1, TS2, TS3, TS4, TS5, TS6>(TS1 storage1, TS2 storage2, TS3 storage3, TS4 storage4, TS5 storage5, TS6 storage6, Rank6RandomDistribution<T1, T2, T3, T4, T5, T6> distribution) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, T4, T5, T6, T7, TS1, TS2, TS3, TS4, TS5, TS6, TS7>(TS1 storage1, TS2 storage2, TS3 storage3, TS4 storage4, TS5 storage5, TS6 storage6, TS7 storage7, Rank7RandomDistribution<T1, T2, T3, T4, T5, T6, T7> distribution) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, T4, T5, T6, T7, T8, TS1, TS2, TS3, TS4, TS5, TS6, TS7, TS8>(TS1 storage1, TS2 storage2, TS3 storage3, TS4 storage4, TS5 storage5, TS6 storage6, TS7 storage7, TS8 storage8, Rank8RandomDistribution<T1, T2, T3, T4, T5, T6, T7, T8> distribution) => false;

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, T4, T5, T6, T7, T8, T9, TS1, TS2, TS3, TS4, TS5, TS6, TS7, TS8, TS9>(TS1 storage1, TS2 storage2, TS3 storage3, TS4 storage4, TS5 storage5, TS6 storage6, TS7 storage7, TS8 storage8, TS9 storage9, Rank9RandomDistribution<T1, T2, T3, T4, T5, T6, T7, T8, T9> distribution) => false;
		#endregion
	}
}
