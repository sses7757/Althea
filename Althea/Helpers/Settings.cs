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
			public bool UseRecentImplementation;

			public bool DisposeNotCurrentImplementation;

			public string Memory, LinearAlgebra, TensorAlgebra, Statistics, Solver;

			internal JsonImplementationSettings(bool _)
			{
				UseRecentImplementation = true;
				DisposeNotCurrentImplementation = true;
				Memory = null;
				LinearAlgebra = null;
				TensorAlgebra = null;
				Statistics = null;
				Solver = null;
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

		internal static JsonSettings singletonSettings;
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
		/// When the current implementation does not support given condition, shall this library try to find and use the most recent one that support such condition or not?<br/>
		/// If so, this library will try to find suitable implementation used before during this running. an error will be thrown when there is no suitable implementation.<br/>
		/// If not, this library will try to create temporary memory which fits the current implementation (may cause significant performance loss). If memory allocation or copies fails, an error will be thrown.
		/// </summary>
		public static bool UseRecentImplementation {
			get => singletonSettings.ImplementationSettings.UseRecentImplementation;
			set => singletonSettings.ImplementationSettings.UseRecentImplementation = value;
		}

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
		public static Type MemoryImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.Memory);
			set {
				if (Memory.AbstractApi.SetImplementation(value))
					singletonSettings.ImplementationSettings.Memory = value.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Which linear algebra implementation to use
		/// </summary>
		public static Type LinearAlgebraImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.LinearAlgebra);
			set {
				if (LinearAlgebra.AbstractApi.SetImplementation(value))
					singletonSettings.ImplementationSettings.LinearAlgebra = value.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Which tensor algebra implementation to use
		/// </summary>
		public static Type TensorAlgebraImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.TensorAlgebra);
			set {
				if (TensorAlgebra.AbstractApi.SetImplementation(value))
					singletonSettings.ImplementationSettings.TensorAlgebra = value.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Which statistics implementation to use
		/// </summary>
		public static Type StatisticsImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.Statistics);
			set {
				if (Statistics.AbstractApi.SetImplementation(value))
					singletonSettings.ImplementationSettings.Statistics = value.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Which general solver implementation to use
		/// </summary>
		public static Type SolverImplementation {
			get => Type.GetType(singletonSettings.ImplementationSettings.Solver);
			set {
				if (Solver.AbstractApi.SetImplementation(value))
					singletonSettings.ImplementationSettings.Solver = value.AssemblyQualifiedName;
			}
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
			Import(logError: true);
			// set default implementations
			// C# implementations
			MemoryImplementation = typeof(CSharp.Memory.MemoryApi);
			LinearAlgebraImplementation = typeof(CSharp.LinearAlgebra.LinearAlgebraApi);
			TensorAlgebraImplementation = typeof(CSharp.TensorAlgebra.TensorAlgebraApi);
			StatisticsImplementation = typeof(CSharp.Statistics.StatisticsApi);
			SolverImplementation = typeof(CSharp.Solver.SolverApi);
			// CUDA implementations
			MemoryImplementation = typeof(Cuda.Memory.MemoryApi);
			LinearAlgebraImplementation = typeof(Cuda.LinearAlgebra.LinearAlgebraApi);
			TensorAlgebraImplementation = typeof(Cuda.TensorAlgebra.TensorAlgebraApi);
			StatisticsImplementation = typeof(Cuda.Statistics.StatisticsApi);
			SolverImplementation = typeof(Cuda.Solver.SolverApi);
			// MKL implementations, the real default implementation
			MemoryImplementation = typeof(Mkl.Memory.MemoryApi);
			LinearAlgebraImplementation = typeof(Mkl.LinearAlgebra.LinearAlgebraApi);
			TensorAlgebraImplementation = typeof(Mkl.TensorAlgebra.TensorAlgebraApi);
			StatisticsImplementation = typeof(Mkl.Statistics.StatisticsApi);
			SolverImplementation = typeof(Mkl.Solver.SolverApi);
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
