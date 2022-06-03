using System.Numerics;
using System.Runtime.CompilerServices;

using Althea.NativeTypes;

namespace Althea.Backend.CSharp.LinearAlgebra;


internal static unsafe class MatrixSolvers
{
	// TODO: use existing codes
	// Ignore Spelling: \vec \alpha \beta \tau \ldots \langle \rangle \pmatrix \cdot eigval argmin
	#region utilities
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T NormSq<T>(T* vec, int n) where T : unmanaged, IFloatingPoint<T>
	{
		T norm = T.Zero;
		if (!Vector.IsHardwareAccelerated || !typeof(T).IsPrimitive)
			goto SCALAR;
		T* end = vec + n;
		Vector<T> norms = Vector<T>.Zero;
		for (; vec < end; vec += Vector<T>.Count)
		{
			var v = Unsafe.ReadUnaligned<Vector<T>>(vec);
			norms += v * v;
		}
		n = (int)(end - vec);
		norm = Vector.Sum(norms);
	SCALAR:
		for (int i = 0; i < n; i++)
		{
			norm += vec[i] * vec[i];
		}
		return norm;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T Dot<T>(T* x, T* y, int n) where T : unmanaged, IFloatingPoint<T>
	{
		T dot = T.Zero;
		if (!Vector.IsHardwareAccelerated || !typeof(T).IsPrimitive)
			goto SCALAR;
		T* end = x + n;
		Vector<T> dots = Vector<T>.Zero;
		for (; x < end; x += Vector<T>.Count, y += Vector<T>.Count)
		{
			var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
			var yy = Unsafe.ReadUnaligned<Vector<T>>(y);
			dots += xx * yy;
		}
		n = (int)(end - x);
		dot = Vector.Sum(dots);
	SCALAR:
		for (int i = 0; i < n; i++)
			dot += x[i] * y[i];
		return dot;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Scale<T>(T* x, T α, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (!Vector.IsHardwareAccelerated || !typeof(T).IsPrimitive)
			goto SCALAR;
		T* end = x + n;
		Vector<T> scalar = new(α);
		for (; x < end; x += Vector<T>.Count)
		{
			var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
			xx *= scalar;
			Unsafe.WriteUnaligned(x, xx);
		}
		n = (int)(end - x);
	SCALAR:
		for (int i = 0; i < n; i++)
			x[i] = α * x[i];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AddScaled<T>(T* y, T* x, T α, int n) where T : unmanaged, IFloatingPoint<T>
	{
		if (!Vector.IsHardwareAccelerated || !typeof(T).IsPrimitive)
			goto SCALAR;
		T* end = x + n;
		Vector<T> scalar = new(α);
		for (; x < end; x += Vector<T>.Count, y += Vector<T>.Count)
		{
			var xx = Unsafe.ReadUnaligned<Vector<T>>(x);
			var yy = Unsafe.ReadUnaligned<Vector<T>>(y);
			yy += xx * scalar;
			Unsafe.WriteUnaligned(y, yy);
		}
		n = (int)(end - x);
	SCALAR:
		for (int i = 0; i < n; i++)
			y[i] += α * x[i];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VecMulMat<T>(T* x, T* A, int ld, T* output, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			output[i] = Dot(A + i * ld, x, m);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void MatMulScaledVec<T>(T* A, int ld, T α, T* x, T* output, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			output[i] = α * Dot(A + i * ld, x, n);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Rank1UpdateNeg<T>(T* A, int ld, T* x, T* y, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			T yy = y[i];
			AddScaled(A + i * ld, x, -yy, m);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SymRank2UpdateNeg<T>(T* A, int ld, T* x, T* y, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			T xx = x[i], yy = y[i];
			AddScaled(A + i * ld, x, -yy, n);
			AddScaled(A + i * ld, y, -xx, n);
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
	#endregion

	// checked
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
	/// Generate the Q matrix from the outputs of <see cref="QrFactorize{T}(int, int, T*, int, T*)"/>. The <paramref name="work"/> shall have length ≥ <paramref name="m"/>.
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
	/// Reduce a symmetric matrix <paramref name="A"/> to a tridiagonal form stored as <paramref name="diag"/> and <paramref name="offDiag"/> where <paramref name="A"/> will be replaced by the unary transformation matrix at exit using Householder reflections.
	/// </summary>
	public static void SymmetricMatrixToTridiagonal<T>(int n, T* A, int ld, T* diag, T* offDiag) where T : unmanaged, IFloatingPoint<T>
	{
		T two = T.One + T.One;
		// reduce to tridiagonal by Householder reflect from the last column
		for (int i = n - 1; i >= 2; i--)
		{
			int im1 = i - 1;
			// get vector u and store in A's column i
			//tex:$$\vec{u} = \pmatrix{\vec{A}_{0:i-2,i} \\  A_{i-1,i} \pm \|\vec{A}_{0:i-1,i}\|}$$
			T* u = A + i * ld;
			ref T uLast = ref u[im1];
			T normSqrU = NormSq(u, i), normU = T.Sqrt(normSqrU);
			normU = T.CopySign(normU, uLast);
			// get tau and store in A[i, i-1]
			//tex: $$\tau = \frac{2}{\|\vec{u}\|^2}$$
			//tex:$$H = I - \tau \vec{u}\vec{u}^T$$
			T tau = T.One / (normSqrU + uLast * normU);
			uLast += normU;
			A[i + im1 * ld] = tau;
			// get A[0..i, 0..i]
			//tex:$\vec{p} = \tau A \vec{u}$ and store in diag[..i]
			MatMulScaledVec(A, ld, tau, u, diag, i);
			//tex:$$\vec{p}=\vec{p}-\frac{\tau\vec{u}\cdot\vec{p}}{2}\vec{u}$$
			T k = Dot(u, diag, i) * tau / two;
			AddScaled(diag, u, -k, i);
			//tex:$A_{0:i-1,0:i-1} = A_{0:i-1,0:i-1} - \vec{q}\vec{u}^T - \vec{u}\vec{q}^T$
			SymRank2UpdateNeg(A, ld, diag, u, i);
			// get beta
			//tex:$$\beta = -\frac{A_{i-1,i}}{|A_{i-1,i}|}\|A_{0:i-1,i}\|$$
			offDiag[im1] = -normU;
		}
		// get first off-diagonal
		offDiag[0] = A[1];
		// reconstruct unary transformation matrix
		diag[0] = A[0]; diag[1] = A[1 + ld];
		A[0] = A[1 + ld] = T.One;
		A[1] = A[ld] = T.Zero;
		for (int i = 2; i < n; i++)
		{
			// get tau and vector u
			T tau = A[i + (i - 1) * ld];
			T* u = A + i * ld;
			// update Householder reflectors' product stored in row major A[0..i, 0..i]
			//tex:$H_{(i)} = H_{(i-1)} - \tau H_{(i-1)} \vec{u}_{(i)}\vec{u}_{(i)}^T$
			for (int j = 0; j < i; j++)
			{
				T dot = Dot(u, A + j * ld, i);
				AddScaled(A + j * ld, u, -tau * dot, i);
			}
			// set diag and reset last row and column of A[..i, ..i] for next iteration
			diag[i] = A[i + i * ld];
			A[i + i * ld] = T.One;
			for (int j = 0; j < i; j++)
				A[j + i * ld] = A[i + j * ld] = T.Zero;
		}
		InPlaceTranspose(A, ld, n);
	}

	/// <summary>
	/// Compute the eigenvalues and eigenvectors of a symmetric tridiagonal matrix represented by <paramref name="diag"/> and <paramref name="offDiag"/> where <paramref name="diag"/> will be replaced by the eigenvalues at exit. The <paramref name="eigenvectors"/> contains the original unary matrix to be multiplied in-place.
	/// </summary>
	public static bool SymmetricTridiagonalMatrixEigensolve<T>(int n, T* diag, T* offDiag, T* eigenvectors, int eigvecLD) where T : unmanaged, IFloatingPoint<T>
	{
		T two = T.One + T.One;
		offDiag[n - 1] = T.Zero;
		for (int i = 0; i < n; i++)
		{
			int iter = 0, m;
		INNER_START:
			for (m = i; m < n - 1; m++)
			{
				T d = T.Abs(diag[m]) + T.Abs(diag[m + 1]);
				// look for a single small sub-diagonal element which indicates convergence of one eigenvalue
				if (T.Abs(offDiag[m]) + d == d)
					break;
			}
			if (m == i)
			{   // eigenvalue converged
				continue;
			}
			// now, A[i..m, i..m] (inclusive m) is tridiagonal in machine precision
			// perform QL with implicit shift
			if (iter++ == 30)
				return false;
			// get k_(shift)
			//tex: $k_s = \text{argmin}_x{|d_i - x|}$ where $x \in \text{eigval}\pmatrix{d_i & e_i \\ e_i & d_{i+1}}$
			T ks = (diag[i + 1] - diag[i]) / (two * offDiag[i]);
			// the last Householder reflector u must be the one in QL factorization of A[i..m, i..m]
			// i.e. the one computed from the last row/column of it
			//tex:$\vec{u} = [0, \ldots, 0, e_{m-1}, d_m \pm \|(e_{m-1}, d_m)\|]^T$
			T r = T.Sqrt(ks * ks + T.One);
			//tex: get $d_m - k_s$
			ks = diag[m] - diag[i] + offDiag[i] / (ks + T.CopySign(r, ks));
			T s = T.One, c = T.One, p = T.Zero;
			for (int j = m - 1; j >= i; j--)
			{
				// a plane rotation as in the original QL, followed by Givens rotations to restore tridiagonal form
				T f = s * offDiag[j], b = c * offDiag[j];
				r = T.Sqrt(ks * ks + f * f);
				offDiag[j + 1] = r;
				// deal with underflow
				if (r == T.Zero)
				{
					diag[j + 1] -= p;
					offDiag[m] = T.Zero;
					goto INNER_START;
				}
				s = f / r;
				c = ks / r;
				ks = diag[j + 1] - p;
				r = (diag[j] - ks) * s + two * c * b;
				p = s * r;
				diag[j + 1] = ks + p;
				ks = c * r - b;
				// compute eigenvectors
				if (eigenvectors == null)
					continue;
				int indI0 = j * eigvecLD;
				int indI1 = (j + 1) * eigvecLD;
				for (int k = 0; k < n; k++)
				{
					ref T ek0 = ref eigenvectors[k + indI0];
					ref T ek1 = ref eigenvectors[k + indI1];
					(ek0, ek1) = (s * ek0 + c * ek1, c * ek0 - s * ek1);
				}
			}
			diag[i] -= p;
			offDiag[i] = ks;
			offDiag[m] = T.Zero;
			goto INNER_START;
		}
		return true;
	}
	#endregion

	#region general eigen
	public static bool HessenbergSchurDecompose<T>(int n, T* A, int ld, T* wr, T* wi) where T : unmanaged, IFloatingPoint<T>
	{
		// TODO: store exceptional shifts to restore Schur form
		// TODO: explicitly get unitary matrix
		// TODO: reorder Schur decomposition result
		// TODO: get eigenvectors from Schur decomposition result

		T half = T.One / (T.One + T.One);
		T threeFourth = (T.One + T.One + T.One) / T.ScaleB(T.One, 2);
		T sevenSixteenth = (T.One + T.One + T.One + T.ScaleB(T.One, 2)) / T.ScaleB(T.One, 4);
		Unsafe.InitBlockUnaligned(wi, 0, (uint)(n * sizeof(T)));
		// get norm of A for small sub-diagonal elements
		T normA = T.Zero;
		for (int j = 0; j < n; j++)
			for (int i = 0; i < Math.Min(j + 2, n); i++)
				normA += T.Abs(A[i + j * ld]);
		int m = n - 1; // last unconverged eigenvalue index
		T t = T.Zero; // exceptional shift
		while (m >= 0)
		{
			int iters = 0;
		INNER_START:
			// look for single small sub-diagonal element
			int i = m;
			for (; i > 0; i--)
			{
				T s = T.Abs(A[i - 1 + (i - 1) * ld]) + T.Abs(A[i + i * ld]);
				if (s == T.Zero)
					s = normA;
				if (T.Abs(A[i + (i - 1) * ld]) + s == s)
				{
					A[i + (i - 1) * ld] = T.Zero;
					break;
				}
			}
			T x = A[m + m * ld];
			if (i == m)
			{   // one eigenvalue found
				wr[m--] = x + t;
				continue;
			}
			T y = A[m - 1 + (m - 1) * ld];
			T w = A[m + (m - 1) * ld] * A[m - 1 + m * ld];
			if (i == m - 1)
			{   // two eigenvalues found
				T pp = half * (y - x);
				T qq = pp * pp + w;
				T z = T.Sqrt(T.Abs(qq));
				x += t;
				if (qq >= T.Zero)
				{   // a real pair
					z = pp + T.CopySign(z, pp);
					wr[m - 1] = wr[m] = x + z;
					if (z != T.Zero)
						wr[m] = x - w / z;
				}
				else
				{   // a complex pair
					wr[m - 1] = wr[m] = x + pp;
					wi[m - 1] = -(wi[m] = z);
				}
				m -= 2;
				continue;
			}
			// continue QR with shift iteration
			if (iters == 30)
				return false; // to many iterations
			if (iters == 10 || iters == 20)
			{   // form exceptional shift
				t += x;
				for (int j = 0; j < m; j++)
					A[j + j * ld] -= x;
				T s = T.Abs(A[m + (m - 1) * ld]) + T.Abs(A[m - 1 + (m - 2) * ld]);
				y = x = threeFourth * s;
				w = -sevenSixteenth * s * s;
			}
			++iters;
			int k = m - 2;
			T p = T.Zero, q = T.Zero, r = T.Zero;
			for (; k >= i; k--)
			{
				// form shift
				//tex:$p_1=a_{2,1}\{[(a_{n,n}-a_{1,1})(a_{n-1,n-1}-a_{1,1}) - a_{n-1,n}a_{n,n-1}] / a_{2,1} + a_{1,2}\}$
				//tex:$q_1 = a_{2,1}[a_{2,2}-a_{1,1}-(a_{n,n} - a_{1,1}) - (a_{n-1,n-1}-a_{1,1})]$
				//tex:$r_1 = a_{2,1}a_{3,2}$
				T z = A[k + k * ld];
				r = x - z;
				T s = y - z;
				p = (r * s - w) / A[k + 1 + k * ld] + A[k + (k + 1) * ld];
				q = A[k + 1 + (k + 1) * ld] - z - r - s;
				r = A[k + 2 + (k + 1) * ld];
				// scale to prevent under- or over-flow
				s = T.Abs(p) + T.Abs(q) + T.Abs(r);
				q /= s; p /= s; r /= s;
				if (k == i)
					break;
				// look for	2 consecutive small sub-diagonal elements
				//tex:$|a_{k,k-1}(|q| + |r|)| \ll |p|(|a_{m+1,m+1}|+|a_{m,m}|+|a_{m-1,m-1}|)$
				T u = T.Abs(A[k + (k - 1) * ld]) * (T.Abs(q) + T.Abs(r));
				T v = T.Abs(p) * (T.Abs(A[k - 1 + (k + 1) * ld]) + T.Abs(z) + T.Abs(A[k + 1 + (k + 1) * ld]));
				if (u + v == v)
					break;
			}
			// set remaining sub-diagonals to 0
			for (int l = k + 2; l <= m; l++)
			{
				A[l + (l - 2) * ld] = T.Zero;
				if (l == m + 2)
					continue;
				A[l + (l - 3) * ld] = T.Zero;
			}
			// double QR steps on A[i..m, k..m] (inclusive m)
			for (int l = k; l < m; l++)
			{
				if (l != k)
				{   // setup of Householder reflector
					p = A[l + (l - 1) * ld];
					q = A[l + 1 + (l - 1) * ld];
					r = T.Zero;
					if (l != m - 1)
						r = A[l + 2 + (l - 1) * ld];
					if ((x = T.Abs(p) + T.Abs(q) + T.Abs(r)) != T.Zero)
					{	// scale to prevent under- or over-flow
						p /= x; q /= x; r /= x;
					}
				}
				T s = T.CopySign(T.Sqrt(p * p + q * q + r * r), p);
				if (s == T.Zero)
					continue;
				if (l == k)
				{
					if (i != k)
						A[l + (l - 1) * ld] = -A[l + (l - 1) * ld];
				}
				else
				{
					A[l + (l - 1) * ld] = -s * x;
				}
				//tex: the non-zero elements of $\vec{u}$ are $(p \pm s)/(\pm s), q/(\pm s), r/(\pm s)$
				p += s;
				x = p / s;
				y = q / s;
				T z = r / s;
				q /= p;
				r /= p;
				// modify rows of A
				for (int j = l; j <= m; j++)
				{
					p = A[l + j * ld] + q * A[l + 1 + j * ld];
					if (l != m - 1)
					{
						p += r * A[l + 2 + j * ld];
						A[l + 2 + j * ld] -= p * z;
					}
					A[l + 1 + j * ld] -= p * y;
					A[l + j * ld] -= p * x;
				}
				// modify columns of A
				int mmin = Math.Min(m, l + 3);
				for (int j = i; j <= mmin; j++)
				{
					p = x * A[j + l * ld] + y * A[j + (l + 1) * ld];
					if (l != (m - 1))
					{
						p += z * A[j + (l + 2) * ld];
						A[j + (l + 2) * ld] -= p * r;
					}
					A[j + (l + 1) * ld] -= p * q;
					A[j + l * ld] -= p;
				}
			}
			// loop
			if (i >= m - 1)
				continue;
			else
				goto INNER_START;
		}
		// chop small sub-diagonal values
		T threshold = T.Sqrt(normA) * Math.Sqrt(NumberType<T>.MachinePrecision).As<double, T>();
		for (int i = 0; i < n; i++)
		{
			for (int j = i + 1; j < n; j++)
			{
				ref T v = ref A[j + i * n];
				if (T.Abs(v) < threshold)
					v = T.Zero;
			}
		}
		return true;
	}
	#endregion
}
