using System;
using System.Collections.Generic;

using Althea.Array;
using Althea.Linq;
using Althea.Storage;

using RT = Althea.Runtime.API;


namespace Althea.Blas
{
	/// <summary>
	/// The BLAS API library wrapper
	/// </summary>
	public static class API
	{
		#region base
		/// <summary>
		/// Static class initializer
		/// </summary>
		static API()
		{
			if (GlobalSettings.BlasGPU != null)
				GPUconstructor = GlobalSettings.BlasGPU.GetConstructor(Array.Empty<Type>());
			else
				GPUconstructor = typeof(Cuda.CudaBlas).GetConstructor(Array.Empty<Type>());
			if (GlobalSettings.BlasCPU != null)
				CPUconstructor = GlobalSettings.BlasCPU.GetConstructor(Array.Empty<Type>());
			else
				CPUconstructor = typeof(Mkl.MklBlas).GetConstructor(Array.Empty<Type>());
			Initialize();
		}

		/// <summary>
		/// Reset the BLAS libraries
		/// </summary>
		public static void Reset()
		{
			try
			{
				GPU.Dispose();
				CPU.Dispose();
			}
			catch (StatusException e)
			{
				Log.Write($"Error at reseting BLAS library \"{e.Message}\":" + Environment.NewLine + e.StackTrace, level: LogLevel.Error);
			}
			finally
			{
				Initialize();
			}
		}

		/// <summary>
		/// Singleton BLAS API of GPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static IBlas GPU => _GPUInit.Value;

		/// <summary>
		/// Singleton BLAS API of CPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static IBlas CPU => _CPUInit.Value;

		private static readonly System.Reflection.ConstructorInfo GPUconstructor, CPUconstructor;

		private static Lazy<IBlas> _GPUInit, _CPUInit;

		private static void Initialize()
		{
			_GPUInit = new Lazy<IBlas>(() => GPUconstructor.Invoke(Array.Empty<object>()) as IBlas, true);
			_CPUInit = new Lazy<IBlas>(() => CPUconstructor.Invoke(Array.Empty<object>()) as IBlas, true);
		}

		private static int GetStridedLength<T>(PureArray<T> array, int stride)
			where T : struct, IComparable<T>
		{
			return checked((int)((array.ActualLength - 1) / stride + 1));
		}
		#endregion


		#region level 1 vector
		/// <summary>
		/// Performs the vector outer product $M = \alpha \vec{a} \vec{b}^T (or \vec{b}^H) + M$ where both $a$ and $b$ are column vectors. If $a = b$, the symmetric/Hermitian rank 1 update will be used.
		/// </summary>
		/// <param name="a"><see cref="DenseVector{T}"/> at left</param>
		/// <param name="b"><see cref="DenseVector{T}"/> at right</param>
		/// <param name="M">output <see cref="DenseMatrix{T}"/> M</param>
		/// <param name="α">scalar of type <typeparamref name="T"/> to multiply</param>
		/// <param name="conjugateB">perform conjugate to <paramref name="b"/> or not, default is true for complex and false otherwise</param>
		/// <param name="strideA">The actual $\vec{a}$ is ${\vec{a}_i}_{i\mod\text{strideA}=0}$</param>
		/// <param name="strideB">The actual $\vec{b}$ is ${\vec{b}_i}_{i\mod\text{strideB}=0}$</param>
		/// <returns>a new <see cref="DenseMatrix{T}"/> containing the result</returns>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorOuterProduct<T>(PureArray<T> a, PureArray<T> b, DenseMatrix<T> M, T α, bool? conjugateB = null, int strideA = 1, int strideB = 1) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			if (b is null || b == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(b), Resource.ArrayCannotNull);
			if (M is null || M == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (GetStridedLength(a, strideA) != M.NRows || GetStridedLength(b, strideB) / strideB != M.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(M));
			var onHost = CudaCSharpHelpers.CheckOnHost(a, b, M);

			// judge symmetric or not
			bool sym = a == b;
			if (sym && GlobalSettings.AutoDetectHermitian)
			{
				using var vec = a.Clone() as DenseVector<T>;
				VectorAddBy(vec, b, Scalars<T>.MinusOne, strideA, strideB);
				sym = vec == Scalars<T>.Zero;
			}
			if (sym && !M.Hermitian)
				throw new ArgumentException(Resource.MatMustHerm, nameof(M));

			// calculate
			bool conjB = !b.IsRealType && (conjugateB ?? !b.IsRealType);
			if (sym)
			{
				var func = onHost ? new IBlas.DelegateSymmHermRankOneUpdate<T>(CPU.SymmHermRankOneUpdate) : GPU.SymmHermRankOneUpdate;
				func(MatrixFillMode.Upper, M.IntRows, α, a.Pointer, strideA, M.Pointer, M.IntLeadDim, !M.IsRealType);
			}
			else
			{
				var func = onHost ? new IBlas.DelegateGenralRankOneUpdate<T>(CPU.GenralRankOneUpdate) : GPU.GenralRankOneUpdate;
				func(M.IntRows, M.IntCols, α, a.Pointer, strideA, b.Pointer, strideB, M.Pointer, M.IntLeadDim, conjB);
			}
		}

