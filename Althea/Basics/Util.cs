using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;

using Althea.Linq;
using Althea.Array;
using Althea.Memory;


namespace Althea
{
	/// <summary>
	/// The print settings
	/// </summary>
	public enum PrintSetting
	{
		/// <summary>
		/// how many digital numbers will be printed out for a real-precision number
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
	/// The settings
	/// </summary>
	public static class GlobalSettings
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
		/// Whether the <see cref="GC.Collect()"/> will be called when memory becomes insufficient. If this option is turned on, there will be performance loss; otherwise, you should carefully calling the <see cref="Storage{T}.Dispose()"/>.
		/// </summary>
		public static bool AutoGCWhenOutOfMemory { get; set; } = true;

		/// <summary>
		/// calculate array's check sum code before writing to files and reading from files
		/// </summary>
		public static bool? FileCheck { get; set; } = false;
		#endregion

		#region user-defined
		internal static Type RuntimeCPU = null, BlasCPU = null, RandCPU = null, SparseCPU = null, SolverCPU = null, TensorCPU = null;
		internal static Type RuntimeGPU = null, BlasGPU = null, RandGPU = null, SparseGPU = null, SolverGPU = null, TensorGPU = null;
		internal static Type StorageFactory = null, SwappableStorage = null, UnswappableStorage = null;
		#endregion

