using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CudaCSharp.Linq;

using Newtonsoft.Json;


namespace TensorCSharp.OneDimension.CustomTensor
{
	/// <summary>
	/// The static class that provides extend methods for <see cref="ICharge{T}"/>
	/// </summary>
	public static class ChargeOperations
	{
		/// <summary>
		/// Additive (<see cref="ICharge{T}.Add(T)"/>) outer product of two charge arrays.
		/// </summary>
		/// <typeparam name="T">the type that inherits <see cref="ICharge{T}"/></typeparam>
		/// <param name="charges1">the left charge array</param>
		/// <param name="charges2">the right charge array</param>
		/// <param name="positive1">is <paramref name="charges1"/> positive or negative</param>
		/// <param name="positive2">is <paramref name="charges2"/> positive or negative</param>
		/// <returns>a new array generated from the additive outer product of <paramref name="charges1"/> (inner array) and <paramref name="charges2"/> (outer array)</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static T[] Outer<T>(this T[] charges1, T[] charges2, bool positive1, bool positive2) where T : ICharge<T>
		{
			if (charges1 is null || charges1.Length == 0)
				throw new ArgumentNullException(nameof(charges1));
			if (charges2 is null || charges2.Length == 0)
				throw new ArgumentNullException(nameof(charges2));

			var output = new T[charges1.LongLength * charges2.LongLength];
			for (int i = 0; i < charges2.Length; i++)
			{
				long ind = i * charges1.LongLength;
				for (int j = 0; j < charges1.Length; j++)
				{
					output[ind + j] = (positive1, positive2) switch
					{
						(true, true) => charges1[j].Add(charges2[i]),
						(false, true) => charges2[i].Sub(charges1[j]),
						(true, false) => charges1[j].Sub(charges2[i]),
						(false, false) => charges1[j].Add(charges2[i]).Dual(),
					};
				}
			}
			return output;
		}

		/// <summary>
		/// Outer product of two charge arrays (<see cref="ICharge{T}.Add(T)">additive</see> outer) and two multiplicity arrays (normal outer).
		/// </summary>
		/// <typeparam name="T">the type that inherits <see cref="ICharge{T}"/></typeparam>
		/// <param name="charge1">the left charge array</param>
		/// <param name="charge2">the right charge array</param>
		/// <param name="multiplicity1">the left multiplicity array</param>
		/// <param name="multiplicity2">the right multiplicity array</param>
		/// <param name="positive1">is <paramref name="charge1"/> positive or negative</param>
		/// <param name="positive2">is <paramref name="charge2"/> positive or negative</param>
		/// <returns>a new array generated from the additive outer product of <paramref name="charge1"/> (inner array) and <paramref name="charge2"/> (outer array)and a new array generated from the outer product of <paramref name="multiplicity1"/> and <paramref name="multiplicity2"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static (T[] charge, int[] multiplicity) Outer<T>(this T[] charge1, T[] charge2, int[] multiplicity1, int[] multiplicity2, bool positive1, bool positive2) where T : ICharge<T>
		{
			if (charge1 is null || charge1.Length == 0)
				throw new ArgumentNullException(nameof(charge1));
			if (charge2 is null || charge2.Length == 0)
				throw new ArgumentNullException(nameof(charge2));
			if (multiplicity1 is null || multiplicity1.Length != charge1.Length)
				throw new ArgumentNullException(nameof(multiplicity1));
			if (multiplicity2 is null || multiplicity2.Length != charge2.Length)
				throw new ArgumentNullException(nameof(multiplicity2));

			var outC = new T[charge1.LongLength * charge2.LongLength];
			var outM = new int[outC.LongLength];
			for (int i = 0; i < charge2.Length; i++)
			{
				long ind = i * charge1.LongLength;
				for (int j = 0; j < charge1.Length; j++)
				{
					outC[ind + j] = (positive1, positive2) switch
					{
						(true, true) => charge1[j].Add(charge2[i]),
						(false, true) => charge2[i].Sub(charge1[j]),
						(true, false) => charge1[j].Sub(charge2[i]),
						(false, false) => charge1[j].Add(charge2[i]).Dual(),
					};
					outM[ind + j] = multiplicity1[j] * multiplicity2[i];
				}
			}
			return (outC, outM);
		}

