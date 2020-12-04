using System;
using System.Collections.Generic;
using System.Text;

namespace Althea.General
{
	/// <summary>
	/// Static and extend operations for arrays, you can include these extend operations by
	/// <code>using static <see cref="CudaCSharp"/>.<see cref="General"/>.<see cref="ArrayOperations"/>;</code>
	/// </summary>
	public static class ArrayOperations
	{
		#region general matrix extend
		/// <summary>
		/// Symmetrize matrix by adding its conjugate transpose out-of-place.
		/// </summary>
		/// <typeparam name="T">the data type, see <see cref="AbstractArray{T}"/> for more detail</typeparam>
		/// <typeparam name="TMat">the matrix type that inherits <see cref="AbstractArray{T}"/> and <see cref="IMatrix{TMat, TVec, T}"/></typeparam>
		/// <param name="A">the input <typeparamref name="TMat"/></param>
		/// <param name="α">scalar to multiply the result</param>
		/// <param name="conjugateAtLast">return the original </param>
		/// <returns>If <c><paramref name="conjugateAtLast"/> == false</c>: $B_{\text{result}}=\alpha \frac{A + A^H}{2}$; otherwise: $B_{\text{result}}=\alpha \frac{\bar{A} + A^T}{2}$</returns>
		public static TMat Symmetrize<TMat, T>(this TMat A, T α, bool conjugateAtLast = false)
			where TMat : AbstractArray<T>, IMatrix<TMat, T>
			where T : struct, IComparable<T>
		{
			if (A is null)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (A.NRows != A.NCols)
				throw new ArgumentException(Resource.MatMustSquare, nameof(A));
			if (A.IsRealType)
				conjugateAtLast = false;

			TMat result = null;
			try
			{
				if (A.Hermitian)
				{
					if (!conjugateAtLast)
						return A;
					result = A.Clone() as TMat;
					result.ConjugateInPlace(); // in-place conjugate;
					if (!α.IsOne())
						result.Scale(α);
					return result;
				}
				else
				{
					T halfAlpha = α.IsOne() ? Scalars<T>.Half : (T)((dynamic)α / 2);
					result.From_αA_Add_βB(A, A, halfAlpha, halfAlpha, opB: MatrixOperation.ConjugateTranspose);
					if (conjugateAtLast)
						result.ConjugateInPlace(); // in-place conjugate
					return result;
				}
			}
			catch (Exception)
			{
				result?.Dispose();
				throw;
			}
		}
		#endregion


		#region general vector extend
		// Ignore Spelling: vec
		//tex: Facts about Kronecker sum  times vector:
		//$$(A\oplus B)vec(X)\equiv(A\otimes I+I\otimes B)vec(X)=vec(XA^T+BX)$$
		//If we want the outcome to be Hermitian, we can use
		//$$\frac12[(A\oplus B)+(A^\dagger\oplus B^\dagger)],\\ \frac12[(A\oplus B)+(A^\dagger\oplus B^\dagger)]vec(X) = \frac12vec[X(A^T+\bar A)+(B+B^\dagger)X]$$
		//If $A$ is Hermitian, the last equation becomes
		//$$vec(XA^T)+\frac12vec[(B+B^\dagger)X]$$
		//If $B$ is Hermitian, the last equation becomes
		//$$\frac12vec[X(A^T+\bar A)]+vec(BX)$$

		/// <summary>
		/// Calculate $(A \oplus B) \cdot \vec{v}_{\text{this}}$ out-of-place.
		/// </summary>
		/// <typeparam name="T">the data type, see <see cref="AbstractArray{T}"/> for more detail</typeparam>
		/// <typeparam name="TMat">the concrete matrix type that inherits <see cref="AbstractArray{T}"/> and <see cref="IMatrix{TMat, TVec, T}"/></typeparam>
		/// <typeparam name="TVec">the concrete vector type that inherits <see cref="AbstractArray{T}"/> and <see cref="IVector{TVec, TMat, T}"/></typeparam>
		/// <param name="vec">input vector <typeparamref name="TVec"/></param>
		/// <param name="A">input matrix <typeparamref name="TMat"/> A</param>
		/// <param name="B">input matrix <typeparamref name="TMat"/> B</param>
		/// <param name="α">scalar to multiply the result</param>
		/// <param name="makeResultHermation">force the result to be Hermitian or not</param>
		/// <returns>$\alpha \cdot (A \oplus B) \cdot \vec{v}_{\text{this}} = \alpha (V A^T + B V)$ where $V=$<c>this.ToMatrix(<paramref name="B"/>.LeadDim)</c>. If both <paramref name="A"/> and <paramref name="B"/> are Hermitian or <paramref name="makeResultHermation"/> is false, only $V A^T + B V$ will be calculated; otherwise, the rank-k update $\alpha [V(A^T+\bar{A})+(B+B^H)V]/2$ will be used.</returns>
		public static TVec KroneckerSumTimesThis<TMat, TVec, T>(this TVec vec, TMat A, TMat B, T α, bool makeResultHermation = false)
			where TMat : Arrays.PureArray<T>, IMatrix<TMat, TVec, T>
			where TVec : Arrays.PureArray<T>, IVector<TVec, TMat, T>
			where T : struct, IComparable<T>
		{
			if (vec is null)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			if (A is null)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (A.NRows != A.NCols || B.NRows != B.NCols)
				throw new ArgumentException(Resource.MatMustSquare);
			if (vec.Length != A.NRows * B.NRows)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(vec));
			if (A.OnHost != B.OnHost || A.OnHost != vec.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (A.GetType() != B.GetType())
				throw new ArrayTypeMismatchException();
			if (!(vec.ToMatrix(B.NRows) is TMat V))
				throw new ArrayTypeMismatchException();

			TMat result = null;
			try
			{
				//tex:$V A^T + B V$
				if (!makeResultHermation || (A.Hermitian && B.Hermitian))
				{
					result = vec is IDenseArray<T> ? V.Clone() as TMat : V;
					result.Mulβ_AddBy_αAB(A: V, B: A, α: α, opB: MatrixOperation.Transpose);
					result.Mulβ_AddBy_αAB(A: B, B: V, α: α, β: Scalars<T>.One);
				}
				//tex:$[V(A^T+\bar{A})+(B+B^H)V]/2$
				else
				{
					TMat symmA = null, symmB = null;
					try
					{
						symmA = A.Symmetrize(); // do transpose later
						symmB = B.Symmetrize();
						result = vec is IDenseArray<T> ? V.Clone() as TMat : V;
						result.Mulβ_AddBy_αAB(A: V, B: symmA, α: α, opB: MatrixOperation.Transpose); // do transpose here
						result.Mulβ_AddBy_αAB(A: symmB, B: V, α: α, β: Scalars<T>.One);

					}
					finally
					{
						if (!A.Hermitian) symmA.Dispose();
						if (!B.Hermitian) symmB.Dispose();
					}
				}
			}
			catch (Exception)
			{
				result?.Dispose();
				throw;
			}
			finally
			{
				if (!(vec is IDenseArray<T>))
					(V as ISparseArray<T>).DisposeComparedTo(vec as ISparseArray<T>);
			}
			
			// return
			if (result is ISparseArray<T> sr)
			{
				var newVec = result.ToVector() as TVec;
				sr.DisposeComparedTo(newVec as ISparseArray<T>);
				return newVec;
			}
			else
				return result.ToVector() as TVec;
		}

