using System.Runtime.CompilerServices;
using System.Threading;

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
	}
}
