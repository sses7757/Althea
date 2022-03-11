using System;
using System.Collections.Generic;
using System.Dynamic;

using Althea.Arrays;
using Althea.LinearAlgebra.Sparse;


namespace Althea.TensorAlgebra.Sparse
{
	/// <summary>
	/// The abstract class for runtime sparse tensor algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractApiSelector
	{
		#region basic
		/// <summary>
		/// Get the currently using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new();

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The <see cref="Type"/> of the given implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		public static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The instance of an implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		internal static bool SetImplementation(AbstractApi? implementation) => SetImplementation(RecentAPIs, implementation);
		#endregion


		#region dynamic invocation
		/// <summary>
		/// Get the dynamic object used to dynamically invoke method(s) not listed explicitly here (the methods extra defined in derived classes)
		/// </summary>
		/// <remarks>
		/// Due to the limitations of dynamic invocation, <c>ref</c>, <c>in</c>, <c>out</c> and <c>ref struct</c>, etc. are not supported and non of the input arguments can be null.<br/>
		/// Since there are internal caching for <see cref="DynamicObject.TryInvokeMember(InvokeMemberBinder, object[], out object)"/>, the average repeated dynamic invocation may cost around 1 microsecond.
		/// </remarks>
		/// <example><code>
		/// long number = AbstractApi.Dynamic.CholeskyDecompose(...);
		/// </code></example>
		public static dynamic Dynamic => singletonDynamic;

		private static readonly DynamicInvocations singletonDynamic = new();

		private sealed class DynamicInvocations : DynamicInvocation
		{
			public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
			{
				result = DynamicInvokeExtraMethod(RecentAPIs, binder.Name, args);
				return true;
			}
		}
		#endregion


		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="SparseTensorFormat"/> is supported by this implementation or not.
		/// </summary>
		/// <param name="format">The given <see cref="SparseTensorFormat"/> to check</param>
		/// <returns>Whether <paramref name="format"/>is supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedFormat(SparseTensorFormat format);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="SparseTensorFormat"/>s are supported by tensor binary operations of this implementation or not.
		/// </summary>
		/// <param name="format1">The first given <see cref="SparseTensorFormat"/></param>
		/// <param name="format2">The second given <see cref="SparseTensorFormat"/></param>
		/// <returns>Whether binary operations on <paramref name="format1"/> and <paramref name="format2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedFormatBinary(SparseTensorFormat format1, SparseTensorFormat format2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="SparseTensorFormat"/>s are supported by tensor trinary operations of this implementation or not.
		/// </summary>
		/// <param name="format1">The first given <see cref="SparseTensorFormat"/></param>
		/// <param name="format2">The second given <see cref="SparseTensorFormat"/></param>
		/// <param name="format3">The third given <see cref="SparseTensorFormat"/></param>
		/// <returns>Whether trinary operations on <paramref name="format1"/>, <paramref name="format2"/> and <paramref name="format3"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedFormatTrinary(SparseTensorFormat format1, SparseTensorFormat format2, SparseTensorFormat format3);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/> is supported by tensor unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether unary operations on <paramref name="location"/> is supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedTensorUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by tensor binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedTensorBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by tensor trinary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">The third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether trinary operations on <paramref name="location1"/>, <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedTensorTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3);
		#endregion


		#region static methods as dispatchers
		/// <summary>
		/// Slice the sparse tensor <paramref name="source"/> with given <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <returns>The sparse sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		public static SparseArrayWrapper<T> GetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format = source.Format;
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription;
			bool success = false;
			SparseArrayWrapper<T> result = default;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormat(format) && a.IsSupportedTensorUnary(location1), node);
				success = node.Value.GetSlice_(source, offsets, lengths, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Slice the sparse tensor <paramref name="source"/> with given <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension and overwrite the result to a sparse <paramref name="sub"/> tensor.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">The sparse sub-tensor to be overwritten</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="sub"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		public static void GetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensorWrapper<T> sub) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format1 = source.Format, format2 = sub.Format;
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription, location2 = sub.ValueStorage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormatBinary(format1, format2) && a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.GetSlice_(source, offsets, lengths, sub);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Slice the sparse tensor <paramref name="source"/> with given <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension and overwrite the result to a dense <paramref name="sub"/> tensor.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">The dense sub-tensor to be overwritten</param>
		/// <param name="subOuterSize">The <see cref="IPitchedArray{T}.OuterSize"/> of the dense <paramref name="sub"/> tensor</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="sub"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		public static void GetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, Storage<T> sub, ReadOnlySpan<long> subOuterSize) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format1 = source.Format;
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription, location2 = sub.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormat(format1) && a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.GetSlice_(source, offsets, lengths, sub, subOuterSize);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Set the sparse tensor <paramref name="source"/>'s slice indicated by <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension with the values of <paramref name="sub"/> sparse tensor.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/> whose slice will be overwritten</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">The sparse sub-tensor used to overwrite</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="sub"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		public static void SetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensorWrapper<T> sub) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format1 = source.Format, format2 = sub.Format;
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription, location2 = sub.ValueStorage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormatBinary(format1, format2) && a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.SetSlice_(source, offsets, lengths, sub);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Convert the sparse tensor <paramref name="source"/> to a dense tensor whose storage is <paramref name="destination"/> and outer size if <paramref name="outerSize"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="outerSize">The <see cref="IPitchedArray{T}.OuterSize"/> of the target dense tensor</param>
		/// <param name="destination">The value array storage of the target dense matrix</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="outerSize"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="outerSize"/> is smaller than <paramref name="source"/> or its product is larger than <paramref name="destination"/>'s length</exception>
		public static void ToDense<T>(SparseTensorWrapper<T> source, Storage<T> destination, ReadOnlySpan<long> outerSize) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format = source.Format;
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription, location2 = destination.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormat(format) && a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.ToDense_(source, destination, outerSize);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Convert the given dense tensor <paramref name="source"/> to a sparse tensor of the given <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="format">The destination <see cref="SparseTensorFormat"/> of the target sparse tensor, must be atomic</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <returns>A created new sparse tensor of the given properties</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		public static SparseArrayWrapper<T> FromDense<T>(Dense.DenseTensorWrapper<T> source, SparseTensorFormat format, float threshold = 0) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription;
			bool success = false;
			SparseArrayWrapper<T> result = default;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormat(format) && a.IsSupportedTensorUnary(location1), node);
				success = node.Value.FromDense_(source, format, out result, threshold);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, reshape the sparse tensor <paramref name="source"/> tensor to the given <paramref name="newSize"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="newSize">The new presenting size as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/></param>
		/// <returns>The output tensor as a <see cref="SparseArrayWrapper{T}"/>.</returns>
		/// <remarks>If <paramref name="newSize"/> is the same as <paramref name="source"/>'s size, this method simply returns default</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="newSize"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="newSize"/> is not a valid reshape size</exception>
		public static SparseArrayWrapper<T> Reshape<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> newSize) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format = source.Format;
			CombinationOfLocations location = source.ValueStorage.LocationDescription;
			bool success = false;
			SparseArrayWrapper<T> result = default;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormat(format) && a.IsSupportedTensorUnary(location), node);
				success = node.Value.Reshape_(source, newSize, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation from the <paramref name="source"/> tensor with the given <paramref name="permutationOrder"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="permutationOrder">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <returns>The output tensor as a <see cref="SparseArrayWrapper{T}"/>.</returns>
		/// <remarks>If <paramref name="permutationOrder"/> is an identity permutation, this method simply returns <paramref name="source"/></remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="permutationOrder"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="permutationOrder"/> is not a full permutation order</exception>
		public static SparseArrayWrapper<T> Permute<T>(SparseTensorWrapper<T> source, ReadOnlySpan<int> permutationOrder) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format = source.Format;
			CombinationOfLocations location = source.ValueStorage.LocationDescription;
			bool success = false;
			SparseArrayWrapper<T> result = default;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormat(format) && a.IsSupportedTensorUnary(location), node);
				success = node.Value.Permute_(source, permutationOrder, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, compute the point-wise binary operation for input <paramref name="leftPerm"/>(<paramref name="left"/>) and <paramref name="rightPerm"/>(<paramref name="right"/>) tensors.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="binary">The <see cref="BinaryOperation"/> to be applied to <paramref name="left"/> and <paramref name="right"/> tensors</param>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="leftPerm">The full permutation order to be applied to <paramref name="left"/> before the binary operation, can be empty if <paramref name="left"/> is invalid</param>
		/// <param name="rightPerm">The full permutation order to be applied to <paramref name="right"/> before the binary operation, can be empty if <paramref name="right"/> is invalid</param>
		/// <returns>The output tensor as a <see cref="SparseArrayWrapper{T}"/>.</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentException">If the given tensors have different sizes under their permutations; or <paramref name="left"/> and <paramref name="right"/> are both invalid</exception>
		public static SparseArrayWrapper<T> OperationBinary<T>(BinaryOperation binary, SparseTensorWrapper<T> left, Span<int> leftPerm, SparseTensorWrapper<T> right, Span<int> rightPerm) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format1 = left.Format, format2 = right.Format;
			CombinationOfLocations location1 = left.ValueStorage.LocationDescription, location2 = right.ValueStorage.LocationDescription;
			bool success = false;
			SparseArrayWrapper<T> result = default;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormatBinary(format1, format2) && a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.OperationBinary_(binary, left, leftPerm, right, rightPerm, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction from the <paramref name="source"/> tensor with the given <paramref name="reduceDimensions"/>:<br/>
		/// <c>result = <paramref name="source"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="reduce"/>(<paramref name="source"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="source"/>[<paramref name="reduceDimensions"/>]))</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="reduce">The (symmetric) reduction operation as a <see cref="BinaryOperation"/></param>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be reduced</param>
		/// <param name="reduceDimensions">The values in this <b>set</b> (as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>) are the dimensions of which <paramref name="source"/> tensor are reduced</param>
		/// <returns>The output tensor as a <see cref="SparseArrayWrapper{T}"/>.</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="reduceDimensions"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduceDimensions"/> is not a partial permutation order or the sizes mismatches</exception>
		public static SparseArrayWrapper<T> Reduce<T>(BinaryOperation reduce, SparseTensorWrapper<T> source, ReadOnlySpan<int> reduceDimensions) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format = source.Format;
			CombinationOfLocations location = source.ValueStorage.LocationDescription;
			bool success = false;
			SparseArrayWrapper<T> result = default;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormat(format) && a.IsSupportedTensorUnary(location), node);
				success = node.Value.Reduce_(reduce, source, reduceDimensions, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors:<br/>
		/// <c>result = <paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * contract(<paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="left"/>), <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="right"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <returns>The output tensor as a <see cref="SparseArrayWrapper{T}"/>.</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors</exception>
		public static SparseArrayWrapper<T> Contract<T>(SparseTensorWrapper<T> left, SparseTensorWrapper<T> right, TensorContractInfo info) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format1 = left.Format, format2 = right.Format;
			CombinationOfLocations location1 = left.ValueStorage.LocationDescription, location2 = right.ValueStorage.LocationDescription;
			bool success = false;
			SparseArrayWrapper<T> result = default;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormatBinary(format1, format2) && a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.Contract_(left, right, info, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, compute the in-place sparse tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and overwrite the result value array to <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * contract(<paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="left"/>), <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="right"/>)) + <paramref name="destination"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="destination"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <param name="destination">The destination sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="destination"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors; or the contraction cannot be performed in-place</exception>
		public static void ContractInPlace<T>(SparseTensorWrapper<T> left, SparseTensorWrapper<T> right, TensorContractInfo info, SparseTensorWrapper<T> destination) where T : unmanaged, INumber<T>
		{
			SparseTensorFormat format1 = left.Format, format2 = right.Format, format3 = destination.Format;
			CombinationOfLocations location1 = left.ValueStorage.LocationDescription, location2 = right.ValueStorage.LocationDescription, location3 = destination.ValueStorage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedFormatTrinary(format1, format2, format3) && a.IsSupportedTensorTrinary(location1, location2, location3), node);
				success = node.Value.ContractInPlace_(left, right, info, destination);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion


		#region abstract methods that actually do computations
		/// <summary>
		/// When implemented by a derived class, slice the sparse tensor <paramref name="source"/> with given <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">Output the sparse sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		protected abstract bool GetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, out SparseArrayWrapper<T> sub) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, slice the sparse tensor <paramref name="source"/> with given <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension and overwrite the result to a sparse <paramref name="sub"/> tensor.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">The sparse sub-tensor to be overwritten</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="sub"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		protected abstract bool GetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensorWrapper<T> sub) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, slice the sparse tensor <paramref name="source"/> with given <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension and overwrite the result to a dense <paramref name="sub"/> tensor.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">The dense sub-tensor to be overwritten</param>
		/// <param name="subOuterSize">The <see cref="IPitchedArray{T}.OuterSize"/> of the dense <paramref name="sub"/> tensor</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="sub"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		protected abstract bool GetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, Storage<T> sub, ReadOnlySpan<long> subOuterSize) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, set the sparse tensor <paramref name="source"/>'s slice indicated by <paramref name="offsets"/> and <paramref name="lengths"/> of each dimension with the values of <paramref name="sub"/> sparse tensor.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/> whose slice will be overwritten</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="sub">The sparse sub-tensor used to overwrite</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="sub"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> or <paramref name="lengths"/> is out of range</exception>
		protected abstract bool SetSlice<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensorWrapper<T> sub) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, convert the sparse tensor <paramref name="source"/> to a dense tensor whose storage is <paramref name="destination"/> and outer size if <paramref name="outerSize"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="outerSize">The <see cref="IPitchedArray{T}.OuterSize"/> of the target dense tensor</param>
		/// <param name="destination">The value array storage of the target dense matrix</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="outerSize"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="outerSize"/> is smaller than <paramref name="source"/> or its product is larger than <paramref name="destination"/>'s length</exception>
		protected abstract bool ToDense<T>(SparseTensorWrapper<T> source, Storage<T> destination, ReadOnlySpan<long> outerSize) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, convert the given dense tensor <paramref name="source"/> to a sparse tensor of the given <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="format">The destination <see cref="SparseTensorFormat"/> of the target sparse tensor, must be atomic</param>
		/// <param name="destination">Output a created new sparse tensor of the given properties</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		protected abstract bool FromDense<T>(Dense.DenseTensorWrapper<T> source, SparseTensorFormat format, out SparseArrayWrapper<T> destination, float threshold = 0) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, reshape the sparse tensor <paramref name="source"/> to the given <paramref name="newSize"/> and output a <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="newSize">The new presenting size as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/></param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="newSize"/> is the same as <paramref name="source"/>'s size, this method shall simply returns default</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="newSize"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="newSize"/> is not a valid reshape size</exception>
		protected abstract bool Reshape<T>(SparseTensorWrapper<T> source, ReadOnlySpan<long> newSize, out SparseArrayWrapper<T> destination) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation from the <paramref name="source"/> tensor with the given <paramref name="permutationOrder"/> and output a <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <param name="permutationOrder">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="permutationOrder"/> is an identity permutation, this method shall simply returns <paramref name="source"/></remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="permutationOrder"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="permutationOrder"/> is not a full permutation order</exception>
		protected abstract bool Permute<T>(SparseTensorWrapper<T> source, ReadOnlySpan<int> permutationOrder, out SparseArrayWrapper<T> destination) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the point-wise binary operation for input <paramref name="leftPerm"/>(<paramref name="left"/>) and <paramref name="rightPerm"/>(<paramref name="right"/>) tensors and output the result as a <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="binary">The <see cref="BinaryOperation"/> to be applied to <paramref name="left"/> and <paramref name="right"/> tensors</param>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="leftPerm">The full permutation order to be applied to <paramref name="left"/> before the binary operation, can be empty if <paramref name="left"/> is invalid</param>
		/// <param name="rightPerm">The full permutation order to be applied to <paramref name="right"/> before the binary operation, can be empty if <paramref name="right"/> is invalid</param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If the given tensors have different sizes under their permutations; or <paramref name="left"/> and <paramref name="right"/> are both invalid</exception>
		protected abstract bool OperationBinary<T>(BinaryOperation binary, SparseTensorWrapper<T> left, Span<int> leftPerm, SparseTensorWrapper<T> right, Span<int> rightPerm, out SparseArrayWrapper<T> destination) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction from the <paramref name="source"/> tensor with the given <paramref name="reduceDimensions"/> and output the result as a <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="source"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="reduce"/>(<paramref name="source"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="source"/>[<paramref name="reduceDimensions"/>]))</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="reduce">The (symmetric) reduction operation as a <see cref="BinaryOperation"/></param>
		/// <param name="source">The source sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be reduced</param>
		/// <param name="reduceDimensions">The values in this <b>set</b> (as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>) are the dimensions of which <paramref name="source"/> tensor are reduced</param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="reduceDimensions"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduceDimensions"/> is not a partial permutation order or the sizes mismatches</exception>
		protected abstract bool Reduce<T>(BinaryOperation reduce, SparseTensorWrapper<T> source, ReadOnlySpan<int> reduceDimensions, out SparseArrayWrapper<T> destination) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and output the result as <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * contract(<paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="left"/>), <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="right"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <param name="destination">The output tensor as a <see cref="SparseArrayWrapper{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors</exception>
		protected abstract bool Contract<T>(SparseTensorWrapper<T> left, SparseTensorWrapper<T> right, TensorContractInfo info, out SparseArrayWrapper<T> destination) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the in-place sparse tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and overwrite the result value array to <paramref name="destination"/>:<br/>
		/// <c><paramref name="destination"/> = <paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * contract(<paramref name="left"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="left"/>), <paramref name="right"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="right"/>)) + <paramref name="destination"/>.<see cref="SparseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="SparseTensorWrapper{T}.Operation">Op</see>(<paramref name="destination"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="left">The left input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="right">The right input sparse tensor as a <see cref="SparseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <param name="destination">The destination sparse tensor as a <see cref="SparseTensorWrapper{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="destination"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors; or the contraction cannot be performed in-place</exception>
		protected abstract bool ContractInPlace<T>(SparseTensorWrapper<T> left, SparseTensorWrapper<T> right, TensorContractInfo info, SparseTensorWrapper<T> destination) where T : unmanaged, INumber<T>;
		#endregion
	}
}