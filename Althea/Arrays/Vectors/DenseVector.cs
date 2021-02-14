using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Storage;
using RT = Althea.Runtime.API;
using BLAS = Althea.Blas.API;
using Sparse = Althea.SparseBlas.API;


namespace Althea.Arrays
{
	/// <summary>
	/// The dense vector class that inherit the <see cref="VectorBase{T}"/> and implements <see cref="IDenseArray{T}"/>.
	/// </summary>
	/// <typeparam name="T">The supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/>; other types of data causes <see cref="NotSupportedException"/></typeparam>
	public sealed class DenseVector<T> : VectorBase<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region dense vector member override
		/// <summary>
		/// The last index of the dense vector is always its length subtract one. Override <see cref="VectorBase{T}.LastIndex"/>.
		/// </summary>
		public override long LastIndex => this.ActualLength - 1;

		/// <summary>
		/// Total actual length of the array in memory, in <typeparamref name="T"/> rather than bytes. Override <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public override long ActualLength => this.Size[0];
		#endregion


		#region initialize and destroy
		/// <summary>
		/// Empty constructor
		/// </summary>
		public DenseVector() : this(0, onHost: false) { }

		/// <summary>
		/// Vector constructor
		/// </summary>
		/// <param name="length">length of vector to initialize</param>
		/// <param name="onHost">allocate one host memory or device memory</param>
		public DenseVector(long length, bool onHost = false) : base(length, length, onHost) { }