		/// <summary>
		/// Additive (<see cref="ICharge{T}.Add(T)"/>) outer product of multiple charge arrays.
		/// </summary>
		/// <typeparam name="T">the type that inherits <see cref="ICharge{T}"/></typeparam>
		/// <param name="chargeArrays">the charge arrays</param>
		/// <param name="positiveArray">a <see cref="bool"/> array to indicate each array in <paramref name="chargeArrays"/> being positive or negative</param>
		/// <returns>a new array generated from the additive outer product of <paramref name="chargeArrays"/> (former arrays are inner ones)</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static T[] Outer<T>(T[][] chargeArrays, bool[] positiveArray) where T : ICharge<T>
		{
			if (chargeArrays is null || chargeArrays.Length == 0)
				throw new ArgumentNullException(nameof(chargeArrays));
			if (positiveArray is null || positiveArray.Length != chargeArrays.Length)
				throw new ArgumentNullException(nameof(positiveArray));
			// shortcut
			if (chargeArrays.Length == 1)
				return positiveArray[0] ? chargeArrays[0] : Array.ConvertAll(chargeArrays[0], c => c.Dual());
			if (chargeArrays.Length == 2)
				return Outer(chargeArrays[0], chargeArrays[1], positiveArray[0], positiveArray[1]);
			// another check
			if (chargeArrays.Any(c => c is null || c.Length == 0))
				throw new ArgumentNullException(nameof(chargeArrays));

			var output = new T[chargeArrays.Aggregate((c, a) => c.Length * a, 1L)];
			Span<int> inds = stackalloc int[chargeArrays.Length];
			for (long i = 0; i < output.LongLength; i++)
			{
				// calculate sum
				// TODO: can use bifurcation sum
				T sum = positiveArray[0] ? chargeArrays[0][inds[0]] : chargeArrays[0][inds[0]].Dual();
				for (int j = 1; j < inds.Length; j++)
				{
					sum = positiveArray[j] ? sum.Add(chargeArrays[j][inds[j]]) : sum.Sub(chargeArrays[j][inds[j]]);
				}
				output[i] = sum;
				// change index
				inds[0]++;
				for (int j = 0; j < inds.Length - 1; j++)
				{
					if (inds[j] == chargeArrays[j].Length)
					{
						inds[j] = 0; inds[j + 1]++;
					}
				}
			}
			return output;
		}

		/// <summary>
		/// Outer product of multiple <paramref name="chargeArrays"/> (<see cref="ICharge{T}.Add(T)">additive</see> outer) and multiple<paramref name="multiplicityArrays"/> (normal outer).
		/// </summary>
		/// <typeparam name="T">the type that inherits <see cref="ICharge{T}"/></typeparam>
		/// <param name="chargeArrays">the charge arrays</param>
		/// <param name="multiplicityArrays">the multiplicity arrays</param>
		/// <param name="positiveArray">a <see cref="bool"/> array to indicate each array in <paramref name="chargeArrays"/> being positive or negative</param>
		/// <returns>a new array generated from the additive outer product of <paramref name="chargeArrays"/> (former arrays are inner ones) and a new array generated from normal outer product of <paramref name="multiplicityArrays"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static (T[] charge, int[] multiplicity) Outer<T>(T[][] chargeArrays, int[][] multiplicityArrays, bool[] positiveArray) where T : ICharge<T>
		{
			if (chargeArrays is null || chargeArrays.Length == 0)
				throw new ArgumentNullException(nameof(chargeArrays));
			if (multiplicityArrays is null || multiplicityArrays.Length != chargeArrays.Length)
				throw new ArgumentNullException(nameof(multiplicityArrays));
			if (positiveArray is null || positiveArray.Length != chargeArrays.Length)
				throw new ArgumentNullException(nameof(positiveArray));
			// shortcut
			if (chargeArrays.Length == 1)
				return (positiveArray[0] ? chargeArrays[0] : Array.ConvertAll(chargeArrays[0], c => c.Dual()), multiplicityArrays[0]);
			if (chargeArrays.Length == 2)
				return Outer(chargeArrays[0], chargeArrays[1], multiplicityArrays[0], multiplicityArrays[1], positiveArray[0], positiveArray[1]);
			// other checks
			if (chargeArrays.Any(c => c is null || c.Length == 0))
				throw new ArgumentNullException(nameof(chargeArrays));
			if (multiplicityArrays.Any(m => m is null || m.Length == 0))
				throw new ArgumentNullException(nameof(multiplicityArrays));
			if (chargeArrays.Zip(multiplicityArrays).Any(a => a.First.Length != a.Second.Length))
				throw new ArgumentException(Resource.LengthNotSame, nameof(multiplicityArrays));

			var outC = new T[chargeArrays.Aggregate((c, a) => c.Length * a, 1L)];
			var outM = new int[outC.LongLength];
			Span<int> inds = stackalloc int[chargeArrays.Length];
			for (long i = 0; i < outC.LongLength; i++)
			{
				// TODO: can use bifurcation sum / multiply, is it much faster?
				// calculate sum
				T sum = positiveArray[0] ? chargeArrays[0][inds[0]] : chargeArrays[0][inds[0]].Dual();
				int prod = multiplicityArrays[0][inds[0]];
				for (int j = 1; j < inds.Length; j++)
				{
					sum = positiveArray[j] ? sum.Add(chargeArrays[j][inds[j]]) : sum.Sub(chargeArrays[j][inds[j]]);
					prod *= multiplicityArrays[j][inds[j]];
				}
				outC[i] = sum;
				outM[i] = prod;
				// change index
				inds[0]++;
				for (int j = 0; j < inds.Length - 1; j++)
				{
					if (inds[j] == chargeArrays[j].Length)
					{
						inds[j] = 0; inds[j + 1]++;
					}
				}
			}
			return (outC, outM);
		}

