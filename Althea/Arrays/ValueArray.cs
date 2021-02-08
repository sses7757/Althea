using System;
using System.Text;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Storage;

using RT = Althea.Runtime.API;
using BLAS = Althea.Blas.API;
using RAND = Althea.Rng.API;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract device array class for array whose <see cref="ValueArray{T}.Pointer"/> only refers to the data array. There may be more pointer(s) standing for different indices in a sparse array, in which case the <see cref="ValueArray{T}.Pointer"/> only refers to the value data array.
	/// </summary>
	/// <typeparam name="T">the supported data type</typeparam>
	public abstract class ValueArray<T> : AbstractArray<T>, IMutableArray<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region empty arrays
		/// <summary>
		/// The general empty dense vector with zero length
		/// </summary>
		public static readonly DenseVector<T> EmptyDnVec = new DenseVector<T>();
		/// <summary>
		/// The general empty sparse vector with zero length
		/// </summary>
		public static readonly SparseVector<T> EmptySpVec = new SparseVector<T>();
		/// <summary>
		/// The general empty dense matrix with zero length
		/// </summary>
		public static readonly DenseMatrix<T> EmptyDnMat = new DenseMatrix<T>();
		/// <summary>
		/// The general empty sparse matrix with zero length
		/// </summary>
		public static readonly SparseMatrix<T> EmptySpMat = new SparseMatrix<T>();
		/// <summary>
		/// The general empty dense tensor with zero length
		/// </summary>
		public static readonly DenseTensor<T> EmptyDnTen = new DenseTensor<T>();
		#endregion

		#region new device array members
		/// <summary>
		/// The array is on the host memory or on device memory
		/// </summary>
		public bool OnHost => this.Pointer.OnHost;

		/// <summary>
		/// The read-only raw data pointer
		/// </summary>
		internal protected Storage<T> Pointer { get; internal set; }

		/// <summary>
		/// Total actual length of the value array in memory, in <typeparamref name="T"/> rather than bytes
		/// </summary>
		public virtual long ActualLength => this.Pointer.Length;
		#endregion

		#region initialize and destroy
		/// <summary>
		/// Constructor with size indicated alone while the actual array size in memory is a dedicated one.
		/// </summary>
		/// <param name="actualLength">the actual size of the array to allocate in device memory, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="size">size of the new array</param>
		/// <param name="onHost">allocate one host memory or device memory</param>
		protected ValueArray(long actualLength, IReadOnlyList<long> size, bool onHost) : base(size)
		{
			this.Pointer = Storage<T>.Create(actualLength, onHost: onHost);
		}

		/// <summary>
		/// Create using exist <see cref="Storage{T}"/>
		/// </summary>
		/// <param name="storage">the existing <see cref="Storage{T}"/></param>
		/// <param name="size">size of this array</param>
		protected ValueArray(Storage<T> storage, IReadOnlyList<long> size) : base(size)
		{
			if (this.Length != storage.Length)
				throw new ArgumentException(Resource.ArraySize, nameof(size));
			this.Pointer = storage;
		}

		/// <summary>
		/// The root reference of this array
		/// </summary>
		protected readonly ValueArray<T> _root = null;

		/// <summary>
		/// Reshape constructor.
		/// </summary>
		/// <param name="refArray">the original <see cref="ValueArray{T}"/></param>
		/// <param name="actualLength">the actual size of the array, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="newSize">the new array's size</param>
		/// <param name="offset">offset in <typeparamref name="T"/> rather than bytes</param>
		protected ValueArray(ValueArray<T> refArray, long actualLength, IReadOnlyList<long> newSize, long offset = 0) : base(newSize)
		{
			if (refArray is null)
				throw new ArgumentNullException(nameof(refArray), Resource.ArrayCannotNull);
			this.Pointer = (refArray.Pointer + offset).MakeSize(actualLength);
			this._root = refArray._root ?? refArray;
			////GC.SuppressFinalize(this); // since this is a referenced array
		}

		/// <summary>
		/// The function that actually implements the dispose functionality
		/// </summary>
		/// <param name="disposing">dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			if (this.Disposed || this.Length == 0 || this.Pointer is null || !(this._root is null))
			{
				this.Disposed = true;
				return;
			}
			this.Pointer.Dispose();
			if (disposing)
			{
				////base.Dispose(true);
			}
			this.Disposed = true;
		}
		#endregion

		#region IMutableArray<T>
		/// <summary>
		/// Fill this array's <see cref="Pointer"/> with zeros.
		/// </summary>
		public void FillWithZeros()
		{
			RT.SetValue(this.Pointer, 0, this.ActualLength);
		}

		/// <summary>
		/// Fill this array's <see cref="Pointer"/> with random numbers in [0, 1).
		/// </summary>
		public void FillWithRandoms()
		{
			RAND.FillWithRandom(this);
		}

		/// <summary>
		/// Fill this array's <see cref="Pointer"/> with ones.
		/// </summary>
		public void FillWithOnes()
		{
			BLAS.FillWithOnes(this);
		}
		#endregion

		#region reshape
		/// <summary>
		/// Flatten the array to a vector.
		/// </summary>
		/// <returns>The flattened vector</returns>
		public virtual ValueArray<T> ToVector() => new DenseVector<T>(this, this.ActualLength);

		/// <summary>
		/// Check the new size (dimensionality) to reshape to with respect to the original one
		/// (this one) and find out the uncertain dimension.
		/// </summary>
		/// <param name="newSize">new size (dimensionality)</param>
		/// <returns>the new size without uncertain dimension or throws an error</returns>
		protected virtual long[] CheckSize(long[] newSize)
		{
			if (newSize is null)
				throw new ArgumentNullException(nameof(newSize));
			if (newSize.Length == 0)
				throw new ArgumentException(Resource.RankZero, nameof(newSize));
			if (newSize.Length == 2 && newSize.All(s => s <= 0)) // try to convert to a square matrix
			{
				if (!this.Length.IsPerfectSquare())
				{
					throw new ArgumentException(Resource.PerfectSquare);
				}
				var leadDim = Convert.ToInt64(Math.Sqrt(this.Length));
				return new[] { leadDim, leadDim };
			}
			int uncertainCount = newSize.Count(r => r <= 0);
			if (uncertainCount == 1)
			{
				int i = newSize.Select(r => r < 0 ? 0 : r).ToList().IndexOf(0);
				var prod = newSize.Where(r => r > 0).Prod();
				var remain = this.Length % prod;
				if (remain != 0)
					throw new ArgumentOutOfRangeException(nameof(newSize));
				else
					newSize[i] = this.Length / newSize.Where(r => r > 0).Prod();
			}
			else if (uncertainCount > 1)
			{
				throw new ArgumentOutOfRangeException(nameof(newSize));
			}
			else if (newSize.Prod() != this.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(newSize));
			}
			newSize = newSize.Where(r => r != 1).ToArray();
			return newSize;
		}

		/// <summary>
		/// Reshape the array (no new memory will be allocated).
		/// </summary>
		/// <param name="newSize">the new dimensions.
		/// You can have one uncertain dimension, indicated by a non-positive number.</param>
		/// <returns>The reshaped array. Note that if out-of-place reshape is performed, the returned one cannot undo reshape.</returns>
		public ValueArray<T> Reshape(params long[] newSize)
		{
			newSize = this.CheckSize(newSize);
			if (newSize.SequenceEqual(this.Size))
				return this;
			return newSize.Length switch
			{
				0 => throw new ArgumentException(Resource.RankZero, nameof(newSize)),
				1 => this.ToVector(),
				2 => this.ToMatrix(newSize[0]),
				_ => this.ToTensor(size: newSize),
			};
		}

		/// <summary>
		/// Take out the data array as a new <see cref="DenseVector{T}"/>.
		/// </summary>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the referenced data array of this one.</returns>
		public virtual DenseVector<T> AsDenseVector() => new DenseVector<T>(this, this.ActualLength);

		/// <summary>
		/// Default implementation. Reshape the array to a <see cref="DenseMatrix{T}"/> with leading dimension = <paramref name="leadDim"/>.
		/// <br/>The default implementation assumes that the data is not aligned in memory, i.e., there is no extra pitch.
		/// </summary>
		/// <param name="leadDim">leading dimension of matrix; if leadDim ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public virtual ValueArray<T> ToMatrix(long leadDim = 0)
		{
			var newSize = this.CheckSize(new[] { leadDim, 0 });
			leadDim = newSize[0];
			var secondDim = newSize[1];
			return new DenseMatrix<T>(this, leadDim, secondDim);
		}

		/// <summary>
		/// Reshape the array to a general <see cref="DenseTensor{T}"/> with dimensionality = size.
		/// <br/>The default implementation assumes that the data is not aligned in memory, i.e., there is no extra pitch.
		/// </summary>
		/// <param name="size">The new dimensions. You can have one or zero uncertain dimension, indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public virtual ValueArray<T> ToTensor(params long[] size)
		{
			size = this.CheckSize(size);
			return new DenseTensor<T>(this, size);
		}

		/// <summary>
		/// Create a new array like this one (with same type and other info) while the data type is <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the new data type</typeparam>
		/// <returns>the new array</returns>
		public abstract ValueArray<TOut> NewArrayAlike<TOut>() where TOut : struct, IComparable<TOut>;

		/// <summary>
		/// Cast this array into another data type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">the data type to cast to</typeparam>
		/// <returns>The casted <see cref="ValueArray{TOut}"/></returns>
		/// <exception cref="NotSupportedException">if the cast from <typeparamref name="T"/> to <typeparamref name="TOut"/> is not supported</exception>
		public override AbstractArray<TOut> DataTypeCast<TOut>()
		{
			DataType typeT = default(T).ToDataType(), typeOut = default(TOut).ToDataType();
			if (typeT == typeOut)
				return this as ValueArray<TOut>;
			if (!typeT.IsFloat())
				throw new NotSupportedException(Resource.DataTypeNotSupport);
			var arr = this.NewArrayAlike<TOut>();
			try
			{
				if (!typeOut.IsReal() && typeT.IsReal()) // cast from real to complex
				{
					BLAS.PointWiseToComplex(this, arr);
				}
				else if (typeT.Bytes() == 4 && typeOut.Bytes() == 8) // cast from float to double
				{
					BLAS.PointWiseUpcast(this, arr);
				}
				else if(typeOut.IsReal() && !typeT.IsReal()) // cast from complex to real
				{
					var srcPtr = this.Pointer.As<TOut>();
					BLAS.VectorGenralCopy(arr, new DenseVector<TOut>(srcPtr, srcPtr.Length), arr.ActualLength, strideSrc: 2);
				}
				else
					throw new NotSupportedException(Resource.DataTypeNotSupport);
				return arr;
			}
			catch (Exception)
			{
				arr.Dispose();
				throw;
			}
		}

		#endregion

		#region overrides
		/// <summary>
		/// Check if this <see cref="ValueArray{T}"/> share some memory / data with <paramref name="another"/> one
		/// </summary>
		/// <param name="another">another <see cref="AbstractArray{T}"/> to check</param>
		/// <returns>True if they do share some memory / data, false otherwise</returns>
		/// <remarks>The default implementation only works for arrays with only one <see cref="Pointer"/></remarks>
		public override bool ShareMemoryWith(AbstractArray<T> another)
		{
			if (another is ValueArray<T> arr)
			{
				ValueArray<T> a = this._root ?? this, b = arr._root ?? arr;
				if (a.Equals(b))
					return true;
				else
					return a.Pointer.ShareMemoryWith(b.Pointer);
			}
			else
				return false;
		}

		/// <summary>
		/// The string representation terms
		/// </summary>
		protected enum StringTerms
		{
			/// <summary>
			/// The data type
			/// </summary>
			DataType,
			/// <summary>
			/// The memory address
			/// </summary>
			Address,
			/// <summary>
			/// The presenting size
			/// </summary>
			Size
		}

		/// <summary>
		/// Get the string representation of this array with new terms and existed ones (existed ones are shown at first).
		/// </summary>
		/// <param name="terms">the additional terms</param>
		/// <param name="include">the include terms, default null means all</param>
		/// <returns>the string representation</returns>
		protected string ToString(IEnumerable<KeyValuePair<string, object>> terms, IEnumerable<StringTerms> include = null)
		{
			// default values
			if (include is null || System.Linq.Enumerable.Count(include) == 0)
				include = new[] { StringTerms.DataType, StringTerms.Address, StringTerms.Size };
			if (terms is null)
				terms = Array.Empty<KeyValuePair<string, object>>();
			// get type name of this array
			var type = this.GetType();
			string name = type.Name;
			if (type.IsGenericType)
			{
				name = name.Replace("`" + type.GenericTypeArguments.Length, "", CudaCSharpConverters.StrCmp);
			}
			// output include terms and other terms
			StringBuilder output = new StringBuilder(name);
			output.Append(this.Disposed ? " (disposed)" : "");
			output.Append(" at ");
			output.Append(this.OnHost ? "host" : $"device {RT.DeviceNo}");
			output.Append(" [");
			foreach (var item in include)
			{
				output.Append(item switch
				{
					StringTerms.DataType => $"data_type={typeof(T).Name}",
					StringTerms.Address => $"address=0x{this.Pointer.ToHexString()}",
					StringTerms.Size => $"size={string.Join("x", Size)}",
					_ => "",
				});
				output.Append(", ");
			}
			foreach (var item in terms)
			{
				output.Append(item.Key);
				output.Append("=");
				output.Append(item.Value);
				output.Append(", ");
			}
			return output.Remove(output.Length - 2, 2) + "]";
		}

		/// <summary>
		/// Override <see cref="AbstractArray{T}.ToString"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return this.ToString(terms: null);
		}

		/// <summary>
		/// Override <see cref="AbstractArray{T}.GetHashCode"/> to get the hash code this array.
		/// </summary>
		/// <returns>The hash code computed by <see cref="Pointer"/> and <see cref="AbstractArray{T}.Size"/></returns>
		public override int GetHashCode() => HashCode.Combine(this.Pointer, this.Size.HashCodeOfArray());

		/// <summary>
		/// Whether this object is equal to another, the shapes / sizes are also compared
		/// </summary>
		/// <param name="obj"></param>
		public override bool Equals(object obj)
		{
			if (obj is null || !(obj is ValueArray<T> a))
				return false;
			else
				return this.Pointer == a.Pointer && this.Size.SequenceEqual(a.Size);
		}
		#endregion

		#region override operators
		//Ignore Spelling: stackrel
		/// <summary>
		/// Array and value compare with threshold
		/// </summary>
		/// <param name="a">array</param>
		/// <param name="v">value</param>
		/// <returns>$$a \stackrel{?}{=} v$$</returns>
		public static bool operator ==(ValueArray<T> a, T v)
		{
			if (a is null || a == EmptyDnVec)
				return false;
			using var da = (a.Clone() as ValueArray<T>).ToVector();
			if (!v.Equals(Scalars<T>.Zero))
			{
				using var ones = new DenseVector<T>(da.ActualLength, a.OnHost);
				BLAS.FillWithOnes(ones);
				BLAS.VectorAddBy(da, ones, v.GenericNegate());
			}
			long index = BLAS.VectorAbsArgmax(da);
			T val = RT.CopyOut(a, index);
			T mval = val.GenericNegate();
			bool condition1 = val.CompareTo(GlobalSettings.EqualThreshold.FromDouble<T>()) <= 0 &&
							  mval.CompareTo(val) <= 0; // v >= 0 and v <= threshold
			bool condition2 = mval.CompareTo(GlobalSettings.EqualThreshold.FromDouble<T>()) <= 0 &&
							  val.CompareTo(mval) <= 0; // v <= 0 and -v <= threshold
			return condition1 || condition2;
		}

		/// <summary>
		/// Array and value compare with threshold
		/// </summary>
		/// <param name="a">array treated as a vector</param>
		/// <param name="v">value</param>
		/// <returns>$$\vec{a} - v \stackrel{?}{=} 0$$</returns>
		public static bool operator ==(T v, ValueArray<T> a) => a == v;

		/// <summary>
		/// Array and value compare with threshold
		/// </summary>
		/// <param name="a">array treated as a vector</param>
		/// <param name="v">value</param>
		/// <returns>$$\vec{a} - v \stackrel{?}{\ne} 0$$</returns>
		public static bool operator !=(ValueArray<T> a, T v) => !(a == v);

		/// <summary>
		/// Array and value compare with threshold
		/// </summary>
		/// <param name="a">array treated as a vector</param>
		/// <param name="v">value</param>
		/// <returns>$$\vec{a} - v \stackrel{?}{\ne} 0$$</returns>
		public static bool operator !=(T v, ValueArray<T> a) => !(a == v);
		#endregion

		#region abstract array operations
		/// <summary>
		/// Conjugate this array out-of-place.
		/// </summary>
		/// <returns>the conjugate array. If <typeparamref name="T"/> is a real type, this array is returned</returns>
		public virtual ValueArray<T> ConjugateOutOfPlace()
		{
			if (this.IsRealType)
				return this;
			return this.ApplyToClone(c => BLAS.PointWiseConjugate(c));
		}

		/// <summary>
		/// Conjugate this array in-place.
		/// </summary>
		public virtual void ConjugateInPlace()
		{
			if (!this.IsRealType)
				BLAS.PointWiseConjugate(this);
		}

		/// <summary>
		/// Truncate the array in-place.
		/// </summary>
		/// <param name="threshold">values lower than <c><paramref name="threshold"/> / this.Length</c> is chopped to 0.
		/// </param>
		public virtual void Truncate(float threshold = 1e-7f)
		{
			BLAS.Truncate(this, threshold / this.Length);
		}

		/// <summary>
		/// Calculate the point-wise absolute value of an array, then sum.
		/// </summary>
		/// <returns>$\sum_i{|\vec{v}_i|}$</returns>
		public virtual double AbsSum()
		{
			return BLAS.VectorAbsSum(this);
		}

		/// <summary>
		/// Calculate the sum of all values of this array
		/// </summary>
		/// <returns>the sum</returns>
		public virtual T Sum()
		{
			return BLAS.Sum(this);
		}

		/// <summary>
		/// Array's absolute values' maximum element's position.
		/// </summary>
		/// <returns>$\text{argmax_i{|\text{abs}{v_i}|}}$</returns>
		public virtual long ArgMaxAbs()
		{
			return BLAS.VectorAbsArgmax(this);
		}

		/// <summary>
		/// Calculate the maximum absolute value of this tensor
		/// </summary>
		/// <returns>the maximum absolute value</returns>
		public double AbsMax()
		{
			long ind = this.ArgMaxAbs();
			return Math.Abs(RT.CopyOut(this.Pointer, ind).ToDouble());
		}

		/// <summary>
		/// Array's absolute values' minimum element's position.
		/// </summary>
		/// <returns>$$\text{argmin}_i |\text{abs}{v_i}|$$</returns>
		public virtual long ArgMinAbs()
		{
			return BLAS.VectorAbsArgmin(this);
		}
		#endregion

		#region serialize
		/// <summary>
		/// The pointer name that <b>shall</b> be used in <see cref="GetPointers"/>.
		/// </summary>
		public const string PointerName = nameof(Pointer);

		/// <summary>
		/// Get the pointer in the class-defined order, the first one must be <see cref="Pointer"/> to the value data.
		/// </summary>
		/// <returns>the pointers</returns>
		public abstract IReadOnlyDictionary<string, IStorage> GetPointers();

		/// <summary>
		/// Get other requisite informations for re-constructing this array, in the class-defined order
		/// </summary>
		/// <returns>other requisite informations</returns>
		public abstract IReadOnlyDictionary<string, object> GetOtherInfo();
		#endregion

		#region host and device convert
		/// <summary>
		/// Convert this array to the other memory.
		/// </summary>
		/// <returns>a new <see cref="ValueArray{T}"/> with same value as this one</returns>
		public abstract ValueArray<T> ToTheOtherMemory();
		#endregion
	}
}
