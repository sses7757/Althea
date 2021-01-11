using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Althea.Runtime.Mkl
{
	/// <summary>
	/// Native methods from MKL runtime API
	/// </summary>
	public static class NativeMethods
	{
		#region normal
		/// <summary>
		/// The MKL Runtime library name
		/// </summary>
		public const string MKLRT_API_DLL_NAME = "mkl_rt";

		/// <summary>
		/// Set number of threads used by MKL library
		/// </summary>
		/// <param name="nt">number of threads to use</param>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern void MKL_Set_Num_Threads(int nt);

		/// <summary>
		/// Get the number of threads used by MKL library
		/// </summary>
		/// <returns>number of threads used</returns>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern int MKL_Get_Max_Threads();

		/// <summary>
		/// Allocates the aligned buffer
		/// </summary>
		/// <param name="size">Size of the memory buffer to be allocated.</param>
		/// <param name="align">Alignment of the memory buffer.</param>
		/// <returns>Pointer to the allocated buffer</returns>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern IntPtr MKL_malloc(long size, int align);

		/// <summary>
		/// Free the aligned buffer
		/// </summary>
		/// <param name="ptr">Pointer to the allocated buffer</param>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern void MKL_free(IntPtr ptr);

		/// <summary>
		/// Frees unused memory allocated by the Intel® MKL Memory Allocator.
		/// </summary>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern void MKL_Free_Buffers();

		/// <summary>
		/// Terminates Intel® MKL execution environment and frees resources allocated by the library.
		/// </summary>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern void MKL_Finalize();

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
		/// <summary>
		/// Returns the Intel® MKL version in a character string.
		/// </summary>
		/// <param name="str">the input and output string</param>
		/// <param name="len">length of <paramref name="str"/></param>
		[DllImport(MKLRT_API_DLL_NAME, CharSet = CharSet.Ansi)]
		internal static extern void MKL_Get_Version_String(StringBuilder str, int len);

		/// <summary>
		/// Enables dispatching for new Intel® architectures or restricts the set of Intel® instruction sets available for dispatching.
		/// </summary>
		/// <param name="isa">The latest Intel® instruction-set architecture (ISA) for Intel® MKL to dispatch.</param>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern int MKL_Enable_Instructions(MKLInstruction isa);

		/// <summary>
		/// Enables or disables Intel® MKL Verbose mode.
		/// </summary>
		/// <param name="enable">Desired state of the Intel® MKL Verbose mode. Indicates whether printing Intel® MKL function call information should be turned on or off.Possible values:
		/// <list type="bullet"><item><description>0 - disable the Verbose mode.</description></item>
		/// <item><description>1 - enable the Verbose mode.</description></item></list>
		/// </param>
		/// <returns>If the requested operation completed successfully, contains previous state of the verbose mode. If the function failed to complete the operation because of an incorrect input parameter, returns -1</returns>
		[DllImport(MKLRT_API_DLL_NAME)]
		internal static extern int MKL_Verbose(int enable);

		/// <summary>
		/// Write output in Intel® MKL Verbose mode to a file.
		/// </summary>
		/// <param name="path">Name of file. Specify the complete path of the output file.</param>
		/// <returns>If the file does not exist or cannot be opened, the write operation is unsuccessful. The function returns 1 and defaults to print to <see cref="Console.Out"/>; otherwise, it returns 0.</returns>
		[DllImport(MKLRT_API_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		internal static extern int MKL_Verbose_Output_File([In] string path);
#pragma warning restore CA2101
		#endregion

		#region custom
		/// <summary>
		/// Set the pointer's value at each byte
		/// </summary>
		/// <param name="a">array pointer</param>
		/// <param name="v">value to set at each byte</param>
		/// <param name="N">length of array in bytes</param>
		[DllImport(Blas.Mkl.Customs.NativeMethods.KERNEL_DLL_NAME)]
		internal static extern void hostmemset(IntPtr a, int v, long N);

		/// <summary>
		/// Copy <paramref name="src"/> to <paramref name="dst"/> with length <paramref name="N"/>
		/// </summary>
		/// <param name="src">source pointer</param>
		/// <param name="dst">destination pointer</param>
		/// <param name="N">size of bytes to copy</param>
		[DllImport(Blas.Mkl.Customs.NativeMethods.KERNEL_DLL_NAME)]
		internal static extern void hostmemcopy([In] IntPtr src, IntPtr dst, long N);

		/// <summary>
		/// Copies 2D data between different pointers.
		/// </summary>
		/// <param name="src">the source pointer</param>
		/// <param name="srcPitch">source array actual height (actual leading dimension) in bytes</param>
		/// <param name="dst">the destination pointer</param>
		/// <param name="dstPitch">destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">height to copy, in bytes</param>
		/// <param name="width">width to copy, in real size rather than bytes</param>
		[DllImport(Blas.Mkl.Customs.NativeMethods.KERNEL_DLL_NAME)]
		internal static extern void hostmemcopy2D([In] IntPtr src, long srcPitch, IntPtr dst, long dstPitch, long height, long width);

		/// <summary>
		/// Get the total and free memory of host
		/// </summary>
		/// <param name="total">output total physical memory</param>
		/// <param name="free">output free physical memory</param>
		[DllImport(Blas.Mkl.Customs.NativeMethods.KERNEL_DLL_NAME)]
		internal static extern void getTotalSystemMemory(ref ulong total, ref ulong free);
		#endregion
	}

	/// <summary>
	/// The enum for instructions of MKL
	/// </summary>
	public enum MKLInstruction
	{
		/// <summary>
		/// Intel® Streaming SIMD Extensions 4-2 (Intel® SSE4-2).
		/// </summary>
		MKL_ENABLE_SSE4_2 = 0,
		/// <summary>
		/// Intel® Advanced Vector Extensions (Intel® AVX).
		/// </summary>
		MKL_ENABLE_AVX = 1,
		/// <summary>
		/// Intel® Advanced Vector Extensions 2 (Intel® AVX2).
		/// </summary>
		MKL_ENABLE_AVX2 = 2,
		/// <summary>
		/// Intel AVX-512 on Intel® Xeon Phi™ processors.
		/// </summary>
		MKL_ENABLE_AVX512_MIC = 3,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) on Intel® Xeon® processors.
		/// </summary>
		MKL_ENABLE_AVX512 = 4,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) with support for Vector Neural Network Instructions on Intel® Xeon Phi™ processors.
		/// </summary>
		MKL_ENABLE_AVX512_MIC_E1 = 5,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) with support for Vector Neural Network Instructions.
		/// </summary>
		MKL_ENABLE_AVX512_E1 = 6,
	}
}
