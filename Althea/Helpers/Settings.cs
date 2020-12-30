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

			public string Memory, LinearAlgebra, TensorAlgebra, Statistics, Solver;

			// TODO: add notify change of implementation to target module
			internal Type MemoryType { get => Type.GetType(Memory); set => Memory = value.AssemblyQualifiedName; }
			internal Type LinearAlgebraType { get => Type.GetType(LinearAlgebra); set => LinearAlgebra = value.AssemblyQualifiedName; }
			internal Type TensorAlgebraType { get => Type.GetType(TensorAlgebra); set => TensorAlgebra = value.AssemblyQualifiedName; }
			internal Type StatisticsType { get => Type.GetType(Statistics); set => Statistics = value.AssemblyQualifiedName; }
			internal Type SolverType { get => Type.GetType(Solver); set => Solver = value.AssemblyQualifiedName; }

			internal JsonImplementationSettings(bool _)
			{
				UseRecentImplementation = true;
				// TODO: write the default implementations' types later
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
		/// When the current implementation does not support given condition, shall this library try to find and use the most recent one that support such condition or not? If not, this library will try to convert the memory to fit the given condition. If conversion still fails, an error will be thrown.
		/// </summary>
		public static bool UseRecentImplementation {
			get => singletonSettings.ImplementationSettings.UseRecentImplementation;
			set => singletonSettings.ImplementationSettings.UseRecentImplementation = value;
		}

		/// <summary>
		/// Which memory / storage implementation to use
		/// </summary>
		public static Type MemoryImplementation {
			get => singletonSettings.ImplementationSettings.MemoryType;
			set => singletonSettings.ImplementationSettings.MemoryType = value;
		}

		/// <summary>
		/// Which linear algebra implementation to use
		/// </summary>
		public static Type LinearAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.LinearAlgebraType;
			set => singletonSettings.ImplementationSettings.LinearAlgebraType = value;
		}

		/// <summary>
		/// Which tensor algebra implementation to use
		/// </summary>
		public static Type TensorAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.TensorAlgebraType;
			set => singletonSettings.ImplementationSettings.TensorAlgebraType = value;
		}

		/// <summary>
		/// Which statistics implementation to use
		/// </summary>
		public static Type StatisticsImplementation {
			get => singletonSettings.ImplementationSettings.StatisticsType;
			set => singletonSettings.ImplementationSettings.StatisticsType = value;
		}

		/// <summary>
		/// Which general solver implementation to use
		/// </summary>
		public static Type SolverImplementation {
			get => singletonSettings.ImplementationSettings.SolverType;
			set => singletonSettings.ImplementationSettings.SolverType = value;
		}
		#endregion

		#region import and export
		private static readonly JsonSerializerOptions options = new JsonSerializerOptions
		{
			DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			IncludeFields = true,
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
			WriteIndented = true,
		};

		private const string fileName = "Althea.json";

		static Settings()
		{
			try
			{
				singletonSettings = JsonSerializer.Deserialize<JsonSettings>(File.ReadAllText(fileName), options);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(Resource.ErrorOccur + e);
				Console.Error.WriteLine(Resource.UseDefault);
			}
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
