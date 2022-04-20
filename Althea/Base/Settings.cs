using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Althea.Helpers;


namespace Althea
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
				this.StackAllocLimit = 8192;
			}
		}

		internal static JsonSettings settings;

		private static readonly object __lockSetting = new();
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
		/// When a new implementation is indicated, whether elder ones will be disposed or not.
		/// </summary>
		/// <remarks>If the value is true, there may be creations and dispositions of implementation classes that may introduce more time loss.<br/>
		/// Otherwise, there may be maintained storages of implementation classes that may introduce some memory loss.</remarks>
		public static bool DisposeNotCurrentImplementation {
			get => settings.ImplementationSettings.DisposeNotCurrentImplementation;
			set => settings.ImplementationSettings = settings.ImplementationSettings with { DisposeNotCurrentImplementation = value };
		}

		/// <summary>
		/// Try to set all back-end implementations at once.
		/// </summary>
		/// <param name="backend">The <see cref="IBackends"/> used to set all back-ends</param>
		/// <return>Success or not. Some implementation may still be changed even if this returns false.</return>
		public static bool TrySetBackend(IBackends backend)
		{
			if (backend is null || !backend.Available)
				return false;
			try
			{
				lock (__lockSetting)
				{
					settings.ImplementationSettings = new(backend);
					settings.ImplementationSettings.SetBackends();
				}
				return true;
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
			Converters = { new ImplementationSettings.JsonConverter() }
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
				lock (__lockSetting)
				{
					settings = JsonSerializer.Deserialize<JsonSettings>(File.ReadAllText(fileName), options) ?? throw new InvalidOperationException();
					settings.ImplementationSettings.SetBackends();
				}
				return true;
			}
			catch (Exception e)
			{
				lock (__lockSetting)
				{
					settings = new JsonSettings();
					settings.ImplementationSettings.SetBackends();
				}
				if (logError)
					Log.Write(Resources.FileError.FileCorrupted + Environment.NewLine + e.Message, level: LogLevel.Error);
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

		internal static IBackends GetInternalBackend(string name)
		{
			// Ignore spelling: Backend
			if (Type.GetType($"Althea.Backend.{name}Backend")?.GetConstructor(Type.EmptyTypes)?.Invoke(null) is not IBackends impls)
				throw new InvalidOperationException();
			return impls;
		}

		// Ignore Spelling: Cuda Mkl
		static Settings()
		{
			// set default implementations
			settings = new();
			// CUDA implementations
			TrySetBackend(GetInternalBackend(@"Cuda"));
			// MKL implementations, the real default implementation
			TrySetBackend(GetInternalBackend(@"Mkl"));
			// import at last
			Import(logError: true);
		}

		/// <summary>
		/// Export current settings to the file "Althea.json"
		/// </summary>
		public static void ExportSettings()
		{
			string json = JsonSerializer.Serialize(settings, options);
			File.WriteAllText(fileName, json);
		}
		#endregion
	}
}
