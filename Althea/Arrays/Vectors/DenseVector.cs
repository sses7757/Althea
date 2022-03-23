using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The dense vector interface whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="ISingleValueStorageArray{T, TS, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Explicit)]
	public class DenseVector<T, TS> : IPitchedArray<T>,
		IVectorOperations<T, DenseVector<T, TS>, DenseVector<T, TS>>, IVectorUnaryOperators<T, DenseVector<T, TS>, DenseVector<T, TS>>, IVectorBinaryOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseVector<T, TS>>,
		IBaseVector<T, DenseVector<T, TS>>, ISingleValueStorageArray<T, TS, DenseVector<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		[FieldOffset(0)]
		private readonly long length;
		[FieldOffset(sizeof(long))]
		private readonly long stride;
		[FieldOffset(sizeof(long) * 2)]
		private readonly long outerSize;
		[FieldOffset(sizeof(long) * 3)]
		private readonly TS values;

		ReadOnlySpan<long> IValueArray<T, DenseVector<T, TS>>.Size => ReflectionHelper.CreateReadOnlySpan(in this.length, 1);
		ReadOnlySpan<long> IPitchedArray<T>.Size => ReflectionHelper.CreateReadOnlySpan(in this.length, 1);
		ReadOnlySpan<long> IPitchedArray<T>.Strides => ReflectionHelper.CreateReadOnlySpan(in this.stride, 1);
		ReadOnlySpan<long> IPitchedArray<T>.OuterSize => ReflectionHelper.CreateReadOnlySpan(in this.outerSize, 1);

		TS ISingleValueStorageArray<T, TS, DenseVector<T, TS>>.OriginalStorage => this.values;

		private DenseVector()
		{
			this.values = TS.Empty;
		}
		static DenseVector<T, TS> IValueArray<T, DenseVector<T, TS>>.Empty => new();

		/// <summary>
		/// Get the stride between consecutive elements of this vector in <typeparamref name="T"/>.
		/// </summary>
		public long Stride => this.stride;

		/// <inheritdoc/>
		public long Length => this.length;

		/// <inheritdoc/>
		public TS Storage => this.values.MakeReference();

		bool ICheckValid.IsValid() => this.values?.IsValid() ?? false;

		/// <summary>
		/// Create a new <see cref="DenseVector{T, TS}"/> with given <paramref name="storage"/> and <paramref name="stride"/>.
		/// </summary>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="stride">The stride between consecutive elements in <paramref name="storage"/>, default 1</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> ≤ 0 or ≥ <paramref name="storage"/>'s length</exception>
		public DenseVector(TS storage!!, long stride = 1)
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (stride <= 0 || stride >= storage.Length)
				throw new ArgumentOutOfRangeException(nameof(stride), Resources.ParameterError.InvalidValue);
			this.stride = stride;
			this.outerSize = storage.Length;
			this.length = this.outerSize / stride;
			this.values = storage.AddToManager();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.values.SafeDispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Deconstructor to be invoked by GC.
		/// </summary>
		~DenseVector()
		{
			this.Dispose();
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
				((IBaseVector<T, DenseVector<T, TS>>)this).CheckIndex(index);
				return (this.values + index * this.stride).ToManaged<T, TS>();
			}
			set
			{
				((IBaseVector<T, DenseVector<T, TS>>)this).CheckIndex(index);
				(this.values + index * this.stride).FromManaged(value);
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.values.GetEnumerator();

		/// <inheritdoc/>
		public DenseVector<T, TS> GetSlice(long start, long count)
		{
			((IBaseVector<T, DenseVector<T, TS>>)this).CheckRange(start, count);
			return new(this.values.MakeReference(start * this.stride, count), this.stride);
		}

		/// <inheritdoc/>
		public void GetSlice(long start, long count, DenseVector<T, TS> overwrite)
		{
			((IBaseVector<T, DenseVector<T, TS>>)this).CheckRange(start, count, overwrite);
			this.values.MakeReference(start * this.stride, count).StridedCopyTo<T, TS, TS>(this.stride, overwrite.values, overwrite.stride);
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
			((IBaseVector<T, DenseVector<T, TS>>)this).CheckRange(start, count, value);
			var src = value.values;
			var dst = this.values + (start * this.stride);
			src.StridedCopyTo<T, TS, TS>(this.stride, dst, this.stride);
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value) => ExtBlas.FillWithValue(this.values, value, this.stride);

		/// <inheritdoc/>
		public void AddScalar(T value) => ExtBlas.PointWiseAddScalar(this.values, this.stride, value);

		/// <inheritdoc/>
		public void Scale(T value) => Blas.Scale(this.values, this.stride, value);

		/// <inheritdoc/>
		public void Conjugate() => ExtBlas.PointWiseConjugate<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public void Power(T power) => ExtBlas.PointWisePower(this.values, this.stride, power);

		/// <inheritdoc/>
		public void Truncate(double threshold) => ExtBlas.PointWiseTruncate<T, TS>(this.values, this.stride, threshold);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => ExtBlas.AggregateSum<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public T AbsSum() => Blas.AbsoluteValueSum<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public T Norm() => Blas.Norm<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => (this.values + Blas.AbsoluteValueArgMax<T, TS>(this.values, this.stride)).ToManaged<T, TS>();

		/// <inheritdoc/>
		public T ValueWithMinAbs() => (this.values + Blas.AbsoluteValueArgMin<T, TS>(this.values, this.stride)).ToManaged<T, TS>();
		#endregion

		#region operations
		/// <inheritdoc/>
		public static T Dot(DenseVector<T, TS> left, DenseVector<T, TS> right, bool conjugateLeft = true) => Blas.Dot<T, TS, TS>(conjugateLeft, left.values, left.stride, right.values, right.stride);

		/// <inheritdoc/>
		public static void AddBy(DenseVector<T, TS> left, DenseVector<T, TS> right, T scalar) => Blas.Add(scalar, right.values, right.stride, left.values, left.stride);
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(DenseVector<T, TS> vector) => vector.ApplyToClone(static c => c.Scale(-T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, T scalar) => vector.ApplyToClone(c => c.Scale(scalar));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(T scalar, DenseVector<T, TS> vector) => vector * scalar;

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator /(DenseVector<T, TS> vector, T scalar) => vector * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator +(DenseVector<T, TS> left, DenseVector<T, TS> right) => left.ApplyToClone(c => AddBy(c, right, T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(DenseVector<T, TS> left, DenseVector<T, TS> right) => left.ApplyToClone(c => AddBy(c, right, -T.One));
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public DenseVector<T, TS> CreateAlike() => new(this.values.CreateAlike(), this.stride);

		/// <summary>
		/// Copy the values from this dense vector to the <paramref name="other"/> one without stride.
		/// </summary>
		/// <typeparam name="TS2">The concrete storage type of <paramref name="other"/></typeparam>
		/// <param name="other">The destination dense storage to copy to</param>
		/// <exception cref="ArgumentException">If <paramref name="other"/>'s length is less than this</exception>
		public void ToCompact<TS2>(TS2 other!!) where TS2 : class, IStorage<T, TS2>
		{
			if (other.Length < this.length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(other));
			this.values.StridedCopyTo<T, TS, TS2>(this.stride, other, 1);
		}
		#endregion

		#region serialization
		private record struct Repr(TS Values, long Stride);
		private static readonly JsonSerializerOptions JsonOptions = new()
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
		public DenseVector<T, TS> JsonDeserialize(string json!!)
		{
			var repr = JsonSerializer.Deserialize<Repr>(json, JsonOptions);
			return new(repr.Values, repr.Stride);
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<DenseVector<T, TS>>.StringMain => nameof(DenseVector<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<DenseVector<T, TS>>.PropertyNames => new[] { "DataType", "Values" };

		IEnumerable<object?> IMainPropertyFormattable<DenseVector<T, TS>>.PropertyValues => new object[] { Unmanaged<T>.DataType, this.values };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<DenseVector<T, TS>>.ToString(this);

		/// <inheritdoc/>
		public unsafe string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.values.Length, settings.Value.ArrayLength);
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			this.Storage.ToManaged(values);
			return values.ToVectorString(settings.Value.Precision, postfix: length == this.values.Length ? "" : string.Format(Resources.Print.MoreStored, this.values.Length - length));
		}
		#endregion
	}
}
