using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Althea.Helpers
{
	#region print setting
	/// <summary>
	/// The structure for print settings
	/// </summary>
	public record struct PrintSettings
	{
		/// <summary>
		/// The print precision
		/// </summary>
		public int Precision { get; internal set; }
		/// <summary>
		/// The maximum lines of vectors to print
		/// </summary>
		public int ArrayLength { get; internal set; }
		/// <summary>
		/// The maximum rows of matrices to print
		/// </summary>
		public int MatrixRow { get; internal set; }
		/// <summary>
		/// The maximum columns of matrices to print
		/// </summary>
		public int MatrixColumn { get; internal set; }
		/// <summary>
		/// Whether to print tensor as embedded matrices (like MATHEMATICA) or a list of matrices (like MATLAB)
		/// </summary>
		public bool MatrixFormTensor { get; internal set; }

		/// <summary>
		/// Create a <see cref="PrintSettings"/> with default settings
		/// </summary>
		public PrintSettings()
		{
			Precision = 8; ArrayLength = 40; MatrixRow = 20; MatrixColumn = 5; MatrixFormTensor = false;
		}
	}
	#endregion

	/// <summary>
	/// The static class for global settings
	/// </summary>
	/// <remarks>Notice that the settings are thread static (see <see cref="ThreadStaticAttribute"/>).</remarks>
	public static partial class Settings
	{
		#region class for settings
		internal record struct ImplementationSettings
		{
			public bool DisposeNotCurrentImplementation { get; set; }

			public Storage.IAbstractApi? Storage { get; set; }
			public LinearAlgebra.Dense.AbstractApi? LinearAlgebraDense { get; set; }
			public LinearAlgebra.Sparse.IAbstractApi? LinearAlgebraSparse { get; set; }
			public TensorAlgebra.Dense.AbstractApi? TensorAlgebraDense { get; set; }
			public TensorAlgebra.Sparse.AbstractApi? TensorAlgebraSparse { get; set; }
			public Random.AbstractApi? Random { get; set; }
			public Solver.AbstractApi? Solver { get; set; }

			public ImplementationSettings() : this(GetInternalBackend("CSharp"))
			{ }

			internal ImplementationSettings(ISetBackend impls)
			{
				DisposeNotCurrentImplementation = true;

				Althea.Storage.IAbstractApi.SetImplementation(impls.StorageImplementation);
				this.Storage = Althea.Storage.IAbstractApi.Current;

				LinearAlgebra.Dense.AbstractApi.SetImplementation(impls.DenseLinearAlgebraImplementation);
				this.LinearAlgebraDense = LinearAlgebra.Dense.AbstractApi.Current;

				LinearAlgebra.Sparse.IAbstractApi.SetImplementation(impls.SparseLinearAlgebraImplementation);
				this.LinearAlgebraSparse = LinearAlgebra.Sparse.IAbstractApi.Current;

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
				Storage.IAbstractApi? storage,
				LinearAlgebra.Dense.AbstractApi? linearAlgebraDense,
				LinearAlgebra.Sparse.IAbstractApi? linearAlgebraSparse,
				TensorAlgebra.Dense.AbstractApi? tensorAlgebraDense,
				TensorAlgebra.Sparse.AbstractApi? tensorAlgebraSparse,
				Random.AbstractApi? random,
				Solver.AbstractApi? solver)
			{
				DisposeNotCurrentImplementation = disposeNotCurrentImplementation;

				Althea.Storage.IAbstractApi.SetImplementation(storage);
				this.Storage = Althea.Storage.IAbstractApi.Current;

				LinearAlgebra.Dense.AbstractApi.SetImplementation(linearAlgebraDense);
				this.LinearAlgebraDense = LinearAlgebra.Dense.AbstractApi.Current;

				LinearAlgebra.Sparse.IAbstractApi.SetImplementation(linearAlgebraSparse);
				this.LinearAlgebraSparse = LinearAlgebra.Sparse.IAbstractApi.Current;

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
			public LogSettings LogSettings { get; set; }
			public PrintSettings PrintSettings { get; set; }
			public ImplementationSettings ImplementationSettings { get; set; }
			public int StackAllocLimit { get; set; }

			public JsonSettings()
			{
				this.LogSettings = new();
				this.PrintSettings = new();
				this.ImplementationSettings = new();
				this.StackAllocLimit = 32 * 1024;
			}
		}

		internal static volatile JsonSettings settings;
		#endregion

		#region print settings
		/// <summary>
		/// Get and set the whole <see cref="PrintSettings"/>
		/// </summary>
		public static PrintSettings PrintSetting {
			get => settings.PrintSettings;
			set => settings.PrintSettings = value;
		}

		/// <summary>
		/// Get and set how many digital numbers will be printed out for a supported data type
		/// </summary>
		public static int PrintPrecision {
			get => settings.PrintSettings.Precision;
			set => settings.PrintSettings = settings.PrintSettings with { Precision = value};
		}

		/// <summary>
		/// Get and set the maximum number of lines of printed vectors and sparse matrices
		/// </summary>
		public static int PrintArrayLength {
			get => settings.PrintSettings.ArrayLength;
			set => settings.PrintSettings = settings.PrintSettings with { ArrayLength = value };
		}
		/// <summary>
		/// Get and set the maximum number of rows of printed matrices
		/// </summary>
		public static int PrintMatrixRow {
			get => settings.PrintSettings.MatrixRow;
			set => settings.PrintSettings = settings.PrintSettings with { MatrixRow = value };
		}
		/// <summary>
		/// Get and set the maximum number of columns of printed matrices
		/// </summary>
		public static int PrintMatrixColumn {
			get => settings.PrintSettings.MatrixColumn;
			set => settings.PrintSettings = settings.PrintSettings with { MatrixColumn = value };
		}
		/// <summary>
		/// Get and set whether to print tensor as embedded matrices (like MATHEMATICA) or a list of matrices (like MATLAB)
		/// </summary>
		public static bool PrintTensorAsEmbeddedMatrices {
			get => settings.PrintSettings.MatrixFormTensor;
			set => settings.PrintSettings = settings.PrintSettings with { MatrixFormTensor = value };
		}
		#endregion

		#region other settings
		// Ignore Spelling: stackalloc
		/// <summary>
		/// Get and set the maximum size in bytes when using C# keyword "stackalloc" to reduce GC pressure. The default stack size of x64 C# program is 4MB, set a value larger than this may cause unexpected error(s).
		/// </summary>
		public static int StackAllocLimit {
			get => settings.StackAllocLimit;
			set => settings.StackAllocLimit = value;
		}
		#endregion

		#region implementation settings
		/// <summary>
		/// When a new implementation is indicated, whether elder ones will be disposed or not.<br/>
		/// If the value is true while 'UseRecentImplementation' is true, there may be creations and dispositions of implementation classes.<br/>
		/// If the value is false and some implementation classes maintains large memory blocks (such as handle of cuBLAS), they will be maintained so that there will be some memory loss.
		/// </summary>
		public static bool DisposeNotCurrentImplementation {
			get => settings.ImplementationSettings.DisposeNotCurrentImplementation;
			set => settings.ImplementationSettings = settings.ImplementationSettings with { DisposeNotCurrentImplementation = value };
		}

		private static bool CheckAllBackend(ISetBackend backend)
		{
			return	settings.ImplementationSettings.Storage?.GetType() == backend.StorageImplementation &&
					settings.ImplementationSettings.LinearAlgebraDense?.GetType() == backend.DenseLinearAlgebraImplementation &&
					settings.ImplementationSettings.LinearAlgebraSparse?.GetType() == backend.SparseLinearAlgebraImplementation &&
					settings.ImplementationSettings.TensorAlgebraDense?.GetType() == backend.DenseTensorAlgebraImplementation &&
					settings.ImplementationSettings.TensorAlgebraSparse?.GetType() == backend.SparseTensorAlgebraImplementation &&
					settings.ImplementationSettings.Random?.GetType() == backend.RandomImplementation &&
					settings.ImplementationSettings.Solver?.GetType() == backend.SolverImplementation;
		}
		private static bool CheckAnyBackend(ISetBackend backend)
		{
			return	settings.ImplementationSettings.Storage?.GetType() == backend.StorageImplementation ||
					settings.ImplementationSettings.LinearAlgebraDense?.GetType() == backend.DenseLinearAlgebraImplementation ||
					settings.ImplementationSettings.LinearAlgebraSparse?.GetType() == backend.SparseLinearAlgebraImplementation ||
					settings.ImplementationSettings.TensorAlgebraDense?.GetType() == backend.DenseTensorAlgebraImplementation ||
					settings.ImplementationSettings.TensorAlgebraSparse?.GetType() == backend.SparseTensorAlgebraImplementation ||
					settings.ImplementationSettings.Random?.GetType() == backend.RandomImplementation ||
					settings.ImplementationSettings.Solver?.GetType() == backend.SolverImplementation;
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
				settings.ImplementationSettings = new(backend);
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
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
				settings = new JsonSettings();
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
			settings = new();
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

			settings.ImplementationSettings.Storage = Storage.IAbstractApi.Current;
			var converter = Storage.IAbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			settings.ImplementationSettings.LinearAlgebraDense = LinearAlgebra.Dense.AbstractApi.Current;
			converter = LinearAlgebra.Dense.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			settings.ImplementationSettings.LinearAlgebraSparse = LinearAlgebra.Sparse.IAbstractApi.Current;
			converter = LinearAlgebra.Sparse.IAbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			settings.ImplementationSettings.TensorAlgebraDense = TensorAlgebra.Dense.AbstractApi.Current;
			converter = TensorAlgebra.Dense.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			settings.ImplementationSettings.TensorAlgebraSparse = TensorAlgebra.Sparse.AbstractApi.Current;
			converter = TensorAlgebra.Sparse.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			settings.ImplementationSettings.Random = Random.AbstractApi.Current;
			converter = Random.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			settings.ImplementationSettings.Solver = Solver.AbstractApi.Current;
			converter = Solver.AbstractApi.Current?.CurrentConverter;
			if (converter is not null)
				currentOptions.Converters.Add(converter);

			string json = JsonSerializer.Serialize(settings, currentOptions);
			File.WriteAllText(fileName, json);
		}
		#endregion

		#region extension methods
		private static ISetBackend GetInternalBackend(string name)
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
		#endregion
	}
}
