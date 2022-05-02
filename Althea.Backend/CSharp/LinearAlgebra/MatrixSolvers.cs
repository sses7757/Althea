using System.Runtime.CompilerServices;

using Althea.NativeTypes;

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
		{
			norm += vec[i] * vec[i];
		}
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
	private static void Scale<T>(T* x, T α, int n) where T : unmanaged, IFloatingPoint<T>
	{
		for (int i = 0; i < n; i++)
			x[i] = α * x[i];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AddScaled<T>(T* y, T* x, T α, int n) where T : unmanaged, IFloatingPoint<T>
	{
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
			for (int j = 0; j < m; j++)
			{
				A[j + i * ld] -= x[j] * y[i];
			}
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

	/// <summary>
	/// Perform the QR factorization of matrix <paramref name="A"/> of size <paramref name="m"/>x<paramref name="n"/>, whose upper part is the target triangular matrix, lower part (including diagonal) is the Householder reflectors and the real triangular diagonal is stored in <paramref name="diag"/>. <paramref name="diag"/> shall have length ≥ <c>max(<paramref name="n"/> - 1, min(m, n))</c>.
	/// </summary>
	public static void QrFactorize<T>(int m, int n, T* A, int ld, T* diag) where T : unmanaged, IFloatingPoint<T>
	{
		T two = T.One + T.One, three = two + T.One;
		// reduce to triangular by Householder reflect from the first column
		int mn = Math.Min(m, n) - 1;
		for (int i = 0; i < mn; i++)
		{
			// get vector u and store in A[i:,i]
			//tex:$$\vec{u} = \pmatrix{A_{i,i} \pm \|\vec{A}_{i:,i}\| \\ \vec{A}_{i+1:,i}}$$
			T* u = A + (i + i * ld);
			T normSqrU = NormSqr(u, m - i), normU = T.Sqrt(normSqrU);
			normU = T.CopySign(normU, u[0]);
			// get tau and A[i, i]
			//tex: $$\tau = \frac{2}{\|\vec{u}\|^2}$$
			T tau = T.One / (normSqrU + u[0] * normU);
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

	public static void QrGenerateQ<T>(bool full, int m, int n, T* A, int ld, T* work) where T : unmanaged, IFloatingPoint<T>
	{
		// generate Q starting from the last column that stores vector u
		int mn = Math.Min(m, n) - 1;
		A[mn + mn * ld] = T.One;
		for (int i = mn - 1; i >= 0; i--)
		{
			int len = mn - i + 1;
			// get u from A's column i and copy to work space
			T* u = work, Aii = A + (i + i * ld);
			Unsafe.CopyBlockUnaligned(work, Aii, (uint)(len * sizeof(T)));
			// set diag and reset first row and column of A[i.., i..] for this iteration
			Aii[0] = T.One;
			for (int j = i; j < mn; j++)
				A[j + i * ld] = A[i + j * ld] = T.Zero;
			// update Householder reflectors' product stored in row major A[i.., i..]
			// TODO: size
			//tex:$H_{(i)} = H_{(i-1)} - \tau H_{(i-1)} \vec{u}_{(i)}\vec{u}_{(i)}^T$
			for (int j = i; j < mn; j++)
			{
				T dot = Dot(u, A + j * ld, len);
				AddScaled(A + j * ld, u, -dot, len);
			}
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
