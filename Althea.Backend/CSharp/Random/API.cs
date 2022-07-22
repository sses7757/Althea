using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
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
		private static unsafe bool Check<T, TS, TDist>(TS storage!!, in TDist distribution, out IntPtr pointer, out int length, out T offset, out T scale)
			where T : unmanaged, IBaseNumber<T>
			where TS : class, IStorage<T, TS>
			where TDist : struct, IRandomDistribution<TDist>
		{
			pointer = default; length = 0; offset = default; scale = default;
			if (storage is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			if (distribution is not (UniformDistribution<T> or RandomBitsDistribution<T>))
				return false; // not support
			if (!distribution.IsValid())
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(distribution));

			pointer = (IntPtr)(ps.Pointer.Pointer.Pointer.ToInt64() + ps.Pointer.OffsetInBytes); length = (int)ps.Pointer.LengthInBytes;
			if (distribution is UniformDistribution<T> u)
			{
				offset = u.LowerBound; scale = u.UpperBound - offset;
				if (!T.Type.IsInteger())
				{
					T s = scale;
					if (typeof(T) == typeof(Float64))
					{
						double r = ReciprocalD * (*(double*)&s);
						scale = *(T*)&r;
					}
					else if (typeof(T) == typeof(Float32))
					{
						float r = (float)(ReciprocalS * (*(float*)&s));
						scale = *(T*)&r;
					}
					else if (typeof(T) == typeof(Float16))
					{
						Half r = (Half)(ReciprocalH * ((double)*(Half*)&s));
						scale = *(T*)&r;
					}
					else
						return false;
				}
			}
			return true;
		}

		private const double ReciprocalD = 1.0 / (ulong.MaxValue + 1.0);
		private const double ReciprocalS = 1.0 / (uint.MaxValue + 1.0);
		private const double ReciprocalH = 1.0 / (ushort.MaxValue + 1.0);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void Generate<T>(IntPtr pointer, int length, T offset, T scale, long? seed) where T : unmanaged, IBaseNumber<T>
		{
			void* p = pointer.ToPointer();
			if (!seed.HasValue)
				System.Random.Shared.NextBytes(new Span<byte>(p, length));
			else
				new System.Random((int)seed.Value).NextBytes(new Span<byte>(p, length));
			////RandomNumberGenerator.Fill(new(p, length));
			if (scale == T.Zero) // random bits
				return;
			// else, random range
			int len = length / T.Size;
			DataType type = T.Type;
			switch (type)
			{
				case DataType.RealFloat32:
					LAD.VectorCastManaged((UnsignedInt32*)p, 1, (Float32*)p, 1, len);
					LAD.VectorModify<Float32, LAD.U_MultiplyScalar>((Float32*)p, 1, (Float32*)p, 1, len, *(Float32*)&scale);
					break;
				case DataType.RealFloat64:
					LAD.VectorCastManaged((UnsignedInt64*)p, 1, (Float64*)p, 1, len);
					LAD.VectorModify<Float64, LAD.U_MultiplyScalar>((Float64*)p, 1, (Float64*)p, 1, len, *(Float64*)&scale);
					break;
				case DataType.RealInt8:
				case DataType.RealInt16:
				case DataType.RealInt32:
				case DataType.RealInt64:
				case DataType.RealUInt8:
				case DataType.RealUInt16:
				case DataType.RealUInt32:
				case DataType.RealUInt64:
					LAD.VectorModify<T, LAD.U_Modulo>((T*)p, 1, (T*)p, 1, len, scale);
					break;
			}
			LAD.VectorModify<T, LAD.U_AddScalar>((T*)p, 1, (T*)p, 1, len, offset);
		}

		/// <inheritdoc/>
		public bool FillWithRandom<T, TS, TDist>(TS storage, in TDist distribution) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TDist : struct, IRank1Distribution<T, TDist>
		{
			if (!Check(storage, distribution, out var ptr, out int len, out T offset, out T scale))
				return false;
			Generate(ptr, len, offset, scale, distribution.RandomSeed);
			return true;
		}

		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, TS1, TS2, TDist>(TS1 storage1, TS2 storage2, in TDist distribution) => false;
		bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, TS1, TS2, TS3, TDist>(TS1 storage1, TS2 storage2, TS3 storage3, in TDist distribution) => false;
		bool Althea.Random.IAbstractApi.FillWithRandom<TDist>(ReadOnlySpan<IStorage> storages, in TDist distribution) => false;
		#endregion
	}
}
