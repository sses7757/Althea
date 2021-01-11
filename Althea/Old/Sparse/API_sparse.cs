using System;
using System.Collections.Generic;

using Althea.Array;
using Althea.Memory;
using RT = Althea.Runtime.API;


namespace Althea.SparseBlas
{
	/// <summary>
	/// The Sparse BLAS API library wrapper
	/// </summary>
	public static class API
	{
		#region base
		/// <summary>
		/// Static class initializer
		/// </summary>
		static API()
		{
			if (GlobalSettings.SparseGPU != null)
				GPUconstructor = GlobalSettings.SparseGPU.GetConstructor(Array.Empty<Type>());
			else
				GPUconstructor = typeof(Cuda.CudaSparse).GetConstructor(Array.Empty<Type>());
			if (GlobalSettings.SparseCPU != null)
				CPUconstructor = GlobalSettings.SparseCPU.GetConstructor(Array.Empty<Type>());
			else
				CPUconstructor = typeof(Mkl.MklSparse).GetConstructor(Array.Empty<Type>());
			Initialize();
		}

		/// <summary>
		/// Reset the Sparse libraries
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
				Log.Write($"Error at reseting Sparse library \"{e.Message}\":" + Environment.NewLine + e.StackTrace, level: LogLevel.Error);
			}
			finally
			{
				Initialize();
			}
		}

		/// <summary>
		/// Singleton Sparse API of GPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static ISparse GPU => _GPUInit.Value;

		/// <summary>
		/// Singleton Sparse API of CPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static ISparse CPU => _CPUInit.Value;

		private static readonly System.Reflection.ConstructorInfo GPUconstructor, CPUconstructor;

		private static Lazy<ISparse> _GPUInit, _CPUInit;

		private static void Initialize()
		{
			_GPUInit = new Lazy<ISparse>(() => GPUconstructor.Invoke(Array.Empty<object>()) as ISparse, true);
			_CPUInit = new Lazy<ISparse>(() => CPUconstructor.Invoke(Array.Empty<object>()) as ISparse, true);
		}
		#endregion


		#region vector
		/// <summary>
		/// Gather vector at indices to override another vector.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="src">the source <see cref="Storage{T}"/> to obtain from</param>
		/// <param name="dst">the destination <see cref="Storage{T}"/> to override</param>
		/// <param name="pos">the positions to gather</param>
		/// <param name="N">length of <paramref name="pos"/> and <paramref name="dst"/></param>
		/// <exception cref="ArgumentException">if the vectors have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorGatherAtIndices<T>(Storage<T> src, Storage<int> pos, Storage<T> dst, long N) where T : struct, IComparable<T>
		{
			if (src is null)
				throw new ArgumentNullException(nameof(src), Resource.ArrayCannotNull);
			if (dst is null)
				throw new ArgumentNullException(nameof(dst), Resource.ArrayCannotNull);
			if (pos is null)
				throw new ArgumentNullException(nameof(pos));
			var onHost = CudaCSharpHelpers.CheckOnHost(src, dst);

			var func = onHost ? new ISparse.DelegateVectorGatherAtIndices<T>(CPU.VectorGatherAtIndices) : GPU.VectorGatherAtIndices;
			func(src, pos, dst, checked((int)N));
		}

		/// <summary>
		/// Set vector at indices to the value of another vector.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="src">the source <see cref="Storage{T}"/> to obtain from</param>
		/// <param name="dst">the destination <see cref="Storage{T}"/> to override</param>
		/// <param name="pos">the positions to gather</param>
		/// <param name="N">length of <paramref name="pos"/> and <paramref name="src"/></param>
		/// <exception cref="ArgumentException">if the vectors have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorSetAtIndices<T>(Storage<T> src, Storage<int> pos, Storage<T> dst, long N) where T : struct, IComparable<T>
		{
			if (src is null)
				throw new ArgumentNullException(nameof(src), Resource.ArrayCannotNull);
			if (dst is null)
				throw new ArgumentNullException(nameof(dst), Resource.ArrayCannotNull);
			if (pos is null)
				throw new ArgumentNullException(nameof(pos));
			var onHost = CudaCSharpHelpers.CheckOnHost(src, dst);

			var func = onHost ? new ISparse.DelegateVectorSparseToDense<T>(CPU.VectorSparseToDense) : GPU.VectorSparseToDense;
			func(new SparseVectorWrapper<T>(src.MakeSize(N), pos.MakeSize(N)), dst);
		}

		/// <summary>
		/// Gather vector at indices to override another vector.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="src">the source <see cref="Storage{T}"/> to obtain from</param>
		/// <param name="dst">the destination <see cref="Storage{T}"/> to override</param>
		/// <param name="pos">the positions to gather</param>
		/// <exception cref="ArgumentException">if the vectors have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorGatherAtIndices<T>(PureArray<T> src, int[] pos, PureArray<T> dst) where T : struct, IComparable<T>
		{
			if (src is null)
				throw new ArgumentNullException(nameof(src), Resource.ArrayCannotNull);
			if (dst is null)
				throw new ArgumentNullException(nameof(dst), Resource.ArrayCannotNull);
			if (pos is null)
				throw new ArgumentNullException(nameof(pos));
			long N = pos.LongLength;
			var onHost = CudaCSharpHelpers.CheckOnHost(src, dst);
			using var p = Storage<int>.Create(length: pos.LongLength, src.OnHost);
			RT.CopyIntoArray(p, pos);
			var func = onHost ? new ISparse.DelegateVectorGatherAtIndices<T>(CPU.VectorGatherAtIndices) : GPU.VectorGatherAtIndices;
			func(src.Pointer, p, dst.Pointer, checked((int)N));
		}

		/// <summary>
		/// Set vector at indices to the value of another vector.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="src">the source <see cref="Storage{T}"/> to obtain from</param>
		/// <param name="dst">the destination <see cref="Storage{T}"/> to override</param>
		/// <param name="pos">the positions to gather</param>
		/// <exception cref="ArgumentException">if the vectors have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorSetAtIndices<T>(PureArray<T> src, int[] pos, PureArray<T> dst) where T : struct, IComparable<T>
		{
			if (src is null)
				throw new ArgumentNullException(nameof(src), Resource.ArrayCannotNull);
			if (dst is null)
				throw new ArgumentNullException(nameof(dst), Resource.ArrayCannotNull);
			if (pos is null)
				throw new ArgumentNullException(nameof(pos));
			long N = pos.LongLength;
			var onHost = CudaCSharpHelpers.CheckOnHost(src, dst);
			using var p = Storage<int>.Create(length: N, src.OnHost);
			RT.CopyIntoArray(p, pos);
			var func = onHost ? new ISparse.DelegateVectorSparseToDense<T>(CPU.VectorSparseToDense) : GPU.VectorSparseToDense;
			func(new SparseVectorWrapper<T>(src.Pointer.MakeSize(N), p.MakeSize(N)), dst.Pointer);
		}

		/// <summary>
		/// Convert sparse vector to override a dense vector.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="src">the source <see cref="SparseVector{T}"/> to convert from</param>
		/// <param name="dst">the destination <see cref="DenseVector{T}"/> to override</param>
		/// <exception cref="ArgumentException">if the vectors have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorSparseToDense<T>(SparseVector<T> src, DenseVector<T> dst) where T : struct, IComparable<T>
		{
			if (src is null)
				throw new ArgumentNullException(nameof(src), Resource.ArrayCannotNull);
			if (dst is null)
				throw new ArgumentNullException(nameof(dst), Resource.ArrayCannotNull);
			if (dst.LastIndex < src.LastIndex)
				throw new ArgumentException(Resource.VectorTooShort, nameof(dst));
			var onHost = CudaCSharpHelpers.CheckOnHost(src, dst);

			var func = onHost ? new ISparse.DelegateVectorSparseToDense<T>(CPU.VectorSparseToDense) : GPU.VectorSparseToDense;
			func(src.ToWrapper(), dst.Pointer);
		}

		/// <summary>
		/// Dense vector added by a sparse vector $\vec{y} = \vec{y} + \alpha \vec{x}$.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="y"><see cref="DenseVector{T}"/> to be added</param>
		/// <param name="x"><see cref="SparseVector{T}"/> to add</param>
		/// <param name="α">scalar to multiply</param>
		/// <exception cref="ArgumentException">if the vectors have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StackOverflowException">if non-zeros cannot be casted to int without loss</exception>
		public static void VectorSparseAddToDense<T>(DenseVector<T> y, SparseVector<T> x, T α) where T : struct, IComparable<T>
		{
			if (x is null)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (y is null)
				throw new ArgumentNullException(nameof(y), Resource.ArrayCannotNull);
			if (y.LastIndex < x.LastIndex)
				throw new ArgumentException(Resource.VectorTooShort, nameof(y));
			var onHost = CudaCSharpHelpers.CheckOnHost(x, y);

			var func = onHost ? new ISparse.DelegateVectorSparseAddToDense<T>(CPU.VectorSparseAddToDense) : GPU.VectorSparseAddToDense;
			func(α, x.ToWrapper(), y.Pointer);
		}

		/// <summary>
		/// Sparse vector dot dense vector $\vec{x} \cdot \vec{y}$ or $\bar{\vec{x}} \cdot \vec{y}$.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="y"><see cref="DenseVector{T}"/> at right</param>
		/// <param name="x"><see cref="SparseVector{T}"/> at left</param>
		/// <param name="conjugateX">perform conjugate to <paramref name="x"/> or not, default is true for complex and false otherwise</param>
		/// <returns>Inner product result</returns>
		/// <exception cref="ArgumentException">if the vectors have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static T VectorSparseDotDense<T>(SparseVector<T> x, DenseVector<T> y, bool? conjugateX = null) where T : struct, IComparable<T>
		{
			if (x is null || x == PureArray<T>.EmptySpVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (y is null || y == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(y), Resource.ArrayCannotNull);
			if (y.LastIndex < x.LastIndex)
				throw new ArgumentException(Resource.VectorTooShort, nameof(y));
			var onHost = CudaCSharpHelpers.CheckOnHost(x, y);
			bool conjX = conjugateX ?? !x.IsRealType;

			var func = onHost ? new ISparse.DelegateVectorSparseDotDense<T>(CPU.VectorSparseDotDense) : GPU.VectorSparseDotDense;
			return func(checked((int)y.Length), x.ToWrapper(), y.Pointer, conjX);
		}
		#endregion


		#region vector and matrix
		/// <summary>
		/// Dense vector add by sparse matrix multiplying another dense vector :$\vec{y} = \beta \vec{y} + \alpha M^{\text{op}} \vec{x}$.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="y"><see cref="DenseVector{T}"/> at right</param>
		/// <param name="x"><see cref="DenseVector{T}"/> to multiply the matrix</param>
		/// <param name="M"><see cref="SparseMatrix{T}"/> to multiply the matrix</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="opM">the operation to matrix <paramref name="M"/></param>
		/// <exception cref="ArgumentException">if the arrays have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void SparseMatrixDenseVectorMultiply<T>(SparseMatrix<T> M, DenseVector<T> x, DenseVector<T> y, T α, T β = default, MatrixOperation opM = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (y is null || y == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(y), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero))
			{
				y.Scale(β);
				return;
			}
			if (x is null || x == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (opM == MatrixOperation.None)
			{
				if (M.NCols != x.Length || M.NRows != y.Length)
					throw new ArgumentException(Resource.MatrixWrongSize, nameof(M));
			}
			else
			{
				if (M.NCols != y.Length || M.NRows != x.Length)
					throw new ArgumentException(Resource.MatrixWrongSize, nameof(M));
			}
			opM = opM.CheckOP(M);
			var onHost = CudaCSharpHelpers.CheckOnHost(x, y, M);

			var func = onHost ? new ISparse.DelegateMatrixVectorSparseMultiplyDense<T>(CPU.MatrixVectorSparseMultiplyDense) : GPU.MatrixVectorSparseMultiplyDense;
			func(opM, M.IntRows, M.IntCols, M.ToWrapper(), M.Format, x.Pointer, y.Pointer, α, β);
		}

		/// <summary>
		/// Dense vector add by dense matrix multiplying a sparse vector :$\vec{y} = \beta \vec{y} + \alpha M^{\text{op}} \vec{x}$.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported types</typeparam>
		/// <param name="y"><see cref="DenseVector{T}"/> at right</param>
		/// <param name="x"><see cref="SparseVector{T}"/> to multiply the matrix</param>
		/// <param name="M"><see cref="DenseMatrix{T}"/> to multiply the matrix</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="opM">the operation to matrix <paramref name="M"/></param>
		/// <exception cref="ArgumentException">if the arrays have incompatible size</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		///  <remarks>If <paramref name="M"/> is Hermitian and the lower part of it are not stored, this part will be used in this method, you will need to fill it manually (<see cref="DenseMatrix{T}.CopyUpperToLower"/>) before calling this method.</remarks>
		public static void DenseMatrixSparseVectorMultiply<T>(DenseMatrix<T> M, SparseVector<T> x, DenseVector<T> y, T α, T β = default, MatrixOperation opM = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (y is null || y == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(y), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero))
			{
				y.Scale(β);
				return;
			}
			if (x is null || x == PureArray<T>.EmptySpVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (M is null || M == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (opM == MatrixOperation.None)
			{
				if (M.NCols != x.Length || M.NRows != y.Length)
					throw new ArgumentException(Resource.MatrixWrongSize, nameof(M));
			}
			else
			{
				if (M.NCols != y.Length || M.NRows != x.Length)
					throw new ArgumentException(Resource.MatrixWrongSize, nameof(M));
			}
			opM = opM.CheckOP(M);
			var onHost = CudaCSharpHelpers.CheckOnHost(x, y, M);

			var func = onHost ? new ISparse.DelegateMatrixVectorDenseMultiplySparse<T>(CPU.MatrixVectorDenseMultiplySparse) : GPU.MatrixVectorDenseMultiplySparse;
			func(opM, M.IntRows, M.IntCols, M.Pointer, M.IntLeadDim, x.ToWrapper(), y.Pointer, α, β);
		}
		#endregion


		#region matrix format conversion
		/// <summary>
		/// Directly convert a <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.CSR"/> to a <see cref="DenseMatrix{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="SparseMatrix{T}"/></param>
		/// <param name="dest">the output <see cref="DenseMatrix{T}"/></param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> or <paramref name="dest"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixSparseCSRToDense<T>(DenseMatrix<T> dest, SparseMatrix<T> M) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (dest is null || dest == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(dest), Resource.ArrayCannotNull);
			if (M.NRows != dest.NRows || M.NCols != dest.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(dest));
			if (M.Format != SparseMatrixFormat.CSR)
				throw new ArgumentException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSR), nameof(M));
			var onHost = CudaCSharpHelpers.CheckOnHost(dest, M);

			var func = onHost ? new ISparse.DelegateMatrixSparseCSRToDense<T>(CPU.MatrixSparseCSRToDense) : GPU.MatrixSparseCSRToDense;
			func(M.IntRows, M.IntRows, dest.Pointer, dest.IntLeadDim, M.ToWrapper());
		}

		/// <summary>
		/// Directly convert a <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.CSC"/> to a <see cref="DenseMatrix{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="SparseMatrix{T}"/></param>
		/// <param name="dest">the output <see cref="DenseMatrix{T}"/></param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> or <paramref name="dest"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixSparseCSCToDense<T>(DenseMatrix<T> dest, SparseMatrix<T> M) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (dest is null || dest == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(dest), Resource.ArrayCannotNull);
			if (M.NRows != dest.NRows || M.NCols != dest.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(dest));
			if (M.Format != SparseMatrixFormat.CSC)
				throw new ArgumentException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSC), nameof(M));
			var onHost = CudaCSharpHelpers.CheckOnHost(dest, M);

			var func = onHost ? new ISparse.DelegateMatrixSparseCSCToDense<T>(CPU.MatrixSparseCSCToDense) : GPU.MatrixSparseCSCToDense;
			func(M.IntRows, M.IntRows, dest.Pointer, dest.IntLeadDim, M.ToWrapper());
		}

		/// <summary>
		/// Directly convert a <see cref="DenseMatrix{T}"/> to a new <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.CSR"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="DenseMatrix{T}"/></param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static SparseMatrix<T> MatrixDenseToSparseCSR<T>(DenseMatrix<T> M) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);

			M.CopyUpperToLower(); // in-place
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixDenseToSparseCSR<T>(CPU.MatrixDenseToSparseCSR) : GPU.MatrixDenseToSparseCSR;
			var spM = func(M.IntRows, M.IntRows, M.Pointer, M.IntLeadDim);

			return new SparseMatrix<T>(M.NRows, M.NCols, spM.Values, spM.Row, spM.Column, SparseMatrixFormat.CSR, herm: M.Hermitian);
		}

		/// <summary>
		/// Directly convert a <see cref="DenseMatrix{T}"/> to a new <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.CSR"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="DenseMatrix{T}"/></param>
		/// <param name="sp">the output <see cref="SparseMatrix{T}"/> to <b>in-place</b> replace</param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixDenseToSparseCSR<T>(DenseMatrix<T> M, SparseMatrix<T> sp) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (sp is null || sp == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(sp), Resource.ArrayCannotNull);
			if (sp.NRows != M.NRows || sp.NCols != M.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(sp));

			M.CopyUpperToLower(); // in-place
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixDenseToSparseCSR<T>(CPU.MatrixDenseToSparseCSR) : GPU.MatrixDenseToSparseCSR;
			var spM = func(M.IntRows, M.IntRows, M.Pointer, M.IntLeadDim);

			sp.Format = SparseMatrixFormat.CSR;
			sp.Hermitian = M.Hermitian;
			sp.Pointer.ReplaceBy(spM.Values);
			sp.RowPointer.ReplaceBy(spM.Row);
			sp.ColumnPointer.ReplaceBy(spM.Column);
		}

		/// <summary>
		/// Directly convert a <see cref="DenseMatrix{T}"/> to a new <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.CSC"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="DenseMatrix{T}"/></param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static SparseMatrix<T> MatrixDenseToSparseCSC<T>(DenseMatrix<T> M) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);

			M.CopyUpperToLower(); // in-place
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixDenseToSparseCSC<T>(CPU.MatrixDenseToSparseCSC) : GPU.MatrixDenseToSparseCSC;
			var spM = func(M.IntRows, M.IntRows, M.Pointer, M.IntLeadDim);

			return new SparseMatrix<T>(M.NRows, M.NCols, spM.Values, spM.Row, spM.Column, SparseMatrixFormat.CSC, herm: M.Hermitian);
		}


		/// <summary>
		/// Directly convert a <see cref="DenseMatrix{T}"/> to a new <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.CSC"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="DenseMatrix{T}"/></param>
		/// <param name="sp">the output <see cref="SparseMatrix{T}"/> to <b>in-place</b> replace</param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixDenseToSparseCSC<T>(DenseMatrix<T> M, SparseMatrix<T> sp) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (sp.NRows != M.NRows || sp.NCols != M.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(sp));

			M.CopyUpperToLower(); // in-place
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixDenseToSparseCSC<T>(CPU.MatrixDenseToSparseCSC) : GPU.MatrixDenseToSparseCSC;
			var spM = func(M.IntRows, M.IntRows, M.Pointer, M.IntLeadDim);

			sp.Format = SparseMatrixFormat.CSC;
			sp.Hermitian = M.Hermitian;
			sp.Pointer.ReplaceBy(spM.Values);
			sp.RowPointer.ReplaceBy(spM.Row);
			sp.ColumnPointer.ReplaceBy(spM.Column);
		}

		/// <summary>
		/// Prune a <see cref="DenseMatrix{T}"/> of format <see cref="SparseMatrixFormat.CSR"/> to a new <see cref="SparseMatrix{T}"/> with absolute values less than <paramref name="threshold"/> regarding as zeros.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported REAL data types</typeparam>
		/// <param name="M">the input <see cref="SparseMatrix{T}"/></param>
		/// <param name="threshold"><c>abs(value)</c> less than <paramref name="threshold"/> will be regarded as zero, must be larger than or equal to 0</param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="threshold"/> is negative</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static SparseMatrix<T> MatrixDensePruneToCSR<T>(DenseMatrix<T> M, float threshold) where T : struct, IComparable<T>
		{
			if (threshold < 0)
				throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resource.ParaCannotNegative);
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (threshold == 0)
				return MatrixDenseToSparseCSR(M);
			M.CopyUpperToLower(); // in-place
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixDensePruneToSparseCSR<T>(CPU.MatrixDensePruneToSparseCSR) : GPU.MatrixDensePruneToSparseCSR;
			var spM = func(M.IntRows, M.IntRows, threshold, M.Pointer, M.IntLeadDim);

			return new SparseMatrix<T>(M.NRows, M.NCols, spM.Values, spM.Row, spM.Column, SparseMatrixFormat.CSR, herm: M.Hermitian);
		}

		/// <summary>
		/// Prune a <see cref="DenseMatrix{T}"/> of format <see cref="SparseMatrixFormat.CSR"/> to a new <see cref="SparseMatrix{T}"/> with absolute values less than <paramref name="threshold"/> regarding as zeros.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported REAL data types</typeparam>
		/// <param name="M">the input <see cref="SparseMatrix{T}"/></param>
		/// <param name="threshold"><c>abs(value)</c> less than <paramref name="threshold"/> will be regarded as zero, must be larger than or equal to 0</param>
		/// <param name="sp">the output <see cref="SparseMatrix{T}"/> to <b>in-place</b> replace</param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="threshold"/> is negative</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixDensePruneToCSR<T>(DenseMatrix<T> M, float threshold, SparseMatrix<T> sp) where T : struct, IComparable<T>
		{
			if (threshold < 0)
				throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resource.ParaCannotNegative);
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (sp.NRows != M.NRows || sp.NCols != M.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(sp));
			if (threshold == 0)
			{
				MatrixDenseToSparseCSR(M, sp);
				return;
			}
			M.CopyUpperToLower(); // in-place
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixDensePruneToSparseCSR<T>(CPU.MatrixDensePruneToSparseCSR) : GPU.MatrixDensePruneToSparseCSR;
			var spM = func(M.IntRows, M.IntRows, threshold, M.Pointer, M.IntLeadDim);

			sp.Format = SparseMatrixFormat.CSR;
			sp.Hermitian = M.Hermitian;
			sp.Pointer.ReplaceBy(spM.Values);
			sp.RowPointer.ReplaceBy(spM.Row);
			sp.ColumnPointer.ReplaceBy(spM.Column);
		}

		/// <summary>
		/// Prune a <see cref="SparseMatrix{T}"/> of format <see cref="SparseMatrixFormat.Compressed"/> to a new <see cref="SparseMatrix{T}"/> with absolute values less than <paramref name="threshold"/> regarding as zeros.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="SparseMatrix{T}"/></param>
		/// <param name="threshold"><c>abs(value)</c> less than <paramref name="threshold"/> will be regarded as zero, must be larger than 0</param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="threshold"/> ≤ 0</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static SparseMatrix<T> MatrixCompressedPrune<T>(SparseMatrix<T> M, float threshold = default) where T : struct, IComparable<T>
		{
			if (threshold < 0)
				throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resource.ParaCannotNegative);
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if ((M.Format & SparseMatrixFormat.Compressed) == 0)
				throw new NotSupportedException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.Compressed));
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixCompressedPruneToCompressed<T>(CPU.MatrixCompressedPruneToCompressed) : GPU.MatrixCompressedPruneToCompressed;
			var spM = func(M.IntRows, M.IntRows, threshold, M.ToWrapper(), M.Format == SparseMatrixFormat.CSR);

			return new SparseMatrix<T>(M.NRows, M.NCols, spM.Values, spM.Row, spM.Column, SparseMatrixFormat.CSR, herm: M.Hermitian);
		}

		/// <summary>
		/// Prune a <see cref="SparseMatrix{T}"/> of format <see cref="SparseMatrixFormat.Compressed"/> to a new <see cref="SparseMatrix{T}"/> with absolute values less than <paramref name="threshold"/> regarding as zeros.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="M">the input <see cref="SparseMatrix{T}"/></param>
		/// <param name="sp">the output <see cref="SparseMatrix{T}"/> to <b>in-place</b> replace</param>
		/// <param name="threshold"><c>abs(value)</c> less than <paramref name="threshold"/> will be regarded as zero, must be larger than 0</param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="threshold"/> ≤ 0</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixCompressedPrune<T>(SparseMatrix<T> M, SparseMatrix<T> sp, float threshold = default) where T : struct, IComparable<T>
		{
			if (threshold < 0)
				throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resource.ParaCannotNegative);
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if ((M.Format & SparseMatrixFormat.Compressed) == 0)
				throw new NotSupportedException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.Compressed));
			if (sp.NRows != M.NRows || sp.NCols != M.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(sp));
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixCompressedPruneToCompressed<T>(CPU.MatrixCompressedPruneToCompressed) : GPU.MatrixCompressedPruneToCompressed;
			var spM = func(M.IntRows, M.IntRows, threshold, M.ToWrapper(), M.Format == SparseMatrixFormat.CSR);

			sp.Format = M.Format;
			sp.Hermitian = M.Hermitian;
			sp.Pointer.ReplaceBy(spM.Values);
			sp.RowPointer.ReplaceBy(spM.Row);
			sp.ColumnPointer.ReplaceBy(spM.Column);
		}

		/// <summary>
		/// Convert a sparse matrix to a sparse matrix with different format.
		/// The out-of-place arrays of this operation are:
		/// <list type="table">
		/// <listheader><term>Format1 ↔ Format2</term><description>  Out-of-place arrays</description></listheader>
		/// <item><term>COOR ↔ COOC</term><description>  All arrays</description></item>
		/// <item><term>COOR ↔ CSR</term><description>  Row index array</description></item>
		/// <item><term>COOR ↔ CSC</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSR</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSC</term><description>  Column index array</description></item>
		/// <item><term>CSR ↔ CSC</term><description>  All arrays</description></item>
		/// </list>
		/// </summary>
		/// <param name="op">the operation to apply to <paramref name="M"/>, default <see cref="MatrixOperation.None"/></param>
		/// <param name="M">source <see cref="SparseMatrix{T}"/></param>
		/// <param name="target">target format, can be non-atomic</param>
		/// <returns>a new <see cref="SparseMatrix{T}"/> if <paramref name="target"/> does not contains original format</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="ArgumentException">if <paramref name="M"/> is not of <see cref="SparseMatrixFormat.Compressed"/></exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static SparseMatrix<T> MatrixSparseFormatConvert<T>(SparseMatrix<T> M, SparseMatrixFormat target, MatrixOperation op = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixSparseFormatConvert<T>(CPU.MatrixSparseFormatConvert) : GPU.MatrixSparseFormatConvert;
			var spM = func(M.IntRows, M.IntRows, op, M.ToWrapper(), M.Format, ref target);

			bool herm = op == MatrixOperation.None ? M.Hermitian : (M.Hermitian && (M.IsRealType || op != MatrixOperation.Transpose));
			if (spM.Values == M.Pointer || spM.Row == M.RowPointer || spM.Column == M.ColumnPointer)
				return new SparseMatrix<T>(M, M.NRows, M.NCols, offsetRef: spM.Values - M.Pointer, rowPtr: spM.Row, colPtr: spM.Column,
											refRow: spM.Row == M.RowPointer, refCol: spM.Column == M.ColumnPointer,
											format: target, herm: herm);
			else
				return new SparseMatrix<T>(M.NRows, M.NCols, value: spM.Values, rowPtr: spM.Row, colPtr: spM.Column,
											refVal: spM.Values == M.Pointer, refRow: spM.Row == M.RowPointer, refCol: spM.Column == M.ColumnPointer,
											format: target, herm: herm);
		}

		/// <summary>
		/// Convert a sparse matrix to a sparse matrix with different format.
		/// The out-of-place arrays of this operation are:
		/// <list type="table">
		/// <listheader><term>Format1 ↔ Format2</term><description>  Out-of-place arrays</description></listheader>
		/// <item><term>COOR ↔ COOC</term><description>  All arrays</description></item>
		/// <item><term>COOR ↔ CSR</term><description>  Row index array</description></item>
		/// <item><term>COOR ↔ CSC</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSR</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSC</term><description>  Column index array</description></item>
		/// <item><term>CSR ↔ CSC</term><description>  All arrays</description></item>
		/// </list>
		/// </summary>
		/// <param name="op">the operation to apply to <paramref name="M"/>, default <see cref="MatrixOperation.None"/></param>
		/// <param name="M">source <see cref="SparseMatrix{T}"/></param>
		/// <param name="sp">the output <see cref="SparseMatrix{T}"/> to <b>in-place</b> replace</param>
		/// <param name="target">target format, can be non-atomic</param>
		/// <returns>a new <see cref="SparseMatrix{T}"/> if <paramref name="target"/> does not contains original format</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="ArgumentException">if <paramref name="M"/> is not of <see cref="SparseMatrixFormat.Compressed"/></exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixSparseFormatConvert<T>(SparseMatrix<T> M, SparseMatrix<T> sp, SparseMatrixFormat target, MatrixOperation op = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (sp.NRows != M.NRows || sp.NCols != M.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(sp));
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixSparseFormatConvert<T>(CPU.MatrixSparseFormatConvert) : GPU.MatrixSparseFormatConvert;
			var spM = func(M.IntRows, M.IntRows, op, M.ToWrapper(), M.Format, ref target);

			sp.Format = target;
			if (op == MatrixOperation.None)
			{
				sp.Hermitian = M.Hermitian;
				if (spM.Values == M.Pointer)
					sp.Pointer = sp.Pointer.MakeRefOf(M.Pointer);
				else
					sp.Pointer.ReplaceBy(M.Pointer);
				if (spM.Row == M.RowPointer)
					sp.RowPointer = sp.RowPointer.MakeRefOf(spM.Row);
				else
					sp.RowPointer.ReplaceBy(spM.Row);
				if (spM.Column == M.ColumnPointer)
					sp.ColumnPointer = sp.ColumnPointer.MakeRefOf(spM.Column);
				else
					sp.ColumnPointer.ReplaceBy(spM.Column);
			}
			else
			{
				sp.Hermitian = M.Hermitian && (M.IsRealType || op != MatrixOperation.Transpose);
				if (spM.Values == M.Pointer)
					sp.Pointer = sp.Pointer.MakeRefOf(M.Pointer);
				else
					sp.Pointer.ReplaceBy(M.Pointer);
				if (spM.Row == M.ColumnPointer)
					sp.RowPointer = sp.RowPointer.MakeRefOf(spM.Row);
				else
					sp.RowPointer.ReplaceBy(spM.Row);
				if (spM.Column == M.RowPointer)
					sp.ColumnPointer = sp.ColumnPointer.MakeRefOf(spM.Column);
				else
					sp.ColumnPointer.ReplaceBy(spM.Column);
			}
		}
		#endregion


		#region matrix computation
		/// <summary>
		/// Compute $C = \alpha A^\text{opA} + \beta B^\text{opB}$ for sparse matrices.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="α">scalar of type <typeparamref name="T"/>. If <c><paramref name="α"/> == 0</c>, please use <see cref="MatrixSparseFormatConvert{T}(SparseMatrix{T}, SparseMatrix{T}, SparseMatrixFormat, MatrixOperation)"/></param>
		/// <param name="A">the <see cref="SparseMatrix{T}"/> A</param>
		/// <param name="B">the <see cref="SparseMatrix{T}"/> B</param>
		/// <param name="C">the output <see cref="SparseMatrix{T}"/> C, will be in-place altered</param>
		/// <param name="opA">operation to <paramref name="A"/></param>
		/// <param name="opB">operation to <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/>. If <c><paramref name="β"/> == 0</c>, please use <see cref="MatrixSparseFormatConvert{T}(SparseMatrix{T}, SparseMatrix{T}, SparseMatrixFormat, MatrixOperation)"/></param>
		/// <exception cref="ArgumentNullException">if any of the matrices is null</exception>
		/// <exception cref="ArgumentException">if the matrices are not compatible in size or matrices are not of <see cref="SparseMatrixFormat.CSR"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> OR <paramref name="β"/> is 0</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixSparseAddSparse<T>(SparseMatrix<T> A, SparseMatrix<T> B, SparseMatrix<T> C, T α, T β, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero) || β.Equals(Scalars<T>.Zero))
				throw new ArgumentException(Resource.ParaCannotZero);
			// simplify op
			opA = opA.CheckOP(A);
			opB = opB.CheckOP(B);
			// check shape of C, A & C, B
			int m = C.IntRows, n = C.IntCols;
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
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B);

			var func = onHost ? new ISparse.DelegateMatrixSparseAddSparse<T>(CPU.MatrixSparseAddSparse) : GPU.MatrixSparseAddSparse;
			var wrapper = func(m, n, opA, opB, A.ToWrapper(), A.Format, B.ToWrapper(), B.Format, α, β, out SparseMatrixFormat target);
			// return
			C.Format = target;
			C.Pointer.ReplaceBy(wrapper.Values);
			C.RowPointer.ReplaceBy(wrapper.Row);
			C.ColumnPointer.ReplaceBy(wrapper.Column);
		}

		/// <summary>
		/// Compute $C = \alpha A^\text{opA} B^\text{opB} + \beta C$ for sparse matrices.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="α">scalar of type <typeparamref name="T"/>, cannot be zero</param>
		/// <param name="A">the <see cref="SparseMatrix{T}"/> A</param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="C"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B</param>
		/// <param name="opA">operation to <paramref name="A"/></param>
		/// <param name="opB">operation to <paramref name="B"/></param>
		/// <param name="C">the input and output <see cref="SparseMatrix{T}"/> C, will be in-place altered</param>
		/// <exception cref="ArgumentNullException">if any of the matrices is null</exception>
		/// <exception cref="ArgumentException">if the matrices are not compatible in size or both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void MatrixSparseMultiplySparse<T>(SparseMatrix<T> A, SparseMatrix<T> B, SparseMatrix<T> C, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentException(Resource.ParaCannotZero, nameof(α));
			if (A is null || A == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (C is null || (!β.Equals(Scalars<T>.Zero) && C == PureArray<T>.EmptySpMat))
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);

			bool onHost;
			int m, n;
			if (β.Equals(Scalars<T>.Zero))
			{
				onHost = CudaCSharpHelpers.CheckOnHost(A, B);
				C = new SparseMatrix<T>(0, 0, 0, SparseMatrixFormat.CSR, A.OnHost);
				m = A.IntRows; n = B.IntCols;
			}
			else
			{
				onHost = CudaCSharpHelpers.CheckOnHost(A, B, C);
				m = C.IntRows; n = C.IntCols;
			}
			// simplify op
			opA = opA.CheckOP(A);
			opB = opB.CheckOP(B);
			// check shape of D, A & D, B
			int p = opA == MatrixOperation.None ? A.IntRows : A.IntCols;
			int q = opA == MatrixOperation.None ? A.IntCols : A.IntRows;
			int r = opB == MatrixOperation.None ? B.IntRows : B.IntCols;
			int s = opB == MatrixOperation.None ? B.IntCols : B.IntRows;
			if (q != r)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(opB)}({nameof(B)}) do not match: {q}!={r}.");
			if (p != m)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(C)} do not match: {p}!={m}.", nameof(A));
			if (s != n)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opB)}({nameof(B)}) and {nameof(C)} do not match: {s}!={n}.", nameof(B));
			int k = q;

			var func = onHost ? new ISparse.DelegateMatrixSparseMultiplySparse<T>(CPU.MatrixSparseMultiplySparse) : GPU.MatrixSparseMultiplySparse;
			var wrapper = func(m, n, k, opA, opB, A.ToWrapper(), A.Format, B.ToWrapper(), B.Format, C.ToWrapper(), C.Format, α, β, out SparseMatrixFormat target);
			// return
			C.Format = target;
			C.Pointer.ReplaceBy(wrapper.Values);
			C.RowPointer.ReplaceBy(wrapper.Row);
			C.ColumnPointer.ReplaceBy(wrapper.Column);
		}

		/// <summary>
		/// Compute $C = \alpha A B + \beta C$ for sparse matrix <paramref name="B"/> and dense matrices <paramref name="A"/> &amp; <paramref name="C"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the input <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B with <see cref="SparseMatrixFormat.CSC"/></param>
		/// <param name="C">the input/output <see cref="DenseMatrix{T}"/> C</param>
		/// <param name="opA">operation to <paramref name="A"/></param>
		/// <param name="opB">operation to <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">if any of the matrices is null</exception>
		/// <exception cref="ArgumentException">if the matrices are not compatible in size or <paramref name="α"/> OR <paramref name="β"/> is 0</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <remarks>If <paramref name="A"/> is Hermitian and the lower part of it are not stored, this part will be used in this method. Hence you need fill it manually (<see cref="DenseMatrix{T}.CopyUpperToLower"/>) before calling this method.</remarks>
		public static void MatrixDenseMultiplySparse<T>(DenseMatrix<T> A, SparseMatrix<T> B, DenseMatrix<T> C, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (C is null || C == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentException(Resource.ParaCannotZero, nameof(α));
			// simplify op
			opA = opA.CheckOP(A);
			opB = opB.CheckOP(B);
			// check shape of D, A & D, B
			int m = C.IntRows, n = C.IntCols;
			int p = opA == MatrixOperation.None ? A.IntRows : A.IntCols;
			int q = opA == MatrixOperation.None ? A.IntCols : A.IntRows;
			int r = opB == MatrixOperation.None ? B.IntRows : B.IntCols;
			int s = opB == MatrixOperation.None ? B.IntCols : B.IntRows;
			if (q != r)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(opB)}({nameof(B)}) do not match: {q}!={r}.");
			if (p != m)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(C)} do not match: {p}!={m}.", nameof(A));
			if (s != n)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opB)}({nameof(B)}) and {nameof(C)} do not match: {s}!={n}.", nameof(B));
			int k = q;
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B, C);

			var func = onHost ? new ISparse.DelegateMatrixDenseMultiplySparse<T>(CPU.MatrixDenseMultiplySparse) : GPU.MatrixDenseMultiplySparse;
			func(m, n, k, opA, opB, A.Pointer, A.IntLeadDim, B.ToWrapper(), B.Format, C.Pointer, C.IntLeadDim, α, β);
		}

		/// <summary>
		/// Compute $C = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C$ for sparse matrix <paramref name="A"/> and dense matrices <paramref name="B"/> &amp; <paramref name="C"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the input <see cref="SparseMatrix{T}"/> A with <see cref="SparseMatrixFormat.CSR"/>/<see cref="SparseMatrixFormat.Coordinated"/></param>
		/// <param name="opA">the <see cref="MatrixOperation"/> to apply on <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> B</param>
		/// <param name="opB">the <see cref="MatrixOperation"/> to apply on <paramref name="B"/></param>
		/// <param name="C">the input/output <see cref="DenseMatrix{T}"/> C</param>
		/// <exception cref="ArgumentNullException">if any of the matrices is null</exception>
		/// <exception cref="ArgumentException">if the matrices are not compatible in size or <paramref name="α"/> OR <paramref name="β"/> is 0</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of CUDA Sparse methods <c>csrgeam</c> return error status</exception>
		/// <remarks>If <paramref name="B"/> is Hermitian and the lower part of it are not stored, this part will be used in this method. Hence you need fill it manually (<see cref="DenseMatrix{T}.CopyUpperToLower"/>) before calling this method.</remarks>
		public static void MatrixSparseMultiplyDense<T>(SparseMatrix<T> A, DenseMatrix<T> B, DenseMatrix<T> C, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (C is null || C == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(C), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentException(Resource.ParaCannotZero, nameof(α));
			// simplify op
			opA = opA.CheckOP(A);
			opB = opB.CheckOP(B);
			// check shape of D, A & D, B
			int m = C.IntRows, n = C.IntCols;
			int p = opA == MatrixOperation.None ? A.IntRows : A.IntCols;
			int q = opA == MatrixOperation.None ? A.IntCols : A.IntRows;
			int r = opB == MatrixOperation.None ? B.IntRows : B.IntCols;
			int s = opB == MatrixOperation.None ? B.IntCols : B.IntRows;
			if (q != r)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(opB)}({nameof(B)}) do not match: {q}!={r}.");
			if (p != m)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opA)}({nameof(A)}) and {nameof(C)} do not match: {p}!={m}.", nameof(A));
			if (s != n)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(opB)}({nameof(B)}) and {nameof(C)} do not match: {s}!={n}.", nameof(B));
			int k = q;
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B, C);

			var func = onHost ? new ISparse.DelegateMatrixSparseMultiplyDense<T>(CPU.MatrixSparseMultiplyDense) : GPU.MatrixSparseMultiplyDense;
			func(m, n, k, opA, opB, A.ToWrapper(), A.Format, B.Pointer, B.IntLeadDim, C.Pointer, C.IntLeadDim, α, β);
		}
		#endregion


		#region customs

		#region int API
		/// <summary>
		/// Find the min and max values of a integer device array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <returns>min and max values</returns>
		public static (int min, int max) IndexMinMax(Storage<int> indexPtr, long N)
		{
			if (indexPtr is null)
				throw new ArgumentNullException(nameof(indexPtr));

			var onHost = CudaCSharpHelpers.CheckOnHost(indexPtr);

			var func = onHost ? new ISparse.DelegateIndexMinMax(CPU.IndexMinMax) : GPU.IndexMinMax;
			return func(indexPtr, N);
		}

		/// <summary>
		/// Find the max value of a integer device array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <returns>max value</returns>
		public static int IndexMax(Storage<int> indexPtr, long N)
		{
			if (indexPtr is null)
				throw new ArgumentNullException(nameof(indexPtr));

			var onHost = CudaCSharpHelpers.CheckOnHost(indexPtr);

			var func = onHost ? new ISparse.DelegateIndexMax(CPU.IndexMax) : GPU.IndexMax;
			return func(indexPtr, N);
		}

		/// <summary>
		/// Find the index of the target value in a integer device array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <param name="toFind">the target value to find</param>
		/// <returns>index of target value, -1 if not found</returns>
		public static int IndexFind(Storage<int> indexPtr, long N, int toFind)
		{
			if (indexPtr is null)
				throw new ArgumentNullException(nameof(indexPtr));

			var onHost = CudaCSharpHelpers.CheckOnHost(indexPtr);

			var func = onHost ? new ISparse.DelegateIndexFind(CPU.IndexFind) : GPU.IndexFind;
			return func(indexPtr, N, toFind);
		}

		/// <summary>
		/// Find the index of the target value as a (inclusive) lower / (exclusive) upper bound in a integer device array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <param name="value">the target value to find</param>
		/// <param name="lowerBound">regard <paramref name="value"/> as lower bound or upper bound</param>
		/// <returns>index of target value, -1 if not found</returns>
		public static int IndexLowerUpperBound(Storage<int> indexPtr, long N, int value, bool lowerBound = true)
		{
			if (indexPtr is null)
				throw new ArgumentNullException(nameof(indexPtr));

			var onHost = CudaCSharpHelpers.CheckOnHost(indexPtr);

			var func = onHost ? new ISparse.DelegateIndexLowerUpperBound(CPU.IndexLowerUpperBound) : GPU.IndexLowerUpperBound;
			return func(indexPtr, N, value, lowerBound);
		}

		/// <summary>
		/// Fill a device array of type <see cref="int"/> with a range.
		/// </summary>
		/// <param name="array">array pointer</param>
		/// <param name="length">length of array</param>
		/// <param name="start">start of range</param>
		/// <param name="step">step of range</param>
		public static void IndexFillWithRange(Storage<int> array, long length, int start, int step)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			var onHost = CudaCSharpHelpers.CheckOnHost(array);

			var func = onHost ? new ISparse.DelegateIndexFillWithRange(CPU.IndexFillWithRange) : GPU.IndexFillWithRange;
			func(array, length, start, step);
		}

		/// <summary>
		/// Point-wise add the <paramref name="scalar"/> to the <paramref name="array"/>. 
		/// </summary>
		/// <param name="array">the <see cref="Storage{T}"/> to be added</param>
		/// <param name="scalar">the scalar <see cref="int"/> to add</param>
		/// <param name="N">length of <paramref name="array"/></param>
		public static void IndexAddScalar(Storage<int> array, int scalar, long N)
		{
			var onHost = CudaCSharpHelpers.CheckOnHost(array);

			var func = onHost ? new ISparse.DelegateIndexAddScalar(CPU.IndexAddScalar) : GPU.IndexAddScalar;
			func(array, scalar, N);
		}
		#endregion

		/// <summary>
		/// Convert the sparse vector's index array to / from a COOC sparse matrix's index arrays by mod and quotient. The value array will not be copied.
		/// </summary>
		/// <param name="vec">the input <see cref="SparseVector{T}"/></param>
		/// <param name="mat">the output <see cref="SparseMatrix{T}"/></param>
		/// <param name="toCOO">convert to COOC matrix or backward</param>
		/// <returns>The new row index pointer and column index pointer</returns>
		/// <exception cref="ArgumentException">if <paramref name="mat"/> is not of <see cref="SparseMatrixFormat.COOC"/></exception>
		/// <exception cref="StatusException">if the calculation of custom CUDA method <c>indexToCOO</c> returns error status</exception>
		/// <exception cref="OverflowException">if leading dimension cannot be casted into <see cref="int"/> without loss</exception>
		/// <remarks>Since all <see cref="SparseVector{T}"/> are maintained sorted, the result arrays will be sorted in the column first order, which can be converted to <see cref="SparseMatrixFormat.CSC"/> format.</remarks>
		public static void VectorToFromCOOMatrix<T>(SparseVector<T> vec, SparseMatrix<T> mat, bool toCOO) where T : struct, IComparable<T>
		{
			if (mat is null)
				throw new ArgumentNullException(nameof(mat), Resource.ArrayCannotNull);
			if (vec is null)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			if ((mat.Format & SparseMatrixFormat.Coordinated) != 0)
				throw new ArgumentException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.Coordinated), nameof(mat));
			if (vec.NonZero != mat.NonZero)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(vec));
			var onHost = CudaCSharpHelpers.CheckOnHost(vec, mat);

			var func = onHost ? new ISparse.DelegateMatrixVectorCOOToFromSparseIndex(CPU.MatrixVectorCOOToFromSparseIndex) : GPU.MatrixVectorCOOToFromSparseIndex;
			func(vec.NonZero, vec.IndexPointer, mat.RowPointer, mat.ColumnPointer, mat.IntRows, toCOO);
		}

		/// <summary>
		/// Prune a <see cref="DenseVector{T}"/> to a new <see cref="SparseVector{T}"/> with absolute values or equal to less than <paramref name="threshold"/> regarding as zeros.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="v">the input <see cref="DenseVector{T}"/></param>
		/// <param name="threshold"><c>abs(value)</c> less than <paramref name="threshold"/> will be regarded as zero</param>
		/// <returns>A new <see cref="SparseVector{T}"/> containing the pruned result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="v"/> is null</exception>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="threshold"/> is negative</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static SparseVector<T> VectorDenseToSparse<T>(DenseVector<T> v, float threshold = 0) where T : struct, IComparable<T>
		{
			if (v is null || v == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(v), Resource.ArrayCannotNull);
			if (threshold < 0)
				throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resource.ParaCannotNegative);
			var onHost = CudaCSharpHelpers.CheckOnHost(v);

			var func = onHost ? new ISparse.DelegateVectorDenseToSparse<T>(CPU.VectorDenseToSparse) : GPU.VectorDenseToSparse;
			var spvec = func(v.Pointer, checked((int)v.Length), threshold);
			return new SparseVector<T>(v.Length, spvec.Values, spvec.Indices);
		}

		/// <summary>
		/// Calculate sparse vector point-wise multiply or divide dense vector: $\vec{v} = \{\vec{v}_i \vec{w}_i\}_i$ or $\vec{v} = \{\vec{v}_i / \vec{w}_i\}_i$
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="v"></param>
		/// <param name="w"></param>
		/// <param name="multiply"></param>
		/// <exception cref="ArgumentNullException">if <paramref name="v"/> or <paramref name="w"/> is null</exception>
		/// <exception cref="ArgumentException">if the vectors the dense vector is too short</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public static void VectorSparseDensePointWiseMultiplyDivide<T>(SparseVector<T> v, DenseVector<T> w, bool multiply) where T : struct, IComparable<T>
		{
			if (v is null || v == PureArray<T>.EmptySpVec)
				throw new ArgumentNullException(nameof(v), Resource.ArrayCannotNull);
			if (w is null || w == PureArray<T>.EmptyDnVec)
				throw new ArgumentNullException(nameof(w), Resource.ArrayCannotNull);
			if (v.LastIndex > w.LastIndex)
				throw new ArgumentException(Resource.CannotOperate + "dense vector is too short", nameof(w));
			var onHost = CudaCSharpHelpers.CheckOnHost(v, w);

			var func = onHost ? new ISparse.DelegateVectorSparsePointWiseMultiplyDivideDense<T>(CPU.VectorSparsePointWiseMultiplyDivideDense) : GPU.VectorSparsePointWiseMultiplyDivideDense;
			func(v.ToWrapper(), w.Pointer, multiply);
		}

		/// <summary>
		/// Add two sparse vectors together to a new sparse vector. The temporary memory is proportional to the sum of non-zero values of <paramref name="a"/> and <paramref name="b"/>.
		/// </summary>
		/// <param name="a">one of the input <see cref="SparseVector{T}"/></param>
		/// <param name="b">one of the input <see cref="SparseVector{T}"/></param>
		/// <param name="lengthOverride">the output <see cref="SparseVector{T}"/>'s length, only positive number is considered as an override</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <returns>A new <see cref="SparseVector{T}"/> containing the addition result.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="a"/> or <paramref name="b"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="lengthOverride"/> is too small</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of custom CUDA method <c>vecPrune</c> returns error status</exception>
		public static SparseVector<T> VectorSparseAddSparse<T>(SparseVector<T> a, SparseVector<T> b, long lengthOverride = 0) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptySpVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			if (b is null || b == PureArray<T>.EmptySpVec)
				throw new ArgumentNullException(nameof(b), Resource.ArrayCannotNull);
			if (lengthOverride <= 0)
				lengthOverride = Math.Max(a.Length, b.Length);
			if (lengthOverride < Math.Max(a.Length, b.Length))
				throw new ArgumentOutOfRangeException(nameof(lengthOverride));
			var onHost = CudaCSharpHelpers.CheckOnHost(a, b);

			var func = onHost ? new ISparse.DelegateVectorSparseAddSparse<T>(CPU.VectorSparseAddSparse) : GPU.VectorSparseAddSparse;
			var spvec = func(a.ToWrapper(), b.ToWrapper());
			return new SparseVector<T>(a.Length, spvec.Values, spvec.Indices);
		}

		/// <summary>
		/// Get the non-empty row/column indices of a given CSR/CSC sparse matrix.
		/// </summary>
		/// <param name="M">the <see cref="SparseMatrix{T}"/> to get non-empty row/column indices</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <returns>The <see cref="Storage{T}"/> pointing to the non-empty row/column indices array <c>nnei</c>.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="M"/> is null</exception>
		/// <exception cref="ArgumentException">if the vectors are not on device memory</exception>
		/// <exception cref="NotSupportedException">if <paramref name="M"/> is not a sparse matrix in CSR or CSC format</exception>
		/// <exception cref="StatusException">if the calculation of custom CUDA method <c>CSRGetNer</c> returns error status</exception>
		public static Storage<int> SparseMatrixGetNEI<T>(SparseMatrix<T> M) where T : struct, IComparable<T>
		{
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if ((M.Format & SparseMatrixFormat.Compressed) == 0)
				throw new NotSupportedException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.Compressed));
			var onHost = CudaCSharpHelpers.CheckOnHost(M);

			var func = onHost ? new ISparse.DelegateMatrixSparseCompressedGetNEI<T>(CPU.MatrixSparseCompressedGetNEI) : GPU.MatrixSparseCompressedGetNEI;
			var nei = func(M.IntRows, M.IntCols, M.ToWrapper(), M.Format == SparseMatrixFormat.CSR);
			return nei;
		}

		/// <summary>
		/// Fill a sparse matrix with identity.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="matrix">the <see cref="SparseMatrix{T}"/> to fill with identity matrix</param>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of custom CUDA method <c>fillOne</c> returns error status</exception>
		public static void FillIdentity<T>(SparseMatrix<T> matrix) where T : struct, IComparable<T>
		{
			if (matrix is null || matrix == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(matrix), Resource.ArrayCannotNull);
			if (matrix.NonZero != matrix.NRows || matrix.NonZero != matrix.NCols)
				throw new ArgumentException(Resource.MatMustSquare, nameof(matrix));
			var onHost = CudaCSharpHelpers.CheckOnHost(matrix);

			var func = onHost ? new ISparse.DelegateMatrixFillIdentity<T>(CPU.MatrixFillIdentity) : GPU.MatrixFillIdentity;
			func(matrix.ToWrapper(), matrix.Format);
		}

		/// <summary>
		/// Compute sparse vectors' outer product $M = \vec{a} \vec{b}^H$ where $M$ is a <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.COOC"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="a">the input <see cref="SparseVector{T}"/></param>
		/// <param name="b">the input <see cref="SparseVector{T}"/></param>
		/// <param name="M">the output <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.COOC"/></param>
		/// <param name="conjugateB">conjugate on <paramref name="b"/> or not if <typeparamref name="T"/> is a complex type</param>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of custom CUDA method <c>fillOne</c> returns error status</exception>
		public static void VectorSparseOuterSparse<T>(SparseVector<T> a, SparseVector<T> b, SparseMatrix<T> M, bool conjugateB = true) where T : struct, IComparable<T>
		{
			if (a is null || a == PureArray<T>.EmptySpVec)
				throw new ArgumentNullException(nameof(a), Resource.ArrayCannotNull);
			if (b is null || b == PureArray<T>.EmptySpVec)
				throw new ArgumentNullException(nameof(b), Resource.ArrayCannotNull);
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (M.Format != SparseMatrixFormat.COOC)
				throw new NotSupportedException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.COOC));
			var onHost = CudaCSharpHelpers.CheckOnHost(a, b, M);

			var func = onHost ? new ISparse.DelegateVectorSparseOuterSparse<T>(CPU.VectorSparseOuterSparse) : GPU.VectorSparseOuterSparse;
			func(a.ToWrapper(), b.ToWrapper(), M.ToWrapper(), conjugateB);
		}

		/// <summary>
		/// Compute $M = A \otimes B$ where all three matrices are <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.Coordinated"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="A">input <see cref="SparseMatrix{T}"/> A</param>
		/// <param name="B">input <see cref="SparseMatrix{T}"/> B</param>
		/// <param name="M">output <see cref="SparseMatrix{T}"/> M</param>
		/// <param name="targetCOOC">the result matrix a <see cref="SparseMatrixFormat.COOC"/> or <see cref="SparseMatrixFormat.COOR"/></param>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of custom CUDA method <c>fillOne</c> returns error status</exception>
		/// <exception cref="OverflowException">if the number of non-zeros, leading and second dimensions of <paramref name="M"/> cannot be casted to <see cref="int"/> without loss</exception>
		public static void SparseMatrixKronecker<T>(SparseMatrix<T> A, SparseMatrix<T> B, SparseMatrix<T> M, bool targetCOOC = true) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if ((A.Format & SparseMatrixFormat.Coordinated) == 0)
				throw new NotSupportedException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.Coordinated));
			if (B is null || B == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if ((B.Format & SparseMatrixFormat.Coordinated) == 0)
				throw new NotSupportedException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.Coordinated));
			if (M is null || M == PureArray<T>.EmptySpMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			var targetFormat = targetCOOC ? SparseMatrixFormat.COOC : SparseMatrixFormat.COOR;
			if (M.Format != targetFormat)
				throw new NotSupportedException(string.Format(Resource.Culture, Resource.SpMatMustFormat, targetFormat));
			if (M.NRows != A.NRows * B.NRows || M.NCols != A.NCols * B.NCols || M.NonZero != A.NonZero * B.NonZero)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(M));
			var onHost = CudaCSharpHelpers.CheckOnHost(A, B, M);

			var func = onHost ? new ISparse.DelegateSparseMatrixKronecker<T>(CPU.SparseMatrixKronecker) : GPU.SparseMatrixKronecker;
			func(A.IntRows, A.IntCols, B.IntRows, B.IntCols, A.ToWrapper(), B.ToWrapper(), M.ToWrapper(), targetCOOC);
		}
		#endregion
	}
}
