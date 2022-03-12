using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The structure for the format of any sparse array
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct SparseFormat : IEqualityOperators<SparseFormat, SparseFormat>
	{
		#region enumerates
		/// <summary>
		/// The type of a <see cref="SparseFormat"/>
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
		/// The blocking of a <see cref="SparseFormat"/>
		/// </summary>
		[Flags]
		public enum Blocking : byte
		{
			/// <summary>
			/// No blocking -- indices are for individual elements
			/// </summary>
			NonBlock = 1 << 0,
			/// <summary>
			/// Standard blocking -- indices are for contiguous blocks
			/// </summary>
			SimpleBlock = 1 << 1,
		}

		/// <summary>
		/// The major of a <see cref="SparseFormat"/>
		/// </summary>
		public enum Major : short
		{
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
		/// Statically get a <see cref="SparseFormat"/> representing any format
		/// </summary>
		public static SparseFormat Any => new((Type)255, (Blocking)255, (Major)255);

		/// <summary>
		/// Statically get a <see cref="SparseFormat"/> representing none of the format
		/// </summary>
		public static SparseFormat None => default;

		/// <summary>
		/// The full constructor of a <see cref="SparseFormat"/>.
		/// </summary>
		public SparseFormat(Type type, Blocking blocking, Major major)
		{
			this.type = type;
			this.blocking = blocking;
			this.major = major;
		}

		/// <summary>
		/// The constructor of a <see cref="SparseFormat"/> with any <see cref="Major"/>, typically used for sparse vector formats.
		/// </summary>
		public SparseFormat(Type type, Blocking blocking)
		{
			this.type = type;
			this.blocking = blocking;
			this.major = (Major)255;
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
		#endregion

		#region methods
		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Blocking"/> is <see cref="Blocking.NonBlock"/>.
		/// </summary>
		public readonly SparseFormat WithoutBlocking => new(this.type, Blocking.NonBlock, this.major);

		/// <summary>
		/// Get a <see cref="SparseFormat"/> whose <see cref="Blocking"/> is <see cref="Blocking.SimpleBlock"/>.
		/// </summary>
		public readonly SparseFormat WithBlocking => new(this.type, Blocking.SimpleBlock, this.major);

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
		/// Get whether this <see cref="SparseFormat"/> is of <see cref="Major.Row"/> or not.
		/// </summary>
		public readonly bool IsRowMajor => this.major == Major.Row;

		/// <summary>
		/// Get whether this <see cref="SparseFormat"/> is of <see cref="Type.Compressed"/> or not.
		/// </summary>
		public readonly bool IsCompress => this.type == Type.Compressed;

		/// <summary>
		/// Get whether this <see cref="SparseFormat"/> is of <see cref="Blocking.SimpleBlock"/> or not.
		/// </summary>
		public readonly bool IsBlocking => this.blocking == Blocking.SimpleBlock;

		/// <summary>
		/// Decompose this <see cref="SparseFormat"/> into atomic <paramref name="result"/>s.
		/// </summary>
		/// <param name="result">The <see cref="Span{T}"/> to put the results</param>
		/// <returns>The sliced <paramref name="result"/> whose length is the number of atomic formats</returns>
		/// <exception cref="ArgumentException">If the length of <paramref name="result"/> is less than the number of atomic formats</exception>
		public Span<SparseFormat> Decompose(Span<SparseFormat> result)
		{
			if (this.data == 0)
				return Span<SparseFormat>.Empty;
			byte t = (byte)this.type, b = (byte)this.blocking, m = (byte)this.major;
			byte pt = t.PopCount(), pb = b.PopCount(), pm = m.PopCount();
			int count = pt * pb * pm;
			if (result.Length < count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(result));
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
			return result;
		}
		#endregion
	}
}
