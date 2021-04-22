using System;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;


#pragma warning disable IDE1006
namespace Althea.Backend.Cuda.LinearAlgebra.Sparse
{
	/// <summary>
	/// CUDA SPARSE library API
	/// </summary>
	public static unsafe class NativeMethods
	{
		/// <summary>
		/// The CUDA SPARSE (cuSPARSE) library name
		/// </summary>
		public const string CUSPARSE_API_DLL_NAME = @"cusparse";
		
		/// <summary>
		/// The custom CUDA library name
		/// </summary>
		public const string CUSTOM_API_DLL_NAME = Storage.NativeMethods.CUSTOM_API_DLL_NAME;

	}
}
