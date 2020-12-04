using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.General;
using Althea.Linq;
using Althea.Memory;
using RT = Althea.Runtime.API;
using BLAS = Althea.Blas.API;
using SPARSE = Althea.SparseBlas.API;


namespace Althea.Arrays
{

	/// <summary>
	/// The sparse vector class that inherit the <see cref="VectorBase{T}"/> and implements <see cref="ISparseArray{T}"/>.
	/// </summary>
	/// <remarks>The indices array are platform specific, <see cref="long"/> for 64-bit system and <see cref="int"/> otherwise.</remarks>
	/// <typeparam name="T">the supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/>; other types of data causes <see cref="NotSupportedException"/></typeparam>
	public sealed class SparseVector<T> : VectorBase<T>, ISparseArray<T>, IVector<SparseVector<T>, SparseMatrix<T>, T> where T : struct, IComparable<T>
	{
		#region sparse vector special
		/// <summary>
		/// The last index of the sparse vector is its <see cref="IndexPointer"/>'s last value. Override <see cref="VectorBase{T}.LastIndex"/>.
		/// </summary>
		public override long LastIndex => RT.CopyOut(this.IndexPointer, offset: this.NonZero - 1);

		/// <summary>
		/// Number of nonzero values of this sparse vector, equal to the array size of <see cref="IndexPointer"/> and <see cref="PureArray{T}.Pointer"/>, from <see cref="ISparseArray{T}.NonZero"/>.
		/// </summary>
		public long NonZero => this.ActualLength;

		internal int IntNNZ => checked((int)this.NonZero);

		/// <summary>
		/// The pointer to the index array (array of <see cref="int"/>) of the sparse vector, read-only
		/// </summary>
		/// <remarks>The index array is kept sorted all the time</remarks>
		internal Storage<int> IndexPointer { get; }
		#endregion


		#region initialize and destroy
		/// <summary>
		/// Empty constructor
		/// </summary>
		public SparseVector() : this(0, 0, onHost: false) { }

		/// <summary>
		/// Vector constructor
		/// </summary>
		/// <param name="length">length of vector to initialize</param>
		/// <param name="nonZeros">the actual length of the stored data, i.e. the number of non-zero values</param>
		/// <param name="onHost">allocate one host memory or device memory</param>
		/// <exception cref="ArgumentException">if <c>2 * <paramref name="nonZeros"/> ≥ <paramref name="length"/></c></exception>
		public SparseVector(long length, long nonZeros, bool onHost = false) : base(actualLength: nonZeros, showLength: length, onHost)
		{
			if (length == 0 && nonZeros == 0)
				return;
			if (2 * nonZeros >= length)
			{
				Log.Write(Resource.SpVecTooDense, category: "SparseVector Creator", level: LogLevel.Warning);
			}
			this.IndexPointer = Storage<int>.Create(nonZeros, onHost);
		}

		/// <summary>
		/// Vector constructor with indices of type <see cref="int"/> pre-allocated, which will be converted to <see cref="long"/> first.
		/// </summary>
		/// <param name="length">length of vector to initialize</param>
		/// <param name="indices">a <see cref="Storage{T}"/> to indicate the index array, if it is default value, a new one will be created</param>
		/// <exception cref="ArgumentException">if <c>2 * <see cref="NonZero"/> ≥ <paramref name="length"/></c></exception>
		public SparseVector(long length, Storage<int> indices) : base(actualLength: indices.Length, showLength: length, onHost: indices is null ? throw new ArgumentNullException(nameof(indices)) : indices.OnHost)
		{
			if (length == 0)
				return;
			if (2 * this.NonZero >= length)
			{
				Log.Write(Resource.SpVecTooDense, category: "SparseVector Creator", level: LogLevel.Warning);
			}
			this.IndexPointer = indices + 0; // +0 for a reference array
			return;
		}

