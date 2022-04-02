using Althea.Arrays;
using Althea.LinearAlgebra.Sparse;
using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.TensorAlgebra.Sparse
{
	/// <summary>
	/// The abstract interface for runtime sparse tensor algebra API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IAbstractApi : IAbstractRuntimeApi<IAbstractApi>
	{
		/// <summary>
		/// When implemented by a derived class, slice the sparse tensor <paramref name="source"/> with given <paramref name="offsets"/> and lengths in <paramref name="sub"/> of each dimension.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">Output the sparse sub-tensor indicated by <paramref name="offsets"/> and its own <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Size"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="sub"/>'s size is out of range</exception>
		[AbstractApiMethod]
		public abstract bool GetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ReadOnlySpan<long> offsets, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> sub) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, set the sparse tensor <paramref name="source"/>'s slice indicated by <paramref name="offsets"/> and lengths in <paramref name="sub"/> of each dimension with the values of <paramref name="sub"/> sparse tensor.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> whose slice will be overwritten</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">The sparse sub-tensor used to overwrite</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="sub"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="sub"/>'s size is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ReadOnlySpan<long> offsets, ISparseArray<T, TInd2, TS2, TSInd2> sub) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, convert the sparse tensor <paramref name="source"/> to a dense tensor whose storage is <paramref name="destination"/> and outer size if <paramref name="outerSize"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd">The concrete storage type of input index array</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <param name="outerSize">The <see cref="IPitchedArray{T}.OuterSize"/> of the target dense tensor</param>
		/// <param name="destination">The value array storage of the target dense matrix</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="outerSize"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="outerSize"/> is smaller than <paramref name="source"/> or its product is larger than <paramref name="destination"/>'s length</exception>
		[AbstractApiMethod]
		public abstract bool ToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> source, TS2 destination, ReadOnlySpan<long> outerSize) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, convert the given dense tensor <paramref name="source"/> to a sparse tensor of the given format of <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="destination">Output a created new sparse tensor of the given properties</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or format of <paramref name="destination"/> is not atomic</exception>
		[AbstractApiMethod]
		public abstract bool FromDense<T, TInd, TS1, TS2, TSInd>(Dense.DenseTensorWrapper<T, TS1> source, ref SparseArrayWrapper<T, TInd, TS2, TSInd> destination, double threshold = 0) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation from the <paramref name="source"/> tensor with the given <paramref name="permutationOrder"/> and overwrite the result multiplied by <paramref name="scalar"/> to <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd">The concrete storage type of input index array</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <param name="scalar">The scalar to multiply during computation</param>
		/// <param name="op">The <see cref="UnaryOperation"/> to be applied to <paramref name="source"/> during computation</param>
		/// <param name="permutationOrder">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <param name="destination">The output dense tensor as a <see cref="Dense.DenseTensorWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="permutationOrder"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="permutationOrder"/> is not a full permutation order</exception>
		[AbstractApiMethod]
		public abstract bool Permute<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> source, T scalar, UnaryOperation op, ReadOnlySpan<int> permutationOrder, Dense.DenseTensorWrapper<T, TS2> destination) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation from the <paramref name="source"/> tensor with the given <paramref name="permutationOrder"/> and output the result multiplied by <paramref name="scalar"/> to <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <param name="scalar">The scalar to multiply during computation</param>
		/// <param name="op">The <see cref="UnaryOperation"/> to be applied to <paramref name="source"/> during computation</param>
		/// <param name="permutationOrder">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="permutationOrder"/> is an identity permutation, this method shall simply returns <paramref name="source"/></remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="permutationOrder"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="permutationOrder"/> is not a full permutation order</exception>
		[AbstractApiMethod]
		public abstract bool Permute<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, T scalar, UnaryOperation op, ReadOnlySpan<int> permutationOrder, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> destination) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, compute the point-wise binary operation for input <paramref name="leftPerm"/>(<paramref name="left"/>) and <paramref name="rightPerm"/>(<paramref name="right"/>) tensors and output the result as a <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the first input index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for first input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for second input value array</typeparam>
		/// <typeparam name="TS3">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd">The concrete storage type of first input index array</typeparam>
		/// <param name="binary">The <see cref="BinaryOperation"/> to be applied to <paramref name="left"/> and <paramref name="right"/> tensors</param>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/>, can be invalid</param>
		/// <param name="scalarLeft">The scalar to multiply to <paramref name="left"/> during computation</param>
		/// <param name="opLeft">The <see cref="UnaryOperation"/> to be applied to <paramref name="left"/> during computation</param>
		/// <param name="right">The right input dense tensor as a <see cref="Dense.DenseTensorWrapper{T, TS}"/>, can be invalid</param>
		/// <param name="leftPerm">The full permutation order to be applied to <paramref name="left"/> before the binary operation, can be empty if <paramref name="left"/> is invalid</param>
		/// <param name="rightPerm">The full permutation order to be applied to <paramref name="right"/> before the binary operation, can be empty if <paramref name="right"/> is invalid</param>
		/// <param name="destination">The output tensor as a <see cref="Dense.DenseTensorWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If the given tensors have different sizes under their permutations; or <paramref name="left"/> and <paramref name="right"/> are both invalid</exception>
		[AbstractApiMethod]
		public abstract bool OperationBinary<T, TInd, TS1, TS2, TS3, TSInd>(BinaryOperation binary, ISparseArray<T, TInd, TS1, TSInd>? left, ReadOnlySpan<int> leftPerm, T scalarLeft, UnaryOperation opLeft, Dense.DenseTensorWrapper<T, TS2> right, ReadOnlySpan<int> rightPerm, Dense.DenseTensorWrapper<T, TS3> destination) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, compute the point-wise binary operation for input <paramref name="leftPerm"/>(<paramref name="left"/>) and <paramref name="rightPerm"/>(<paramref name="right"/>) tensors and output the result as a <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the first input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the second input index type</typeparam>
		/// <typeparam name="TInd3">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for first input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for second input value array</typeparam>
		/// <typeparam name="TS3">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of first input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of second input index array</typeparam>
		/// <typeparam name="TSInd3">The concrete storage type of output index array</typeparam>
		/// <param name="binary">The <see cref="BinaryOperation"/> to be applied to <paramref name="left"/> and <paramref name="right"/> tensors</param>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/>, can be invalid</param>
		/// <param name="scalarLeft">The scalar to multiply to <paramref name="left"/> during computation</param>
		/// <param name="opLeft">The <see cref="UnaryOperation"/> to be applied to <paramref name="left"/> during computation</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/>, can be invalid</param>
		/// <param name="leftPerm">The full permutation order to be applied to <paramref name="left"/> before the binary operation, can be empty if <paramref name="left"/> is invalid</param>
		/// <param name="rightPerm">The full permutation order to be applied to <paramref name="right"/> before the binary operation, can be empty if <paramref name="right"/> is invalid</param>
		/// <param name="scalarRight">The scalar to multiply to <paramref name="right"/> during computation</param>
		/// <param name="opRight">The <see cref="UnaryOperation"/> to be applied to <paramref name="right"/> during computation</param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If the given tensors have different sizes under their permutations; or <paramref name="left"/> and <paramref name="right"/> are both invalid</exception>
		[AbstractApiMethod]
		public abstract bool OperationBinary<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(BinaryOperation binary, ISparseArray<T, TInd1, TS1, TSInd1>? left, ReadOnlySpan<int> leftPerm, T scalarLeft, UnaryOperation opLeft, ISparseArray<T, TInd2, TS2, TSInd2>? right, ReadOnlySpan<int> rightPerm, T scalarRight, UnaryOperation opRight, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> destination) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInteger<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction from the <paramref name="source"/> tensor with the given <paramref name="reduceDimensions"/> and overwrite the result to <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="scalar"/> * <paramref name="reduce"/>(<paramref name="op"/>(<paramref name="source"/>)[<paramref name="reduceDimensions"/>])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd">The concrete storage type of input index array</typeparam>
		/// <param name="reduce">The (symmetric) reduction operation as a <see cref="BinaryOperation"/></param>
		/// <param name="source">The source sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to be reduced</param>
		/// <param name="scalar">The scalar to multiply during computation</param>
		/// <param name="op">The <see cref="UnaryOperation"/> to be applied to <paramref name="source"/> during computation</param>
		/// <param name="reduceDimensions">The values in this <b>set</b> (as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>) are the dimensions of which <paramref name="source"/> tensor are reduced</param>
		/// <param name="destination">The output dense tensor as a <see cref="Dense.DenseTensorWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="reduceDimensions"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduceDimensions"/> is not a partial permutation order or the sizes mismatches</exception>
		[AbstractApiMethod]
		public abstract bool Reduce<T, TInd, TS1, TS2, TSInd>(BinaryOperation reduce, ISparseArray<T, TInd, TS1, TSInd> source, T scalar, UnaryOperation op, ReadOnlySpan<int> reduceDimensions, Dense.DenseTensorWrapper<T, TS2> destination) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction from the <paramref name="source"/> tensor with the given <paramref name="reduceDimensions"/> and output the result as a <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="scalar"/> * <paramref name="reduce"/>(<paramref name="op"/>(<paramref name="source"/>)[<paramref name="reduceDimensions"/>])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="reduce">The (symmetric) reduction operation as a <see cref="BinaryOperation"/></param>
		/// <param name="source">The source sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to be reduced</param>
		/// <param name="scalar">The scalar to multiply during computation</param>
		/// <param name="op">The <see cref="UnaryOperation"/> to be applied to <paramref name="source"/> during computation</param>
		/// <param name="reduceDimensions">The values in this <b>set</b> (as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>) are the dimensions of which <paramref name="source"/> tensor are reduced</param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="reduceDimensions"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduceDimensions"/> is not a partial permutation order or the sizes mismatches</exception>
		[AbstractApiMethod]
		public abstract bool Reduce<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(BinaryOperation reduce, ISparseArray<T, TInd1, TS1, TSInd1> source, T scalar, UnaryOperation op, ReadOnlySpan<int> reduceDimensions, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> destination) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and output the result as <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="right"/>.<see cref="Dense.DenseTensorWrapper{T, TS}.Scalar">α</see> * contract(<paramref name="opLeft"/>(<paramref name="left"/>), <paramref name="right"/>.<see cref="Dense.DenseTensorWrapper{T, TS}.Operation">op</see>(<paramref name="right"/>) + <paramref name="destination"/>.<see cref="Dense.DenseTensorWrapper{T, TS}.Scalar">β</see> * <paramref name="destination"/>.<see cref="Dense.DenseTensorWrapper{T, TS}.Operation">op</see>(<paramref name="destination"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the first input index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for first input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for second input value array</typeparam>
		/// <typeparam name="TS3">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd">The concrete storage type of first input index array</typeparam>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to be contracted</param>
		/// <param name="opLeft">The <see cref="UnaryOperation"/> to be applied to <paramref name="left"/> during computation</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to be contracted</param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors</exception>
		[AbstractApiMethod]
		public abstract bool Contract<T, TInd, TS1, TS2, TS3, TSInd>(ISparseArray<T, TInd, TS1, TSInd> left, UnaryOperation opLeft, Dense.DenseTensorWrapper<T, TS2> right, TensorContractInfo info, Dense.DenseTensorWrapper<T, TS3> destination) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and output the result as <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="α"/> * contract(<paramref name="opLeft"/>(<paramref name="left"/>), <paramref name="opRight"/>(<paramref name="right"/>) + <paramref name="β"/> * <paramref name="opDst"/>(<paramref name="destination"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the first input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the second input index type</typeparam>
		/// <typeparam name="TInd3">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for first input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for second input value array</typeparam>
		/// <typeparam name="TS3">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of first input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of second input index array</typeparam>
		/// <typeparam name="TSInd3">The concrete storage type of output index array</typeparam>
		/// <param name="α">The scalar to multiply to <paramref name="left"/> or <paramref name="right"/> during computation</param>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to be contracted</param>
		/// <param name="opLeft">The <see cref="UnaryOperation"/> to be applied to <paramref name="left"/> during computation</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to be contracted</param>
		/// <param name="opRight">The <see cref="UnaryOperation"/> to be applied to <paramref name="right"/> during computation</param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <param name="β">The scalar to multiply to <paramref name="destination"/> during computation</param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <param name="opDst">The <see cref="UnaryOperation"/> to be applied to <paramref name="destination"/> during computation</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors</exception>
		[AbstractApiMethod]
		public abstract bool Contract<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(T α, ISparseArray<T, TInd1, TS1, TSInd1> left, UnaryOperation opLeft, ISparseArray<T, TInd2, TS2, TSInd2> right, UnaryOperation opRight, in StorableContractInfo info, T β, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> destination, UnaryOperation opDst) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInteger<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>;
	}
}