using System;
using System.IO;
using System.Text.Json;


namespace Althea.Helpers
{
	/// <summary>
	/// The static class for global settings
	/// </summary>
	public static partial class Settings
	{
		#region class for settings
		internal struct JsonPrintSettings
		{
			public int Precision, ArrayLength, MatrixRow, MatrixColumn;
			
			internal JsonPrintSettings(bool _)
			{
				Precision = 8; ArrayLength = 40; MatrixRow = 20; MatrixColumn = 5;
			}
		}

		internal struct JsonImplementationSettings
		{
			////public bool UseRecentImplementation;

			public bool DisposeNotCurrentImplementation;

			public string Storage, LinearAlgebra, TensorAlgebra, Statistics, Solver;

			internal JsonImplementationSettings(bool _)
			{
				////UseRecentImplementation = true;
				DisposeNotCurrentImplementation = true;
				Storage = string.Empty;
				LinearAlgebra = string.Empty;
				TensorAlgebra = string.Empty;
				Statistics = string.Empty;
				Solver = string.Empty;
			}
		}

		internal struct JsonSettings
		{
			public JsonLogSettings LogSettings;

			public JsonPrintSettings PrintSettings;

			public JsonImplementationSettings ImplementationSettings;

			internal JsonSettings(bool _)
			{
				LogSettings = new JsonLogSettings(false);
				PrintSettings = new JsonPrintSettings(false);
				ImplementationSettings = new JsonImplementationSettings(false);
			}
		}

		internal static JsonSettings singletonSettings = default;
		#endregion

		#region print settings
		/// <summary>
		/// How many digital numbers will be printed out for a supported data type
		/// </summary>
		public static int PrintPrecision {
			get => singletonSettings.PrintSettings.Precision;
			set => singletonSettings.PrintSettings.Precision = value;
		}

		/// <summary>
		/// The maximum number of lines of printed vectors and sparse matrices
		/// </summary>
		public static int PrintArrayLength {
			get => singletonSettings.PrintSettings.ArrayLength;
			set => singletonSettings.PrintSettings.ArrayLength = value;
		}
		/// <summary>
		/// The maximum number of rows of printed matrices
		/// </summary>
		public static int PrintMatrixRow {
			get => singletonSettings.PrintSettings.MatrixRow;
			set => singletonSettings.PrintSettings.MatrixRow = value;
		}
		/// <summary>
		/// The maximum number of columns of printed matrices
		/// </summary>
		public static int PrintMatrixColumn {
			get => singletonSettings.PrintSettings.MatrixColumn;
			set => singletonSettings.PrintSettings.MatrixColumn = value;
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
		/// <exception cref="ArgumentNullException">If the given value is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the given value has no <see cref="Type.AssemblyQualifiedName"/></exception>
		public static Type? StorageImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.Storage);
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (Storage.AbstractApi.SetImplementation(value))
				{
					var v = value.AssemblyQualifiedName;
					if (v is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					singletonSettings.ImplementationSettings.Storage = v;
				}
			}
		}

		/// <summary>
		/// Which linear algebra implementation to use
		/// </summary>
		/// <exception cref="ArgumentNullException">If the given value is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the given value has no <see cref="Type.AssemblyQualifiedName"/></exception>
		public static Type? LinearAlgebraImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.LinearAlgebra);
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (LinearAlgebra.AbstractApi.SetImplementation(value))
				{
					var v = value.AssemblyQualifiedName;
					if (v is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					singletonSettings.ImplementationSettings.LinearAlgebra = v;
				}
			}
		}

		/// <summary>
		/// Which tensor algebra implementation to use
		/// </summary>
		/// <exception cref="ArgumentNullException">If the given value is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the given value has no <see cref="Type.AssemblyQualifiedName"/></exception>
		public static Type? TensorAlgebraImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.TensorAlgebra);
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (TensorAlgebra.AbstractApi.SetImplementation(value))
				{
					var v = value.AssemblyQualifiedName;
					if (v is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					singletonSettings.ImplementationSettings.TensorAlgebra = v;
				}
			}
		}

		/// <summary>
		/// Which statistics implementation to use
		/// </summary>
		/// <exception cref="ArgumentNullException">If the given value is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the given value has no <see cref="Type.AssemblyQualifiedName"/></exception>
		public static Type? StatisticsImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.Statistics);
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (Statistics.AbstractApi.SetImplementation(value))
				{
					var v = value.AssemblyQualifiedName;
					if (v is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					singletonSettings.ImplementationSettings.Statistics = v;
				}
			}
		}

		/// <summary>
		/// Which general solver implementation to use
		/// </summary>
		/// <exception cref="ArgumentNullException">If the given value is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the given value has no <see cref="Type.AssemblyQualifiedName"/></exception>
		public static Type? SolverImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.Solver);
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (Solver.AbstractApi.SetImplementation(value))
				{
					var v = value.AssemblyQualifiedName;
					if (v is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					singletonSettings.ImplementationSettings.Solver = v;
				}
			}
		}

		/// <summary>
		/// Set all back-end implementations at once
		/// </summary>
		/// <param name="backend">The <see cref="Backend.ISetBackend"/> used to set all back-ends</param>
		/// <return>Success or not</return>
		public static bool SetBackend(Backend.ISetBackend backend)
		{
			if (!backend.Available)
				return false;
			StorageImplementation = backend.MemoryImplementation;
			LinearAlgebraImplementation = backend.LinearAlgebraImplementation;
			TensorAlgebraImplementation = backend.TensorAlgebraImplementation;
			StatisticsImplementation = backend.StatisticsImplementation;
			SolverImplementation = backend.SolverImplementation;
			return true;
		}
		#endregion

		#region import and export
		private static readonly JsonSerializerOptions options = new JsonSerializerOptions
		{
			DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			IgnoreReadOnlyProperties = true,
			IncludeFields = true,
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
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
				return true;
			}
			catch (Exception e)
			{
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
		/// <param name="filePath">the file path to set</param>
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
