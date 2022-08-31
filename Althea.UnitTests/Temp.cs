using Althea.Array;
using Althea.Numerics;


namespace Althea.UnitTests;

internal class Temp
{
	// contract triangular shaped tensor network
	public static TT GenericTensorNetworkContractExample<T, TT, TOp>(TT A, TT B, TT C)
		where T : unmanaged, IBaseNumber<T>
		where TT : class, IBaseTensor<T, TT>
		where TOp : ITensorOperations<T, TT, TT, TT>
	{
		using var AA = A.SetLabels(stackalloc[] { 'x', 'a', 'b' });
		using var BB = B.SetLabels(stackalloc[] { 'a', 'y', 'c' });
		using var CC = C.SetLabels(stackalloc[] { 'c', 'b', 'z' });

		using var tempAB = TOp.Contract(AA, LinearAlgebra.UnaryOperation.Identity, BB, LinearAlgebra.UnaryOperation.Identity, T.One);
		var ABC = TOp.Contract(tempAB, LinearAlgebra.UnaryOperation.Identity, CC, LinearAlgebra.UnaryOperation.Identity, T.One);
		return ABC;
	}
}
