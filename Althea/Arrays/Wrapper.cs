using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Storage;


namespace Althea.Array
{
	/// <summary>
	/// The wrapper structure for any dense array.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public readonly ref struct DenseArrayWrapper<T, TS> where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly TS m_values;

		private readonly ReadOnlySpan<long> m_size, m_outerSize, m_strides;

		/// <summary>
		/// Get the value array of this tensor as a <typeparamref name="TS"/>
		/// </summary>
		public readonly TS ValueStorage
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_values;
		}

		/// <summary>
		/// Get the presenting size of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		public readonly ReadOnlySpan<long> Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_size;
		}

		/// <summary>
		/// Get the rank (number of dimensions) of this tensor as a <see cref="int"/>
		/// </summary>
		public readonly int Rank
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_size.Length;
		}

		/// <summary>
		/// Get the outer size (actual size of all dimensions) of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		/// <remarks>If there is not pitch, <see cref="OuterSize"/> == <see cref="Size"/> (reference equals)</remarks>
		public readonly ReadOnlySpan<long> OuterSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_outerSize;
		}

		/// <summary>
		/// Get the strides between consecutive elements in each dimension of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		/// <remarks>If there is not pitch, <see cref="Strides"/> is empty</remarks>
		public readonly ReadOnlySpan<long> Strides
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_outerSize;
		}
		#endregion

		#region equality
		/// <summary>
		/// Check whether this <see cref="DenseArrayWrapper{T, TS}"/> is identical to the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="DenseArrayWrapper{T, TS}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool Equals(DenseArrayWrapper<T, TS> other)
		{
			return this.m_values == other.m_values && this.m_size.SequenceEqual(other.m_size) && this.m_outerSize.SequenceEqual(other.m_outerSize);
		}

		/// <summary>
		/// Check whether this <see cref="DenseArrayWrapper{T, TS}"/> has identical size (and outer size) as the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="DenseArrayWrapper{T, TS}"/> to compare</param>
		/// <returns>this == <paramref name="other"/> for sizes</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool SizeEquals(DenseArrayWrapper<T, TS> other)
		{
			return this.m_size.SequenceEqual(other.m_size) && this.m_outerSize.SequenceEqual(other.m_outerSize);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(DenseArrayWrapper<T, TS> left, DenseArrayWrapper<T, TS> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(DenseArrayWrapper<T, TS> left, DenseArrayWrapper<T, TS> right)
		{
			return !left.Equals(right);
		}

		/// <summary>
		/// Always returns false since a ref struct cannot be boxed
		/// </summary>
		public override bool Equals(object? obj) => false;

		/// <summary>
		/// Always throws <see cref="InvalidOperationException"/> since a ref struct cannot be stored on heap
		/// </summary>
		public override int GetHashCode() => throw new InvalidOperationException();

		/// <summary>
		/// Get the string representation of this <see cref="DenseArrayWrapper{T, TS}"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="DenseArrayWrapper{T, TS}"/></returns>
		public override string ToString()
		{
			return nameof(DenseArrayWrapper<T, TS>) + $"[ValueStorage={this.m_values}, Size={this.m_size.SpanJoin('x')}" + (this.m_size == this.m_outerSize ? "]" : $"OuterSize={this.m_outerSize.SpanJoin('x')}]");
		}
		#endregion

		#region create
		/// <summary>
		/// Create a new <see cref="DenseArrayWrapper{T, TS}"/> with given parameters and assuming that there is not pitch.
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size / extent of all dimensions</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseArrayWrapper(TS value, ReadOnlySpan<long> size)
		{
			this.m_values = value;
			this.m_size = size;
			this.m_outerSize = size;
			this.m_strides = default;
		}

		/// <summary>
		/// Create a new <see cref="DenseArrayWrapper{T, TS}"/> with all given parameters.
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size / extent of all dimensions</param>
		/// <param name="outerSize">The actual outer size, will be replaced by <paramref name="size"/> if <paramref name="size"/> sequence equals <paramref name="outerSize"/></param>
		/// <param name="strides">The strides between consecutive elements in each dimension, will be replaced by empty if <paramref name="size"/> sequence equals <paramref name="outerSize"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseArrayWrapper(TS value, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> strides)
		{
			this.m_values = value; this.m_size = size;
			if (outerSize.SequenceEqual(size))
			{
				outerSize = size;
				strides = default;
			}
			this.m_outerSize = outerSize;
			this.m_strides = strides;
		}

		/// <summary>
		/// Create a new <see cref="DenseArrayWrapper{T, TS}"/> with a given dense <paramref name="array"/>.
		/// </summary>
		/// <param name="array">The given dense array as a <see cref="IDenseArray{T, TS}"/></param>
		/// <exception cref="ArgumentException">If <paramref name="array"/> is a <see cref="ISparseArray{T}"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseArrayWrapper(IDenseArray<T, TS>? array)
		{
			if (array is null)
			{
				this = default; this.m_values = TS.Empty;
				return;
			}
			if (array is ISparseArray<T>)
				throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(array));

			ReadOnlySpan<long> outerSize, strides;
			if (array.OuterSize.SequenceEqual(array.Size))
			{
				outerSize = array.Size; strides = default;
			}
			else
			{
				outerSize = array.OuterSize; strides = array.Strides;
			}
			this = new(array.Storage, array.Size, outerSize, strides);
		}
		#endregion
	}


	/// <summary>
	/// The structure for the format of any sparse array with size the same as an <see cref="int"/>.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct SparseFormat : System.Numerics.IEqualityOperators<SparseFormat, SparseFormat>, System.Numerics.IBitwiseOperators<SparseFormat, SparseFormat, SparseFormat>
	{
		#region enumerates
		/// <summary>
		/// The class type of a <see cref="SparseFormat"/>, other types like ELLPACK is not built in but can be supported manually.
		/// </summary>
		[Flags]
		public enum Type : byte
		{
			/// <summary>
			/// The coordinated type
			/// </summary>
			Coordinated = 1 << 0,
			/// <summary>
			/// The compressed type
			/// </summary>
			Compressed = 1 << 1,
			/// <summary>
			/// Any class type
			/// </summary>
			Any = 255
		}

		/// <summary>
		/// The blocking of a <see cref="SparseFormat"/>, other blocking types can be supported manually.
		/// </summary>
		[Flags]
		public enum Blocking : byte
		{
			/// <summary>
			/// No blocking -- indices are for custom things
			/// </summary>
			None = 0,
			/// <summary>
			/// Element blocking -- indices are for individual elements
			/// </summary>
			Element = 1 << 0,
			/// <summary>
			/// Standard blocking -- indices are for contiguous blocks of same size that divides the overall array into <c>size / blockSize</c> blocks
			/// </summary>
			Simple = 1 << 1,
			/// <summary>
			/// Complicated blocking -- indices are for contiguous blocks of possibly different sizes that divides the overall array into <c>blockSize.Length</c> blocks
			/// </summary>
			Complicated = 1 << 2,
			/// <summary>
			/// Any blocking type
			/// </summary>
			Any = 255
		}

		/// <summary>
		/// The major of a <see cref="SparseFormat"/>, other majoring types can be supported manually.
		/// </summary>
		public enum Major : byte
		{
			/// <summary>
			/// No major -- the <see cref="Major"/> is not applicable
			/// </summary>
			None = 0,
			/// <summary>
			/// The column major
			/// </summary>
			Column = 1 << 0,
			/// <summary>
			/// The row major
			/// </summary>
			Row = 1 << 1,
			/// <summary>
			/// Any major type
			/// </summary>
			Any = 255
		}
		#endregion

		#region basic
		[FieldOffset(0)]
		private readonly int data = 0;

		[FieldOffset(0)]
		private readonly Type type;
		[FieldOffset(1)]
		private readonly Blocking blocking;
		[FieldOffset(2)]
		private readonly Major major;

		/// <summary>
		/// Get the <see cref="Type"/> of this <see cref="SparseFormat"/>.
		/// </summary>
		public Type Class => this.type;

		/// <summary>
		/// Get the <see cref="Blocking"/> of this <see cref="SparseFormat"/>.
		/// </summary>
		public Blocking BlockType => this.blocking;

		/// <summary>
		/// Get the <see cref="Major"/> of this <see cref="SparseFormat"/>.
		/// </summary>
		public Major MajorType => this.major;

		internal SparseFormat(int data)
		{
			this = default;
			this.data = data;
		}

		internal int Data => this.data;

		/// <summary>
		/// The full constructor of a <see cref="SparseFormat"/>.
		/// </summary>
		public SparseFormat(Type type, Blocking blocking = Blocking.None, Major major = Major.None)
		{
			this.type = type;
			this.blocking = blocking;
			this.major = major;
		}

		/// <summary>
		/// Checks whether this <see cref="SparseFormat"/> is the same as the <paramref name="other"/> one.
		/// </summary>
		/// <param name="other">The other <see cref="SparseFormat"/> to compare</param>
		/// <returns><c>this == <paramref name="other"/></c></returns>
		public bool Equals(SparseFormat other) => this.data == other.data;

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SparseFormat left, SparseFormat right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SparseFormat left, SparseFormat right) => !left.Equals(right);

		/// <summary>
		/// Checks whether this <see cref="SparseFormat"/> is the same as the other <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns><c>this == <paramref name="obj"/></c></returns>
		public override bool Equals(object? obj) => obj is SparseFormat s && this.Equals(s);

		/// <summary>
		/// Get the hash code of this <see cref="SparseFormat"/>.
		/// </summary>
		/// <returns>A <see cref="int"/> that is the hash code for this <see cref="SparseFormat"/>.</returns>
		public override int GetHashCode() => this.data;

		/// <summary>
		/// Explicitly convert this <see cref="SparseFormat"/> to its raw data as a <see cref="int"/>.
		/// </summary>
		/// <param name="s">The <see cref="SparseFormat"/> to convert</param>
		public static explicit operator int(SparseFormat s) => s.data;

		/// <summary>
		/// Get the common <see cref="SparseFormat"/> of the <paramref name="left"/> and <paramref name="right"/> <see cref="SparseFormat"/>s.
		/// </summary>
		public static SparseFormat operator &(SparseFormat left, SparseFormat right) => new(left.data & right.data);

		/// <summary>
		/// Get the union <see cref="SparseFormat"/> of the <paramref name="left"/> and <paramref name="right"/> <see cref="SparseFormat"/>s.
		/// </summary>
		public static SparseFormat operator |(SparseFormat left, SparseFormat right) => new(left.data | right.data);

		/// <summary>
		/// Get the XOR <see cref="SparseFormat"/> of the <paramref name="left"/> and <paramref name="right"/> <see cref="SparseFormat"/>s.
		/// </summary>
		public static SparseFormat operator ^(SparseFormat left, SparseFormat right) => new(left.data ^ right.data);

		/// <summary>
		/// Get the complementary <see cref="SparseFormat"/> of the <paramref name="value"/> <see cref="SparseFormat"/>.
		/// </summary>
		public static SparseFormat operator ~(SparseFormat value) => new(~value.type, ~value.blocking, ~value.major);
		#endregion

		#region constants
		/// <summary>
		/// Any format
		/// </summary>
		public static readonly SparseFormat Any = new(Type.Any, Blocking.Any, Major.Any);

		/// <summary>
		/// None of the formats
		/// </summary>
		public static readonly SparseFormat None = default;

		/// <summary>
		/// The element-blocked coordinated format for vectors
		/// </summary>
		public static readonly SparseFormat VectorCooFormat = new(Type.Coordinated, Blocking.Element);
		/// <summary>
		/// The simple-blocked coordinated format for vectors
		/// </summary>
		public static readonly SparseFormat VectorBooFormat = new(Type.Coordinated, Blocking.Simple);
		/// <summary>
		/// The element-blocked column-major coordinated format for matrices
		/// </summary>
		public static readonly SparseFormat MatrixCocFormat = new(Type.Coordinated, Blocking.Element, Major.Column);
		/// <summary>
		/// The element-blocked row-major coordinated format for matrices
		/// </summary>
		public static readonly SparseFormat MatrixCorFormat = new(Type.Coordinated, Blocking.Element, Major.Row);
		/// <summary>
		/// The element-blocked column-major compressed format for matrices
		/// </summary>
		public static readonly SparseFormat MatrixCscFormat = new(Type.Compressed, Blocking.Element, Major.Column);
		/// <summary>
		/// The element-blocked row-major compressed format for matrices
		/// </summary>
		public static readonly SparseFormat MatrixCsrFormat = new(Type.Compressed, Blocking.Element, Major.Row);
		/// <summary>
		/// The simple-blocked column-major compressed format for matrices
		/// </summary>
		public static readonly SparseFormat MatrixBscFormat = new(Type.Compressed, Blocking.Simple, Major.Column);
		/// <summary>
		/// The simple-blocked row-major compressed format for matrices
		/// </summary>
		public static readonly SparseFormat MatrixBsrFormat = new(Type.Compressed, Blocking.Simple, Major.Row);
		/// <summary>
		/// The element-blocked column-major coordinated format for tensors
		/// </summary>
		public static readonly SparseFormat TensorCocFormat = new(Type.Coordinated, Blocking.Element, Major.Column);
		/// <summary>
		/// The element-blocked row-major coordinated format for tensors
		/// </summary>
		public static readonly SparseFormat TensorCorFormat = new(Type.Coordinated, Blocking.Element, Major.Row);
		/// <summary>
		/// The simple-blocked column-major coordinated format for tensors
		/// </summary>
		public static readonly SparseFormat TensorBocFormat = new(Type.Coordinated, Blocking.Simple, Major.Column);
		/// <summary>
		/// The simple-blocked row-major coordinated format for tensors
		/// </summary>
		public static readonly SparseFormat TensorBorFormat = new(Type.Coordinated, Blocking.Simple, Major.Row);
		#endregion

		#region methods
		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Blocking"/> is <see cref="Blocking.None"/>.
		/// </summary>
		public readonly SparseFormat WithoutBlocking => new(this.type, Blocking.None, this.major);

		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Blocking"/> is <see cref="Blocking.Element"/>.
		/// </summary>
		public readonly SparseFormat WithElementBlocking => new(this.type, Blocking.Element, this.major);

		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Blocking"/> is <see cref="Blocking.Simple"/>.
		/// </summary>
		public readonly SparseFormat WithSimpleBlocking => new(this.type, Blocking.Simple, this.major);

		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Blocking"/> is <see cref="Blocking.Complicated"/>.
		/// </summary>
		public readonly SparseFormat WithComplicatedBlocking => new(this.type, Blocking.Complicated, this.major);

		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Major"/> is <see cref="Major.None"/>.
		/// </summary>
		public readonly SparseFormat WithoutMajor => new(this.type, this.blocking, Major.None);

		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Major"/> is <see cref="Major.Column"/>.
		/// </summary>
		public readonly SparseFormat WithColumnMajor => new(this.type, this.blocking, Major.Column);

		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Major"/> is <see cref="Major.Row"/>.
		/// </summary>
		public readonly SparseFormat WithRowMajor => new(this.type, this.blocking, Major.Row);

		/// <summary>
		/// Get whether this <see cref="SparseFormat"/> is an atomic format, i.e. a specific format, or not.
		/// </summary>
		public readonly bool IsAtomic => byte.IsPow2((byte)this.type) && byte.IsPow2((byte)this.blocking) && byte.IsPow2((byte)this.major);

		/// <summary>
		/// Get whether this <see cref="SparseFormat"/> is of <see cref="Type.Compressed"/> or not.
		/// </summary>
		public readonly bool IsCompressed => this.type == Type.Compressed;

		/// <summary>
		/// Get whether this <see cref="SparseFormat"/> isn't of <see cref="Major.None"/> or not.
		/// </summary>
		public readonly bool HasMajor => this.major != Major.None;

		/// <summary>
		/// Get whether this <see cref="SparseFormat"/> is of <see cref="Major.Row"/> or not.
		/// </summary>
		public readonly bool IsRowMajor => this.major == Major.Row;

		/// <summary>
		/// Get whether this <see cref="SparseFormat"/> isn't of <see cref="Blocking.None"/> or not.
		/// </summary>
		public readonly bool HasBlocking => this.blocking != Blocking.None;

		/// <summary>
		/// Get the number of atomic combinations corresponding this <see cref="SparseFormat"/>.
		/// </summary>
		public int NCombinations
		{
			get
			{
				byte t = (byte)this.type, b = (byte)this.blocking, m = (byte)this.major;
				byte pt = byte.PopCount(t), pb = byte.PopCount(b), pm = byte.PopCount(m);
				return pt * pb * pm;
			}
		}

		/// <summary>
		/// Decompose this <see cref="SparseFormat"/> into atomic <paramref name="result"/>s.
		/// </summary>
		/// <param name="result">The <see cref="Span{T}"/> to put the results</param>
		/// <returns>The <paramref name="result"/> filled with atomic formats</returns>
		/// <exception cref="ArgumentException">If the length of <paramref name="result"/> is less than the number of atomic formats</exception>
		public Span<SparseFormat> Decompose(Span<SparseFormat> result)
		{
			if (this.data == 0)
				return Span<SparseFormat>.Empty;
			byte t = (byte)this.type, b = (byte)this.blocking, m = (byte)this.major;
			byte pt = byte.PopCount(t), pb = byte.PopCount(b), pm = byte.PopCount(m);
			int count = pt * pb * pm;
			if (result.Length < count)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(result));
			result = result[..count];
			count = 0;
			for (byte it = 0; it < pt; it++)
			{
				for (byte ib = 0; ib < pb; ib++)
				{
					for (byte im = 0; im < pm; im++)
					{
						byte lt = byte.Log2(t), lb = byte.Log2(b), lm = byte.Log2(m);
						result[count++] = new((Type)(1 << lt), (Blocking)(1 << lb), (Major)(1 << lm));
						t = t.ResetBit(lt); b = b.ResetBit(lb); m = m.ResetBit(lm);
					}
				}
			}
			return result[..count];
		}
		#endregion

		#region string
		/// <summary>
		/// Get the full string representation of this <see cref="SparseFormat"/>.
		/// </summary>
		public override string ToString()
		{
			if (this == Any)
				return nameof(Any);
			if (this == None)
				return nameof(None);
			if (this.IsAtomic)
				return $"{this.type.GetName()}_{this.blocking.GetName()}{nameof(Blocking)}_{this.major.GetName()}{nameof(Major)}";
			byte t = (byte)this.type, b = (byte)this.blocking, m = (byte)this.major;
			string st, sb, sm;
			st = t == 255 ? "Any" : $"{{{this.type.GetName()}}}";
			sb = b == 255 ? "Any" : $"{{{this.blocking.GetName()}}}";
			sm = m == 255 ? "Any" : $"{{{this.major.GetName()}}}";
			return $"[{nameof(Type)}={st}; {nameof(Blocking)}={sb}; {nameof(Major)}={sm}]";
		}
		#endregion
	}


	/// <summary>
	/// The wrapper structure for any sparse array.
	/// </summary>
	/// <typeparam name="TVal">Any unmanaged number as the value data type</typeparam>
	/// <typeparam name="TSVal">The storage type used by the value array(s)</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
	/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
	/// <remarks>The wrapper is quite large and therefore shall be passed by reference if possible.</remarks>
	[StructLayout(LayoutKind.Explicit)]
	public struct SparseArrayWrapper<TVal, TInd, TSVal, TSInd>
		where TVal : unmanaged, IBaseNumber<TVal> where TInd : unmanaged, IBinaryInt<TInd>
		where TSVal : class, IStorage<TVal, TSVal> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		static SparseArrayWrapper()
		{
			if (TInd.Size > sizeof(long))
				throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		[FieldOffset(0)]
		private FixedBuffer_128<long> size;
		[FieldOffset(128 - sizeof(long))]
		private int rank;
		[FieldOffset(128)]
		private FixedClassBuffer_8<TSVal> valueStorage;
		[FieldOffset(128 + 64)]
		private FixedClassBuffer_8<TSInd> indexStorage;
		[FieldOffset(128 * 2)]
		private FixedBuffer_128<TInd> blockSize;
		[FieldOffset(128 * 3 - sizeof(long))]
		private int vals;
		[FieldOffset(128 * 3 - sizeof(int))]
		private int inds;
		[FieldOffset(128 * 3)]
		private SparseFormat format;
		[FieldOffset(128 * 3 + sizeof(int))]
		private TVal defaultVal;

		private const byte MAX_RANK = 15, MAX_STORAGES = 8;

		/// <summary>
		/// The default value of this sparse array
		/// </summary>
		public TVal DefaultValue
		{
			get => this.defaultVal;
			set => this.defaultVal = value;
		}

		/// <summary>
		/// The <see cref="SparseFormat"/> of this sparse array
		/// </summary>
		public SparseFormat Format
		{
			get => this.format;
			set => this.format = value;
		}

		/// <summary>
		/// The size of this sparse array
		/// </summary>
		public ReadOnlySpan<long> Size => this.size.AsSpan(this.rank);
		#endregion

		#region create and set
		/// <summary>
		/// Set the <see cref="ValueStorages"/>, <see cref="IndexStorages"/> of this <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> for 1-value 2-index storages.
		/// </summary>
		/// <param name="values">The <see cref="ValueStorages"/> to set</param>
		/// <param name="indices1">The first <see cref="IndexStorages"/> to set</param>
		/// <param name="indices2">The second <see cref="IndexStorages"/> to set</param>
		/// <exception cref="ArgumentNullException">if any of the input storages is null</exception>
		/// <exception cref="NotSupportedException">If the lengths exceeds the internal limits</exception>
		public void SetValues(TSVal values!!, TSInd indices1!!, TSInd indices2!!)
		{
			this.vals = 1;
			this.valueStorage[0] = values;
			this.inds = 2;
			this.indexStorage[0] = indices1;
			this.indexStorage[1] = indices2;
		}

		/// <summary>
		/// Set the <see cref="Size"/>, <see cref="ValueStorages"/>, <see cref="IndexStorages"/> of this <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> for a matrix.
		/// </summary>
		/// <param name="rows">The first value of <see cref="Size"/></param>
		/// <param name="cols">The second value of <see cref="Size"/></param>
		/// <param name="values">The <see cref="ValueStorages"/> to set</param>
		/// <param name="indices1">The first <see cref="IndexStorages"/> to set</param>
		/// <param name="indices2">The second <see cref="IndexStorages"/> to set</param>
		/// <exception cref="ArgumentNullException">if any of the input storages is null</exception>
		/// <exception cref="NotSupportedException">If the lengths exceeds the internal limits</exception>
		public void SetValues(long rows, long cols, TSVal values!!, TSInd indices1!!, TSInd indices2!!)
		{
			this.rank = 2;
			this.size[0] = rows; this.size[1] = cols;
			this.SetValues(values, indices1, indices2);
		}

		/// <summary>
		/// Set the <see cref="Size"/>, <see cref="BlockSize"/>, <see cref="ValueStorages"/>, <see cref="IndexStorages"/> of this <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> for a matrix.
		/// </summary>
		/// <param name="rows">The first value of <see cref="Size"/></param>
		/// <param name="cols">The second value of <see cref="Size"/></param>
		/// <param name="blockRows">The first value of <see cref="BlockSize"/></param>
		/// <param name="blockCols">The second value of <see cref="BlockSize"/></param>
		/// <param name="values">The <see cref="ValueStorages"/> to set</param>
		/// <param name="indices1">The first <see cref="IndexStorages"/> to set</param>
		/// <param name="indices2">The second <see cref="IndexStorages"/> to set</param>
		/// <exception cref="ArgumentNullException">if any of the input storages is null</exception>
		/// <exception cref="NotSupportedException">If the lengths exceeds the internal limits</exception>
		public void SetValues(long rows, long cols, TInd blockRows, TInd blockCols, TSVal values!!, TSInd indices1!!, TSInd indices2!!)
		{
			this.blockSize[0] = blockRows; this.blockSize[1] = blockCols;
			this.SetValues(rows, cols, values, indices1, indices2);
		}

		/// <summary>
		/// Set the <see cref="Size"/>, <see cref="ValueStorages"/>, <see cref="IndexStorages"/> (and <see cref="BlockSize"/>) of this <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> while inputs are all of length 1.
		/// </summary>
		/// <param name="size">The <see cref="Size"/> to set</param>
		/// <param name="values">The <see cref="ValueStorages"/> to set</param>
		/// <param name="indices">The <see cref="IndexStorages"/> to set</param>
		/// <param name="blockSize">The <see cref="BlockSize"/> to set, default empty means no block or element block</param>
		/// <exception cref="ArgumentNullException">if any of the input storages is null</exception>
		/// <exception cref="NotSupportedException">If the lengths exceeds the internal limits</exception>
		/// <exception cref="ArgumentException">If <paramref name="blockSize"/> is not empty while its length is not the same as <paramref name="size"/></exception>
		public void SetValues(long size, TSVal values!!, TSInd indices!!, TInd blockSize = default)
		{
			this.rank = 1;
			this.size[0] = size;
			this.blockSize[0] = blockSize == default ? TInd.One : blockSize;
			this.vals = 1;
			this.valueStorage[0] = values;
			this.inds = 1;
			this.indexStorage[0] = indices;
		}

		/// <summary>
		/// Set the <see cref="Size"/>, <see cref="ValueStorages"/>, <see cref="IndexStorages"/> (and <see cref="BlockSize"/>) of this <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/>.
		/// </summary>
		/// <param name="size">The <see cref="Size"/> to set</param>
		/// <param name="values">The <see cref="ValueStorages"/> to set</param>
		/// <param name="indexes">The <see cref="IndexStorages"/> to set</param>
		/// <param name="blockSize">The <see cref="BlockSize"/> to set, default empty means no block or element block</param>
		/// <exception cref="ArgumentNullException">if any of the inputs is empty</exception>
		/// <exception cref="NotSupportedException">If the lengths exceeds the internal limits</exception>
		/// <exception cref="ArgumentException">If <paramref name="blockSize"/> is not empty while its length is not the same as <paramref name="size"/></exception>
		public void SetValues(ReadOnlySpan<long> size, ReadOnlySpan<TSVal> values, ReadOnlySpan<TSInd> indexes, ReadOnlySpan<TInd> blockSize = default)
		{
			if (size.IsEmpty)
				throw new ArgumentNullException(nameof(size));
			if (values.IsEmpty)
				throw new ArgumentNullException(nameof(values));
			if (indexes.IsEmpty)
				throw new ArgumentNullException(nameof(indexes));
			if (size.Length > MAX_RANK || values.Length > MAX_STORAGES || indexes.Length > MAX_STORAGES)
				throw new NotSupportedException(Resources.ParameterError.WrongSize);
			if (!blockSize.IsEmpty && blockSize.Length != size.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(blockSize));
			this.rank = size.Length;
			this.size.CopyFromSpan(size);
			if (!blockSize.IsEmpty)
				this.blockSize.CopyFromSpan(blockSize);
			this.vals = values.Length;
			values.CopyTo(this.valueStorage.AsSpan(this.vals));
			this.inds = indexes.Length;
			indexes.CopyTo(this.indexStorage.AsSpan(this.inds));
		}

		/// <summary>
		/// The value array(s) of this sparse array
		/// </summary>
		public ReadOnlySpan<TSVal> ValueStorages => this.valueStorage.AsSpan(this.vals);

		/// <summary>
		/// The index array(s) of this sparse array
		/// </summary>
		public ReadOnlySpan<TSInd> IndexStorages => this.indexStorage.AsSpan(this.inds);

		/// <summary>
		/// The constant block size of this sparse array, can be empty if it is not a <see cref="SparseFormat.Blocking.Simple"/>
		/// </summary>
		public ReadOnlySpan<TInd> BlockSize => this.blockSize.AsSpan(this.rank);

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> with only meta information.
		/// </summary>
		public SparseArrayWrapper(TVal defaultValue, SparseFormat format, ReadOnlySpan<long> size = default)
		{
			this = default;
			this.defaultVal = defaultValue;
			this.format = format;
			this.rank = size.Length;
			this.size.CopyFromSpan(size);
		}

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> with given <paramref name="array"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseArrayWrapper<TVal, TInd, TSVal, TSInd> Create(ISparseArray<TVal, TInd, TSVal, TSInd> array!!)
		{
			SparseArrayWrapper<TVal, TInd, TSVal, TSInd> wrapper = default;
			wrapper.DefaultValue = array.DefaultValue;
			wrapper.Format = array.Format;
			wrapper.SetValues(array.Size, array.ValueStorages, array.IndexStorages, array.BlockSize);
			return wrapper;
		}

		/// <summary>
		/// Invoke <see cref="IDisposable.Dispose"/> for all storages of this wrapper.
		/// </summary>
		public void DisposeAll()
		{
			this.ValueStorages.ClearList();
			this.IndexStorages.ClearList();
		}
		#endregion
	}
}