		/// <summary>
		/// Outer product of two permutation arrays.
		/// </summary>
		/// <param name="permutation1">the first permutation arrays</param>
		/// <param name="permutation2">the second permutation arrays</param>
		/// <returns>a new permutation generated from the outer product of <paramref name="permutation1"/> and <paramref name="permutation2"/> (former arrays are inner ones)</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static int[] Outer(int[] permutation1, int[] permutation2)
		{
			if (permutation1 is null || permutation1.Length == 0)
				throw new ArgumentNullException(nameof(permutation1));
			if (permutation2 is null || permutation2.Length == 0)
				throw new ArgumentNullException(nameof(permutation2));

			int len1 = permutation1.Length, len2 = permutation2.Length;
			var output = new int[len1 * len2];
			for (int i = 0; i < len2; i++)
			{
				int ind = i * len1;
				for (int j = 0; j < len1; j++)
				{
					output[ind + j] = permutation1[j] + permutation2[i] * len1;
				}
			}
			return output;
		}

		/// <summary>
		/// Outer product of multiple permutation arrays.
		/// </summary>
		/// <param name="permutations">the permutation arrays</param>
		/// <returns>a new permutation generated from the outer product of <paramref name="permutations"/> (former arrays are inner ones)</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static int[] Outer(params int[][] permutations)
		{
			if (permutations is null || permutations.Length == 0)
				throw new ArgumentNullException(nameof(permutations));
			// shortcut
			if (permutations.Length == 1)
				return permutations[0];
			if (permutations.Length == 2)
				return Outer(permutations[0], permutations[1]);
			// another check
			if (permutations.Any(p => p is null || p.Length == 0))
				throw new ArgumentNullException(nameof(permutations));

			// get size prod
			Span<int> sizeProd = stackalloc int[permutations.Length + 1];
			sizeProd[0] = 1;
			for (int i = 0; i < sizeProd.Length; i++)
			{
				sizeProd[i + 1] = sizeProd[i] * permutations[i].Length;
			}
			// calculate
			var output = new int[sizeProd[^1]];
			Span<int> inds = stackalloc int[permutations.Length];
			for (int i = 0; i < output.Length; i++)
			{
				int sum = permutations[0][inds[0]];
				for (int j = 1; j < inds.Length; j++)
				{
					sum += permutations[j][inds[j]] * sizeProd[j];
				}
				output[i] = sum;
				// change index
				inds[0]++;
				for (int j = 0; j < inds.Length - 1; j++)
				{
					if (inds[j] == permutations[j].Length)
					{
						inds[j] = 0; inds[j + 1]++;
					}
				}
			}
			return output;
		}
	}

