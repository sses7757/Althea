using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace Althea.Helpers
{
	/// <summary>
	/// The print settings
	/// </summary>
	public enum PrintSetting
	{
		/// <summary>
		/// how many digital numbers will be printed out for a floating point number
		/// </summary>
		Precision,
		/// <summary>
		/// how long vectors are displayed
		/// </summary>
		ArrayLength,
		/// <summary>
		/// how many rows are displayed
		/// </summary>
		MatrixRow,
		/// <summary>
		/// how many columns are displayed
		/// </summary>
		MatrixColumn
	}

	/// <summary>
	/// The static class for global settings
	/// </summary>
	public static partial class Settings
	{
		#region lanczos
		/// <summary>
		/// The maximum free memory ratio that Lanczos uses
		/// </summary>
		public static double FreeMemoryRatio { get; set; } = 0.95;

		/// <summary>
		/// When ((basis vectors' inner product) > MachinePrecision ^ <see cref="ReorthogonizePower"/>), re-orthogonalization will be performed
		/// </summary>
		public static double ReorthogonizePower { get; set; } = 0.75;

		/// <summary>
		/// How often the tridiagonal matrix is constructed and the convergence is checked
		/// </summary>
		public static int CheckConvergePer { get; set; } = 1;

		/// <summary>
		/// The hard limit of iterations per restart for Hermitian problem
		/// </summary>
		public static int IterPerRestartHardLimitHerm { get; set; } = 60;

		/// <summary>
		/// The hard limit of iterations per restart for non-Hermitian problem
		/// </summary>
		public static int IterPerRestartHardLimitNonHerm { get; set; } = 20;
		#endregion

		#region print
		/// <summary>
		/// The default print configuration, read-only
		/// </summary>
		public static Dictionary<PrintSetting, int> PrintConfig { get; } = new Dictionary<PrintSetting, int>
		{
			[PrintSetting.Precision] = 8,
			[PrintSetting.ArrayLength] = 40,
			[PrintSetting.MatrixRow] = 20,
			[PrintSetting.MatrixColumn] = 5
		};
		#endregion

		#region threshold
		/// <summary>
		/// When comparing if the array is all approximately equal to 0, this threshold is used for judgment.
		/// </summary>
		public static double EqualThreshold { get; set; } = 1e-10;

		/// <summary>
		/// If a sparse matrix has size less than this, whether the occupancy of space larger than dense will not be checked
		/// </summary>
		public static int SparseMatrixUncheck { get; set; } = 64;
		#endregion

		#region automation
		/// <summary>
		/// If this is false, only the O(1) time complexity method is used to determine whether the new matrix is Hermitian or not, this may lead to a lot of false negatives (it is Hermitian and the judgment says it's not).
		/// </summary>
		public static bool AutoDetectHermitian { get; set; } = false;

		/// <summary>
		/// Whether the <see cref="GC.Collect()"/> will be called when memory becomes insufficient. If this option is turned on, there will be performance loss if you do not dispose allocated unmanaged memory manually; otherwise, you should carefully calling the <see cref="IDisposable.Dispose()"/> to release each unmanaged memory block manually.
		/// </summary>
		public static bool AutoGCWhenOutOfMemory { get; set; } = true;

		/// <summary>
		/// Calculate array's check sum code before writing to files and reading from files
		/// </summary>
		public static bool? FileCheck { get; set; } = false;
		#endregion

		#region user-defined
		internal static Type RuntimeCPU = null, BlasCPU = null, RandCPU = null, SparseCPU = null, SolverCPU = null, TensorCPU = null;
		internal static Type RuntimeGPU = null, BlasGPU = null, RandGPU = null, SparseGPU = null, SolverGPU = null, TensorGPU = null;
		internal static Type StorageFactory = null, SwappableStorage = null, UnswappableStorage = null;
		#endregion

		static Settings()
		{
			dynamic settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("Althea.json"));

			static void Check(Action action)
			{
				try
				{
					action();
				}
				catch (TypeLoadException e)
				{
					Log.Write(e.ToString(), category: "Load user-defined routine", level: LogLevel.Error);
				}
			}

			Check(() =>
			{
				FreeMemoryRatio = settings.Lanczos.FreeMemoryRatio;
				ReorthogonizePower = settings.Lanczos.ReorthogonizePower;
				CheckConvergePer = settings.Lanczos.CheckConvergePer;
				IterPerRestartHardLimitHerm = settings.Lanczos.IterPerRestartHardLimitHerm;
				IterPerRestartHardLimitNonHerm = settings.Lanczos.IterPerRestartHardLimitNonHerm;
			});

			Check(() =>
			{
				PrintConfig[PrintSetting.Precision] = settings.Print.Precision;
				PrintConfig[PrintSetting.ArrayLength] = settings.Print.MaxArrayLength;
				PrintConfig[PrintSetting.MatrixRow] = settings.Print.MaxMatrixRow;
				PrintConfig[PrintSetting.MatrixColumn] = settings.Print.MaxMatrixColumn;
			});

			Check(() =>
			{
				EqualThreshold = settings.Threshold.Equality;
				SparseMatrixUncheck = settings.Threshold.SparseMatrixUncheck;
			});

			Check(() =>
			{
				AutoDetectHermitian = settings.Automation.DetectHermitian;
				AutoGCWhenOutOfMemory = settings.Automation.GarbageCollection;
				FileCheck = settings.Automation.FileCheck;
			});

			// read and initialize user-defined routine
			Check(() =>
			{
				string str = settings.UserDefinedCPU.RuntimeClass;
				if (!string.IsNullOrEmpty(str))
					RuntimeCPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedGPU.RuntimeClass;
				if (!string.IsNullOrEmpty(str))
					RuntimeGPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedCPU.BLASClass;
				if (!string.IsNullOrEmpty(str))
					BlasCPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedGPU.BLASClass;
				if (!string.IsNullOrEmpty(str))
					BlasGPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedCPU.SparseClass;
				if (!string.IsNullOrEmpty(str))
					SparseCPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedGPU.SparseClass;
				if (!string.IsNullOrEmpty(str))
					SparseGPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedCPU.RandClass;
				if (!string.IsNullOrEmpty(str))
					RandCPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedGPU.RandClass;
				if (!string.IsNullOrEmpty(str))
					RandGPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedCPU.SolverClass;
				if (!string.IsNullOrEmpty(str))
					SolverCPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedGPU.SolverClass;
				if (!string.IsNullOrEmpty(str))
					SolverGPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedCPU.TensorClass;
				if (!string.IsNullOrEmpty(str))
					TensorCPU = Type.GetType(str);
			});
			Check(() =>
			{
				string str = settings.UserDefinedGPU.TensorClass;
				if (!string.IsNullOrEmpty(str))
					TensorGPU = Type.GetType(str);
			});

			// user-defined storage
			Check(() =>
			{
				string str = settings.UserDefinedStorage.StorageFactory;
				if (!string.IsNullOrEmpty(str))
					StorageFactory = Type.GetType(str);
			});
			if (StorageFactory is null)
			{
				Check(() =>
				{
					string str = settings.UserDefinedStorage.SwappableStorage;
					if (!string.IsNullOrEmpty(str))
						SwappableStorage = Type.GetType(str);
				});
				Check(() =>
				{
					string str = settings.UserDefinedStorage.UnswappableStorage;
					if (!string.IsNullOrEmpty(str))
						UnswappableStorage = Type.GetType(str);
				});
			}
		}
	}
}
