using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Althea.Storage
{
	internal enum ReadFileEnum
	{
		Success = 0,
		OpenFileError = 1,
		MemoryAllocationError = 2,
		FileSizeInconsistentError = 3
	};

	/// <summary>
	/// The custom file operation API
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The custom supplementary routines library on CPU
		/// </summary>
		public const string KERNEL_DLL_NAME = "kernels_cpu";

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
		/// <summary>
		/// Write host pointer contents to file
		/// </summary>
		/// <param name="a">array pointer</param>
		/// <param name="N">length of array in bytes</param>
		/// <param name="path">path of file to write</param>
		[DllImport(KERNEL_DLL_NAME, CharSet = CharSet.Ansi)]
		internal static extern ReadFileEnum hostToFile([In] IntPtr a, long N, string path);

		/// <summary>
		/// Read host pointer contents from file
		/// </summary>
		/// <param name="a">array pointer</param>
		/// <param name="N">length of array in bytes</param>
		/// <param name="path">path of file to read from</param>
		[DllImport(KERNEL_DLL_NAME, CharSet = CharSet.Ansi)]
		internal static extern ReadFileEnum hostFromFile(IntPtr a, long N, string path);

		/// <summary>
		/// Get file's length in bytes
		/// </summary>
		/// <param name="N">output length of file in bytes</param>
		/// <param name="path">path of file to read from</param>
		[DllImport(KERNEL_DLL_NAME, CharSet = CharSet.Ansi)]
		internal static extern ReadFileEnum hostFromFileGetSize(ref long N, string path);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
	}
}
