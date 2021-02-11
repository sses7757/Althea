using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Althea.Helpers
{
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

		internal PrintSettings(bool _)
		{
			Precision = 8; ArrayLength = 40; MatrixRow = 20; MatrixColumn = 5;
		}

		/// <summary>
		/// Create a new <see cref="PrintSettings"/> with previous settings. The non-positive new value(s) are inherited from <paramref name="previousSettings"/>
		/// </summary>
		public PrintSettings(PrintSettings previousSettings, int precision = 0, int arrayLength = 0, int matrixRow = 0, int matrixColumn = 0)
		{
			Precision = precision > 0 ? precision : previousSettings.Precision;
			ArrayLength = arrayLength > 0 ? arrayLength : previousSettings.ArrayLength;
			MatrixRow = matrixRow > 0 ? matrixRow : previousSettings.MatrixRow;
			MatrixColumn = matrixColumn > 0 ? matrixColumn : previousSettings.MatrixColumn;
		}

		[JsonConstructor]
		internal PrintSettings(int precision, int arrayLength, int matrixRow, int matrixColumn)
		{
			Precision = precision; ArrayLength = arrayLength; MatrixRow = matrixRow; MatrixColumn = matrixColumn;
		}
	}

	/// <summary>
	/// The static class for global settings
	/// </summary>
	public static partial class Settings
	{
		#region class for settings
		internal struct ImplementationSettings
		{
			public bool DisposeNotCurrentImplementation;

			public Type Storage, LinearAlgebra, TensorAlgebra, Statistics, Solver;

			internal ImplementationSettings(bool _)
			{
				DisposeNotCurrentImplementation = true;
				Backend.ISetBackend impls = new Backend.CSharp.CSharpImplementations();
				Storage = impls.StorageImplementation;
				LinearAlgebra = impls.LinearAlgebraImplementation;
				TensorAlgebra = impls.TensorAlgebraImplementation;
				Statistics = impls.StatisticsImplementation;
				Solver = impls.SolverImplementation;
			}

			[JsonConstructor]
			internal ImplementationSettings(bool disposeNotCurrentImplementation, string storage, string linearAlgebra, string tensorAlgebra, string statistics, string solver)
			{
				DisposeNotCurrentImplementation = disposeNotCurrentImplementation;
				Backend.ISetBackend impls = new Backend.CSharp.CSharpImplementations();
				try
				{
					Storage = Type.GetType(storage) ?? impls.StorageImplementation;
				}
				catch (Exception)
				{
					Storage = impls.StorageImplementation;
				}

				try
				{
					LinearAlgebra = Type.GetType(linearAlgebra) ?? impls.LinearAlgebraImplementation;
				}
				catch (Exception)
				{
					LinearAlgebra = impls.LinearAlgebraImplementation;
				}

				try
				{
					TensorAlgebra = Type.GetType(tensorAlgebra) ?? impls.TensorAlgebraImplementation;
				}
				catch (Exception)
				{
					TensorAlgebra = impls.TensorAlgebraImplementation;
				}

				try
				{
					Statistics = Type.GetType(statistics) ?? impls.StatisticsImplementation;
				}
				catch (Exception)
				{
					Statistics = impls.StatisticsImplementation;
				}

				try
				{
					Solver = Type.GetType(solver) ?? impls.SolverImplementation;
				}
				catch (Exception)
				{
					Solver = impls.SolverImplementation;
				}
			}
		}

		internal struct JsonSettings
		{
			public LogSettings LogSettings;

			public PrintSettings PrintSettings;

			public ImplementationSettings ImplementationSettings;

			internal JsonSettings(bool _)
			{
				LogSettings = new LogSettings(false);
				PrintSettings = new PrintSettings(false);
				ImplementationSettings = new ImplementationSettings(false);
			}

			[JsonConstructor]
			internal JsonSettings(LogSettings logSettings, PrintSettings printSettings, ImplementationSettings implementationSettings)
			{
				LogSettings = logSettings;
				PrintSettings = printSettings;
				ImplementationSettings = implementationSettings;
			}
		}

		internal static JsonSettings singletonSettings = default;
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

		/// <summary>
		/// Which memory / storage implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type StorageImplementation {
			get => singletonSettings.ImplementationSettings.Storage;
			set => singletonSettings.ImplementationSettings.Storage = Storage.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which linear algebra implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type LinearAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.LinearAlgebra;
			set => singletonSettings.ImplementationSettings.LinearAlgebra = LinearAlgebra.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which tensor algebra implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type TensorAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.TensorAlgebra;
			set => singletonSettings.ImplementationSettings.TensorAlgebra = TensorAlgebra.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which statistics implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type StatisticsImplementation {
			get => singletonSettings.ImplementationSettings.Statistics;
			set => singletonSettings.ImplementationSettings.Statistics = Statistics.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which general solver implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type SolverImplementation {
			get => singletonSettings.ImplementationSettings.Solver;
			set => singletonSettings.ImplementationSettings.Solver = Solver.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Set all back-end implementations at once
		/// </summary>
		/// <param name="backend">The <see cref="Backend.ISetBackend"/> used to set all back-ends</param>
		/// <return>Success or not. Some implementation may still be changed even if this returns false.</return>
		public static bool SetBackend(Backend.ISetBackend backend)
		{
			if (!backend.Available)
				return false;
			try
			{
				StorageImplementation = backend.StorageImplementation;
				LinearAlgebraImplementation = backend.LinearAlgebraImplementation;
				TensorAlgebraImplementation = backend.TensorAlgebraImplementation;
				StatisticsImplementation = backend.StatisticsImplementation;
				SolverImplementation = backend.SolverImplementation;
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion

		#region import and export
		private static readonly JsonSerializerOptions options = new JsonSerializerOptions
		{
			DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			IncludeFields = true,
			NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
			WriteIndented = true,
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
				singletonSettings = JsonSerializer.Deserialize<JsonSettings>(File.ReadAllText(fileName), options);
				// set default back-end
				SetBackend(new Backend.CSharp.CSharpImplementations());
				Storage.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.Storage);
				LinearAlgebra.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.LinearAlgebra);
				TensorAlgebra.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.TensorAlgebra);
				Statistics.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.Statistics);
				Solver.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.Solver);
				return true;
			}
			catch (Exception e)
			{
				singletonSettings = new JsonSettings(false);
				if (logError)
				{
					Log.Write(Resources.Other.ErrorOccur + e.Message, "Initialization of Settings", LogLevel.Error);
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
			SetBackend(new Backend.CSharp.CSharpImplementations());
			// CUDA implementations
			SetBackend(new Backend.Cuda.CudaImplementations());
			// MKL implementations, the real default implementation
			SetBackend(new Backend.Mkl.MklImplementations());
			// import at last
			Import(logError: true);
		}

		/// <summary>
		/// Export current settings to the file "Althea.json"
		/// </summary>
		public static void ExportSettings()
		{
			string json = JsonSerializer.Serialize(singletonSettings, options);
			File.WriteAllText(fileName, json);
		}
		#endregion
	}
}
