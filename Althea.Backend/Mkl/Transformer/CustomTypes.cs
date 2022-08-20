using System.Runtime.CompilerServices;

using Althea.Backend.Mkl.Transformer;


namespace Althea.Backend.Mkl.Transformer
{
	/// <summary>
	/// The returned status of the MKL discrete Fourier transform library APIs
	/// </summary>
	public enum MklDftError : long
	{
		/// <summary>
		/// No error
		/// </summary>
		Success,
		/// <summary>
		/// Memory related error
		/// </summary>
		MemoryError,
		/// <summary>
		/// The given configuration is invalid 
		/// </summary>
		InvalidConfiguration,
		/// <summary>
		/// The given configuration is inconsistent 
		/// </summary>
		InconsistentConfiguration,
		/// <summary>
		/// Multi-thread related error
		/// </summary>
		MultiThreadError,
		/// <summary>
		/// The given descriptor is invalid 
		/// </summary>
		BadDescriptor,
		/// <summary>
		/// The requested operation is not implemented
		/// </summary>
		Unimplemented,
		/// <summary>
		/// MKL internal error
		/// </summary>
		InternalError,
		/// <summary>
		/// Number of threads related error
		/// </summary>
		NumberOfThreadsError,
		/// <summary>
		/// The length of 1D DFT exceeds <see cref="uint.MaxValue"/>
		/// </summary>
		LengthExceedsInt32,
	}

	/// <summary>
	/// The enum for MKL discrete Fourier transform complex type
	/// </summary>
	public enum MklDftDomain
	{
		/// <summary>
		/// The complex data domain
		/// </summary>
		Complex = 32,
		/// <summary>
		/// The real data domain
		/// </summary>
		Real = 33,
		/// <summary>
		/// The conjugate-even data domain, NOT implemented by MKL
		/// </summary>
		ConjugateEven = 34,
	}

	/// <summary>
	/// The enum for MKL discrete Fourier transform data type (precision)
	/// </summary>
	public enum MklDftPrecision
	{
		/// <summary>
		/// The <see cref="float"/> or <see cref="Complex{T}"/> of <see cref="float"/>
		/// </summary>
		Single = 35,
		/// <summary>
		/// The <see cref="double"/> or <see cref="Complex{T}"/> of <see cref="double"/>
		/// </summary>
		Double = 36,
	}

	/// <summary>
	/// The enum for MKL discrete Fourier transform storage type
	/// </summary>
	public enum MklDftStorage
	{
		/// <summary>
		/// The complex values are stored as it is
		/// </summary>
		ComplexAsComplex = 39,
		/// <summary>
		/// The complex values are stored as real ones (only real parts)
		/// </summary>
		ComplexAsReal = 40,
		/// <summary>
		/// The real values are stored as complex ones
		/// </summary>
		RealAsComplex = 41,
		/// <summary>
		/// The real values are stored as it is
		/// </summary>
		RealAsReal = 42,
	}

	/// <summary>
	/// The enum for MKL discrete Fourier transform placement
	/// </summary>
	public enum MklDftPlacement
	{
		/// <summary>
		/// The output overwrites the input
		/// </summary>
		InPlace = 43,
		/// <summary>
		/// The output dose not overwrite the input and they are different
		/// </summary>
		OutOfPlace = 44,
	}
}

namespace Althea.Backend.Mkl
{
	/// <summary>
	/// The static class for checking <see cref="MklDftError"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check the given <see cref="MklDftError"/>
		/// </summary>
		/// <param name="status">The given <see cref="MklDftError"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this MklDftError status)
		{
			if (status != MklDftError.Success)
				throw new StatusException(status);
		}
	}
}