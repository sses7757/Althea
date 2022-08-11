using System.Runtime.CompilerServices;

using Althea.LinearAlgebra.Sparse;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse;

/// <summary>
/// The CUDA back-end of the sparse linear algebra <see cref="IConversionAbstractApi"/>, <see cref="IComputationAbstractApi"/> and <see cref="IIndexOperationAbstractApi"/> that utilizes cuSPARSE and custom CUDA functions.
/// </summary>
/// <remarks>CUDA stream is not supported yet but can be easily added.</remarks>
public unsafe class Api : IConversionAbstractApi, IComputationAbstractApi, IIndexOperationAbstractApi
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

	#region conversion

	#endregion
}
