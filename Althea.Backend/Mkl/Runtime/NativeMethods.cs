using System.Runtime.InteropServices;
using System.Text;


namespace Althea.Backend.Mkl;

/// <summary>
/// The native methods for MKL runtime API
/// </summary>
public static class NativeMethods
{
	/// <summary>
	/// The MKL Runtime library name
	/// </summary>
	public const string MKL_DLL_NAME = "mkl_rt";

	/// <summary>
	/// The custom BLAS library name
	/// </summary>
	public const string CUSTOM_DLL_NAME = "ExtendBlasTBB";

	/// <summary>
	/// Set number of threads used by MKL library
	/// </summary>
	/// <param name="nt">number of threads to use</param>
	[DllImport(MKL_DLL_NAME)]
	internal static extern void MKL_Set_Num_Threads(int nt);

	/// <summary>
	/// Get the number of threads used by MKL library
	/// </summary>
	/// <returns>number of threads used</returns>
	[DllImport(MKL_DLL_NAME)]
	internal static extern int MKL_Get_Max_Threads();

	/// <summary>
	/// Terminates Intel® MKL execution environment and frees resources allocated by the library.
	/// </summary>
	[DllImport(MKL_DLL_NAME)]
	internal static extern void MKL_Finalize();

	/// <summary>
	/// Returns the Intel® MKL version in a character string.
	/// </summary>
	/// <param name="str">the input and output string</param>
	/// <param name="len">length of <paramref name="str"/></param>
	[DllImport(MKL_DLL_NAME, CharSet = CharSet.Ansi)]
	internal static extern void MKL_Get_Version_String(StringBuilder str, int len);

	/// <summary>
	/// Enables dispatching for new Intel® architectures or restricts the set of Intel® instruction sets available for dispatching.
	/// </summary>
	/// <param name="isa">The latest Intel® instruction-set architecture (ISA) for Intel® MKL to dispatch.</param>
	[DllImport(MKL_DLL_NAME)]
	internal static extern int MKL_Enable_Instructions(Instruction isa);

	/// <summary>
	/// Enables or disables Intel® MKL Verbose mode.
	/// </summary>
	/// <param name="enable">Desired state of the Intel® MKL Verbose mode. Indicates whether printing Intel® MKL function call information should be turned on or off.Possible values:
	/// <list type="bullet"><item><description>0 - disable the Verbose mode.</description></item>
	/// <item><description>1 - enable the Verbose mode.</description></item></list>
	/// </param>
	/// <returns>If the requested operation completed successfully, contains previous state of the verbose mode. If the function failed to complete the operation because of an incorrect input parameter, returns -1</returns>
	[DllImport(MKL_DLL_NAME)]
	internal static extern int MKL_Verbose(int enable);

	/// <summary>
	/// Write output in Intel® MKL Verbose mode to a file.
	/// </summary>
	/// <param name="path">Name of file. Specify the complete path of the output file.</param>
	/// <returns>If the file does not exist or cannot be opened, the write operation is unsuccessful. The function returns 1 and defaults to print to <see cref="System.Console.Out"/>; otherwise, it returns 0.</returns>
	[DllImport(MKL_DLL_NAME, CharSet = CharSet.Ansi)]
	internal static extern int MKL_Verbose_Output_File(string path);
}
