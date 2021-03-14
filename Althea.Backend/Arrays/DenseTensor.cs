using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.TensorAlgebra;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete dense tensor class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual (pitched) data storage without any index storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public class DenseTensor<T> : TensorBase<T>, IPitchedArray<T>, IKrylovVector<DenseTensor<T>, T> where T : unmanaged
	{
		#region basic
		private readonly FixedBuffer_128<long> m_outerSize = default;

		private readonly FixedBuffer_128<long> m_outerSizeProd = default;

		/// <summary>
		/// Get the pitch (in <typeparamref name="T"/>) of this array (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		public ReadOnlySpan<long> OuterSize => this.m_outerSize.AsSpan(this.Rank);

		/// <summary>
		/// Get the strides (the inclusive accumulated product of <see cref="OuterSize"/>) of this tensor at all dimensions as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		public ReadOnlySpan<long> Strides => this.m_outerSizeProd.AsSpan(this.Rank);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetActualLength(ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize)
		{
			if (outerSize.IsEmpty)
				return 0;
			if (outerSize.Length != size.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(outerSize));
			long prodOuter = outerSize.Prod(), prod = 1;
			int r = size.Length - 1;
			for (int i = 0; i < r; i++)
			{
				prodOuter -= prod * (outerSize[i] - outerSize[i]);
				prod *= outerSize[i];
			}
			return prodOuter;
		}

		/// <summary>
		/// Create a <see cref="DenseTensor{T}"/> with given <paramref name="values"/>, presenting <paramref name="size"/>, actual <paramref name="outerSize"/> and <paramref name="labels"/>
		/// </summary>
		/// <param name="values">The preallocated <see cref="Storage{T}"/> of the value array</param>
		/// <param name="size">The presenting size of the tensor</param>
		/// <param name="outerSize">The outer size (actual lengths at all dimensions) of this tensor, empty mean the same as <paramref name="size"/></param>
		/// <param name="labels">The presenting labels of each dimension of this tensor, an empty one means auto generate as <c>{'a', 'b', ...}</c></param>
		/// <exception cref="ArgumentException">If <paramref name="labels"/> or <paramref name="outerSize"/>'s length is neither 0 nor the same as the rank</exception>
		public DenseTensor(Storage<T> values, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<char> labels) :
			base(values, size, labels, GetActualLength(size, outerSize))
		{
			if (outerSize.IsEmpty)
				outerSize = size;
			this.m_outerSize.CopyFromSpan(outerSize);
			var prod = this.m_outerSizeProd.AsSpan(this.Rank);
			this.OuterSize.AccumulateProd(result: prod, inclusive: true);
		}

		/// <summary>
		/// Create an empty
		/// </summary>
		public DenseTensor() : base(Storage<T>.Empty, stackalloc long[1], ReadOnlySpan<char>.Empty) { }
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override DenseTensor<T> Clone();

		/// <summary>
		/// Create a new array with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new array alike this one</returns>
		public override DenseTensor<T> NewArrayAlike();

		/// <summary>
		/// Create a new array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new array alike this one</returns>
		public override DenseTensor<TOut> NewArrayAlike<TOut>();
		#endregion

		#region reshape
		/// <summary>
		/// Reshape this array to a vector
		/// </summary>
		/// <returns>The referenced vector reshaped from this array</returns>
		public override ValueArray<T> ToVector();

		/// <summary>
		/// Reshape this array to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public override ValueArray<T> ToMatrix(long rows = 0);

		/// <summary>
		/// Reshape this tensor to another tensor with the given <paramref name="newSize"/> 
		/// </summary>
		/// <param name="newSize">The new size of the tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/></param>
		/// <returns>The reshaped tensor, may be a referenced one of this tensor</returns>
		public override DenseTensor<T> TensorReshape(ReadOnlySpan<long> newSize)
		{
			Span<long> size = stackalloc long[newSize.Length];
			newSize.CopyTo(size);
			CheckSize(this, size);
			if (size.SequenceEqual(this.Size))
				return this;

		}
		#endregion

		#region indexing
		/// <summary>
		/// Provide the basic indexed getter and setter of this tensor
		/// </summary>
		/// <param name="indices">The position indicated by a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <returns>The element at <paramref name="indices"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		public override T this[ReadOnlySpan<long> indices] { get; set; }

		/// <summary>
		/// Get a sub-tensor indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <returns>The sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/>. Shall be a referenced tensor if possible.</returns>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public override TensorBase<T> GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths);

		/// <summary>
		/// Get the sub-tensor indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/> and copy it to <paramref name="overwrite"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="overwrite">The tensor to be overwritten by the sub-tensor</param>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank; or <paramref name="overwrite"/> cannot be overwritten</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public override void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, TensorBase<T> overwrite);

		/// <summary>
		/// Set the sub-tensor indicated by the given starting <paramref name="offsets"/> and the size of <paramref name="value"/> to the underlying tensor of <paramref name="value"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="value">The tensor to set whose size is the lengths of the sub-tensor's size</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is out of range</exception>
		public override void SetSlice(ReadOnlySpan<long> offsets, TensorBase<T> value);
		#endregion

		#region tensor algebra methods
		/// <summary>
		/// Compute the tensor reduction (self partial summation) of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate which part(s) of dimension(s) to sum, its order will be ignored</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The reduction result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a partial permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override DenseTensor<T> Reduce(TensorOrder order, T scalar);

		/// <summary>
		/// Compute the tensor permutation of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate the permutation order</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The permutation result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a full permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override DenseTensor<T> Permute(TensorOrder order, T scalar);

		/// <summary>
		/// Compute the tensor contraction of this tensor and the <paramref name="other"/> tensor using their .
		/// </summary>
		/// <param name="other">The other tensor to perform the contraction with</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The contraction result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/>'s labels indicate that it cannot contract with this tensor</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override DenseTensor<T> Contract(TensorBase<T> other, T scalar);

		/// <summary>
		/// Compute the tensor point-wise addition of this tensor and the <paramref name="other"/> tensor.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this tensor</param>
		/// <param name="other">The other tensor to perform the contraction with</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> tensor</param>
		/// <returns>The addition result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different size than this one</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		public override DenseTensor<T> AddTensor(T scalarThis, TensorBase<T> other, T scalarOther);
		#endregion

		#region in-place tensor operations
		/// <summary>
		/// Permute <paramref name="tensor"/> by <paramref name="order"/> and replace to this tensor
		/// </summary>
		/// <param name="tensor">The tensor to be permuted</param>
		/// <param name="order">The new permutation <see cref="TensorOrder"/>, zero-based</param>
		public void Permute(DenseTensor<T> tensor, TensorOrder order)
		{
			
		}

		/// <summary>
		/// Partial reduction of tensor <paramref name="A"/>: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The missing indices of <paramref name="A"/> compared to <paramref name="C"/> will be aggregated according to <paramref name="reduction"/>.
		/// </summary>
		/// <param name="reduction">The reduce <see cref="BinaryOperation"/> <c>Φ</c></param>
		/// <param name="α">scalar α</param>
		/// <param name="opA"><see cref="UnaryOperation"/> <c>Ψ<sub>A</sub></c></param>
		/// <param name="A"><see cref="DenseTensor{T}"/> A</param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="opC"><see cref="UnaryOperation"/> <c>Ψ<sub>C</sub></c>, default identity</param>
		/// <param name="C"><see cref="DenseTensor{T}"/> C, default null</param>
		/// <remarks>If <paramref name="C"/> is null, or <paramref name="β"/> is zero, this tensor itself will be used instead of <paramref name="C"/>.</remarks>
		public void Reduce(BinaryOperation reduction, T α, UnaryOperation opA, DenseTensor<T> A, T β = default, UnaryOperation opC = UnaryOperation.Identity, DenseTensor<T> C = null)
		{
			
		}

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $\text{this}_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A"><see cref="DenseTensor{T}"/> A</param>
		/// <param name="B"><see cref="DenseTensor{T}"/> B</param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="C"><see cref="DenseTensor{T}"/> C, default null means this</param>
		/// <remarks>If <paramref name="C"/> is null, or <paramref name="β"/> is zero, this tensor itself will be used instead of <paramref name="C"/>.</remarks>
		public void Contract(T α, DenseTensor<T> A, DenseTensor<T> B, T β = default, DenseTensor<T> C = null)
		{
			TENSOR.Contract(α, A, B, β, C, this);
		}
		#endregion

		#region matrix operation and decompositions
		/// <summary>
		/// Multiply this tensor as a matrix with the <paramref name="right"/> tensor as another matrix.
		/// </summary>
		/// <param name="right">The other <see cref="DenseTensor{T}"/> as a matrix</param>
		/// <param name="partitionLeft">a <see cref="Index"/> to indicate the first <paramref name="partitionLeft"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="partitionRight">a <see cref="Index"/> to indicate the first <paramref name="partitionRight"/> (exclude) indices of tensor <paramref name="right"/> will be regarded as the row and others column</param>
		/// <param name="leftOp">The <see cref="MatrixOperation"/> to apply on this one</param>
		/// <param name="rightOp">The <see cref="MatrixOperation"/> to apply on <paramref name="right"/></param>
		/// <returns>the multiplication result, out-of-place</returns>
		public DenseTensor<T> OperatorMatrixMultiply(DenseTensor<T> right, Index partitionLeft, Index partitionRight, MatrixOperation leftOp = MatrixOperation.None, MatrixOperation rightOp = MatrixOperation.None)
		{
			if (this.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(right));
			int pl = (int)partitionLeft.GetPosition(this.Rank);
			int pr = (int)partitionRight.GetPosition(right.Rank);

			var (m, n) = (this.SizeProd[pl], this.Length / this.SizeProd[pl]);
			if (leftOp != MatrixOperation.None)
				(m, n) = (n, m);
			var (p, q) = (right.SizeProd[pr], right.Length / right.SizeProd[pr]);
			if (rightOp != MatrixOperation.None)
				(p, q) = (q, p);
			if (n != p)
				throw new ArgumentException(Resource.TensorWrongSize, nameof(right));
			var outSizeL = leftOp == MatrixOperation.None ? this.Size.Take(pl) : this.Size.TakeLast(this.Rank - pl);
			var outSizeR = rightOp != MatrixOperation.None ? right.Size.Take(pr) : right.Size.TakeLast(right.Rank - pr);

			var output = new DenseMatrix<T>(m, q, this.OnHost);
			try
			{
				using var l = this.ToMatrix(this.SizeProd[pl]) as DenseMatrix<T>;
				using var r = right.ToMatrix(right.SizeProd[pr]) as DenseMatrix<T>;
				output.Mulβ_AddBy_αAB(l, r, Scalars<T>.One, opA: leftOp, opB: rightOp);
				return FromDense(output, outSizeL.Concat(outSizeR).ToArray());
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>the singular values as a <see cref="double"/> array and left, right singular vectors</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public (double[] S, DenseTensor<T> U, DenseTensor<T> Vct) SingularValues(Index partition, bool calcU = true, bool calcV = true)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			var middleSize = new[] { Math.Min(leftLength, rightLength) };
			var Usize = leftSize.Concat(middleSize).ToArray();
			var VctSize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (S, U, Vct) = mat.SingularValues(null, null, null, calcU, calcV);
			using (S)
				return (Array.ConvertAll(S.ToFortranOrderArray(), s => s.ToDouble()), FromDense(U, Usize), FromDense(Vct, VctSize));
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix. Not necessarily sorted descending by singular values. Then truncate the singular values $S$ and vectors $U$, $V^*$ to preserve at most <paramref name="maxPreserve"/> entries.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="maxPreserve">The maximum number of singular values and vectors to preserve, must be positive</param>
		/// <returns>The singular values and left, right singular vectors with at most <paramref name="maxPreserve"/> entries.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		public (DenseTensor<T> S, DenseTensor<T> U, DenseTensor<T> Vct) SingularValuesTruncate(Index partition, int maxPreserve)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			var middleSize = new[] { Math.Min(Math.Min(leftLength, rightLength), maxPreserve) };
			var Usize = leftSize.Concat(middleSize).ToArray();
			var VctSize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (S, U, Vct) = mat.SingularValues(null, null, null, calcU: true, calcV: true);
			if (maxPreserve >= leftLength || maxPreserve >= rightLength)
			{
				var returnS = new DenseTensor<T>(new[] { middleSize[0], middleSize[0] }, onHost: this.OnHost);
				try
				{
					(returnS.ToMatrix() as DenseMatrix<T>).SetDiag(0, S);
					return (returnS, FromDense(U, Usize), FromDense(Vct, VctSize));
				}
				catch (Exception)
				{
					returnS?.Dispose();
					throw;
				}
				finally
				{
					S.Dispose();
				}
			}
			// else
			using (S) using (U) using (Vct)
			{
				var arrayS = S.ToFortranOrderArray();
				var arrayU = U.GetColumns();
				var arrayV = Vct.GetRows();
				try
				{
					var combine = arrayU.Zip(arrayV).ToArray();
					Array.Sort(keys: arrayS, items: combine);
					arrayS = arrayS.Reverse().ToArray();
					combine = combine.Reverse().ToArray();
					arrayS = arrayS[..maxPreserve];
					combine = combine[..maxPreserve];
					// copy to return U
					var returnU = new DenseMatrix<T>(U.NRows, U.NCols, U.OnHost);
					returnU.FromColumnVectors(Array.ConvertAll(combine, c => c.First));
					// copy to return V
					using var returnV = new DenseMatrix<T>(Vct.NCols, Vct.NRows, Vct.OnHost);
					returnV.FromColumnVectors(Array.ConvertAll(combine, c => c.Second));
					var returnVct = returnV.Transpose();
					// copy to return S
					var returnS = new DenseTensor<T>(new[] { middleSize[0], middleSize[0] }, onHost: this.OnHost);
					using var vecS = new DenseVector<T>(arrayS.Length, S.OnHost);
					vecS.FromFortranOrderArray(arrayS);
					(returnS.ToMatrix() as DenseMatrix<T>).SetDiag(0, S);
					// return
					return (returnS, FromDense(returnU, Usize), FromDense(returnVct, VctSize));
				}
				finally
				{
					arrayU.ClearList();
					arrayV.ClearList();
				}
			}
		}

		/// <summary>
		/// QR factorize this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="full">perform full factorization or not</param>
		/// <returns>the Q matrix and R matrix</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public (DenseTensor<T> Q, DenseTensor<T> R) QR(Index partition, bool full = false)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			full = full && leftLength > rightLength; // for 'fat' matrices, full == economic
			var middleSize = new[] { full ? leftLength : Math.Min(leftLength, rightLength) };
			var Qsize = leftSize.Concat(middleSize).ToArray();
			var Rsize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (Q, R) = mat.QR(full, null, null);
			return (FromDense(Q, Qsize), FromDense(R, Rsize));
		}

		/// <summary>
		/// (Conjugate) transpose this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="conjugate">conjugate or not, default null means true for complex type (<see cref="IComplex{T}"/>)</param>
		/// <returns>the (conjugate) transpose of this tensor with <c>Size = this.Size[<paramref name="partition"/>..] concatenate this.Size[..<paramref name="partition"/>]</c></returns>
		public DenseTensor<T> Transpose(Index partition, bool? conjugate = null)
		{
			int p = (int)partition.GetPosition(this.Rank);
			using var mat = this.ToMatrix(this.SizeProd[p]) as DenseMatrix<T>;
			var matTranspose = (conjugate ?? !default(T).ToDataType().IsReal()) ? mat.ConjugateTranspose() : mat.Transpose();
			return FromDense(matTranspose, this.Size.TakeLast(this.Rank - p).Concat(this.Size.Take(p)).ToArray());
		}

		/// <summary>
		/// Calculate the trace of this tensor as a matrix.
		/// </summary>
		/// <returns>the trace of this tensor as a matrix</returns>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public T Trace()
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();
			using var diag = ((DenseMatrix<T>)this.ToMatrix(this.Size[0])).GetDiag(0);
			return diag.Sum();
		}

		/// <summary>
		/// Shift all the eigenvalues of this tensor by adding <paramref name="shift"/> to each diagonal elements of this tensor as a matrix.
		/// </summary>
		/// <param name="shift">The shift value, if it is zero, no operation shall be performed</param>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public void EigenvalueShift(T shift)
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();
			// shortcut
			if (shift.CompareTo(Scalars<T>.Zero) == 0)
				return;
			using var ones = new DenseVector<T>(this.Size[0], this.OnHost);
			ones.FillWithOnes();
			BLAS.VectorAddBy(y: (DenseMatrix<T>)this.ToMatrix(this.Size[0]), x: ones, α: shift, strideY: (int)ones.Length + 1);
		}
		#endregion

		#region print
		/// <summary>
		/// Print out this tensor.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation</returns>
		public override string Print(PrintSettings? overrideSetting = null)
		{

		}
		#endregion

		#region serialization
		/// <summary>
		/// Get all the storages of this array. Only returns <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages() => new Dictionary<string, IStorage>(1)
		{
			[StorageName] = this.Storage
		};

		/// <summary>
		/// The presenting name of <see cref="OuterSize"/>
		/// </summary>
		protected internal const string OuterSizeName = nameof(OuterSize);

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type. Only returns the <see cref="OuterSize"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(1)
		{
			[OuterSizeName] = this.OuterSize.ToArray()
		};
		#endregion
	}
}