using System.Drawing;
using System;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Storage;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The structure for the format of any sparse array
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct SparseFormat : IEqualityOperators<SparseFormat, SparseFormat>, IBitwiseOperators<SparseFormat, SparseFormat, SparseFormat>
	{
		#region enumerates
		/// <summary>
		/// The type of a <see cref="SparseFormat"/>, other types like ELLPACK is not built in but can be supported manually.
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
		}

		/// <summary>
		/// The major of a <see cref="SparseFormat"/>, other majoring types can be supported manually.
		/// </summary>
		public enum Major : short
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

		/// <summary>
		/// Statically get a <see cref="SparseFormat"/> representing any format.
		/// </summary>
		public static SparseFormat Any => new((Type)255, (Blocking)255, (Major)255);

		/// <summary>
		/// Statically get a <see cref="SparseFormat"/> representing none of the format.
		/// </summary>
		public static SparseFormat None => default;

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
		public readonly bool IsAtomic => ((byte)this.type).IsPowerOfTwo() && ((byte)this.blocking).IsPowerOfTwo() && ((byte)this.major).IsPowerOfTwo();

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
				byte pt = t.PopCount(), pb = b.PopCount(), pm = m.PopCount();
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
			byte pt = t.PopCount(), pb = b.PopCount(), pm = m.PopCount();
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
						byte lt = t.Log2(), lb = b.Log2(), lm = m.Log2();
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
	/// The wrapper ref struct for any sparse array.
	/// </summary>
	/// <typeparam name="TVal">Any unmanaged number as the value data type</typeparam>
	/// <typeparam name="TSVal">The storage type used by the value array(s)</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
	/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
	public ref struct SparseArrayWrapper<TVal, TInd, TSVal, TSInd>
		where TVal : unmanaged, INumber<TVal> where TInd : unmanaged, IBinaryInteger<TInd>
		where TSVal : class, IStorage<TVal, TSVal> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		/// <summary>
		/// The default value of this sparse array
		/// </summary>
		public TVal DefaultValue { get; set; }

		/// <summary>
		/// The <see cref="SparseFormat"/> of this sparse array
		/// </summary>
		public SparseFormat Format { get; set; }
		
		/// <summary>
		/// The size of this sparse array
		/// </summary>
		public ReadOnlySpan<long> Size { get; set; }

		/// <summary>
		/// The value array(s) of this sparse array
		/// </summary>
		public ReadOnlySpan<TSVal> ValueStorages { get; set; }

		/// <summary>
		/// The index array(s) of this sparse array
		/// </summary>
		public ReadOnlySpan<TSInd> IndexStorages { get; set; }

		/// <summary>
		/// The constant block size of this sparse array, can be empty if it is not a <see cref="SparseFormat.Blocking.Simple"/>
		/// </summary>
		public ReadOnlySpan<TInd> BlockSize { get; set; }

		/// <summary>
		/// Other information related to this sparse array as a <see cref="object"/>
		/// </summary>
		public object? OtherInfo { get; set; }

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> with only meta information.
		/// </summary>
		public SparseArrayWrapper(TVal defaultValue, SparseFormat format)
		{
			this.DefaultValue = defaultValue;
			this.Format = format;
			this.Size = default;
			this.ValueStorages = default;
			this.IndexStorages = default;
			this.BlockSize = default;
			this.OtherInfo = default;
		}

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> with given detailed parameters.
		/// </summary>
		public SparseArrayWrapper(TVal defaultValue, SparseFormat format, ReadOnlySpan<long> size, ReadOnlySpan<TSVal> values, ReadOnlySpan<TSInd> indexes, ReadOnlySpan<TInd> blockSize = default, object? otherInfo = null)
		{
			this.DefaultValue = defaultValue;
			this.Format = format;
			this.Size = size;
			this.ValueStorages = values;
			this.IndexStorages = indexes;
			this.BlockSize = blockSize;
			this.OtherInfo = otherInfo;
		}

		/// <summary>
		/// Invoke <see cref="IDisposable.Dispose"/> for all storages of this wrapper.
		/// </summary>
		public void DisposeAll()
		{
			this.ValueStorages.ClearList();
			this.ValueStorages.ClearList();
		}
		#endregion
	}
}
