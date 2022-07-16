using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Althea.Array;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;

using NM = Althea.Backend.Mkl.LinearAlgebra.Sparse.NativeMethods;
using NMC = Althea.Backend.Mkl.LinearAlgebra.Sparse.CustomNativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Sparse
{
	/// <summary>
	/// The MKL back-end of <see cref="IConversionAbstractApi"/> and <see cref="IComputationAbstractApi"/> that supports storage locations of CPU memory.
	/// </summary>
	public unsafe partial class Api : IConversionAbstractApi, IComputationAbstractApi
	{
		#region basic
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FreeSparseMatrix(IntPtr handle)
		{
			var err = NM.mkl_sparse_destroy(handle);
			if (err != MklSparseBlasError.Success)
				Helpers.Log.Write($"Error in disposing MKL Sparse BLAS and LAPACK library: {err}", level: Helpers.LogLevel.Error);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (var kv in this.mvCache.Concat(this.svCache))
				FreeSparseMatrix(kv.Value);
			foreach (var kv in this.mmCache.Concat(this.smCache))
				FreeSparseMatrix(kv.Value);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public bool Disposed { get; set; } = false;

		private readonly Dictionary<(object matrix, MatrixOp trans), IntPtr> mvCache = new();
		private readonly Dictionary<(object matrix, MatrixOp trans), IntPtr> svCache = new();
		private readonly Dictionary<(object matrix, MatrixOp trans, long cols), IntPtr> mmCache = new();
		private readonly Dictionary<(object matrix, MatrixOp trans, long cols), IntPtr> smCache = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS s, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default; length = 0;
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(sName);
			if (s is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
			length = ps.Length;
			if (length > MklInt.MaxValue || length < 0)
				return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TInd, TS, TSInd>(TS s, TSInd sInd, out T* pointer, out TInd* pointerInd, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("sInd")] string? sIndName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
		{
			pointer = default; pointerInd = default; length = 0;
			if (!TInd.Type.IsInteger() || TInd.Size != sizeof(MklInt))
				return false;
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(sName);
			if (sInd is null || !sInd.IsValid())
				throw new ArgumentNullException(sIndName);
			if (s is not PureStorage<T, CpuMemoryPointer> ps || sInd is not PureStorage<TInd, CpuMemoryPointer> psInd)
				return false; // not support
			pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
			pointerInd = psInd.Pointer.Pointer.UnmangedPointer<TInd>(psInd.Pointer.OffsetInBytes);
			if (pointerInd == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, sIndName);
			length = ps.Length;
			if (length > MklInt.MaxValue || length < 0)
				return false;
			if (psInd.Length != length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, sIndName);
			return true;
		}

		#endregion

		#region vector conversion
		/// <inheritdoc/>
		public virtual bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(x, positions, out T* px, out TInd* pp, out var n))
				return false;
			return NMC.vecSetValAt(T.Type, px, &value, (MklInt*)pp, n) >= 0;
		}

		/// <inheritdoc/>
		public virtual bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(values, positions, out T* px, out TInd* pp, out var n))
				return false;
			if (!GetPointer(x, out T* py, out var n2))
				return false;
			if (n2 < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

			var func = default(T) switch
			{
				Float32 => new NM.cblas_sctr<Float32>(NM.cblas_ssctr) as NM.cblas_sctr<T>,
				Float64 => new NM.cblas_sctr<Float64>(NM.cblas_dsctr) as NM.cblas_sctr<T>,
				Complex<Float32> => new NM.cblas_sctr<Complex<Float32>>(NM.cblas_csctr) as NM.cblas_sctr<T>,
				Complex<Float64> => new NM.cblas_sctr<Complex<Float64>>(NM.cblas_zsctr) as NM.cblas_sctr<T>,
				_ => null
			};
			if (func is null)
				return false;
			func.Invoke(n, px, (MklInt*)pp, py);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(values, positions, out T* py, out TInd* pp, out var n))
				return false;
			if (!GetPointer(x, out T* px, out var n2))
				return false;
			if (n2 < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

			var func = default(T) switch
			{
				Float32 => new NM.cblas_gthr<Float32>(NM.cblas_sgthr) as NM.cblas_gthr<T>,
				Float64 => new NM.cblas_gthr<Float64>(NM.cblas_dgthr) as NM.cblas_gthr<T>,
				Complex<Float32> => new NM.cblas_gthr<Complex<Float32>>(NM.cblas_cgthr) as NM.cblas_gthr<T>,
				Complex<Float64> => new NM.cblas_gthr<Complex<Float64>>(NM.cblas_zgthr) as NM.cblas_gthr<T>,
				_ => null
			};
			if (func is null)
				return false;
			func.Invoke(n, py, px, (MklInt*)pp);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x!!, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (strideY != 1 || x.Format != new SparseFormat(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element))
				return false;
			if (x.IndexStorages.Length != 1 || x.ValueStorages.Length != 1)
				return false;
			if (!GetPointer(x.ValueStorages[0], x.IndexStorages[0], out T* px, out TInd* pp, out var n))
				return false;
			if (!GetPointer(y, out T* py, out var n2))
				return false;
			if (n2 < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));

			var func = default(T) switch
			{
				Float32 => new NM.cblas_sctr<Float32>(NM.cblas_ssctr) as NM.cblas_sctr<T>,
				Float64 => new NM.cblas_sctr<Float64>(NM.cblas_dsctr) as NM.cblas_sctr<T>,
				Complex<Float32> => new NM.cblas_sctr<Complex<Float32>>(NM.cblas_csctr) as NM.cblas_sctr<T>,
				Complex<Float64> => new NM.cblas_sctr<Complex<Float64>>(NM.cblas_zsctr) as NM.cblas_sctr<T>,
				_ => null
			};
			if (func is null)
				return false;
			new Span<T>(py, (int)n2).Fill(x.DefaultValue);
			func.Invoke(n, px, (MklInt*)pp, py);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold = 0) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS2 : class, IStorage<T, TS2> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd>
		{

		}
		#endregion
	}
}
