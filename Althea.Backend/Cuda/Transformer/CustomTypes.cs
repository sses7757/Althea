using System.Diagnostics;
using System.Runtime.CompilerServices;

using Althea.Backend.Cuda.Transformer;


namespace Althea.Backend.Cuda
{
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaFftStatus"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaFftStatus"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Check(this CudaFftStatus err)
		{
			if (err == CudaFftStatus.NotSupported)
				return false;
			if (err != CudaFftStatus.Success)
				throw new StatusException(err, new StackTrace(0));
			return true;
		}
	}
}


namespace Althea.Backend.Cuda.Transformer
{
	#region error
	/// <summary>
	/// The returns status of cuFFT API calls
	/// </summary>
	public enum CudaFftStatus
	{
		/// <summary>
		/// The cuFFT operation was successful
		/// </summary>
		Success = 0,
		/// <summary>
		/// cuFFT was passed an invalid plan handle
		/// </summary>
		InvalidPlan = 1,
		/// <summary>
		/// cuFFT failed to allocate GPU or CPU memory
		/// </summary>
		AllocFailed = 2,
		/// <summary>
		/// cuFFT was passed an invalid data type
		/// </summary>
		[Obsolete("No longer used")]
		InvalidType = 3,
		/// <summary>
		/// User specified an invalid pointer or parameter
		/// </summary>
		InvalidValue = 4,
		/// <summary>
		/// Driver or internal cuFFT library error
		/// </summary>
		InternalError = 5,
		/// <summary>
		/// Failed to execute an FFT on the GPU
		/// </summary>
		ExecFailed = 6,
		/// <summary>
		/// The cuFFT library failed to initialize
		/// </summary>
		SetupFailed = 7,
		/// <summary>
		/// User specified an invalid transform size
		/// </summary>
		InvalidSize = 8,
		/// <summary>
		/// cuFFT was passed an unaligned array
		/// </summary>
		[Obsolete("No longer used")]
		UnalignedData = 9,
		/// <summary>
		/// Missing parameters in call
		/// </summary>
		IncompleteParameterList = 10,
		/// <summary>
		/// Execution of a plan was on different GPU than plan creation
		/// </summary>
		InvalidDevice = 11,
		/// <summary>
		/// Internal plan database error
		/// </summary>
		ParseError = 12,
		/// <summary>
		/// No workspace has been provided prior to plan execution
		/// </summary>
		NoWorkspace = 13,
		/// <summary>
		/// Function does not implement functionality for parameters given
		/// </summary>
		NotImplemented = 14,
		/// <summary>
		/// Used in previous versions
		/// </summary>
		LicenseError = 15,
		/// <summary>
		/// Operation is not supported for parameters given
		/// </summary>
		NotSupported = 16,
	}
	#endregion

	#region other enum
	internal enum FftType
	{
		R2C = 0x2a, // Real to complex (interleaved)
		C2R = 0x2c, // Complex (interleaved) to real
		C2C = 0x29, // Complex to complex (interleaved)
		D2Z = 0x6a, // Double to double-complex (interleaved)
		Z2D = 0x6c, // Double-complex (interleaved) to double
		Z2Z = 0x69 // Double-complex to double-complex (interleaved)
	}

	internal enum FftDirection
	{
		Forward = -1,
		Backward = 1,
	}

	internal enum DataType
	{
		R_16F = 2, // 16 bit real
		C_16F = 6, // 16 bit complex
		R_32F = 0, // 32 bit real
		C_32F = 4, // 32 bit complex
		R_64F = 1, // 64 bit real
		C_64F = 5, // 64 bit complex
		R_8I = 3, // 8 bit real as a signed integer
		C_8I = 7, // 8 bit complex as a pair of signed integers
		R_8U = 8, // 8 bit real as a signed integer
		C_8U = 9 // 8 bit complex as a pair of signed integers
	}

	internal static class Conversion
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static DataType ToCuda(this Numerics.DataType type) => type switch
		{
			Numerics.DataType.RealFloat16 => DataType.R_16F,
			Numerics.DataType.RealFloat32 => DataType.R_32F,
			Numerics.DataType.RealFloat64 => DataType.R_64F,
			Numerics.DataType.ComplexFloat16 => DataType.C_16F,
			Numerics.DataType.ComplexFloat32 => DataType.C_32F,
			Numerics.DataType.ComplexFloat64 => DataType.C_64F,
			Numerics.DataType.RealInt8 => DataType.R_8I,
			Numerics.DataType.RealUInt8 => DataType.R_8U,
			Numerics.DataType.ComplexInt8 => DataType.C_8I,
			Numerics.DataType.ComplexUInt8 => DataType.C_8U,
			_ => (DataType)(-1)
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static DataType ChangeComplex(this DataType type) => type switch
		{
			DataType.R_16F => DataType.C_16F,
			DataType.C_16F => DataType.R_16F,
			DataType.R_32F => DataType.C_32F,
			DataType.C_32F => DataType.R_32F,
			DataType.R_64F => DataType.C_64F,
			DataType.C_64F => DataType.R_64F,
			DataType.R_8I => DataType.C_8I,
			DataType.C_8I => DataType.R_8I,
			DataType.R_8U => DataType.C_8U,
			DataType.C_8U => DataType.R_8U,
			_ => (DataType)(-1)
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static DataType ToComplex(this DataType type) => type switch
		{
			DataType.R_16F or DataType.C_16F => DataType.C_16F,
			DataType.R_32F or DataType.C_32F => DataType.C_32F,
			DataType.R_64F or DataType.C_64F => DataType.C_64F,
			DataType.R_8I or DataType.C_8I => DataType.C_8I,
			DataType.R_8U or DataType.C_8U => DataType.C_8U,
			_ => (DataType)(-1)
		};
	}
	#endregion
}