		/// <summary>
		/// Full constructor with pre-allocated value and index arrays
		/// </summary>
		/// <param name="length">length of the vector</param>
		/// <param name="values">the pre-allocated value array</param>
		/// <param name="indices">the pre-allocated index array</param>
		public SparseVector(long length, Storage<T> values, Storage<int> indices) : base(values, length)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));
			if (indices is null)
				throw new ArgumentNullException(nameof(indices));
			if (values.OnHost != indices.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (values.Length != indices.Length)
				throw new ArgumentException(Resource.VectorWrongSize);
			this.IndexPointer = indices ?? throw new ArgumentNullException(nameof(indices));
			if (2 * this.NonZero >= length)
			{
				Log.Write(Resource.SpVecTooDense, category: "SparseVector Creator", level: LogLevel.Warning);
			}
		}

		/// <summary>
		/// Sparse vector offset reference constructor.
		/// </summary>
		/// <param name="refArray">reference array</param>
		/// <param name="newLength">length of the new sparse vector</param>
		/// <param name="newNNZ">the actual length of the stored data, i.e. the number of non-zero values of the new sparse vector</param>
		/// <param name="indices">the indices array pointer, if it is null and the <paramref name="refArray"/> is not a <see cref="SparseVector{T}"/>, a new one will be allocated</param>
		/// <param name="offset">offset to the data pointer</param>
		public SparseVector(PureArray<T> refArray, long newLength, long newNNZ, Storage<int> indices = null, long offset = 0) : base(refArray, newNNZ, newLength, offset)
		{
			if (refArray is null)
				throw new ArgumentNullException(nameof(refArray), Resource.ArrayCannotNull);
			if (indices != null)
			{
				if (refArray.OnHost != indices.OnHost)
					throw new ArgumentException(Resource.RequireSamePos);
			}
			if (refArray is SparseVector<T> sv)
			{
				this.IndexPointer = (indices ?? sv.IndexPointer) + offset;
			}
			else
			{
				if (indices is null)
				{
					this.IndexPointer = Storage<int>.Create(newNNZ, refArray.OnHost);
				}
				else
				{
					this.IndexPointer = offset == 0 ? indices : (indices + offset);
				}
			}
			if (this.IndexPointer.Length != newNNZ)
				throw new ArgumentOutOfRangeException(nameof(newNNZ));
		}

		/// <summary>
		/// The function that actually implements the dispose functionality, override <see cref="PureArray{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposing">dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (this.Disposed || this.Length == 0 || this.Pointer is null || !(this._root is null))
				return;
			if (!(this.IndexPointer is null))
				this.IndexPointer.Dispose();
		}
		#endregion


		#region clone
		/// <summary>
		/// Deep copy the array including the <see cref="IndexPointer"/>.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override object Clone()
		{
			return this.CloneValueAndIndex();
		}

		/// <summary>
		/// Create a new array with same immutable properties as this one, the mutable status such as will not be copied. Implements the <see cref="AbstractArray{T}.NewArrayAlike"/>.
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public override AbstractArray<T> NewArrayAlike() => new SparseVector<T>(this.Length, this.NonZero, this.OnHost);

		/// <summary>
		/// Create a new array like this one (with same type and other info) while the data type is <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the new data type</typeparam>
		/// <returns>the new array</returns>
		public override PureArray<TOut> NewArrayAlike<TOut>() => new SparseVector<TOut>(this.Length, this.NonZero, this.OnHost);

		/// <summary>
		/// Clone only the value array of this sparse vector.
		/// </summary>
		/// <returns>The new <see cref="SparseVector{T}"/> whose value array is copied and index array is not.</returns>
		public SparseVector<T> CloneValueAlone()
		{
			var v = new SparseVector<T>(this.Length, indices: this.IndexPointer);
			try
			{
				RT.CopyTo(source: this, dest: v);
			}
			catch (Exception)
			{
				v.Dispose();
				throw;
			}
			return v;
		}

		/// <summary>
		/// Clone both the value and the index array of this sparse vector.
		/// </summary>
		/// <returns>The new <see cref="SparseVector{T}"/> whose value and index arrays are copied.</returns>
		public SparseVector<T> CloneValueAndIndex()
		{
			var v = new SparseVector<T>(this.Length, this.NonZero, this.OnHost); // new index pointer
			try
			{
				RT.CopyTo(source: this, dest: v, length: this.NonZero);
				RT.CopyTo(source: this.IndexPointer, dest: v.IndexPointer, length: this.NonZero);
			}
			catch (Exception)
			{
				v.Dispose();
				throw;
			}
			return v;
		}
		#endregion


		#region reshape
		/// <summary>
		/// Reshape this sparse vector to a <see cref="SparseMatrix{T}"/> of <see cref="SparseMatrixFormat.COOC"/> with leading dimension = leadDim. Override <see cref="PureArray{T}.ToMatrix(long)"/>.
		/// </summary>
		/// <param name="leadDim">leading dimension of matrix; if leadDim ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public override PureArray<T> ToMatrix(long leadDim = 0)
		{
			var newSize = this.CheckSize(new[] { leadDim, 0 });
			leadDim = newSize[0];
			var secondDim = newSize[1];
			var mat = new SparseMatrix<T>(this, leadDim, secondDim, format: SparseMatrixFormat.COOC, herm: false);
			try
			{
				SPARSE.VectorToFromCOOMatrix(this, mat, toCOO: true);
				RT.CopyTo(source: this, dest: mat, length: this.NonZero);
			}
			catch (Exception)
			{
				mat.Dispose();
				throw;
			}
			return mat;
		}

		/// <summary>
		/// Reshape this sparse vector to a general <see cref="DenseTensor{T}"/> with dimensionality = size. Override <see cref="PureArray{T}.ToTensor(long[])"/>.
		/// </summary>
		/// <param name="size">The new dimensions. You can have one or zero uncertain dimension, indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override PureArray<T> ToTensor(params long[] size)
		{
			throw new NotImplementedException();
		}
		#endregion


		#region sparse array interface
		/// <summary>
		/// Fill this sparse vector's index array(s) with arithmetic sequence(s), from <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="v">start values and steps of the sequence(s), must be of same length as <see cref="AbstractArray{T}.Size"/></param>
		/// <exception cref="ArgumentException">if the lengths/values of <paramref name="v"/> do not follow the rule</exception>
		public void FillIndexWithRange(params (int start, int step)[] v)
		{
			if (v is null)
				throw new ArgumentNullException(nameof(v));
			if (v.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(v));
			if (v[0].start < 0 || v[0].step <= 0)
				throw new ArgumentException(Resource.VectorWrongValue);
			SPARSE.IndexFillWithRange(this.IndexPointer, this.NonZero, v[0].start, v[0].step);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long offset, long count) GetNNZRange(Range range)
		{
			var (offset, count) = range.GetOffsetAndCount(this.NonZero);
			if (offset + count > this.NonZero || offset < 0)
				throw new ArgumentOutOfRangeException(nameof(range));
			return (offset, count);
		}

		/// <summary>
		/// Convert the values of this vector to a C# array.
		/// </summary>
		/// <param name="ranges">the range with max value = <c>nnz</c>, default is all</param>
		/// <returns>C# array of type <typeparamref name="T"/> containing the values of this vector</returns>
		public T[] ValueToFortranOrderArray(params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.GetNNZRange(ranges[0]);
			return RT.CopyOutArray(this, length: count, offset: offset);
		}

		/// <summary>
		/// Convert the index array of this vector to an <see cref="IEnumerable{T}"/> of C# array
		/// </summary>
		/// <param name="ranges">the range of each index array, default all</param>
		/// <returns>an <see cref="IEnumerable{T}"/> of C# array of type <see cref="long"/></returns>
		public IEnumerable<long[]> IndexToLongArray(params Range[] ranges)
		{
			var longArr = System.Linq.Enumerable.First(this.IndexToIntArray(ranges));
			yield return Array.ConvertAll(longArr, a => (long)a);
		}

		/// <summary>
		/// Convert the index array of this vector to an <see cref="IEnumerable{T}"/> of C# array
		/// </summary>
		/// <param name="ranges">the range of each index array, default all</param>
		/// <returns>an <see cref="IEnumerable{T}"/> of C# array of type <see cref="int"/></returns>
		public IEnumerable<int[]> IndexToIntArray(params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.GetNNZRange(ranges[0]);
			yield return RT.CopyOutArray(this.IndexPointer, length: count, offset: offset);
		}

		/// <summary>
		/// Copy the <paramref name="values"/> into this sparse vector's value array.
		/// </summary>
		/// <param name="values">the value array of element type <typeparamref name="T"/></param>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		public void ValueFromFortranOrderArray(T[] values, params Range[] ranges)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.GetNNZRange(ranges[0]);
			if (values.LongLength < count)
				throw new ArgumentException(Resource.VectorTooShort, nameof(values));
			RT.CopyIntoArray(this, values, length: count, offset: offset);
		}

		/// <summary>
		/// Copy the <paramref name="indices"/> into this sparse vector's index array.
		/// </summary>
		/// <param name="indices">an <see cref="IEnumerable{T}"/> of C# <see cref="long"/> array</param>
		/// <param name="ranges">the range of each index array, default all</param>
		public void IndexFromLongArray(IEnumerable<long[]> indices, params Range[] ranges)
		{
			if (indices is null || !System.Linq.Enumerable.Any(indices))
				throw new ArgumentNullException(nameof(indices));
			var values = Array.ConvertAll(System.Linq.Enumerable.First(indices), a => (int)a);
			this.IndexFromIntArray(new[] { values }, ranges);
		}

		/// <summary>
		/// Copy the <paramref name="indices"/> into this sparse vector's index array.
		/// </summary>
		/// <param name="indices">an <see cref="IEnumerable{T}"/> of C# <see cref="int"/> array</param>
		/// <param name="ranges">the range of each index array, default all</param>
		public void IndexFromIntArray(IEnumerable<int[]> indices, params Range[] ranges)
		{
			if (indices is null || !System.Linq.Enumerable.Any(indices))
				throw new ArgumentNullException(nameof(indices));
			var values = System.Linq.Enumerable.First(indices);
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.GetNNZRange(ranges[0]);
			if (values.LongLength < count)
				throw new ArgumentException(Resource.VectorTooShort, nameof(indices));
			RT.CopyIntoArray(this.IndexPointer, values, length: count, offset: offset);
		}

		/// <summary>
		/// Dispose this sparse vector after comparing the pointers between this vector and the target <paramref name="array"/>.
		/// </summary>
		/// <param name="array">the target <see cref="ISparseArray{T}"/> to compare</param>
		public void DisposeComparedTo(ISparseArray<T> array)
		{
			if (array is SparseVector<T> sv)
			{
				if (this.Pointer != sv.Pointer)
					this.Pointer.Dispose();
				if (this.IndexPointer != sv.IndexPointer)
					this.IndexPointer.Dispose();
			}
			else if (array is SparseMatrix<T> sm)
			{
				if (this.Pointer != sm.Pointer)
					this.Pointer.Dispose();
				this.IndexPointer.Dispose();
			}
			else
			{
				// other cases cannot share same pointers
				this.Pointer.Dispose();
				this.IndexPointer.Dispose();
			}

			this.Disposed = true;
		}
		#endregion


		#region implement converter
		/// <summary>
		/// Convert this array to another memory.
		/// </summary>
		/// <returns>a new <see cref="PureArray{T}"/> with same value as this one if this array is on host memory</returns>
		public override PureArray<T> ToTheOtherMemory()
		{
			var newVec = new SparseVector<T>(this.Length, this.NonZero, !this.OnHost);
			try
			{
				RT.CopyTo(source: this.Pointer, dest: newVec.Pointer, length: this.NonZero);
				RT.CopyTo(source: this.IndexPointer, dest: newVec.IndexPointer, length: this.NonZero);
				return newVec;
			}
			catch (Exception)
			{
				newVec.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert this vector to a <see cref="DenseVector{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <returns>Converted dense vector</returns>
		/// <remarks>Override <see cref="VectorBase{T}.ToDense"/></remarks>
		public override DenseVector<T> ToDense()
		{
			var dense = new DenseVector<T>(this.Length, this.OnHost);
			try
			{
				SPARSE.VectorSparseToDense(this, dense);
				return dense;
			}
			catch (Exception)
			{
				dense?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert this vector to a <see cref="SparseVector{T}"/>. The out-of-place conversion may be performed, override <see cref="VectorBase{T}.ToSparse(float)"/>.
		/// </summary>
		/// <param name="threshold">values smaller than <c>abs(threshold)</c> are regarded as zeros</param>
		/// <returns>Converted sparse vector</returns>
		/// <remarks>If this vector is sparse, this method returns <c>this</c> directly rather than performing prunes.</remarks>
		public override SparseVector<T> ToSparse(float threshold = default) => this;

		/// <summary>
		/// Extract the data array directly to a <see cref="DenseVector{T}"/>.
		/// </summary>
		/// <returns>Extracted data vector</returns>
		/// <remarks>This operation is always in-place</remarks>
		/// <remarks>Override <see cref="PureArray{T}.AsDenseVector"/></remarks>
		public override DenseVector<T> AsDenseVector()
		{
			return new DenseVector<T>(this.Pointer, this.NonZero);
		}

		/// <summary>
		/// Cast this array into another data type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">the data type to cast to</typeparam>
		/// <returns>The casted <see cref="AbstractArray{T}"/>.</returns>
		public override AbstractArray<TOut> DataTypeCast<TOut>()
		{
			if (typeof(TOut) == typeof(T))
				return this as SparseVector<TOut>;
			var vec = base.DataTypeCast<TOut>() as SparseVector<TOut>;
			try
			{
				RT.CopyTo(this.IndexPointer, vec.IndexPointer, this.NonZero);
				return vec;
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
		}
		#endregion


		#region sparse vector sparse matrix restricted operations
		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result vector <see cref="SparseVector{T}"/>. From <see cref="IKrylovVector{TVec, T}.OperateOn(IReadOnlyList{TVec}, T[])"/>
		/// </summary>
		/// <param name="notJoinedVecs">the columns of the matrix to operate</param>
		/// <param name="input">the input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <see cref="SparseVector{T}"/>.</returns>
		/// <remarks>this method is actually static</remarks>
		public SparseVector<T> OperateOn(IReadOnlyList<SparseVector<T>> notJoinedVecs, T[] input)
		{
			if (input is null || input.Length == 0)
				throw new ArgumentNullException(nameof(input));
			if (notJoinedVecs is null || notJoinedVecs.Count != input.Length)
				throw new ArgumentNullException(nameof(notJoinedVecs));

			// sort first to reduce errors
			var sort = input.Select((x, i) => new KeyValuePair<int, T>(i, x)).OrderBy(x => x.Value).Select(x => x.Key).ToList();

			var vec = new DenseVector<T>(this.Length, this.OnHost);
			vec.FillWithZeros();
			try
			{
				foreach (var i in sort)
				{
					var spvec = notJoinedVecs[i];
					if (spvec is null || spvec.Length != this.Length)
						throw new ArgumentException(Resource.VectorWrongSize, nameof(notJoinedVecs));
					if (spvec.OnHost != this.OnHost || spvec.Disposed)
						throw new ArgumentException(Resource.VectorWrongValue, nameof(notJoinedVecs));
					vec.AddBy_αx(spvec, input[i]);
				}
				return vec.ToSparse();
			}
			finally
			{
				vec.Dispose();
			}
		}

		/// <summary>
		/// Replace the values of this vector by the one from <paramref name="other"/>, from <see cref="IKrylovVector{TVec, T}.ReplaceBy(TVec)"/>.
		/// </summary>
		/// <param name="other">the <see cref="SparseVector{T}"/> used to replace</param>
		/// <exception cref="ArgumentNullException">if <paramref name="other"/> is null</exception>
		/// <exception cref="ArgumentException">if <paramref name="other"/> has different non-zero values than this</exception>
		public void ReplaceBy(SparseVector<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other.NonZero != this.NonZero)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(other));
			RT.CopyTo(source: other.Pointer, dest: this.Pointer, length: this.NonZero);
			RT.CopyTo(source: other.IndexPointer, dest: this.IndexPointer, length: this.NonZero);
		}

		/// <summary>
		/// Compute $\vec{y} = \vec{y} + \alpha \vec{x}$, from <see cref="IKrylovVector{TVec, T}.AddBy_αx(TVec, T)"/>.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public void AddBy_αx(SparseVector<T> x, T α)
		{
			if (x is null || x == EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);

			SparseVector<T> sp;
			if (α.Equals(Scalars<T>.One))
			{
				sp = SPARSE.VectorSparseAddSparse(this, x);
			}
			else
			{
				using var v = x.CloneValueAlone();
				v.Scale(α);
				sp = SPARSE.VectorSparseAddSparse(this, v);
			}
			using (sp)
			{
				if (sp.NonZero == this.NonZero)
					this.ReplaceBy(sp);
				else
				{
					this.Pointer.ReplaceBy(sp.Pointer);
					this.IndexPointer.ReplaceBy(sp.IndexPointer);
				}
			}
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$
		/// </summary>
		/// <param name="A"><see cref="SparseMatrix{T}"/></param>
		/// <param name="x"><see cref="SparseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public void Mulβ_AddBy_αopAx(SparseMatrix<T> A, SparseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			if (x is null || x == EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			using var dx = x.ToDense();
			using var dy = this.ToDense();
			SPARSE.SparseMatrixDenseVectorMultiply(A, dx, dy, α, β, op);
			dx.Dispose();
			using var sp = dy.ToSparse();
			dy.Dispose();
			if (sp.NonZero == this.NonZero)
				this.ReplaceBy(sp);
			else
			{
				this.Pointer.ReplaceBy(sp.Pointer);
				this.IndexPointer.ReplaceBy(sp.IndexPointer);
			}
		}

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H (\text{or }\vec{v}_{\text{this}}^T) \vec{v}_{\text{other}}$, from <see cref="IKrylovVector{TVec, T}.Dot(TVec, bool?)"/>.
		/// </summary>
		/// <param name="other">the other <see cref="SparseVector{T}"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is symmetric (semi-symmetric, e.g. the conjugate relation, when data type is a complex type) for this vector and the other vector.</remarks>
		public T Dot(SparseVector<T> other, bool? conjugateThis = null)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (this.NonZero > other.NonZero)
			{
				using var ddx = other.ToDense();
				return SPARSE.VectorSparseDotDense(this, ddx, conjugateThis);
			}
			else
			{
				using var ddx = this.ToDense();
				return SPARSE.VectorSparseDotDense(other, ddx, conjugateThis).GenericConjugate();
			}
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$, from <see cref="IVector{TVec, T}.PointWiseMultiply(TVec)"/>.
		/// </summary>
		/// <param name="other">the other <see cref="SparseVector{T}"/></param>
		/// <returns>The result <see cref="SparseVector{T}"/>.</returns>
		/// <remarks>This method is symmetric since only the sparse vector one may be modified.</remarks>
		public void PointWiseMultiply(SparseVector<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			using var sx = other.ToDense();
			SPARSE.VectorSparseDensePointWiseMultiplyDivide(this, sx, multiply: true);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} ./ \vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$, from <see cref="IVector{TVec, T}.PointWiseDivide(TVec)"/>.
		/// </summary>
		/// <param name="other">the other <see cref="SparseVector{T}"/></param>
		/// <returns>The result <see cref="SparseVector{T}"/>.</returns>
		/// <exception cref="DivideByZeroException">ALWAYS</exception>
		public void PointWiseDivide(SparseVector<T> other)
		{
			throw new DivideByZeroException();
		}

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$ out-of-place, from <see cref="IVector{TVec, TMat, T}.OuterProduct"/>.
		/// </summary>
		/// <param name="other">the other input <see cref="SparseVector{T}"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite as result, default null</param>
		/// <returns>The result <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null</returns>
		public SparseMatrix<T> OuterProduct(SparseVector<T> other, bool? conjugateOther = null, SparseMatrix<T> overwrite = null)
		{
			if (other is null || other == EmptySpVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			SparseMatrix<T> mat;
			if (overwrite is null || overwrite == EmptySpMat || overwrite.NRows < this.Length || overwrite.NCols != other.Length || overwrite.NonZero < this.NonZero * other.NonZero || overwrite.Format != SparseMatrixFormat.COOC)
				mat = new SparseMatrix<T>(this.Length, other.Length, this.NonZero * other.NonZero, SparseMatrixFormat.COOC, this.OnHost);
			else
				mat = overwrite;
			try
			{
				SPARSE.VectorSparseOuterSparse(this, other, mat, conjugateOther ?? !this.IsRealType);
				return mat;
			}
			catch (Exception)
			{
				if (mat != overwrite) mat.Dispose();
				throw;
			}
		}
		#endregion

		#region dense vector dense matrix restricted operations
		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result vector <see cref="DenseVector{T}"/>. From <see cref="IKrylovVector{TVec, T}.OperateOn(IReadOnlyList{TVec}, T[])"/>
		/// </summary>
		/// <param name="notJoinedVecs">the columns of the matrix to operate</param>
		/// <param name="input">the input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <see cref="DenseVector{T}"/>.</returns>
		/// <remarks>this method is actually static</remarks>
		public DenseVector<T> OperateOn(IReadOnlyList<DenseVector<T>> notJoinedVecs, T[] input) => notJoinedVecs is null || notJoinedVecs.Count == 0 ? throw new ArgumentNullException(nameof(notJoinedVecs)) : notJoinedVecs[0].OperateOn(notJoinedVecs, input);

		/// <summary>
		/// Replace the values of this vector by the one from <paramref name="other"/>, from <see cref="IKrylovVector{TVec, T}.ReplaceBy(TVec)"/>.
		/// </summary>
		/// <param name="other">the <see cref="DenseVector{T}"/> used to replace</param>
		/// <exception cref="ArgumentNullException">if <paramref name="other"/> is null</exception>
		/// <exception cref="ArgumentException">if length of <paramref name="other"/> is not same as <see cref="NonZero"/></exception>
		public void ReplaceBy(DenseVector<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other.Length != this.NonZero)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(other));
			RT.CopyTo(source: other.Pointer, dest: this.Pointer, length: this.NonZero);
		}

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H (\text{or }\vec{v}_{\text{this}}^T) \vec{v}_{\text{other}}$, from <see cref="IKrylovVector{TVec, T}.Dot(TVec, bool?)"/>.
		/// </summary>
		/// <param name="other">the other <see cref="DenseVector{T}"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is symmetric (semi-symmetric, e.g. the conjugate relation, when data type is a complex type) for this vector and the other vector.</remarks>
		public T Dot(DenseVector<T> other, bool? conjugateThis = null)
		{
			return SPARSE.VectorSparseDotDense(this, other, conjugateThis);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$, from <see cref="IVector{TVec, T}.PointWiseMultiply(TVec)"/>.
		/// </summary>
		/// <param name="other">the other <see cref="DenseVector{T}"/></param>
		/// <remarks>This method is symmetric since only the sparse vector one may be modified.</remarks>
		public void PointWiseMultiply(DenseVector<T> other)
		{
			SPARSE.VectorSparseDensePointWiseMultiplyDivide(this, other, multiply: true);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} ./ \vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$, from <see cref="IVector{TVec, T}.PointWiseDivide(TVec)"/>.
		/// </summary>
		/// <param name="other">the other <see cref="DenseVector{T}"/></param>
		/// <exception cref="DivideByZeroException">ALWAYS</exception>
		public void PointWiseDivide(DenseVector<T> other)
		{
			SPARSE.VectorSparseDensePointWiseMultiplyDivide(this, other, multiply: false);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \vec{v}_{\text{this}} + \alpha \vec{x}$.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <returns>a new <see cref="DenseVector{T}"/> as the result</returns>
		public DenseVector<T> AddBy_αx(DenseVector<T> x, T α)
		{
			var dv = this.ToDense();
			try
			{
				BLAS.VectorAddBy(dv, x, α);
				return dv;
			}
			catch (Exception)
			{
				dv?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{result}} = \beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$
		/// </summary>
		/// <param name="A">input <see cref="DenseMatrix{T}"/></param>
		/// <param name="x">input <see cref="DenseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		/// <returns>The result <see cref="DenseVector{T}"/>.</returns>
		public DenseVector<T> Mulβ_AddBy_αopAx(DenseMatrix<T> A, DenseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			// since the vector to add is a dense one, a new dense vector is created and returned
			var v = this.ToDense();
			try
			{
				BLAS.MatrixVectorMultiply(A, x, v, α, β, op);
				return v;
			}
			catch (Exception)
			{
				v.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$ out-of-place, from <see cref="IVector{TVec, TMat, T}.OuterProduct"/>.
		/// </summary>
		/// <param name="other">the other input <see cref="DenseVector{T}"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite as result, default null</param>
		/// <returns>The result <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null</returns>
		public DenseMatrix<T> OuterProduct(DenseVector<T> other, bool? conjugateOther = null, DenseMatrix<T> overwrite = null)
		{
			using var dv = this.ToDense();
			return dv.OuterProduct(other, conjugateOther, overwrite);
		}
		#endregion

		#region dense vector sparse matrix restricted operations
		/// <summary>
		/// Compute $\beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$
		/// </summary>
		/// <param name="A">input <see cref="SparseMatrix{T}"/></param>
		/// <param name="x">input <see cref="DenseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		/// <returns>The result <see cref="DenseVector{T}"/>.</returns>
		public DenseVector<T> Mulβ_AddBy_αopAx(SparseMatrix<T> A, DenseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			if (x is null || x == EmptyDnVec)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			var dy = this.ToDense();
			try
			{
				SPARSE.SparseMatrixDenseVectorMultiply(A, x, dy, α, β, op);
				return dy;
			}
			catch (Exception)
			{
				dy.Dispose();
				throw;
			}
		}
		#endregion

		#region sparse vector dense matrix restricted operations
		/// <summary>
		/// Compute $\beta \cdot \vec{v}_{\text{this}} + \alpha \cdot A \vec{x}$
		/// </summary>
		/// <param name="A">input <see cref="DenseMatrix{T}"/></param>
		/// <param name="x">input <see cref="SparseVector{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		/// <returns>The result <see cref="SparseVector{T}"/></returns>
		public DenseVector<T> Mulβ_AddBy_αopAx(DenseMatrix<T> A, SparseVector<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			// since the vector to add is a dense one, a new dense vector is created and returned
			var v = this.ToDense();
			try
			{
				SPARSE.DenseMatrixSparseVectorMultiply(A, x, v, α, β, op);
				return v;
			}
			catch (Exception)
			{
				v.Dispose();
				throw;
			}
		}
		#endregion


		#region implement abstract operations
		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result vector <see cref="VectorBase{T}"/>. From <see cref="VectorBase{T}.OperateOn"/>
		/// </summary>
		/// <param name="notJoinedVecs">the columns of the matrix to operate</param>
		/// <param name="input">the input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <see cref="VectorBase{T}"/>.</returns>
		/// <remarks>this method is actually static</remarks>
		public override VectorBase<T> OperateOn(IReadOnlyList<VectorBase<T>> notJoinedVecs, T[] input)
		{
			if (input is null || input.Length == 0)
				throw new ArgumentNullException(nameof(input));
			if (notJoinedVecs is null || notJoinedVecs.Count != input.Length)
				throw new ArgumentNullException(nameof(notJoinedVecs));

			return this.OperateOn(notJoinedVecs as IReadOnlyList<SparseVector<T>>, input);
		}

		/// <summary>
		/// Replace the values of this vector by the one from <paramref name="other"/>, from <see cref="IKrylovVector{TVec, T}.ReplaceBy(TVec)"/>.
		/// </summary>
		/// <param name="other">the <see cref="VectorBase{T}"/> used to replace</param>
		/// <exception cref="ArgumentNullException">if <paramref name="other"/> is null</exception>
		/// <exception cref="ArgumentException">if <paramref name="other"/> has different non-zero values than this</exception>
		public override void ReplaceBy(VectorBase<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dx)
			{
				this.ReplaceBy(dx);
			}
			else if (other is SparseVector<T> sx)
			{
				this.ReplaceBy(sx);
			}
			else
			{
				var dv = other.AsDenseVector();
				this.ReplaceBy(dv);
			}
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \vec{v}_{\text{this}} + \alpha \vec{x}$.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public override void AddBy_αx(VectorBase<T> x, T α)
		{
			if (x is DenseVector<T>)
			{
				throw new InvalidOperationException();
			}
			else if (x is SparseVector<T> sv)
			{
				x.AddBy_αx(sv, α);
			}
			else
			{
				x.AddBy_αx_Opposite(this, α);
			}
		}

		/// <summary>
		/// Compute $\vec{y}_{\text{this}} = \beta \cdot \vec{y}_{\text{this}} + \alpha \cdot A^{\text{op}} \vec{x}$.
		/// </summary>
		/// <param name="x">the input <see cref="VectorBase{T}"/></param>
		/// <param name="A">the input <see cref="MatrixBase{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public override void Mulβ_AddBy_αopAx(MatrixBase<T> A, VectorBase<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			if (x is DenseVector<T> || A is DenseMatrix<T>)
			{
				throw new InvalidOperationException();
			}
			else if (x is SparseVector<T> sv && A is SparseMatrix<T> sA)
			{
				this.Mulβ_AddBy_αopAx(sA, sv, α, β, op);
			}
			else
			{
				x.Mulβ_AddBy_αopAx_Opposite(A, this, α, β, op);
			}
		}
	
		/// <summary>
		/// DO NOT call this method since the class <see cref="SparseVector{T}"/> has no need to implement it.
		/// </summary>
		/// <exception cref="NotImplementedException">in any case</exception>
		/// <remarks>See <see cref="VectorBase{T}.AddBy_αx_Opposite(VectorBase{T}, T)"/></remarks>
		protected internal override void AddBy_αx_Opposite(VectorBase<T> y, T α)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(SparseVector<T>)));
		}

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H (\text{or }\vec{v}_{\text{this}}^T) \vec{v}_{\text{other}}$, override <see cref="VectorBase{T}.Dot(VectorBase{T}, bool?)"/>.
		/// </summary>
		/// <param name="other">the other <see cref="VectorBase{T}"/></param>
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
				return other.Dot(this).GenericConjugate();
			}
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$, override <see cref="VectorBase{T}.PointWiseMultiply(VectorBase{T})"/>.
		/// </summary>
		/// <param name="other">the other <see cref="VectorBase{T}"/></param>
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
		/// Compute $\vec{v}_{\text{this}} ./ \vec{v}_2 \equiv \{\vec{v}_{\text{this}}^i \vec{v}_2^i\}_i$, override <see cref="VectorBase{T}.PointWiseDivide(VectorBase{T})"/>.
		/// </summary>
		/// <param name="other">the other <see cref="VectorBase{T}"/></param>
		/// <exception cref="DivideByZeroException">if the <paramref name="other"/> vector is sparse</exception>
		public override void PointWiseDivide(VectorBase<T> other)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dx)
			{
				this.PointWiseDivide(dx);
			}
			else if (other is SparseVector<T> sx)
			{
				this.PointWiseDivide(sx);
			}
			else
			{
				other.PointWiseDivide_Opposite(this);
			}
		}

		/// <summary>
		/// DO NOT call this method since the class <see cref="SparseVector{T}"/> has no need to implement it.
		/// </summary>
		/// <exception cref="NotImplementedException">in any case</exception>
		/// <remarks>See <see cref="VectorBase{T}.PointWiseDivide_Opposite(VectorBase{T})"/></remarks>
		protected internal override void PointWiseDivide_Opposite(VectorBase<T> other)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(SparseVector<T>)));
		}

		/// <summary>
		/// DO NOT call this method since the class <see cref="SparseVector{T}"/> has no need to implement it.
		/// </summary>
		/// <exception cref="NotImplementedException">in any case</exception>
		/// <remarks>See <see cref="VectorBase{T}.Mulβ_AddBy_αopAx_Opposite(MatrixBase{T}, VectorBase{T}, T, T, MatrixOperation)"/></remarks>
		protected internal override void Mulβ_AddBy_αopAx_Opposite(MatrixBase<T> A, VectorBase<T> y, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(SparseVector<T>)));
		}

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$ out-of-place. Override <see cref="VectorBase{T}.OuterProduct"/>
		/// </summary>
		/// <param name="other">the other input <see cref="VectorBase{T}"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite as result, default null</param>
		/// <returns>The result <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null</returns>
		public override MatrixBase<T> OuterProduct(VectorBase<T> other, bool? conjugateOther = null, MatrixBase<T> overwrite = null)
		{
			if (other is null || other == EmptyDnVec)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (other is DenseVector<T> dv)
			{
				return this.OuterProduct(dv, conjugateOther, overwrite as DenseMatrix<T>);
			}
			else if (other is SparseVector<T> sv)
			{
				return this.OuterProduct(sv, conjugateOther, overwrite as SparseMatrix<T>);
			}
			else
			{
				var ssv = other.ToSparse();
				try
				{
					return this.OuterProduct(ssv, conjugateOther, overwrite as SparseMatrix<T>);
				}
				finally
				{
					if (ssv != other) ssv.Dispose();
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
		/// <exception cref="AccessViolationException">if you are trying to insert into this sparse array</exception>
		public override T this[Index i] {
			get {
				var ind = checked((int)CheckRange(i));
				var find = SPARSE.IndexFind(this.IndexPointer, this.IntNNZ, ind);
				if (find >= 0)
				{
					return RT.CopyOut(this, offset: find); // get value at index 'find'
				}
				else
				{
					return default;
				}
			}
			set {
				var ind = checked((int)CheckRange(i));
				var find = SPARSE.IndexFind(this.IndexPointer, this.IntNNZ, ind);
				if (find >= 0)
				{
					RT.CopyInto(this, value, offset: find);
					RT.CopyInto(this.IndexPointer, ind, offset: find);
				}
				else
				{
					throw new AccessViolationException(Resource.InsertSparse);
				}
			}
		}

		private T this[int i] {
			set {
				var find = SPARSE.IndexFind(this.IndexPointer, this.IntNNZ, i);
				if (find >= 0)
				{
					RT.CopyInto(this, value, offset: find);
					RT.CopyInto(this.IndexPointer, i, offset: find);
				}
				else
				{
					throw new AccessViolationException(Resource.InsertSparse);
				}
			}
		}

		/// <summary>
		/// Single range indexer of vector.
		/// </summary>
		/// <param name="r">the <see cref="Range"/> of index</param>
		/// <returns>The reference <see cref="SparseVector{T}"/> of the selected range</returns>
		/// <remarks>
		/// The getter returns a reference <see cref="SparseVector{T}"/>. <br/>
		/// The setter value will be used as dense vector <see cref="PureArray{T}.AsDenseVector"/>. The sparse pattern (if any) will not be considered.
		/// </remarks>
		/// <exception cref="OverflowException">if <paramref name="r"/> cannot be casted into <see cref="int"/> without loss</exception>
		public override VectorBase<T> this[Range r] {
			get {
				var (startL, countL) = CheckRange(r);
				int start = checked((int)startL), count = checked((int)countL);
				var lower = SPARSE.IndexLowerUpperBound(this.IndexPointer, this.IntNNZ, start, lowerBound: true);
				var upper = SPARSE.IndexLowerUpperBound(this.IndexPointer, this.IntNNZ, start + count - 1, lowerBound: false);
				return new SparseVector<T>(this, count, upper - lower, offset: lower);
			}
			set {
				if (value is null || value == EmptyDnVec)
					return;
				var (startL, countL) = CheckRange(r);
				int start = checked((int)startL), count = checked((int)countL);
				var lower = SPARSE.IndexLowerUpperBound(this.IndexPointer, this.IntNNZ, start, lowerBound: true);
				var upper = SPARSE.IndexLowerUpperBound(this.IndexPointer, this.IntNNZ, start + count - 1, lowerBound: false);
				var v = value.AsDenseVector();
				if (v.Length < upper - lower)
					throw new ArgumentException(Resource.VectorTooShort, nameof(value));
				RT.CopyTo(source: v, dest: this, length: upper - lower, offsetDest: lower);
			}
		}

		/// <summary>
		/// Multiple position indexer of vector.
		/// </summary>
		/// <param name="indices">positions indicated by array of <see cref="Index"/></param>
		/// <returns>All the values on the indices at a copied <see cref="DenseVector{T}"/></returns>
		/// <remarks>This indexer is implemented by <see cref="this[Index]"/> which may be of bad performance.</remarks>
		public override DenseVector<T> this[params Index[] indices] {
			get {
				return (DenseVector<T>)(indices.Select(a => this[a]).ToArray(), this.OnHost);
			}
			set {
				if (value is null || value == EmptyDnVec)
					return;
				var idx = CheckRange(indices);
				if (value.ActualLength < idx.Length)
					throw new ArgumentException(Resource.VectorTooShort, nameof(value));
				var v = RT.CopyOutArray(value, length: idx.Length);
				Array.Sort(idx, v);
				for (int j = 0; j < idx.Length; j++)
				{
					this[checked((int)idx[j])] = v[j];
				}
			}
		}

		private DenseVector<T> this[int[] indices] {
			set {
				if (value.ActualLength < indices.Length)
					throw new ArgumentException(Resource.VectorTooShort, nameof(value));
				var v = RT.CopyOutArray(value, length: indices.Length);
				Array.Sort(indices, v);
				for (int j = 0; j < indices.Length; j++)
				{
					this[indices[j]] = v[j];
				}
			}
		}

		/// <summary>
		/// Multiple range indexer of vector.
		/// </summary>
		/// <param name="ranges">the array of <see cref="Range"/> of indices</param>
		/// <returns>All the values in the ranges at a copied <see cref="VectorBase{T}"/></returns>
		/// <remarks>The getter copies the values in the ranges while the setter calls <see cref="PureArray{T}.AsDenseVector"/> before utilizing the <see cref="this[int[]]"/>, which may be of bad performance.</remarks>
		public override DenseVector<T> this[params Range[] ranges] {
			get {
				CheckRange(ranges);
				var vecs = ranges.Select(i => this[i] as SparseVector<T>);
				long nnz = vecs.Sum(v => v.NonZero);
				var vals = vecs.Select(v => v.Pointer).ToArray();
				var lens = vecs.Select(v => v.NonZero).ToArray();
				var offsets = lens.AccumulateSum();
				// create long enough arrays
				using var newValue = Storage<T>.Create(nnz, this.OnHost);
				// copy into new arrays
				for (int i = 0; i < ranges.Length; i++)
				{
					RT.CopyTo(source: vals[i], dest: newValue, length: lens[i], offsetDest: offsets[i]);
				}
				return new DenseVector<T>(newValue, nnz);
			}
			set {
				if (value is null || value == EmptyDnVec)
					return;
				var index = CheckRange(ranges);
				this[Array.ConvertAll(index, a => (int)a)] = value.AsDenseVector();
			}
		}
		#endregion


		#region managed converter
		/// <summary>
		/// Convert from managed C# arrays.
		/// </summary>
		/// <param name="value">value C# array of type <typeparamref name="T"/>, index C# array of type <see cref="long"/>, display length and on host indicator</param>
		public static explicit operator SparseVector<T>((T[] values, int[] indices, long length, bool onHost) value)
		{
			var (values, indices, length, onHost) = value; // unpack
			long nnz = values.LongLength;
			if (nnz != indices.LongLength)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(value));
			var vec = new SparseVector<T>(length, nnz, onHost);
			try
			{
				RT.CopyIntoArray(vec, values);
				RT.CopyIntoArray(vec.IndexPointer, indices);
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
			return vec;
		}
		#endregion


		#region equality
		/// <summary>
		/// Check if this <see cref="PureArray{T}"/> share some memory / data with <paramref name="another"/> one
		/// </summary>
		/// <param name="another">another <see cref="AbstractArray{T}"/> to check</param>
		/// <returns>True if they do share some memory / data, false otherwise</returns>
		public override bool ShareMemoryWith(AbstractArray<T> another)
		{
			if (base.ShareMemoryWith(another))
				return true;
			else if (another is SparseVector<T> sv)
			{
				return this.IndexPointer.ShareMemoryWith(sv.IndexPointer);
			}
			else if (another is SparseMatrix<T> sm)
			{
				return this.IndexPointer.ShareMemoryWith(sm.RowPointer) || this.IndexPointer.ShareMemoryWith(sm.ColumnPointer);
			}
			else
				return false;
		}

		/// <summary>
		/// Override <see cref="PureArray{T}.GetHashCode"/> to get the hash code this array.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), this.IndexPointer);

		/// <summary>
		/// Whether this array is equal to another one, override <see cref="AbstractArray{T}.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another <see cref="SparseVector{T}"/></param>
		public override bool Equals(object obj)
		{
			if (obj is null || !(obj is PureArray<T> a))
				return false;
			else if (this.Pointer != a.Pointer)
				return false;
			if (obj is SparseVector<T> s)
				return this.IndexPointer == s.IndexPointer && this.NonZero == s.NonZero;
			return false;
		}
		#endregion


		#region print
		/// <summary>
		/// Override <see cref="PureArray{T}.ToString()"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return base.ToString(new Dictionary<string, object>
			{
				["value_address"] = $"0x{this.Pointer.ToHexString()}",
				["index_address"] = $"0x{this.IndexPointer.ToHexString()}",
				["non_zeros"] = this.NonZero,
			}, new[] { StringTerms.DataType, StringTerms.Size });
		}

		private (int[] indices, T[] values) Raw(IReadOnlyDictionary<PrintSetting, int> config = null)
		{
			config ??= GlobalSettings.PrintConfig;
			long length = Math.Min(config[PrintSetting.ArrayLength], this.NonZero);
			T[] res = RT.CopyOutArray(this.Pointer, length);
			int[] ind = RT.CopyOutArray(this.IndexPointer, length);
			return (ind, res);
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
			var (ind, res) = this.Raw(printConfig);
			detail += res.ToSparseVectorString(ind, precision: printConfig[PrintSetting.Precision]);
			if (this.NonZero > res.LongLength)
				detail += Environment.NewLine + $"...{this.NonZero - res.Length} more elements";

			return description + detail;
		}
		#endregion


		#region serialize
		/// <summary>
		/// Get the pointers of this instance.
		/// </summary>
		/// <returns>the pointers</returns>
		public override IReadOnlyDictionary<string, IPointer> GetPointers() => SparseVectorFactory.GetPointers(this);

		/// <summary>
		/// Get other requisite informations for re-constructing this array.
		/// </summary>
		/// <returns>other requisite informations</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => SparseVectorFactory.GetOtherInfo(this);
		#endregion
	}
}