	/// <summary>
	/// The interface for charges used by <see cref="BlockSparseTensor{T, TC}"/>
	/// </summary>
	/// <typeparam name="T">the type that inherits <see cref="ICharge{T}"/>, to make sure that the declaration of <typeparamref name="T"/> is like "<c>struct SomeCharge : ICharge&lt;SomeCharge&gt;</c>"</typeparam>
	/// <remarks>
	/// <c>default(<typeparamref name="T"/>)</c> should be implemented as the identity value of charge <typeparamref name="T"/><br/>
	/// We recommend use a <c>struct</c> to implement this interface for performance.<br/>
	/// It shall has ways to control deserialization and serialization, such as constructor with attribute <see cref="JsonConstructorAttribute"/> and members with attribute <see cref="JsonPropertyAttribute"/>.
	/// </remarks>
	public interface ICharge<T> : IEquatable<T>, IComparable<T> where T : ICharge<T>
	{
		/// <summary>
		/// Check whether this charge contains valid value
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Add another value <b>out-of-place</b>. Default <see cref="Sub(T)"/> is done by <c><see cref="Add">Add</see>(<paramref name="another"/>.<see cref="Dual">Dual()</see>)</c>.
		/// </summary>
		/// <param name="another">another value to add</param>
		/// <returns>a new <typeparamref name="T"/> as the addition result</returns>
		T Add(T another);

		/// <summary>
		/// Get the dual charge <b>out-of-place</b>
		/// </summary>
		/// <returns>a new <typeparamref name="T"/> as the dual charge of this one</returns>
		T Dual();

		/// <summary>
		/// The default implementation for subtracting another value <b>out-of-place</b>.
		/// </summary>
		/// <param name="another">another value to subtract</param>
		/// <returns>a new <typeparamref name="T"/> as the subtraction result</returns>
		public T Sub(T another) => this.Add(another.Dual());

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
		int GetHashCode();

		/// <summary>
		/// Convert the array of <typeparamref name="T"/> to an <see cref="Array"/> that can be serialized by JSON converters.
		/// </summary>
		/// <param name="array">the array to be converted</param>
		/// <returns>the <see cref="Array"/> that can be serialized</returns>
		/// <remarks>This is actually a static method.<br/>
		/// It is recommended to implement in a way such that <paramref name="array"/> is converted to a native JSON array format rather than an array of JSON objects.</remarks>
		Array SerializeArray(T[] array);

		/// <summary>
		/// Convert the <see cref="Array"/> that can be serialized to an array of <typeparamref name="T"/>.
		/// </summary>
		/// <param name="obj">the <see cref="Array"/> to be converted</param>
		/// <returns>the converted array of <typeparamref name="T"/></returns>
		/// <remarks>This is actually a static method.<br/>
		/// The implementation can only consider the <see cref="Array"/> obtained by <see cref="SerializeArray(T[])"/>.</remarks>
		T[] DeserializeArray(Array obj);
	}

