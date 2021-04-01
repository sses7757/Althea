using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Althea.NativeTypes;


namespace Althea.Helpers
{
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
		internal struct ImplementationSettings
		{
			public bool DisposeNotCurrentImplementation;

			public Type Storage, LinearAlgebraDense, LinearAlgebraSparse, TensorAlgebraDense, TensorAlgebraSparse, Statistics, Solver;

			internal ImplementationSettings(bool _)
			{
				DisposeNotCurrentImplementation = true;
				ISetBackend impls = GetInternalBackend("CSharp");
				Storage = impls.StorageImplementation;
				LinearAlgebraDense = impls.DenseLinearAlgebraImplementation;
				LinearAlgebraSparse = impls.SparseLinearAlgebraImplementation;
				TensorAlgebraDense = impls.DenseTensorAlgebraImplementation;
				TensorAlgebraSparse = impls.SparseTensorAlgebraImplementation;
				Statistics = impls.StatisticsImplementation;
				Solver = impls.SolverImplementation;
			}

			[JsonConstructor]
			internal ImplementationSettings(bool disposeNotCurrentImplementation, string storage, string linearAlgebra, string tensorAlgebra, string statistics, string solver)
			{
				DisposeNotCurrentImplementation = disposeNotCurrentImplementation;
				ISetBackend impls = GetInternalBackend("CSharp");
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
					LinearAlgebraDense = Type.GetType(linearAlgebra) ?? impls.DenseLinearAlgebraImplementation;
				}
				catch (Exception)
				{
					LinearAlgebraDense = impls.DenseLinearAlgebraImplementation;
				}

				try
				{
					LinearAlgebraSparse = Type.GetType(linearAlgebra) ?? impls.SparseLinearAlgebraImplementation;
				}
				catch (Exception)
				{
					LinearAlgebraSparse = impls.SparseLinearAlgebraImplementation;
				}

				try
				{
					TensorAlgebraDense = Type.GetType(tensorAlgebra) ?? impls.DenseTensorAlgebraImplementation;
				}
				catch (Exception)
				{
					TensorAlgebraDense = impls.DenseTensorAlgebraImplementation;
				}

				try
				{
					TensorAlgebraSparse = Type.GetType(tensorAlgebra) ?? impls.SparseTensorAlgebraImplementation;
				}
				catch (Exception)
				{
					TensorAlgebraSparse = impls.SparseTensorAlgebraImplementation;
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

			public int StackAllocLimit;

			internal JsonSettings(bool _)
			{
				LogSettings = new LogSettings(false);
				PrintSettings = new PrintSettings(false);
				ImplementationSettings = new ImplementationSettings(false);
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

		/// <summary>
		/// Which memory / storage implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type StorageImplementation {
			get => singletonSettings.ImplementationSettings.Storage;
			set => singletonSettings.ImplementationSettings.Storage = Storage.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which dense linear algebra implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type DenseLinearAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.LinearAlgebraDense;
			set => singletonSettings.ImplementationSettings.LinearAlgebraDense = LinearAlgebra.Dense.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which sparse linear algebra implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type SparseLinearAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.LinearAlgebraSparse;
			set => singletonSettings.ImplementationSettings.LinearAlgebraSparse = LinearAlgebra.Sparse.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which dense tensor algebra implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type DenseTensorAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.TensorAlgebraDense;
			set => singletonSettings.ImplementationSettings.TensorAlgebraDense = TensorAlgebra.Dense.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
		}

		/// <summary>
		/// Which sparse tensor algebra implementation to use
		/// </summary>
		/// <exception cref="NotSupportedException">If the input value cannot be set to the current implementation</exception>
		public static Type SparseTensorAlgebraImplementation {
			get => singletonSettings.ImplementationSettings.TensorAlgebraSparse;
			set => singletonSettings.ImplementationSettings.TensorAlgebraSparse = TensorAlgebra.Sparse.AbstractApi.SetImplementation(value) ? value : throw new NotSupportedException();
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
		/// <param name="backend">The <see cref="ISetBackend"/> used to set all back-ends</param>
		/// <return>Success or not. Some implementation may still be changed even if this returns false.</return>
		public static bool SetBackend(ISetBackend backend)
		{
			if (!backend.Available)
				return false;
			try
			{
				StorageImplementation = backend.StorageImplementation;
				DenseLinearAlgebraImplementation = backend.DenseLinearAlgebraImplementation;
				SparseLinearAlgebraImplementation = backend.SparseLinearAlgebraImplementation;
				DenseTensorAlgebraImplementation = backend.DenseTensorAlgebraImplementation;
				SparseTensorAlgebraImplementation = backend.SparseTensorAlgebraImplementation;
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
		private static readonly JsonSerializerOptions options = new()
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
				SetBackend(GetInternalBackend("CSharp"));
				Storage.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.Storage);
				LinearAlgebra.Dense.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.LinearAlgebraDense);
				LinearAlgebra.Sparse.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.LinearAlgebraSparse);
				TensorAlgebra.Dense.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.TensorAlgebraDense);
				TensorAlgebra.Sparse.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.TensorAlgebraSparse);
				Statistics.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.Statistics);
				Solver.AbstractApi.SetImplementation(singletonSettings.ImplementationSettings.Solver);
				return true;
			}
			catch (Exception e)
			{
				singletonSettings = new JsonSettings(false);
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
			string json = JsonSerializer.Serialize(singletonSettings, options);
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
		/// Check whether the given <paramref name="length"/> and type <typeparamref name="T"/> fits the <see cref="StackAllocLimit"/> and create an array of <typeparamref name="T"/> if not. (Otherwise, you shall <c>stackalloc <typeparamref name="T"/>[<paramref name="length"/>]</c> yourself.)
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="length">The desired length to allocate</param>
		/// <returns>The allocated C# array of given <paramref name="length"/> or null</returns>
		public static T[]? CheckStackLimitFast<T>(this int length) where T : unmanaged
		{
			if (length * Const<T>.SizeT > StackAllocLimit)
				return new T[length];
			else
				return null;
		}

		/// <summary>
		/// Check whether the given <paramref name="length"/> and type <typeparamref name="T"/> fits the <see cref="StackAllocLimit"/> and create an array of <typeparamref name="T"/> if not. (Otherwise, you shall <c>stackalloc <typeparamref name="T"/>[<paramref name="length"/>]</c> yourself.)
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="length">The desired length to allocate</param>
		/// <param name="size">Output the size of <typeparamref name="T"/></param>
		/// <returns>The allocated C# array of given <paramref name="length"/> or null</returns>
		public static T[]? CheckStackLimit<T>(this int length, out int size) where T : notnull
		{
			size = Marshal.SizeOf<T>();
			if (length * size > StackAllocLimit)
				return new T[length];
			else
				return null;
		}
		#endregion
	}
}
