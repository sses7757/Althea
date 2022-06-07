using System.Numerics;
using System.Runtime.CompilerServices;

using Althea.NativeTypes;

namespace Althea.Backend.CSharp.LinearAlgebra;


internal static unsafe class MatrixSolvers
{
	// TODO: use existing codes
	// Ignore Spelling: \vec \alpha \beta \tau \ldots \langle \rangle \pmatrix \cdot \begin \leftarrow eigval argmin
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
	private static void VecMulScaledMat<T>(T* A, int ld, T α, T* x, T* output, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < m; i++)
		{
			output[i] = α * Dot(A + i * ld, x, n);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void MatMulScaledVec<T>(T* A, int ld, T α, T* x, T* output, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		Unsafe.InitBlockUnaligned(output, 0, (uint)(m * sizeof(T)));
		for (int i = 0; i < n; i++)
		{
			AddScaled(output, A + i * ld, x[i] * α, m);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Rank1UpdateNeg<T>(T* A, int ld, T* x, T* y, int m, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			AddScaled(A + i * ld, x, -y[i], m);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SymRank2UpdateNeg<T>(T* A, int ld, T* x, T* y, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			AddScaled(A + i * ld, x, -y[i], n);
			AddScaled(A + i * ld, y, -x[i], n);
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

	// TODO: SIMD
	//tex:$H = \begin{pmatrix}h_1&h_3\\h_3&h_2\end{pmatrix}$
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void HouseholderColumnUpdate2<T>(T* A, int ld, int n, T h1, T h2, T h3) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0, j = ld; i < n; i++, j++)
		{
			(A[i], A[j]) = (A[i] * h1 + A[j] * h3, A[i] * h3 + A[j] * h2);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void HouseholderRowUpdate2<T>(T* A, int ld, int n, T h1, T h2, T h3) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0, j = 0; i < n; i++, j += ld)
		{
			(A[j], A[j + 1]) = (A[j] * h1 + A[j + 1] * h3, A[j] * h3 + A[j + 1] * h2);
		}
	}
	//tex:$H = \begin{pmatrix}h_1&h_4&h_5\\h_4&h_2&h_6\\h_5&h_6&h_3\end{pmatrix}$
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void HouseholderColumnUpdate3<T>(T* A, int ld, int n, T h1, T h2, T h3, T h4, T h5, T h6) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0, j = ld, k = ld * 2; i < n; i++, j++, k++)
		{
			(A[i], A[j], A[k]) = (A[i] * h1 + A[j] * h4 + A[k] * h5, A[i] * h4 + A[j] * h2 + A[k] * h6, A[i] * h5 + A[j] * h6 + A[k] * h3);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void HouseholderRowUpdate3<T>(T* A, int ld, int n, T h1, T h2, T h3, T h4, T h5, T h6) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0, j = 0; i < n; i++, j += ld)
		{
			(A[j], A[j + 1], A[j + 2]) = (A[j] * h1 + A[j + 1] * h4 + A[j + 2] * h5, A[j] * h4 + A[j + 1] * h2 + A[j + 2] * h6, A[j] * h5 + A[j + 1] * h6 + A[j + 2] * h3);
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
				HouseholderColumnUpdate2(eigenvectors + j * eigvecLD, eigvecLD, n, h1, h2, h3);
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

	/// <summary>
	/// Compute the Schur factorization of given upper Hessenberg matrix <paramref name="A"/> and corresponding unary matrix <paramref name="Q"/> to transformation a general matrix to <paramref name="A"/>. After return, <paramref name="A"/> will be replaced by its Schur form, <paramref name="Q"/> will multiply the Schur vectors in-place and <c><paramref name="wr"/> + √(-1) * <paramref name="wi"/></c> will be the eigenvalues.
	/// </summary>
	public static bool HessenbergSchurFactorize<T>(int n, T* A, int lda, T* Q, int ldq, T* wr, T* wi) where T : unmanaged, IFloatingPoint<T>
	{
		// constants
		T two = T.One + T.One, half = T.One / two, halfSqrt2 = T.Sqrt(two) * half, four = two + two;
		Unsafe.InitBlockUnaligned(wi, 0, (uint)(n * sizeof(T)));
		T* v = stackalloc T[3];

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
				// transform to standard Schur form
				//tex:$A_{i:k,:}\gets HA_{i:k,:}$; $A_{:,i:k}\gets A_{:,i:k}H$; $U_{:,i:k}\gets U_{:,i:k}H$,
				//where $h_2,h_3=\frac{\sqrt2}{2}\sqrt{1\pm\frac{\left|b+c\right|}{\sqrt{\left(b+c\right)^2+\left(a-d\right)^2}}}$ for complex;
				//or $h_2,h_3=\sqrt{\frac{2\alpha\left(b+c\right)+\left(a-d\right)\left[a-d\pm\sqrt{4bc+\left(a-d\right)^2}\right]}{2\left[\left(b+c\right)^2+\left(a-d\right)^2\right]}}$, $\alpha = b, c$ for real pair
				T h1, h2, h3;
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
						goto CONT;
					h2 = halfSqrt2 * T.Sqrt(T.One + bc / nrm);
					h3 = halfSqrt2 * T.Sqrt(T.One - bc / nrm);
					if (ad < T.Zero && bc > T.Zero)
					{
						(h2, h3) = (h3, h2);
					}
					if (ad < T.Zero && bc < T.Zero)
					{ }
					if (ad > T.Zero && bc > T.Zero)
					{ }
					if (ad > T.Zero && bc < T.Zero)
					{ }
				}
				h1 = -h2;
				HouseholderColumnUpdate2(A + i * lda, lda, k + 1, h1, h2, h3);
				HouseholderRowUpdate2(A + (i + i * lda), lda, n - i, h1, h2, h3);
				HouseholderColumnUpdate2(Q + i * ldq, ldq, n, h1, h2, h3);
				if (delta >= T.Zero)
					A[k + i * lda] = T.Zero;
				CONT:
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
				if (j == i)
				{
					//tex:$$\vec{v}\gets \begin{pmatrix}{\left[\left(a_{k,k}-a_{i,i}\right)\left(a_{k-1,k-1}-a_{i,i}\right)-a_{k-1,k}a_{k,k-1}\right]}/{a_{i+1,i}}+a_{i,i+1} \\ a_{i,i}+a_{i+1,i+1}-\left(a_{k-1,k-1}+a_{k,k}\right) \\ a_{i+2,i+1} \end{pmatrix}$$
					T akk = A[k + k * lda], aii = A[i + i * lda],
					  ak1k1 = A[k - 1 + (k - 1) * lda], ai1i1 = A[i + 1 + (i + 1) * lda],
					  ak1k = A[k - 1 + k * lda], akk1 = A[k + (k - 1) * lda],
					  ai1i = A[i + 1 + i * lda], aii1 = A[i + (i + 1) * lda];
					v[0] = ((akk - aii) * (ak1k1 - aii) - ak1k * akk1) / ai1i + aii1;
					v[1] = aii + ai1i1 - (ak1k1 + akk);
					v[2] = A[i + 2 + (i + 1) * lda];
				}
				else
				{
					//tex:$\vec{v}\gets{\vec{a}}_{j:j+2,j-1}$
					Unsafe.CopyBlockUnaligned(v, A + (j + (j - 1) * lda), (uint)(3 * sizeof(T)));
				}
				int nv = j == k - 1 ? 2 : 3;
				//tex:$H\gets I - 2\vec{v}\vec{v}^T / \|v\|^2 = \begin{pmatrix}h_1&h_4&h_5\\h_4&h_2&h_6\\h_5&h_6&h_3\end{pmatrix}$
				T normV = T.Sqrt(NormSq(v, nv));
				v[0] += T.CopySign(normV, v[0]);
				T tau = two / NormSq(v, nv);
				T h1 = T.One - tau * v[0] * v[0], h2 = T.One - tau * v[1] * v[1], h3 = T.One - tau * v[2] * v[2];
				T h4 = -tau * v[0] * v[1], h5 = -tau * v[0] * v[2], h6 = -tau * v[1] * v[2];
				//tex:$A_{j:j+2,:}\gets HA_{j:j+2,:}$; $A_{:,j:j+2}\gets A_{:,j:j+2}H$; $U_{:,j:j+2}\gets U_{:,j:j+2}H$
				int colFree = j == i ? i : j - 1;
				if (j == k - 1)
				{
					HouseholderColumnUpdate2(A + (j * lda), lda, Math.Min(j + 4, n), h1, h2, h4);
					HouseholderRowUpdate2(A + (j + colFree * lda), lda, n - colFree, h1, h2, h4);
					HouseholderColumnUpdate2(Q + (j * ldq), ldq, n, h1, h2, h4);
					A[j + 1 + (j - 1) * lda] = T.Zero;
				}
				else
				{
					HouseholderColumnUpdate3(A + (j * lda), lda, Math.Min(j + 4, n), h1, h2, h3, h4, h5, h6);
					HouseholderRowUpdate3(A + (j + colFree * lda), lda, n - colFree, h1, h2, h3, h4, h5, h6);
					HouseholderColumnUpdate3(Q + (j * ldq), ldq, n, h1, h2, h3, h4, h5, h6);
					if (j != i)
						A[j + 1 + (j - 1) * lda] = A[j + 2 + (j - 1) * lda] = T.Zero;
				}
			}
			// restart iteration for this eigenvalue
			goto RESTART_EIGVAL;
		}
		// get first eigenvalue and return
		wr[0] = A[0];
		return true;
	}

	#endregion
}
