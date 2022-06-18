using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.NativeTypes;

namespace Althea.Backend.CSharp.LinearAlgebra;


/// <summary>
/// Static class for solving matrices represented by <see cref="Span{T}"/>s.
/// </summary>
public static unsafe class MatrixSolvers
{
	// TODO: use existing codes
	// Ignore Spelling: \vec \alpha \beta \tau \ldots \langle \rangle \pmatrix \cdot \begin \leftarrow \circ \odot \otimes \mathcal \mathrm \le eigval argmin
	#region utilities
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T NormSq<T>(T* vec, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (Api.Inner<T, byte>(true, vec, 1, vec, 1, n, out T norm))
			return norm;
		norm = T.Zero;
		for (int i = 0; i < n; i++)
		{
			norm += vec[i].Conjugate() * vec[i];
		}
		return norm;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T Dot<T>(T* x, T* y, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (Api.Inner<T, bool>(true, x, 1, y, 1, n, out T dot))
			return dot;
		dot = T.Zero;
		for (int i = 0; i < n; i++)
		{
			dot += x[i].Conjugate() * y[i];
		}
		return dot;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Scale<T>(T* x, T α, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (Api.VectorModify<T, T, Api.U_MultiplyScalar>(x, 1, x, 1, n, α))
			return;
		for (int i = 0; i < n; i++)
		{
			x[i] *= α;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AddScaled<T>(T* y, T* x, T α, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (Api.VectorsBinary<T, Api.B_AddScaled>(x, 1, x, 1, y, 1, α, n))
			return;
		for (int i = 0; i < n; i++)
		{
			y[i] += α * x[i];
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VecMulMat<T>(T* x, T* A, int ld, T* output, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			output[i] = Dot(x, A + i * ld, m);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VecMulScaledMat<T>(T* A, int ld, T α, T* x, T* output, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < m; i++)
		{
			output[i] = α * Dot(x, A + i * ld, n);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void MatMulScaledVec<T>(T* A, int ld, T α, T* x, T* output, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		Unsafe.InitBlockUnaligned(output, 0, (uint)(m * sizeof(T)));
		if (NumberType<T>.IsComplex)
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(output, A + i * ld, (x[i] * α).Conjugate(), m);
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(output, A + i * ld, x[i] * α, m);
			}
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void MatMulVec<T>(T* A, int ld, T* x, T* output, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		Unsafe.InitBlockUnaligned(output, 0, (uint)(m * sizeof(T)));
		if (NumberType<T>.IsComplex)
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(output, A + i * ld, x[i].Conjugate(), m);
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(output, A + i * ld, x[i], m);
			}
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Rank1UpdateNeg<T>(T* A, int ld, T* x, T* y, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (NumberType<T>.IsComplex)
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(A + i * ld, x, -y[i].Conjugate(), m);
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(A + i * ld, x, -y[i], m);
			}
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SymRank2UpdateNeg<T>(T* A, int ld, T* x, T* y, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (NumberType<T>.IsComplex)
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(A + i * ld, x, -y[i].Conjugate(), n);
				AddScaled(A + i * ld, y, -x[i].Conjugate(), n);
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				AddScaled(A + i * ld, x, -y[i], n);
				AddScaled(A + i * ld, y, -x[i], n);
			}
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void InPlaceTranspose<T>(T* A, int ld, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			for (int j = i + 1; j < n; j++)
			{
				(A[i + j * ld], A[j + i * ld]) = (A[j + i * ld], A[i + j * ld]);
			}
		}
	}

	private const DataType Accelerated = DataType.RealSingle | DataType.RealDouble | DataType.ComplexSingle | DataType.ComplexDouble;

	// x / (y + scalar)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PointWiseDivideAddScalar<T>(T* x, T* y, int n, T scalar, T scalarIm, T* result, T* resultIm) where T : unmanaged, IFloatingPoint<T>
	{
		if (resultIm != null)
		{	// real type
			T imSq = scalarIm * scalarIm;
			scalarIm = -scalarIm;
			T* xEnd = x + n;
			Vector<T> imSqs = new(imSq), scalarIms = new(scalarIm);
			while (x + Vector<T>.Count <= xEnd)
			{
				var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
				var yy = Unsafe.ReadUnaligned<Vector<T>>(y);
				var abs = yy * yy + imSqs;
				yy *= xx / abs;
				xx *= scalarIms / abs;
				Unsafe.WriteUnaligned(result, yy);
				Unsafe.WriteUnaligned(resultIm, xx);
				x += Vector<T>.Count; y += Vector<T>.Count;
				result += Vector<T>.Count; resultIm += Vector<T>.Count;
			}
			n = (int)(xEnd - x);
			for (int i = 0; i < n; i++)
			{
				T abs = y[i] * y[i] + imSq;
				result[i] = x[i] * y[i] / abs;
				resultIm[i] = x[i] * scalarIm / abs;
			}
		}
		else
		{
			if (!Vector.IsHardwareAccelerated || (Unmanaged<T>.DataType & Accelerated) == 0 || n <= 8)
				goto SCALAR;
			if (NumberType<T>.IsComplex)
			{
				Api.VectorModify<T, T, Api.U_AddScalar>(y, 1, result, 1, n, scalar);
				Api.VectorsBinary<T, Api.B_Divide>(x, 1, result, 1, result, 1, default, n);
				return;
			}
			// real type
			T* xEnd = x + n;
			Vector<T> scalars = new(scalar);
			while (x + Vector<T>.Count <= xEnd)
			{
				var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
				var yy = Unsafe.ReadUnaligned<Vector<T>>(y);
				xx /= (yy + scalars);
				Unsafe.WriteUnaligned(result, xx);
				x += Vector<T>.Count; y += Vector<T>.Count; result += Vector<T>.Count;
			}
			n = (int)(xEnd - x);
		SCALAR:
			for (int i = 0; i < n; i++)
			{
				result[i] = x[i] / (y[i] + scalar);
			}
		}
	}
	// x / y
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PointWiseDivide<T>(T* x, T* y, T* yIm, int n, T* result, T* resultIm) where T : unmanaged, IFloatingPoint<T>
	{
		if (resultIm != null)
		{	// real type
			T* xEnd = x + n;
			while (x + Vector<T>.Count <= xEnd)
			{
				var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
				var yy = Unsafe.ReadUnaligned<Vector<T>>(y);
				var yyI = Unsafe.ReadUnaligned<Vector<T>>(yIm);
				var abs = yy * yy + yyI * yyI;
				var re = xx * yy / abs;
				var im = -xx * yyI / abs;
				Unsafe.WriteUnaligned(result, re);
				Unsafe.WriteUnaligned(resultIm, im);
				x += Vector<T>.Count; y += Vector<T>.Count; yIm += Vector<T>.Count;
				result += Vector<T>.Count; resultIm += Vector<T>.Count;
			}
			n = (int)(xEnd - x);
			for (int i = 0; i < n; i++)
			{
				T abs = y[i] * y[i] + yIm[i] * yIm[i];
				(result[i], resultIm[i]) = (x[i] * y[i] / abs, -x[i] * yIm[i] / abs);
			}
		}
		else
		{
			if (Api.VectorsBinary<T, Api.B_Divide>(x, 1, y, 1, result, 1, default, n))
				return;
			// software fall back
			for (int i = 0; i < n; i++)
			{
				result[i] = x[i] / y[i];
			}
		}
	}
	// x * y
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PointWiseMultiply<T>(T* x, T* xIm, T* y, T* yIm, int n, T* result, T* resultIm) where T : unmanaged, IFloatingPoint<T>
	{
		if (resultIm != null)
		{   // real type
			T* xEnd = x + n;
			while (x + Vector<T>.Count <= xEnd)
			{
				var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
				var xxI = Unsafe.ReadUnaligned<Vector<T>>(xIm);
				var yy = Unsafe.ReadUnaligned<Vector<T>>(y);
				var yyI = Unsafe.ReadUnaligned<Vector<T>>(yIm);
				var re = xx * yy - xxI * yyI;
				var im = xxI * yy + xx * yyI;
				Unsafe.WriteUnaligned(result, re);
				Unsafe.WriteUnaligned(resultIm, im);
				x += Vector<T>.Count; xIm += Vector<T>.Count;
				y += Vector<T>.Count; yIm += Vector<T>.Count;
				result += Vector<T>.Count; resultIm += Vector<T>.Count;
			}
			n = (int)(xEnd - x);
			for (int i = 0; i < n; i++)
			{
				(result[i], resultIm[i]) = (x[i] * y[i] - xIm[i] * yIm[i], xIm[i] * y[i] + x[i] * yIm[i]);
			}
		}
		else
		{
			if (Api.VectorsBinary<T, Api.B_Multiply>(x, 1, y, 1, result, 1, default, n))
				return;
			// software fall back
			for (int i = 0; i < n; i++)
			{
				result[i] = x[i] * y[i];
			}
		}
	}
	// x * y + z + scalar
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PointWiseMultiplyAddScalar<T>(T* x, T* xIm, T* y, T* z, int n, T scalar, T scalarIm, T* result, T* resultIm) where T : unmanaged, IFloatingPoint<T>
	{
		if (resultIm != null)
		{   // real type
			T* xEnd = x + n;
			Vector<T> scalars = new(scalar), scalarsIm = new(scalarIm);
			while (x + Vector<T>.Count <= xEnd)
			{
				var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
				var xxI = Unsafe.ReadUnaligned<Vector<T>>(xIm);
				var yy = Unsafe.ReadUnaligned<Vector<T>>(y);
				var zz = Unsafe.ReadUnaligned<Vector<T>>(z);
				var re = xx * yy + zz + scalars;
				var im = xxI * yy + scalarsIm;
				Unsafe.WriteUnaligned(result, re);
				Unsafe.WriteUnaligned(resultIm, im);
				x += Vector<T>.Count; xIm += Vector<T>.Count;
				y += Vector<T>.Count;
				result += Vector<T>.Count; resultIm += Vector<T>.Count;
			}
			n = (int)(xEnd - x);
			for (int i = 0; i < n; i++)
			{
				(result[i], resultIm[i]) = (x[i] * y[i] + z[i] + scalar, xIm[i] * y[i] + scalarIm);
			}
		}
		else
		{
			if (Api.VectorsBinary<T, Api.B_Multiply>(x, 1, y, 1, result, 1, default, n))
			{
				Api.VectorsBinary<T, Api.B_Add>(result, 1, z, 1, result, 1, default, n);
				Api.VectorModify<T, T, Api.U_AddScalar>(result, 1, result, 1, n, scalar);
				return;
			}
			// software fall back
			for (int i = 0; i < n; i++)
			{
				result[i] = x[i] * y[i] + z[i] + scalar;
			}
		}
	}
	// a * b + x * y
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PointWiseMultiplyAdd<T>(T* a, T* aIm, T* b, T* x, T* xIm, T* y, int n, T* result, T* resultIm) where T : unmanaged, IFloatingPoint<T>
	{
		if (aIm != null)
		{
			for (int i = 0; i < n; i++)
			{
				(result[i], resultIm[i]) = (a[i] * b[i] + x[i] * y[i], aIm[i] * b[i] + xIm[i] * y[i]);
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				result[i] = a[i] * b[i] + x[i] * y[i];
			}
		}
	}
	// 1 / x
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PointWiseInv<T>(T* x, T* xIm, int n, T* result, T* resultIm) where T : unmanaged, IFloatingPoint<T>
	{
		if (resultIm != null)
		{
			for (int i = 0; i < n; i++)
			{
				T abs = x[i] * x[i] + xIm[i] * xIm[i];
				(result[i], resultIm[i]) = (x[i] / abs, -xIm[i] / abs);
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				result[i] = T.One / x[i];
			}
		}
	}
	// x + y * scalar
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PointWiseAddScaled<T>(T* x, T* xIm, T* y, T* yIm, int n, T scalar, T scalarIm, T* result, T* resultIm) where T : unmanaged, IFloatingPoint<T>
	{
		if (resultIm != null)
		{
			for (int i = 0; i < n; i++)
			{
				(result[i], resultIm[i]) = (x[i] + scalar * y[i] - scalarIm * yIm[i], xIm[i] + scalarIm * y[i] + scalar * yIm[i]);
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				result[i] = x[i] + scalar * y[i];
			}
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AllZeroComparedTo<T>(T* x, int n, T scalar) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			if (x[i] + scalar != scalar)
				return false;
		}
		return true;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (T Re, T Im) Dot<T>(T* xRe, T* xIm, T* yRe, T* yIm, int n) where T : unmanaged, IFloatingPoint<T>
	{
		T dotRe = T.Zero, dotIm = T.Zero;
		for (int i = 0; i < n; i++)
		{
			dotRe += xRe[i] * yRe[i] + xIm[i] * yIm[i];
			dotIm += xRe[i] * yIm[i] - xIm[i] * yRe[i];
		}
		return (dotRe, dotIm);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T NormSq<T>(T* vecRe, T* vecIm, int n) where T : unmanaged, IFloatingPoint<T>
	{
		T norm = T.Zero;
		for (int i = 0; i < n; i++)
			norm += vecRe[i] * vecRe[i] + vecIm[i] * vecIm[i];
		return norm;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Scale<T>(T* xRe, T* xIm, int n, T scalar) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			(xRe[i], xIm[i]) = (xRe[i] * scalar, xIm[i] * scalar);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Orthogonalize<T>(bool complex, int n, int nVecs, T* Q, int ldq) where T : unmanaged, IFloatingPoint<T>
	{
		if (complex)
		{
			for (int i = 0; i < nVecs; i++)
			{
				T* rRe = Q + 2 * i * ldq, rIm = Q + (2 * i + 1) * ldq;
				for (int j = 0; j < i; j++)
				{
					T* qRe = Q + 2 * j * ldq, qIm = Q + (2 * j + 1) * ldq;
					var (wRe, wIm) = Dot(qRe, qIm, rRe, rIm, n);
					var denom = NormSq(qRe, qIm, n);
					wRe /= denom; wIm /= denom;
					PointWiseAddScaled(rRe, rIm, qRe, qIm, n, -wRe, -wIm, rRe, rIm);
				}
				Scale(rRe, rIm, n, T.One / T.Sqrt(NormSq(rRe, rIm, n)));
			}
		}
		else
		{
			for (int i = 0; i < nVecs; i++)
			{
				var r = Q + i * ldq;
				for (int j = 0; j < i; j++)
				{
					var q = Q + j * ldq;
					var weight = Dot(q, r, n) / NormSq(q, n);
					AddScaled(r, q, -weight, n);
				}
				Scale(r, T.One / T.Sqrt(NormSq(r, n)), n);
			}
		}
	}
	#endregion

	#region orthogonal transformations
	[StructLayout(LayoutKind.Sequential)]
	private readonly ref struct Householder2<T> where T : unmanaged, IFloatingPoint<T>
	{
		private readonly T v1, v2, tau;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Householder2(T v1, T v2)
		{
			T normV = T.Sqrt(v1 * v1 + v2 * v2);
			v1 += T.CopySign(normV, v1);
			this.tau = (T.One + T.One) / (v1 * v1 + v2 * v2);
			this.v1 = v1; this.v2 = v2;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly Householder2x2<T> ToReflectionMatrix()
		{
			T h2 = T.One - tau * v2 * v2, h4 = -tau * v1 * v2;
			return new(h2, h4);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private readonly ref struct Householder3<T> where T : unmanaged, IFloatingPoint<T>
	{
		private readonly T v1, v2, v3, tau;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Householder3(T v1, T v2, T v3)
		{
			T temp = v2 * v2 + v3 * v3;
			T normV = T.Sqrt(v1 * v1 + temp);
			v1 += T.CopySign(normV, v1);
			this.tau = (T.One + T.One) / (v1 * v1 + temp);
			this.v1 = v1; this.v2 = v2; this.v3 = v3;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly Householder3x3<T> ToReflectionMatrix()
		{
			T h1 = T.One - tau * v1 * v1, h2 = T.One - tau * v2 * v2, h4 = -tau * v1 * v2;
			T h3 = T.One - tau * v3 * v3, h5 = -tau * v1 * v3, h6 = -tau * v2 * v3;
			return new(h1, h2, h3, h4, h5, h6);
		}
	}

	//tex:$H = \begin{pmatrix}h_1&h_3\\h_3&h_2\end{pmatrix}$
	[StructLayout(LayoutKind.Sequential)]
	private readonly ref struct Householder2x2<T> where T : unmanaged, IFloatingPoint<T>
	{
		private readonly T h1, h2, h3;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Householder2x2(T h22, T h12)
		{
			this.h1 = -h22; this.h2 = h22; this.h3 = h12;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void ColumnUpdate(T* A, int ld, int n)
		{
			for (int i = 0, j = ld; i < n; i++, j++)
			{
				(A[i], A[j]) = (A[i] * h1 + A[j] * h3, A[i] * h3 + A[j] * h2);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RowUpdate(T* A, int ld, int n)
		{
			for (int nn = 0, i = 0; nn < n; nn++, i += ld)
			{
				int j = i + 1;
				(A[i], A[j]) = (A[i] * h1 + A[j] * h3, A[i] * h3 + A[j] * h2);
			}
		}
	}

	//tex:$H = \begin{pmatrix}h_1&h_4&h_5\\h_4&h_2&h_6\\h_5&h_6&h_3\end{pmatrix}$
	[StructLayout(LayoutKind.Sequential)]
	private readonly ref struct Householder3x3<T> where T : unmanaged, IFloatingPoint<T>
	{
		private readonly T h1, h2, h3, h4, h5, h6;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Householder3x3(T h1, T h2, T h3, T h4, T h5, T h6)
		{
			this.h1 = h1; this.h2 = h2; this.h3 = h3; this.h4 = h4; this.h5 = h5; this.h6 = h6;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void ColumnUpdate(T* A, int ld, int n)
		{
			for (int i = 0, j = ld, k = ld * 2; i < n; i++, j++, k++)
			{
				(A[i], A[j], A[k]) = (A[i] * h1 + A[j] * h4 + A[k] * h5, A[i] * h4 + A[j] * h2 + A[k] * h6, A[i] * h5 + A[j] * h6 + A[k] * h3);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RowUpdate(T* A, int ld, int n)
		{
			for (int i = 0, j = 0; i < n; i++, j += ld)
			{
				(A[j], A[j + 1], A[j + 2]) = (A[j] * h1 + A[j + 1] * h4 + A[j + 2] * h5, A[j] * h4 + A[j + 1] * h2 + A[j + 2] * h6, A[j] * h5 + A[j + 1] * h6 + A[j + 2] * h3);
			}
		}
	}

	//tex:$Q = \begin{pmatrix}q_1&q_4&q_7\\q_2&q_5&q_8\\q_3&q_6&q_9\end{pmatrix}$
	[StructLayout(LayoutKind.Sequential)]
	private readonly ref struct Orthogonal3x3<T> where T : unmanaged, IFloatingPoint<T>
	{
		private readonly T q1, q2, q3, q4, q5, q6, q7, q8, q9;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Orthogonal3x3(T q1, T q2, T q3, T q4, T q5, T q6, T q7, T q8, T q9)
		{
			this.q1 = q1; this.q2 = q2; this.q3 = q3; this.q4 = q4; this.q5 = q5;
			this.q6 = q6; this.q7 = q7; this.q8 = q8; this.q9 = q9;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void ColumnUpdate(T* A, int ld, int n)
		{
			for (int i = 0, j = ld, k = ld * 2; i < n; i++, j++, k++)
			{
				(A[i], A[j], A[k]) = (A[i] * q1 + A[j] * q2 + A[k] * q3, A[i] * q4 + A[j] * q5 + A[k] * q6, A[i] * q7 + A[j] * q8 + A[k] * q9);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RowUpdate(T* A, int ld, int n)
		{
			for (int nn = 0, i = 0; nn < n; nn++, i += ld)
			{
				int j = i + 1, k = j + 1;
				// Q is transposed
				(A[i], A[j], A[k]) = (A[i] * q1 + A[j] * q2 + A[k] * q3, A[i] * q4 + A[j] * q5 + A[k] * q6, A[i] * q7 + A[j] * q8 + A[k] * q9);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private readonly ref struct Orthogonal4x4<T> where T : unmanaged, IFloatingPoint<T>
	{
		private readonly T q1, q2, q3, q4, q5, q6, q7, q8, q9, qA, qB, qC, qD, qE, qF, qG;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void ColumnUpdate(T* A, int ld, int n)
		{
			for (int i = 0, j = ld, k = ld * 2, l = ld * 3; i < n; i++, j++, k++, l++)
			{
				(A[i], A[j], A[k], A[l]) =
				(
					A[i] * q1 + A[j] * q2 + A[k] * q3 + A[l] * q4,
					A[i] * q5 + A[j] * q6 + A[k] * q7 + A[l] * q8,
					A[i] * q9 + A[j] * qA + A[k] * qB + A[l] * qC,
					A[i] * qD + A[j] * qE + A[k] * qF + A[l] * qG
				);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RowUpdate(T* A, int ld, int n)
		{
			for (int nn = 0, i = 0; nn < n; nn++, i += ld)
			{
				int j = i + 1, k = j + 1, l = k + 1;
				// Q is transposed
				(A[i], A[j], A[k], A[l]) =
				(
					A[i] * q1 + A[j] * q2 + A[k] * q3 + A[l] * q4,
					A[i] * q5 + A[j] * q6 + A[k] * q7 + A[l] * q8,
					A[i] * q9 + A[j] * qA + A[k] * qB + A[l] * qC,
					A[i] * qD + A[j] * qE + A[k] * qF + A[l] * qG
				);
			}
		}
	}
	#endregion


	#region QR
	/// <summary>
	/// Perform the QR factorization of matrix <paramref name="A"/> of size <paramref name="m"/>×<paramref name="n"/>, whose upper part will be replaced by the output triangular matrix and lower part (including diagonal) will be replaced by the Householder reflectors; the diagonal elements are stored in <paramref name="diag"/> which shall have length ≥ <c>max(<paramref name="n"/> - 1, min(m, n))</c>.
	/// </summary>
	public static void QrFactorize<T>(int m, int n, T* A, int ld, T* diag) where T : unmanaged, IFloatingPoint<T>
	{
		// reduce to triangular by Householder reflect from the first column
		int mn = Math.Min(m, n);
		for (int i = 0; i < mn; i++)
		{
			// get vector u and store in A[i:,i]
			//tex:$$\vec{u} = \pmatrix{A_{i,i} \pm \|\vec{A}_{i:,i}\| \\ \vec{A}_{i+1:,i}}$$
			T* u = A + (i + i * ld);
			T normSqU = NormSq(u, m - i), normU = T.Sqrt(normSqU);
			normU = T.CopySign(normU, u[0]);
			// get tau and A[i, i]
			//tex: $$\tau = \frac{2}{\|\vec{u}\|^2}$$
			T tau = T.One / (normSqU + u[0] * normU);
			//tex:$$H = I - \tau \vec{u}\vec{u}^T $$
			//tex:$$A_{i,i}' = \vec{H}_{i,i:} \vec{A}_{i:,i} = - \|\vec{A}_{i:,i}\|$$
			u[0] += normU;
			diag[i] = -normU;
			Scale(u, T.Sqrt(tau), m - i);
			// get p and store temporarily in diag[(i+1)..]
			//tex:$\vec{p}^T = \vec{u}^T A_{i:,i:}$
			VecMulMat(u, A + (i + (i + 1) * ld), ld, diag + (i + 1), m - i, n - i - 1);
			//tex:$A_{i:,i:} = H A_{i:,i:} = A_{i:,i:} - \tau \vec{u} \vec{p}^T$
			Rank1UpdateNeg(A + (i + (i + 1) * ld), ld, u, diag + (i + 1), m - i, n - i - 1);
		}
	}

	/// <summary>
	/// Generate the Q matrix from the outputs of <see cref="QrFactorize{T}(int, int, T*, int, T*)"/>. The <paramref name="work"/> shall have length ≥ <paramref name="m"/>. Only the columns from <paramref name="colLeft"/> to <paramref name="colRight"/>(exclusive) of <paramref name="A"/> are modified to the result Q matrix.
	/// </summary>
	public static void QrGenerateQ<T>(int m, int n, int colLeft, int colRight, T* A, int ld, T* work) where T : unmanaged, IFloatingPoint<T>
	{
		// generate Q starting from the last column that stores vector u
		int mn = Math.Min(m, n) - 1;
		for (int i = mn; i >= 0; i--)
		{
			if (i >= colRight)
				continue;
			int len = m - i;
			// get u from A's column i and copy to work space
			T* u = work, Aii = A + (i + i * ld);
			Unsafe.CopyBlockUnaligned(work, Aii, (uint)(len * sizeof(T)));
			// prepare matrix A[i.., i..]
			if (m > n && i == mn)
			{   // H_n for full-sized Q, all fill with identity matrix
				for (int j = i; j < m; j++)
				{
					if (j < colLeft || j >= colRight)
						continue;
					Unsafe.InitBlockUnaligned(A + (j + 1 + j * ld), 0, (uint)((len - 1) * sizeof(T)));
					A[j + j * ld] = T.One;
				}
			}
			else
			{   // only fill the first row and column of A[i.., i..] for this iteration
				if (i >= colLeft)
				{
					Aii[0] = T.One;
					for (int j = i + 1; j < m; j++)
					{
						A[j + i * ld] = T.Zero;
						if (j >= colLeft && j < colRight)
							A[i + j * ld] = T.Zero;
					}
				}
				else
				{
					for (int j = Math.Max(i + 1, colLeft); j < m && j < colRight; j++)
					{
						A[i + j * ld] = T.Zero;
					}
				}
			}
			// update Householder reflectors' product stored in A[i.., i..]
			//tex:$H_{(i)} = H_{(i-1)} - \tau \vec{u}_{(i)}\vec{u}_{(i)}^T H_{(i-1)}$
			for (int j = Math.Max(i, colLeft); j < m && j < colRight; j++)
			{
				T dot = Dot(u, A + (i + j * ld), len);
				AddScaled(A + (i + j * ld), u, -dot, len);
			}
		}
	}

	/// <summary>
	/// Compute the multiplication of Q matrix's (conjugate) transpose and <paramref name="B"/> with output of <see cref="QrFactorize{T}(int, int, T*, int, T*)"/>. <paramref name="B"/> is of size <paramref name="m"/>×<paramref name="nrhs"/> where <paramref name="A"/> is the output of <see cref="QrFactorize{T}(int, int, T*, int, T*)"/>.
	/// </summary>
	public static void QrQtMultiply<T>(int m, int n, int nrhs, T* A, int lda, T* B, int ldb) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			//tex: compute $H_{(i)} B = B - \tau \vec{u}_{(i)}\vec{u}_{(i)}^T B$
			for (int j = 0; j < nrhs; j++)
			{
				T dot = Dot(A + (i + i * lda), B + (i + j * ldb), m - i);
				AddScaled(B + (i + j * ldb), A + (i + i * lda), -dot, m - i);
			}
		}
	}

	/// <summary>
	/// Solve a set of linear equations <c><paramref name="A"/> * X == <paramref name="B"/></c> where <paramref name="A"/> is the output of <see cref="QrFactorize{T}(int, int, T*, int, T*)"/> (together with <paramref name="diag"/>) is an upper triangular matrix and <paramref name="B"/> of size <paramref name="n"/>×<paramref name="nrhs"/> is the right hand side vectors which will be replaced by the solutions <c>X</c>.
	/// </summary>
	public static void QrLinearSolve<T>(int n, int nrhs, T* A, T* diag, int lda, T* B, int ldb) where T : unmanaged, IFloatingPoint<T>
	{
		if (nrhs > 4)
		{
			InPlaceTranspose(A, lda, n);
			for (int k = 0; k < nrhs; k++)
			{
				// back substitution solve
				T* b = B + k * ldb;
				b[n - 1] /= diag[n - 1];
				for (int i = n - 2; i >= 0; i--)
				{
					int i1 = i + 1;
					T dot = Dot(A + (i1 + i * lda), b + i1, n - i1);
					b[i] = (b[i] - dot) / diag[i];
				}
			}
			InPlaceTranspose(A, lda, n);
		}
		else
		{   // direct access by row, suitable for small number of right hand sides
			for (int k = 0; k < nrhs; k++)
			{
				// back substitution solve
				T* b = B + k * ldb;
				b[n - 1] /= diag[n - 1];
				for (int i = n - 2; i >= 0; i--)
				{
					int i1 = i + 1;
					T dot = T.Zero;
					for (int j = i1; j < n; j++)
						dot += b[j] * A[j * lda + i];
					b[i] = (b[i] - dot) / diag[i];
				}
			}
		}
	}
	#endregion

	#region symmetric eigen
	/// <summary>
	/// Reduce a symmetric matrix <paramref name="A"/> to a tridiagonal form stored as <paramref name="diag"/> and <paramref name="offDiag"/>[1..] where <paramref name="A"/> will be replaced by the unary transformation matrix at exit using Householder reflections.
	/// </summary>
	public static void SymmetricMatrixToTridiagonal<T>(int n, T* A, int ld, T* diag, T* offDiag) where T : unmanaged, IFloatingPoint<T>
	{
		T two = T.One + T.One;
		// reduce to tridiagonal by Householder reflect from the last column
		for (int i = 0; i < n - 2; i++)
		{
			int len = n - i - 1;
			// get Householder reflector and store in A's column i
			// Householder reflector u is generated from A[i+1..,i]
			T* u = A + (i + 1 + i * ld);
			T normSqrU = NormSq(u, len), normU = T.Sqrt(normSqrU);
			normU = T.CopySign(normU, u[0]);
			// get tau and store in A[i, i+1]
			//tex: $\tau = {2}/{\|\vec{u}\|^2}$, $H = I - \tau \vec{u}\vec{u}^T$
			T tau = T.One / (normSqrU + u[0] * normU);
			u[0] += normU;
			A[i + (i + 1) * ld] = tau;
			// get A[i.., i..]
			//tex:$\vec{p} = \tau A_{i+1:,i+1:} \vec{u}$ and store in diag[..i]
			VecMulScaledMat(A + (i + 1 + (i + 1) * ld), ld, tau, u, diag, len, len);
			//tex:$$\vec{p}=\vec{p}-\frac{\tau\vec{u}\cdot\vec{p}}{2}\vec{u}$$
			T k = Dot(u, diag, len) * tau / two;
			AddScaled(diag, u, -k, len);
			//tex:$A_{i+1:,i+1:} = A_{i+1:,i+1:} - \vec{q}\vec{u}^T - \vec{u}\vec{q}^T$
			SymRank2UpdateNeg(A + (i + 1 + (i + 1) * ld), ld, diag, u, len);
			// get beta
			//tex:$$\beta = \mp\|\vec{A}_{i+1:,i}\|$$
			offDiag[i + 1] = -normU;
		}
		// get last off-diagonal
		offDiag[n - 1] = A[n - 1 + (n - 2) * ld];
		// reconstruct unary transformation matrix
		diag[n - 1] = A[n - 1 + (n - 1) * ld];
		diag[n - 2] = A[n - 2 + (n - 2) * ld];
		A[n - 1 + (n - 1) * ld] = A[n - 2 + (n - 2) * ld] = T.One;
		A[n - 2 + (n - 1) * ld] = A[n - 1 + (n - 2) * ld] = T.Zero;
		for (int i = n - 3; i >= 0; i--)
		{
			// get tau and vector u
			int len = n - i - 1;
			T tau = A[i + (i + 1) * ld];
			T* u = A + (i + 1 + i * ld);
			// update Householder reflectors' product stored in A[0..i, 0..i]
			//tex:$Q = Q - \tau \vec{u}_{(i)}\vec{u}_{(i)}^T Q$
			for (int j = i + 1; j < n; j++)
			{
				T dot = Dot(u, A + (i + 1 + j * ld), len);
				AddScaled(A + (i + 1 + j * ld), u, -tau * dot, len);
			}
			// set diag and reset last row and column of A[..i, ..i] for next iteration
			diag[i] = A[i + i * ld];
			A[i + i * ld] = T.One;
			for (int j = i + 1; j < n; j++)
				A[j + i * ld] = A[i + j * ld] = T.Zero;
		}
	}

	/// <summary>
	/// Compute the eigenvalues and eigenvectors of a symmetric tridiagonal matrix represented by <paramref name="diag"/> and <paramref name="offDiag"/> where <paramref name="diag"/> will be replaced by the eigenvalues at exit. The <paramref name="eigenvectors"/> contains the original unary matrix to be multiplied in-place.
	/// </summary>
	public static bool SymmetricTridiagonalEigensolve<T>(int n, T* diag, T* offDiag, T* eigenvectors, int eigvecLD) where T : unmanaged, IFloatingPoint<T>
	{
		// constants
		T half = T.One / (T.One + T.One), two = T.One + T.One, four = T.One + T.One + T.One + T.One;
		offDiag[0] = T.Zero; // off-diag is advanced by 1

		// loop from the eigenvalue at right-bottom to the one at top-left
		for (int k = n - 1; k > 0; k--)
		{
			int iter = 0, i;
		RESTART_EIGVAL:
			for (i = k - 1; i >= 0; i--)
			{
				T d = T.Abs(diag[i]) + T.Abs(diag[i + 1]);
				// look for a single small sub-diagonal element which indicates convergence of one eigenvalue
				if (T.Abs(offDiag[i + 1]) + d == d)
					break;
			}
			if (i == k - 1)
			{   // eigenvalue converged
				continue;
			}
			if (i < 0)
				i = 0;
			// now, A[i..k, i..k] (inclusive k) is tridiagonal in machine precision
			// perform QR with implicit shift
			if (iter++ == 30)
			{   // too many iterations for one eigenvalue, there may be errors
				return false;
			}
			// get eigenvalue shift
			//tex: $s = \text{argmin}_x{|d_k - x|}$ where $x \in \text{eigval}\pmatrix{d_{k-1} & e_{k-1} \\ e_{k-1} & d_k}$
			T s;
			{
				T dSub = diag[k - 1] - diag[k], dAdd = diag[k - 1] + diag[k];
				T sqrt = T.Sqrt(dSub * dSub + four * offDiag[k] * offDiag[k]);
				T s1 = half * (dAdd - sqrt);
				T s2 = half * (dAdd + sqrt);
				if (T.Abs(s1 - diag[k]) <= T.Abs(s2 - diag[k]))
					s = s1;
				else
					s = s2;
			}
			// Householder reflect from the first column
			T c = default;
			for (int j = i; j < k; j++)
			{
				// get Householder reflector matrix [h1, h3; h3, h2]
				//tex:$\gamma=\beta_{j-1}^2+c^2\pm\beta_{j-1}\sqrt{\beta_{j-1}^2+c^2}$, $h_1=-h_2={c^2}/{\gamma}-1$ and $h_3=-{c\left(\beta_{j-1}\pm\sqrt{\beta_{j-1}^2+c^2}\right)}/{\gamma}$
				T b, norm, normSq, γ, h1, h2, h3;
				if (j == i)
				{
					b = diag[i] - s;
					normSq = b * b + offDiag[i + 1] * offDiag[i + 1];
					norm = T.Sqrt(normSq);
					c = offDiag[i + 1];
				}
				else
				{
					b = offDiag[j];
					normSq = c * c + offDiag[j] * offDiag[j];
					norm = T.Sqrt(normSq);
				}
				γ = normSq + T.Abs(b) * norm;
				h3 = -c * (b + T.CopySign(norm, b)) / γ;
				h1 = c * c / γ - T.One; h2 = -h1;
				// update diag and off-diag and c
				//tex:$$\left[\begin{matrix}\beta_{j-1}&\alpha_j&\beta_j&c\\0&\beta_j&\alpha_{j+1}&\beta_{j+1}\\\end{matrix}\right]\gets\left[\begin{matrix}h_1&h_3\\h_3&h_2\\\end{matrix}\right]\left[\begin{matrix}\beta_{j-1}&\alpha_j&\beta_j&0\\c&\beta_j&\alpha_{j+1}&\beta_{j+1}\\\end{matrix}\right]$$
				//$$\left[\begin{matrix}\alpha_j&\beta_j\\\beta_j&\alpha_{j+1}\\\end{matrix}\right]\gets\left[\begin{matrix}\alpha_j&\beta_j\\\beta_j&\alpha_{j+1}\\\end{matrix}\right]\left[\begin{matrix}h_1&h_3\\h_3&h_2\\\end{matrix}\right]$$
				offDiag[j] = h1 * offDiag[j] + h3 * c;
				if (j + 2 < n)
					(offDiag[j + 2], c) = (h2 * offDiag[j + 2], h3 * offDiag[j + 2]);
				(diag[j], diag[j + 1], offDiag[j + 1]) =
					(
					h1 * h1 * diag[j] + h3 * h3 * diag[j + 1] + two * h1 * h3 * offDiag[j + 1],
					h3 * h3 * diag[j] + h2 * h2 * diag[j + 1] + two * h2 * h3 * offDiag[j + 1],
					h3 * (h1 * diag[j] + h2 * diag[j + 1]) + offDiag[j + 1] * (h1 * h2 + h3 * h3)
					);
				// update eigenvectors
				//tex:$$U_{:,j:j+1}\gets U_{:,j:j+1}\left[\begin{matrix}h_1&h_3\\h_3&h_2\\\end{matrix}\right]$$
				if (eigenvectors == null)
					continue;
				Householder2x2<T> reflector = new(h2, h3);
				reflector.ColumnUpdate(eigenvectors + j * eigvecLD, eigvecLD, n);
			}
			goto RESTART_EIGVAL;
		}
		return true;
	}
	#endregion

	#region general eigen
	/// <summary>
	/// Reduce the given matrix <paramref name="A"/> of size <paramref name="n"/>×<paramref name="n"/> to a Hessenberg matrix in-place by a unary transformation which will be stored in <paramref name="Q"/>. If <paramref name="Q"/> is not required, then <paramref name="ldq"/> shall be 0 and <paramref name="Q"/> shall be a workspace with size ≥<c>3 * <paramref name="n"/></c>.
	/// </summary>
	public static void MatrixToHessenberg<T>(int n, T* A, int lda, T* Q, int ldq) where T : unmanaged, IFloatingPoint<T>
	{
		T two = T.One + T.One;
		// reduce to Hessenberg form by Householder reflect from the last column
		for (int i = 0; i < n - 2; i++)
		{
			int len = n - i - 1;
			// get Householder reflector and store in A's column i
			// Householder reflector u is generated from A[i+1..,i]
			T* u = ldq == 0 ? Q + 1 : Q + (1 + i * ldq);
			Unsafe.CopyBlockUnaligned(u, A + (i + 1 + i * lda), (uint)(len * sizeof(T)));
			T normSqrU = NormSq(u, len), normU = T.Sqrt(normSqrU);
			normU = T.CopySign(normU, u[0]);
			// get tau and
			//tex: $\tau = {2}/{\|\vec{u}\|^2}$, $H = I - \tau \vec{u}\vec{u}^T$
			T tau = T.One / (normSqrU + u[0] * normU);
			u[0] += normU;
			// get transformed A
			//tex:$A \leftarrow H A H = (A - \tau A\vec{u}\vec{u}^T - \tau \vec{u}\vec{u}^T A + \tau^2 \vec{u}\vec{u}^T A \vec{u}\vec{u}^T)$
			//tex: let $\vec{p} = \tau A \vec{u}$ and store in Q[.., i + 1], and
			//let $\vec{q}^T = \tau \vec{u}^T A$ and store in Q[.., i + 2]
			T* p = ldq == 0 ? Q + n : Q + (i + 1) * ldq;
			T* q = ldq == 0 ? Q + 2 * n : Q + (i + 2) * ldq;
			MatMulScaledVec(A + (i + 1) * lda, lda, tau, u, p, n, len);
			VecMulScaledMat(A + (i + 1), lda, tau, u, q, n, len);
			//tex:$A_{i:,i:} = A_{i:,i:} - \vec{p}\vec{u}^T - \vec{u}\vec{q}^T (I - \tau \vec{u}\vec{u}^T)$
			//tex:let $\vec{q} = \vec{q} - \tau (\vec{u}\cdot\vec{q}) \vec{u}$ then $A = A - \vec{p}\vec{u}^T - \vec{u}\vec{q}^T$
			AddScaled(q + (i + 1), u/*.Conjugate*/, -tau * Dot(u, q + (i + 1), len), len);
			Rank1UpdateNeg(A + (i + 1) * lda, lda, p, u, n, len);
			Rank1UpdateNeg(A + (i + 1), lda, u, q, len, n);
			// store tau
			u[-1] = tau;
			Unsafe.InitBlockUnaligned(A + (i + 2 + i * lda), 0, (uint)((n - i - 2) * sizeof(T)));
		}
		if (ldq == 0)
			return;
		// reconstruct unary transformation matrix
		Q[n - 1 + (n - 1) * ldq] = Q[n - 2 + (n - 2) * ldq] = T.One;
		Q[n - 2 + (n - 1) * ldq] = Q[n - 1 + (n - 2) * ldq] = T.Zero;
		for (int i = n - 3; i >= 0; i--)
		{
			// get tau and vector u
			int len = n - i - 1;
			T* u = ldq == 0 ? Q + 1 : Q + (1 + i * ldq);
			T tau = u[-1];
			// update Householder reflectors' product stored in Q[..i, ..i]
			//tex:$Q = Q - \tau \vec{u}_{(i)}\vec{u}_{(i)}^T Q$
			for (int j = i + 1; j < n; j++)
			{
				T dot = Dot(u, Q + (i + 1 + j * ldq), len);
				AddScaled(Q + (i + 1 + j * ldq), u, -tau * dot, len);
			}
			// set diag and reset last row and column of Q[..i, ..i] for next iteration
			Q[i + i * ldq] = T.One;
			for (int j = i + 1; j < n; j++)
				Q[j + i * ldq] = Q[i + j * ldq] = T.Zero;
		}
	}

	private static void ToStandardSchurForm<T>(int n, T* A, int lda, T* Q, int ldq, int i) where T : unmanaged, IFloatingPoint<T>
	{
		// constants
		T two = T.One + T.One, half = T.One / two, halfSqrt2 = T.Sqrt(two) * half, four = two + two;
		int k = i + 1;
		// transform to standard Schur form
		//tex:$A_{i:k,:}\gets HA_{i:k,:}$; $A_{:,i:k}\gets A_{:,i:k}H$; $U_{:,i:k}\gets U_{:,i:k}H$,
		//where $h_2,h_3=\frac{\sqrt2}{2}\sqrt{1\pm\frac{\left|b+c\right|}{\sqrt{\left(b+c\right)^2+\left(a-d\right)^2}}}$ for complex;
		//or $h_2,h_3=\sqrt{\frac{2\alpha\left(b+c\right)+\left(a-d\right)\left[a-d\pm\sqrt{4bc+\left(a-d\right)^2}\right]}{2\left[\left(b+c\right)^2+\left(a-d\right)^2\right]}}$, $\alpha = b, c$ for real pair
		T h2, h3;
		T ad = A[i + i * lda] - A[k + k * lda];
		T bc = A[i + k * lda] + A[k + i * lda];
		T b_c = A[i + k * lda] * A[k + i * lda];
		T delta = ad * ad + four * b_c;
		T nrm = bc * bc + ad * ad;
		if (delta >= T.Zero)
		{
			delta = T.Sqrt(delta);
			nrm *= two;
			h2 = (two * A[i + k * lda] * bc + ad * (ad + delta)) / nrm;
			h3 = (two * A[k + i * lda] * bc + ad * (ad - delta)) / nrm;
			h2 = T.Sqrt(h2); h3 = T.Sqrt(h3);
		}
		else
		{
			nrm = T.Sqrt(nrm);
			if (nrm == T.Abs(bc))
				return;
			h2 = halfSqrt2 * T.Sqrt(T.One + bc / nrm);
			h3 = halfSqrt2 * T.Sqrt(T.One - bc / nrm);
			if (ad < T.Zero)
			{
				(h2, h3) = (h3, h2);
			}
		}
		Householder2x2<T> householder = new(h2, h3);
		householder.ColumnUpdate(A + i * lda, lda, k + 1);
		householder.RowUpdate(A + (i + i * lda), lda, n - i);
		householder.ColumnUpdate(Q + i * ldq, ldq, n);
		if (delta >= T.Zero)
			A[k + i * lda] = T.Zero;
	}

	/// <summary>
	/// Compute the Schur factorization of given upper Hessenberg matrix <paramref name="A"/> and corresponding unary matrix <paramref name="Q"/> to transformation a general matrix to <paramref name="A"/>. After return, <paramref name="A"/> will be replaced by its Schur form, <paramref name="Q"/> will multiply the Schur vectors in-place and <c><paramref name="wr"/> + √(-1) * <paramref name="wi"/></c> will be the eigenvalues.
	/// </summary>
	public static bool HessenbergSchurFactorize<T>(int n, T* A, int lda, T* Q, int ldq, T* wr, T* wi) where T : unmanaged, IFloatingPoint<T>
	{
		// constants
		T two = T.One + T.One, half = T.One / two, halfSqrt2 = T.Sqrt(two) * half, four = two + two;
		Unsafe.InitBlockUnaligned(wi, 0, (uint)(n * sizeof(T)));
		wr[0] = T.NaN;

		// main loop to compute eigenvalues from bottom to top
		for (int k = n - 1; k > 0; k--)
		{
			int iter = 0, i;
		RESTART_EIGVAL:
			// look for small sub-diagonal to split matrix
			for (i = k; i > 0; i--)
			{
				T d = T.Abs(A[i + i * lda]) + T.Abs(A[i - 1 + (i - 1) * lda]);
				if (T.Abs(A[i + (i - 1) * lda]) + d == d)
				{
					A[i + (i - 1) * lda] = T.Zero;
					break;
				}
			}
			// one eigenvalue converged
			if (i == k)
			{
				wr[k] = A[i + i * lda];
				continue;
			}
			// two eigenvalues converged
			if (i == k - 1)
			{
				ToStandardSchurForm(n, A, lda, Q, ldq, i);
				T re = A[i + i * lda];
				wr[i] = wr[k] = re;
				re *= re;
				T im = -A[i + k * lda] * A[k + i * lda];
				if (re + im != re)
				{
					wi[i] = T.Sqrt(im);
					wi[k] = -wi[i];
				}
				else
				{
					wr[k] = A[k + k * lda];
				}
				k--;
				continue;
			}
			// no eigenvalue converged
			if (i < 0)
				i = 0;
			if (iter++ == 30)
			{   // too many iterations, there may be errors
				return false;
			}
			// continue implicit QR iteration for A[i..k, i..k] (inclusive)
			for (int j = i; j < k; j++)
			{
				Householder3<T> householder = default;
				if (j == i)
				{
					//tex:$$\vec{v}\gets \begin{pmatrix}{\left[\left(a_{k,k}-a_{i,i}\right)\left(a_{k-1,k-1}-a_{i,i}\right)-a_{k-1,k}a_{k,k-1}\right]}/{a_{i+1,i}}+a_{i,i+1} \\ a_{i,i}+a_{i+1,i+1}-\left(a_{k-1,k-1}+a_{k,k}\right) \\ a_{i+2,i+1} \end{pmatrix}$$
					T akk = A[k + k * lda], aii = A[i + i * lda],
					  ak1k1 = A[k - 1 + (k - 1) * lda], ai1i1 = A[i + 1 + (i + 1) * lda],
					  ak1k = A[k - 1 + k * lda], akk1 = A[k + (k - 1) * lda],
					  ai1i = A[i + 1 + i * lda], aii1 = A[i + (i + 1) * lda];
					householder = new
					(
						((akk - aii) * (ak1k1 - aii) - ak1k * akk1) / ai1i + aii1,
						aii + ai1i1 - (ak1k1 + akk),
						A[i + 2 + (i + 1) * lda]
					);
				}
				else
				{
					//tex:$\vec{v}\gets{\vec{a}}_{j:j+2,j-1}$
					householder = new(A[j + (j - 1) * lda], A[j + 1 + (j - 1) * lda], A[j + 2 + (j - 1) * lda]);
				}
				//tex:$H\gets I - 2\vec{v}\vec{v}^T / \|v\|^2 = \begin{pmatrix}h_1&h_4&h_5\\h_4&h_2&h_6\\h_5&h_6&h_3\end{pmatrix}$
				//tex:$A_{j:j+2,:}\gets HA_{j:j+2,:}$; $A_{:,j:j+2}\gets A_{:,j:j+2}H$; $U_{:,j:j+2}\gets U_{:,j:j+2}H$
				int colFree = j == i ? i : j - 1;
				if (j == k - 1)
				{
					Householder2<T> householder2 = new(A[j + (j - 1) * lda], A[j + 1 + (j - 1) * lda]);
					var reflector = householder2.ToReflectionMatrix();
					reflector.ColumnUpdate(A + (j * lda), lda, Math.Min(j + 4, n));
					reflector.RowUpdate(A + (j + colFree * lda), lda, n - colFree);
					if (Q != null)
						reflector.ColumnUpdate(Q + (j * ldq), ldq, n);
					A[j + 1 + (j - 1) * lda] = T.Zero;
				}
				else
				{
					var reflector = householder.ToReflectionMatrix();
					reflector.ColumnUpdate(A + (j * lda), lda, Math.Min(j + 4, n));
					reflector.RowUpdate(A + (j + colFree * lda), lda, n - colFree);
					if (Q != null)
						reflector.ColumnUpdate(Q + (j * ldq), ldq, n);
					if (j != i)
						A[j + 1 + (j - 1) * lda] = A[j + 2 + (j - 1) * lda] = T.Zero;
				}
			}
			// restart iteration for this eigenvalue
			goto RESTART_EIGVAL;
		}
		// get first eigenvalue and return
		if (T.IsNaN(wr[0]))
			wr[0] = A[0];
		return true;
	}

	/// <summary>
	/// Sort the Schur factorization result (possibly generated from <see cref="HessenbergSchurFactorize{T}(int, T*, int, T*, int, T*, T*)"/>) by the given <paramref name="keys"/> corresponding to the eigenvalues <c><paramref name="wr"/> + √(-1) * <paramref name="wi"/></c>. All of the inputs will be sorted by <paramref name="keys"/> stably if <paramref name="keys"/> are same for eigenvalues with same value or conjugate.
	/// </summary>
	public static void ReorderSchurForm<T, TKey>(int n, T* A, int lda, T* Q, int ldq, T* wr, T* wi, Span<TKey> keys) where T : unmanaged, IFloatingPoint<T> where TKey : IComparisonOperators<TKey, TKey>
	{
		// work spaces
		T* vecX = stackalloc T[4], work = stackalloc T[4];

		// bubble sort outer loop
		for (int k = 1; k < n; k++)
		{
			// bubble sort inner loop
			for (int i = n - 1; i >= k;)
			{
				// get size
				int q = A[i + (i - 1) * lda] != T.Zero ? 2 : 1;
				int p = i >= q + 1 && A[i - q + (i - q - 1) * lda] != T.Zero ? 2 : 1;
				if (keys[i] >= keys[i - q])
				{
					i -= q;
					continue;
				}
				// 4 cases
				if (p == 1 && q == 1)
				{
					int j = i - 1;
					// swap simple ones
					(keys[i], keys[j]) = (keys[j], keys[i]);
					(wr[i], wr[j]) = (wr[j], wr[i]);
					(wi[i], wi[j]) = (wi[j], wi[i]);
					// set matrix T
					T alpha1 = A[j + j * lda];
					T alpha2 = A[i + i * lda];
					T t = A[j + i * lda];
					// solve X
					T x = t / (alpha1 - alpha2);
					// QR factorize
					var reflector = new Householder2<T>(-x, T.One).ToReflectionMatrix();
					// swap blocks
					reflector.ColumnUpdate(A + j * lda, lda, i + 1);
					reflector.RowUpdate(A + (j + j * lda), lda, n - j);
					reflector.ColumnUpdate(Q + j * lda, ldq, n);
					A[i + j * lda] = T.Zero;
				}
				else if (p == 2 && q == 1)
				{
					int jj = i - 2, j = i - 1;
					// swap simple ones
					(keys[i], keys[jj]) = (keys[jj], keys[i]);
					(wr[i], wr[jj]) = (wr[jj], wr[i]);
					(wi[jj], wi[j], wi[i]) = (wi[i], wi[jj], wi[j]);
					// set matrix T
					T alpha1 = A[jj + jj * lda], delta1 = A[j + j * lda], beta1 = A[jj + j * lda], gamma1 = A[j + jj * lda];
					T t1 = A[jj + i * lda], t3 = A[j + i * lda];
					T alpha2 = A[i + i * lda];
					// solve X
					//tex:$$x_1\gets \frac{t_1 \left(\alpha _2-\delta _1\right)+\beta _1 t_3}{\left(\alpha _1-\alpha _2\right) \left(\alpha _2-\delta _1\right)+\beta _1 \gamma _1},x_2\gets \frac{\left(\alpha _2-\alpha _1\right) t_3+\gamma _1 t_1}{\left(\alpha _1-\alpha _2\right) \left(\alpha _2-\delta _1\right)+\beta _1 \gamma _1}$$
					T denom = T.One / ((alpha1 - alpha2) * (alpha2 - delta1) + beta1 * gamma1);
					T x1 = ((alpha2 - delta1) * t1 + beta1 * t3) * denom;
					T x2 = ((alpha2 - alpha1) * t3 + gamma1 * t1) * denom;
					// QR factorize
					var reflector = new Householder3<T>(-x1, -x2, T.One).ToReflectionMatrix();
					// swap blocks
					reflector.ColumnUpdate(A + jj * lda, lda, i + 1);
					reflector.RowUpdate(A + (jj + jj * lda), lda, n - jj);
					reflector.ColumnUpdate(Q + jj * lda, ldq, n);
					A[j + jj * lda] = A[i + jj * lda] = T.Zero;
				}
				else if (p == 1 && q == 2)
				{
					int j = i - 2, ii = i - 1;
					// swap simple ones
					(keys[i], keys[j]) = (keys[j], keys[i]);
					(wr[i], wr[j]) = (wr[j], wr[i]);
					(wi[j], wi[ii], wi[i]) = (wi[ii], wi[i], wi[j]);
					// set matrix T
					T alpha1 = A[j + j * lda];
					T t3 = A[j + ii * lda], t4 = A[j + i * lda];
					T alpha2 = A[ii + ii * lda], delta2 = A[i + i * lda], beta2 = A[ii + i * lda], gamma2 = A[i + ii * lda];
					// solve X
					//tex:$$x_1 \gets \frac{t_3 \left(\delta _2-\alpha _1\right)-\gamma _2 t_4}{\beta _2 \gamma _2-\left(\alpha _1-\alpha _2\right) \left(\alpha _1-\delta _2\right)}, x_2 \gets \frac{\left(\alpha _1-\alpha _2\right) t_4+\beta _2 t_3}{\left(\alpha _1-\alpha _2\right) \left(\alpha _1-\delta _2\right)-\beta _2 \gamma _2}$$
					T denom = T.One / ((alpha1 - alpha2) * (alpha1 - delta2) - beta2 * gamma2);
					T x1 = (gamma2 * t4 + t3 * (alpha1 - delta2)) * denom;
					T x2 = (beta2 * t3 + t4 * (alpha1 - alpha2)) * denom;
					// QR factorize
					Orthogonal3x3<T> reflector = new(-x1, T.One, T.Zero, -x2, T.Zero, T.One, default, default, default);
					QrFactorize(3, 2, (T*)&reflector, 3, work);
					QrGenerateQ(3, 2, 0, 3, (T*)&reflector, 3, work);
					// swap blocks
					reflector.ColumnUpdate(A + j * lda, lda, i + 1);
					reflector.RowUpdate(A + (j + j * lda), lda, n - j);
					reflector.ColumnUpdate(Q + j * lda, ldq, n);
					A[i + j * lda] = A[i + ii * lda] = T.Zero;
				}
				else //if (p == 2 && q == 2)
				{
					int jj = i - 3, j = i - 2, ii = i - 1;
					// swap simple ones
					(keys[ii], keys[i], keys[jj], keys[j]) = (keys[jj], keys[j], keys[ii], keys[i]);
					(wr[ii], wr[i], wr[jj], wr[j]) = (wr[jj], wr[j], wr[ii], wr[i]);
					(wi[ii], wi[i], wi[jj], wi[j]) = (wi[jj], wi[j], wi[ii], wi[i]);
					// set matrix T
					//tex:$$T \gets \begin{pmatrix} \alpha _1-\alpha _2 & -\gamma _2 & \beta _1 & 0 \\ -\beta _2 & \alpha _1-\delta _2 & 0 & \beta _1 \\ \gamma _1 & 0 & \delta _1-\alpha _2 & -\gamma _2 \\ 0 & \gamma _1 & -\beta _2 & \delta _1-\delta _2 \end{pmatrix}$$
					T alpha1 = A[jj + jj * lda], delta1 = A[j + j * lda], beta1 = A[jj + j * lda], gamma1 = A[j + jj * lda];
					T alpha2 = A[ii + ii * lda], delta2 = A[i + i * lda], beta2 = A[ii + i * lda], gamma2 = A[i + ii * lda];
					Orthogonal4x4<T> reflector = default;
					T* matT = (T*)&reflector;
					matT[0] = alpha1 - alpha2; matT[1] = -beta2; matT[2] = gamma1;
					matT[4] = -gamma2; matT[5] = alpha1 - delta2; matT[7] = gamma1;
					matT[8] = beta1; matT[10] = delta1 - alpha2; matT[11] = -beta2;
					matT[13] = beta1; matT[14] = -gamma2; matT[15] = delta1 - delta2;
					vecX[0] = A[jj + ii * lda]; vecX[1] = A[jj + i * lda];
					vecX[2] = A[j + ii * lda]; vecX[3] = A[j + i * lda];
					// solve X
					QrFactorize(4, 4, matT, 4, work);
					QrQtMultiply(4, 4, 1, matT, 4, vecX, 4);
					QrLinearSolve(4, 1, matT, work, 4, vecX, 4);
					// QR factorize
					reflector = default;
					matT[0] = -vecX[0]; matT[1] = -vecX[2]; matT[4] = -vecX[1]; matT[5] = -vecX[3];
					matT[2] = T.One; matT[7] = T.One;
					QrFactorize(4, 2, matT, 4, work);
					QrGenerateQ(4, 2, 0, 4, matT, 4, work);
					// swap blocks
					reflector.ColumnUpdate(A + jj * lda, lda, i + 1);
					reflector.RowUpdate(A + (jj + jj * lda), lda, n - jj);
					reflector.ColumnUpdate(Q + jj * lda, ldq, n);
					A[ii + jj * lda] = A[ii + j * lda] = A[i + jj * lda] = A[i + j * lda] = T.Zero;
				}
				i -= p;
			}
			// to standard Schur form if necessary
			if (A[k + (k - 1) * lda] != T.Zero)
				ToStandardSchurForm(n, A, lda, Q, ldq, k - 1);
		}
	}

	/// <summary>
	/// Compute the right eigenvectors of given Schur form <paramref name="A"/> multiplied by <paramref name="Q"/> and store the result in <paramref name="V"/> (cannot overlap with the former two matrices). <paramref name="work"/> shall have size ≥ 9 * <paramref name="n"/>.
	/// </summary>
	public static void SchurFormEigensolve<T>(int n, T* A, int lda, T* Q, int ldq, T* wr, T* wi, T* V, int ldv, T* work) where T : unmanaged, IFloatingPoint<T>
	{
		// constant
		T criteriaAmplifier = T.ScaleB(T.One, sizeof(T) * 3 / 2), sqrtCriteriaAmplifier = T.Sqrt(criteriaAmplifier);
		// store some diagonals
		T* diag_m1 = work, diag_0 = work + n, diag_p1 = work + 2 * n;
		diag_m1[0] = diag_p1[0] = diag_0[0] = T.Zero;
		for (int i = 0; i < n - 1; i++)
		{
			int ip = i + 1;
			diag_m1[ip] = -A[ip + i * lda];
			diag_p1[ip] = A[i + ip * lda];
			diag_0[ip] = A[i + i * lda];
		}
		// main loop
		for (int k = 0; k < n;)
		{
			T* alpha = work + 3 * n, alphaIm = work + 4 * n;
			T* beta = work + 5 * n, betaIm = work + 6 * n;
			T* temp = work + 7 * n, tempIm = work + 8 * n;
			// find last eigenvalue equals or conjugates
			int l;
			T λ = wr[k];
			bool noComplex = wi[k] == T.Zero;
			if (noComplex)
			{
				// lessen the criteria
				T λAbs = sqrtCriteriaAmplifier * T.Abs(λ);
				for (l = k; l < n; l++)
				{
					T diff = T.Abs(wr[l] - λ);
					if (wi[l] != T.Zero || diff + λAbs != λAbs)
						break;
				}
				alphaIm = betaIm = tempIm = null;
				λ = λAbs;
			}
			else
			{
				// lessen the criteria
				T λAbsSq = criteriaAmplifier * (λ * λ + wi[k] * wi[k]);
				for (l = k; l < n; l++)
				{
					T diff = (wr[l] - λ) * (wr[l] - λ) + (T.Abs(wi[l]) - wi[k]) * (T.Abs(wi[l]) - wi[k]);
					if (wi[l] == T.Zero || diff + λAbsSq != λAbsSq)
						break;
				}
				λ = T.Sqrt(λAbsSq);
			}
			// get columns whose eigenvectors are not 0 and copy to V[..k, k..l]
			//tex:$\mathcal{I}\gets\left\{i\middle|{\vec{a}}_{k:l,i}={\vec{e}}_{i-k+1}\lambda_k,k\le i < l\right\}$
			int zeroColCount = 0;
			for (int i = k; i < l; i++)
			{
				if (noComplex && !AllZeroComparedTo(A + (k + i * lda), i - k, λ))
					continue;
				if (!noComplex && ((i - k) % 2 == 1 || !AllZeroComparedTo(A + (k + i * lda), i - k, λ) || !AllZeroComparedTo(A + (k + (i + 1) * lda), i - k, λ)))
					continue;
				if (k != 0)
				{
					if (noComplex)
						Unsafe.CopyBlockUnaligned(V + ((zeroColCount + k) * ldv), A + (i * lda), (uint)(k * sizeof(T)));
					else
						Unsafe.CopyBlockUnaligned(V + ((zeroColCount + k) * ldv), A + ((i + 1) * lda), (uint)((k + 1) * sizeof(T)));
				}
				zeroColCount++;
			}
			// real end row number for complex
			int kk = noComplex ? k : k + 1;
			// first eigenvectors shortcut
			if (k == 0)
			{
				if (!noComplex)
					zeroColCount *= 2;
				for (int i = 0; i < zeroColCount; i++)
				{
					Unsafe.CopyBlockUnaligned(V + (i * ldv), Q + (i * ldq), (uint)(n * sizeof(T)));
				}
				for (int i = zeroColCount; i < l; i++)
				{
					Unsafe.InitBlockUnaligned(V + (i * ldv), 0, (uint)(n * sizeof(T)));
				}
				if (!noComplex)
				{
					//tex:$$\vec{v} = \left[ \pm \frac{\sqrt{b}}{\sqrt{c-b}},\frac{1}{\sqrt{\left| {b}/{c}\right| +1}} \right]$$
					for (int i = 0; i < zeroColCount; i += 2)
					{
						T b = A[i + (i + 1) * lda], c = A[i + 1 + i * lda];
						T im = T.Sqrt(T.Abs(b / (c - b))), re = T.One / T.Sqrt(T.Abs(b / c) + T.One);
						// store real and imaginary parts in two columns
						Scale(V + i * ldv, im, n);
						Scale(V + (i + 1) * ldv, re, n);
					}
				}
				k = l; continue;
			}
			// get work vector alpha and beta for row reduction
			//tex:$\vec{\beta}\gets-{{\rm \text{diag}}_{-1}{A_{0:k-1,0:k-1}}}/{\left(\text{diag}{A_{0:k-2,0:k-2}}-\lambda_k\right)}$
			//$\vec{\alpha}\gets{1}/{\left(\text{diag}{A_{1:k-1,1:k-1}}+\vec{\beta}\odot{\rm \text{diag}}_1{A_{0:k-1,0:k-1}}-\lambda_k\right)}$
			//$\vec{\beta}\gets\vec{\alpha}\odot\vec{\beta}$
			PointWiseDivideAddScalar(diag_m1, diag_0, kk, -wr[k], -wi[k], beta, betaIm);
			PointWiseMultiplyAddScalar(beta, betaIm, diag_p1, diag_0 + 1, kk, -wr[k], -wi[k], alpha, alphaIm);
			PointWiseInv(alpha, alphaIm, kk, alpha, alphaIm);
			PointWiseMultiply(alpha, alphaIm, beta, betaIm, kk, beta, betaIm);
			// move beta forward for future computation
			for (int i = 0; i < kk - 1; i++)
				beta[i] = beta[i + 1];
			if (betaIm != null)
				for (int i = 0; i < k; i++)
					betaIm[i] = betaIm[i + 1];
			beta[kk - 1] = T.Zero;
			if (betaIm != null)
				betaIm[k] = T.Zero;
			// row reduce V[..k, k..l]
			//tex:$$V_{1:k-1,\mathcal{I}}\gets \text{diag}{\vec{\beta}}\cdot V_{0:k-2,\mathcal{I}}+\text{diag}{\vec{\alpha}}\cdot V_{1:k-1,\mathcal{I}}$$
			//For j = k - 2, ..., 1 Do
			//$$V_{1:j,\mathcal{I}}\gets V_{1:j,\mathcal{I}}-\left({\vec{\beta}}_{1:j}\odot{\vec{a}}_{0:j-1,j+1}+{\vec{\alpha}}_{1:j}\odot{\vec{a}}_{1:j,j+1}\right)\otimes{\vec{v}}_{k-1,\mathcal{I}}$$
			for (int i = 0; i < zeroColCount; i++)
			{
				T* Vre = noComplex ? V + (k + i) * ldv : V + (k + i * 2) * ldv;
				T* Vim = noComplex ? null : V + (k + i * 2 + 1) * ldv;
				PointWiseMultiplyAdd(beta, betaIm, Vre, alpha + 1, alphaIm + 1, Vre + 1, kk - 1, temp + 1, tempIm + 1);
				temp[0] = alpha[0] * Vre[0];
				if (!noComplex)
					tempIm[0] = alphaIm[0] * Vre[0];
				Unsafe.CopyBlockUnaligned(Vre, temp, (uint)(kk * sizeof(T)));
				if (!noComplex)
					Unsafe.CopyBlockUnaligned(Vim, tempIm, (uint)(kk * sizeof(T)));
			}
			for (int j = kk - 1; j > 0; j--)
			{
				PointWiseMultiplyAdd(beta, betaIm, A + j * lda, alpha + 1, alphaIm + 1, A + (1 + j * lda), j - 1, temp + 1, tempIm + 1);
				temp[0] = alpha[0] * A[j * lda];
				if (!noComplex)
					tempIm[0] = alphaIm[0] * A[j * lda];
				for (int i = 0; i < zeroColCount; i++)
				{
					T* Vre = noComplex ? V + (k + i) * ldv : V + (k + i * 2) * ldv;
					T* Vim = noComplex ? null : V + (k + i * 2 + 1) * ldv;
					PointWiseAddScaled(Vre, Vim, temp, tempIm, j, -Vre[j], noComplex ? default : -Vim[j], Vre, Vim);
				}
			}
			// from eigenvectors
			//tex:$V_{k:n,\mathcal{I}}\gets-\left[{\vec{e}}_1,\ldots,{\vec{e}}_{\left|\mathcal{I}\right|}\right]$; 
			//$V_{:,\mathcal{I}}\gets\text{Orthogonalize}\left(V_{:,\mathcal{I}}\right)$; 
			//$V_{:,\mathcal{I}}\gets U\cdot V_{:,\mathcal{I}}$
			for (int i = 0; i < zeroColCount; i++)
			{
				if (noComplex)
				{
					Unsafe.InitBlockUnaligned(V + (k + (k + i) * ldv), 0, (uint)((n - k) * sizeof(T)));
					V[k + i + (k + i) * ldv] = -T.One;
				}
				else
				{
					Unsafe.InitBlockUnaligned(V + (kk + (k + i * 2) * ldv), 0, (uint)((n - kk) * sizeof(T)));
					Unsafe.InitBlockUnaligned(V + (kk + (k + i * 2 + 1) * ldv), 0, (uint)((n - kk) * sizeof(T)));
					V[kk + i * 2 + (k + i * 2) * ldv] = -T.One;
				}
			}
			Orthogonalize(!noComplex, kk + zeroColCount, zeroColCount, V + k * ldv, ldv);
			if (!noComplex)
				zeroColCount *= 2;
			for (int i = 0; i < zeroColCount; i++)
			{
				MatMulVec(Q, ldq, V + (k + i) * ldv, temp, n, k + i + 1);
				Unsafe.CopyBlockUnaligned(V + (k + i) * ldv, temp, (uint)(n * sizeof(T)));
			}
			for (int i = zeroColCount + k; i < l; i++)
			{
				Unsafe.InitBlockUnaligned(V + i * ldv, 0, (uint)(n * sizeof(T)));
			}
			// continue loop
			k = l;
		}
	}
	#endregion
}
