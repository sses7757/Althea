using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Sparse;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.TensorAlgebra.Sparse
{
	/// <summary>
	/// The CUDA back-end of <see cref="AbstractApi"/> which supports <see cref="Arrays.BlockedSparseTensor{T, TInd}"/> and <see cref="Arrays.VariableBlockSparseTensor{T, TInd}"/>
	/// </summary>
	public class SparseApi : AbstractApi
	{
		protected override bool ContractInPlace_<T>(SparseTensorWrapper<T> left, SparseTensorWrapper<T> right, TensorContractInfo info, SparseTensorWrapper<T> destination) => throw new NotImplementedException();
		protected override bool Contract_<T>(SparseTensorWrapper<T> left, SparseTensorWrapper<T> right, TensorContractInfo info, out Althea.LinearAlgebra.Sparse.SparseArrayWrapper<T> destination) => throw new NotImplementedException();
		protected override void Dispose(bool disposeManaged) => throw new NotImplementedException();
		protected override bool FromDense_<T>(Althea.TensorAlgebra.Dense.DenseTensorWrapper<T> source, SparseTensorFormat format, out Althea.LinearAlgebra.Sparse.SparseArrayWrapper<T> destination, float threshold = 0) => throw new NotImplementedException();
		protected override bool GetSlice_<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, out Althea.LinearAlgebra.Sparse.SparseArrayWrapper<T> sub) => throw new NotImplementedException();
		protected override bool GetSlice_<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensorWrapper<T> sub) => throw new NotImplementedException();
		protected override bool GetSlice_<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, Storage<T> sub, ReadOnlySpan<long> subOuterSize) => throw new NotImplementedException();
		protected override bool IsSupportedFormat(SparseTensorFormat format) => throw new NotImplementedException();
		protected override bool IsSupportedFormatBinary(SparseTensorFormat format1, SparseTensorFormat format2) => throw new NotImplementedException();
		protected override bool IsSupportedFormatTrinary(SparseTensorFormat format1, SparseTensorFormat format2, SparseTensorFormat format3) => throw new NotImplementedException();
		protected override bool IsSupportedTensorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => throw new NotImplementedException();
		protected override bool IsSupportedTensorTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => throw new NotImplementedException();
		protected override bool IsSupportedTensorUnary(CombinationOfLocations location) => throw new NotImplementedException();
		protected override bool OperationBinary_<T>(BinaryOperation binary, SparseTensorWrapper<T> left, Span<int> leftPerm, SparseTensorWrapper<T> right, Span<int> rightPerm, out Althea.LinearAlgebra.Sparse.SparseArrayWrapper<T> destination) => throw new NotImplementedException();
		protected override bool Permute_<T>(SparseTensorWrapper<T> source, ReadOnlySpan<int> permutationOrder, out Althea.LinearAlgebra.Sparse.SparseArrayWrapper<T> destination) => throw new NotImplementedException();
		protected override bool Reduce_<T>(BinaryOperation reduce, SparseTensorWrapper<T> source, ReadOnlySpan<int> reduceDimensions, out Althea.LinearAlgebra.Sparse.SparseArrayWrapper<T> destination) => throw new NotImplementedException();
		protected override bool Reshape_<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> newSize, out Althea.LinearAlgebra.Sparse.SparseArrayWrapper<T> destination) => throw new NotImplementedException();
		protected override bool SetSlice_<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensorWrapper<T> sub) => throw new NotImplementedException();
		protected override bool ToDense_<T>(SparseTensorWrapper<T> source, Storage<T> destination, ReadOnlySpan<long> outerSize) => throw new NotImplementedException();
	}
}