		static GlobalSettings()
		{
			dynamic settings = JsonConvert.DeserializeObject(File.ReadAllText("Althea.json"));

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


	/// <summary>
	/// Converters module
	/// </summary>
	public static class CudaCSharpConverters
	{
		internal static StringComparison StrCmp => StringComparison.OrdinalIgnoreCase;

		#region internal
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static SparseMatrixFormat GetTransposedFormat(this SparseMatrixFormat format)
		{
			return format switch
			{
				SparseMatrixFormat.COOR => SparseMatrixFormat.COOC,
				SparseMatrixFormat.COOC => SparseMatrixFormat.COOR,
				SparseMatrixFormat.CSR => SparseMatrixFormat.CSC,
				SparseMatrixFormat.CSC => SparseMatrixFormat.CSR,
				_ => throw new NotSupportedException(Resource.FormatNotSupport),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static PowerOperation ToPowerOp(this MatrixOperation op)
		{
			return op switch
			{
				MatrixOperation.Transpose => PowerOperation.Transpose,
				MatrixOperation.ConjugateTranspose => PowerOperation.Dagger,
				MatrixOperation.None => PowerOperation.None,
				_ => PowerOperation.None
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MatrixOperation ToBlasOp(this PowerOperation op)
		{
			return op switch
			{
				PowerOperation.None => MatrixOperation.None,
				PowerOperation.Dagger => MatrixOperation.ConjugateTranspose,
				PowerOperation.Transpose => MatrixOperation.Transpose,
				_ => MatrixOperation.None
			};
		}
		#endregion

		#region public
		/// <summary>
		/// Convert from <see cref="DataType"/> to CUDA BLAS's <see cref="CudaDataType"/>.
		/// </summary>
		/// <param name="t">the <see cref="DataType"/> to convert from</param>
		/// <returns>The result <see cref="CudaDataType"/></returns>
		public static CudaDataType ToCudaDataType(this DataType t)
		{
			return t switch
			{
				DataType.RealInt8 => CudaDataType.RealInt8,
				DataType.RealInt32 => CudaDataType.RealInt32,
				DataType.RealUInt8 => CudaDataType.RealUInt8,
				DataType.RealUInt32 => CudaDataType.RealUInt32,
				DataType.ComplexInt8 => CudaDataType.ComplexInt8,
				DataType.ComplexInt32 => CudaDataType.ComplexInt32,
				DataType.ComplexUInt8 => CudaDataType.ComplexUInt8,
				DataType.ComplexUInt32 => CudaDataType.ComplexUInt32,
				DataType.RealSingle => CudaDataType.RealFloat32,
				DataType.RealDouble => CudaDataType.RealFloat64,
				DataType.ComplexSingle => CudaDataType.ComplexFloat32,
				DataType.ComplexDouble => CudaDataType.ComplexFloat64,
				DataType.Real | DataType.TypeFloat | DataType.Byte2 => CudaDataType.RealFloat16,
				DataType.Complex | DataType.TypeFloat | DataType.Byte2 => CudaDataType.ComplexFloat16,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
		}

		#endregion
	}

	/// <summary>
	/// The helper module
	/// </summary>
	public static class CudaCSharpHelpers
	{
		#region predictors
		/// <summary>
		/// Is the <see cref="SparseMatrixFormat"/> an atomic format or not
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsAtomic(this SparseMatrixFormat format) => IsPowerOfTwo((int)format);
		#endregion

		#region helpers
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ApproxIndexOfSingle(this DoubleComplex[] array, DoubleComplex value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				var diff = array[i] - value;
				double diffMax = Math.Max(Math.Abs(diff.Real()), Math.Abs(diff.Imaginary()));
				double max = Math.Max(Math.Abs(array[i].Real()), Math.Abs(array[i].Imaginary()));
				if (diffMax / max < singlePrecision23)
					return i;
			}
			return -1;
		}

		private static readonly double	doublePrecision13 = Math.Pow(General.Common.DoubleMachinePrecision, 1.0 / 3),
										singlePrecision23 = Math.Pow(General.Common.SingleMachinePrecision, 2.0 / 3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ApproxIndexOfDouble(this DoubleComplex[] array, DoubleComplex value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				var diff = array[i] - value;
				double diffMax = Math.Max(Math.Abs(diff.Real()), Math.Abs(diff.Imaginary()));
				double max = Math.Max(Math.Abs(array[i].Real()), Math.Abs(array[i].Imaginary()));
				if (diffMax / max < doublePrecision13)
					return i;
			}
			return -1;
		}

		internal static bool CheckOnHost<T>(params PureArray<T>[] arrays) where T : struct, IComparable<T>
		{
			if (arrays is null || arrays.Length == 0)
				throw new ArgumentNullException(nameof(arrays));
			if (arrays.Any(a => a.Disposed))
				throw new ObjectDisposedException(nameof(arrays));
			if (arrays.All(a => a.Length == 0 || !a.OnHost)) // empty array can be any where
				return false;
			if (arrays.All(a => a.Length == 0 || a.OnHost))
				return true;
			// else
			throw new ArgumentException(Resource.RequireSamePos);
		}

		internal static bool CheckOnHost<T>(params Storage<T>[] arrays) where T : struct, IComparable<T>
		{
			if (arrays is null || arrays.Length == 0)
				throw new ArgumentNullException(nameof(arrays));
			////if (arrays.Any(a => a.AlreadyDisposed))
			////	throw new ObjectDisposedException(nameof(arrays));
			if (arrays.All(a => !a.OnHost))
				return false;
			if (arrays.All(a => a.OnHost))
				return true;
			// else
			throw new ArgumentException(Resource.RequireSamePos);
		}

		internal static MatrixOperation CheckOP<T>(this MatrixOperation input, MatrixBase<T> mat) where T : struct, IComparable<T>
		{
			if (mat is null)
				return default;
			if (mat.Hermitian && (mat.IsRealType || input == MatrixOperation.ConjugateTranspose))
				return MatrixOperation.None;
			else if (mat.IsRealType && input == MatrixOperation.ConjugateTranspose)
				return MatrixOperation.Transpose;

			return input;
		}

		/// <summary>
		/// Safely apply <paramref name="action"/> to the cloned <paramref name="array"/> -- when <paramref name="action"/> throws error, the new copied array will be safely disposed.
		/// </summary>
		/// <typeparam name="T">the array that is <see cref="ICloneable"/> and <see cref="IDisposable"/></typeparam>
		/// <param name="array">the array to be acted by <paramref name="action"/></param>
		/// <param name="action">the <see cref="Action{T}"/> to apply</param>
		/// <returns>the cloned <paramref name="array"/> after applying <paramref name="action"/></returns>
		public static T ApplyToClone<T>(this T array, Action<T> action) where T : class, ICloneable, IDisposable
		{
			var copy = array.Clone() as T;
			try
			{
				action(copy);
				return copy;
			}
			catch (Exception)
			{
				copy?.Dispose();
				throw;
			}
		}
		#endregion

		#region public
		/// <summary>
		/// Perform general inner product of two matrices <paramref name="left"/> and <paramref name="right"/> with <paramref name="multiply"/> as general multiplication
		/// </summary>
		/// <typeparam name="TL">the left matrix's data type</typeparam>
		/// <typeparam name="TR">the right matrix's data type</typeparam>
		/// <typeparam name="TO">the output matrix's data type</typeparam>
		/// <param name="m">number of rows of <paramref name="left"/></param>
		/// <param name="n">number of columns of <paramref name="right"/></param>
		/// <param name="k">number of columns of <paramref name="left"/> and rows of <paramref name="right"/></param>
		/// <param name="left">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="right">right matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">the function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">the function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[,] InnerProduct<TL, TR, TO>(int m, int n, int k, Func<int, int, TL> left, Func<int, int, TR> right, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (m <= 0) throw new ArgumentOutOfRangeException(nameof(m));
			if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
			if (left is null) throw new ArgumentNullException(nameof(left));
			if (right is null) throw new ArgumentNullException(nameof(right));
			if (multiply is null) throw new ArgumentNullException(nameof(multiply));

			var output = new TO[m, n];
			for (int i = 0; i < m; i++)
			{
				for (int j = 0; j < n; j++)
				{
					output[i, j] = newZero();
					for (int t = 0; t < k; t++)
					{
						inPlaceAdd(output[i, j], multiply(left(i, t), right(t, j)));
					}
				}
			}
			return output;
		}

		/// <summary>
		/// Perform general inner product of a matrix <paramref name="leftMat"/> and a vector <paramref name="rightVec"/> with <paramref name="multiply"/> as general multiplication
		/// </summary>
		/// <typeparam name="TL">the left matrix's data type</typeparam>
		/// <typeparam name="TR">the right matrix's data type</typeparam>
		/// <typeparam name="TO">the output matrix's data type</typeparam>
		/// <param name="m">number of rows of <paramref name="leftMat"/></param>
		/// <param name="k">number of columns of <paramref name="leftMat"/> and rows of <paramref name="rightVec"/></param>
		/// <param name="leftMat">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="rightVec">right vector as an function whose input is the index <c>i</c> and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">the function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">the function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[] InnerProduct<TL, TR, TO>(int m, int k, Func<int, int, TL> leftMat, Func<int, TR> rightVec, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (m <= 0) throw new ArgumentOutOfRangeException(nameof(m));
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
			if (leftMat is null) throw new ArgumentNullException(nameof(leftMat));
			if (rightVec is null) throw new ArgumentNullException(nameof(rightVec));
			if (multiply is null) throw new ArgumentNullException(nameof(multiply));

			var output = new TO[m];
			for (int i = 0; i < m; i++)
			{
				output[i] = newZero();
				for (int t = 0; t < k; t++)
				{
					inPlaceAdd(output[i], multiply(leftMat(i, t), rightVec(t)));
				}
			}
			return output;
		}

		/// <summary>
		/// Perform general inner product of a vector <paramref name="leftVec"/> and a matrix <paramref name="rightMat"/> with <paramref name="multiply"/> as general multiplication
		/// </summary>
		/// <typeparam name="TL">the left matrix's data type</typeparam>
		/// <typeparam name="TR">the right matrix's data type</typeparam>
		/// <typeparam name="TO">the output matrix's data type</typeparam>
		/// <param name="n">number of columns of <paramref name="leftVec"/> and rows of <paramref name="rightMat"/></param>
		/// <param name="k">number of columns of <paramref name="rightMat"/></param>
		/// <param name="rightMat">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="leftVec">right vector as an function whose input is the index <c>i</c> and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">the function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">the function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[] InnerProduct<TL, TR, TO>(int n, int k, Func<int, TL> leftVec, Func<int, int, TR> rightMat, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
			if (rightMat is null) throw new ArgumentNullException(nameof(rightMat));
			if (leftVec is null) throw new ArgumentNullException(nameof(leftVec));
			if (multiply is null) throw new ArgumentNullException(nameof(multiply));

			var output = new TO[k];
			for (int i = 0; i < k; i++)
			{
				output[i] = newZero();
				for (int t = 0; t < n; t++)
				{
					inPlaceAdd(output[i], multiply(leftVec(t), rightMat(t, i)));
				}
			}
			return output;
		}
		#endregion
	}

	/// <summary>
	/// Extend methods
	/// </summary>
	public static class Extensions
	{
		#region index extends
		/// <summary>
		/// Calculate the offset from the start using the giving collection length.
		/// </summary>
		/// <param name="index">the <see cref="Index"/></param>
		/// <param name="length">The length of the collection that the Index will be used with. It has to be a positive value</param>
		/// <remarks>
		/// This is a <see cref="long"/> version of <see cref="Index.GetOffset(int)"/> at x64 platforms.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of [0, <paramref name="length"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetPosition(this Index index, long length)
		{
			long val;
			if (index.IsFromEnd)
			{
				val = length - index.Value;
			}
			else
			{
				val = index.Value;
			}
			if (val < 0 || val > length)
				throw new ArgumentOutOfRangeException(nameof(index));
			return val;
		}

		/// <summary>
		/// Calculate the start offset and length of range object using a collection length.
		/// </summary>
		/// <param name="range">the <see cref="Range"/></param>
		/// <param name="length">The length of the collection that the range will be used with. It has to be a positive value.</param>
		/// <returns>the offset and length of <paramref name="range"/> under <paramref name="length"/></returns>
		/// <remarks>This is a <see cref="long"/> version of <see cref="Range.GetOffsetAndLength(int)"/> at x64 platforms.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="range"/> is out of [0, <paramref name="length"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (long Offset, long Length) GetOffsetAndCount(this Range range, long length)
		{
			long start = range.Start.GetPosition(length), end = range.End.GetPosition(length);
			if (end <= start || start >= length || end < 0 || end > length)
				throw new ArgumentOutOfRangeException(nameof(range));
			return (start, end - start);
		}
		#endregion

		#region other extends
		/// <summary>
		/// Get the nearest power of 2 number of the input number
		/// </summary>
		/// <param name="x">input number</param>
		/// <returns>the nearest power of 2</returns>
		/// <remarks>extend method</remarks>
		public static uint NearestPowerOfTwo(this uint x)
		{
			x--;
			x |= x >> 1;
			x |= x >> 2;
			x |= x >> 4;
			x |= x >> 8;
			x |= x >> 16;
			return x + 1;
		}
		/// <summary>
		/// Get the nearest power of 2 number of the input number
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest power of 2</returns>
		/// <remarks>extend method</remarks>
		public static int NearestPowerOfTwo(this int input)
		{
			uint x = unchecked((uint)input);
			return Convert.ToInt32(x.NearestPowerOfTwo());
		}
		/// <summary>
		/// Get the nearest power of 2 number of the input number
		/// </summary>
		/// <param name="x">input number</param>
		/// <returns>the nearest power of 2</returns>
		/// <remarks>extend method</remarks>
		public static ulong NearestPowerOfTwo(this ulong x)
		{
			x--;
			x |= x >> 1;
			x |= x >> 2;
			x |= x >> 4;
			x |= x >> 8;
			x |= x >> 16;
			x |= x >> 32;
			return x + 1;
		}
		/// <summary>
		/// Get the nearest power of 2 number of the input number
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest power of 2</returns>
		/// <remarks>extend method</remarks>
		public static long NearestPowerOfTwo(this long input)
		{
			ulong x = unchecked((ulong)input);
			return Convert.ToInt64(x.NearestPowerOfTwo());
		}
		#endregion

		#region 1D to 2D array extends
		/// <summary>
		/// Convert a 1D array to a 2D jagged array
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array to convert</param>
		/// <param name="innerSize">the size of inner dimension of the 2D jagged array</param>
		/// <returns>the 2D jagged array</returns>
		public static T[][] ToJagged<T>(this T[] array, long innerSize)
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			if (array.Length % innerSize != 0)
				throw new ArgumentOutOfRangeException(nameof(array));

			var output = new T[array.Length / innerSize][];
			for (int i = 0; i < array.Length / innerSize; i++)
			{
				output[i] = new T[innerSize];
				for (int j = 0; j < innerSize; j++)
				{
					output[i][j] = array[j + i * innerSize];
				}
			}
			return output;
		}

		/// <summary>
		/// Construct a new 2D array out of a 1D array, column major
		/// </summary>
		/// <typeparam name="T">any data type</typeparam>
		/// <returns>a 2D array T[,]</returns>
		/// <param name="input">input 1D array</param>
		/// <param name="rows">height (number of rows) of the new 2D array</param>
		/// <param name="columns">width (number of columns) of the new 2D array</param>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static T[,] Make2DArray<T>(this T[] input, long rows, long columns)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			T[,] output = new T[rows, columns];
			for (long i = 0; i < columns; i++)
				for (long j = 0; j < rows; j++)
					output[j, i] = input[i * rows + j];

			return output;
		}
		#endregion

		#region 2D array extends
		/// <summary>
		/// Get the number of rows and columns of the 2D array.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="arr">2D array</param>
		/// <returns>the number of rows and columns</returns>
		/// <remarks>extend method of <paramref name="arr"/></remarks>
		public static (int rows, int columns) GetRowColumns<T>(this T[,] arr)
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr));
			return (arr.GetLength(0), arr.GetLength(1));
		}

		/// <summary>
		/// Take out the 2D array column by column.
		/// </summary>
		/// <param name="arr">input array</param>
		/// <returns>a <see cref="IEnumerable"/></returns>
		/// <typeparam name="T">any data type</typeparam>
		/// <remarks>extend method of <paramref name="arr"/></remarks>
		public static T[] ColumnTake<T>(this T[,] arr)
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr), Resource.ArrayCannotNull);

			var (rows, columns) = arr.GetRowColumns();
			T[] oneDim = new T[rows * columns];
			for (long j = 0; j < columns; j++)
				for (long i = 0; i < rows; i++)
					oneDim[i + j * rows] = arr[i, j];
			return oneDim;
		}

		/// <summary>
		/// Act on each element of a 2D array.
		/// </summary>
		/// <typeparam name="T">input array type</typeparam>
		/// <param name="arr">input 2D array</param>
		/// <param name="action">action function, parameter 2 &amp; 3 are row &amp; column indices respectively</param>
		/// <remarks>extend method of <paramref name="arr"/></remarks>
		public static void ForEach<T>(this T[,] arr, Action<T, int, int> action)
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr), Resource.ArrayCannotNull);

			var (rows, cols) = arr.GetRowColumns();
			for (int i = 0; i < rows; i++)
				for (int j = 0; j < cols; j++)
					action(arr[i, j], i, j);
		}

		/// <summary>
		/// Check if the 2D array is Hermitian or not
		/// </summary>
		/// <param name="arr">input 2D array to test</param>
		/// <returns>Hermitian or not</returns>
		/// <remarks>extend method</remarks>
		/// <typeparam name="T">the supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/> and <see cref="int"/></typeparam>
		/// <exception cref="NotSupportedException">if the <typeparamref name="T"/> is not supported</exception>
		/// <remarks>extend method of <paramref name="arr"/></remarks>
		public static bool IsHermitian<T>(this T[,] arr) where T : struct, IComparable<T>
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr), Resource.ArrayCannotNull);

			var (rows, cols) = arr.GetRowColumns();
			if (rows != cols)
				return false;

			if (default(T).IsReal())
			{
				for (long i = 0; i < rows; i++)
					for (long j = 0; j < i; j++)
						if (arr[i, j].CompareTo(arr[j, i]) != 0)
							return false;
			}
			else
			{
				for (long i = 0; i < rows; i++)
					for (long j = 0; j < i; j++)
						if (arr[i, j].CompareTo(arr[j, i].GenericConjugate()) != 0)
							return false;
			}
			return true;
		}
		#endregion
	}
}
