using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Althea.Helpers
{
	// TODO: thread static
	#region print setting
	/// <summary>
	/// The structure for print settings
	/// </summary>
	public readonly struct PrintSettings
	{
		/// <summary>
		/// The print precision
		/// </summary>
		public readonly int Precision;
		/// <summary>
		/// The maximum lines of vectors to print
		/// </summary>
		public readonly int ArrayLength;
		/// <summary>
		/// The maximum rows of matrices to print
		/// </summary>
		public readonly int MatrixRow;
		/// <summary>
		/// The maximum columns of matrices to print
		/// </summary>
		public readonly int MatrixColumn;
		/// <summary>
		/// Whether to print tensor as embedded matrices (like MATHEMATICA) or a list of matrices (like MATLAB)
		/// </summary>
		public readonly bool MatrixFormTensor;

		internal PrintSettings(bool _)
		{
			Precision = 8; ArrayLength = 40; MatrixRow = 20; MatrixColumn = 5; MatrixFormTensor = false;
		}

		/// <summary>
		/// Create a new <see cref="PrintSettings"/> with previous settings. The non-positive new value(s) are inherited from <paramref name="previousSettings"/>
		/// </summary>
		public PrintSettings(PrintSettings previousSettings, int precision = 0, int arrayLength = 0, int matrixRow = 0, int matrixColumn = 0, bool? matrixFormTensor = null)
		{
			Precision = precision > 0 ? precision : previousSettings.Precision;
			ArrayLength = arrayLength > 0 ? arrayLength : previousSettings.ArrayLength;
			MatrixRow = matrixRow > 0 ? matrixRow : previousSettings.MatrixRow;
			MatrixColumn = matrixColumn > 0 ? matrixColumn : previousSettings.MatrixColumn;
			MatrixFormTensor = matrixFormTensor ?? previousSettings.MatrixFormTensor;
		}

		[JsonConstructor]
		internal PrintSettings(int precision, int arrayLength, int matrixRow, int matrixColumn, bool matrixFormTensor)
		{
			Precision = precision; ArrayLength = arrayLength; MatrixRow = matrixRow; MatrixColumn = matrixColumn; MatrixFormTensor = matrixFormTensor;
		}
	}
	#endregion

	/// <summary>
	/// The static class for global settings
	/// </summary>
	public static partial class Settings
	{
		#region class for settings
		internal record ImplementationSettings
		{
			public bool DisposeNotCurrentImplementation { get; set; }

			public Storage.AbstractApi? Storage { get; set; }
			public LinearAlgebra.Dense.AbstractApi? LinearAlgebraDense { get; set; }
			public LinearAlgebra.Sparse.AbstractApi? LinearAlgebraSparse { get; set; }
			public TensorAlgebra.Dense.AbstractApi? TensorAlgebraDense { get; set; }
			public TensorAlgebra.Sparse.AbstractApi? TensorAlgebraSparse { get; set; }
			public Random.AbstractApi? Random { get; set; }
			public Solver.AbstractApi? Solver { get; set; }

			internal ImplementationSettings(ISetBackend impls)
			{
				DisposeNotCurrentImplementation = true;

				Althea.Storage.AbstractApi.SetImplementation(impls.StorageImplementation);
				this.Storage = Althea.Storage.AbstractApi.Current;

				LinearAlgebra.Dense.AbstractApi.SetImplementation(impls.DenseLinearAlgebraImplementation);
				this.LinearAlgebraDense = LinearAlgebra.Dense.AbstractApi.Current;

				LinearAlgebra.Sparse.AbstractApi.SetImplementation(impls.SparseLinearAlgebraImplementation);
				this.LinearAlgebraSparse = LinearAlgebra.Sparse.AbstractApi.Current;

				TensorAlgebra.Dense.AbstractApi.SetImplementation(impls.DenseTensorAlgebraImplementation);
				this.TensorAlgebraDense = TensorAlgebra.Dense.AbstractApi.Current;
				
				TensorAlgebra.Sparse.AbstractApi.SetImplementation(impls.SparseTensorAlgebraImplementation);
				this.TensorAlgebraSparse = TensorAlgebra.Sparse.AbstractApi.Current;

				Althea.Random.AbstractApi.SetImplementation(impls.RandomImplementation);
				this.Random = Althea.Random.AbstractApi.Current;

				Althea.Solver.AbstractApi.SetImplementation(impls.SolverImplementation);
				this.Solver = Althea.Solver.AbstractApi.Current;
			}

			[JsonConstructor]
			internal ImplementationSettings(bool disposeNotCurrentImplementation,
				Storage.AbstractApi? storage,
				LinearAlgebra.Dense.AbstractApi? linearAlgebraDense,
				LinearAlgebra.Sparse.AbstractApi? linearAlgebraSparse,
				TensorAlgebra.Dense.AbstractApi? tensorAlgebraDense,
				TensorAlgebra.Sparse.AbstractApi? tensorAlgebraSparse,
				Random.AbstractApi? random,
				Solver.AbstractApi? solver)
			{
				DisposeNotCurrentImplementation = disposeNotCurrentImplementation;

				Althea.Storage.AbstractApi.SetImplementation(storage);
				this.Storage = Althea.Storage.AbstractApi.Current;

				LinearAlgebra.Dense.AbstractApi.SetImplementation(linearAlgebraDense);
				this.LinearAlgebraDense = LinearAlgebra.Dense.AbstractApi.Current;

				LinearAlgebra.Sparse.AbstractApi.SetImplementation(linearAlgebraSparse);
				this.LinearAlgebraSparse = LinearAlgebra.Sparse.AbstractApi.Current;

				TensorAlgebra.Dense.AbstractApi.SetImplementation(tensorAlgebraDense);
				this.TensorAlgebraDense = TensorAlgebra.Dense.AbstractApi.Current;

				TensorAlgebra.Sparse.AbstractApi.SetImplementation(tensorAlgebraSparse);
				this.TensorAlgebraSparse = TensorAlgebra.Sparse.AbstractApi.Current;

				Althea.Random.AbstractApi.SetImplementation(random);
				this.Random = Althea.Random.AbstractApi.Current;

				Althea.Solver.AbstractApi.SetImplementation(solver);
				this.Solver = Althea.Solver.AbstractApi.Current;
			}
		}

		internal record JsonSettings
		{
			public LogSettings LogSettings;

			public PrintSettings PrintSettings;

			public ImplementationSettings ImplementationSettings;

			public int StackAllocLimit;

			internal JsonSettings()
			{
				LogSettings = new LogSettings();
				PrintSettings = new PrintSettings(false);
				ImplementationSettings = new ImplementationSettings(GetInternalBackend("CSharp"));
				StackAllocLimit = 32768; // 32 KiB
			}

			[JsonConstructor]
			internal JsonSettings(LogSettings logSettings, PrintSettings printSettings, ImplementationSettings implementationSettings, int stackAllocLimit)
			{
				LogSettings = logSettings;
				PrintSettings = printSettings;
				ImplementationSettings = implementationSettings;
				StackAllocLimit = stackAllocLimit;
			}
		}

		internal static JsonSettings singletonSettings = new();
		#endregion

		#region print settings
		/// <summary>
		/// Get and set the whole <see cref="PrintSettings"/>
		/// </summary>
		public static PrintSettings PrintSetting {
			get => singletonSettings.PrintSettings;
			set => singletonSettings.PrintSettings = value;
		}

		/// <summary>
		/// Get and set how many digital numbers will be printed out for a supported data type
		/// </summary>
		public static int PrintPrecision {
			get => singletonSettings.PrintSettings.Precision;
			set => singletonSettings.PrintSettings = new PrintSettings(singletonSettings.PrintSettings, precision: value);
		}

		/// <summary>
		/// Get and set the maximum number of lines of printed vectors and sparse matrices
		/// </summary>
		public static int PrintArrayLength {
			get => singletonSettings.PrintSettings.ArrayLength;
			set => singletonSettings.PrintSettings = new PrintSettings(singletonSettings.PrintSettings, arrayLength: value);
		}
		/// <summary>
		/// Get and set the maximum number of rows of printed matrices
		/// </summary>
		public static int PrintMatrixRow {
			get => singletonSettings.PrintSettings.MatrixRow;
			set => singletonSettings.PrintSettings = new PrintSettings(singletonSettings.PrintSettings, matrixRow: value);
		}
		/// <summary>
		/// Get and set the maximum number of columns of printed matrices
		/// </summary>
		public static int PrintMatrixColumn {
			get => singletonSettings.PrintSettings.MatrixColumn;
			set => singletonSettings.PrintSettings = new PrintSettings(singletonSettings.PrintSettings, matrixColumn: value);
		}
		/// <summary>
		/// Get and set whether to print tensor as embedded matrices (like MATHEMATICA) or a list of matrices (like MATLAB)
		/// </summary>
		public static bool PrintTensorAsEmbeddedMatrices {
			get => singletonSettings.PrintSettings.MatrixFormTensor;
			set => singletonSettings.PrintSettings = new PrintSettings(singletonSettings.PrintSettings, matrixFormTensor: value);
		}
		#endregion

		#region other settings
		// Ignore Spelling: stackalloc
		/// <summary>
		/// Get and set the maximum size in bytes when using C# keyword "stackalloc" to reduce GC pressure. The default stack size of x64 C# program is 4MB, set a value larger than this may cause unexpected error(s).
		/// </summary>
		public static int StackAllocLimit {
			get => singletonSettings.StackAllocLimit;
			set => singletonSettings.StackAllocLimit = value;
		}
		#endregion

		#region implementation settings
		/// <summary>
		/// When a new implementation is indicated, whether elder ones will be disposed or not.<br/>
		/// If the value is true while 'UseRecentImplementation' is true, there may be creations and dispositions of implementation classes.<br/>
		/// If the value is false and some implementation classes maintains large memory blocks (such as handle of cuBLAS), they will be maintained so that there will be some memory loss.
		/// </summary>
		public static bool DisposeNotCurrentImplementation {
			get => singletonSettings.ImplementationSettings.DisposeNotCurrentImplementation;
			set => singletonSettings.ImplementationSettings.DisposeNotCurrentImplementation = value;
		}

		private static bool CheckAllBackend(ISetBackend backend)
		{
			return	singletonSettings.ImplementationSettings.Storage?.GetType() == backend.StorageImplementation &&
					singletonSettings.ImplementationSettings.LinearAlgebraDense?.GetType() == backend.DenseLinearAlgebraImplementation &&
					singletonSettings.ImplementationSettings.LinearAlgebraSparse?.GetType() == backend.SparseLinearAlgebraImplementation &&
					singletonSettings.ImplementationSettings.TensorAlgebraDense?.GetType() == backend.DenseTensorAlgebraImplementation &&
					singletonSettings.ImplementationSettings.TensorAlgebraSparse?.GetType() == backend.SparseTensorAlgebraImplementation &&
					singletonSettings.ImplementationSettings.Random?.GetType() == backend.RandomImplementation &&
					singletonSettings.ImplementationSettings.Solver?.GetType() == backend.SolverImplementation;
		}
		private static bool CheckAnyBackend(ISetBackend backend)
		{
			return	singletonSettings.ImplementationSettings.Storage?.GetType() == backend.StorageImplementation ||
					singletonSettings.ImplementationSettings.LinearAlgebraDense?.GetType() == backend.DenseLinearAlgebraImplementation ||
					singletonSettings.ImplementationSettings.LinearAlgebraSparse?.GetType() == backend.SparseLinearAlgebraImplementation ||
					singletonSettings.ImplementationSettings.TensorAlgebraDense?.GetType() == backend.DenseTensorAlgebraImplementation ||
					singletonSettings.ImplementationSettings.TensorAlgebraSparse?.GetType() == backend.SparseTensorAlgebraImplementation ||
					singletonSettings.ImplementationSettings.Random?.GetType() == backend.RandomImplementation ||
					singletonSettings.ImplementationSettings.Solver?.GetType() == backend.SolverImplementation;
		}

		/// <summary>
		/// Set all back-end implementations at once
		/// </summary>
		/// <param name="backend">The <see cref="ISetBackend"/> used to set all back-ends</param>
		/// <return>Success or not. Some implementation may still be changed even if this returns false.</return>
		public static bool SetBackend(ISetBackend backend)
		{
			if (backend is null || !backend.Available)
				return false;
			try
			{
				singletonSettings.ImplementationSettings = new(backend);
				return CheckAllBackend(backend);
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion

		#region import and export
		private static readonly JsonSerializerOptions options = new()
		{
			DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			IncludeFields = true,
			NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
			WriteIndented = true,
			IgnoreReadOnlyProperties = true,
		};

		private static string fileName = "Althea.json";

		/// <summary>
		/// Import the settings from the JSON configuration file.
		/// </summary>
		/// <returns>Success or not.</returns>
		public static bool Import(bool logError = false)
		{
			try
			{
				var settings = JsonSerializer.Deserialize<JsonSettings>(File.ReadAllText(fileName), options);
				return CheckAnyBackend(GetInternalBackend("CSharp"));
			}
			catch (Exception e)
			{
				singletonSettings = new JsonSettings();
				if (logError)
				{
					Log.Write(Resources.Other.ErrorOccur + e.Message, level: LogLevel.Error);
					Log.Write(Resources.Other.UseDefault);
				}
				return false;
			}
		}

		/// <summary>
		/// Set the path of the JSON configuration file and <b>import</b> the settings of it.
		/// </summary>
		/// <param name="filePath">The file path to set</param>
		/// <returns>If <paramref name="filePath"/> is an existing file and the import succeeded, return true. Otherwise, return false.</returns>
		public static bool SetConfigFile(string filePath)
		{
			if (!File.Exists(filePath))
				return false;
			fileName = filePath;
			return Import(logError: false);
		}

		static Settings()
		{
			// set default implementations
			// C# implementations
			SetBackend(GetInternalBackend(@"CSharp"));
			// CUDA implementations
			SetBackend(GetInternalBackend(@"Cuda"));
			// MKL implementations, the real default implementation
			SetBackend(GetInternalBackend(@"Mkl"));
			// import at last
			Import(logError: true);
		}

		/// <summary>
		/// Export current settings to the file "Althea.json"
		/// </summary>
		public static void ExportSettings()
		{
			var currentOptions = new JsonSerializerOptions(options);

			singletonSettings.ImplementationSettings.Storage = Storage.AbstractApi.Current;
			var converter = Storage.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			singletonSettings.ImplementationSettings.LinearAlgebraDense = LinearAlgebra.Dense.AbstractApi.Current;
			converter = LinearAlgebra.Dense.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			singletonSettings.ImplementationSettings.LinearAlgebraSparse = LinearAlgebra.Sparse.AbstractApi.Current;
			converter = LinearAlgebra.Sparse.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			singletonSettings.ImplementationSettings.TensorAlgebraDense = TensorAlgebra.Dense.AbstractApi.Current;
			converter = TensorAlgebra.Dense.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			singletonSettings.ImplementationSettings.TensorAlgebraSparse = TensorAlgebra.Sparse.AbstractApi.Current;
			converter = TensorAlgebra.Sparse.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			singletonSettings.ImplementationSettings.Random = Random.AbstractApi.Current;
			converter = Random.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			singletonSettings.ImplementationSettings.Solver = Solver.AbstractApi.Current;
			converter = Solver.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			string json = JsonSerializer.Serialize(singletonSettings, currentOptions);
			File.WriteAllText(fileName, json);
		}
		#endregion

		#region extension methods
		internal static ISetBackend GetInternalBackend(string name)
		{
			if (Type.GetType($"Althea.Backend.{name}.{name}Implementations")?.GetConstructor(Type.EmptyTypes)?.Invoke(null) is not ISetBackend res)
				throw new InvalidOperationException();
			return res;
		}

		/// <summary>
		/// Check whether the given <paramref name="length"/> and an unmanaged type <typeparamref name="T"/> fits the <see cref="StackAllocLimit"/> or not.<br/>
		/// If the size is too large, array of <typeparamref name="T"/> will not be created and you shall <c>stackalloc <typeparamref name="T"/>[<paramref name="length"/>]</c> yourself.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="length">The desired length to allocate</param>
		/// <returns>The allocated C# array of given <paramref name="length"/> or null</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[]? CheckStackLimit<T>(this int length) where T : notnull
		{
			if (length * Unsafe.SizeOf<T>() > StackAllocLimit)
				return new T[length];
			else
				return null;
		}

		/// <summary>
		/// Check whether the given <paramref name="length"/> and an unmanaged type <typeparamref name="T"/> fits the <see cref="StackAllocLimit"/> or not.<br/>
		/// If the size is too large, array of <typeparamref name="T"/> will not be created and you shall <c>stackalloc <typeparamref name="T"/>[<paramref name="length"/>]</c> yourself.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="length">The desired length to allocate</param>
		/// <param name="sizeT">Output the size of <typeparamref name="T"/></param>
		/// <returns>The allocated C# array of given <paramref name="length"/> or null</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[]? CheckStackLimit<T>(this int length, out int sizeT) where T : notnull
		{
			sizeT = Unsafe.SizeOf<T>();
			if (length * sizeT > StackAllocLimit)
				return new T[length];
			else
				return null;
		}
		#endregion
	}
}
