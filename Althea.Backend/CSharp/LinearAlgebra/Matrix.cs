using System.Runtime.CompilerServices;

namespace Althea.Backend.CSharp.LinearAlgebra;

internal static unsafe class MatrixSolvers
{
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

	/// <summary>
	/// Compute the eigenvalues and eigenvectors of a symmetric tridiagonal matrix represented by <paramref name="diag"/> and <paramref name="offDiag"/> where <paramref name="diag"/> will be replaced by the eigenvalues. The <paramref name="eigenvectors"/> contains the original unary matrix to be multiplied in-place.
	/// </summary>
	public static bool SymmetricTridiagonalMatrixEigensolve<T>(int n, T* diag, T* offDiag, T* eigenvectors, int eigvecLD) where T : unmanaged, IFloatingPoint<T>
	{
		T two = T.One + T.One;
		offDiag[n - 1] = T.Zero;
		for (int l = 0; l < n; l++)
		{
			int iter = 0, m;
		INNER_START:
			for (m = l; m < n - 1; m++)
			{
				T d = T.Abs(diag[m]) + T.Abs(diag[m + 1]);
				// look for a single small sub-diagonal element to split the matrix.
				if (T.Abs(offDiag[m]) + d == d)
					break;
			}
			if (m == l)
				continue;
			// QL with implicit shift
			if (iter++ == 30)
				return false;
			// form shift
			T g = (diag[l + 1] - diag[l]) / (two * offDiag[l]);
			T r = Hypot(g, T.One);
			g = diag[m] - diag[l] + offDiag[l] / (g + T.CopySign(r, g));
			T s = T.One, c = T.One, p = T.Zero;
			for (int i = m - 1; i >= l; i--)
			{
				T f = s * offDiag[i], b = c * offDiag[i];
				r = Hypot(f, g);
				offDiag[i + 1] = r;
				if (r == T.Zero)
				{
					diag[i + 1] -= p;
					offDiag[m] = T.Zero;
					goto INNER_START;
				}
				s = f / r;
				c = g / r;
				g = diag[i + 1] - p;
				r = (diag[i] - g) * s + two * c * b;
				p = s * r;
				diag[i + 1] = g + p;
				g = c * r - b;
				// compute eigenvectors
				for (int k = 0; k < n; k++)
				{
					int indI = k + i * eigvecLD;
					int indI1 = k + (i + 1) * eigvecLD;
					f = eigenvectors[indI1];
					eigenvectors[indI1] = s * eigenvectors[indI] + c * f;
					eigenvectors[indI] = c * eigenvectors[indI] - s * f;
				}
			}
			diag[l] -= p;
			offDiag[l] = g;
			offDiag[m] = T.Zero;
			goto INNER_START;
		}
		return true;
	}
}