		/// <summary>
		/// Vector deep clone constructor
		/// </summary>
		/// <param name="vector">original vector</param>
		public DenseVector(DenseVector<T> vector) : this(vector != null ? vector.Length : throw new ArgumentNullException(nameof(vector), Resource.ArrayCannotNull), vector.OnHost)
		{
			try
			{
				RT.CopyTo(source: vector, dest: this);
			}
			catch (Exception)
			{
				this.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Full constructor with pre-allocated value array
		/// </summary>
		/// <param name="values"><see cref="Storage{T}"/> of the value array</param>
		/// <param name="length">length of the vector</param>
		public DenseVector(Storage<T> values, long length) : base(values, length) { }

		/// <summary>
		/// Vector reshape constructor
		/// </summary>
		/// <param name="refArray">original array</param>
		/// <param name="newLength">size of the new vector</param>
		/// <param name="offset">offset to the <see cref="ValueArray{T}.Storage"/> in T rather than bytes</param>
		public DenseVector(ValueArray<T> refArray, long newLength, long offset = 0) : base(refArray, newLength, newLength, offset) { }
		#endregion


		#region reshape
		// not necessary to override the DeviceArray<T>'s default implementation
		#endregion


		#region dense array interface
		/// <summary>
		/// Convert the values of this vector to a C# array.
		/// </summary>
		/// <param name="ranges">The range with max value = length of this vector, default is all</param>
		/// <returns>C# array of type <typeparamref name="T"/> containing the values of this vector</returns>
		public T[] ToFortranOrderArray(params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.CheckRange(ranges[0]);
			return RT.CopyOutArray(this, length: count, offset: offset);
		}

		/// <summary>
		/// Copy the <paramref name="values"/> into this dense vector.
		/// </summary>
		/// <param name="values">The value array of element type <typeparamref name="T"/></param>
		/// <param name="ranges">The ranges of each dimension, default is all</param>
		public void FromFortranOrderArray(T[] values, params Range[] ranges)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.CheckRange(ranges[0]);
			if (values.LongLength < count)
				throw new ArgumentException(Resource.VectorTooShort, nameof(values));
			RT.CopyIntoArray(this, values, length: count, offset: offset);
		}
		#endregion


		#region implement converter
		/// <summary>
		/// Convert this array to another memory.
		/// </summary>
		/// <returns>a new <see cref="ValueArray{T}"/> with same value as this one if this array is on host memory</returns>
		public override ValueArray<T> ToTheOtherMemory()
		{
			var newVec = new DenseVector<T>(this.Length, !this.OnHost);
			try
			{
				RT.CopyTo(source: this, dest: newVec);
				return newVec;
			}
			catch (Exception)
			{
				newVec.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Take out the data array as a new <see cref="DenseVector{T}"/>. Override 
		/// </summary>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the referenced data array of this one.</returns>
		public override DenseVector<T> AsDenseVector() => this;

		/// <summary>
		/// Convert this vector to a <see cref="DenseVector{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <returns>Converted dense vector</returns>
		/// <remarks>Override <see cref="VectorBase{T}.ToDense"/></remarks>
		public override DenseVector<T> ToDense() => this;

		/// <summary>
		/// Convert this vector to a <see cref="SparseVector{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <param name="threshold">values smaller than or equal to <c>abs(<paramref name="threshold"/>)</c> are regarded as zeros</param>
		/// <returns>Converted sparse vector</returns>
		/// <remarks>If this vector is sparse, this method returns <c>this</c> directly rather than performing prunes.</remarks>
		/// <remarks>Override <see cref="VectorBase{T}.ToSparse(float)"/></remarks>
		public override SparseVector<T> ToSparse(float threshold = default)
			=> Sparse.VectorDenseToSparse(this, Math.Abs(threshold));

		/// <summary>
		/// Deep clone the array, the mutable status such as will not be copied. Implements the <see cref="AbstractArray{T}.Clone"/>.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override object Clone() => new DenseVector<T>(this);

		/// <summary>
		/// Create a new array with same immutable properties as this one, the mutable status such as will not be copied. Implements the <see cref="AbstractArray{T}.NewArrayAlike"/>.
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public override AbstractArray<T> NewArrayAlike() => new DenseVector<T>(this.Length, this.OnHost);

		/// <summary>
		/// Create a new array like this one (with same type and other info) while the data type is <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The new data type</typeparam>
		/// <returns>the new array</returns>
		public override ValueArray<TOut> NewArrayAlike<TOut>() => new DenseVector<TOut>(this.Length, this.OnHost);
		#endregion


		#region dense vector dense matrix restricted operations
		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result vector <see cref="DenseVector{T}"/>. From <see cref="General.IKrylovVector{TVec, T}.OperateOn(IReadOnlyList{TVec}, T[])"/>
		/// </summary>
		/// <param name="notJoinedVecs">The columns of the matrix to operate</param>
		/// <param name="input">The input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <see cref="DenseVector{T}"/>.</returns>
		/// <remarks>this method is actually static</remarks>
		public DenseVector<T> OperateOn(IReadOnlyList<DenseVector<T>> notJoinedVecs, T[] input)
		{
			if (input is null || input.Length == 0)
				throw new ArgumentNullException(nameof(input));
			if (notJoinedVecs is null || notJoinedVecs.Count != input.Length)
				throw new ArgumentNullException(nameof(notJoinedVecs));

			#region use JoinToMatrix
			////foreach (var vec in notJoinedVecs)
			////{
			////	if (vec is null || vec.Length != this.Length)
			////		throw new ArgumentException(Resource.VectorWrongSize, nameof(notJoinedVecs));
			////	if (vec.OnHost != this.OnHost || vec.AlreadyDisposed)
			////		throw new ArgumentException(Resource.VectorWrongValue, nameof(notJoinedVecs));
			////}

			////using var mat = new DenseMatrix<T>(this.Length, notJoinedVecs.Count, this.OnHost);
			////mat.JoinToMatrix(notJoinedVecs.ToArray());
			////using var inputvec = (DenseVector<T>)(input, this.OnHost);
			////var output = this.NewArrayAlike() as DenseVector<T>;
			////try
			////{
			////	output.Mulβ_AddBy_αopAx(mat, inputvec, Scalars<T>.One);
			////	return output;
			////}
			////catch (Exception)
			////{
			////	output.Dispose();
			////	throw;
			////}
			#endregion

			#region direct add
			// sort first to reduce errors
			var sort = input.Select((x, i) => new KeyValuePair<int, T>(i, x)).OrderBy(x => x.Value).Select(x => x.Key);

			var vec = new DenseVector<T>(this.Length, this.OnHost);
			vec.FillWithZeros();
			try
			{
				foreach (var i in sort)
				{
					var dnvec = notJoinedVecs[i];
					if (dnvec is null || dnvec.Length != this.Length)
						throw new ArgumentException(Resource.VectorWrongSize, nameof(notJoinedVecs));
					if (dnvec.OnHost != this.OnHost || dnvec.Disposed)
						throw new ArgumentException(Resource.VectorWrongValue, nameof(notJoinedVecs));
					if (!input[i].IsZero())
						vec.AddBy_αx(dnvec, input[i]);
				}
				return vec;
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
			#endregion
		}

		/// <summary>
		/// Replace the values of this vector by the one from <paramref name="other"/>, from <see cref="General.IKrylovVector{TVec, T}.ReplaceBy(TVec)"/>.
		/// </summary>
		/// <param name="other">The <see cref="DenseVector{T}"/> used to replace</param>
		/// <exception cref="ArgumentNullException">if <paramref name="other"/> is null</exception>
		/// <exception cref="ArgumentException">if <paramref name="other"/> have different size than this</exception>
		public void ReplaceBy(DenseVector<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other.Length != this.Length)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(other));
			RT.CopyTo(source: other, dest: this);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$, from <see cref="IVector{TVec, TMat, T}.Mulβ_AddBy_αopAx"/>.
		/// </summary>
		/// <param name="A"><see cref="DenseMatrix{T}"/></param>
		/// <param name="x"><see cref="DenseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public void Mulβ_AddBy_αopAx(DenseMatrix<T> A, DenseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			BLAS.MatrixVectorMultiply(A, x, this, α, β, op);
		}

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$ out-of-place, from <see cref="IVector{TVec, TMat, T}.OuterProduct"/>.
		/// </summary>
		/// <param name="other">The other input <see cref="DenseVector{T}"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">The <see cref="MatrixBase{T}"/> to overwrite as result, default null</param>
		/// <returns>The result <see cref="DenseMatrix{T}"/> or <paramref name="overwrite"/> if it is not null</returns>
		public DenseMatrix<T> OuterProduct(DenseVector<T> other, bool? conjugateOther = null, DenseMatrix<T> overwrite = null)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			DenseMatrix<T> mat;
			if (overwrite is null || overwrite == EmptyDnMat || overwrite.NRows < this.Length || overwrite.NCols < other.Length)
				mat = new DenseMatrix<T>(this.Length, other.Length, this.OnHost, herm: this == other);
			else
				mat = overwrite;
			try
			{
				BLAS.VectorOuterProduct(this, other, mat, Scalars<T>.One, conjugateB: conjugateOther);
				return mat;
			}
			catch (Exception)
			{
				if (mat != overwrite) mat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H (\text{or }\vec{v}_{\text{this}}^T) \vec{v}_{\text{other}}$, from <see cref="General.IKrylovVector{TVec, T}.Dot(TVec, bool?)"/>.
		/// </summary>
		/// <param name="other">The other <see cref="DenseVector{T}"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is symmetric for this vector and the other vector.</remarks>
		public T Dot(DenseVector<T> other, bool? conjugateThis = null)
		{
			return BLAS.VectorDot(this, other, conjugateX: conjugateThis);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_\text{other} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_\text{other}^i\}_i$, from <see cref="IVector{TVec, T}.PointWiseMultiply(TVec)"/>.
		/// </summary>
		/// <param name="other">The other <see cref="DenseVector{T}"/></param>
		/// <remarks>This method is symmetric since only the sparse vector one may be modified.</remarks>
		public void PointWiseMultiply(DenseVector<T> other)
		{
			BLAS.PointWiseMultiply(this, other);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} ./ \vec{v}_\text{other} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_\text{other}^i\}_i$, from <see cref="IVector{TVec, T}.PointWiseDivide(TVec)"/>.
		/// </summary>
		/// <param name="other">The other <see cref="DenseVector{T}"/></param>
		/// <exception cref="DivideByZeroException">if the <paramref name="other"/> vector is sparse</exception>
		public void PointWiseDivide(DenseVector<T> other)
		{
			BLAS.PointWiseDivision(this, other);
		}

		/// <summary>
		/// Compute $\vec{y} = \vec{y} + \alpha \vec{x}$, from <see cref="General.IKrylovVector{TVec, T}.AddBy_αx(TVec, T)"/>.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public void AddBy_αx(DenseVector<T> x, T α)
		{
			BLAS.VectorAddBy(this, x, α);
		}
		#endregion

		#region sparse vector dense matrix restricted operations
		/// <summary>
		/// Replace the values of this vector by the one from <paramref name="other"/>, from <see cref="General.IKrylovVector{TVec, T}.ReplaceBy(TVec)"/>.
		/// </summary>
		/// <param name="other">The <see cref="SparseVector{T}"/> used to replace</param>
		/// <exception cref="ArgumentNullException">if <paramref name="other"/> is null</exception>
		/// <exception cref="ArgumentException">if <paramref name="other"/> is too long</exception>
		public void ReplaceBy(SparseVector<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other.LastIndex > this.LastIndex)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(other));
			this.FillWithZeros();
			Sparse.VectorSparseToDense(other, this);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$, from <see cref="IVector{TVec, TMat, T}.Mulβ_AddBy_αopAx"/>.
		/// </summary>
		/// <param name="A">input <see cref="DenseMatrix{T}"/></param>
		/// <param name="x">input <see cref="SparseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public void Mulβ_AddBy_αopAx(DenseMatrix<T> A, SparseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			Sparse.DenseMatrixSparseVectorMultiply(A, x, this, α, β, op);
		}

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$ out-of-place, from <see cref="IVector{TVec, TMat, T}.OuterProduct"/>.
		/// </summary>
		/// <param name="other">The other input <see cref="SparseVector{T}"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">The <see cref="MatrixBase{T}"/> to overwrite as result, default null</param>
		/// <returns>The result <see cref="DenseMatrix{T}"/> or <paramref name="overwrite"/> if it is not null</returns>
		public DenseMatrix<T> OuterProduct(SparseVector<T> other, bool? conjugateOther = null, DenseMatrix<T> overwrite = null)
		{
			if (other is null || other == EmptySpVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			using var dv = other.ToDense();
			return this.OuterProduct(dv, conjugateOther, overwrite);
		}

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H (\text{or }\vec{v}_{\text{this}}^T) \vec{v}_{\text{other}}$, from <see cref="General.IKrylovVector{TVec, T}.Dot(TVec, bool?)"/>.
		/// </summary>
		/// <param name="other">The other <see cref="SparseVector{T}"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is semi-symmetric, i.e. the conjugate relation for this vector and the other vector.</remarks>
		public T Dot(SparseVector<T> other, bool? conjugateThis = null)
		{
			return Sparse.VectorSparseDotDense(other, this, conjugateThis).GenericConjugate();
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_\text{other} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_\text{other}^i\}_i$, from <see cref="IVector{TVec, T}.PointWiseMultiply(TVec)"/>.
		/// </summary>
		/// <param name="other">The other <see cref="SparseVector{T}"/></param>
		/// <returns>The result <see cref="SparseVector{T}"/>.</returns>
		/// <remarks>This method is symmetric since only the sparse vector one may be modified.</remarks>
		public void PointWiseMultiply(SparseVector<T> other)
		{
			if (other is null || other == EmptySpVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			using var v = other.Clone() as SparseVector<T>;
			Sparse.VectorSparseDensePointWiseMultiplyDivide(v, this, multiply: true);
			Sparse.VectorSparseToDense(v, this);
		}

		/// <summary>
		/// Compute $\vec{y} = \vec{y} + \alpha \vec{x}$, from <see cref="General.IKrylovVector{TVec, T}.AddBy_αx(TVec, T)"/>.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public void AddBy_αx(SparseVector<T> x, T α)
		{
			Sparse.VectorSparseAddToDense(this, x, α);
		}
		#endregion

		#region dense vector sparse matrix restricted operations
		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$, from <see cref="IVector{TVec, TMat, T}.Mulβ_AddBy_αopAx"/>.
		/// </summary>
		/// <param name="A"><see cref="SparseMatrix{T}"/></param>
		/// <param name="x"><see cref="DenseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public void Mulβ_AddBy_αopAx(SparseMatrix<T> A, DenseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			Sparse.SparseMatrixDenseVectorMultiply(A, x, this, α, β, op);
		}
		#endregion

		#region sparse vector sparse matrix restricted operations
		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$, from <see cref="IVector{TVec, TMat, T}.Mulβ_AddBy_αopAx"/>.
		/// </summary>
		/// <param name="A"><see cref="SparseMatrix{T}"/></param>
		/// <param name="x"><see cref="SparseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public void Mulβ_AddBy_αopAx(SparseMatrix<T> A, SparseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			if (x is null || x == EmptySpVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			using var dx = x.ToDense();
			this.Mulβ_AddBy_αopAx(A, dx, α, β, op);
		}
		#endregion