		/// <summary>
		/// Compute $\vec{y} = \alpha \vec{x} + \vec{y}$, $\vec{y}$ is overridden after the operation.
		/// </summary>
		/// <param name="y">vector to be added by</param>
		/// <param name="x">vector to add</param>
		/// <param name="α">scalar to multiply <paramref name="x"/></param>
		/// <param name="strideY">The actual $\vec{y}$ is ${\vec{y}_i}_{i\mod\text{strideY}=0}$</param>
		/// <param name="strideX">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{strideX}=0}$</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorAddBy<T>(PureArray<T> y, PureArray<T> x, T α, int strideY = 1, int strideX = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (y is null || y == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(y), Resource.ArrayCannotNull);
			int n = Math.Min(GetStridedLength(x, strideX), GetStridedLength(y, strideY));
			var onHost = CudaCSharpHelpers.CheckOnHost(x, y);

			var func = onHost ? new IBlas.DelegateVectorGeneralAdd<T>(CPU.VectorGeneralAdd) : GPU.VectorGeneralAdd;
			func(n, α, x.Pointer, strideX, y.Pointer, strideY);
		}

		/// <summary>
		/// Compute $\vec{x} = \alpha \vec{x}$, $\vec{x}$ is overridden after the operation.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar to multiply</param>
		/// <param name="stride">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{stride}=0}$</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorScale<T>(PureArray<T> x, T α, int stride = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			// shortcut
			if (stride == 1 && α.IsZero())
			{
				RT.SetValue(x.Pointer, 0, x.ActualLength);
				return;
			}
			int n = GetStridedLength(x, stride);
			var onHost = CudaCSharpHelpers.CheckOnHost(x);

