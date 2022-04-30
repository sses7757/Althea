using System.Runtime.CompilerServices;


namespace Althea.Backend.CSharp.LinearAlgebra;

internal static unsafe class MatrixSolvers
{
	// Ignore Spelling: \vec \alpha \beta \tau \ldots \langle \rangle \pmatrix \cdot
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T Hypot<T>(T x, T y) where T : unmanaged, IFloatingPoint<T>
	{
		return T.Sqrt(x * x + y * y);
		////T t;
		////x = T.Abs(x);
		////y = T.Abs(y);
		////t = T.Min(x, y);
		////x = T.Max(x, y);
		////t /= x;
		////return x * T.Sqrt(T.One + t * t);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T NormSqr<T>(T* vec, int n) where T : unmanaged, IFloatingPoint<T>
	{
		T norm = T.Zero;
		for (int i = 0; i < n; i++)
			norm += vec[i] * vec[i];
		return norm;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T Dot<T>(T* x, T* y, int n) where T : unmanaged, IFloatingPoint<T>
	{
		T dot = T.Zero;
		for (int i = 0; i < n; i++)
			dot += x[i] * y[i];
		return dot;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AddScaled<T>(T* y, T* x, T α, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
			y[i] += α * x[i];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SymMatMulVec<T>(T* A, int ld, T α, T* x, T* output, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			output[i] = α * Dot(A + i * ld, x, n);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SymRank2UpdateNeg<T>(T* A, int ld, T* x, T* y, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < n; j++)
			{
				A[j + i * ld] -= x[i] * y[j] + x[j] * y[i];
			}
		}
	}


	public static void QRDecompose<T>(int m, int n, T* A, int ld, T* tau) where T : unmanaged, IFloatingPoint<T>
	{
		T two = T.One + T.One;
		// reduce to tridiagonal by Householder reflect from the last column
		for (int i = n - 1; i >= 2; i--)
		{
			int im1 = i - 1;
			// get vector u and store in A's column i
			//tex:$$\vec{u} = \pmatrix{\vec{A}_{0:i-1,i} \\  A_{i,i} \pm \|\vec{A}_{0:i,i}\|}$$
			T* u = A + i * ld;
			ref T uLast = ref u[i];
			T normSqrU = NormSqr(u, i), normU = T.Sqrt(normSqrU);
			normU = T.CopySign(normU, uLast);
			// get tau[i]
			//tex:$$H = I - \tau \vec{u}\vec{u}^T$$
			tau[i] = T.One / (normSqrU + uLast * normU);
			uLast += normU;
			// get A[0..i, 0..i]
			//tex:$\vec{p} = \tau A \vec{u}$ and store in diag[..i]
			SymMatMulVec(A, ld, tau, u, diag, i);
			//tex:$$\vec{p}=\vec{p}-\frac{\tau\vec{u}\cdot\vec{p}}{2}\vec{u}$$
			T k = Dot(u, diag, i) * tau / two;
			AddScaled(diag, u, -k, i);
			//tex:$A_{0:i-1,0:i-1} = A_{0:i-1,0:i-1} - \vec{q}\vec{u}^T - \vec{u}\vec{q}^T$
			SymRank2UpdateNeg(A, ld, diag, u, i);
		}
	}

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
			T normSqrU = NormSqr(u, i), normU = T.Sqrt(normSqrU);
			normU = T.CopySign(normU, uLast);
			// get tau and store in A[i, i-1]
			//tex:$$H = I - \tau \vec{u}\vec{u}^T$$
			T tau = T.One / (normSqrU + uLast * normU);
			uLast += normU;
			A[i + im1 * ld] = tau;
			// get A[0..i, 0..i]
			//tex:$\vec{p} = \tau A \vec{u}$ and store in diag[..i]
			SymMatMulVec(A, ld, tau, u, diag, i);
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
			// set diag and reset last row and column of A[..i, ..i] to identity for next iteration
			diag[i] = A[i + i * ld];
			A[i + i * ld] = T.One;
			for (int j = 0; j < i; j++)
				A[j + i * ld] = A[i + j * ld] = T.Zero;
		}
		// in-place transpose A
		for (int i = 0; i < n; i++)
		{
			for (int j = i + 1; j < n; j++)
			{
				(A[i + j * ld], A[j + i * ld]) = (A[j + i * ld], A[i + j * ld]);
			}
		}
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
				// look for a single small sub-diagonal element to split the matrix.
				if (T.Abs(offDiag[m]) + d == d)
					break;
			}
			if (m == i)
				continue;
			// QL with implicit shift
			if (iter++ == 30)
				return false;
			//tex: form shift $k_s$
			T ks = (diag[i + 1] - diag[i]) / (two * offDiag[i]);
			T r = Hypot(ks, T.One);
			//tex: get $d_m - k_s$
			ks = diag[m] - diag[i] + offDiag[i] / (ks + T.CopySign(r, ks));
			T s = T.One, c = T.One, p = T.Zero;
			for (int j = m - 1; j >= i; j--)
			{
				// a plane rotation as in the original QL, followed by Givens rotations to restore tridiagonal form
				T f = s * offDiag[j], b = c * offDiag[j];
				r = Hypot(f, ks);
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
				for (int k = 0; k < n; k++)
				{
					int indI = k + j * eigvecLD;
					int indI1 = k + (j + 1) * eigvecLD;
					eigenvectors[indI1] = s * eigenvectors[indI] + c * eigenvectors[indI1];
					eigenvectors[indI] = c * eigenvectors[indI] - s * eigenvectors[indI1];
				}
			}
			diag[i] -= p;
			offDiag[i] = ks;
			offDiag[m] = T.Zero;
			goto INNER_START;
		}
		return true;
	}
}
