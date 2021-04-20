using System;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;
using Althea.Helpers;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.LinearAlgebra.Dense
{
	/// <summary>
	/// The CUDA back-end of the dense linear algebra <see cref="AbstractApi"/> that utilizes cuBLAS and cuSOLVER API with 8.0 ≤ CUDA version ≤ 11.3 (and maybe future versions)
	/// </summary>
	/// <remarks>The legacy cuBLAS APIs are not supported.<br/>
	/// The only supported location is a pure one on GPU memory. But cuFILE cached ones can be supported easily.<br/>
	/// The stream operation is not supported here, but it can be easily added by utilizing "cudaStreamCreate()", "cublasSetStream()", etc.<br/>
	/// The packed matrix, batched matrices and banded matrix BLAS operations are not supported, but it can be easily added as well.</remarks>
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

		/// <summary>
		/// Whether this implementation shall use the Gauss complexity reduction routines ("GEMM3M") or the original complex-typed general matrices multiplications ("GEMM")
		/// </summary>
		public bool ComplexGemmUseGemm3m {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._complexGemm3m;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (value)
				{
					var cap = Storage.StorageApi.GetDeviceComputeCapability(Storage.StorageApi.CurrentDeviceID);
					if (cap.major < 5)
					{
						Log.Write(string.Format(Resource.InsufficientCudaCapability, cap, (5, 0)));
						return;
					}
				}
				this._complexGemm3m = value;
			}
		}

		private bool _complexGemm3m = false;

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
			ptr = mp.OffsetPointer(p.OffsetInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckPointerLong<T>(Storage<T> s, out IntPtr ptr, out long length, int stride = 1) where T : unmanaged
		{
			ptr = default; length = 0;
			var p = s[0];
			if (s.Count != 1 || p.Pointer is not IMemoryPointer mp || !this.IsSupported(mp.Location))
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
			if (A.Count != 1 || p.Pointer is not IMemoryPointer mp || !this.IsSupported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			if (cols * ld > len)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(A));
			if (rows > int.MaxValue || cols > int.MaxValue || ld > int.MaxValue)
				return false;
			r = (int)rows; c = (int)cols; l = (int)ld;
			ptr = mp.OffsetPointer(p.OffsetInBytes);
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckPointer<T>(Storage<T>? A, MatrixOperation op, long rowsAfterOp, long colsAfterOp, long ld, out CuBlasMatrixOperation opCuda, out IntPtr ptr, out int r, out int c, out int l) where T : unmanaged
		{
			ptr = default; r = c = l = 1; opCuda = op.ToCuda();
			if (A is null) // specific null input
				return true;
			if (opCuda == CuBlasMatrixOperation.ConjugateAlone)
				return false;
			var p = A[0];
			if (A.Count != 1 || p.Pointer is not IMemoryPointer mp || !this.IsSupported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			long cols = op.CanInPlace() ? colsAfterOp : rowsAfterOp;
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
			if (A.Count != 1 || p.Pointer is not IMemoryPointer mp || !this.IsSupported(mp.Location))
				return false;
			long len = p.LengthInBytes / Const<T>.SizeT;
			if (cols * ld > len)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(A));
			ptr = mp.OffsetPointer(p.OffsetInBytes);
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
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixUnaryIndexUnary(CombinationOfLocations matrix, CombinationOfLocations index, DataType indexType) => this.IsSupported(matrix) && (index == default || this.IsSupported(index)) && (indexType == DataType.RealInt32 || (this.Cuda11OrAbove && indexType == DataType.RealInt64));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixBinaryIndexUnary(CombinationOfLocations matrix1, CombinationOfLocations matrix2, CombinationOfLocations index, DataType indexType) => this.IsSupported(matrix1) && this.IsSupported(matrix2) && (index == default || this.IsSupported(index)) && (indexType == DataType.RealInt32 || (this.Cuda11OrAbove && indexType == DataType.RealInt64));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool AllQRSupport<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work) => this.IsSupported(A.LocationDescription) && this.IsSupported(B.LocationDescription) && (work is null || this.IsSupported(work.LocationDescription));
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
				funcD = Const<T>.DataType == DataType.ComplexDouble ? &NativeMethods.cublasDzasum : null;
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
			NativeMethods.vecDataConvert(Const<T>.DataType, Const<TOut>.DataType, px, py, n, incSrc, incDst, true).Check();
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
				Storage.NativeMethods.vecFillVal(Const<T>.DataType, px, &one, n, stride);
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
				Storage.NativeMethods.vecFillVal(Const<T>.DataType, px, &one, n, stride);
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
			var opCuda = op.ToCuda();
			if (opCuda == CuBlasMatrixOperation.ConjugateAlone)
				return false;
			if (nx < (opCuda == CuBlasMatrixOperation.None ? nn : mm))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			if (ny < (opCuda == CuBlasMatrixOperation.None ? nn : mm))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<IntPtr, CuBlasMatrixOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSgemv,
					DataType.RealDouble => &NativeMethods.cublasDgemv,
					DataType.ComplexSingle => &NativeMethods.cublasCgemv,
					DataType.ComplexDouble => &NativeMethods.cublasZgemv,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSgemv_v2,
					DataType.RealDouble => &NativeMethods.cublasDgemv_v2,
					DataType.ComplexSingle => &NativeMethods.cublasCgemv_v2,
					DataType.ComplexDouble => &NativeMethods.cublasZgemv_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			func(this.cublasHandle, opCuda, mm, nn, &α, pA, llda, px, strideX, &β, py, strideY).Check();
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

			delegate*<IntPtr, MatrixFillMode, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsymv,
					DataType.RealDouble => &NativeMethods.cublasDsymv,
					DataType.ComplexSingle => hermA ? &NativeMethods.cublasChemv : &NativeMethods.cublasCsymv,
					DataType.ComplexDouble => hermA ? &NativeMethods.cublasZhemv : &NativeMethods.cublasZsymv,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsymv_v2,
					DataType.RealDouble => &NativeMethods.cublasDsymv_v2,
					DataType.ComplexSingle => hermA ? &NativeMethods.cublasChemv_v2 : &NativeMethods.cublasCsymv_v2,
					DataType.ComplexDouble => hermA ? &NativeMethods.cublasZhemv_v2 : &NativeMethods.cublasZsymv_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			func(this.cublasHandle, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, nn, &α, pA, llda, px, strideX, &β, py, strideY).Check();
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
			if (nx < mm)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			if (ny < nn)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<IntPtr, int, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSger,
					DataType.RealDouble => &NativeMethods.cublasDger,
					DataType.ComplexSingle => conjY ? &NativeMethods.cublasCgerc : &NativeMethods.cublasCgerc,
					DataType.ComplexDouble => conjY ? &NativeMethods.cublasZgerc : &NativeMethods.cublasZgerc,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSger_v2,
					DataType.RealDouble => &NativeMethods.cublasDger_v2,
					DataType.ComplexSingle => conjY ? &NativeMethods.cublasCgerc_v2 : &NativeMethods.cublasCgerc_v2,
					DataType.ComplexDouble => conjY ? &NativeMethods.cublasZgerc_v2 : &NativeMethods.cublasZgerc_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			func(this.cublasHandle, mm, nn, &α, px, strideX, py, strideY, pA, llda).Check();
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
			if (nx < nn)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			delegate*<IntPtr, MatrixFillMode, int, T*, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyr,
					DataType.RealDouble => &NativeMethods.cublasDsyr,
					DataType.ComplexSingle => conjX ? &NativeMethods.cublasCher : &NativeMethods.cublasCsyr,
					DataType.ComplexDouble => conjX ? &NativeMethods.cublasZher : &NativeMethods.cublasZsyr,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyr_v2,
					DataType.RealDouble => &NativeMethods.cublasDsyr_v2,
					DataType.ComplexSingle => conjX ? &NativeMethods.cublasCher_v2 : &NativeMethods.cublasCsyr_v2,
					DataType.ComplexDouble => conjX ? &NativeMethods.cublasZher_v2 : &NativeMethods.cublasZsyr_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			func(this.cublasHandle, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, nn, &α, px, strideX, pA, llda).Check();
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
			if (nx < nn)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			if (ny < nn)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<IntPtr, MatrixFillMode, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyr2,
					DataType.RealDouble => &NativeMethods.cublasSsyr2,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cublasCher2 : &NativeMethods.cublasCsyr2,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cublasZher2 : &NativeMethods.cublasZsyr2,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyr2_v2,
					DataType.RealDouble => &NativeMethods.cublasSsyr2_v2,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cublasCher2_v2 : &NativeMethods.cublasCsyr2_v2,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cublasZher2_v2 : &NativeMethods.cublasZsyr2_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			func(this.cublasHandle, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, nn, &α, px, strideX, py, strideY, pA, llda).Check();
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
			if (nx < nn)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			delegate*<IntPtr, MatrixFillMode, CuBlasMatrixOperation, DiagType, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasStrmv,
					DataType.RealDouble => &NativeMethods.cublasDtrmv,
					DataType.ComplexSingle => &NativeMethods.cublasCtrmv,
					DataType.ComplexDouble => &NativeMethods.cublasZtrmv,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasStrmv_v2,
					DataType.RealDouble => &NativeMethods.cublasDtrmv_v2,
					DataType.ComplexSingle => &NativeMethods.cublasCtrmv_v2,
					DataType.ComplexDouble => &NativeMethods.cublasZtrmv_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			var opCuda = op.ToCuda();
			if (opCuda == CuBlasMatrixOperation.ConjugateAlone)
				return false;
			func(this.cublasHandle, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opCuda, unitDiag ? DiagType.Unit : DiagType.NonUnit, nn, pA, llda, px, strideX).Check();
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
			if (nx < (leftA ? nn : mm))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			delegate*<IntPtr, SideMode, int, int, IntPtr, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cublasSdgmm,
				DataType.RealDouble => &NativeMethods.cublasDdgmm,
				DataType.ComplexSingle => &NativeMethods.cublasCdgmm,
				DataType.ComplexDouble => &NativeMethods.cublasZdgmm,
				_ => null,
			};
			if (func is null)
				return false;
			func(this.cublasHandle, leftA ? SideMode.Right : SideMode.Left, mm, nn, pA, llda, px, strideX, pC, lldc).Check();
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

			delegate*<IntPtr, SideMode, MatrixFillMode, CuBlasMatrixOperation, DiagType, int, int, T*, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasStrsm,
					DataType.RealDouble => &NativeMethods.cublasDtrsm,
					DataType.ComplexSingle => &NativeMethods.cublasCtrsm,
					DataType.ComplexDouble => &NativeMethods.cublasZtrsm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasStrsm_v2,
					DataType.RealDouble => &NativeMethods.cublasDtrsm_v2,
					DataType.ComplexSingle => &NativeMethods.cublasCtrsm_v2,
					DataType.ComplexDouble => &NativeMethods.cublasZtrsm_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			var opCuda = op.ToCuda();
			if (opCuda == CuBlasMatrixOperation.ConjugateAlone)
				return false;
			func(this.cublasHandle, leftA ? SideMode.Right : SideMode.Left, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opCuda, unitDiag ? DiagType.Unit : DiagType.NonUnit, mm, nn, &α, pA, llda, pB, lldb).Check();
			return true;
		}

		protected override unsafe bool TriangularMatrixMultiply_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			var opCuda = op.ToCuda();
			if (opCuda == CuBlasMatrixOperation.ConjugateAlone)
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out int mm, out int nn, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
				return false;
			if (!CheckPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
				return false;
			if (α.IsZero()) // result if 0
				this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, null, 0, C, ldc);

			delegate*<IntPtr, SideMode, MatrixFillMode, CuBlasMatrixOperation, DiagType, int, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasStrmm,
					DataType.RealDouble => &NativeMethods.cublasDtrmm,
					DataType.ComplexSingle => &NativeMethods.cublasCtrmm,
					DataType.ComplexDouble => &NativeMethods.cublasZtrmm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasStrmm_v2,
					DataType.RealDouble => &NativeMethods.cublasDtrmm_v2,
					DataType.ComplexSingle => &NativeMethods.cublasCtrmm_v2,
					DataType.ComplexDouble => &NativeMethods.cublasZtrmm_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			func(this.cublasHandle, leftA ? SideMode.Right : SideMode.Left, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opCuda, unitDiag ? DiagType.Unit : DiagType.NonUnit, mm, nn, &α, pA, llda, pB, lldb, pC, lldc).Check();
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

			delegate*<IntPtr, CuBlasMatrixOperation, CuBlasMatrixOperation, int, int, T*, IntPtr, int, T*, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NativeMethods.cublasSgeam,
				DataType.RealDouble => &NativeMethods.cublasDgeam,
				DataType.ComplexSingle => &NativeMethods.cublasCgeam,
				DataType.ComplexDouble => &NativeMethods.cublasZgeam,
				_ => null,
			};
			if (func is null)
				return false;
			func(this.cublasHandle, opcA, opcB, mm, nn, &α, pA, llda, &β, pB, lldb, pC, lldc).Check();
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

			delegate*<IntPtr, CuBlasMatrixOperation, CuBlasMatrixOperation, int, int, int, T*, IntPtr, int,IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSgemm,
					DataType.RealDouble => &NativeMethods.cublasDgemm,
					DataType.ComplexSingle => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasCgemm3m : &NativeMethods.cublasCgemm,
					DataType.ComplexDouble => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasZgemm3m : &NativeMethods.cublasZgemm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSgemm_v2,
					DataType.RealDouble => &NativeMethods.cublasDgemm_v2,
					DataType.ComplexSingle => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasCgemm3m : &NativeMethods.cublasCgemm_v2,
					DataType.ComplexDouble => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasZgemm3m : &NativeMethods.cublasZgemm_v2,
					_ => null,
				};
			}
			if (func is not null)
			{
				func(this.cublasHandle, opcA, opcB, mm, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
			}
			else
			{
				if (Const<T>.DataType == DataType.ComplexHalf || Const<T>.DataType == BrainFloatConst.ComplexBrainFloat16)
					return false;
				var type = Const<T>.DataType.ToCudaDataType();
				ComputeType cType = type switch
				{
					CudaDataType.RealFloat32 or CudaDataType.ComplexFloat32 => ComputeType.Compute32F,
					CudaDataType.RealFloat64 or CudaDataType.ComplexFloat64 => ComputeType.Compute64F,
					CudaDataType.RealFloat16 => ComputeType.Compute16F,
					CudaDataType.RealBrainFloat16 => ComputeType.Compute32F,
					_ => default,
				};
				NativeMethods.cublasGemmEx(this.cublasHandle, opcA, opcB, mm, nn, kk, &α, pA, type, llda, pB, type, lldb, &β, pC, type, lldc, cType, GemmAlgorithm.Default).Check();
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

			delegate*<IntPtr, SideMode, MatrixFillMode, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsymm,
					DataType.RealDouble => &NativeMethods.cublasDsymm,
					DataType.ComplexSingle => hermA ? &NativeMethods.cublasChemm : &NativeMethods.cublasCsymm,
					DataType.ComplexDouble => hermA ? &NativeMethods.cublasZhemm : &NativeMethods.cublasZsymm,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsymm_v2,
					DataType.RealDouble => &NativeMethods.cublasDsymm_v2,
					DataType.ComplexSingle => hermA ? &NativeMethods.cublasChemm_v2 : &NativeMethods.cublasCsymm_v2,
					DataType.ComplexDouble => hermA ? &NativeMethods.cublasZhemm_v2 : &NativeMethods.cublasZsymm_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			func(this.cublasHandle, leftA ? SideMode.Left : SideMode.Right, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
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

			delegate*<IntPtr, MatrixFillMode, CuBlasMatrixOperation, int, int, T*, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyrk,
					DataType.RealDouble => &NativeMethods.cublasDsyrk,
					DataType.ComplexSingle => conjA ? &NativeMethods.cublasCherk : &NativeMethods.cublasCsyrk,
					DataType.ComplexDouble => conjA ? &NativeMethods.cublasZherk : &NativeMethods.cublasZsyrk,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyrk_v2,
					DataType.RealDouble => &NativeMethods.cublasDsyrk_v2,
					DataType.ComplexSingle => conjA ? &NativeMethods.cublasCherk_v2 : &NativeMethods.cublasCsyrk_v2,
					DataType.ComplexDouble => conjA ? &NativeMethods.cublasZherk_v2 : &NativeMethods.cublasZsyrk_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			func(this.cublasHandle, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opcA, nn, kk, &α, pA, llda, &β, pC, lldc).Check();
			return true;
		}

		protected override unsafe bool RankTwoKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, op, n, k, lda, out var opCuda, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			delegate*<IntPtr, MatrixFillMode, CuBlasMatrixOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyr2k,
					DataType.RealDouble => &NativeMethods.cublasDsyr2k,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cublasCher2k : &NativeMethods.cublasCsyr2k,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cublasZher2k : &NativeMethods.cublasZsyr2k,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyr2k_v2,
					DataType.RealDouble => &NativeMethods.cublasDsyr2k_v2,
					DataType.ComplexSingle => conjugate ? &NativeMethods.cublasCher2k_v2 : &NativeMethods.cublasCsyr2k_v2,
					DataType.ComplexDouble => conjugate ? &NativeMethods.cublasZher2k_v2 : &NativeMethods.cublasZsyr2k_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			func(this.cublasHandle, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opCuda, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
			return true;
		}

		protected override unsafe bool RankKUpdateVariant_<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, op, n, k, lda, out var opCuda, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			delegate*<IntPtr, MatrixFillMode, CuBlasMatrixOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
			if (this.Cuda11OrAbove)
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyrkx,
					DataType.RealDouble => &NativeMethods.cublasDsyrkx,
					DataType.ComplexSingle => conjB ? &NativeMethods.cublasCherkx : &NativeMethods.cublasCsyrkx,
					DataType.ComplexDouble => conjB ? &NativeMethods.cublasZherkx : &NativeMethods.cublasZsyrkx,
					_ => null,
				};
			}
			else
			{
				func = Const<T>.DataType switch
				{
					DataType.RealSingle => &NativeMethods.cublasSsyrkx_v2,
					DataType.RealDouble => &NativeMethods.cublasDsyrkx_v2,
					DataType.ComplexSingle => conjB ? &NativeMethods.cublasCherkx_v2 : &NativeMethods.cublasCsyrkx_v2,
					DataType.ComplexDouble => conjB ? &NativeMethods.cublasZherkx_v2 : &NativeMethods.cublasZsyrkx_v2,
					_ => null,
				};
			}
			if (func is null)
				return false;
			func(this.cublasHandle, fillUpper ? MatrixFillMode.Upper : MatrixFillMode.Lower, opCuda, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
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

		protected override unsafe bool MatrixKronecker_<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(A, na, lda, out var pA))
				return false;
			if (!CheckPointerLong(B, nb, ldb, out var pB))
				return false;
			if (ldc < ma * mb)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(ldc));
			if (!CheckPointerLong(C, na * nb, ldc, out var pC))
				return false;
			NativeMethods.matKron(Const<T>.DataType, pA, lda, ma, na, pB, ldb, mb, nb, pC, ldc, &α, &β);
			return true;
		}
		#endregion

		#region solve
		protected override bool LUDecomposition_<T, TInd>(long n, Storage<T> A, long lda, Storage<TInd> pivot) => throw new NotImplementedException();

		protected override bool LinearSolveByLU_<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<TInd> pivot, Storage<T> B, long ldb) => throw new NotImplementedException();

		protected override bool ImplicitQR_<T>(long m, long n, Storage<T> A, long lda, Storage<T> τ) => throw new NotImplementedException();

		protected override bool ImplicitQRFormQ_<T>(long m, long n, long k, Storage<T> Q, long ldq, Storage<T> τ) => throw new NotImplementedException();

		protected override bool ImplicitQRMultiplyQ_<T>(bool leftQ, MatrixOperation op, long m, long n, long k, Storage<T> A, long lda, Storage<T> τ, Storage<T> C, long ldc) => throw new NotImplementedException();

		protected override bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda) => throw new NotImplementedException();

		protected override bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb) => throw new NotImplementedException();

		protected override bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) => throw new NotImplementedException();

		protected override bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) => throw new NotImplementedException();

		protected override bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null) => throw new NotImplementedException();

		protected override bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct) => throw new NotImplementedException();
		#endregion
	}
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释