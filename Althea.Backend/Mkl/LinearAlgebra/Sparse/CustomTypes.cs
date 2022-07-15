using System.Runtime.CompilerServices;

using Althea.Backend.Mkl.LinearAlgebra.Sparse;


namespace Althea.Backend.Mkl.LinearAlgebra.Sparse
{
    /// <summary>
    /// The error enum for MKL sparse BLAS library APIs
    /// </summary>
    public enum MklSparseBlasError
    {
        /// <summary>
        /// The operation was successful
        /// </summary>
        Success = 0,
        /// <summary>
        /// Empty handle or matrix arrays
        /// </summary>
        NotInitialized = 1,
        /// <summary>
        /// Internal error: memory allocation failed
        /// </summary>
        AllocFailed = 2,
        /// <summary>
        /// Invalid input value
        /// </summary>
        InvalidValue = 3,
        /// <summary>
        /// Execution failed due to e.g. 0-diagonal element for triangular solver, etc.
        /// </summary>
        ExecutionFailed = 4,
        /// <summary>
        /// Other internal error
        /// </summary>
        InternalError = 5,
        /// <summary>
        /// The operation is not supported yet, e.g. operation for double precision doesn't support other types
        /// </summary>
        NotSupported = 6,
    }

    internal enum MatrixOp
    {
        None = 10,
        Trans = 11,
        ConjTrans = 12
    }

    internal enum MatrixType
    {
        General = 20,
        Symmetric = 21,
        Hermitian = 22,
        Triangular = 23,
        Diagonal = 24,
        BlockTriangular = 25,
        BlockDiagonal = 26
    }

    internal enum MatrixFillMode
    {
        Lower = 40,
        Upper = 41,
        Full = 42
    }

    internal enum MatrixDiagType
    {
        NonUnit = 50,           /* triangular matrix with non-unit diagonal */
        Unit = 51            /* triangular matrix with unit diagonal */
    }

    internal enum MatrixMajor
    {
        Row = 101,
        Column = 102
    }

    internal enum MemoryUsage
    {
        None = 80,       /* no memory should be allocated for matrix values and structures; auxiliary structures could be created only for workload balancing, parallelization, etc. */
        Aggresive = 81        /* matrix could be converted to any internal format */
    }

    internal enum Request
    {
        FullMultiply = 90,
        CountNonzeros = 91,
        FinalizeMultiply = 92,
        FullMultiplyNoValues = 93,
        FinalizeMultiplyNoValues = 94
    }

    internal readonly record struct MatrixDescr(MatrixType Type, MatrixFillMode Mode, MatrixDiagType Diag);
}

namespace Althea.Backend.Mkl
{
    public static partial class StatusExtension
    {
        /// <summary>
        /// Check the given <see cref="MklSparseBlasError"/>
        /// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Check(this MklSparseBlasError error)
		{
            if (error == MklSparseBlasError.Success)
                return;
            throw new StatusException(error);
		}
    }
}