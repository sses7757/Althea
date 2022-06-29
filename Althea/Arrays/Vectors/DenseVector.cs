using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra.Dense;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The base dense vector class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public class DenseVector<T, TS> : IDenseArray<T, TS>,
		IBaseVector<T, DenseVector<T, TS>>,
		IVectorUnaryOperators<T, DenseVector<T, TS>, DenseVector<T, TS>>,
		IVectorBinaryOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseVector<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly long length;
		private readonly long stride;
		private readonly long outerSize;
		private readonly TS values;

		ReadOnlySpan<long> IValueArray<T, DenseVector<T, TS>>.Size => SpanHelper.CreateReadOnlySpan(in this.length, 1);
		ReadOnlySpan<long> IArray<T>.Size => SpanHelper.CreateReadOnlySpan(in this.length, 1);
		ReadOnlySpan<long> IPitchedArray<T>.Strides => SpanHelper.CreateReadOnlySpan(in this.stride, 1);
		ReadOnlySpan<long> IPitchedArray<T>.OuterSize => SpanHelper.CreateReadOnlySpan(in this.outerSize, 1);

		private DenseVector()
		{
			this.values = TS.Empty;
		}

		/// <summary>
		/// Reference copy the <paramref name="original"/> <see cref="DenseVector{T, TS}"/> that not managed by <see cref="ArrayStorageManager"/>.
		/// </summary>
		/// <param name="original">The original <see cref="DenseVector{T, TS}"/> to reference from</param>
		/// <remarks>ONLY use this when <paramref name="original"/> will be lost immediately.</remarks>
		protected DenseVector(DenseVector<T, TS> original)
		{
			this.length = original.length;
			this.stride = original.stride;
			this.outerSize = original.outerSize;
			this.values = original.values;
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> Empty => new();

		/// <summary>
		/// Get the stride between consecutive elements of this vector in <typeparamref name="T"/>.
		/// </summary>
		public long Stride => this.stride;

		/// <inheritdoc/>
		public long Length => this.length;

		/// <summary>
		/// Get the value storage of this vector as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference();

		bool ICheckValid.IsValid() => this.values?.IsValid() ?? false;

		/// <summary>
		/// Create a new <see cref="DenseVector{T, TS}"/> with given <paramref name="storage"/> and <paramref name="stride"/>.
		/// </summary>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="length">The presenting length of the vector to create</param>
		/// <param name="stride">The stride between consecutive elements in <paramref name="storage"/>, default 1</param>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0 or <paramref name="stride"/> ≤ 0 or ≥ <paramref name="storage"/>'s length</exception>
		public DenseVector(TS storage!!, long length, long stride = 1)
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), Resources.ParameterError.MustPositive);
			if (stride <= 0 || stride >= storage.Length)
				throw new ArgumentOutOfRangeException(nameof(stride), Resources.ParameterError.InvalidValue);
			this.stride = stride;
			this.length = length;
			this.outerSize = length * stride;
			long patchedLength = storage.Length + (stride - 1);
			if (patchedLength < this.outerSize)
				throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(stride));
			if (patchedLength > this.outerSize)
				storage = storage.MakeReference(0, this.outerSize - (stride - 1));
			this.values = storage.AddToManager();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.values.SafeDispose();
			GC.SuppressFinalize(this);
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(DenseVector<T, TS>? other) => other is not null && this.stride == other.stride && this.values == other.values;

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(DenseVector<T, TS> left, DenseVector<T, TS> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(DenseVector<T, TS> left, DenseVector<T, TS> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as DenseVector<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.values, this.stride);
		#endregion

		#region indexing
		/// <inheritdoc/>
		public T this[long index]
		{
			get
			{
				IBaseVector<T, DenseVector<T, TS>>.CheckIndex(this, index);
				return (this.values + index * this.stride).ToManaged<T, TS>();
			}
			set
			{
				IBaseVector<T, DenseVector<T, TS>>.CheckIndex(this, index);
				(this.values + index * this.stride).FromManaged(value);
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.values.GetEnumerator();

		/// <inheritdoc/>
		public DenseVector<T, TS> GetSlice(long start, long count)
		{
			IBaseVector<T, DenseVector<T, TS>>.CheckRange(this, start, count);
			return new(this.values + start * this.stride, count, this.stride);
		}

		/// <inheritdoc/>
		public void GetSlice(long start, long count, DenseVector<T, TS> overwrite)
		{
			IBaseVector<T, DenseVector<T, TS>>.CheckRange(this, start, count, overwrite);
			this.values.MakeReference(start * this.stride, (count - 1) * this.stride + 1).StridedCopyTo<T, TS, TS>(this.stride, overwrite.values, overwrite.stride);
		}

		/// <inheritdoc/>
		public void CopyTo(DenseVector<T, TS> destination)
		{
			if (destination.length != this.length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.values.StridedCopyTo<T, TS, TS>(this.stride, destination.values, destination.stride);
		}

		/// <inheritdoc/>
		public void SetSlice(long start, long count, DenseVector<T, TS> value)
		{
			IBaseVector<T, DenseVector<T, TS>>.CheckRange(this, start, count, value);
			var src = value.values;
			var dst = this.values + (start * this.stride);
			src.StridedCopyTo<T, TS, TS>(this.stride, dst, this.stride);
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value) => ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Fill, value, this.values, this.stride, this.values, this.stride);

		/// <inheritdoc/>
		public void AddScalar(T value) => ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, value, this.values, this.stride, this.values, this.stride);

		/// <inheritdoc/>
		public void Scale(T value) => Blas.Scale(this.values, this.stride, value);

		/// <inheritdoc/>
		public void Conjugate() => ExtBlas.GeneralVectorUnary<T, TS, TS>(LinearAlgebra.UnaryOperation.Conjugate, this.values, this.stride, this.values, this.stride);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => ExtBlas.GeneralVectorReduce<T, TS>(LinearAlgebra.ReduceOperation.Add, this.values, this.stride);

		/// <inheritdoc/>
		public T AbsSum() => Blas.AbsoluteValueSum<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public T Norm() => Blas.Norm<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => (this.values + Blas.AbsoluteValueArgMax<T, TS>(this.values, this.stride)).ToManaged<T, TS>();

		/// <inheritdoc/>
		public T ValueWithMinAbs() => (this.values + Blas.AbsoluteValueArgMin<T, TS>(this.values, this.stride)).ToManaged<T, TS>();
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, T scalar) => vector.ApplyToClone(c => c.Scale(scalar));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(DenseVector<T, TS> vector) => vector * (-T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(T scalar, DenseVector<T, TS> vector) => vector * scalar;

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator /(DenseVector<T, TS> vector, T scalar) => vector * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator +(DenseVector<T, TS> left, DenseVector<T, TS> right) => left.ApplyToClone(c => DenseOperation<T, TS>.AddBy(c, right, T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(DenseVector<T, TS> left, DenseVector<T, TS> right) => left.ApplyToClone(c => DenseOperation<T, TS>.AddBy(c, right, -T.One));
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public DenseVector<T, TS> CreateAlike() => new(this.values.ResizeAlike(this.length), this.length);

		/// <summary>
		/// Copy the values from this dense vector to a new <typeparamref name="TS"/> without stride.
		/// </summary>
		/// <returns>The created compact vector's storage as a <typeparamref name="TS"/></returns>
		public TS ToCompact()
		{
			var compact = this.values.ResizeAlike(this.length);
			try
			{
				this.values.StridedCopyTo<T, TS, TS>(this.stride, compact, 1);
				return compact;
			}
			catch (Exception)
			{
				compact.Dispose();
				throw;
			}
		}
		#endregion

		#region serialization
		private record struct Repr(TS Values, long Stride);
		private static JsonSerializerOptions JsonOptions => new()
		{
			Converters = { TS.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public string JsonSerialize()
		{
			return JsonSerializer.Serialize<Repr>(new(this.values, this.stride), JsonOptions);
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> JsonDeserialize(string json!!)
		{
			var repr = JsonSerializer.Deserialize<Repr>(json, JsonOptions);
			return new(repr.Values, repr.Values.Length + repr.Stride - 1, repr.Stride);
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<DenseVector<T, TS>>.StringMain => nameof(DenseVector<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<DenseVector<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Length", "Stride" };

		IEnumerable<object?> IMainPropertyFormattable<DenseVector<T, TS>>.PropertyValues => new object[] { T.Type, this.length, this.values, this.stride };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<DenseVector<T, TS>>.ToString(this);

		/// <inheritdoc/>
		public string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.values.Length, settings.Value.ArrayLength);
			using var temp = length.CheckStackLimit<T>();
			Span<T> values = temp.IsEmpty ? stackalloc T[length] : temp.Data;
			this.values.ToManaged(values);
			return values.ToVectorString(settings.Value.Precision) + (length == this.values.Length ? "" : string.Format(Resources.Print.MoreStored, this.values.Length - length));
		}
		#endregion
	}
}
