using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Cuda.Storage;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NM = Althea.Backend.Cuda.LinearAlgebra.Sparse.NativeMethods;
using NMC = Althea.Backend.Cuda.LinearAlgebra.Sparse.CustomNativeMethods;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse;

public unsafe partial class Api
{
	#region vector computation
	/// <inheritdoc/>
	public virtual bool VectorSparseAddToDense<T, TInd, TS1, TS2, TSInd>(T α, ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (strideY != 1)
			return false;
		if (α == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
		using var dn = DenseVectorWrapper.Create<T, TS2>(this, y, out bool success);
		if (!success) return false;
		using var sp = SparseVectorWrapper.Create(this, x, out success);
		if (!success) return false;
		T one = T.One;
		return NM.cusparseAxpby(this.cusparseHandle, &α, sp, &one, dn).Check();
	}

	/// <inheritdoc/>
	public virtual bool VectorSparseDotDense<T, TInd, TS1, TS2, TSInd>(bool conjX, ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY, out T dot) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
	{
		dot = default;
		if (strideY != 1)
			return false;
		using var dn = DenseVectorWrapper.Create<T, TS2>(this, y, out bool success);
		if (!success) return false;
		using var sp = SparseVectorWrapper.Create(this, x, out success);
		if (!success) return false;
		var op = conjX ? CuBlasOperation.ConjugateTranspose : CuBlasOperation.None;
		var type = T.Type.ToCudaDataType().ToComputeType();
		T res = default;
		if (!NM.cusparseSpVV_bufferSize(this.cusparseHandle, op, sp, dn, &res, type, out var bufSize).Check())
			return false;
		using var buf = CudaBuffer.Create(bufSize);
		if (!NM.cusparseSpVV(this.cusparseHandle, op, sp, dn, &res, type, buf).Check())
			return false;
		dot = res;
		return true;
	}

	/// <inheritdoc/>
	public virtual bool VectorSparseAddSparse<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(T α, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd1 : class, IStorage<TInd1, TSInd1> where TSInd2 : class, IStorage<TInd2, TSInd2> where TSInd3 : class, IStorage<TInd3, TSInd3>
	{
		if (sizeof(TInd1) != sizeof(TInd2))
			return false;
		if (x.Size.Length != 1 || y.Size.Length != 1)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
		if (x.Size[0] != y.Size[0])
			throw new ArgumentException(Resources.ParameterError.NotSameSize);
		if (x.Format != SparseFormat.VectorCooFormat || y.Format != SparseFormat.VectorCooFormat || (target.Format & SparseFormat.VectorCooFormat) == SparseFormat.None || x.DefaultValue != T.Zero || y.DefaultValue != T.Zero || target.DefaultValue != T.Zero)
			return false;
		if (!GetPointer(this, x, out T* px, out var ppx, out var nnzx))
			return false;
		if (!GetPointer(this, y, out T* py, out var ppy, out var nnzy))
			return false;
		if (target.Size.Length is not 0 and not 1)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
		if (target.IndexStorages.Length is not 0 and not 1 || target.ValueStorages.Length is not 0 and not 1)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

		long bufferSize = sizeof(TInd1) == sizeof(int) ? NMC.vecSpAddBuffer_i32(T.Type, nnzx, nnzy) : NMC.vecSpAddBuffer_i64(T.Type, nnzx, nnzy);
		if (bufferSize < 0)
			return false;
		using var buffer = ArrayPoolBuffers.Create<byte>(bufferSize);
		long nnz = sizeof(TInd1) == sizeof(int) ? NMC.vecSpAddNnz_i32(T.Type, (int*)ppx, px, nnzx, (int*)ppy, py, nnzy, &α, buffer) : NMC.vecSpAddNnz_i64(T.Type, (long*)ppx, px, nnzx, (long*)ppy, py, nnzy, &α, buffer);
		bool hasError = true;
		using var valOut = target.ValueStorages.CreateFromFirst<T, TS3>(nnz, ref hasError);
		if (valOut.Invalid) return false;
		using var idxOut = target.IndexStorages.CreateFromFirst<TInd3, TSInd3>(nnz, ref hasError);
		if (idxOut.Invalid) return false;
		var err = sizeof(TInd1) == sizeof(int) ? NMC.vecSpAddCal_i32(T.Type, buffer, nnzx + nnzy, nnz, idxOut.As<int>(), valOut) : NMC.vecSpAddCal_i64(T.Type, buffer, nnzx + nnzy, nnz, idxOut.As<long>(), valOut);
		if (err != 0) return false;
		target.SetValues(x.Size[0], valOut, idxOut);
		target.Format = SparseFormat.VectorCooFormat;
		target.DefaultValue = T.Zero;
		hasError = false;
		return true;
	}
	#endregion

	#region vector and matrix computation
	/// <inheritdoc/>
	public virtual bool MatrixSparseMultiplyVectorDense<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation op, T α, ISparseArray<T, TInd, TS1, TSInd> M, TS2 x, long strideX, T β, TS3 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (α == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
		bool conjY = op == MatrixOperation.Conjugate;
		if (strideX != 1 || strideY != 1 || (conjY && β != T.Zero))
			return false;
		if (!GetPointer(this, y, out T* py, out var ny))
			return false;
		using var spm = SparseMatrixWrapper.Create(this, M, out bool success);
		if (!success) return false;
		using var dnx = DenseVectorWrapper.Create<T, TS2>(this, x, out success);
		if (!success) return false;
		using var dny = DenseVectorWrapper.Create(py, ny, out success);
		if (!success) return false;
		var opA = op.ToCuda();
		var type = T.Type.ToCudaDataType().ToComputeType();
		if (!NM.cusparseSpMV_bufferSize(this.cusparseHandle, opA, &α, spm, dnx, &β, dny, type, SparseMVAlgorithm.Default, out var bufSize).Check())
			return false;
		using var buf = CudaBuffer.Create(bufSize);
		if (!NM.cusparseSpMV(this.cusparseHandle, opA, &α, spm, dnx, &β, dny, type, SparseMVAlgorithm.Default, buf).Check())
			return false;
		if (conjY)
			Dense.Conjugater.Conjugate(py, ny, 1);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool VectorSparseOuter<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(bool conjY, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd1 : class, IStorage<TInd1, TSInd1> where TSInd2 : class, IStorage<TInd2, TSInd2> where TSInd3 : class, IStorage<TInd3, TSInd3>
	{
		if ((target.Format & SparseFormat.MatrixCooFormat) == SparseFormat.None)
			return false;
		if (!GetPointer(this, x, out T* px, out var ppx, out var nnzx))
			return false;
		if (!GetPointer(this, y, out T* py, out var ppy, out var nnzy))
			return false;
		if (sizeof(TInd3) != sizeof(TInd2) || sizeof(TInd2) != sizeof(TInd1))
			return false;
		if (target.ValueStorages.Length is not 0 and not 1 || target.IndexStorages.Length is not 0 and not 2)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

		if (NMC.spVecOuterCheck(T.Type) < 0)
			return false;
		long nnz = nnzx * nnzy;
		bool hasError = true;
		using var valOut = target.ValueStorages.CreateFromFirst<T, TS3>(nnz, ref hasError);
		if (valOut.Invalid) return false;
		using var rowOut = target.IndexStorages.CreateFromFirst<TInd3, TSInd3>(nnz, ref hasError);
		if (rowOut.Invalid) return false;
		using var colOut = target.IndexStorages.CreateFromSecond<TInd3, TSInd3>(nnz, ref hasError);
		if (colOut.Invalid) return false;
		var err = sizeof(TInd1) == sizeof(int) ? NMC.spVecOuter_i32(T.Type, px, (int*)ppx, nnzx, py, (int*)ppy, nnzy, valOut, rowOut.As<int>(), colOut.As<int>(), conjY) : NMC.spVecOuter_i64(T.Type, px, (long*)ppx, nnzx, py, (long*)ppy, nnzy, valOut, rowOut.As<long>(), colOut.As<long>(), conjY);
		if (err != 0) return false;
		target.SetValues(x.Size[0], y.Size[0], valOut, rowOut, colOut);
		target.Format = SparseFormat.MatrixCocFormat;
		target.DefaultValue = T.Zero;
		hasError = false;
		return true;
	}
	#endregion

	#region matrix computation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool OpConvert<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(MatrixOperation opA, T scale, ISparseArray<T, TInd1, TS1, TSInd1> A, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
	{
		if ((A.Format & NM.SupportFormatIncludeBlocked) == SparseFormat.None || (target.Format & NM.SupportFormatIncludeBlocked) == SparseFormat.None)
			return false;
		var converter = GetConverter(this, A, target);
		if (!opA.CanInPlace())
		{
			// shortcut
			if ((target.Format & A.Format.WithTransposedMajor) != SparseFormat.None)
			{
				if (A.Format.BlockType != SparseFormat.Blocking.Element)
					return false; // do not support transpose blocks yet
				if (typeof(TS2) != typeof(TS1) || typeof(TSInd2) != typeof(TSInd1))
					return false;
				target.Format = A.Format.WithComplicatedBlocking;
				target.SetValues(A.Size[1], A.Size[0], (A.ValueStorages[0] as TS2)!, (A.IndexStorages[1] as TSInd2)!, (A.IndexStorages[0] as TSInd2)!);
				return true;
			}
			if (A.Size[0] != A.Size[1])
				return false; // do not support transpose and change format for non-rectangular matrix
			var finalFormat = A.Format.IsRowMajor ? target.Format.WithRowMajor : target.Format.WithColumnMajor;
			if ((target.Format & finalFormat) == SparseFormat.None)
				return false;
			// intermediate format
			target.Format = A.Format.IsRowMajor ? target.Format.WithColumnMajor : target.Format.WithRowMajor;
		}
		if (!converter.Convert(A, ref target))
			return false;
		if (opA.HasConjugate())
		{
			bool hasError = true;
			long n = A.ValueStorages[0].Length;
			if (target.ValueStorages[0] == A.ValueStorages[0])
			{
				GetPointer(A.ValueStorages[0], out T* pA, out _);
				using var valOut = n.Create<T, TS2>(ref hasError);
				if (valOut.Invalid) return false;
				if (!Dense.Conjugater.Conjugate(n, pA, 1, valOut, 1))
					return false;
				target.SetValues(valOut, target.IndexStorages[0], target.IndexStorages[1]);
			}
			else
			{
				GetPointer(target.ValueStorages[0], out T* ptr, out _);
				if (!Dense.Conjugater.Conjugate(ptr, n, 1))
					return false;
			}
			hasError = false;
		}
		if (!opA.CanInPlace())
		{
			target.Format = target.Format.IsRowMajor ? target.Format.WithColumnMajor : target.Format.WithRowMajor;
			// cannot be blocked sparse matrix
			target.SetValues(target.ValueStorages[0], target.IndexStorages[1], target.IndexStorages[0]);
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool MatrixSparseAddSparse<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(MatrixOperation opA, MatrixOperation opB, T α, ISparseArray<T, TInd1, TS1, TSInd1>? A, T β, ISparseArray<T, TInd2, TS2, TSInd2>? B, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>
	{
		if (target.ValueStorages.Length != 0 || target.IndexStorages.Length != 0)
			return false;
		if (A is not null && B is not null)
			return false;
		if (α == T.Zero)
			A = null;
		if (β == T.Zero)
			B = null;
		if (A is null && B is null)
			throw new ArgumentException(Resources.ParameterError.CannotAllNull);

		opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
		if (A is not null)
			return OpConvert(opA, α, A, ref target);
		else if (B is not null)
			return OpConvert(opB, β, B, ref target);
		else
			return false;
	}

	/// <inheritdoc/>
	public virtual bool MatrixSparseMultiplySparse<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(MatrixOperation opA, MatrixOperation opB, T α, ISparseArray<T, TInd1, TS1, TSInd1> A, ISparseArray<T, TInd2, TS2, TSInd2> B, T β, ISparseArray<T, TInd3, TS3, TSInd3>? C, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>
	{
		if (α == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
		if (sizeof(TInd1) != sizeof(int) || sizeof(TInd2) != sizeof(int) || sizeof(TInd3) != sizeof(int))
			return false;
		if (A.DefaultValue != T.Zero || B.DefaultValue != T.Zero || (C is not null && C.DefaultValue != T.Zero) || target.DefaultValue != T.Zero)
			return false;
		if (β == T.Zero)
			C = null;
		if ((target.Format & SparseFormat.MatrixCsrFormat) == SparseFormat.None ||
			A.Format != SparseFormat.MatrixCsrFormat || B.Format != SparseFormat.MatrixCsrFormat)
			return false;
		if (C is not null && (!C.Size.SequenceEqual(target.Size) || !C.ValueStorages.SequenceEqual(target.ValueStorages) || !C.IndexStorages.SequenceEqual(target.IndexStorages)))
			return false;
		opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
		long m = opA.CanInPlace() ? A.Size[0] : A.Size[1], n = opB.CanInPlace() ? B.Size[1] : B.Size[0];
		T* pcVal = null; TInd3* pcRow = null, pcCol = null; long nnzC = 0;
		if (C is not null && !GetPointer(this, C, out pcVal, out pcRow, out pcCol, out nnzC))
			return false;
		using var matA = SparseMatrixWrapper.Create(this, A, out bool success);
		if (!success) return false;
		using var matB = SparseMatrixWrapper.Create(this, B, out success);
		if (!success) return false;
		using var matC = SparseMatrixWrapper.Create(SparseFormat.MatrixCsrFormat, m, n, nnzC, pcVal, pcRow, pcCol, out success);
		if (!success) return false;
		// prepare
		if ((opA == MatrixOperation.Conjugate && opB == MatrixOperation.None) || (opA == MatrixOperation.None && opB == MatrixOperation.Conjugate))
			return false;
		if (opA == MatrixOperation.Conjugate && β != T.Zero)
		{
			if (!Dense.Conjugater.Conjugate(pcVal, nnzC, 1))
				return false;
		}
		CuBlasOperation copA = opA.ToCuda(), copB = opB.ToCuda();
		var type = T.Type.ToCudaDataType();
		using var descr = new SparseGemmDescriptor();
		long bufSize1 = 0, bufSize2 = 0;
		// work estimate
		if (!NM.cusparseSpGEMM_workEstimation(this.cusparseHandle, copA, copB, &α, matA, matB, &β, matC, type, SparseGemmAlgorithm.Default, descr, ref bufSize1).Check())
			return false;
		using var buf1 = CudaBuffer.Create(bufSize1, 0, false);
		if (!NM.cusparseSpGEMM_workEstimation(this.cusparseHandle, copA, copB, &α, matA, matB, &β, matC, type, SparseGemmAlgorithm.Default, descr, ref bufSize1, buf1).Check())
			return false;
		// compute
		if (!NM.cusparseSpGEMM_compute(this.cusparseHandle, copA, copB, &α, matA, matB, &β, matC, type, SparseGemmAlgorithm.Default, descr, ref bufSize2).Check())
			return false;
		using var buf2 = CudaBuffer.Create(bufSize2, 0, false);
		if (!NM.cusparseSpGEMM_compute(this.cusparseHandle, copA, copB, &α, matA, matB, &β, matC, type, SparseGemmAlgorithm.Default, descr, ref bufSize2, buf2).Check())
			return false;
		// copy
		if (!matC.GetSizes(out _, out _, out nnzC))
			return false;
		bool hasError = true;
		using var valOut = target.ValueStorages.CreateFromFirst<T, TS3>(nnzC, ref hasError);
		if (valOut.Invalid) return false;
		using var colOut = target.IndexStorages.CreateFromSecond<TInd3, TSInd3>(nnzC, ref hasError);
		if (colOut.Invalid) return false;
		using var rowOut = target.IndexStorages.CreateFromFirst<TInd3, TSInd3>(m + 1, ref hasError);
		if (rowOut.Invalid) return false;
		matC.SetPointers(SparseFormat.MatrixCsrFormat, valOut, rowOut, colOut);
		if (!NM.cusparseSpGEMM_copy(this.cusparseHandle, copA, copB, &α, matA, matB, &β, matC, type, SparseGemmAlgorithm.Default, descr).Check())
			return false;
		// final
		if (opA == MatrixOperation.Conjugate)
		{
			if (!Dense.Conjugater.Conjugate<T>(valOut, nnzC, 1))
				return false;
		}
		target.SetValues(m, n, valOut, rowOut, colOut);
		target.Format = SparseFormat.MatrixCsrFormat;
		hasError = false;
		return true;
	}

	/// <inheritdoc/>
	public virtual bool MatrixSparseMultiplyDense<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseArray<T, TInd, TS1, TSInd> A, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (α == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
		opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
		var (m, k) = opA.CanInPlace() ? (A.Size[0], A.Size[1]) : (A.Size[1], A.Size[0]);
		if (!GetPointer(this, B, opB, k, n, ldb, out T* pb))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pc))
			return false;
		if (!opB.CanInPlace())
			(k, n) = (n, k);
		using var matB = DenseMatrixWrapper.Create(pb, k, n, ldb, false, out bool success);
		if (!success) return false;
		using var matC = DenseMatrixWrapper.Create(pc, m, n, ldc, false, out success);
		if (!success) return false;
		using var matA = SparseMatrixWrapper.Create(this, A, out success);
		if (!success) return false;
		// prepare
		if ((opA == MatrixOperation.Conjugate && opB == MatrixOperation.None) || (opA == MatrixOperation.None && opB == MatrixOperation.Conjugate))
			return false;
		if (opA == MatrixOperation.Conjugate && β != T.Zero)
		{
			if (!Dense.Conjugater.Conjugate(pc, m, n, ldc))
				return false;
		}
		CuBlasOperation copA = opA.ToCuda(), copB = opB.ToCuda();
		var type = T.Type.ToCudaDataType().ToComputeType();
		// calculate
		// TODO: NM.cusparseSpMM_preprocess
		if (!NM.cusparseSpMM_bufferSize(this.cusparseHandle, copA, copB, &α, matA, matB, &β, matC, type, SparseMMAlgorithm.Default, out long bufSize).Check())
			return false;
		using var buf = CudaBuffer.Create(bufSize);
		if (!NM.cusparseSpMM(this.cusparseHandle, copA, copB, &α, matA, matB, &β, matC, type, SparseMMAlgorithm.Default, buf).Check())
			return false;
		// final
		if (opA == MatrixOperation.Conjugate)
		{
			if (!Dense.Conjugater.Conjugate(pc, m, n, ldc))
				return false;
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool MatrixDenseMultiplySparse<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation opA, MatrixOperation opB, long m, T α, TS1 A, long lda, ISparseArray<T, TInd, TS2, TSInd> B, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (α == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
		opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
		var (k, n) = opB.CanInPlace() ? (B.Size[0], B.Size[1]) : (B.Size[1], B.Size[0]);
		if (!GetPointer(this, A, opA, m, k, lda, out T* pa))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pc))
			return false;
		if (!opA.CanInPlace())
			(k, m) = (m, k);
		opB = opB.Transpose();
		using var matA = DenseMatrixWrapper.Create(pa, k, m, lda, true, out bool success);
		if (!success) return false;
		using var matC = DenseMatrixWrapper.Create(pc, n, m, ldc, true, out success);
		if (!success) return false;
		using var matB = SparseMatrixWrapper.Create(this, B, out success);
		if (!success) return false;
		// prepare
		if ((opA == MatrixOperation.Conjugate && opB == MatrixOperation.None) || (opA == MatrixOperation.None && opB == MatrixOperation.Conjugate))
			return false;
		if (opA == MatrixOperation.Conjugate && β != T.Zero)
		{
			if (!Dense.Conjugater.Conjugate(pc, m, n, ldc))
				return false;
		}
		CuBlasOperation copA = opA.ToCuda(), copB = opB.ToCuda();
		var type = T.Type.ToCudaDataType().ToComputeType();
		// calculate
		// TODO: NM.cusparseSpMM_preprocess
		if (!NM.cusparseSpMM_bufferSize(this.cusparseHandle, copA, copB, &α, matB, matA, &β, matC, type, SparseMMAlgorithm.Default, out long bufSize).Check())
			return false;
		using var buf = CudaBuffer.Create(bufSize);
		if (!NM.cusparseSpMM(this.cusparseHandle, copA, copB, &α, matB, matA, &β, matC, type, SparseMMAlgorithm.Default, buf).Check())
			return false;
		// final
		if (opA == MatrixOperation.Conjugate)
		{
			if (!Dense.Conjugater.Conjugate(pc, m, n, ldc))
				return false;
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool MatrixSparseKronecker<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(ISparseArray<T, TInd1, TS1, TSInd1> A, ISparseArray<T, TInd2, TS2, TSInd2> B, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>
	{
		if ((target.Format & SparseFormat.MatrixCocFormat) == SparseFormat.None ||
			(A.Format & SparseFormat.MatrixCooFormat) == SparseFormat.None || (B.Format & SparseFormat.MatrixCooFormat) == SparseFormat.None)
			return false; // not supported
		if (sizeof(TInd1) != sizeof(TInd2) || sizeof(TInd1) != sizeof(TInd3))
			return false;
		if (!GetPointer(this, A, out T* pa, out var pra, out var pca, out var nnza))
			return false;
		if (!GetPointer(this, B, out T* pb, out var prb, out var pcb, out var nnzb))
			return false;
		if (target.ValueStorages.Length is not 0 and not 1 || target.IndexStorages.Length is not 0 and not 2)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
		if (NMC.spVecOuterCheck(T.Type) < 0)
			return false;

		long nnz = nnza * nnzb;
		bool hasError = true;
		using var valOut = target.ValueStorages.CreateFromFirst<T, TS3>(nnz, ref hasError);
		if (valOut.Invalid) return false;
		using var rowOut = target.IndexStorages.CreateFromFirst<TInd3, TSInd3>(nnz, ref hasError);
		if (rowOut.Invalid) return false;
		using var colOut = target.IndexStorages.CreateFromSecond<TInd3, TSInd3>(nnz, ref hasError);
		if (colOut.Invalid) return false;
		var err = sizeof(TInd1) == sizeof(int) ? NMC.cooMatKron_i32(T.Type, pa, (int*)pra, (int*)pca, nnza, pb, (int*)prb, (int*)pcb, nnzb, B.Size[0], B.Size[1], valOut, rowOut.As<int>(), colOut.As<int>()) : NMC.cooMatKron_i64(T.Type, pa, (long*)pra, (long*)pca, nnza, pb, (long*)prb, (long*)pcb, nnzb, B.Size[0], B.Size[1], valOut, rowOut.As<long>(), colOut.As<long>());
		target.SetValues(A.Size[0] * B.Size[0], A.Size[1] * B.Size[1], valOut, rowOut, colOut);
		target.Format = SparseFormat.MatrixCocFormat;
		target.DefaultValue = T.Zero;
		hasError = false;
		return true;
	}
	#endregion

	#region unsupported
	bool IComputationAbstractApi.VectorSparseDotSparse<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(bool conjX, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, out T dot) { dot = default; return false; }
	bool IComputationAbstractApi.VectorSparsePointwiseMultiplyDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) => false;
	bool IComputationAbstractApi.VectorSparsePointwiseDivideDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) => false;
	bool IComputationAbstractApi.MatrixDenseMultiplyVectorSparse<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation op, T α, long m, TS2 M, long ldm, ISparseArray<T, TInd, TS1, TSInd> x, T β, TS3 y, long strideY) => false;
	bool IComputationAbstractApi.SparseMatrixGetDiag<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, long k, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> vector) => false;
	bool IComputationAbstractApi.SparseMatrixSetDiag<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd2, TS2, TSInd2> matrix, long k, ISparseArray<T, TInd1, TS1, TSInd1> vector) => false;
	bool IComputationAbstractApi.MatrixDenseAddSparse<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation opA, MatrixOperation opB, T α, TS1? A, long lda, T β, ISparseArray<T, TInd, TS2, TSInd> B, TS3 C, long ldc) where TS1 : class => false;
	bool IComputationAbstractApi.MatrixSparseMultiplySparse<T, TInd1, TInd2, TS1, TS2, TS3, TSInd1, TSInd2>(MatrixOperation opA, MatrixOperation opB, T α, ISparseArray<T, TInd1, TS1, TSInd1> A, ISparseArray<T, TInd2, TS2, TSInd2> B, T β, TS3 C, long ldc) => false;
	#endregion
}