			var func = onHost ? new IBlas.DelegateScale<T>(CPU.Scale) : GPU.Scale;
			func(n, α, x.Pointer, stride);
		}

		// Ignore Spelling: Im
		/// <summary>
		/// Calculate the sum of the vector's each element's absolute value $\sum_i{|Re(\vec{x}_i)| + |Im(\vec{x}_i)|}$.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="stride">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{stride}=0}$</param>
		/// <returns>abs sum</returns>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static double VectorAbsSum<T>(PureArray<T> x, int stride = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			int n = GetStridedLength(x, stride);
			var onHost = CudaCSharpHelpers.CheckOnHost(x);

			var func = onHost ? new IBlas.DelegateAbsSum<T>(CPU.AbsSum) : GPU.AbsSum;
			return func(n, x.Pointer, stride);
		}

		/// <summary>
		/// Finds the (smallest) index of the element of the maximum magnitude $\text{argmax}_i{\vec{x}_i}$.
		/// </summary>
		/// <param name="x">vector x</param>
		/// <param name="stride">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{stride}=0}$</param>
		/// <returns>the zero-based index</returns>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static long VectorAbsArgmax<T>(PureArray<T> x, int stride = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			int n = GetStridedLength(x, stride);
			var onHost = CudaCSharpHelpers.CheckOnHost(x);

			var func = onHost ? new IBlas.DelegateAbsMax<T>(CPU.AbsMax) : GPU.AbsMax;
			return func(n, x.Pointer, stride) - 1;
		}

		/// <summary>
		/// Finds the (smallest) index of the element of the minimum magnitude.
		/// </summary>
		/// <param name="x">vector x</param>
		/// <param name="stride">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{stride}=0}$</param>
		/// <returns>the zero-based index</returns>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static long VectorAbsArgmin<T>(PureArray<T> x, int stride = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			int n = GetStridedLength(x, stride);
			var onHost = CudaCSharpHelpers.CheckOnHost(x);

			var func = onHost ? new IBlas.DelegateAbsMin<T>(CPU.AbsMin) : GPU.AbsMin;
			return func(n, x.Pointer, stride) - 1;
		}

		/// <summary>
		/// Compute vector inner product $\vec{x} \cdot \vec{y} \equiv \vec{x}^H \vec{y}$
		/// </summary>
		/// <param name="x">vector x</param>
		/// <param name="y">vector y</param>
		/// <param name="conjugateX">perform conjugate to <paramref name="x"/> or not, default is true for complex and false otherwise</param>
		/// <param name="strideY">The actual $\vec{y}$ is ${\vec{y}_i}_{i\mod\text{strideY}=0}$</param>
		/// <param name="strideX">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{strideX}=0}$</param>
		/// <returns>the dot result</returns>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static T VectorDot<T>(PureArray<T> x, PureArray<T> y, bool? conjugateX = null, int strideX = 1, int strideY = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (y is null || y == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(y), Resource.ArrayCannotNull);
			int n = Math.Min(GetStridedLength(x, strideX), GetStridedLength(y, strideY));
			var onHost = CudaCSharpHelpers.CheckOnHost(x, y);

			bool conjX = !x.IsRealType && (conjugateX ?? !x.IsRealType);
			var func = onHost ? new IBlas.DelegateDot<T>(CPU.Dot) : GPU.Dot;
			return func(n, x.Pointer, strideX, y.Pointer, strideY, conjX);
		}

		/// <summary>
		/// Calculate vector's norm (2-norm) $\|\vec{x}\|\equiv \sqrt{\sum_i{\vec{x}_i^2}}$
		/// </summary>
		/// <param name="x">vector x</param>
		/// <param name="stride">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{stride}=0}$</param>
		/// <returns>norm of vector</returns>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static double VectorNorm<T>(PureArray<T> x, int stride = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			int n = GetStridedLength(x, stride);
			var onHost = CudaCSharpHelpers.CheckOnHost(x);

			var func = onHost ? new IBlas.DelegateNorm<T>(CPU.Norm) : GPU.Norm;
			return func(n, x.Pointer, stride);
		}

		/// <summary>
		/// Copy from source array <paramref name="src"/> with stride <paramref name="strideSrc"/> and offset <paramref name="offsetSrc"/> to destination array <paramref name="strideDst"/> with stride <paramref name="strideDst"/> and offset <paramref name="offsetDst"/>.
		/// </summary>
		/// <param name="dst">destination array</param>
		/// <param name="src">source array</param>
		/// <param name="count">number of elements to copy</param>
		/// <param name="strideDst">stride of destination array</param>
		/// <param name="strideSrc">stride of source array</param>
		/// <param name="offsetDst">offset of destination pointer, in T rather than bytes</param>
		/// <param name="offsetSrc">offset of source pointer, in T rather than bytes</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if the offsets and/or the strides are out of range, i.e. it would access address out of the vectors' range in memory</exception>
		public static void VectorGenralCopy<T>(PureArray<T> dst, PureArray<T> src, long count, long strideDst = 1, long strideSrc = 1, long offsetDst = 0, long offsetSrc = 0) where T : struct, IComparable<T>
		{
			if (src is null || src == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(src), Resource.ArrayCannotNull);
			if (dst is null || dst == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(dst), Resource.ArrayCannotNull);
			if (offsetDst + (count - 1) * strideDst >= dst.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offsetDst));
			if (offsetSrc + (count - 1) * strideSrc >= src.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offsetSrc));
			var onHost = CudaCSharpHelpers.CheckOnHost(dst, src);

			var func = onHost ? new IBlas.DelegateCopy<T>(CPU.Copy) : GPU.Copy;
			func(checked((int)count), src.Pointer + offsetSrc, checked((int)strideSrc), dst.Pointer + offsetDst, checked((int)strideDst));
		}
		#endregion


		#region level 2 vector and matrix
		/// <summary>
		/// Directly compute $\vec{y} = \alpha A^{\text{op}} \vec{x}$ + \beta \vec{y}, $\vec{y}$ is overridden after the operation.
		/// </summary>
		/// <param name="A">The input <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="x">The input <see cref="DenseVector{T}"/> to multiply matrix</param>
		/// <param name="y">The output <see cref="DenseVector{T}"/></param>
		/// <param name="α">scalar to multiply <paramref name="x"/></param>
		/// <param name="β">scalar to multiply <paramref name="y"/> with default 0</param>
		/// <param name="strideY">The actual $\vec{y}$ is ${\vec{y}_i}_{i\mod\text{strideY}=0}$</param>
		/// <param name="strideX">The actual $\vec{x}$ is ${\vec{x}_i}_{i\mod\text{strideX}=0}$</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentException">if vectors and matrix disagrees in size</exception>
		public static void MatrixVectorMultiply<T>(DenseMatrix<T> A, DenseVector<T> x, DenseVector<T> y, T α, T β = default, MatrixOperation opA = MatrixOperation.None, int strideX = 1, int strideY = 1) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (y is null || y == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(y), Resource.ArrayCannotNull);
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			var (m, n) = opA == MatrixOperation.None ? (A.IntRows, A.IntCols) : (A.IntCols, A.IntRows);
			int lenY = GetStridedLength(y, strideY), lenX = GetStridedLength(x, strideX);
			if (m != lenY || n != lenX)
				throw new ArgumentException(Resource.CannotOperate + $"{m} != {lenY} || {n} != {lenX}");
			opA = opA.CheckOP(A);
			var onHost = CudaCSharpHelpers.CheckOnHost(A, x, y);

			if (A.Hermitian)
			{
				var func = onHost ? new IBlas.DelegateSymmHermMatrixMultiplyVector<T>(CPU.SymmHermMatrixMultiplyVector) : GPU.SymmHermMatrixMultiplyVector;
				func(MatrixFillMode.Upper, n, α, A.Pointer, A.IntLeadDim, x.Pointer, strideX, β, y.Pointer, strideY, !A.IsRealType);
			}
			else
			{
				var func = onHost ? new IBlas.DelegateGeneralMatrixMultiplyVector<T>(CPU.GeneralMatrixMultiplyVector) : GPU.GeneralMatrixMultiplyVector;
				func(opA, m, n, α, A.Pointer, A.IntLeadDim, x.Pointer, strideX, β, y.Pointer, strideY);
			}
		}
		#endregion


		#region level 3 matrix
		/// <summary>
		/// Calculate $C = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$, implemented in CUDA BLAS by adding over every corresponding elements. <paramref name="A"/>, <paramref name="B"/> or <paramref name="C"/> may be the same.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">The <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">The input <see cref="DenseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="C">output <see cref="DenseMatrix{T}"/> C</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="ArgumentOutOfRangeException">if in-place mode is used while <paramref name="opA"/> or <paramref name="opB"/> is not <see cref="MatrixOperation.None"/></exception>
		/// <remarks>If only some of the matrices are Hermitian and the lower parts of them are not stored, these parts will be used in this method. Hence you need fill it manually (<see cref="DenseMatrix{T}.CopyUpperToLower"/>) before calling this method.</remarks>
		public static void MatrixGeneralAdd<T>(DenseMatrix<T> A, DenseMatrix<T> B, DenseMatrix<T> C, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) where T : struct, IComparable<T>
		{
#pragma warning disable CA1062 // Validate arguments of public methods, wrong warning here
			if (A is null || (!α.Equals(Scalars<T>.Zero) && A == PureArray<T>.EmptyDnMat))
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || (!β.Equals(Scalars<T>.Zero) && B == PureArray<T>.EmptyDnMat))
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (C is null || C == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero) && β.Equals(Scalars<T>.Zero))
				throw new ArgumentException(Resource.ParaCannotZero);
			if (α.Equals(Scalars<T>.Zero))
				A = PureArray<T>.EmptyDnMat;
			if (β.Equals(Scalars<T>.Zero))
				B = PureArray<T>.EmptyDnMat;
			// simplify op
			opA = opA.CheckOP(A);
			opB = opB.CheckOP(B);
			int m = C.IntRows, n = C.IntCols;
			// check shape of C, A & C, B
			if (!α.Equals(Scalars<T>.Zero))
			{
				int p = opA == MatrixOperation.None ? A.IntRows : A.IntCols;
				int q = opA == MatrixOperation.None ? A.IntCols : A.IntRows;
				if (m != p || n != q)
					throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(C)} do not match: {m}!={p} || {n}!={q}.");
				// check equality of C, A			
				if (C == A && opA != MatrixOperation.None)
					throw new ArgumentOutOfRangeException(nameof(opA));
			}
			if (!β.Equals(Scalars<T>.Zero))
			{
				int p = opB == MatrixOperation.None ? B.IntRows : B.IntCols;
				int q = opB == MatrixOperation.None ? B.IntCols : B.IntRows;
				if (m != p || n != q)
					throw new ArgumentException(Resource.CannotOperate + $"{nameof(opB)}({nameof(B)}) and {nameof(C)} do not match: {m}!={p} || {n}!={q}.");
				// check equality of C, B
				if (C == B && opB != MatrixOperation.None)
					throw new ArgumentOutOfRangeException(nameof(opB));
			}
			int leadDimA = A == PureArray<T>.EmptyDnMat ? C.IntRows : A.IntLeadDim;
			int leadDimB = B == PureArray<T>.EmptyDnMat ? C.IntRows : B.IntLeadDim;
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B, C);

			// prevent accessing null pointer
			if (α.Equals(Scalars<T>.Zero))
				A = B;
			if (β.Equals(Scalars<T>.Zero))
				B = A;
			// calculate
			var func = onHost ? new IBlas.DelegateGeneralMatricesAdd<T>(CPU.GeneralMatricesAdd) : GPU.GeneralMatricesAdd;
			func(opA, opB, m, n, α, A.Pointer, leadDimA, β, B.Pointer, leadDimB, C.Pointer, C.IntLeadDim);
