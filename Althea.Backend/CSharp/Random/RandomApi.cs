using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

using Althea.Backend.Storage;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Random;

using LAD = Althea.Backend.CSharp.LinearAlgebra.DenseApi;


namespace Althea.Backend.CSharp.Random
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// The C# back-end of <see cref="AbstractApi"/> that utilizes <see cref="System.Random"/>.<br/>
	/// Only supports storages on CPU memory of primitive and pre-defined real types.<br/>
	/// Only supports the <see cref="UniformDistribution{T}"/> and <see cref="RandomBitsDistribution{T}"/> or their <see cref="SimpleJointRandomDistribution"/> with dimension ≤ 3.<br/>
	/// Other distributions can be easily supported by utilizing the result of <see cref="UniformDistribution{T}"/>.
	/// </summary>
	public class RandomApi : AbstractApi
	{
		#region basic
		public RandomApi()
		{
			// do nothing
		}

		protected override void Dispose(bool disposeManaged)
		{
			// do nothing
		}
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Supported(CombinationOfLocations location) => location.Count == 1 && location[0].Type == LocationType.CpuRam;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedUnary(CombinationOfLocations location1) => Supported(location1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2) => Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => Supported(location1) && Supported(location2) && Supported(location3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNary(ReadOnlySpan<CombinationOfLocations> locations) => locations.All(Supported);
		#endregion

		#region methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool Check<T>(Storage<T> storage, IRandomDistribution distribution, out IntPtr pointer, out int length, out T offset, out T scale) where T : unmanaged
		{
			pointer = default; length = 0; offset = default; scale = default;
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));
			if (distribution is null)
				throw new ArgumentNullException(nameof(distribution));
			if (distribution.Count != 1)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));
			if (distribution.RandomSeed.HasValue)
				return false; // not support
			if (!Const<T>.IsPreDefined)
				return false; // not support
			var ss = storage[0];
			if (storage.Count != 1 || ss.Pointer is not IMemoryPointer p)
				return false; // not support
			if (distribution is not (UniformDistribution<T> or RandomBitsDistribution<T>))
				return false; // not support
			if (ss.LengthInBytes > int.MaxValue)
				return false; // not support
			if (Const<T>.IsComplex)
				return false; // not support

			pointer = (IntPtr)(p.Pointer.ToInt64() + ss.OffsetInBytes); length = (int)ss.LengthInBytes;
			if (distribution is UniformDistribution<T> u)
			{
				offset = u.LowerBound; scale = u.UpperBound.NativeSub(offset);
				if (Const<T>.DataTypeClass == DataTypeClassification.FloatPoint_IEEE754)
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
		private static unsafe void Generate<T>(IntPtr pointer, int length, T offset, T scale) where T : unmanaged
		{
			void* p = pointer.ToPointer();
			RandomNumberGenerator.Fill(new(p, length));
			if (scale.IsZero()) // random bits
				return;
			// else, random range
			long len = length / Const<T>.SizeT;
			DataType type = Const<T>.DataType;
			switch (type)
			{
				case DataType.RealSingle:
					var pS = new ManagedPureStorage<float>(p, len);
					LAD.PointWiseCast(new ManagedPureStorage<uint>(p, len), pS);
					LAD.Scale(pS, *(float*)&scale);
					LAD.PointWiseAddScalar(pS, *(float*)&offset);
					break;
				case DataType.RealDouble:
					var pD = new ManagedPureStorage<double>(p, len);
					LAD.PointWiseCast(new ManagedPureStorage<ulong>(p, len), pD);
					LAD.Scale(pD, *(double*)&scale);
					LAD.PointWiseAddScalar(pD, *(double*)&offset);
					break;
				case DataType.RealInt8:
					LAD.PointWiseModulo(new ManagedPureStorage<byte>(p, len), *(byte*)&scale);
					LAD.PointWiseAddScalar(new ManagedPureStorage<T>(p, len), offset);
					break;
				case DataType.RealInt16:
					LAD.PointWiseModulo(new ManagedPureStorage<ushort>(p, len), *(ushort*)&scale);
					LAD.PointWiseAddScalar(new ManagedPureStorage<T>(p, len), offset);
					break;
				case DataType.RealInt32:
					LAD.PointWiseModulo(new ManagedPureStorage<uint>(p, len), *(uint*)&scale);
					LAD.PointWiseAddScalar(new ManagedPureStorage<T>(p, len), offset);
					break;
				case DataType.RealInt64:
					LAD.PointWiseModulo(new ManagedPureStorage<ulong>(p, len), *(ulong*)&scale);
					LAD.PointWiseAddScalar(new ManagedPureStorage<T>(p, len), offset);
					break;
				case DataType.RealUInt8:
				case DataType.RealUInt16:
				case DataType.RealUInt32:
				case DataType.RealUInt64:
					var pU = new ManagedPureStorage<T>(p, len);
					LAD.PointWiseModulo(pU, scale);
					LAD.PointWiseAddScalar(pU, offset);
					break;
				default:
					break;
			}
		}

		protected override bool FillWithRandom_<T>(Storage<T> storage, IRandomDistribution distribution)
		{
			return FillWithRandom(storage, distribution);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal static new bool FillWithRandom<T>(Storage<T> storage, IRandomDistribution distribution) where T : unmanaged
		{
			if (!Check(storage, distribution, out var p, out int length, out T offset, out T scale))
				return false; // not support
			Generate(p, length, offset, scale);
			return true;
		}

		protected override bool FillWithRandom_<T1, T2>(Storage<T1> storage1, Storage<T2> storage2, IRandomDistribution distribution)
		{
			return FillWithRandom(storage1, storage2, distribution);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal static new bool FillWithRandom<T1, T2>(Storage<T1> storage1, Storage<T2> storage2, IRandomDistribution distribution) where T1 : unmanaged where T2 : unmanaged
		{
			if (distribution is null)
				throw new ArgumentNullException(nameof(distribution));
			if (distribution.Count != 2)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));
			if (distribution is not SimpleJointRandomDistribution d)
				return false; // not support
			if (!Check(storage1, d[0], out var p1, out int length1, out T1 offset1, out T1 scale1))
				return false; // not support
			if (!Check(storage2, d[1], out var p2, out int length2, out T2 offset2, out T2 scale2))
				return false; // not support

			Generate(p1, length1, offset1, scale1);
			Generate(p2, length2, offset2, scale2);
			return true;
		}

		protected override bool FillWithRandom_<T1, T2, T3>(Storage<T1> storage1, Storage<T2> storage2, Storage<T3> storage3, IRandomDistribution distribution)
		{
			return FillWithRandom(storage1, storage2, storage3, distribution);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal static new bool FillWithRandom<T1, T2, T3>(Storage<T1> storage1, Storage<T2> storage2, Storage<T3> storage3, IRandomDistribution distribution) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
		{
			if (distribution is null)
				throw new ArgumentNullException(nameof(distribution));
			if (distribution.Count != 3)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));
			if (distribution is not SimpleJointRandomDistribution d)
				return false; // not support
			if (!Check(storage1, d[0], out var p1, out int length1, out T1 offset1, out T1 scale1))
				return false; // not support
			if (!Check(storage2, d[1], out var p2, out int length2, out T2 offset2, out T2 scale2))
				return false; // not support
			if (!Check(storage3, d[1], out var p3, out int length3, out T3 offset3, out T3 scale3))
				return false; // not support

			Generate(p1, length1, offset1, scale1);
			Generate(p2, length2, offset2, scale2);
			Generate(p3, length3, offset3, scale3);
			return true;
		}

		protected override bool FillWithRandom_(IRandomDistribution distribution, params IStorage[] storages)
		{
			return FillWithRandom(distribution, storages);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal static new bool FillWithRandom(IRandomDistribution distribution, params IStorage[] storages)
		{
			return false; // not support
		}
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
