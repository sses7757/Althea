using System.Runtime.InteropServices;


namespace Althea.Backend.Mkl.Random
{
	/// <summary>
	/// MKL Random Number Generator library API
	/// </summary>
	public static unsafe class NativeMethods
	{
		#region helpers
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vslNewStream(out IntPtr stream, GeneratorType generator, uint seed);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vslDeleteStream(in IntPtr stream);
		#endregion

		#region floating point
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngUniform(MklRngMethodUniform method, IntPtr stream, MklInt n, float* array, float a, float b);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngUniform(MklRngMethodUniform method, IntPtr stream, MklInt n, double* array, double a, double b);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngGaussian(MklRngMethodGaussian method, IntPtr stream, MklInt n, float* array, float mean, float sigma);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngGaussian(MklRngMethodGaussian method, IntPtr stream, MklInt n, double* array, double mean, double sigma);

		// multidimensional Gaussian, covariance matrix = T Tᵀ, length(means) == dim
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngGaussianMV(MklRngMethodGaussian method, IntPtr stream, MklInt n, float** arrays, int dim, MklRngMatrixStorage storageT, in float means, in float matrixT);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngGaussianMV(MklRngMethodGaussian method, IntPtr stream, MklInt n, double** arrays, int dim, MklRngMatrixStorage storageT, in double means, in double matrixT);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngExponential(MklRngMethodExponential method, IntPtr stream, MklInt n, float* array, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngExponential(MklRngMethodExponential method, IntPtr stream, MklInt n, double* array, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngLaplace(MklRngMethodLaplace method, IntPtr stream, MklInt n, float* array, float mean, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngLaplace(MklRngMethodLaplace method, IntPtr stream, MklInt n, double* array, double mean, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngWeibull(MklRngMethodWeibull method, IntPtr stream, MklInt n, float* array, float alpha, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngWeibull(MklRngMethodWeibull method, IntPtr stream, MklInt n, double* array, double alpha, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngCauchy(MklRngMethodCauchy method, IntPtr stream, MklInt n, float* array, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngCauchy(MklRngMethodCauchy method, IntPtr stream, MklInt n, double* array, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngRayleigh(MklRngMethodRayleigh method, IntPtr stream, MklInt n, float* array, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngRayleigh(MklRngMethodRayleigh method, IntPtr stream, MklInt n, double* array, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngLognormal(MklRngMethodLogNormal method, IntPtr stream, MklInt n, float* array, float normalMean, float normalSigma, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngLognormal(MklRngMethodLogNormal method, IntPtr stream, MklInt n, double* array, double normalMean, double normalSigma, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngGumbel(MklRngMethodGumbel method, IntPtr stream, MklInt n, float* array, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngGumbel(MklRngMethodGumbel method, IntPtr stream, MklInt n, double* array, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngGamma(MklRngMethodGamma method, IntPtr stream, MklInt n, float* array, float alpha, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngGamma(MklRngMethodGamma method, IntPtr stream, MklInt n, double* array, double alpha, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngBeta(MklRngMethodBeta method, IntPtr stream, MklInt n, float* array, float p, float q, float displacement, float beta);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngBeta(MklRngMethodBeta method, IntPtr stream, MklInt n, double* array, double p, double q, double displacement, double beta);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vsRngChiSquare(MklRngMethodChiSquare method, IntPtr stream, MklInt n, float* array, int DoF);
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus vdRngChiSquare(MklRngMethodChiSquare method, IntPtr stream, MklInt n, double* array, int DoF);
		#endregion

		#region integer
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngUniform(MklRngMethodUniform method, IntPtr stream, MklInt n, int* array, int a, int b);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngUniformBits(MklRngMethodUniformBits method, IntPtr stream, MklInt n, uint* array);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngUniformBits32(MklRngMethodUniformBits method, IntPtr stream, MklInt n, uint* array);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngUniformBits64(MklRngMethodUniformBits method, IntPtr stream, MklInt n, ulong* array);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngBernoulli(MklRngMethodBernoulli method, IntPtr stream, MklInt n, int* array, double p);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngGeometric(MklRngMethodGeometric method, IntPtr stream, MklInt n, int* array, double p);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngBinomial(MklRngMethodBinomial method, IntPtr stream, MklInt n, int* array, int nTrial, double p);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngHypergeometric(MklRngMethodHypergeometric method, IntPtr stream, MklInt n, int* array, int lotSize, int sampleSize, int markedElements);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngPoisson(MklRngMethodPoisson method, IntPtr stream, MklInt n, int* array, double lambda);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngPoissonV(MklRngMethodPoissonVariableMean method, IntPtr stream, MklInt n, int* array, double* lambda);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngNegbinomial(MklRngMethodNegativeBinomial method, IntPtr stream, MklInt n, int* array, double a, double p);

		// multi-dimensional, length(p) == dim
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklRngStatus viRngMultinomial(MklRngMethodMultinomial method, IntPtr stream, MklInt n, int** arrays, int nTrial, int dim, in double p);
		#endregion
	}
}