#pragma warning restore CA1062 // Validate arguments of public methods, wrong warning here
		}

		/// <summary>
		/// Directly calculate $C = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C$.
		/// </summary>
		/// <param name="A">The input <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The input <see cref="DenseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c>β == 0</c>, <paramref name="C"/> will be completely overridden</param>
		/// <param name="C">The output <see cref="DenseMatrix{T}"/> C</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <remarks>If <paramref name="A"/> is Hermitian and <paramref name="B"/> is not while <paramref name="opB"/> != <see cref="MatrixOperation.None"/>, a temporary <see cref="DenseMatrix{T}"/> -- the result of <paramref name="opB"/>(<paramref name="B"/>) will be created to use BLAS function <c>symm</c> or <c>hemm</c>. <para/>
		/// All of the Hermitian matrices except for <paramref name="A"/> (or <paramref name="B"/> if <paramref name="B"/> and <paramref name="C"/> are Hermitian) must be filled manually (<see cref="DenseMatrix{T}.CopyUpperToLower"/>) before calling this method if their lower parts are not actually stored.</remarks>
		public static void MatrixMultiply<T>(DenseMatrix<T> A, DenseMatrix<T> B, DenseMatrix<T> C, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (A is null || (!α.Equals(Scalars<T>.Zero) && A == PureArray<T>.EmptyDnMat))
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || (!α.Equals(Scalars<T>.Zero) && B == PureArray<T>.EmptyDnMat))
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (C is null || C == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			// shortcut: scale C if alpha == 0
			if (A == PureArray<T>.EmptyDnMat)
			{
				VectorScale(C, β);
				return;
			}
			// simplify op
			opA = opA.CheckOP(A);
			opB = opB.CheckOP(B);
			int m = C.IntRows, n = C.IntCols;
			// shortcut: use symmetric rank-k update if A == B and C is Hermitian
			if (A == B && C.Hermitian && ((opA == MatrixOperation.None && opB != MatrixOperation.None) || (opB == MatrixOperation.None && opA != MatrixOperation.None)))
			{
				RankKUpdate(A, C, α, β, conjugateA: Math.Abs(opA - opB) == 2, opA: opA);
				return;
			}
			// get side mode
			SideMode side = SideMode.Left;
			if (/*C.Hermitian &&*/ !A.Hermitian && B.Hermitian) // swap A B 
			{
				(A, B) = (B, A);
				(opA, opB) = (opB, opA);
				side = SideMode.Right;
			}
			// check shape of C, A & C, B
			int p = opA == MatrixOperation.None ? A.IntRows : A.IntCols;
			int q = opA == MatrixOperation.None ? A.IntCols : A.IntRows;
			int r = opB == MatrixOperation.None ? B.IntRows : B.IntCols;
			int s = opB == MatrixOperation.None ? B.IntCols : B.IntRows;
			if (side == SideMode.Right) // swap A B
				(p, q, r, s) = (r, s, p, q);
			if (q != r)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(opB)}({nameof(B)}) do not match: {q}!={r}.");
			if (p != m)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(C)} do not match: {p}!={m}.", nameof(A));
			if (s != n)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opB)}({nameof(B)}) and {nameof(C)} do not match: {s}!={n}.", nameof(B));
			int k = q;
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B, C);

			if (A.Hermitian) // use SYMM or HEMM
			{
				if (opB != MatrixOperation.None) // B is not Hermitian
				{ // create a new matrix
					var transB = new DenseMatrix<T>(B.NCols, B.NRows, A.OnHost, herm: false);
					MatrixGeneralAdd(B, PureArray<T>.EmptyDnMat, transB, α: Scalars<T>.One, opA: opB);
					B = transB;
				}
				var func = onHost ? new IBlas.DelegateSymmHermMatrixMultiplyGeneral<T>(CPU.SymmHermMatrixMultiplyGeneral) : GPU.SymmHermMatrixMultiplyGeneral;
				try
				{
					func(side, MatrixFillMode.Upper, m, n, α, A.Pointer, A.IntLeadDim, B.Pointer, B.IntLeadDim, β, C.Pointer, C.IntLeadDim, !A.IsRealType);
				}
				finally
				{
					if (opB != MatrixOperation.None) // B is not Hermitian, dispose created transB
						B.Dispose();
				}
			}
			else // use GEMM
			{
				// calculate
				var func = onHost ? new IBlas.DelegateGeneralMatricesMultiply<T>(CPU.GeneralMatricesMultiply) : GPU.GeneralMatricesMultiply;
				func(opA, opB, m, n, k, α, A.Pointer, A.IntLeadDim, B.Pointer, B.IntLeadDim, β, C.Pointer, C.IntLeadDim);
				// check result is Hermitian or not
				if (C.NRows == C.NCols && GlobalSettings.AutoDetectHermitian)
				{
					using var Cdag = new DenseMatrix<T>(C.NRows, C.NRows, C.OnHost, herm: false);
					using var diff = new DenseMatrix<T>(C.NRows, C.NRows, C.OnHost, herm: false);
					MatrixGeneralAdd(C, PureArray<T>.EmptyDnMat, Cdag, α: Scalars<T>.One, opA: MatrixOperation.ConjugateTranspose);
					MatrixGeneralAdd(C, Cdag, diff, α: Scalars<T>.One, β: Scalars<T>.MinusOne); // C - C_dagger
					if (diff == Scalars<T>.Zero)
						C.Hermitian = true;
					else
						C.Hermitian = false;
				}
				else
				{
					C.Hermitian = false;
				}
			}
		}

		/// <summary>
		/// Perform symmetric (Hermitian) rank-k update $C = \alpha A^{op}A^{\text{op}}^H + \beta C$, where $\alpha$ and $\beta$ are scalars, $C$ is a symmetric (Hermitian) matrix stored in upper mode, and $A$ is a non-Hermitian matrix.
		/// </summary>
		/// <param name="A">non-Hermitian <see cref="DenseMatrix{T}"/></param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="conjugateA">perform conjugate to <paramref name="A"/> or not, default is true for complex and false otherwise</param>
		/// <param name="C">output <see cref="DenseMatrix{T}"/>, which will be completely overridden if <c><paramref name="β"/> == 0, must be Hermitian</c></param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size or β ≠ 0 while C is not Hermitian</exception>
		public static void RankKUpdate<T>(DenseMatrix<T> A, DenseMatrix<T> C, T α, T β = default, bool? conjugateA = null, MatrixOperation opA = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (C is null || C == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (C.NRows != C.NCols)
				throw new ArgumentException(Resource.MatMustSquare, nameof(C));
			int n = opA == MatrixOperation.None ? A.IntRows : A.IntCols;
			int k = opA == MatrixOperation.None ? A.IntCols : A.IntRows;
			if (C.IntRows != n)
				throw new ArgumentException(Resource.CannotOperate + $"matrix {nameof(A)} and {nameof(C)} sizes do not match: {C.NRows} != {n}.");
			if (!β.Equals(Scalars<T>.Zero) && !C.Hermitian)
				throw new ArgumentException(Resource.CannotOperate + $"matrix {nameof(C)} is not Hermitian while {nameof(β)} ≠ 0.");
			// simplify op
			opA = opA.CheckOP(A);
			var onHost = CudaCSharpHelpers.CheckOnHost(A, C);

			// calculate
			bool conjA = !A.IsRealType && (conjugateA ?? !A.IsRealType);
			var func = onHost ? new IBlas.DelegateRankKUpdate<T>(CPU.RankKUpdate) : GPU.RankKUpdate;
			func(MatrixFillMode.Upper, opA, n, k, α, A.Pointer, A.IntLeadDim, β, C.Pointer, C.IntLeadDim, conjA);
		}

		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="A"/> diag(<paramref name="x"/>) if <paramref name="side"/> == <see cref="SideMode.Right"/> or <paramref name="C"/> = diag(<paramref name="x"/>) * <paramref name="A"/> otherwise.
		/// </summary>
		/// <param name="side">left or right multiply</param>
		/// <param name="A">input <see cref="DenseMatrix{T}"/></param>
		/// <param name="x">input <see cref="DenseVector{T}"/></param>
		/// <param name="strideX">stride of vector <paramref name="x"/></param>
		/// <param name="C">output <see cref="DenseMatrix{T}"/></param>
		public static void DiagonalMatrixMultiply<T>(DenseMatrix<T> A, DenseVector<T> x, DenseMatrix<T> C, SideMode side = SideMode.Right, int strideX = 1) where T : struct, IComparable<T>
		{
			if (C is null || C == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (A.NRows != C.NRows || A.NCols != C.NCols)
				throw new ArgumentException(Resource.CannotOperate + $"matrix {nameof(A)} and {nameof(C)} sizes do not match: {A.NRows} != {C.NRows} || {A.NCols} != {C.NCols}.");
			int n = GetStridedLength(x, strideX);
			if (side == SideMode.Right && A.NCols != n)
			{
				throw new ArgumentException(Resource.CannotOperate + $"matrix {nameof(A)} and {nameof(x)} sizes do not match: {A.NCols} != {n}.");
			}
			else if (side == SideMode.Left && A.NRows != n)
			{
				throw new ArgumentException(Resource.CannotOperate + $"matrix {nameof(A)} and {nameof(x)} sizes do not match: {A.NRows} != {n}.");
			}
			var onHost = CudaCSharpHelpers.CheckOnHost(A, C);

			// calculate
			var func = onHost ? new IBlas.DelegateDiagonalMatrixMultiplyGeneral<T>(CPU.DiagonalMatrixMultiplyGeneral) : GPU.DiagonalMatrixMultiplyGeneral;
			func(side, A.IntRows, A.IntCols, A.Pointer, A.IntLeadDim, x.Pointer, strideX, C.Pointer, C.IntLeadDim);
		}
		#endregion


		#region custom level 1
		/// <summary>
		/// Compute $\vec{a}_i = \vec{a}_i / \vec{b}_i$ (element wise division) in-place.
		/// </summary>
		/// <param name="a">vector a that will be overridden</param>
		/// <param name="b">vector b</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <remarks>there will be a warning if <c>a.Length != b.Length</c></remarks>
		public static void PointWiseDivision<T>(PureArray<T> a, PureArray<T> b) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			if (b is null || b == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(b), Resource.ArrayCannotNull);
			if (a.ActualLength != b.ActualLength)
				Log.Write($"the lengths of the arrays disagree: {a.ActualLength} != {b.ActualLength}.", level: LogLevel.Warning);
			var onHost = CudaCSharpHelpers.CheckOnHost(a, b);

			var func = onHost ? new IBlas.DelegatePointWiseDivide<T>(CPU.PointWiseDivide) : GPU.PointWiseDivide;
			func(a.Pointer, b.Pointer, Math.Min(a.ActualLength, b.ActualLength));
		}

		/// <summary>
		/// Compute $\vec{a}_i = \vec{a}_i * \vec{b}_i$ (element wise division) in-place.
		/// </summary>
		/// <param name="a">vector a that will be overridden</param>
		/// <param name="b">vector b</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <remarks>there will be a warning if <c>a.Length != b.Length</c></remarks>
		public static void PointWiseMultiply<T>(PureArray<T> a, PureArray<T> b) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			if (b is null || b == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(b), Resource.ArrayCannotNull);
			if (a.ActualLength != b.ActualLength)
				Log.Write($"the lengths of the arrays disagree: {a.ActualLength} != {b.ActualLength}.", level: LogLevel.Warning);
			var onHost = CudaCSharpHelpers.CheckOnHost(a, b);

			var func = onHost ? new IBlas.DelegatePointWiseMultiply<T>(CPU.PointWiseMultiply) : GPU.PointWiseMultiply;
			func(a.Pointer, b.Pointer, Math.Min(a.ActualLength, b.ActualLength));
		}

		/// <summary>
		/// Compute $\vec{a}_i = \vec{a}_i ^ p$ (element wise division) in-place.
		/// </summary>
		/// <param name="a">vector a that will be overridden</param>
		/// <param name="p">a <see cref="double"/> power</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void PointWisePower<T>(PureArray<T> a, double p) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			if (!a.IsRealType)
				throw new NotSupportedException("Element wise float power of complex" + Resource.BaseNotSupport);
			var onHost = CudaCSharpHelpers.CheckOnHost(a);

			var func = onHost ? new IBlas.DelegatePointWisePower<T>(CPU.PointWisePower) : GPU.PointWisePower;
			func(a.Pointer, p, a.ActualLength);
		}

		/// <summary>
		/// In-place array conjugate.
		/// </summary>
		/// <param name="a">array to take conjugate</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void PointWiseConjugate<T>(PureArray<T> a) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			var onHost = CudaCSharpHelpers.CheckOnHost(a);

			var func = onHost ? new IBlas.DelegatePointWiseConjugate<T>(CPU.PointWiseConjugate) : GPU.PointWiseConjugate;
			func(a.Pointer, a.ActualLength);
		}

		/// <summary>
		/// Up-cast the array of single types to double types.
		/// </summary>
		/// <param name="a">input array</param>
		/// <param name="result">The up-casted array of type <typeparamref name="TOut"/></param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types, only single types</typeparam>
		/// <typeparam name="TOut">see <see cref="PureArray{T}"/> for supported data types, only double types</typeparam>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type or you are trying to perform other casting</exception>
		public static void PointWiseUpcast<T, TOut>(PureArray<T> a, PureArray<TOut> result) where T : struct, IComparable<T> where TOut : struct, IComparable<TOut>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			if (a.ActualLength != result.ActualLength)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(result));
			DataType typeOut = default(TOut).ToDataType(), typeIn = default(T).ToDataType();
			if (!typeIn.IsFloat() || !typeOut.IsFloat() || typeIn.Bytes() != 4 || typeOut.Bytes() != 8)
				throw new NotSupportedException($"Up-cast from {typeof(T).Name} to {typeof(TOut).Name}" + Resource.BaseNotSupport);
			var onHost = CudaCSharpHelpers.CheckOnHost(a);

			var func = onHost ? new IBlas.DelegatePointWiseUpcast<T, TOut>(CPU.PointWiseUpcast) : GPU.PointWiseUpcast;
			func(result.Pointer, a.Pointer, a.ActualLength);
		}

		/// <summary>
		/// Up-cast the array of real types to complex types.
		/// </summary>
		/// <param name="src">input real array</param>
		/// <param name="dst">output complex array</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types, only real types</typeparam>
		/// <typeparam name="TOut">see <see cref="PureArray{T}"/> for supported data types, only complex types</typeparam>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type or you are trying to perform other casting</exception>
		public static void PointWiseToComplex<T, TOut>(PureArray<T> src, PureArray<TOut> dst) where T : struct, IComparable<T> where TOut : struct, IComparable<TOut>
		{
			if (src is null || src == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(src), Resource.ArrayCannotNull);
			if (dst is null || dst == PureArray<TOut>.EmptyDnVec)
				throw new ArgumentNullException(nameof(dst), Resource.ArrayCannotNull);
			DataType typeOut = default(TOut).ToDataType(), typeIn = default(T).ToDataType();
			if (!typeIn.IsFloat() || !typeOut.IsFloat() || typeIn.Bytes() != typeOut.Bytes() || !typeIn.IsReal() || typeOut.IsReal())
				throw new NotSupportedException($"Up-cast from {typeof(T).Name} to {typeof(TOut).Name}" + Resource.BaseNotSupport);
			if (src.ActualLength != dst.ActualLength)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(dst));
			var onHostA = CudaCSharpHelpers.CheckOnHost(src);
			var onHostB = CudaCSharpHelpers.CheckOnHost(dst);
			if (onHostA != onHostB)
				throw new ArgumentException(Resource.RequireSamePos);

			dst.FillWithZeros();
			var func = onHostA ? new IBlas.DelegateCopy<T>(CPU.Copy) : GPU.Copy;
			func(checked((int)src.Length), src.Pointer, 1, dst.Pointer.As<T>(), 2);
		}

		/// <summary>
		/// Fill existed array with ones.
		/// </summary>
		/// <param name="a">The array to fill</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentNullException">if <paramref name="a"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void FillWithOnes<T>(PureArray<T> a) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			var onHost = CudaCSharpHelpers.CheckOnHost(a);
			
			var func = onHost ? new IBlas.DelegateFillWithOnes<T>(CPU.FillWithOnes) : GPU.FillWithOnes;
			func(a.Pointer, a.ActualLength);
		}

		/// <summary>
		/// Set the <paramref name="array"/>'s values to <paramref name="value"/> at certain <paramref name="positions"/>.
		/// </summary>
		/// <param name="array">array to be set</param>
		/// <param name="positions">positions, a <see cref="int"/> <see cref="Storage{T}"/></param>
		/// <param name="value">The value to be set</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the arrays is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <remarks>For performance reasons, the <paramref name="positions"/> are not check for out-of-range, please do it yourself if necessary.</remarks>
		public static void SetArrayValues<T>(PureArray<T> array, int[] positions, T value) where T : struct, IComparable<T>
		{
			if (array is null || array == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(array), Resource.ArrayCannotNull);
			if (positions is null)
				throw new ArgumentNullException(nameof(positions), Resource.ArrayCannotNull);
			var onHost = CudaCSharpHelpers.CheckOnHost(array);

			using var pos = Storage<int>.Create(positions.Length, array.OnHost);
			RT.CopyIntoArray(pos, positions);
			SetArrayValues(array.Pointer, pos, value, array.ActualLength, onHost);
		}

		/// <summary>
		/// Set the <paramref name="array"/>'s values to <paramref name="value"/> at certain <paramref name="positions"/>.
		/// </summary>
		/// <param name="array">array to be set</param>
		/// <param name="positions">positions, a <see cref="int"/> <see cref="Storage{T}"/></param>
		/// <param name="value">The value to be set</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentNullException">if any of the arrays is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <remarks>For performance reasons, the <paramref name="positions"/> are not check for out-of-range, please do it yourself if necessary.</remarks>
		public static void SetArrayValues<T>(PureArray<T> array, Storage<int> positions, T value) where T : struct, IComparable<T>
		{
			if (array is null || array == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(array), Resource.ArrayCannotNull);
			if (positions is null)
				throw new ArgumentNullException(nameof(positions), Resource.ArrayCannotNull);
			var onHost = CudaCSharpHelpers.CheckOnHost(array);

			SetArrayValues(array.Pointer, positions, value, array.ActualLength, onHost);
		}

		/// <summary>
		/// Set the <paramref name="array"/>'s values to <paramref name="value"/> at certain <paramref name="positions"/>.
		/// </summary>
		/// <param name="array">array to be set</param>
		/// <param name="positions">positions int array</param>
		/// <param name="value">The value to be set</param>
		/// <param name="length">length of <paramref name="positions"/></param>
		/// <param name="onHost">The memory position</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void SetArrayValues<T>(Storage<T> array, Storage<int> positions, T value, long length, bool onHost) where T : struct, IComparable<T>
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array), Resource.ArrayCannotNull);
			if (positions is null)
				throw new ArgumentNullException(nameof(positions), Resource.ArrayCannotNull);
			var func = onHost ? new IBlas.DelegateSetArrayWithValue<T>(CPU.SetArrayWithValue) : GPU.SetArrayWithValue;
			func(array, value, positions, length);
		}

		/// <summary>
		/// Truncate the array by comparing between each element and the largest (abs) one $\vec{a}_i \leftarrow 0 \text{ i.f.f. } \vec{a}_i \le \text{ratioThreshold}\cdot \text{abs}{\max{\vec{a}}}$.
		/// </summary>
		/// <param name="a">array</param>
		/// <param name="threshold">if an element is smaller than <c><paramref name="threshold"/> * abs(the_largest_one)</c> , it will be set to 0</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void Truncate<T>(PureArray<T> a, float threshold) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			var onHost = CudaCSharpHelpers.CheckOnHost(a);

			var func = onHost ? new IBlas.DelegateTruncateArray<T>(CPU.TruncateArray) : GPU.TruncateArray;
			func(a.Pointer, threshold, a.ActualLength);
		}

		/// <summary>
		/// Directly sum the array's elements.
		/// </summary>
		/// <param name="a">The array to be summed</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="a"/></param>
		/// <returns>the sum of <paramref name="a"/></returns>
		public static T Sum<T>(PureArray<T> a, int stride = 1) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			var onHost = CudaCSharpHelpers.CheckOnHost(a);

			var func = onHost ? new IBlas.DelegateSum<T>(CPU.Sum) : GPU.Sum;
			return func(a.Pointer, a.ActualLength, stride);
		}
		#endregion


		#region custom level 2
		/// <summary>
		/// Set a matrix's diagonal elements with 1 and others to 0.
		/// </summary>
		/// <param name="mat">pre-allocated matrix to fill with identity</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of <see cref="DenseMatrix{T}.SetDiag(long, DenseVector{T})"/> returns error status</exception>
		public static void FillIdentity<T>(DenseMatrix<T> mat) where T : struct, IComparable<T>
		{
			if (mat is null || mat == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(mat), Resource.ArrayCannotNull);
			if (mat.NRows != mat.NCols)
				throw new ArgumentException(Resource.MatMustSquare);

			using var vec = new DenseVector<T>(mat.NRows, onHost: mat.OnHost);
			FillWithOnes(vec);
			mat.FillWithZeros();
			mat.SetDiag(0, vec);
		}
		#endregion


		#region custom level 3
		/// <summary>
		/// Copy upper part of <paramref name="A"/> to the lower part of it.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="A">input and output <see cref="DenseMatrix{T}"/></param>
		/// <exception cref="ArgumentNullException">if <paramref name="A"/> is null</exception>
		/// <exception cref="ArgumentException">if <paramref name="A"/> is not square</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixCopyUpperPartToLower<T>(DenseMatrix<T> A) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (A.NRows != A.NCols)
				throw new ArgumentException(Resource.MatMustSquare, nameof(A));
			var onHost = CudaCSharpHelpers.CheckOnHost(A);

			var func = onHost ? new IBlas.DelegateMatrixCopyUpperToLowerPart<T>(CPU.MatrixCopyUpperToLowerPart) : GPU.MatrixCopyUpperToLowerPart;
			func(A.Pointer, A.IntLeadDim, A.IntRows);
		}

		/// <summary>
		/// Calculate matrix Kronecker product <paramref name="dest"/> = <paramref name="A"/> ⊗ <paramref name="B"/> using compiled kernels.
		/// </summary>
		/// <param name="A"><see cref="DenseMatrix{T}"/> at left</param>
		/// <param name="B"><see cref="DenseMatrix{T}"/> at right</param>
		/// <param name="dest">destination <see cref="DenseMatrix{T}"/>, must be preallocated</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <exception cref="ArgumentException">if the matrices sizes do not match</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of custom CUDA method <c>arrConj</c> returns error status</exception>
		public static void MatrixKronecker<T>(DenseMatrix<T> A, DenseMatrix<T> B, DenseMatrix<T> dest) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (dest is null || dest == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(dest), Resource.ArrayCannotNull);
			if (dest.NRows != A.NRows * B.NRows || dest.NCols != A.NCols * B.NCols)
				throw new ArgumentException(Resource.CannotOperate + $"{dest.NRows} != {A.NRows} * {B.NRows} || {dest.NCols} != {A.NCols} * {B.NCols}.", nameof(dest));
			var onHost = CudaCSharpHelpers.CheckOnHost(A);

			var func = onHost ? new IBlas.DelegateMatrixKronecker<T>(CPU.MatrixKronecker) : GPU.MatrixKronecker;
			func(A.Pointer, A.IntLeadDim, A.IntRows, A.IntCols, B.Pointer, B.IntLeadDim, B.IntRows, B.IntCols, dest.Pointer, dest.IntLeadDim);
		}
		#endregion
	}
}
