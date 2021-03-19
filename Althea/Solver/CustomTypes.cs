using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.NativeTypes;


namespace Althea.Solver
{
	#region interface
	/// <summary>
	/// The interface of vector that contains the operation needed for Krylov-subspace methods such as Lanczos and Krylov-Schur solver.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TVec">The vector type</typeparam>
	public interface IKrylovVector<TVec, T> : IDisposable
		where TVec : class, IKrylovVector<TVec, T>, IDisposable, new()
		where T : unmanaged
	{
		/// <summary>
		/// The total presenting length of this vector
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Create a new vector alike this one
		/// </summary>
		/// <returns>The new vector alike this one</returns>
		TVec NewArrayAlike();

		/// <summary>
		/// Fill this vector with the given <paramref name="value"/>
		/// </summary>
		/// <param name="value">The value to fill</param>
		void FillWith(T value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this vector with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		void Scale(T value);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this vector.
		/// </summary>
		/// <returns>The 2-norm of this vector</returns>
		double Norm();

		/// <summary>
		/// When implemented by a derived class, in-place scale this vector such that its 2-norm (Euclidean norm) is one.
		/// </summary>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		void Normalize();

		/// <summary>
		/// When implemented by a derived class, compute dot (inner) product of this vector and <paramref name="other"/> vector. The conjugate of this vector shall be actually used.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to perform the dot product</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		T Dot(TVec other);

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		void AddBy(TVec other, T scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		void ReplaceBy(TVec other);

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <typeparamref name="TVec"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <typeparamref name="TVec"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		/// <exception cref="ArgumentNullException">If any of <paramref name="unjoinedVectors"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="input"/> and <paramref name="unjoinedVectors"/> have different size, or any element of <paramref name="unjoinedVectors"/> has different size than this vector</exception>
		TVec OperateOn(ReadOnlySpan<TVec> unjoinedVectors, ReadOnlySpan<T> input)
		{
			if (unjoinedVectors.IsEmpty)
				throw new ArgumentNullException(nameof(unjoinedVectors));
			if (input.IsEmpty)
				throw new ArgumentNullException(nameof(input));
			if (unjoinedVectors.Length != input.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);

			// sort first to reduce errors
			int length = input.Length;
			Span<(T, IntPtr)> temp = length.CheckStackLimit<(T, IntPtr)>() ?? stackalloc (T, IntPtr)[length];
			Span<(T val, TVec vec)> values = MemoryMarshal.CreateSpan(ref Unsafe.As<(T, IntPtr), (T, TVec)>(ref temp[0]), length);
			Span<double> keys = length.CheckStackLimit<double>() ?? stackalloc double[length];
			TVec[] vectors = unjoinedVectors.ToArray();
			for (int i = 0; i < length; i++)
			{
				values[i] = (input[i], unjoinedVectors[i]);
				keys[i] = Const<T>.AbsoluteDelegate.Invoke(input[i]);
			}
			keys.Sort(values);

			long vecLen = this.Length;
			var vec = this.NewArrayAlike();
			try
			{
				vec.FillWith(default);
				for (int i = 0; i < length; i++)
				{
					var dnvec = values[i].vec;
					var val = values[i].val;
					if (dnvec is null)
						throw new ArgumentNullException(nameof(unjoinedVectors));
					if (dnvec.Length != vecLen)
						throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(unjoinedVectors));
					if (!val.IsZero())
						vec.AddBy(dnvec, val);
				}
				return vec;
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
		}
	}
	#endregion
}