		#region implement abstract operations
		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result vector <see cref="VectorBase{T}"/>. From <see cref="VectorBase{T}.OperateOn"/>
		/// </summary>
		/// <param name="notJoinedVecs">The columns of the matrix to operate</param>
		/// <param name="input">The input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <see cref="VectorBase{T}"/>.</returns>
		/// <remarks>this method is actually static</remarks>
		public override VectorBase<T> OperateOn(IReadOnlyList<VectorBase<T>> notJoinedVecs, T[] input)
		{
			if (input is null || input.Length == 0)
				throw new ArgumentNullException(nameof(input));
			if (notJoinedVecs is null || notJoinedVecs.Count != input.Length)
				throw new ArgumentNullException(nameof(notJoinedVecs));

			return this.OperateOn(notJoinedVecs as IReadOnlyList<DenseVector<T>>, input);
		}

		/// <summary>
		/// Replace the values of this vector by the one from <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The <see cref="VectorBase{T}"/> used to replace</param>
		public override void ReplaceBy(VectorBase<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dv)
				this.ReplaceBy(dv);
			else if (other is SparseVector<T> sv)
				this.ReplaceBy(sv);
			else
			{
				var v = other.ToDense();
				try
				{
					this.ReplaceBy(v);
				}
				finally
				{
					if (v != other) v.Dispose();
				}
			}
		}

		/// <summary>
		/// Compute $\vec{y} = \vec{y} + \alpha \vec{x}$, override <see cref="VectorBase{T}.AddBy_αx(VectorBase{T}, T)"/>.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public override void AddBy_αx(VectorBase<T> x, T α)
		{
			if (x is null || x == EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (x is DenseVector<T> dx)
			{
				this.AddBy_αx(dx, α);
			}
			else if (x is SparseVector<T> sx)
			{
				this.AddBy_αx(sx, α);
			}
			else
			{   // using the opposite abstract method implemented by the real class of 'x'
				x.AddBy_αx_Opposite(this, α);
			}
		}

		/// <summary>
		/// DO NOT call this method since the class <see cref="DenseVector{T}"/> has no need to implement it.
		/// </summary>
		/// <exception cref="NotImplementedException">in any case</exception>
		/// <remarks>See <see cref="VectorBase{T}.AddBy_αx_Opposite(VectorBase{T}, T)"/></remarks>
		protected internal override void AddBy_αx_Opposite(VectorBase<T> y, T α)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(DenseVector<T>)));
		}

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H \vec{v}_{\text{other}}$, override <see cref="VectorBase{T}.Dot"/>.
		/// </summary>
		/// <param name="other">The other <see cref="VectorBase{T}"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is symmetric (semi-symmetric, e.g. the conjugate relation, when data type is a complex type) for this vector and the other vector.</remarks>
		public override T Dot(VectorBase<T> other, bool? conjugateThis = null)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dx)
			{
				return this.Dot(dx, conjugateThis);
			}
			else if (other is SparseVector<T> sx)
			{
				return this.Dot(sx, conjugateThis);
			}
			else
			{
				return other.Dot(this, conjugateThis).GenericConjugate();
			}
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_\text{other} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_\text{other}^i\}_i$, override <see cref="VectorBase{T}.PointWiseMultiply(VectorBase{T})"/>.
		/// </summary>
		/// <param name="other">The other <see cref="VectorBase{T}"/></param>
		/// <remarks>This method is symmetric since only the sparse vector one may be modified.</remarks>
		public override void PointWiseMultiply(VectorBase<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dx)
			{
				this.PointWiseMultiply(dx);
			}
			else if (other is SparseVector<T> sx)
			{
				this.PointWiseMultiply(sx);
			}
			else
			{
				other.PointWiseMultiply(this);
			}
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} ./ \vec{v}_\text{other} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_\text{other}^i\}_i$, override <see cref="VectorBase{T}.PointWiseDivide(VectorBase{T})"/>.
		/// </summary>
		/// <param name="other">The other <see cref="VectorBase{T}"/></param>
		/// <exception cref="DivideByZeroException">if the <paramref name="other"/> vector is sparse</exception>
		public override void PointWiseDivide(VectorBase<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dx)
			{
				this.PointWiseDivide(dx);
			}
			else if (other is SparseVector<T>)
			{
				throw new DivideByZeroException();
			}
			else
			{
				other.PointWiseDivide_Opposite(this);
			}
		}

		/// <summary>
		/// DO NOT call this method since the class <see cref="DenseVector{T}"/> has no need to implement it.
		/// </summary>
		/// <exception cref="NotImplementedException">in any case</exception>
		/// <remarks>See <see cref="VectorBase{T}.PointWiseDivide_Opposite(VectorBase{T})"/></remarks>
		protected internal override void PointWiseDivide_Opposite(VectorBase<T> other)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(DenseVector<T>)));
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$, override <see cref="VectorBase{T}.Mulβ_AddBy_αopAx"/>.
		/// </summary>
		/// <param name="A"><see cref="MatrixBase{T}"/></param>
		/// <param name="x"><see cref="VectorBase{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public override void Mulβ_AddBy_αopAx(MatrixBase<T> A, VectorBase<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (x is null || x == EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (A is DenseMatrix<T> dA)
			{
				if (x is DenseVector<T> dx)
				{
					this.Mulβ_AddBy_αopAx(dA, dx, α, β, op);
				}
				else if (x is SparseVector<T> sx)
				{
					this.Mulβ_AddBy_αopAx(dA, sx, α, β, op);
				}
				else
				{
					x.Mulβ_AddBy_αopAx_Opposite(dA, this, α, β, op);
				}
			}
			else if (A is SparseMatrix<T> sA)
			{
				if (x is DenseVector<T> dx)
				{
					this.Mulβ_AddBy_αopAx(sA, dx, α, β, op);
				}
				else if (x is SparseVector<T> sx)
				{
					this.Mulβ_AddBy_αopAx(sA, sx, α, β, op);
				}
				else
				{
					x.Mulβ_AddBy_αopAx_Opposite(sA, this, α, β, op);
				}
			}
			else // not built-in matrix type
			{
				A.Mulx_AddTo_y(x, this, α, β, op);
			}
		}

		/// <summary>
		/// DO NOT call this method since the class <see cref="DenseVector{T}"/> has no need to implement it.
		/// </summary>
		/// <exception cref="NotImplementedException">in any case</exception>
		/// <remarks>See <see cref="VectorBase{T}.Mulβ_AddBy_αopAx_Opposite(MatrixBase{T}, VectorBase{T}, T, T, MatrixOperation)"/></remarks>
		protected internal override void Mulβ_AddBy_αopAx_Opposite(MatrixBase<T> A, VectorBase<T> y, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(DenseVector<T>)));
		}

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$ out-of-place. Override <see cref="VectorBase{T}.OuterProduct"/>
		/// </summary>
		/// <param name="other">The other input <see cref="VectorBase{T}"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">The <see cref="MatrixBase{T}"/> to overwrite as result, default null</param>
		/// <returns>The result <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null</returns>
		public override MatrixBase<T> OuterProduct(VectorBase<T> other, bool? conjugateOther = null, MatrixBase<T> overwrite = null)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dx)
			{
				return this.OuterProduct(dx, conjugateOther, overwrite as DenseMatrix<T>);
			}
			else if (other is SparseVector<T> sx)
			{
				return this.OuterProduct(sx, conjugateOther, overwrite as SparseMatrix<T>);
			}
			else
			{
				var dv = other.ToDense();
				try
				{
					return this.OuterProduct(dv, conjugateOther, overwrite as DenseMatrix<T>);
				}
				finally
				{
					if (dv != other) dv.Dispose();
				}
			}
		}
		#endregion


		#region implement indexers
		/// <summary>
		/// Basic indexer of vector, from <see cref="IVector{DeviceVector, DeviceMatrix, T}"/>.
		/// </summary>
		/// <param name="i">position indicated by <see cref="Index"/></param>
		/// <returns>an instance of the data type <typeparamref name="T"/></returns>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position</remarks>
		public override T this[Index i] {
			get {
				return RT.CopyOut(this, CheckRange(i));
			}
			set {
				RT.CopyInto(this, value, CheckRange(i));
			}
		}

		/// <summary>
		/// Single range indexer of vector.
		/// </summary>
		/// <param name="r">The <see cref="Range"/> of index</param>
		/// <returns>The reference <see cref="VectorBase{T}"/> of the selected range</returns>
		/// <remarks>Range [a, b) are inclusive and exclusive respectively. See <see cref="Range"/> and <see cref="Index"/> for more information</remarks>
		public override VectorBase<T> this[Range r] {
			get {
				var (start, count) = CheckRange(r);
				return new DenseVector<T>(this, count, start);
			}
			set {
				var (startL, countL) = CheckRange(r);
				int start = checked((int)startL), count = checked((int)countL);
				if (value.Length != countL)
				{
					BLAS.SetArrayValues(this, ArrayLinq.Range(start, count).ToArray(), RT.CopyOut(value));
					return;
				}
				var v = value.ToDense();
				try
				{
					RT.CopyTo(source: v, dest: this, length: v.Length, offsetDest: startL);
				}
				finally
				{
					if (v != value) v.Dispose();
				}
			}
		}

		private DenseVector<T> this[params long[] indices] {
			get {
				if (indices is null || indices.LongLength == 0)
					return EmptyDnVec;
				var ind = checked(Array.ConvertAll(indices, a => (int)a));
				var vec = new DenseVector<T>(ind.LongLength, onHost: this.OnHost);
				try
				{
					Sparse.VectorGatherAtIndices(this, ind, vec);
					return vec;
				}
				catch (Exception)
				{
					vec.Dispose();
					throw;
				}
			}
			set {
				if (indices is null || indices.LongLength == 0 || value is null)
					return;
				var ind = checked(Array.ConvertAll(indices, i => (int)i));
				if (ind.LongLength == this.Length)
				{
					this[Range.All] = value; // array value replace
					return;
				}
				if (value.Length != ind.LongLength)
					BLAS.SetArrayValues(this, ind, RT.CopyOut(value));
				else
					Sparse.VectorSetAtIndices(value, ind, this);
			}
		}

		/// <summary>
		/// Multiple position indexer of vector.
		/// </summary>
		/// <param name="indices">positions indicated by array of <see cref="Index"/></param>
		/// <returns>All the values at the indices are copied to a new <see cref="DenseVector{T}"/></returns>
		/// <remarks>Since values are copied to a new <see cref="VectorBase{T}"/>, altering the retrieved values does not change this array's values at these positions</remarks>
		public override DenseVector<T> this[params Index[] indices] {
			get => this[CheckRange(indices)];
			set => this[CheckRange(indices)] = value;
		}

		/// <summary>
		/// Multiple range indexer of vector.
		/// </summary>
		/// <param name="ranges">The array of <see cref="Range"/> of indices</param>
		/// <returns>All the values inside the ranges are copied to a new <see cref="DenseVector{T}"/></returns>
		/// <remarks>This indexer copies the values in the ranges, altering the retrieved values does not change this array's values at these positions</remarks>
		public override DenseVector<T> this[params Range[] ranges] {
			get => this[CheckRange(ranges)];
			set => this[CheckRange(ranges)] = value;
		}
		#endregion


		#region managed converter
		/// <summary>
		/// Convert from a managed C# array.
		/// </summary>
		/// <param name="input">C# array of type <typeparamref name="T"/> and on host <see cref="bool"/> indicator</param>
		public static explicit operator DenseVector<T>((T[] values, bool onHost) input)
		{
			var (values, onHost) = input;
			if (values is null || values.Length == 0)
				return EmptyDnVec;
			var vec = new DenseVector<T>(values.LongLength, onHost);
			try
			{
				RT.CopyIntoArray(vec, values);
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
			return vec;
		}
		#endregion


		#region print
		internal T[] Raw(IReadOnlyDictionary<PrintSetting, int> config = null)
		{
			config ??= GlobalSettings.PrintConfig;
			long length = Math.Min(config[PrintSetting.ArrayLength], this.Length);
			T[] res = RT.CopyOutArray(this, length);
			return res;
		}

		/// <summary>
		/// Override <see cref="AbstractArray{T}.Print"/> to show detail.
		/// </summary>
		/// <param name="overrideSetting">See <see cref="AbstractArray{T}.Print"/></param>
		/// <returns>The string representation</returns>
		public override string Print(IReadOnlyDictionary<PrintSetting, int> overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;

			var printConfig = new Dictionary<PrintSetting, int>(GlobalSettings.PrintConfig);
			if (overrideSetting != null)
			{
				if (overrideSetting.ContainsKey(PrintSetting.ArrayLength))
					printConfig[PrintSetting.ArrayLength] = overrideSetting[PrintSetting.ArrayLength];
				if (overrideSetting.ContainsKey(PrintSetting.Precision))
					printConfig[PrintSetting.Precision] = overrideSetting[PrintSetting.Precision];
			}

			string detail = ":" + Environment.NewLine;
			T[] res = this.Raw(printConfig);
			detail += res.ToVectorString(precision: printConfig[PrintSetting.Precision]);
			if (this.Length > res.Length)
				detail += Environment.NewLine + $"...{this.Length - res.Length} more elements";

			return description + detail;
		}
		#endregion


		#region serialize
		/// <summary>
		/// Get the pointers of this instance.
		/// </summary>
		/// <returns>the pointer</returns>
		public override IReadOnlyDictionary<string, IStorage> GetPointers() => DenseVectorFactory.GetPointers(this);

		/// <summary>
		/// Get other requisite informations for re-constructing this array.
		/// </summary>
		/// <returns>other requisite informations</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => null;
		#endregion
	}
}
