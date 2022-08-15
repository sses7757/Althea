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

#region buffer
internal unsafe static class SignalErrorBufferExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<T, TS> Create<T, TS>(this long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!Check<T, TS>())
			return default;
		TS val = TS.Create(stackalloc[] { size });
		T* ptr = val.GetPointerDirect<T, TS>();
		return new(val, ptr, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<U, TS> Create<T, TS, U>(this long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where U : unmanaged
	{
		if (!Check<T, TS>())
			return default;
		TS val = TS.Create(stackalloc[] { size });
		T* ptr = val.GetPointerDirect<T, TS>();
		return new(val, ptr, ref hasError);
	}
}
#endregion

/// <summary>
/// The CUDA back-end of the sparse linear algebra <see cref="IConversionAbstractApi"/>, <see cref="IComputationAbstractApi"/> and <see cref="IIndexOperationAbstractApi"/> that utilizes cuSPARSE and custom CUDA functions.
/// </summary>
/// <remarks>CUDA stream and blocked-ELL sparse matrix format are not supported yet but can be easily added.</remarks>
public unsafe partial class Api : IBindedDevice, IConversionAbstractApi, IComputationAbstractApi, IIndexOperationAbstractApi
{
	#region basic
	/// <summary>
	/// The actual CUDA library handle used in its API calls
	/// </summary>
	protected readonly IntPtr cusparseHandle;

	/// <inheritdoc/>
	public int BindedDeviceID { get; }

	/// <summary>
	/// Get or set a <see cref="bool"/> to indicate whether this implementation shall use the polar decomposition to perform the singular value decomposition or the legacy QR decomposition to do so.
	/// </summary>
	/// <remarks>The polar decomposition approach is much faster but may leads to larger error(s) when the matrix to be decomposed is (near) singularity.</remarks>
	public bool SvdViaPolarDecomposition { get; set; }

	/// <summary>
	/// The default constructor of <see cref="Api"/>
	/// </summary>
	public Api()
	{
		this.BindedDeviceID = Runtime.CurrentDeviceID;
		NativeMethods.cusparseCreate(out this.cusparseHandle).Check();
		NativeMethods.cusparseSetPointerMode(this.cusparseHandle).Check();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		NativeMethods.cusparseDestroy(this.cusparseHandle);
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; } = false;
	#endregion

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
		using var buf = CudaBuffer.Create(bufSize, 0, false);
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
		TS3 valOut; T* pVal;
		TSInd3 idxOut; TInd3* pIdx;
		if (target.ValueStorages.Length == 1)
		{
			valOut = target.ValueStorages[0];
			if (!GetPointer(valOut, out pVal, out var n2) || n2 != nnz)
				return false;
		}
		else
		{
			var s = PureStorage<T, CpuMemoryPointer>.Create(nnz);
			pVal = (T*)s.Pointer.Pointer.Pointer;
			valOut = s as TS3 ?? TS3.Empty; // never empty
		}
		if (target.IndexStorages.Length == 2)
		{
			idxOut = target.IndexStorages[0];
			if (!GetPointer(idxOut, out pIdx, out var n2) || n2 != nnz)
				return false;
		}
		else
		{
			var s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
			pIdx = (TInd3*)s.Pointer.Pointer.Pointer;
			idxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
		}
		_ = sizeof(TInd1) == sizeof(int) ? NMC.vecSpAddCal_i32(T.Type, buffer, nnzx + nnzy, nnz, (int*)pIdx, pVal) : NMC.vecSpAddCal_i64(T.Type, buffer, nnzx + nnzy, nnz, (long*)pIdx, pVal);
		target.SetValues(x.Size[0], valOut, idxOut);
		target.Format = SparseFormat.VectorCooFormat;
		target.DefaultValue = T.Zero;
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
			return false; // not supported
		if (!GetPointer(this, y, 1, out T* py, out var ny))
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
		using var buf = CudaBuffer.Create(bufSize, 0, false);
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
			return false; // not supported
		if (!GetPointer(this, x, out T* px, out var ppx, out var nnzx))
			return false;
		if (!GetPointer(this, y, out T* py, out var ppy, out var nnzy))
			return false;
		if (sizeof(TInd3) != sizeof(TInd2) || sizeof(TInd2) != sizeof(TInd1))
			return false;
		if (target.ValueStorages.Length is not 0 and not 1 || target.IndexStorages.Length is not 0 and not 2)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

		if (NMC.spVecOuterCheck_i32(T.Type) < 0)
			return false;
		TS3 valOut; T* pVal;
		TSInd3 rowIdxOut, colIdxOut; TInd3* pRow, pCol;
		long nnz = nnzx * nnzy;
		if (target.ValueStorages.Length == 1)
		{
			valOut = target.ValueStorages[0].MakeReference();
			if (!GetPointer(valOut, out pVal, out var n2) || n2 != nnz)
				return false;
		}
		else
		{
			var s = PureStorage<T, CpuMemoryPointer>.Create(nnz);
			pVal = (T*)s.Pointer.Pointer.Pointer;
			valOut = s as TS3 ?? TS3.Empty; // never empty
		}
		if (target.IndexStorages.Length == 2)
		{
			rowIdxOut = target.IndexStorages[0].MakeReference();
			colIdxOut = target.IndexStorages[1].MakeReference();
			if (!GetPointer(rowIdxOut, out pRow, out var n2) || n2 != nnz)
				return false;
			if (!GetPointer(colIdxOut, out pCol, out n2) || n2 != nnz)
				return false;
		}
		else
		{
			var s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
			pRow = (TInd3*)s.Pointer.Pointer.Pointer;
			rowIdxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
			s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
			pCol = (TInd3*)s.Pointer.Pointer.Pointer;
			colIdxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
		}
		_ = sizeof(TInd1) == sizeof(int) ? NMC.spVecOuter_i32(T.Type, px, (int*)ppx, nnzx, py, (int*)ppy, nnzy, pVal, (int*)pRow, (int*)pCol, conjY) : NMC.spVecOuter_i64(T.Type, px, (long*)ppx, nnzx, py, (long*)ppy, nnzy, pVal, (long*)pRow, (long*)pCol, conjY);
		target.SetValues(x.Size[0], y.Size[0], valOut, rowIdxOut, colIdxOut);
		target.Format = SparseFormat.MatrixCocFormat;
		target.DefaultValue = T.Zero;
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
			return false; // not supported
		if (A is not null && B is not null)
			return false; // not supported
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
	#endregion
}
