using System;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
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
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckPointer<T>(Storage<T>? s, out IntPtr ptr, out int length, int stride = 1) where T : unmanaged
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
		private bool CheckPointerLong<T>(Storage<T> s, out IntPtr ptr, out long length, int stride = 1) where T : unmanaged
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
		private bool CheckPointer<T>(Storage<T>? A, long rows, long cols, long ld, out IntPtr ptr, out int r, out int c, out int l) where T : unmanaged
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
		private bool CheckPointer<T>(Storage<T>? A, MatrixOperation op, long rowsAfterOp, long colsAfterOp, long ld, out MklBlasOperation opMkl, out IntPtr ptr, out int r, out int c, out int l) where T : unmanaged
		{
			ptr = default; r = c = l = 1; opMkl = op.Simplify<T>().ToMkl();
			if (A is null) // specific null input
				return true;
			if (opMkl == MklBlasOperation.ConjugateAlone)
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
		private bool CheckPointerLong<T>(Storage<T>? A, long cols, long ld, out IntPtr ptr) where T : unmanaged
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool AllQRSupport<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work) => Supported(A.LocationDescription) && Supported(B.LocationDescription) && (work is null || Supported(work.LocationDescription));
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
		protected internal unsafe bool HorizontalAbsoluteValueArgMax<T>(Storage<T> x, int strideX, out long index) where T : unmanaged
		{
			index = -1;
			if (!Const<T>.IsComplex || !Const<T>.DataType.CheckBaseSupport())
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
		protected internal unsafe bool HorizontalAbsoluteValueArgMin<T>(Storage<T> x, int strideX, out long index) where T : unmanaged
		{
			index = -1;
			if (!Const<T>.IsComplex || !Const<T>.DataType.CheckBaseSupport())
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
		protected internal unsafe bool HorizontalAbsoluteSum<T>(Storage<T> x, int strideX, out double sum) where T : unmanaged
		{
			sum = 0;
			if (!Const<T>.IsComplex || !Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (Const<T>.DataType == DataType.ComplexSingle)
			{
				sum = NativeMethods.cblas_scasum(n, px, strideX);
			}
			else
			{
				sum = NativeMethods.cblas_dzasum(n, px, strideX);
			}
			return true;
		}

		protected override unsafe bool AbsoluteValueArgMax_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (!Const<T>.IsPreDefined || (Const<T>.DataTypeClass == DataTypeClassification.FloatPoint_IEEE754 && Const<T>.DataType.Bytes() < sizeof(float)))
				return false; // half float is not supported
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cblas_isamax,
				DataType.RealDouble => &NativeMethods.cblas_idamax,
				_ => null,
			};
			index = func(n, px, strideX) - 1;
			return true;
		}

		protected override unsafe bool AbsoluteValueArgMin_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false; // half float is not supported
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cblas_isamin,
				DataType.RealDouble => &NativeMethods.cblas_idamin,
				_ => null,
			};
			index = func(n, px, strideX) - 1;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe bool AbsSumOrNorm<T, Sum>(Storage<T> x, int strideX, out double sum) where T : unmanaged
		{
			bool doSum = typeof(Sum) == typeof(bool);
			sum = 0;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (!Const<T>.IsPreDefinedNoHalf)
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
			else
			{
				sum = funcD(n, px, strideX);
			}
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
			if (!Const<T>.IsPreDefined)
				return false;
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
					func = null;
					break;
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
			if (!Const<T>.IsPreDefined)
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
					func = null;
					break;
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
			if (!Const<T>.IsPreDefined)
				return false;
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
					func = null;
					break;
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
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			var opMkl = op.ToMkl();
			if (opMkl == MklBlasOperation.ConjugateAlone)
				return false;
			////if (nx < (opMkl == MklBlasOperation.None ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < (opMkl == MklBlasOperation.None ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<MklBlasOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_sgemv,
					DataType.RealDouble => &NativeMethods.cblas_dgemv,
					DataType.ComplexSingle => &NativeMethods.cblas_cgemv,
					DataType.ComplexDouble => &NativeMethods.cblas_zgemv,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_sgemv_v2,
					DataType.RealDouble => &NativeMethods.cblas_dgemv_v2,
					DataType.ComplexSingle => &NativeMethods.cblas_cgemv_v2,
					DataType.ComplexDouble => &NativeMethods.cblas_zgemv_v2,
					_ => null,
				};
			}
			func(opMkl, mm, nn, &α, pA, llda, px, strideX, &β, py, strideY);
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

			delegate*<MatrixFillMode, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssymv,
					DataType.RealDouble => &NativeMethods.cblas_dsymv,
					DataType.ComplexSingle => hermA ? &NativeMethods.cblas_chemv : &NativeMethods.cblas_csymv,
					DataType.ComplexDouble => hermA ? &NativeMethods.cblas_zhemv : &NativeMethods.cblas_zsymv,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssymv_v2,
					DataType.RealDouble => &NativeMethods.cblas_dsymv_v2,
					DataType.ComplexSingle => hermA ? &NativeMethods.cblas_chemv_v2 : &NativeMethods.cblas_csymv_v2,
					DataType.ComplexDouble => hermA ? &NativeMethods.cblas_zhemv_v2 : &NativeMethods.cblas_zsymv_v2,
					_ => null,
				};
			}
			func(fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, nn, &α, pA, llda, px, strideX, &β, py, strideY);
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

			delegate*<int, int, T*, IntPtr, int, IntPtr, int, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_sger,
					DataType.RealDouble => &NativeMethods.cblas_dger,
					DataType.ComplexSingle => conjY ? &NativeMethods.cblas_cgerc : &NativeMethods.cblas_cgerc,
					DataType.ComplexDouble => conjY ? &NativeMethods.cblas_zgerc : &NativeMethods.cblas_zgerc,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_sger_v2,
					DataType.RealDouble => &NativeMethods.cblas_dger_v2,
					DataType.ComplexSingle => conjY ? &NativeMethods.cblas_cgerc_v2 : &NativeMethods.cblas_cgerc_v2,
					DataType.ComplexDouble => conjY ? &NativeMethods.cblas_zgerc_v2 : &NativeMethods.cblas_zgerc_v2,
					_ => null,
				};
			}
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			func(mm, nn, &α, px, strideX, py, strideY, pA, llda);
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
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			delegate*<MatrixFillMode, int, T*, IntPtr, int, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyr,
					DataType.RealDouble => &NativeMethods.cblas_dsyr,
					DataType.ComplexSingle => conjX ? &NativeMethods.cblas_cher : &NativeMethods.cblas_csyr,
					DataType.ComplexDouble => conjX ? &NativeMethods.cblas_zher : &NativeMethods.cblas_zsyr,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyr_v2,
					DataType.RealDouble => &NativeMethods.cblas_dsyr_v2,
					DataType.ComplexSingle => conjX ? &NativeMethods.cblas_cher_v2 : &NativeMethods.cblas_csyr_v2,
					DataType.ComplexDouble => conjX ? &NativeMethods.cblas_zher_v2 : &NativeMethods.cblas_zsyr_v2,
					_ => null,
				};
			}
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			func(fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, nn, &α, px, strideX, pA, llda);
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
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<MatrixFillMode, int, T*, IntPtr, int, IntPtr, int, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyr2,
					DataType.RealDouble => &NativeMethods.cblas_ssyr2,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cblas_cher2 : &NativeMethods.cblas_csyr2,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cblas_zher2 : &NativeMethods.cblas_zsyr2,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyr2_v2,
					DataType.RealDouble => &NativeMethods.cblas_ssyr2_v2,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cblas_cher2_v2 : &NativeMethods.cblas_csyr2_v2,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cblas_zher2_v2 : &NativeMethods.cblas_zsyr2_v2,
					_ => null,
				};
			}
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			func(fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, nn, &α, px, strideX, py, strideY, pA, llda);
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
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			delegate*<MatrixFillMode, MklBlasOperation, DiagType, int, IntPtr, int, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_strmv,
					DataType.RealDouble => &NativeMethods.cblas_dtrmv,
					DataType.ComplexSingle => &NativeMethods.cblas_ctrmv,
					DataType.ComplexDouble => &NativeMethods.cblas_ztrmv,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_strmv_v2,
					DataType.RealDouble => &NativeMethods.cblas_dtrmv_v2,
					DataType.ComplexSingle => &NativeMethods.cblas_ctrmv_v2,
					DataType.ComplexDouble => &NativeMethods.cblas_ztrmv_v2,
					_ => null,
				};
			}
			var opMkl = op.ToMkl();
			if (opMkl == MklBlasOperation.ConjugateAlone)
				return false;
			func(fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opMkl, unitDiag ? DiagType.Unit : DiagType.NonUnit, nn, pA, llda, px, strideX);
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

			delegate*<SideMode, int, int, IntPtr, int, IntPtr, int, IntPtr, int> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cblas_sdgmm,
				DataType.RealDouble => &NativeMethods.cblas_ddgmm,
				DataType.ComplexSingle => &NativeMethods.cblas_cdgmm,
				DataType.ComplexDouble => &NativeMethods.cblas_zdgmm,
				_ => null,
			};
			func(leftA ? SideMode.Right : SideMode.Left, mm, nn, pA, llda, px, strideX, pC, lldc);
			return true;
		}
		#endregion


		#region BLAS level 3
		protected override unsafe bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, m, m, lda, out var pA, out int mm, out _, out int llda))
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out _, out int nn, out int lldb))
				return false;
			if (α.IsZero()) // result is 0
				return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, B, ldb, default, null, 0, B, ldb);

			delegate*<SideMode, MatrixFillMode, MklBlasOperation, DiagType, int, int, T*, IntPtr, int, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_strsm,
					DataType.RealDouble => &NativeMethods.cblas_dtrsm,
					DataType.ComplexSingle => &NativeMethods.cblas_ctrsm,
					DataType.ComplexDouble => &NativeMethods.cblas_ztrsm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_strsm_v2,
					DataType.RealDouble => &NativeMethods.cblas_dtrsm_v2,
					DataType.ComplexSingle => &NativeMethods.cblas_ctrsm_v2,
					DataType.ComplexDouble => &NativeMethods.cblas_ztrsm_v2,
					_ => null,
				};
			}
			var opMkl = op.ToMkl();
			if (opMkl == MklBlasOperation.ConjugateAlone)
				return false;
			func(leftA ? SideMode.Right : SideMode.Left, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opMkl, unitDiag ? DiagType.Unit : DiagType.NonUnit, mm, nn, &α, pA, llda, pB, lldb);
			return true;
		}

		protected override unsafe bool TriangularMatrixMultiply_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			var opMkl = op.ToMkl();
			if (opMkl == MklBlasOperation.ConjugateAlone)
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out int mm, out int nn, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
				return false;
			if (!CheckPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
				return false;
			if (α.IsZero()) // result if 0
				this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, null, 0, C, ldc);

			delegate*<SideMode, MatrixFillMode, MklBlasOperation, DiagType, int, int, T*, IntPtr, int, IntPtr, int, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_strmm,
					DataType.RealDouble => &NativeMethods.cblas_dtrmm,
					DataType.ComplexSingle => &NativeMethods.cblas_ctrmm,
					DataType.ComplexDouble => &NativeMethods.cblas_ztrmm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_strmm_v2,
					DataType.RealDouble => &NativeMethods.cblas_dtrmm_v2,
					DataType.ComplexSingle => &NativeMethods.cblas_ctrmm_v2,
					DataType.ComplexDouble => &NativeMethods.cblas_ztrmm_v2,
					_ => null,
				};
			}
			func(leftA ? SideMode.Right : SideMode.Left, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opMkl, unitDiag ? DiagType.Unit : DiagType.NonUnit, mm, nn, &α, pA, llda, pB, lldb, pC, lldc);
			return true;
		}

		protected override unsafe bool GeneralMatricesAdd_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, opA, m, n, lda, out var opcA, out var pA, out _, out _, out int llda))
				return false;
			if (!CheckPointer(B, opB, m, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;

			delegate*<MklBlasOperation, MklBlasOperation, int, int, T*, IntPtr, int, T*, IntPtr, int, IntPtr, int> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cblas_sgeam,
				DataType.RealDouble => &NativeMethods.cblas_dgeam,
				DataType.ComplexSingle => &NativeMethods.cblas_cgeam,
				DataType.ComplexDouble => &NativeMethods.cblas_zgeam,
				_ => null,
			};
			func(opcA, opcB, mm, nn, &α, pA, llda, &β, pB, lldb, pC, lldc);
			return true;
		}

		protected override unsafe bool GeneralMatricesMultiply_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckEx2Support())
				return false;
			if (!CheckPointer(A, opA, m, k, lda, out var opcA, out var pA, out _, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, opB, k, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;

			delegate*<MklBlasOperation, MklBlasOperation, int, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_sgemm,
					DataType.RealDouble => &NativeMethods.cblas_dgemm,
					DataType.ComplexSingle => this.ComplexGemmUseGemm3m ? &NativeMethods.cblas_cgemm3m : &NativeMethods.cblas_cgemm,
					DataType.ComplexDouble => this.ComplexGemmUseGemm3m ? &NativeMethods.cblas_zgemm3m : &NativeMethods.cblas_zgemm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_sgemm_v2,
					DataType.RealDouble => &NativeMethods.cblas_dgemm_v2,
					DataType.ComplexSingle => this.ComplexGemmUseGemm3m ? &NativeMethods.cblas_cgemm3m : &NativeMethods.cblas_cgemm_v2,
					DataType.ComplexDouble => this.ComplexGemmUseGemm3m ? &NativeMethods.cblas_zgemm3m : &NativeMethods.cblas_zgemm_v2,
					_ => null,
				};
			}
			if (func is not null)
			{
				func(opcA, opcB, mm, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc);
			}
			else
			{
				if (Const<T>.DataType == DataType.ComplexHalf || Const<T>.DataType == BrainFloatConst.ComplexBrainFloat16)
					return false;
				var type = Const<T>.DataType.ToMklDataType();
				ComputeType cType = type switch
				{
					MklDataType.RealFloat32 or MklDataType.ComplexFloat32 => ComputeType.Compute32F,
					MklDataType.RealFloat64 or MklDataType.ComplexFloat64 => ComputeType.Compute64F,
					MklDataType.RealFloat16 => ComputeType.Compute16F,
					MklDataType.RealBrainFloat16 => ComputeType.Compute32F,
					_ => default,
				};
				NativeMethods.cublasGemmEx(this.cublasHandle, opcA, opcB, mm, nn, kk, &α, pA, type, llda, pB, type, lldb, &β, pC, type, lldc, cType, GemmAlgorithm.Default);
			}
			return true;
		}

		protected override unsafe bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;

			delegate*<SideMode, MatrixFillMode, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssymm,
					DataType.RealDouble => &NativeMethods.cblas_dsymm,
					DataType.ComplexSingle => hermA ? &NativeMethods.cblas_chemm : &NativeMethods.cblas_csymm,
					DataType.ComplexDouble => hermA ? &NativeMethods.cblas_zhemm : &NativeMethods.cblas_zsymm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssymm_v2,
					DataType.RealDouble => &NativeMethods.cblas_dsymm_v2,
					DataType.ComplexSingle => hermA ? &NativeMethods.cblas_chemm_v2 : &NativeMethods.cblas_csymm_v2,
					DataType.ComplexDouble => hermA ? &NativeMethods.cblas_zhemm_v2 : &NativeMethods.cblas_zsymm_v2,
					_ => null,
				};
			}
			func(leftA ? SideMode.Left : SideMode.Right, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, op, n, k, lda, out var opcA, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			delegate*<MatrixFillMode, MklBlasOperation, int, int, T*, IntPtr, int, T*, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyrk,
					DataType.RealDouble => &NativeMethods.cblas_dsyrk,
					DataType.ComplexSingle => conjA ? &NativeMethods.cblas_cherk : &NativeMethods.cblas_csyrk,
					DataType.ComplexDouble => conjA ? &NativeMethods.cblas_zherk : &NativeMethods.cblas_zsyrk,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyrk_v2,
					DataType.RealDouble => &NativeMethods.cblas_dsyrk_v2,
					DataType.ComplexSingle => conjA ? &NativeMethods.cblas_cherk_v2 : &NativeMethods.cblas_csyrk_v2,
					DataType.ComplexDouble => conjA ? &NativeMethods.cblas_zherk_v2 : &NativeMethods.cblas_zsyrk_v2,
					_ => null,
				};
			}
			func(fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opcA, nn, kk, &α, pA, llda, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankTwoKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, op, n, k, lda, out var opMkl, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			delegate*<MatrixFillMode, MklBlasOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyr2k,
					DataType.RealDouble => &NativeMethods.cblas_dsyr2k,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cblas_cher2k : &NativeMethods.cblas_csyr2k,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cblas_zher2k : &NativeMethods.cblas_zsyr2k,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyr2k_v2,
					DataType.RealDouble => &NativeMethods.cblas_dsyr2k_v2,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cblas_cher2k_v2 : &NativeMethods.cblas_csyr2k_v2,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cblas_zher2k_v2 : &NativeMethods.cblas_zsyr2k_v2,
					_ => null,
				};
			}
			func(fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opMkl, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankKUpdateVariant_<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, op, n, k, lda, out var opMkl, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			delegate*<MatrixFillMode, MklBlasOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int> func;
			if (this.Mkl110OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyrkx,
					DataType.RealDouble => &NativeMethods.cblas_dsyrkx,
					DataType.ComplexSingle => conjB ? &NativeMethods.cblas_cherkx : &NativeMethods.cblas_csyrkx,
					DataType.ComplexDouble => conjB ? &NativeMethods.cblas_zherkx : &NativeMethods.cblas_zsyrkx,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cblas_ssyrkx_v2,
					DataType.RealDouble => &NativeMethods.cblas_dsyrkx_v2,
					DataType.ComplexSingle => conjB ? &NativeMethods.cblas_cherkx_v2 : &NativeMethods.cblas_csyrkx_v2,
					DataType.ComplexDouble => conjB ? &NativeMethods.cblas_zherkx_v2 : &NativeMethods.cblas_zsyrkx_v2,
					_ => null,
				};
			}
			func(fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opMkl, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
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
		#region LU
		protected override unsafe bool LUDecomposition_<T, TInd>(long n, Storage<T> A, long lda, Storage<TInd> pivot)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if ((typeof(TInd) == typeof(long) || typeof(TInd) == typeof(ulong)) && !this.Mkl111OrAbove)
				return false;
			if (typeof(TInd) != typeof(long) && typeof(TInd) == typeof(ulong) && typeof(TInd) != typeof(int) && typeof(TInd) != typeof(uint))
				return false;

			if ((typeof(TInd) == typeof(long) || typeof(TInd) == typeof(ulong)) && this.Mkl111OrAbove)
			{
				if (!CheckPointerLong(A, n, lda, out var pA))
					return false;
				if (!CheckPointerLong(pivot, out var pP, out var np))
					return false;
				////if (np < n)
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(pivot));

				var type = Const<T>.DataType.ToMklDataType();
				NativeMethods.cusolverDnXgetrf_bufferSize(this.cusolverHandle, IntPtr.Zero, n, n, type, pA, lda, type, out var workDevice, out var workHost);
				using var buffer = MklBuffer.Create(workDevice, workHost);
				NativeMethods.cusolverDnXgetrf(this.cusolverHandle, IntPtr.Zero, n, n, type, pA, lda, pP, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo);
				SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			else
			{   // use legacy
				if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
					return false;
				if (!CheckPointer(pivot, out var pP, out int np))
					return false;
				////if (np < nn)
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(pivot));

				delegate*<int, int, IntPtr, int, out int, MklSolverStatus> bufFunc;
				delegate*<int, int, IntPtr, int, IntPtr, IntPtr, IntPtr, MklSolverStatus> calFunc;
				bufFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSgetrf_bufferSize,
					DataType.RealDouble => &NativeMethods.cusolverDnDgetrf_bufferSize,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCgetrf_bufferSize,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZgetrf_bufferSize,
					_ => null,
				};
				calFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSgetrf,
					DataType.RealDouble => &NativeMethods.cusolverDnDgetrf,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCgetrf,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZgetrf,
					_ => null,
				};
				bufFunc(this.cusolverHandle, nn, nn, pA, llda, out var work);
				using var buffer = MklBuffer.Create<T>(work);
				calFunc(this.cusolverHandle, nn, nn, pA, llda, buffer.DeviceBuffer, pP, buffer.ExtraDeviceInfo);
				SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			return true;
		}

		protected override unsafe bool LinearSolveByLU_<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<TInd> pivot, Storage<T> B, long ldb)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if ((typeof(TInd) == typeof(long) || typeof(TInd) == typeof(ulong)) && !this.Mkl111OrAbove)
				return false;
			if (typeof(TInd) != typeof(long) && typeof(TInd) == typeof(ulong) && typeof(TInd) != typeof(int) && typeof(TInd) != typeof(uint))
				return false;
			var opMkl = op.ToMkl();
			if (opMkl == MklBlasOperation.ConjugateAlone)
				return false;

			using var buffer = MklBuffer.Create(0, extraDeviceInfo: true);
			if ((typeof(TInd) == typeof(long) || typeof(TInd) == typeof(ulong)) && this.Mkl111OrAbove)
			{
				if (!CheckPointerLong(A, n, lda, out var pA))
					return false;
				if (!CheckPointerLong(B, nrhs, ldb, out var pB))
					return false;
				if (!CheckPointerLong(pivot, out var pP, out var np))
					return false;
				////if (np < n)
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(pivot));

				var type = Const<T>.DataType.ToMklDataType();
				NativeMethods.cusolverDnXgetrs(this.cusolverHandle, IntPtr.Zero, opMkl, n, nrhs, type, pA, lda, pP, type, pB, ldb, buffer.ExtraDeviceInfo);
			}
			else
			{   // use legacy
				if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
					return false;
				if (!CheckPointer(B, n, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
					return false;
				if (!CheckPointer(pivot, out var pP, out int np))
					return false;
				////if (np < nn)
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(pivot));

				delegate*<MklBlasOperation, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calFunc;
				calFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSgetrs,
					DataType.RealDouble => &NativeMethods.cusolverDnDgetrs,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCgetrs,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZgetrs,
					_ => null,
				};
				calFunc(this.cusolverHandle, opMkl, nn, nnrhs, pA, llda, pP, pB, lldb, buffer.ExtraDeviceInfo);
			}
			SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			return true;
		}
		#endregion

		#region QR
		protected override unsafe bool ImplicitQR_<T>(long m, long n, Storage<T> A, long lda, Storage<T> τ)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (this.Mkl111OrAbove)
			{
				if (!CheckPointerLong(A, n, lda, out var pA))
					return false;
				if (!CheckPointerLong(τ, out var pT, out long nt))
					return false;
				////if (nt < Math.Min(m, n))
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(τ));

				var type = Const<T>.DataType.ToMklDataType();
				NativeMethods.cusolverDnXgeqrf_bufferSize(this.cusolverHandle, IntPtr.Zero, m, n, type, pA, lda, type, pT, type, out var workDevice, out var workHost);
				using var buffer = MklBuffer.Create(workDevice, workHost);
				NativeMethods.cusolverDnXgeqrf(this.cusolverHandle, IntPtr.Zero, m, n, type, pA, lda, type, pT, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo);
				SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			else
			{
				if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
					return false;
				if (!CheckPointer(τ, out var pT, out int nt))
					return false;
				////if (nt < Math.Min(mm, nn))
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(τ));

				delegate*<int, int, IntPtr, int, out int, MklSolverStatus> bufFunc;
				delegate*<int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calFunc;
				bufFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSgeqrf_bufferSize,
					DataType.RealDouble => &NativeMethods.cusolverDnDgeqrf_bufferSize,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCgeqrf_bufferSize,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZgeqrf_bufferSize,
					_ => null,
				};
				calFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSgeqrf,
					DataType.RealDouble => &NativeMethods.cusolverDnDgeqrf,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCgeqrf,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZgeqrf,
					_ => null,
				};
				bufFunc(this.cusolverHandle, mm, nn, pA, llda, out var work);
				using var buffer = MklBuffer.Create<T>(work);
				calFunc(this.cusolverHandle, mm, nn, pA, llda, pT, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo);
				SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			return true;
		}

		protected override unsafe bool ImplicitQRFormQ_<T>(long m, long n, long k, Storage<T> Q, long ldq, Storage<T> τ)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(Q, m, n, ldq, out var pQ, out int mm, out int nn, out int lldq))
				return false;
			if (!CheckPointer(τ, out var pT, out int nt))
				return false;
			////if (k > n || n > m)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(n));
			////if (nt < k)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(τ));
			int kk = (int)k;

			delegate*<int, int, int, IntPtr, int, IntPtr, out int, MklSolverStatus> bufFunc;
			delegate*<int, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calFunc;
			bufFunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cusolverDnSorgqr_bufferSize,
				DataType.RealDouble => &NativeMethods.cusolverDnDorgqr_bufferSize,
				DataType.ComplexSingle => &NativeMethods.cusolverDnCungqr_bufferSize,
				DataType.ComplexDouble => &NativeMethods.cusolverDnZungqr_bufferSize,
				_ => null,
			};
			calFunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cusolverDnSorgqr,
				DataType.RealDouble => &NativeMethods.cusolverDnDorgqr,
				DataType.ComplexSingle => &NativeMethods.cusolverDnCungqr,
				DataType.ComplexDouble => &NativeMethods.cusolverDnZungqr,
				_ => null,
			};
			bufFunc(this.cusolverHandle, mm, nn, kk, pQ, lldq, pT, out var work);
			using var buffer = MklBuffer.Create<T>(work);
			calFunc(this.cusolverHandle, mm, nn, kk, pQ, lldq, pT, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo);
			SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			return true;
		}

		protected override unsafe bool ImplicitQRMultiplyQ_<T>(bool leftQ, MatrixOperation op, long m, long n, long k, Storage<T> A, long lda, Storage<T> τ, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, op, k, m, lda, out var opMkl, out var pA, out int kk, out int mm, out int llda))
				return false;
			if (!CheckPointer(C, m, n, lda, out var pC, out _, out int nn, out int lldc))
				return false;
			if (!CheckPointer(τ, out var pT, out int nt))
				return false;
			////if (nt < kk)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(τ));

			delegate*<SideMode, MklBlasOperation, int, int, int, IntPtr, int, IntPtr, IntPtr, int, out int, MklSolverStatus> bufFunc;
			delegate*<SideMode, MklBlasOperation, int, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, IntPtr, MklSolverStatus> calFunc;
			bufFunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cusolverDnSormqr_bufferSize,
				DataType.RealDouble => &NativeMethods.cusolverDnDormqr_bufferSize,
				DataType.ComplexSingle => &NativeMethods.cusolverDnCunmqr_bufferSize,
				DataType.ComplexDouble => &NativeMethods.cusolverDnZunmqr_bufferSize,
				_ => null,
			};
			calFunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cusolverDnSormqr,
				DataType.RealDouble => &NativeMethods.cusolverDnDormqr,
				DataType.ComplexSingle => &NativeMethods.cusolverDnCunmqr,
				DataType.ComplexDouble => &NativeMethods.cusolverDnZunmqr,
				_ => null,
			};
			bufFunc(this.cusolverHandle, leftQ ? SideMode.Left : SideMode.Right, opMkl, mm, nn, kk, pA, llda, pT, pC, lldc, out var work);
			using var buffer = MklBuffer.Create<T>(work);
			calFunc(this.cusolverHandle, leftQ ? SideMode.Left : SideMode.Right, opMkl, mm, nn, kk, pA, llda, pT, pC, lldc, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo);
			SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			return true;
		}

		// modify these methods to use a unified buffer for (maybe) better performance
		protected override unsafe bool LeastSquareSolve_<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work = null)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			if (!CheckPointer(B, m, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
				return false;
			if (!CheckPointer(work, out var pW, out int nw))
				return false;
			////if (nw > 0 && nw < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(work));

			IntPtr tau;
			if (pW == default)
				Storage.NativeMethods.cudaMalloc(out tau, n * sizeof(T));
			else
				tau = pW;
			try
			{
				delegate*<int, int, IntPtr, int, out int, MklSolverStatus> bufQRFunc = null;
				delegate*<int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calQRFunc = null;
				delegate*<SideMode, MklBlasOperation, int, int, int, IntPtr, int, IntPtr, IntPtr, int, out int, MklSolverStatus> bufQmulFunc = null;
				delegate*<SideMode, MklBlasOperation, int, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, IntPtr, MklSolverStatus> calQmulFunc = null;
				delegate*<SideMode, MatrixFillMode, MklBlasOperation, DiagType, int, int, T*, IntPtr, int, IntPtr, int> triSolveFunc = null;
				MklBlasOperation op = MklBlasOperation.Transpose;
				switch (Const<T>.DataType)
				{
					case DataType.RealSingle:
						bufQRFunc = &NativeMethods.cusolverDnSgeqrf_bufferSize;
						bufQmulFunc = &NativeMethods.cusolverDnSormqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnSgeqrf;
						calQmulFunc = &NativeMethods.cusolverDnSormqr;
						triSolveFunc = this.Mkl110OrAbove ? &NativeMethods.cblas_strsm : &NativeMethods.cblas_strsm_v2;
						break;
					case DataType.RealDouble:
						bufQRFunc = &NativeMethods.cusolverDnDgeqrf_bufferSize;
						bufQmulFunc = &NativeMethods.cusolverDnDormqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnDgeqrf;
						calQmulFunc = &NativeMethods.cusolverDnDormqr;
						triSolveFunc = this.Mkl110OrAbove ? &NativeMethods.cblas_dtrsm : &NativeMethods.cblas_dtrsm_v2;
						break;
					case DataType.ComplexSingle:
						bufQRFunc = &NativeMethods.cusolverDnCgeqrf_bufferSize;
						bufQmulFunc = &NativeMethods.cusolverDnCunmqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnCgeqrf;
						calQmulFunc = &NativeMethods.cusolverDnCunmqr;
						triSolveFunc = this.Mkl110OrAbove ? &NativeMethods.cblas_ctrsm : &NativeMethods.cblas_ctrsm_v2;
						op = MklBlasOperation.ConjugateTranspose;
						break;
					case DataType.ComplexDouble:
						bufQRFunc = &NativeMethods.cusolverDnZgeqrf_bufferSize;
						bufQmulFunc = &NativeMethods.cusolverDnZunmqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnZgeqrf;
						calQmulFunc = &NativeMethods.cusolverDnZunmqr;
						triSolveFunc = this.Mkl110OrAbove ? &NativeMethods.cblas_ztrsm : &NativeMethods.cblas_ztrsm_v2;
						op = MklBlasOperation.ConjugateTranspose;
						break;
					default:
						break;
				}
				// get buffer
				bufQRFunc(this.cusolverHandle, nn, nn, pA, llda, out var workSizeT1);
				bufQmulFunc(this.cusolverHandle, SideMode.Left, MklBlasOperation.None, nn, nnrhs, nn, pA, llda, tau, pB, lldb, out var workSizeT2);
				using var buffer = MklBuffer.Create<T>(Math.Max(workSizeT1, workSizeT2));
				// implicit QR
				calQRFunc(this.cusolverHandle, mm, nn, pA, llda, tau, buffer.DeviceBuffer, workSizeT1, buffer.ExtraDeviceInfo);
				SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
				// implicit Q^H * B
				calQmulFunc(this.cusolverHandle, SideMode.Left, op, mm, nnrhs, nn, pA, llda, tau, pB, lldb, buffer.DeviceBuffer, workSizeT2, buffer.ExtraDeviceInfo);
				SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
				// triangular solve R * X = Q^H * B
				T one = Const<T>.One;
				triSolveFunc(this.cublasHandle, SideMode.Left, MatrixFillMode.Upper, MklBlasOperation.None, DiagType.NonUnit, nn, nnrhs, &one, pA, llda, pB, lldb);
				return true;
			}
			finally
			{
				if (tau != pW)
					Storage.NativeMethods.cudaFree(tau);
			}
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

			IntPtr tau;
			if (pW == default)
				Storage.NativeMethods.cudaMalloc(out tau, n * sizeof(T));
			else
				tau = pW;
			try
			{
				delegate*<int, int, IntPtr, int, out int, MklSolverStatus> bufQRFunc = null;
				delegate*<int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calQRFunc = null;
				delegate*<int, int, int, IntPtr, int, IntPtr, out int, MklSolverStatus> bufGetQFunc = null;
				delegate*<int, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calGetQFunc = null;
				switch (Const<T>.DataType)
				{
					case DataType.RealSingle:
						bufQRFunc = &NativeMethods.cusolverDnSgeqrf_bufferSize;
						bufGetQFunc = &NativeMethods.cusolverDnSorgqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnSgeqrf;
						calGetQFunc = &NativeMethods.cusolverDnSorgqr;
						break;
					case DataType.RealDouble:
						bufQRFunc = &NativeMethods.cusolverDnDgeqrf_bufferSize;
						bufGetQFunc = &NativeMethods.cusolverDnDorgqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnDgeqrf;
						calGetQFunc = &NativeMethods.cusolverDnDorgqr;
						break;
					case DataType.ComplexSingle:
						bufQRFunc = &NativeMethods.cusolverDnCgeqrf_bufferSize;
						bufGetQFunc = &NativeMethods.cusolverDnCungqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnCgeqrf;
						calGetQFunc = &NativeMethods.cusolverDnCungqr;
						break;
					case DataType.ComplexDouble:
						bufQRFunc = &NativeMethods.cusolverDnZgeqrf_bufferSize;
						bufGetQFunc = &NativeMethods.cusolverDnZungqr_bufferSize;
						calQRFunc = &NativeMethods.cusolverDnZgeqrf;
						calGetQFunc = &NativeMethods.cusolverDnZungqr;
						break;
					default:
						break;
				}
				// get buffer
				bufQRFunc(this.cusolverHandle, nn, nn, pA, llda, out var workSizeT1);
				bufGetQFunc(this.cusolverHandle, mm, nnQ, kk, pQ, lldq, tau, out var workSizeT2);
				using var buffer = MklBuffer.Create<T>(Math.Max(workSizeT1, workSizeT2));
				// implicit QR
				calQRFunc(this.cusolverHandle, mm, nn, pA, llda, tau, buffer.DeviceBuffer, workSizeT1, buffer.ExtraDeviceInfo);
				SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
				// copy A to Q
				Storage.NativeMethods.cudaMemcpy2D(pQ, ldq, pA, lda, m, Math.Min(colsQ, n), Storage.MemoryCopyKind.DeviceToDevice);
				// form Q
				calGetQFunc(this.cusolverHandle, mm, nnQ, kk, pQ, lldq, tau, buffer.DeviceBuffer, workSizeT2, buffer.ExtraDeviceInfo);
				SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
				return true;
			}
			finally
			{
				if (tau != pW)
					Storage.NativeMethods.cudaFree(tau);
			}
		}
		#endregion

		#region eigen
		protected override unsafe bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (mode != SolveVectorMode.NoVector)
				mode = SolveVectorMode.Vector;

			if (this.Mkl111OrAbove)
			{
				if (!CheckPointerLong(A, n, lda, out var pA))
					return false;
				if (!CheckPointerLong(valOut, out var pV, out long nv))
					return false;
				////if (nv < n)
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

				var type = Const<T>.DataType.ToMklDataType();
				NativeMethods.cusolverDnXsyevd_bufferSize(this.cusolverHandle, IntPtr.Zero, mode, MatrixFillMode.Upper, n, type, pA, lda, type, pV, type, out var workDevice, out var workHost);
				using var buffer = MklBuffer.Create(workDevice, workHost);
				NativeMethods.cusolverDnXsyevd(this.cusolverHandle, IntPtr.Zero, mode, MatrixFillMode.Upper, n, type, pA, lda, type, pV, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo);
				SolveMethodKind.Eigenvalue.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			else
			{   // CUDA version <= 11.0
				if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
					return false;
				if (!CheckPointer(valOut, out var pV, out int nv))
					return false;
				////if (nv < nn)
				////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

				delegate*<SolveVectorMode, MatrixFillMode, int, IntPtr, int, IntPtr, out int, MklSolverStatus> bufFunc;
				delegate*<SolveVectorMode, MatrixFillMode, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calFunc;
				bufFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSsyevd_bufferSize,
					DataType.RealDouble => &NativeMethods.cusolverDnDsyevd_bufferSize,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCheevd_bufferSize,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZheevd_bufferSize,
					_ => null,
				};
				calFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSsyevd,
					DataType.RealDouble => &NativeMethods.cusolverDnDsyevd,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCheevd,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZheevd,
					_ => null,
				};
				bufFunc(this.cusolverHandle, mode, MatrixFillMode.Upper, nn, pA, llda, pV, out var work);
				using var buffer = MklBuffer.Create<T>(work);
				calFunc(this.cusolverHandle, mode, MatrixFillMode.Upper, nn, pA, llda, pV, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo);
				SolveMethodKind.Eigenvalue.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			return true;
		}

		protected override unsafe bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType eigType, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (mode != SolveVectorMode.NoVector)
				mode = SolveVectorMode.Vector;
			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(B, n, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<GeneralEigenType, SolveVectorMode, MatrixFillMode, int, IntPtr, int, IntPtr, int, IntPtr, out int, MklSolverStatus> bufFunc;
			delegate*<GeneralEigenType, SolveVectorMode, MatrixFillMode, int, IntPtr, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, MklSolverStatus> calFunc;
			bufFunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cusolverDnSsygvd_bufferSize,
				DataType.RealDouble => &NativeMethods.cusolverDnDsygvd_bufferSize,
				DataType.ComplexSingle => &NativeMethods.cusolverDnChegvd_bufferSize,
				DataType.ComplexDouble => &NativeMethods.cusolverDnZhegvd_bufferSize,
				_ => null,
			};
			calFunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cusolverDnSsygvd,
				DataType.RealDouble => &NativeMethods.cusolverDnDsygvd,
				DataType.ComplexSingle => &NativeMethods.cusolverDnChegvd,
				DataType.ComplexDouble => &NativeMethods.cusolverDnZhegvd,
				_ => null,
			};
			bufFunc(this.cusolverHandle, eigType, mode, MatrixFillMode.Upper, nn, pA, llda, pB, lldb, pV, out var work);
			using var buffer = MklBuffer.Create<T>(work);
			calFunc(this.cusolverHandle, eigType, mode, MatrixFillMode.Upper, nn, pA, llda, pB, lldb, pV, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo);
			SolveMethodKind.GeneralEigen.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			return true;
		}

		protected override unsafe bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct)
		{
			if (storeU == SVDStore.Overwrite && storeV == SVDStore.Overwrite)
				throw new ArgumentException(Resources.Parameter.DuplicateValue, nameof(storeU));
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			sbyte jobU = storeU.ToChar(), jobV = storeV.ToChar();
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

			if (this.Mkl111OrAbove)
			{
				var type = Const<T>.DataType.ToMklDataType();
				if (this.SvdViaPolarDecomposition)
				{
					if (storeU != storeV)
						return false;
					if (storeU == SVDStore.Overwrite)
						return false;
					SolveVectorMode mode = storeU == SVDStore.None ? SolveVectorMode.NoVector : SolveVectorMode.Vector;
					int econ = storeU == SVDStore.Economic ? 1 : 0;
					NativeMethods.cusolverDnXgesvdp_bufferSize(this.cusolverHandle, IntPtr.Zero, mode, econ, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, out var workDevice, out var workHost);
					using var buffer = MklBuffer.Create(workDevice, workHost);
					NativeMethods.cusolverDnXgesvdp(this.cusolverHandle, IntPtr.Zero, mode, econ, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo, out var error);
					SolveMethodKind.SVD.CheckDeviceInfo(buffer.ExtraDeviceInfo);
				}
				else
				{
					if (m < n)
						return false;
					NativeMethods.cusolverDnXgesvd_bufferSize(this.cusolverHandle, IntPtr.Zero, jobU, jobV, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, out var workDevice, out var workHost);
					using var buffer = MklBuffer.Create(workDevice, workHost);
					NativeMethods.cusolverDnXgesvd(this.cusolverHandle, IntPtr.Zero, jobU, jobV, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo);
					SolveMethodKind.SVD.CheckDeviceInfo(buffer.ExtraDeviceInfo);
				}
			}
			else
			{   // CUDA version <= 11.0
				if (m < n)
					return false;
				delegate*<int, int, out int, MklSolverStatus> bufFunc;
				delegate*<sbyte, sbyte, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, IntPtr, int, IntPtr, IntPtr, MklSolverStatus> calFunc;
				bufFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSgesvd_bufferSize,
					DataType.RealDouble => &NativeMethods.cusolverDnDgesvd_bufferSize,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCgesvd_bufferSize,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZgesvd_bufferSize,
					_ => null,
				};
				calFunc = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cusolverDnSgesvd,
					DataType.RealDouble => &NativeMethods.cusolverDnDgesvd,
					DataType.ComplexSingle => &NativeMethods.cusolverDnCgesvd,
					DataType.ComplexDouble => &NativeMethods.cusolverDnZgesvd,
					_ => null,
				};
				bufFunc(this.cusolverHandle, mm, nn, out var work);
				using var buffer = MklBuffer.Create<T>(work);
				calFunc(this.cusolverHandle, jobU, jobV, mm, nn, pA, llda, pS, pU, lldu, pV, lldv, buffer.DeviceBuffer, work, IntPtr.Zero, buffer.ExtraDeviceInfo);
				SolveMethodKind.GeneralEigen.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			return true;
		}
		#endregion

		#region not supported routines
		protected override bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) => false;

		protected override bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) => false;

		protected override bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null)
		{
			actualNumber = 0; return false;
		}
		#endregion
		#endregion
	}
}
