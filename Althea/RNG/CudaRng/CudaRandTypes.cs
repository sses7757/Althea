namespace Althea.Rng.Cuda
{
	/// <summary>
	/// CURAND function call status types
	/// </summary>
	public enum Status
	{
		/// <summary>
		/// No errors.
		/// </summary>
		Success = 0,
		/// <summary>
		/// Header file and linked library version do not match.
		/// </summary>
		VersionMismatch = 100,
		/// <summary>
		/// Generator not initialized.
		/// </summary>
		NotInitialized = 101,
		/// <summary>
		/// Memory allocation failed.
		/// </summary>
		AllocationFailed = 102,
		/// <summary>
		/// Generator is wrong type.
		/// </summary>
		TypeError = 103,
		/// <summary>
		/// Argument out of range.
		/// </summary>
		OutOfRange = 104,
		/// <summary>
		/// Length requested is not a multiple of dimension.
		/// </summary>
		LengthNotMultiple = 105,
		/// <summary>
		/// GPU does not have double precision required by MRG32k3a.
		/// </summary>
		DoublePrecisionRequired = 106,
		/// <summary>
		/// Kernel launch failure.
		/// </summary>
		LaunchFailure = 201,
		/// <summary>
		/// Preexisting failure on library entry.
		/// </summary>
		PreexistingFailure = 202,
		/// <summary>
		/// Initialization of CUDA failed.
		/// </summary>
		InitializationFailed = 203,
		/// <summary>
		/// Architecture mismatch, GPU does not support requested feature.
		/// </summary>
		ArchMismatch = 204,
		/// <summary>
		/// Internal library error.
		/// </summary>
		InternalError = 999
	}

	/// <summary>
	/// CURAND generator types
	/// </summary>
	public enum GeneratorType
	{
		/// <summary>
		/// 
		/// </summary>
		Test = 0,
		/// <summary>
		/// Default pseudo-random generator.
		/// </summary>
		PseudoDefault = 100,
		/// <summary>
		/// XORWOW pseudo-random generator.
		/// </summary>
		PseudoXORWOW = 101,
		/// <summary>
		/// MRG32k3a pseudo-random generator.
		/// </summary>
		PseudoMRG32K3A = 121,
		/// <summary>
		/// Mersenne Twister pseudo-random generator.
		/// </summary>
		PseudoMTGP32 = 141,
		/// <summary>
		/// Mersenne Twister MT19937 pseudo-random generator.
		/// </summary>
		PseudoMT19937 = 142,
		/// <summary>
		/// PseudoPhilox4_32_10 quasi-random generator.
		/// </summary>
		PseudoPhilox4_32_10 = 161,
		/// <summary>
		/// Default quasi-random generator.
		/// </summary>
		QuasiDefault = 200,
		/// <summary>
		/// Sobol32 quasi-random generator.
		/// </summary>
		QuasiSobol32 = 201,
		/// <summary>
		/// Scrambled Sobol32 quasi-random generator.
		/// </summary>
		QuasiScrambledSobol32 = 202,
		/// <summary>
		/// Sobol64 quasi-random generator.
		/// </summary>
		QuasiSobol64 = 203,
		/// <summary>
		/// Scrambled Sobol64 quasi-random generator.
		/// </summary>
		QuasiScrambledSobol64 = 204
	}

	/// <summary>
	/// CURAND orderings of results in memory
	/// </summary>
	public enum Ordering
	{
		/// <summary>
		/// Best ordering for pseudo-random results.
		/// </summary>
		PseudoBest = 100,
		/// <summary>
		/// Specific default 4096 thread sequence for pseudo-random results.
		/// </summary>
		PseudoDefault = 101,
		/// <summary>
		/// Specific seeding pattern for fast lower quality pseudo-random results.
		/// </summary>
		Pseudoeseded = 102,
		/// <summary>
		/// Specific n-dimensional ordering for quasi-random results.
		/// </summary>
		QuasiDefault = 201
	}

}
