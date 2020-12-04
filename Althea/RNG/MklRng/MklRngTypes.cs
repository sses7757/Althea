using System;
using System.Collections.Generic;
using System.Text;

namespace Althea.Rng.Mkl
{
	/// <summary>
	/// MKL library status enum
	/// </summary>
	public enum Status
	{
		/// <summary>
		/// No error
		/// </summary>
		VSL_STATUS_OK = 0,


#pragma warning disable CS1591
		VSL_ERROR_FEATURE_NOT_IMPLEMENTED = -1,
		VSL_ERROR_UNKNOWN = -2,
		VSL_ERROR_BADARGS = -3,
		VSL_ERROR_MEM_FAILURE = -4,
		VSL_ERROR_NULL_PTR = -5,
		VSL_ERROR_CPU_NOT_SUPPORTED = -6,


		VSL_RNG_ERROR_INVALID_BRNG_INDEX = -1000,
		VSL_RNG_ERROR_LEAPFROG_UNSUPPORTED = -1002,
		VSL_RNG_ERROR_SKIPAHEAD_UNSUPPORTED = -1003,
		VSL_RNG_ERROR_BRNGS_INCOMPATIBLE = -1005,
		VSL_RNG_ERROR_BAD_STREAM = -1006,
		VSL_RNG_ERROR_BRNG_TABLE_FULL = -1007,
		VSL_RNG_ERROR_BAD_STREAM_STATE_SIZE = -1008,
		VSL_RNG_ERROR_BAD_WORD_SIZE = -1009,
		VSL_RNG_ERROR_BAD_NSEEDS = -1010,
		VSL_RNG_ERROR_BAD_NBITS = -1011,
		VSL_RNG_ERROR_QRNG_PERIOD_ELAPSED = -1012,
		VSL_RNG_ERROR_LEAPFROG_NSTREAMS_TOO_BIG = -1013,
		VSL_RNG_ERROR_BRNG_NOT_SUPPORTED = -1014,

		/// <summary>
		/// non-deterministic stream not supported
		/// </summary>
		VSL_RNG_ERROR_NONDETERM_NOT_SUPPORTED = -1130,
		/// <summary>
		/// non-deterministic stream too many entries
		/// </summary>
		VSL_RNG_ERROR_NONDETERM_NRETRIES_EXCEEDED = -1131,

		/// <summary>
		/// ARS5 stream related errors
		/// </summary>
		VSL_RNG_ERROR_ARS5_NOT_SUPPORTED = -1140,

		VSL_RNG_ERROR_FILE_CLOSE = -1100,
		VSL_RNG_ERROR_FILE_OPEN = -1101,
		VSL_RNG_ERROR_FILE_WRITE = -1102,
		VSL_RNG_ERROR_FILE_READ = -1103,

		VSL_RNG_ERROR_BAD_FILE_FORMAT = -1110,
		VSL_RNG_ERROR_UNSUPPORTED_FILE_VER = -1111,

		VSL_RNG_ERROR_BAD_MEM_FORMAT = -1200,
#pragma warning restore CS1591
	}

	/// <summary>
	/// Random generator type enum
	/// </summary>
	public enum GeneratorType
	{
#pragma warning disable CS1591
		MCG31 = 1 << 20,
		R250 = 2 << 20,
		MRG32K3A = 3 << 20,
		MCG59 = 4 << 20,
		WH = 5 << 20,
		SOBOL = 6 << 20,
		NIEDERR = 7 << 20,
		MT19937 = 8 << 20,
		MT2203 = 9 << 20,
		IABSTRACT = 10 << 20,
		DABSTRACT = 11 << 20,
		SABSTRACT = 12 << 20,
		SFMT19937 = 13 << 20,
		NONDETERM = 14 << 20,
		ARS5 = 15 << 20,
		PHILOX4X32X10 = 16 << 20,
#pragma warning restore CS1591
	}

	/// <summary>
	/// Method to be used to generate the random numbers
	/// </summary>
	public enum Method
	{
		/// <summary>
		/// Standard methods
		/// </summary>
		Standard = 0,
		/// <summary>
		/// Accurate methods
		/// </summary>
		Accurate = 0 | 1 << 30,
	}
}
