using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	/// <summary>
	/// The MKL back-end of <see cref="AbstractApi"/> that supports storage locations of CPU.
	/// </summary>
	public class DenseApi : AbstractApi
	{
		#region basic
		public DenseApi()
		{
			// do nothing
		}

		protected override void Dispose(bool disposeManaged)
		{
			// do nothing
		}

		/// <summary>
		/// Whether this implementation shall use the Gauss complexity reduction routines ("GEMM3M") or the original complex-typed general matrices multiplications ("GEMM")
		/// </summary>
		public bool ComplexGemmUseGemm3m {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}
		#endregion


		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckPointer<T>(Storage<T>? s, out IntPtr ptr, out int length, int stride = 1) where T : unmanaged
		{
			ptr = default; length = 0;
			if (s is null)
				return true;
			var p = s[0];
			if (s.Count != 1 || p.Pointer is not IMemoryPointer mp || !Supported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			len = (len - 1) / stride + 1;
			if (len > int.MaxValue)
				return false;
			length = (int)len;
			ptr = mp.OffsetPointer(p.OffsetInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckPointerLong<T>(Storage<T> s, out IntPtr ptr, out long length, int stride = 1) where T : unmanaged
		{
			ptr = default; length = 0;
			var p = s[0];
			if (s.Count != 1 || p.Pointer is not IMemoryPointer mp || !Supported(mp.Location))
				return false;
			length = p.LengthInBytes / Const<T>.SizeT;
			length = (length - 1) / stride + 1;
			ptr = mp.OffsetPointer(p.OffsetInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckPointer<T>(Storage<T>? A, long rows, long cols, long ld, out IntPtr ptr, out int r, out int c, out int l) where T : unmanaged
		{
			ptr = default; r = c = l = 1;
			if (A is null) // specific null input
				return true;
			var p = A[0];
			if (A.Count != 1 || p.Pointer is not IMemoryPointer mp || !Supported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			if (ld < rows)
				throw new ArgumentOutOfRangeException(nameof(ld), ld, Resources.Parameter.InvalidValue);
			if (cols * ld > len)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(A));
			if (rows > int.MaxValue || cols > int.MaxValue || ld > int.MaxValue)
				return false;
			r = (int)rows; c = (int)cols; l = (int)ld;
			ptr = mp.OffsetPointer(p.OffsetInBytes);
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckPointer<T>(Storage<T>? A, MatrixOperation op, long rowsAfterOp, long colsAfterOp, long ld, out MklOperation opMkl, out IntPtr ptr, out int r, out int c, out int l) where T : unmanaged
		{
			ptr = default; r = c = l = 1; opMkl = op.Simplify<T>().ToMkl();
			if (A is null) // specific null input
				return true;
			if (opMkl == MklOperation.ConjugateAlone)
				return false;
			var p = A[0];
			if (A.Count != 1 || p.Pointer is not IMemoryPointer mp || !Supported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			long cols = op.CanInPlace() ? colsAfterOp : rowsAfterOp;
			long rows = op.CanInPlace() ? rowsAfterOp : colsAfterOp;
			if (ld < rows)
				throw new ArgumentOutOfRangeException(nameof(ld), ld, Resources.Parameter.InvalidValue);
			if (cols * ld > len)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(A));
			if (rowsAfterOp > int.MaxValue || colsAfterOp > int.MaxValue || ld > int.MaxValue)
				return false;
			r = (int)rowsAfterOp; c = (int)colsAfterOp; l = (int)ld;
			ptr = mp.OffsetPointer(p.OffsetInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckPointerLong<T>(Storage<T>? A, long cols, long ld, out IntPtr ptr) where T : unmanaged
		{
			ptr = default;
			if (A is null) // specific null input
				return true;
			var p = A[0];
			if (A.Count != 1 || p.Pointer is not IMemoryPointer mp || !Supported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			if (cols * ld > len)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(A));
			ptr = mp.OffsetPointer(p.OffsetInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Supported(StorageLocation location) => location.Type == LocationType.CpuRam;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Supported(CombinationOfLocations location) => location.Count == 1 && Supported(location[0]);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3)
			=> Supported(location1) && Supported(location2) && Supported(location3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixUnary(CombinationOfLocations location)
			=> Supported(location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNormalTypeComplexType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> complexes) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNormalTypeRealType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> reals)
		{
			if (normals.IsEmpty || reals.IsEmpty)
				return false;
			for (int i = 0; i < normals.Length; i++)
			{
				if (!Supported(normals[i]))
					return false;
			}
			for (int i = 0; i < reals.Length; i++)
			{
				if (!Supported(reals[i]))
					return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix)
			=> Supported(vector1) && Supported(vector2) && Supported(matrix);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnary(CombinationOfLocations location)
			=> Supported(location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2) => Supported(matrix1) && Supported(matrix2) && Supported(vector);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix) => Supported(vector) && Supported(matrix);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixUnaryIndexUnary(CombinationOfLocations matrix, CombinationOfLocations index, DataType indexType) => Supported(matrix) && (index == default || Supported(index)) && (indexType == DataType.RealInt32 || indexType == DataType.RealUInt32);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixBinaryIndexUnary(CombinationOfLocations matrix1, CombinationOfLocations matrix2, CombinationOfLocations index, DataType indexType) => Supported(matrix1) && Supported(matrix2) && (index == default || Supported(index)) && (indexType == DataType.RealInt32 || indexType == DataType.RealUInt32);
		#endregion


		#region BLAS level 1
		/// <summary>
		/// Get the index of the element with horizontal maximum absolute value (<c>abs(x[i].real) + abs(x[i].imag)</c>) in <paramref name="x"/>
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <param name="x">The vector to get maximum absolute value's index</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">The output real index in <paramref name="x"/></param>
		/// <returns>Support or not</returns>
		internal protected static unsafe bool HorizontalAbsoluteValueArgMax<T>(Storage<T> x, int strideX, out long index) where T : unmanaged
		{
			index = -1;
			if (!Const<T>.IsComplex)
				return false;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.ComplexSingle => &NativeMethods.cblas_icamax,
				DataType.ComplexDouble => &NativeMethods.cblas_izamax,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		/// <summary>
		/// Get the index of the element with horizontal minimum absolute value (<c>abs(x[i].real) + abs(x[i].imag)</c>) in <paramref name="x"/>
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <param name="x">The vector to get minimum absolute value's index</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">The output real index in <paramref name="x"/></param>
		/// <returns>Support or not</returns>
		internal protected static unsafe bool HorizontalAbsoluteValueArgMin<T>(Storage<T> x, int strideX, out long index) where T : unmanaged
		{
			index = -1;
			if (!Const<T>.IsComplex)
				return false;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.ComplexSingle => &NativeMethods.cblas_icamin,
				DataType.ComplexDouble => &NativeMethods.cblas_izamin,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		/// <summary>
		/// Sum the absolute values (<c>abs(x[i].real) + abs(x[i].imag)</c>) of vector <paramref name="x"/>'s all elements
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <param name="x">The vector to be summed</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="sum">Output the sum as a <see cref="double"/></param>
		/// <returns>Support or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal 		protected static unsafe bool HorizontalAbsoluteSum<T>(Storage<T> x, int strideX, out double sum) where T : unmanaged
		{
			sum = 0;
			if (!Const<T>.IsComplex)
				return false;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (Const<T>.DataType == DataType.ComplexSingle)
			{
				sum = NativeMethods.cblas_scasum(n, px, strideX);
			}
			else if (Const<T>.DataType == DataType.ComplexDouble)
			{
				sum = NativeMethods.cblas_dzasum(n, px, strideX);
			}
			else
				return false;
			return true;
		}

		protected override unsafe bool AbsoluteValueArgMax_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cblas_isamax,
				DataType.RealDouble => &NativeMethods.cblas_idamax,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		protected override unsafe bool AbsoluteValueArgMin_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cblas_isamin,
				DataType.RealDouble => &NativeMethods.cblas_idamin,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool AbsSumOrNorm<T, Sum>(Storage<T> x, int strideX, out double sum) where T : unmanaged
		{
			bool doSum = typeof(Sum) == typeof(bool);
			sum = 0;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, float> funcS;
			delegate*<int, IntPtr, int, double> funcD;
			funcS = Const<T>.DataType switch
			{
				DataType.RealSingle => doSum ? &NativeMethods.cblas_sasum : &NativeMethods.cblas_snrm2,
				DataType.ComplexSingle => doSum ? null : &NativeMethods.cblas_scnrm2,
				_ => null,
			};
			funcD = Const<T>.DataType switch
			{
				DataType.RealDouble => doSum ? &NativeMethods.cblas_dasum : &NativeMethods.cblas_dnrm2,
				DataType.ComplexSingle => doSum ? null : &NativeMethods.cblas_dznrm2,
				_ => null,
			};
			if (funcS is not null)
			{
				sum = funcS(n, px, strideX);
			}
			else if(funcD is not null)
			{
				sum = funcD(n, px, strideX);
			}
			else
				return false;
			return true;
		}

		protected override unsafe bool AbsoluteValueSum_<T>(Storage<T> x, int strideX, out double sum)
		{
			return AbsSumOrNorm<T, bool>(x, strideX, out sum);
		}

		protected override unsafe bool Norm_<T>(Storage<T> x, int strideX, out double norm)
		{
			return AbsSumOrNorm<T, byte>(x, strideX, out norm);
		}

		protected override unsafe bool Dot_<T>(bool conjX, Storage<T> x, int strideX, Storage<T> y, int strideY, out T dot)
		{
			dot = default;
			if (!CheckPointer(x, out var px, out var n1, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var n2, strideY))
				return false;
			int n = Math.Min(n1, n2);
			delegate*<int, IntPtr, int, IntPtr, int, T*, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					float dotS = NativeMethods.cblas_sdot(n, px, strideX, py, strideY);
					dot = *(T*)&dotS;
					return true;
				case DataType.RealDouble:
					double dotD = NativeMethods.cblas_ddot(n, px, strideX, py, strideY);
					dot = *(T*)&dotD;
					return true;
				case DataType.ComplexSingle:
					func = conjX ? &NativeMethods.cblas_cdotc_sub : &NativeMethods.cblas_cdotu_sub;
					break;
				case DataType.ComplexDouble:
					func = conjX ? &NativeMethods.cblas_zdotc_sub : &NativeMethods.cblas_zdotu_sub;
					break;
				default:
					return false;
			}
			T dotC;
			func(n, px, strideX, py, strideY, &dotC);
			dot = dotC;
			return true;
		}

		protected override unsafe bool Scale_<T>(Storage<T> x, int strideX, T scalar)
		{
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_sscal(n, *(float*)&scalar, px, strideX);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dscal(n, *(double*)&scalar, px, strideX);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_cscal;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_zscal;
					break;
				default:
					return false;
			}
			func(n, &scalar, px, strideX);
			return true;
		}

		protected override unsafe bool VectorGeneralAdd_<T>(T α, Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (!CheckPointer(x, out var px, out var n1, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var n2, strideY))
				return false;
			int n = Math.Min(n1, n2);
			delegate*<int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_saxpy(n, *(float*)&α, px, strideX, py, strideY);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_daxpy(n, *(double*)&α, px, strideX, py, strideY);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_caxpy;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_zaxpy;
					break;
				default:
					return false;
			}
			func(n, &α, px, strideX, py, strideY);
			return true;
		}
		#endregion


		#region custom level 1
		protected override unsafe bool AggregateProduct_<T>(Storage<T> x, int stride, out T product)
		{
			product = default;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			T result;
			NativeMethods.vecProd(Const<T>.DataType, px, n, stride, &result);
			product = result;
			return true;
		}

		protected override unsafe bool AggregateSum_<T>(Storage<T> x, int stride, out T sum)
		{
			sum = default;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			T result;
			NativeMethods.vecSum(Const<T>.DataType, px, n, stride, &result);
			sum = result;
			return true;
		}

		protected override unsafe bool PartialProduct_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointerLong(y, out var py, out var ny, strideY))
				return false;
			long n = Math.Min(nx, ny);
			NativeMethods.vecParProd(Const<T>.DataType, px, py, n, inclusive, strideX, strideY);
			return true;
		}

		protected override bool PartialSum_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointerLong(y, out var py, out var ny, strideY))
				return false;
			long n = Math.Min(nx, ny);
			NativeMethods.vecParSum(Const<T>.DataType, px, py, n, inclusive, strideX, strideY);
			return true;
		}

		protected override unsafe bool PointWiseAddScalar_<T>(Storage<T> x, int stride, T scalr)
		{
			if (scalr.IsZero())
				return true;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			NativeMethods.vecAddScalar(Const<T>.DataType, px, &scalr, n, stride);
			return true;
		}

		protected override bool PointWiseCast_<T, TOut>(Storage<T> source, int incSrc, Storage<TOut> destination, int incDst)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(source, out var px, out var nx, incSrc))
				return false;
			if (!CheckPointerLong(destination, out var py, out var ny, incDst))
				return false;
			long n = Math.Min(nx, ny);
			NativeMethods.vecDataConvert(Const<T>.DataType, Const<TOut>.DataType, px, py, n, incSrc, incDst, true);
			return true;
		}

		protected override bool PointWiseConjugate_<T>(Storage<T> x, int stride)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			NativeMethods.vecConj(Const<T>.DataType, px, n, stride);
			return true;
		}

		protected override bool PointWiseDivide_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointerLong(y, out var py, out var ny, strideY))
				return false;
			long n = Math.Min(nx, ny);
			NativeMethods.vecsMulDiv(Const<T>.DataType, px, py, n, strideX, strideY, false);
			return true;
		}

		protected override bool PointWiseEquals_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, out bool equals)
		{
			equals = false;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointerLong(y, out var py, out var ny, strideY))
				return false;
			long n = Math.Min(nx, ny);
			equals = NativeMethods.vecsEq(Const<T>.DataType, px, py, n, strideX, strideY);
			return true;
		}

		protected override bool PointWiseMultiply_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointerLong(y, out var py, out var ny, strideY))
				return false;
			long n = Math.Min(nx, ny);
			NativeMethods.vecsMulDiv(Const<T>.DataType, px, py, n, strideX, strideY, true);
			return true;
		}

		protected override unsafe bool PointWisePower_<T>(Storage<T> x, int stride, double p)
		{
			if (p == 1)
				return true;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			if (p == 0)
			{
				T one = Const<T>.One;
				NativeMethods.vecFillVal(Const<T>.DataType, px, &one, n, stride);
				return true;
			}
			if (p == 2)
			{
				NativeMethods.vecsMulDiv(Const<T>.DataType, px, px, n, stride, stride, true);
				return true;
			}
			T pp = p.FromDouble<T>(); // for complex type, (&pp)[0..sizeof(T)/2] == (T::value_type)p
			NativeMethods.vecPowSameType(Const<T>.DataType, px, &pp, n, stride);
			return true;
		}

		protected override unsafe bool PointWisePower_<T>(Storage<T> x, int stride, T p)
		{
			if (p.IsEqual(Const<T>.One))
				return true;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			if (p.IsZero())
			{
				T one = Const<T>.One;
				NativeMethods.vecFillVal(Const<T>.DataType, px, &one, n, stride);
				return true;
			}
			if (p.IsEqual(Const<T>.Two))
			{
				NativeMethods.vecsMulDiv(Const<T>.DataType, px, px, n, stride, stride, true);
				return true;
			}
			NativeMethods.vecPowSameType(Const<T>.DataType, px, &p, n, stride);
			return true;
		}

		protected override unsafe bool TruncateArray_<T>(Storage<T> x, int stride, double threshold)
		{
			if (threshold <= 0)
				throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resources.Parameter.MustPositive);
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			T pp = threshold.FromDouble<T>();
			NativeMethods.vecClip(Const<T>.DataType, px, &pp, n, stride);
			return true;
		}
		#endregion


		#region BLAS level 2
		// Ignore Spelling: func
		protected override unsafe bool GeneralMatrixMultiplyVector_<T>(MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY)
		{
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;
			////if (nx < (opMkl == MklBlasOperation.None ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < (opMkl == MklBlasOperation.None ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<MklMatrixLayout, MklOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_sgemv(MklMatrixLayout.ColMajor, opMkl, mm,nn, *(float*)&α, pA, llda, px, strideX, *(float*)&β, py, strideY);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dgemv(MklMatrixLayout.ColMajor, opMkl, mm, nn, *(double*)&α, pA, llda, px, strideX, *(double*)&β, py, strideY);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_cgemv;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_zgemv;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, opMkl, mm, nn, &α, pA, llda, px, strideX, &β, py, strideY);
			return true;
		}

		protected override unsafe bool SymmHermMatrixMultiplyVector_<T>(bool fillUpper, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			if (!hermA && Const<T>.IsComplex)
				return false;

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_ssymv(MklMatrixLayout.ColMajor, fill, nn, *(float*)&α, pA, llda, px, strideX, *(float*)&β, py, strideY);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dsymv(MklMatrixLayout.ColMajor, fill, nn, *(double*)&α, pA, llda, px, strideX, *(double*)&β, py, strideY);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_chemv;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_zhemv;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, nn, &α, pA, llda, px, strideX, &β, py, strideY);
			return true;
		}

		protected override unsafe bool GenralRankOneUpdate_<T>(bool conjY, long m, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			////if (nx < mm)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<MklMatrixLayout, int, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_sger(MklMatrixLayout.ColMajor, mm, nn, *(float*)&α,  px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dger(MklMatrixLayout.ColMajor, mm, nn, *(double*)&α, px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = conjY ? &NativeMethods.cblas_cgerc : &NativeMethods.cblas_cgerc;
					break;
				case DataType.ComplexDouble:
					func = conjY ? &NativeMethods.cblas_zgerc : &NativeMethods.cblas_zgerc;
					break;
				default:
					return false;
			}
			// scale A
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			// add to A
			func(MklMatrixLayout.ColMajor, mm, nn, &α, px, strideX, py, strideY, pA, llda);
			return true;
		}

		protected override unsafe bool SymmHermRankOneUpdate_<T>(bool fillUpper, bool conjX, long n, T α, Storage<T> x, int strideX, T β, Storage<T> A, long lda)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			if (!conjX && Const<T>.IsComplex)
				return false;
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_ssyr(MklMatrixLayout.ColMajor, fill, nn, *(float*)&α, px, strideX, pA, llda);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dsyr(MklMatrixLayout.ColMajor, fill, nn, *(double*)&α, px, strideX, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_cher;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_zher;
					break;
				default:
					return false;
			}
			// scale A
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			// add to A
			func(MklMatrixLayout.ColMajor, fill, nn, &α, px, strideX, pA, llda);
			return true;
		}

		protected override unsafe bool SymmHermRankTwoUpdate_<T>(bool fillUpper, bool conjugate, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			if (!conjugate && Const<T>.IsComplex)
				return false;
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_ssyr2(MklMatrixLayout.ColMajor, fill, nn, *(float*)&α, px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dsyr2(MklMatrixLayout.ColMajor, fill, nn, *(double*)&α, px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_cher2;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_zher2;
					break;
				default:
					return false;
			}
			// scale A
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			// add to A
			func(MklMatrixLayout.ColMajor, fill, nn, &α, px, strideX, py, strideY, pA, llda);
			return true;
		}

		protected override unsafe bool TriangularMatrixMultiplyVector_<T>(bool fillUpper, bool unitDiag, MatrixOperation op, long n, Storage<T> A, long lda, Storage<T> x, int strideX)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			MklBlasDiagType diag = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, MklBlasDiagType, int, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_strmv(MklMatrixLayout.ColMajor, fill, opMkl, diag, nn, px, strideX, pA, llda);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dtrmv(MklMatrixLayout.ColMajor, fill, opMkl, diag, nn, px, strideX, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_ctrmv;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_ztrmv;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, opMkl, diag, nn, px, strideX, pA, llda);
			return true;
		}
		#endregion


		#region BLAS like level 2
		protected override unsafe bool DiagonalMatrixMultiplyGeneral_<T>(bool leftA, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
				return false;
			////if (nx < (leftA ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			delegate*<MklMatrixLayout, in MklBlasSideMode, in int, in int, in IntPtr, in int, in IntPtr, in int, ref IntPtr, in int, int, in int, void> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cblas_sdgmm_batch,
				DataType.RealDouble => &NativeMethods.cblas_ddgmm_batch,
				DataType.ComplexSingle => &NativeMethods.cblas_cdgmm_batch,
				DataType.ComplexDouble => &NativeMethods.cblas_zdgmm_batch,
				_ => null,
			};
			IntPtr cacheC = default;
			if (!β.IsZero())
				cacheC = Marshal.AllocHGlobal((IntPtr)(sizeof(T) * m * n));
			var oldC = new ManagedPureStorage<T>(cacheC, m * n);
			try
			{
				// cache C
				if (!β.IsZero())
				{
					if (!this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, Const<T>.One, C, ldc, default, default, default, oldC, m))
						return false;
				}
				// overwrite C by diagonal multiply result
				var side = leftA ? MklBlasSideMode.Right : MklBlasSideMode.Left;
				int one = 1;
				func(MklMatrixLayout.ColMajor, in side, in mm, in nn, in pA, in llda, in px, in strideX, ref pC, in lldc, 1, in one);
				// C = α * C + β * oldC
				if (!β.IsZero())
					return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, β, oldC, m, C, ldc);
				else if (!α.IsOne())
					return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, default, default, C, ldc);
				else
					return true;
			}
			finally
			{
				if (cacheC != default)
					Marshal.FreeHGlobal(cacheC);
			}
		}
		#endregion


		#region BLAS level 3
		protected override unsafe bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if (!CheckPointer(A, m, m, lda, out var pA, out int mm, out _, out int llda))
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out _, out int nn, out int lldb))
				return false;
			if (α.IsZero()) // result is 0
				return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, B, ldb, default, default, default, B, ldb);
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;

			var side = leftA ? MklBlasSideMode.Right : MklBlasSideMode.Left;
			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var diag = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			delegate*<MklMatrixLayout, MklBlasSideMode, MklFillMode, MklOperation, MklBlasDiagType, int, int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_strsm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(float*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dtrsm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(double*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_ctrsm;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_ztrsm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, &α, pA, llda, pB, lldb);
			return true;
		}

		protected override unsafe bool TriangularMatrixMultiply_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc)
		{
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out int mm, out int nn, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
				return false;
			if (!CheckPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
				return false;
			if (α.IsZero()) // result is 0
				return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, default, default, C, ldc);
			////if (pC == pB && ldc != ldb)
			////	throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(ldc));
			if (pC != pB)
			{   // copy B to C
				if (!this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, Const<T>.One, B, ldb, default, default, default, C, ldc))
					return false;
				lldb = lldc; pB = pC;
			}

			var side = leftA ? MklBlasSideMode.Right : MklBlasSideMode.Left;
			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var diag = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			delegate*<MklMatrixLayout, MklBlasSideMode, MklFillMode, MklOperation, MklBlasDiagType, int, int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_strmm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(float*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dtrmm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(double*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.ComplexSingle:
					func = &NativeMethods.cblas_ctrmm;
					break;
				case DataType.ComplexDouble:
					func = &NativeMethods.cblas_ztrmm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, &α, pA, llda, pB, lldb);
			return true;
		}

		protected override unsafe bool GeneralMatricesAdd_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, opA, m, n, lda, out var opcA, out var pA, out _, out _, out int llda))
				return false;
			if (!CheckPointer(B, opB, m, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;
			// shortcut
			if ((A is null || α.IsZero()) || (B is null || β.IsZero()))
			{
				if ((A is null || α.IsZero()) && opB == MatrixOperation.None && β.IsOne())
				{   // copy B to C
					Storage.StorageApi.PointerMemoryCopy2D(pC, ldc * sizeof(T), pB, ldb * sizeof(T), m * sizeof(T), n);
					return true;
				}
				if ((B is null || β.IsZero()) && opA == MatrixOperation.None && α.IsOne())
				{   // copy A to C
					Storage.StorageApi.PointerMemoryCopy2D(pC, ldc * sizeof(T), pA, lda * sizeof(T), m * sizeof(T), n);
					return true;
				}
				// matrix copy
				if (A is null || α.IsZero())
				{
					pA = pB; llda = lldb; α = β; opcA = opcB;
				}
				if (pA != pC)
				{
					var cpyFunc = Const<T>.DataType switch
					{
						DataType.RealSingle => new NativeMethods.omatcopy<float>(NativeMethods.MKL_Somatcopy) as NativeMethods.omatcopy<T>,
						DataType.RealDouble => new NativeMethods.omatcopy<double>(NativeMethods.MKL_Domatcopy) as NativeMethods.omatcopy<T>,
						DataType.ComplexSingle => new NativeMethods.omatcopy<ComplexSingle>(NativeMethods.MKL_Comatcopy) as NativeMethods.omatcopy<T>,
						DataType.ComplexDouble => new NativeMethods.omatcopy<ComplexDouble>(NativeMethods.MKL_Zomatcopy) as NativeMethods.omatcopy<T>,
						_ => null,
					};
					if (cpyFunc is null)
						return false;
					cpyFunc(MklMatrixLayoutChar.ColMajor, opcA.ToChar(), mm, nn, α, pA, llda, pC, lldc);
				}
				else
				{
					if (lda != ldc)
						throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(lda));
					var cpyFunc = Const<T>.DataType switch
					{
						DataType.RealSingle => new NativeMethods.imatcopy<float>(NativeMethods.MKL_Simatcopy) as NativeMethods.imatcopy<T>,
						DataType.RealDouble => new NativeMethods.imatcopy<double>(NativeMethods.MKL_Dimatcopy) as NativeMethods.imatcopy<T>,
						DataType.ComplexSingle => new NativeMethods.imatcopy<ComplexSingle>(NativeMethods.MKL_Cimatcopy) as NativeMethods.imatcopy<T>,
						DataType.ComplexDouble => new NativeMethods.imatcopy<ComplexDouble>(NativeMethods.MKL_Zimatcopy) as NativeMethods.imatcopy<T>,
						_ => null,
					};
					if (cpyFunc is null)
						return false;
					cpyFunc(MklMatrixLayoutChar.ColMajor, opcA.ToChar(), mm, nn, α, pA, llda);
				}
			}
			// both matrices are not null
			var func = Const<T>.DataType switch
			{
				DataType.RealSingle => new NativeMethods.omatadd<float>(NativeMethods.MKL_Somatadd) as NativeMethods.omatadd<T>,
				DataType.RealDouble => new NativeMethods.omatadd<double>(NativeMethods.MKL_Domatadd) as NativeMethods.omatadd<T>,
				DataType.ComplexSingle => new NativeMethods.omatadd<ComplexSingle>(NativeMethods.MKL_Comatadd) as NativeMethods.omatadd<T>,
				DataType.ComplexDouble => new NativeMethods.omatadd<ComplexDouble>(NativeMethods.MKL_Zomatadd) as NativeMethods.omatadd<T>,
				_ => null,
			};
			if (func is null)
				return false;
			func(MklMatrixLayoutChar.ColMajor, opcA.ToChar(), opcB.ToChar(), mm, nn, α, pA, llda, β, pB, lldb, pC, lldc);
			return true;
		}

		protected override unsafe bool GeneralMatricesMultiply_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, opA, m, k, lda, out var opcA, out var pA, out _, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, opB, k, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;

			delegate*<MklMatrixLayout, MklOperation, MklOperation, int, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_sgemm(MklMatrixLayout.ColMajor, opcA, opcB, mm, nn, kk, *(float*)&α, pA, llda, pB, lldb, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dgemm(MklMatrixLayout.ColMajor, opcA, opcB, mm, nn, kk, *(double*)&α, pA, llda, pB, lldb, *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = this.ComplexGemmUseGemm3m ? &NativeMethods.cblas_cgemm3m : &NativeMethods.cblas_cgemm;
					break;
				case DataType.ComplexDouble:
					func = this.ComplexGemmUseGemm3m ? &NativeMethods.cblas_zgemm3m : &NativeMethods.cblas_zgemm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, opcA, opcB, mm, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;

			var side = leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right;
			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklBlasSideMode, MklFillMode, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_ssymm(MklMatrixLayout.ColMajor, side, fill, mm, nn, *(float*)&α, pA, llda, pB, lldb, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dsymm(MklMatrixLayout.ColMajor, side, fill, mm, nn, *(double*)&α, pA, llda, pB, lldb, *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = hermA ? &NativeMethods.cblas_csymm : &NativeMethods.cblas_chemm;
					break;
				case DataType.ComplexDouble:
					func = hermA ? &NativeMethods.cblas_zsymm : &NativeMethods.cblas_zhemm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, side, fill, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, op, n, k, lda, out var opMkl, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, int, int, T*, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_ssyrk(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(float*)&α, pA, llda, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dsyrk(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(double*)&α, pA, llda,  *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = conjA ? &NativeMethods.cblas_cherk : &NativeMethods.cblas_csyrk;
					break;
				case DataType.ComplexDouble:
					func = conjA ? &NativeMethods.cblas_zherk : &NativeMethods.cblas_zsyrk;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, &α, pA, llda, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankTwoKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, op, n, k, lda, out var opMkl, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NativeMethods.cblas_ssyr2k(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(float*)&α, pA, llda, pB, lldb, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NativeMethods.cblas_dsyr2k(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(double*)&α, pA, llda, pB, lldb, *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = conjugate ? &NativeMethods.cblas_cher2k : &NativeMethods.cblas_csyr2k;
					break;
				case DataType.ComplexDouble:
					func = conjugate ? &NativeMethods.cblas_zher2k : &NativeMethods.cblas_zsyr2k;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankKUpdateVariant_<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			return false;
		}
		#endregion


		#region custom level 3
		protected override bool MatrixCopyUpperLowerParts_<T>(bool storedUpper, bool hermitian, long n, Storage<T> A, long lda)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(A, n, lda, out var pA))
				return false;
			NativeMethods.matMakeHerm(Const<T>.DataType, pA, lda, n, storedUpper, hermitian);
			return true;
		}

		protected override bool MatrixClearUpperLowerPart_<T>(bool clearLower, long n, Storage<T> A, long lda)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(A, n, lda, out var pA))
				return false;
			NativeMethods.matTriClear(Const<T>.DataType, pA, lda, n, clearLower);
			return true;
		}

		// Ignore Spelling: lda ma na ldb mb nb ldc
		protected override unsafe bool MatrixKronecker_<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(A, na, lda, out var pA))
				return false;
			if (!CheckPointerLong(B, nb, ldb, out var pB))
				return false;
			////if (ldc < ma * mb)
			////	throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(ldc));

			if (!CheckPointerLong(C, na * nb, ldc, out var pC))
				return false;
			NativeMethods.matKron(Const<T>.DataType, pA, lda, ma, na, pB, ldb, mb, nb, pC, ldc, &α, &β);
			return true;
		}
		#endregion


		#region solve
		#region linear solve
		protected override unsafe bool LinearSolve_<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<TInd>? work = null)
		{
			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(B, n, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
				return false;
			if (!CheckPointer(work, out var pW, out int nw))
				return false;
			////if (nw > 0 && nw < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(work));
			
			delegate*<MklMatrixLayout, int, int, IntPtr, int, IntPtr, IntPtr, int, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.LAPACKE_sgesv,
				DataType.RealDouble => &NativeMethods.LAPACKE_dgesv,
				DataType.ComplexSingle => &NativeMethods.LAPACKE_cgesv,
				DataType.ComplexDouble => &NativeMethods.LAPACKE_zgesv,
				_ => null,
			};
			if (func is null)
				return false;
			// calculate
			IntPtr tau;
			if (pW == default)
				tau = Marshal.AllocHGlobal((IntPtr)(n * sizeof(T)));
			else
				tau = pW;
			try
			{
				var info = func(MklMatrixLayout.ColMajor, nn, nnrhs, pA, llda, tau, pB, lldb);
				SolveMethodKind.LU.CheckLapackInfo(info);
				return true;
			}
			finally
			{
				if (tau != pW)
					Marshal.FreeHGlobal(tau);
			}
		}
		#endregion

		#region QR
		// modify these methods to use a unified buffer for (maybe) better performance
		protected override unsafe bool LeastSquareSolve_<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work = null)
		{
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			if (!CheckPointer(B, n, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
				return false;

			delegate*<MklMatrixLayout, MklOperationChar, int, int, int, IntPtr, int, IntPtr, int, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.LAPACKE_sgels,
				DataType.RealDouble => &NativeMethods.LAPACKE_dgels,
				DataType.ComplexSingle => &NativeMethods.LAPACKE_cgels,
				DataType.ComplexDouble => &NativeMethods.LAPACKE_zgels,
				_ => null,
			};
			if (func is null)
				return false;
			var info = func(MklMatrixLayout.ColMajor, MklOperationChar.NoneTranspose, mm, nn, nnrhs, pA, llda, pB, lldb);
			SolveMethodKind.QR.CheckLapackInfo(info);
			return true;
		}

		protected override unsafe bool QRDecomposition_<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq, Storage<T>? work = null)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			int kk = Math.Min(mm, nn); long colsQ = full ? m : kk;
			if (!CheckPointer(Q, m, colsQ, ldq, out var pQ, out _, out int nnQ, out int lldq))
				return false;
			if (!CheckPointer(work, out var pW, out int nw))
				return false;
			////if (nw > 0 && nw < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(work));

			delegate*<MklMatrixLayout, int, int, IntPtr, int, IntPtr, MklLapackInfo> qrfunc;
			qrfunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.LAPACKE_sgeqrf,
				DataType.RealDouble => &NativeMethods.LAPACKE_dgeqrf,
				DataType.ComplexSingle => &NativeMethods.LAPACKE_cgeqrf,
				DataType.ComplexDouble => &NativeMethods.LAPACKE_zgeqrf,
				_ => null,
			};
			if (qrfunc is null)
				return false;
			delegate*<MklMatrixLayout, int, int, int, IntPtr, int, IntPtr, MklLapackInfo> gqfunc;
			gqfunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.LAPACKE_sorgqr,
				DataType.RealDouble => &NativeMethods.LAPACKE_dorgqr,
				DataType.ComplexSingle => &NativeMethods.LAPACKE_cungqr,
				DataType.ComplexDouble => &NativeMethods.LAPACKE_zungqr,
				_ => null,
			};
			// calculate
			IntPtr tau;
			if (pW == default)
				tau = Marshal.AllocHGlobal((IntPtr)(kk * sizeof(T)));
			else
				tau = pW;
			try
			{
				// implicit QR
				var info = qrfunc(MklMatrixLayout.ColMajor, mm, nn, pA, llda, tau);
				SolveMethodKind.QR.CheckLapackInfo(info);
				// copy A to Q
				Storage.StorageApi.PointerMemoryCopy2D(pA, lda, pQ, ldq,  m, Math.Min(colsQ, n));
				// form Q
				info = gqfunc(MklMatrixLayout.ColMajor, mm, nnQ, kk, pQ, lldq, tau);
				SolveMethodKind.QR.CheckLapackInfo(info);
				return true;
			}
			finally
			{
				if (tau != pW)
					Marshal.FreeHGlobal(tau);
			}
		}
		#endregion

		#region simple eigen
		protected override unsafe bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda)
		{
			if ((!Const<T>.IsComplex && typeof(T) != typeof(TReal)) ||
				(Const<T>.IsComplex && (Const<TReal>.IsComplex || typeof(T).GenericTypeArguments[0] != typeof(TReal))))
				throw new TypeMismatchException(typeof(T), typeof(TReal), TypeMismatchException.MismatchReason.IsNotRealCorrespondence);

			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklFillModeChar, int, IntPtr, int, IntPtr, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.LAPACKE_ssyev,
				DataType.RealDouble => &NativeMethods.LAPACKE_dsyev,
				DataType.ComplexSingle => &NativeMethods.LAPACKE_cheev,
				DataType.ComplexDouble => &NativeMethods.LAPACKE_zheev,
				_ => null,
			};
			if (func is null)
				return false;
			var info = func(MklMatrixLayout.ColMajor, mode.ToChar(), MklFillModeChar.Upper, nn, pA, llda, pV);
			SolveMethodKind.Eigenvalue.CheckLapackInfo(info);
			return true;
		}

		protected override unsafe bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType eigType, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if ((!Const<T>.IsComplex && typeof(T) != typeof(TReal)) ||
				(Const<T>.IsComplex && (Const<TReal>.IsComplex || typeof(T).GenericTypeArguments[0] != typeof(TReal))))
				throw new TypeMismatchException(typeof(T), typeof(TReal), TypeMismatchException.MismatchReason.IsNotRealCorrespondence);

			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(B, n, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<MklMatrixLayout, GeneralEigenType, MklVectorModeChar, MklFillModeChar, int, IntPtr, int, IntPtr, int, IntPtr, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.LAPACKE_ssygv,
				DataType.RealDouble => &NativeMethods.LAPACKE_dsygv,
				DataType.ComplexSingle => &NativeMethods.LAPACKE_chegv,
				DataType.ComplexDouble => &NativeMethods.LAPACKE_zhegv,
				_ => null,
			};
			if (func is null)
				return false;
			var info = func(MklMatrixLayout.ColMajor, eigType, mode.ToChar(), MklFillModeChar.Upper, nn, pA, llda, pB, lldb, pV);
			SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
			return true;
		}
		#endregion

		#region general eigen
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe bool CopyEigenToComplex<T, TComplex>(MklVectorModeChar modeL, MklVectorModeChar modeR, long n, int nn, IntPtr pV, T* valR, T* valI, Storage<TComplex>? leftVec, IntPtr pVl, long ldvl, T* vecL, Storage<TComplex>? rightVec, IntPtr pVr, long ldvr, T* vecR) where T : unmanaged where TComplex : unmanaged
		{
			// copy eigenvalues
			Storage.StorageApi.PointerStridedCopy(valR, 1, (T*)pV, 2, nn);
			Storage.StorageApi.PointerStridedCopy(valI, 1, 1 + (T*)pV, 2, nn);
			// expand cases for better performance
			float* floatValI = (float*)valI; double* doubleValI = (double*)valI;
			if (leftVec is not null && rightVec is not null)
			{
				// set eigenvectors to zeros
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, leftVec, ldvl, default, default, default, leftVec, ldvl))
					return false;
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, rightVec, ldvr, default, default, default, rightVec, ldvr))
					return false;
				ldvl *= 2; ldvr *= 2;
				// copy eigenvectors
				for (int i = 0; i < nn; i++)
				{
					// copy real parts in both cases
					Storage.StorageApi.PointerStridedCopy(vecL + n * i, 1, (T*)pVl + i * ldvl, 2, nn);
					Storage.StorageApi.PointerStridedCopy(vecR + n * i, 1, (T*)pVr + i * ldvr, 2, nn);
					// check real or complex eigen-pair
					if ((typeof(T) == typeof(float) && floatValI[i] != 0) || (typeof(T) == typeof(double) && doubleValI[i] != 0))
					{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
						// left
						T* ptr = (T*)pVl + (i * ldvl + 1), ptr2 = ptr + ldvl;
						Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr, 2, nn);
						Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
						// right
						ptr = (T*)pVr + (i * ldvr + 1); ptr2 = ptr + ldvr;
						Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr, 2, nn);
						Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
				}
			}
			else if (leftVec is not null && rightVec is null)
			{
				// set eigenvectors to zeros
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, leftVec, ldvl, default, default, default, leftVec, ldvl))
					return false;
				ldvl *= 2; ldvr *= 2;
				// copy eigenvectors
				for (int i = 0; i < nn; i++)
				{
					// copy real parts in both cases
					Storage.StorageApi.PointerStridedCopy(vecL + n * i, 1, (T*)pVl + i * ldvl, 2, nn);
					// check real or complex eigen-pair
					if ((typeof(T) == typeof(float) && floatValI[i] != 0) || (typeof(T) == typeof(double) && doubleValI[i] != 0))
					{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
						T* ptr = (T*)pVl + (i * ldvl + 1), ptr2 = ptr + ldvl;
						Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr, 2, nn);
						Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
				}
			}
			else if (leftVec is null && rightVec is not null)
			{
				// set eigenvectors to zeros
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, rightVec, ldvr, default, default, default, rightVec, ldvr))
					return false;
				ldvl *= 2; ldvr *= 2;
				// copy eigenvectors
				for (int i = 0; i < nn; i++)
				{
					// copy real parts in both cases
					Storage.StorageApi.PointerStridedCopy(vecR + n * i, 1, (T*)pVr + i * ldvr, 2, nn);
					// check real or complex eigen-pair
					if ((typeof(T) == typeof(float) && floatValI[i] != 0) || (typeof(T) == typeof(double) && doubleValI[i] != 0))
					{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
						T* ptr = (T*)pVr + (i * ldvr + 1), ptr2 = ptr + ldvr;
						Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr, 2, nn);
						Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
				}
			}
			else
			{
				// no copy
			}
			return true;
			/*
			// set eigenvectors to zeros
			if (leftVec is not null)
			{
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, leftVec, ldvl, default, default, default, leftVec, ldvl))
					return false;
			}
			if (rightVec is not null)
			{
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, rightVec, ldvr, default, default, default, rightVec, ldvr))
					return false;
			}
			ldvl *= 2; ldvr *= 2;
			// copy eigenvectors
			for (int i = 0; i < nn; i++)
			{
				// copy real parts in both cases
				if (leftVec is not null)
				{
					Storage.StorageApi.PointerStridedCopy(vecL + n * i, 1, (T*)pVl + i * ldvl, 2, nn);
				}
				if (rightVec is not null)
				{
					Storage.StorageApi.PointerStridedCopy(vecR + n * i, 1, (T*)pVr + i * ldvr, 2, nn);
				}
				// check real or complex eigen-pair
				if (valI[i].IsZero())
				{   // the i-th eigen-pair is real
					// do nothing
				}
				else
				{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
					if (leftVec is not null)
					{
						T* ptr = (T*)pVl + (i * ldvl + 1), ptr2 = ptr + ldvl;
						Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr, 2, nn);
						Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
					if (rightVec is not null)
					{
						T* ptr = (T*)pVr + (i * ldvr + 1), ptr2 = ptr + ldvr;
						Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr, 2, nn);
						Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
				}
			}
			*/
		}

		protected override unsafe bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda)
		{
			if ((Const<T>.IsComplex && typeof(T) != typeof(TComplex)) ||
				(!Const<T>.IsComplex && (!Const<TComplex>.IsComplex || typeof(T) != typeof(TComplex).GenericTypeArguments[0])))
				throw new TypeMismatchException(typeof(T), typeof(TComplex), TypeMismatchException.MismatchReason.IsNotComplexCorrespondence);

			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(leftVec, n, n, ldvl, out var pVl, out _, out _, out int lldvl))
				return false;
			if (!CheckPointer(rightVec, n, n, ldvr, out var pVr, out _, out _, out int lldvr))
				return false;
			if (!CheckPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, T*, T*, T*, int, T*, int, MklLapackInfo> funcR = null;
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, MklLapackInfo> funcC = null;
			if (typeof(T) == typeof(float))
			{
				funcR = &NativeMethods.LAPACKE_sgeev;
			}
			else if (typeof(T) == typeof(double))
			{
				funcR = &NativeMethods.LAPACKE_dgeev;
			}
			else
			{
				switch (Const<T>.DataType)
				{
					case DataType.ComplexSingle:
						funcC = &NativeMethods.LAPACKE_cgeev;
						break;
					case DataType.ComplexDouble:
						funcC = &NativeMethods.LAPACKE_zgeev;
						break;
					default:
						break;
				}
			}
			if (funcR is null && funcC is null)
				return false;
			var (modeL, modeR) = mode.ToLRChar();
			if (funcC is not null)
			{	// complex typed T
				var info = funcC(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, pV, pVl, lldvl, pVr, lldvr);
				SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
				return true;
			}
			// real typed T
			// buffer
			using var buffer = CpuBuffer.Create((2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0) + (modeR == MklVectorModeChar.Vector ? n * n : 0)) * sizeof(T));
			fixed (byte* buf = buffer.Buffer)
			{
				T* valR = (T*)buf, valI = (T*)buf + n, vecL = (T*)buf + 2 * n, vecR = (T*)buf + 2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0);
				// calculate
				var info = funcR(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, valR, valI, vecL, lldvl, vecR, lldvr);
				SolveMethodKind.NonSymmetricEigenvalue.CheckLapackInfo(info);
				// copy
				return this.CopyEigenToComplex(modeL, modeR, n, nn, pV, valR, valI, leftVec, pVl, ldvl, vecL, rightVec, pVr, ldvr, vecR);
			}
		}

		protected override unsafe bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> α, Storage<T> β, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if ((Const<T>.IsComplex && typeof(T) != typeof(TComplex)) ||
				(!Const<T>.IsComplex && (!Const<TComplex>.IsComplex || typeof(T) != typeof(TComplex).GenericTypeArguments[0])))
				throw new TypeMismatchException(typeof(T), typeof(TComplex), TypeMismatchException.MismatchReason.IsNotComplexCorrespondence);

			if (type != GeneralEigenType.Type1)
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(B, n, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(leftVec, n, n, ldvl, out var pVl, out _, out _, out int lldvl))
				return false;
			if (!CheckPointer(rightVec, n, n, ldvr, out var pVr, out _, out _, out int lldvr))
				return false;
			if (!CheckPointer(α, out var pVa, out int nva))
				return false;
			if (!CheckPointer(β, out var pVb, out int nvb))
				return false;
			////if (nva < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(α));
			////if (nvb < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(β));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, IntPtr, int, T*, T*, IntPtr, T*, int, T*, int, MklLapackInfo> funcR = null;
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, IntPtr, int, IntPtr, IntPtr, IntPtr, int, IntPtr, int, MklLapackInfo> funcC = null;
			if (typeof(T) == typeof(float))
			{
				funcR = &NativeMethods.LAPACKE_sggev;
			}
			else if (typeof(T) == typeof(double))
			{
				funcR = &NativeMethods.LAPACKE_dggev;
			}
			else
			{
				if (Const<T>.DataType == DataType.ComplexSingle)
					funcC = &NativeMethods.LAPACKE_cggev;
				else if (Const<T>.DataType == DataType.ComplexDouble)
					funcC = &NativeMethods.LAPACKE_zggev;
			}
			if (funcR is null && funcC is null)
				return false;
			var (modeL, modeR) = mode.ToLRChar();
			if (funcC is not null)
			{   // complex typed T
				var info = funcC(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, pB, lldb, pVa, pVb, pVl, lldvl, pVr, lldvr);
				SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
				return true;
			}
			// real typed T
			// buffer
			using var buffer = CpuBuffer.Create((2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0) + (modeR == MklVectorModeChar.Vector ? n * n : 0)) * sizeof(T));
			fixed (byte* buf = buffer.Buffer)
			{
				T* valR = (T*)buf, valI = (T*)buf + n, vecL = (T*)buf + 2 * n, vecR = (T*)buf + 2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0);
				// calculate
				var info = funcR(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, pB, lldb, valR, valI, pVb, vecL, lldvl, vecR, lldvr);
				SolveMethodKind.NonSymmetricGenearlEigenvalue.CheckLapackInfo(info);
				// copy
				return this.CopyEigenToComplex(modeL, modeR, n, nn, pVa, valR, valI, leftVec, pVl, ldvl, vecL, rightVec, pVr, ldvr, vecR);
			}
		}
		#endregion

		#region other decompose
		protected override unsafe bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct)
		{
			if (storeU == SVDStore.Overwrite && storeV == SVDStore.Overwrite)
				throw new ArgumentException(Resources.Parameter.DuplicateValue, nameof(storeU));

			MklSvdModeChar jobU = storeU.ToChar(), jobV = storeV.ToChar();
			if (jobU == 0 || jobV == 0)
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			int kk = Math.Min(mm, nn);
			if (!CheckPointer(U, storeU == SVDStore.All ? m : kk, m, ldu, out var pU, out int mmU, out _, out int lldu))
				return false;
			if (!CheckPointer(Vct, n, storeV == SVDStore.All ? n : kk, ldvct, out var pV, out _, out int nnV, out int lldv))
				return false;
			if (!CheckPointer(S, out var pS, out int ns))
				return false;
			////if (ns < kk)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(S));

			delegate*<MklMatrixLayout, MklSvdModeChar, MklSvdModeChar, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, byte[], MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.LAPACKE_sgesvd,
				DataType.RealDouble => &NativeMethods.LAPACKE_dgesvd,
				DataType.ComplexSingle => &NativeMethods.LAPACKE_cgesvd,
				DataType.ComplexDouble => &NativeMethods.LAPACKE_zgesvd,
				_ => null,
			};
			if (func is null)
				return false;
			using var buffer = CpuBuffer.Create<T>(Const<T>.IsComplex ? kk : (kk / 2));
			var info = func(MklMatrixLayout.ColMajor, jobU, jobV, mm, nn, pA, llda, pS, pU, lldu, pV, lldv, buffer.Buffer);
			SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe int ApproxIndexOfSingle(ComplexSingle* array, int len, ComplexSingle value)
		{
			for (int i = 0; i < len; i++)
			{
				var diff = array[i] - value;
				float diffMax = Math.Max(Math.Abs(diff.Real), Math.Abs(diff.Imag));
				float max = Math.Max(Math.Abs(array[i].Real), Math.Abs(array[i].Imag));
				if (diffMax / max < 0.00007011098358136203F)
					return i;
			}
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe int ApproxIndexOfDouble(ComplexDouble* array, int len, ComplexDouble value)
		{
			for (int i = 0; i < len; i++)
			{
				var diff = array[i] - value;
				double diffMax = Math.Max(Math.Abs(diff.Real), Math.Abs(diff.Imag));
				double max = Math.Max(Math.Abs(array[i].Real), Math.Abs(array[i].Imag));
				if (diffMax / max < 5.477420592293901E-7)
					return i;
			}
			return -1;
		}

		protected override unsafe bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null)
		{
			actualNumber = 0;
			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(U, n, n, ldu, out var pU, out _, out _, out int lldu))
				return false;
			if (!CheckPointer(orderVal, out var pO, out int orderLen))
				return false;
			if (orderLen >= n)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(orderVal));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklSortModeChar, NativeMethods.SchurSelect2?, int, IntPtr, int, out int, T*, T*, IntPtr, int, MklLapackInfo> funcR = null;
			delegate*<MklMatrixLayout, MklVectorModeChar, MklSortModeChar, NativeMethods.SchurSelect1?, int, IntPtr, int, out int, T*, IntPtr, int, MklLapackInfo> funcC = null;
			if (typeof(T) == typeof(float))
			{
				funcR = &NativeMethods.LAPACKE_sgees;
			}
			else if (typeof(T) == typeof(double))
			{
				funcR = &NativeMethods.LAPACKE_dgees;
			}
			else
			{
				if (Const<T>.DataType == DataType.ComplexSingle)
					funcC = &NativeMethods.LAPACKE_cgees;
				else if (Const<T>.DataType == DataType.ComplexDouble)
					funcC = &NativeMethods.LAPACKE_zgees;
			}
			if (funcR is null && funcC is null)
				return false;

			var mode = jobu.ToChar();
			var sort = orderVal is null ? MklSortModeChar.NoSort : MklSortModeChar.Sort;
			int getEigNumber;
			MklLapackInfo info;
			using var buffer = Const<T>.IsComplex ? CpuBuffer.Create<T>(nn + orderLen) : CpuBuffer.Create<T>((nn + orderLen) * 2);
			fixed (byte* buf = buffer.Buffer)
			{
				if (funcC is not null)
				{
					// calculate
					NativeMethods.SchurSelect1? selector;
					if (orderVal is null)
					{
						selector = null;
					}
					else if (Const<T>.DataType == DataType.ComplexSingle)
					{
						// covert to correct type
						ComplexSingle* selectValues = (ComplexSingle*)buf + n;
						CSharp.LinearAlgebra.DenseApi.PointWiseCast(orderVal, new ManagedPureStorage<ComplexSingle>(selectValues, orderLen));
						// local function
						int Selector(void* pVal)
						{
							ComplexSingle val = *(ComplexSingle*)pVal;
							return ApproxIndexOfSingle(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					else // complex double
					{
						// covert to correct type
						ComplexDouble* selectValues = (ComplexDouble*)buf + n;
						Unsafe.CopyBlockUnaligned(selectValues, (void*)pO, (uint)(n * sizeof(ComplexDouble)));
						// local function
						int Selector(void* pVal)
						{
							ComplexDouble val = *(ComplexDouble*)pVal;
							return ApproxIndexOfDouble(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					info = funcC(MklMatrixLayout.ColMajor, mode, sort, selector, nn, pA, llda, out getEigNumber, (T*)buf, pU, lldu);
				}
				else
				{
					NativeMethods.SchurSelect2? selector;
					if (orderVal is null)
					{
						selector = null;
					}
					else if (typeof(T) == typeof(float))
					{
						// covert to correct type
						ComplexSingle* selectValues = (ComplexSingle*)buf + n;
						CSharp.LinearAlgebra.DenseApi.PointWiseCast(orderVal, new ManagedPureStorage<ComplexSingle>(selectValues, orderLen));
						// local function
						int Selector(void* pValR, void* pValI)
						{
							ComplexSingle val = new(*(float*)pValR, *(float*)pValI);
							return ApproxIndexOfSingle(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					else // double
					{
						// covert to correct type
						ComplexDouble* selectValues = (ComplexDouble*)buf + n;
						Unsafe.CopyBlockUnaligned(selectValues, (void*)pO, (uint)(n * sizeof(ComplexDouble)));
						// local function
						int Selector(void* pValR, void* pValI)
						{
							ComplexDouble val = new(*(double*)pValR, *(double*)pValI);
							return ApproxIndexOfDouble(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					info = funcR(MklMatrixLayout.ColMajor, mode, sort, selector, nn, pA, llda, out getEigNumber, (T*)buf, (T*)buf + n, pU, lldu);
				}
			}
			SolveMethodKind.Schur.CheckLapackInfo(info);
			actualNumber = getEigNumber;
			return true;
		}
		#endregion
		#endregion
	}
}
