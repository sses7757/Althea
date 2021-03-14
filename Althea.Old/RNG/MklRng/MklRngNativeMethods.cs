using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Althea.Rng.Mkl
{
	/// <summary>
	/// MKL Random Number Generator library API
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The MKL RNG library name
		/// </summary>
		public const string MKLRNG_API_DLL_NAME = "mkl_rt";


		[DllImport(MKLRNG_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status vslNewStream(ref IntPtr stream, GeneratorType generator, int seed);

		[DllImport(MKLRNG_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status vslDeleteStream(ref IntPtr stream);

		[DllImport(MKLRNG_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status vsRngUniform(Method method, IntPtr stream, int n, IntPtr array, float a, float b);

		[DllImport(MKLRNG_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status vdRngUniform(Method method, IntPtr stream, int n, IntPtr array, double a, double b);

		[DllImport(MKLRNG_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status viRngUniformBits(Method method, IntPtr stream, int n, IntPtr array);
	}
}