	/// <summary>
	/// The charge for U(1) symmetry group
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct U1Symmetry : ICharge<U1Symmetry>
	{
		#region basics
		[JsonProperty]
		private readonly int quantumNumber;

		/// <summary>
		/// Construct with the given quantum number
		/// </summary>
		/// <param name="quantumNumber">the given quantum number</param>
		[JsonConstructor]
		public U1Symmetry(int quantumNumber) => this.quantumNumber = quantumNumber;

		/// <summary>
		/// Converter from quantum number as an <see cref="int"/>
		/// </summary>
		/// <param name="quantumNumber">the quantum number as an <see cref="int"/></param>
		public static implicit operator U1Symmetry(int quantumNumber) => new U1Symmetry(quantumNumber);

		/// <summary>
		/// Get the string representation of this <see cref="U1Symmetry"/>.
		/// </summary>
		/// <returns>the string representation</returns>
		public override string ToString()
		{
			return quantumNumber.ToString();
		}
		#endregion

		#region interface
		/// <summary>
		/// Check whether this charge contains valid value
		/// </summary>
		public bool IsValid => true;

		/// <summary>
		/// Add another value <b>out-of-place</b>.
		/// </summary>
		/// <param name="another">another value to add</param>
		/// <returns>a new <see cref="U1Symmetry"/> as the addition result</returns>
		public U1Symmetry Add(U1Symmetry another)
		{
			return new U1Symmetry(this.quantumNumber + another.quantumNumber);
		}

		/// <summary>
		/// Get the dual charge <b>out-of-place</b>
		/// </summary>
		/// <returns>a new <see cref="U1Symmetry"/> as the dual charge of this charge</returns>
		public U1Symmetry Dual()
		{
			return new U1Symmetry(-this.quantumNumber);
		}

		/// <summary>
		/// Subtract another value <b>out-of-place</b>.
		/// </summary>
		/// <param name="another">another value to subtract</param>
		/// <returns>a new <see cref="U1Symmetry"/> as the subtraction result</returns>
		public U1Symmetry Sub(U1Symmetry another)
		{
			return new U1Symmetry(this.quantumNumber - another.quantumNumber);
		}
		#endregion

		#region equality
		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/>; otherwise, false</returns>
		public bool Equals(U1Symmetry other)
		{
			return this.quantumNumber == other.quantumNumber;
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object.
		/// </summary>
		/// <param name="obj">another object to compare with this one</param>
		/// <returns>true if the current object is equal to <paramref name="obj"/>; otherwise, false</returns>
		public override bool Equals(object obj)
		{
			return obj is U1Symmetry u1 && this.Equals(u1);
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
		public override int GetHashCode()
		{
			return this.quantumNumber;
		}

		/// <summary>
		/// Compare to another object <paramref name="other"/>
		/// </summary>
		/// <param name="other">another object to compare with</param>
		/// <returns>0 if this object equals <paramref name="other"/>, positive if this object proceeds <paramref name="other"/>, negative otherwise</returns>
		public int CompareTo(U1Symmetry other)
		{
			return this.quantumNumber.CompareTo(other.quantumNumber);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(U1Symmetry left, U1Symmetry right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Non-equality operator
		/// </summary>
		public static bool operator !=(U1Symmetry left, U1Symmetry right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Less than operator
		/// </summary>
		public static bool operator <(U1Symmetry left, U1Symmetry right)
		{
			return left.CompareTo(right) < 0;
		}

		/// <summary>
		/// Less than or equals operator
		/// </summary>
		public static bool operator <=(U1Symmetry left, U1Symmetry right)
		{
			return left.CompareTo(right) <= 0;
		}

		/// <summary>
		/// Larger than operator
		/// </summary>
		public static bool operator >(U1Symmetry left, U1Symmetry right)
		{
			return left.CompareTo(right) > 0;
		}

		/// <summary>
		/// Larger than or equals operator
		/// </summary>
		public static bool operator >=(U1Symmetry left, U1Symmetry right)
		{
			return left.CompareTo(right) >= 0;
		}

		/// <summary>
		/// Explicitly convert <see cref="U1Symmetry"/> <paramref name="v"/> to a <see cref="int"/>
		/// </summary>
		/// <param name="v">the <see cref="U1Symmetry"/> to convert</param>
		public static explicit operator int(U1Symmetry v)
		{
			return v.quantumNumber;
		}
		#endregion

		#region serialization
		/// <summary>
		/// Convert the array of <see cref="U1Symmetry"/> to an <see cref="Array"/> that can be serialized by JSON converters.
		/// </summary>
		/// <param name="array">the array to be converted</param>
		/// <returns>the <see cref="Array"/> that can be serialized</returns>
		/// <remarks>This is actually a static method.<br/>
		/// It is recommended to implement in a way such that <paramref name="array"/> is converted to a native JSON array format rather than an array of JSON objects.</remarks>
		public Array SerializeArray(U1Symmetry[] array) => Array.ConvertAll(array, a => a.quantumNumber);

		/// <summary>
		/// Convert the <see cref="Array"/> that can be serialized to an array of <see cref="U1Symmetry"/>.
		/// </summary>
		/// <param name="obj">the <see cref="Array"/> to be converted</param>
		/// <returns>the converted array of <see cref="U1Symmetry"/></returns>
		/// <remarks>This is actually a static method.<br/>
		/// The implementation can only consider the <see cref="Array"/> obtained by <see cref="SerializeArray"/>.</remarks>
		public U1Symmetry[] DeserializeArray(Array obj) => Array.ConvertAll(obj as int[], q => new U1Symmetry(q));
		#endregion
	}
}
