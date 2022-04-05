using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;
using Althea.TensorAlgebra;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpTen = Althea.TensorAlgebra.Sparse.ApiSelector;


namespace Althea.Arrays.Tensors
{
	internal class ConcreteSparseTensor
	{
	}
}
