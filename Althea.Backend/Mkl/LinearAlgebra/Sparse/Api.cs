using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Althea.Array;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;

using NM = Althea.Backend.Mkl.LinearAlgebra.Sparse.NativeMethods;


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
		#endregion
	}
}
