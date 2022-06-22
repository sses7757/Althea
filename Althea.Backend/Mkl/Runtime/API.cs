using System.Text;


namespace Althea.Backend.Mkl
{
	// Ignore Spelling: Xeon
	/// <summary>
	/// The enum for instruction sets of MKL.
	/// </summary>
	public enum Instruction
	{
		/// <summary>
		/// Intel® Streaming SIMD Extensions 4.2 (Intel® SSE4.2).
		/// </summary>
		SSE_42 = 0,
		/// <summary>
		/// Intel® Advanced Vector Extensions (Intel® AVX).
		/// </summary>
		AVX = 1,
		/// <summary>
		/// Intel® Advanced Vector Extensions 2 (Intel® AVX2).
		/// </summary>
		AVX2 = 2,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) on Intel® Xeon Phi™ processors.
		/// </summary>
		AVX512_MIC = 3,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) on Intel® Xeon processors.
		/// </summary>
		AVX512 = 4,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) with support for Vector Neural Network Instructions on Intel® Xeon Phi™ processors.
		/// </summary>
		AVX512_MIC_E1 = 5,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) with support for Vector Neural Network Instructions.
		/// </summary>
		AVX512_E1 = 6,
	}

	/// <summary>
	/// The static class for static global methods and properties of MKL runtime.
	/// </summary>
	public static class Runtime
	{
		/// <summary>
		/// Get the MKL version
		/// </summary>
		/// <returns>The MKL's major and minor version</returns>
		public static (int major, int minor) GetDriverVersion()
		{
			const int STR_LEN = 198;
			StringBuilder sb = new(STR_LEN);
			NativeMethods.MKL_Get_Version_String(sb, STR_LEN);
			string s = sb.ToString();
			int versionStart = s.IndexOf("Version") + "Version".Length + 1;
			s = s[versionStart..s.IndexOf("Product")];
			var ss = s.Split('.');
			return (Convert.ToInt32(ss[0]), Convert.ToInt32(ss[1]));
		}

		/// <summary>
		/// Get or set the maximum number of threads used by the MKL
		/// </summary>
		public static int NumberOfThreads {
			get => NativeMethods.MKL_Get_Max_Threads();
			set {
				if (value > 0 && value <= Environment.ProcessorCount)
					NativeMethods.MKL_Set_Num_Threads(value);
				else
					throw new ArgumentOutOfRangeException(nameof(value), value, Resources.ParameterError.InvalidValue);
			}
		}


		private static bool? _verbose = null;

		/// <summary>
		/// Get or set the verbose mode of MKL
		/// </summary>
		public static bool Verbose {
			get {
				if (_verbose.HasValue)
					return _verbose.Value;
				_ = NativeMethods.MKL_Verbose(0);
				_verbose = false;
				return false;
			}
			set {
				_ = NativeMethods.MKL_Verbose(value ? 1 : 0);
				_verbose = value;
			}
		}


		private static Instruction? _instrction = null;
		
		/// <summary>
		/// Get or set the instruction set(s) used by the MKL
		/// </summary>
		public static Instruction Instruction {
			get {
				if (_instrction.HasValue)
					return _instrction.Value;
				int err = NativeMethods.MKL_Enable_Instructions(Instruction.AVX512);
				if (err != 0)
				{
					_instrction = Instruction.AVX512;
					return _instrction.Value;
				}
				err = NativeMethods.MKL_Enable_Instructions(Instruction.AVX2);
				if (err != 0)
				{
					_instrction = Instruction.AVX2;
					return _instrction.Value;
				}
				err = NativeMethods.MKL_Enable_Instructions(Instruction.AVX);
				if (err != 0)
				{
					_instrction = Instruction.AVX;
					return _instrction.Value;
				}
				err = NativeMethods.MKL_Enable_Instructions(Instruction.SSE_42);
				if (err != 0)
				{
					_instrction = Instruction.SSE_42;
					return _instrction.Value;
				}
				throw new InvalidOperationException();
			}
			set {
				int err = NativeMethods.MKL_Enable_Instructions(value);
				if (err == 0)
					throw new NotSupportedException();
				_instrction = value;
			}
		}
	}
}
