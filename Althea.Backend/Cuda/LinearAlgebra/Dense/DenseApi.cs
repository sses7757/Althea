using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.LinearAlgebra.Dense
{
	/// <summary>
	/// The CUDA back-end of the dense linear algebra <see cref="AbstractApi"/> that utilizes cuBLAS and cuSOLVER API with 8.0 ≤ CUDA version ≤ 11.3 (and maybe future versions)
	/// </summary>
	/// <remarks>The legacy cuBLAS APIs are not supported.<br/>
	/// The only supported location is a pure one on GPU memory. But cuFILE cached ones can be supported easily.<br/>
	/// The stream operation is not supported here, but it can be easily added by utilizing "cudaStreamCreate()", "cublasSetStream()", etc.</remarks>
	public class DenseApi : AbstractApi
	{
		#region basic
		/// <summary>
		/// The actual CUDA library handles used in its API calls
		/// </summary>
		protected readonly IntPtr cublasHandle, cusolverHandle;

		/// <summary>
		/// Get the device ID that binds to this instance when initializing it.
		/// </summary>
		public int BindedDeviceID {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether the CUDA driver version is larger than or equals to 11.0 (when the cuBLAS legacy ABI are not available)
		/// </summary>
		protected internal bool Cuda11OrAbove {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get or set whether the CDUA BLAS library uses the atomics mode or not
		/// </summary>
		public bool UseAtomicsMode {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				AtomicsMode mode = default;
				NativeMethods.cublasGetAtomicsMode(this.cublasHandle, ref mode).Check();
				return mode == AtomicsMode.Allowed;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				AtomicsMode mode = value ? AtomicsMode.Allowed : AtomicsMode.NotAllowed;
				NativeMethods.cublasSetAtomicsMode(this.cublasHandle, mode).Check();
			}
		}

		public DenseApi()
		{
			this.Cuda11OrAbove = Storage.StorageApi.GetDriverVersion().major >= 11;
			this.BindedDeviceID = Storage.StorageApi.CurrentDeviceID;
			if (this.Cuda11OrAbove)
			{
				NativeMethods.cublasCreate(ref this.cublasHandle).Check();
				NativeMethods.cublasSetPointerMode(this.cublasHandle, PointerMode.Host);
			}
			else
			{
				NativeMethods.cublasCreate_v2(ref this.cublasHandle).Check();
				NativeMethods.cublasSetPointerMode_v2(this.cublasHandle, PointerMode.Host);
			}
			this.UseAtomicsMode = true;
			// TODO: cuSolver
		}

		protected override void Dispose(bool disposeManaged)
		{
			if (this.Cuda11OrAbove)
				NativeMethods.cublasDestroy(this.cublasHandle);
			else
				NativeMethods.cublasDestroy_v2(this.cublasHandle);
			// TODO: cuSolver
		}
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckPointer<T>(Storage<T> s, out IntPtr ptr, out int length, int stride = 1) where T : unmanaged
		{
			ptr = default; length = 0;
			var p = s[0];
			if (s.Count != 1 || p.Pointer is not IMemoryPointer mp || !this.IsSupported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			len = (len - 1) / stride + 1;
			if (len > int.MaxValue)
				return false;
			length = (int)len;
			ptr = mp.OffsetPointer(s[0].OffsetInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupported(StorageLocation location) => location.Type == LocationType.GpuRam && location.LocationDetail == this.BindedDeviceID;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupported(CombinationOfLocations location) => location.Count == 1 && this.IsSupported(location[0]);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> this.IsSupported(location1) && this.IsSupported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		protected override bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3)
			=> this.IsSupported(location1) && this.IsSupported(location2) && this.IsSupported(location3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixUnary(CombinationOfLocations location)
			=> this.IsSupported(location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNormalTypeComplexType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> complexes)
		{
			if (normals.IsEmpty || complexes.IsEmpty)
				return false;
			for (int i = 0; i < normals.Length; i++)
			{
				if (!this.IsSupported(normals[i]))
					return false;
			}
			for (int i = 0; i < complexes.Length; i++)
			{
				if (!this.IsSupported(complexes[i]))
					return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNormalTypeRealType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> reals)
		{
			if (normals.IsEmpty || reals.IsEmpty)
				return false;
			for (int i = 0; i < normals.Length; i++)
			{
				if (!this.IsSupported(normals[i]))
					return false;
			}
			for (int i = 0; i < reals.Length; i++)
			{
				if (!this.IsSupported(reals[i]))
					return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> this.IsSupported(location1) && this.IsSupported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix)
			=> this.IsSupported(vector1) && this.IsSupported(vector2) && this.IsSupported(matrix);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnary(CombinationOfLocations location)
			=> this.IsSupported(location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2) => this.IsSupported(matrix1) && this.IsSupported(matrix2) && this.IsSupported(vector);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix) => this.IsSupported(vector) && this.IsSupported(matrix);
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
			delegate*<IntPtr, int, IntPtr, int, int*, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.ComplexSingle => &NativeMethods.cublasIcamax,
					DataType.ComplexDouble => &NativeMethods.cublasIzamax,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.ComplexSingle => &NativeMethods.cublasIcamax_v2,
					DataType.ComplexDouble => &NativeMethods.cublasIzamax_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			int result;
			func(this.cublasHandle, n, px, strideX, &result).Check();
			index = result - 1;
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
			delegate*<IntPtr, int, IntPtr, int, int*, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.ComplexSingle => &NativeMethods.cublasIcamin,
					DataType.ComplexDouble => &NativeMethods.cublasIzamin,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.ComplexSingle => &NativeMethods.cublasIcamin_v2,
					DataType.ComplexDouble => &NativeMethods.cublasIzamin_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			int result;
			func(this.cublasHandle, n, px, strideX, &result).Check();
			index = result - 1;
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
			delegate*<IntPtr, int, IntPtr, int, float*, CudaBlasStatus> funcS;
			delegate*<IntPtr, int, IntPtr, int, double*, CudaBlasStatus> funcD;
			if (this.Cuda11OrAbove)
			{
				funcS = Const<T>.DataType == DataType.ComplexSingle ? &NativeMethods.cublasScasum : null;
				funcD = Const<T>.DataType == DataType.ComplexSingle ? &NativeMethods.cublasScasum : null;
			}
			else
			{
				funcS = Const<T>.DataType == DataType.ComplexSingle ? &NativeMethods.cublasScasum_v2 : null;
				funcD = Const<T>.DataType == DataType.ComplexDouble ? &NativeMethods.cublasDzasum_v2 : null;
			}
			if (funcS is null && funcD is null)
				return false;
			float resultS; double resultD;
			if (funcS is not null)
			{
				funcS(this.cublasHandle, n, px, strideX, &resultS).Check();
				sum = resultS;
			}
			if (funcD is not null)
			{
				funcD(this.cublasHandle, n, px, strideX, &resultD).Check();
				sum = resultD;
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
			delegate*<IntPtr, int, IntPtr, int, int*, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasIsamax,
					DataType.RealDouble => &NativeMethods.cublasIdamax,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasIsamax_v2,
					DataType.RealDouble => &NativeMethods.cublasIdamax_v2,
					_ => null,
				};
			}
			if (func is not null)
			{
				int result;
				func(this.cublasHandle, n, px, strideX, &result).Check();
				index = result - 1;
			}
			else
			{
				index = NativeMethods.vecArgAbsMax(Const<T>.DataType, px, n, strideX);
			}
			return true;
		}

		protected override unsafe bool AbsoluteValueArgMin_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (!Const<T>.IsPreDefinedNoHalf)
				return false; // half float is not supported
			delegate*<IntPtr, int, IntPtr, int, int*, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasIsamin,
					DataType.RealDouble => &NativeMethods.cublasIdamin,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasIsamin_v2,
					DataType.RealDouble => &NativeMethods.cublasIdamin_v2,
					_ => null,
				};
			}
			if (func is not null)
			{
				int result;
				func(this.cublasHandle, n, px, strideX, &result).Check();
				index = result - 1;
			}
			else
			{
				index = NativeMethods.vecArgAbsMin(Const<T>.DataType, px, n, strideX);
			}
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
			delegate*<IntPtr, int, IntPtr, int, float*, CudaBlasStatus> funcS;
			delegate*<IntPtr, int, IntPtr, int, double*, CudaBlasStatus> funcD;
			if (this.Cuda11OrAbove)
			{
				funcS = Const<T>.DataType switch
				{
					DataType.RealSingle => doSum ? &NativeMethods.cublasSasum : &NativeMethods.cublasSnrm2,
					DataType.ComplexSingle => doSum ? null : &NativeMethods.cublasScnrm2,
					_ => null,
				};
				funcD = Const<T>.DataType switch
				{
					DataType.RealDouble => doSum ? &NativeMethods.cublasDasum : &NativeMethods.cublasDnrm2,
					DataType.ComplexSingle => doSum ? null : &NativeMethods.cublasDznrm2,
					_ => null,
				};
			}
			else
			{
				funcS = Const<T>.DataType switch
				{
					DataType.RealSingle => doSum ? &NativeMethods.cublasSasum_v2 : &NativeMethods.cublasSnrm2_v2,
					DataType.ComplexSingle => doSum ? null : &NativeMethods.cublasScnrm2_v2,
					_ => null,
				};
				funcD = Const<T>.DataType switch
				{
					DataType.RealDouble => doSum ? &NativeMethods.cublasDasum_v2 : &NativeMethods.cublasDnrm2_v2,
					DataType.ComplexDouble => doSum ? null : &NativeMethods.cublasDznrm2_v2,
					_ => null,
				};
			}
			if (funcS is null && funcD is null)
			{
				sum = doSum ? NativeMethods.vecAbsSum(Const<T>.DataType, px, n, strideX) : NativeMethods.vecNorm(Const<T>.DataType, px, n, strideX);
			}
			else
			{
				float resultS; double resultD;
				if (funcS is not null)
				{
					funcS(this.cublasHandle, n, px, strideX, &resultS).Check();
					sum = resultS;
				}
				if (funcD is not null)
				{
					funcD(this.cublasHandle, n, px, strideX, &resultD).Check();
					sum = resultD;
				}
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
			delegate*<IntPtr, int, IntPtr, int, IntPtr, int, T*, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSdot,
					DataType.RealDouble => &NativeMethods.cublasDdot,
					DataType.ComplexSingle => conjX ? &NativeMethods.cublasCdotc : &NativeMethods.cublasCdotu,
					DataType.ComplexDouble => conjX ? &NativeMethods.cublasZdotc : &NativeMethods.cublasZdotu,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSdot_v2,
					DataType.RealDouble => &NativeMethods.cublasDdot_v2,
					DataType.ComplexSingle => conjX ? &NativeMethods.cublasCdotc_v2 : &NativeMethods.cublasCdotu_v2,
					DataType.ComplexDouble => conjX ? &NativeMethods.cublasZdotc_v2 : &NativeMethods.cublasZdotu_v2,
					_ => null,
				};
			}
			T result;
			if (func is not null)
			{
				func(this.cublasHandle, n, px, strideX, py, strideY, &result).Check();
			}
			else if (Const<T>.DataType == DataType.RealHalf || Const<T>.DataType == DataType.ComplexHalf)
			{
				CudaDataType type = Const<T>.DataType == DataType.RealHalf ? CudaDataType.RealFloat16 : CudaDataType.ComplexFloat16;
				if (conjX)
					NativeMethods.cublasDotcEx(this.cublasHandle, n, px, type, strideX, py, type, strideY, &result, type, type).Check();
				else
					NativeMethods.cublasDotEx(this.cublasHandle, n, px, type, strideX, py, type, strideY, &result, type, type).Check();
			}
			else
			{
				if (conjX && Const<T>.IsComplex)
					NativeMethods.vecDotc(Const<T>.DataType, px, py, n, strideX, strideY, &result);
				else
					NativeMethods.vecDot(Const<T>.DataType, px, py, n, strideX, strideY, &result);
			}
			dot = result;
			return true;
		}

		protected override unsafe bool Scale_<T>(Storage<T> x, int strideX, T scalar)
		{
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (!Const<T>.IsPreDefined)
				return false;
			delegate*<IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSscal,
					DataType.RealDouble => &NativeMethods.cublasDscal,
					DataType.ComplexSingle => &NativeMethods.cublasCscal,
					DataType.ComplexDouble => &NativeMethods.cublasZscal,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSscal_v2,
					DataType.RealDouble => &NativeMethods.cublasDscal_v2,
					DataType.ComplexSingle => &NativeMethods.cublasCscal_v2,
					DataType.ComplexDouble => &NativeMethods.cublasZscal_v2,
					_ => null,
				};
			}
			if (func is not null)
			{
				func(this.cublasHandle, n, &scalar, px, strideX).Check();
			}
			else if (Const<T>.DataType == DataType.RealHalf || Const<T>.DataType == DataType.ComplexHalf)
			{
				CudaDataType type = Const<T>.DataType == DataType.RealHalf ? CudaDataType.RealFloat16 : CudaDataType.ComplexFloat16;
				NativeMethods.cublasScalEx(this.cublasHandle, n, &scalar, type, px, type, strideX, type).Check();
			}
			else
			{
				NativeMethods.vecMulScalar(Const<T>.DataType, px, &scalar, n, strideX);
			}
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
			delegate*<IntPtr, int, T*, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSaxpy,
					DataType.RealDouble => &NativeMethods.cublasDaxpy,
					DataType.ComplexSingle => &NativeMethods.cublasCaxpy,
					DataType.ComplexDouble => &NativeMethods.cublasZaxpy,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSaxpy_v2,
					DataType.RealDouble => &NativeMethods.cublasDaxpy_v2,
					DataType.ComplexSingle => &NativeMethods.cublasCaxpy_v2,
					DataType.ComplexDouble => &NativeMethods.cublasZaxpy_v2,
					_ => null,
				};
			}
			if (func is not null)
			{
				func(this.cublasHandle, n, &α, px, strideX, py, strideY).Check();
			}
			else if (Const<T>.DataType == DataType.RealHalf || Const<T>.DataType == DataType.ComplexHalf)
			{
				CudaDataType type = Const<T>.DataType == DataType.RealHalf ? CudaDataType.RealFloat16 : CudaDataType.ComplexFloat16;
				NativeMethods.cublasAxpyEx(this.cublasHandle, n, &α, type, px, type, strideX, py, type, strideY, type).Check();
			}
			else
			{
				NativeMethods.vecsAdd(Const<T>.DataType, &α, px, py, n, strideX, strideY);
			}
			return true;
		}
		#endregion

		#region custom level 1
		protected override bool AggregateProduct_<T>(Storage<T> x, int stride, out T product) => throw new NotImplementedException();

		protected override bool AggregateSum_<T>(Storage<T> x, int stride, out T sum) => throw new NotImplementedException();

		protected override bool PartialProduct_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive) => throw new NotImplementedException();

		protected override bool PartialSum_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive) => throw new NotImplementedException();

		protected override bool PointWiseAddScalar_<T>(Storage<T> x, int stride, T scalr) => throw new NotImplementedException();

		protected override bool PointWiseCast_<T, TOut>(Storage<T> source, int incSrc, Storage<TOut> destination, int incDst) => throw new NotImplementedException();

		protected override bool PointWiseConjugate_<T>(Storage<T> x, int stride) => throw new NotImplementedException();

		protected override bool PointWiseDivide_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) => throw new NotImplementedException();

		protected override bool PointWiseEquals_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, out bool equals) => throw new NotImplementedException();

		protected override bool PointWiseMultiply_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) => throw new NotImplementedException();

		protected override bool PointWisePower_<T>(Storage<T> x, int stride, double p) => throw new NotImplementedException();

		protected override bool PointWisePower_<T>(Storage<T> x, int stride, T p) => throw new NotImplementedException();

		protected override bool TruncateArray_<T>(Storage<T> x, double threshold) => throw new NotImplementedException();
		#endregion

		protected override bool DiagonalMatrixMultiplyGeneral_<T>(bool leftA, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) => throw new NotImplementedException();
		protected override bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb) => throw new NotImplementedException();
		protected override bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) => throw new NotImplementedException();
		protected override bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda) => throw new NotImplementedException();
		protected override bool GeneralMatricesAdd_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool GeneralMatricesMultiply_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool GeneralMatrixMultiplyVector_<T>(MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) => throw new NotImplementedException();
		protected override bool GenralRankOneUpdate_<T>(bool conjY, long m, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda) => throw new NotImplementedException();
		protected override bool LinearSolve_<T>(long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb) => throw new NotImplementedException();
		protected override bool LuDecomposition_<T>(long n, Storage<T> A, long lda) => throw new NotImplementedException();
		protected override bool MatrixCopyUpperLowerParts_<T>(bool storedUpper, bool hermitian, long n, Storage<T> A, long lda) => throw new NotImplementedException();
		protected override bool MatrixKronecker_<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool QRDecomposition_<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq) => throw new NotImplementedException();
		protected override bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null) => throw new NotImplementedException();
		protected override bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct) => throw new NotImplementedException();
		protected override bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool SymmHermMatrixMultiplyVector_<T>(bool fillUpper, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) => throw new NotImplementedException();
		protected override bool SymmHermRankOneUpdate_<T>(bool fillUpper, bool conjX, long n, T α, Storage<T> x, int strideX, T β, Storage<T> A, long lda) => throw new NotImplementedException();
		protected override bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb) => throw new NotImplementedException();
	}
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释