		//tex: Facts about Kronecker product times vector:
		//$$(A\otimes B)vec(X)=vec(BXA^T) \text{ (not } A^\dagger\text)$$
		//If we want the outcome to be Hermitian, we can use (if both $A$ and $B$ are square)
		//$$\frac12[(A\otimes B^\dagger)+(A^\dagger\otimes B)],\quad
		//\frac12[(A\otimes B^\dagger)+(A^\dagger\otimes B)]vec(X)=\frac12[vec(B^\dagger XA^T+BX\bar A)]$$
		//If $A$ is Hermitian, the last equation becomes
		//$$\frac12vec[(B+B^\dagger)XA^T]$$
		//If $B$ is Hermitian, the last equation becomes
		//$$\frac12vec[BX(A^T+\bar A)]$$

		/// <summary>
		/// Calculate $(A \otimes B) \cdot \vec{v}_{\text{this}}$ out-of-place.
		/// </summary>
		/// <typeparam name="T">the data type, see <see cref="AbstractArray{T}"/> for more detail</typeparam>
		/// <typeparam name="TMat">the concrete matrix type that inherits <see cref="AbstractArray{T}"/> and <see cref="IMatrix{TMat, TVec, T}"/></typeparam>
		/// <typeparam name="TVec">the concrete vector type that inherits <see cref="AbstractArray{T}"/> and <see cref="IVector{TVec, TMat, T}"/></typeparam>
		/// <param name="vec">input vector <typeparamref name="TVec"/></param>
		/// <param name="A">input matrix <typeparamref name="TMat"/> A</param>
		/// <param name="B">input matrix <typeparamref name="TMat"/> B</param>
		/// <param name="α">scalar to multiply the result</param>
		/// <returns>$\alpha (A \otimes B) \cdot \vec{v}_{\text{this}} = \alpha (B V A^T)$ where $V=$<c>this.ToMatrix(<paramref name="B"/>.SecondDim)</c>.</returns>
		public static TVec KroneckerProdTimesThis<TMat, TVec, T>(this TVec vec, TMat A, TMat B, T α)
			where TMat : Arrays.PureArray<T>, IMatrix<TMat, TVec, T> 
			where TVec : Arrays.PureArray<T>, IVector<TVec, TMat, T>
			where T : struct, IComparable<T>
		{
			if (vec is null)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			if (A is null)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (A.OnHost != B.OnHost || A.OnHost != vec.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (vec.Length != A.NCols * B.NCols)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(vec));
			if (A.GetType() != B.GetType())
				throw new ArrayTypeMismatchException();
			if (!(vec.ToMatrix(B.NRows) is TMat V))
				throw new ArrayTypeMismatchException();

			TMat result = null, V_At = null, B_V_At = null;
			try
			{
				// calculate V * A^T
				V_At = Arrays.PureArrayFactory.Create<T>(A.GetType(), new[] { B.NCols, A.NRows }, A.OnHost, A.GetOtherInfo()) as TMat;
				V_At.Mulβ_AddBy_αAB(A: V, B: A, α: Scalars<T>.One, opB: MatrixOperation.Transpose);
				// calculate B * V * A^T
				B_V_At = Arrays.PureArrayFactory.Create<T>(A.GetType(), new[] { B.NCols, A.NRows }, A.OnHost, A.GetOtherInfo()) as TMat;
				B_V_At.Mulβ_AddBy_αAB(A: B, B: V_At, α: α);
				V_At.Dispose();
			}
			catch (Exception)
			{
				result?.Dispose();
				throw;
			}
			finally
			{
				if (result != B_V_At) B_V_At?.Dispose();
				if (result != V_At) V_At?.Dispose();
				if (!(vec is IDenseArray<T>))
					(V as ISparseArray<T>).DisposeComparedTo(vec as ISparseArray<T>);
			}

			// return
			if (result is ISparseArray<T> sr)
			{
				var newVec = result.ToVector() as TVec;
				sr.DisposeComparedTo(newVec as ISparseArray<T>);
				return newVec;
			}
			else
				return result.ToVector() as TVec;
		}
		#endregion
	}
}
