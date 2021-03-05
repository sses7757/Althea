using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Storage;


namespace Althea.Arrays
{
	/// <summary>
	/// The interface for array factory
	/// </summary>
	public interface IArrayFactory
	{
		/// <summary>
		/// Abstract method to create array by size from other information obtained from <see cref="ValueArray{T}.GetOtherInfo"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="size">The size of the array about to create</param>
		/// <param name="onHost">create on host memory or device</param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetOtherInfo"/></param>
		/// <returns>created array</returns>
		ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>;

		internal delegate ValueArray<T> DelegateCreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>;

		/// <summary>
		/// Abstract method to reconstruct array from the pointers obtained from <see cref="ValueArray{T}.GetPointers"/> and other information from <see cref="ValueArray{T}.GetOtherInfo"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="size">The size of the array about to create</param>
		/// <param name="pointers">The pointers obtained from <see cref="ValueArray{T}.GetPointers"/></param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetOtherInfo"/></param>
		/// <returns>created array</returns>
		ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>;

		internal delegate ValueArray<T> DelegateReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>;
	}

	/// <summary>
	/// The abstract <see cref="ValueArray{T}"/> factory used to create instances
	/// </summary>
	public static class PureArrayFactory
	{
		#region private helper
		private static TDelegate GetDelegate<TDelegate>(Type type, string methodName, IDictionary<Type, Delegate> cache) where TDelegate : Delegate
		{
			// cache
			TDelegate delegateCreate;
			var delegateType = typeof(TDelegate);
			if (cache.ContainsKey(type))
			{
				delegateCreate = cache[type] as TDelegate;
				if (!(delegateCreate.Target is null))
					return delegateCreate;
			}
			// else, cache miss
			string fullName = type.AssemblyQualifiedName;
			int find = fullName.IndexOf('`', CudaCSharpConverters.StrCmp);
			if (find >= 0)
				fullName = fullName.Substring(0, find);
			fullName += "Factory"; // the factory name
			var factoryType = Type.GetType(fullName);
			if (!typeof(IArrayFactory).IsAssignableFrom(factoryType))
				throw new TypeAccessException();
			var factoryInstance = Activator.CreateInstance(factoryType);
			if (factoryInstance is null)
				throw new TypeInitializationException(factoryType.FullName, null);
			var method = factoryType.GetMethod(methodName);
			if (method is null)
				throw new MissingMethodException();
			method = method.MakeGenericMethod(delegateType.GetGenericArguments());
			delegateCreate = Delegate.CreateDelegate(delegateType, factoryInstance, method) as TDelegate;
			// add to cache
			cache.Add(type, delegateCreate);
			return delegateCreate;
		}
		#endregion

		#region create
		private static readonly Dictionary<Type, Delegate> cacheCreate = new();

		/// <summary>
		/// Create an array of concrete type <typeparamref name="TArray"/> using reflection.
		/// </summary>
		/// <typeparam name="TArray">The concrete array type</typeparam>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="size">The size of the array about to create</param>
		/// <param name="onHost">create on host memory or device</param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetOtherInfo"/></param>
		/// <returns>created array of type <typeparamref name="TArray"/></returns>
		/// <remarks>If <typeparamref name="TArray"/> is a user-defined class that inherits <see cref="ValueArray{T}"/>, its factory must also be created with the same class name and a postfix "Factory" which should also lie in the same naming space.</remarks>
		/// <exception cref="TypeAccessException">if <typeparamref name="TArray"/>'s factory is not a sub-class of <see cref="PureArrayFactory"/></exception>
		/// <exception cref="TypeLoadException">if <typeparamref name="TArray"/>'s factory cannot be loaded</exception>
		/// <exception cref="TypeInitializationException">if <typeparamref name="TArray"/>'s factory cannot be created with parameterless constructor</exception>
		/// <exception cref="System.Reflection.AmbiguousMatchException">if <typeparamref name="TArray"/>'s factory has multiple method named <see cref="IArrayFactory.CreateArray"/></exception>
		/// <exception cref="MissingMethodException">if <typeparamref name="TArray"/>'s factory has no method <see cref="IArrayFactory.CreateArray"/></exception>
		public static TArray Create<TArray, T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where TArray : ValueArray<T>, new() where T : struct, IComparable<T>
		{
			var delegateCreate = GetDelegate<IArrayFactory.DelegateCreateArray<T>>(typeof(TArray), nameof(IArrayFactory.CreateArray), cacheCreate);
			return delegateCreate(size, onHost, otherInfo) as TArray;
		}

		/// <summary>
		/// Create an array of concrete type <paramref name="arrayType"/> using reflection.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="arrayType">The type of the array to reconstruct</param>
		/// <param name="size">The size of the array about to create</param>
		/// <param name="onHost">create on host memory or device</param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetOtherInfo"/></param>
		/// <returns>created array of type <paramref name="arrayType"/></returns>
		/// <remarks>If <paramref name="arrayType"/> is a user-defined class that inherits <see cref="ValueArray{T}"/>, its factory must also be created with the same class name and a postfix "Factory" which should also lie in the same naming space.</remarks>
		/// <exception cref="ArgumentNullException">if <paramref name="arrayType"/> is not a sub-type of <see cref="ValueArray{T}"/> or is not a concrete one</exception>
		/// <exception cref="TypeAccessException">if <paramref name="arrayType"/>'s factory is not a sub-class of <see cref="PureArrayFactory"/></exception>
		/// <exception cref="TypeLoadException">if <paramref name="arrayType"/>'s factory cannot be loaded</exception>
		/// <exception cref="System.Reflection.AmbiguousMatchException">if <paramref name="arrayType"/>'s factory has multiple method named <see cref="IArrayFactory.ReconstructArray"/></exception>
		/// <exception cref="MissingMethodException">if <paramref name="arrayType"/>'s factory has no method <see cref="IArrayFactory.ReconstructArray"/></exception>
		public static ValueArray<T> Create<T>(Type arrayType, IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (arrayType is null || !arrayType.IsSubclassOf(typeof(ValueArray<T>)) || arrayType.IsAbstract)
				throw new ArgumentNullException(nameof(arrayType));
			var delegateReconstruct = GetDelegate<IArrayFactory.DelegateCreateArray<T>>(arrayType, nameof(IArrayFactory.CreateArray), cacheReconstruct);
			return delegateReconstruct(size, onHost, otherInfo);
		}
		#endregion

		#region reconstruct
		private static readonly Dictionary<Type, Delegate> cacheReconstruct = new();

		/// <summary>
		/// Reconstruct an array of concrete type <typeparamref name="TArray"/> using reflection.
		/// </summary>
		/// <typeparam name="TArray">The concrete array type</typeparam>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="size">The size of the array about to create</param>
		/// <param name="pointers">The pointers obtained from <see cref="ValueArray{T}.GetPointers"/></param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetOtherInfo"/></param>
		/// <returns>created array of type <typeparamref name="TArray"/></returns>
		/// <remarks>If <typeparamref name="TArray"/> is a user-defined class that inherits <see cref="ValueArray{T}"/>, its factory must also be created with the same class name and a postfix "Factory" which should also lie in the same naming space.</remarks>
		/// <exception cref="TypeAccessException">if <typeparamref name="TArray"/>'s factory is not a sub-class of <see cref="PureArrayFactory"/></exception>
		/// <exception cref="TypeLoadException">if <typeparamref name="TArray"/>'s factory cannot be loaded</exception>
		/// <exception cref="System.Reflection.AmbiguousMatchException">if <typeparamref name="TArray"/>'s factory has multiple method named <see cref="IArrayFactory.ReconstructArray"/></exception>
		/// <exception cref="MissingMethodException">if <typeparamref name="TArray"/>'s factory has no method <see cref="IArrayFactory.ReconstructArray"/></exception>
		public static TArray Reconstruct<TArray, T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where TArray : ValueArray<T>, new() where T : struct, IComparable<T>
		{
			var delegateReconstruct = GetDelegate<IArrayFactory.DelegateReconstructArray<T>>(typeof(TArray), nameof(IArrayFactory.ReconstructArray), cacheReconstruct);
			return delegateReconstruct(size, pointers, otherInfo) as TArray;
		}

		/// <summary>
		/// Reconstruct an array of concrete type <paramref name="arrayType"/> using reflection.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="arrayType">The type of the array to reconstruct</param>
		/// <param name="size">The size of the array about to create</param>
		/// <param name="pointers">The pointers obtained from <see cref="ValueArray{T}.GetPointers"/></param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetOtherInfo"/></param>
		/// <returns>created array of type <paramref name="arrayType"/></returns>
		/// <remarks>If <paramref name="arrayType"/> is a user-defined class that inherits <see cref="ValueArray{T}"/>, its factory must also be created with the same class name and a postfix "Factory" which should also lie in the same naming space.</remarks>
		/// <exception cref="ArgumentNullException">if <paramref name="arrayType"/> is not a sub-type of <see cref="ValueArray{T}"/> or is not a concrete one</exception>
		/// <exception cref="TypeAccessException">if <paramref name="arrayType"/>'s factory is not a sub-class of <see cref="PureArrayFactory"/></exception>
		/// <exception cref="TypeLoadException">if <paramref name="arrayType"/>'s factory cannot be loaded</exception>
		/// <exception cref="System.Reflection.AmbiguousMatchException">if <paramref name="arrayType"/>'s factory has multiple method named <see cref="IArrayFactory.ReconstructArray"/></exception>
		/// <exception cref="MissingMethodException">if <paramref name="arrayType"/>'s factory has no method <see cref="IArrayFactory.ReconstructArray"/></exception>
		public static ValueArray<T> Reconstruct<T>(Type arrayType, IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (arrayType is null || !arrayType.IsSubclassOf(typeof(ValueArray<T>)) || arrayType.IsAbstract)
				throw new ArgumentNullException(nameof(arrayType));
			var delegateReconstruct = GetDelegate<IArrayFactory.DelegateReconstructArray<T>>(arrayType, nameof(IArrayFactory.ReconstructArray), cacheReconstruct);
			return delegateReconstruct(size, pointers, otherInfo);
		}
		#endregion

		#region from C# array factory methods
		/// <summary>
		/// Create from C# array
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">a C# array</param>
		/// <param name="onHost">create on host memory or device, default device</param>
		/// <returns>the created <see cref="VectorBase{T}"/></returns>
		public static VectorBase<T> FromArray<T>(T[] array, bool onHost = false) where T : struct, IComparable<T>
		{
			return (DenseVector<T>)(array, onHost);
		}

		/// <summary>
		/// Create from C# 2D array
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">a C# 2D array</param>
		/// <param name="onHost">create on host memory or device, default device</param>
		/// <returns>the created <see cref="MatrixBase{T}"/></returns>
		public static MatrixBase<T> FromArray<T>(T[,] array, bool onHost = false) where T : struct, IComparable<T>
		{
			return (DenseMatrix<T>)(array, onHost);
		}

		/// <summary>
		/// Create from C# arrays as columns
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">C# arrays as columns</param>
		/// <param name="onHost">create on host memory or device, default device</param>
		/// <returns>the created <see cref="MatrixBase{T}"/></returns>
		public static MatrixBase<T> FromColumnArrays<T>(T[][] array, bool onHost = false) where T : struct, IComparable<T>
		{
			if (array is null || array.Length == 0 || array.Any(a => a is null || a.Length != array[0].Length))
				throw new ArgumentNullException(nameof(array));

			var len = array[0].Length;
			T[] arr = new T[array.Length * len];
			for (int i = 0; i < array.Length; i++)
			{
				Array.Copy(array[i], 0, arr, len * i, len);
			}
			return (DenseMatrix<T>)(arr, len, onHost);
		}

		/// <summary>
		/// Create from C# array as a column-majored 1D array
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">C# array as a column-majored 1D array</param>
		/// <param name="rows">The number of rows</param>
		/// <param name="onHost">create on host memory or device, default device</param>
		/// <returns>the created <see cref="MatrixBase{T}"/></returns>
		public static MatrixBase<T> FromColumnMajoredArray<T>(T[] array, long rows, bool onHost = false) where T : struct, IComparable<T>
		{
			if (array is null || array.Length % rows != 0)
				throw new ArgumentNullException(nameof(array));

			return (DenseMatrix<T>)(array, rows, onHost);
		}

		/// <summary>
		/// Create from C# value and index array
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">a C# value array</param>
		/// <param name="index">a C# index array</param>
		/// <param name="length">The length of the array, default 0 means using the maximum value of <paramref name="index"/></param>
		/// <param name="onHost">create on host memory or device, default device</param>
		/// <returns>the created <see cref="VectorBase{T}"/></returns>
		public static VectorBase<T> FromIndexedArray<T>(T[] array, int[] index, int length = 0, bool onHost = false) where T : struct, IComparable<T>
		{
			return (SparseVector<T>)(array, index, length == 0 ? index.Max() : length, onHost);
		}

		/// <summary>
		/// Create from C# value and index array
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="array">a C# value array</param>
		/// <param name="rowIndex">a C# row index array</param>
		/// <param name="columnIndex">a C# column index array</param>
		/// <param name="rows">The length of the array, default 0 means using the maximum value of <paramref name="rowIndex"/></param>
		/// <param name="columns">The length of the array, default 0 means using the maximum value of <paramref name="columnIndex"/></param>
		/// <param name="onHost">create on host memory or device, default device</param>
		/// <returns>the created <see cref="MatrixBase{T}"/></returns>
		public static MatrixBase<T> FromIndexedArray<T>(T[] array, int[] rowIndex, int[] columnIndex, int rows = 0, int columns = 0, bool onHost = false) where T : struct, IComparable<T>
		{
			return (SparseMatrix<T>)(array, rowIndex, columnIndex, rows == 0 ? rowIndex.Max() : rows, columns == 0 ? columnIndex.Max() : columns, SparseMatrixFormat.COOC, onHost);
		}
		#endregion

		#region public helper
		/// <summary>
		/// Check the pointer and cast it into target type
		/// </summary>
		/// <typeparam name="T">The type to cast to</typeparam>
		/// <param name="pointers">The pointers' dictionary</param>
		/// <param name="name">pointer name to check</param>
		/// <param name="size">The size to check, default 0 means do not check</param>
		/// <returns>the casted <see cref="Storage{T}"/></returns>
		public static Storage<T> CheckPointer<T>(IReadOnlyDictionary<string, IStorage> pointers, string name, long size = 0) where T : struct
		{
			if (pointers is null || !pointers.ContainsKey(name))
				throw new ArgumentNullException(nameof(pointers));
			Storage<T> data;
			if (pointers[name] is Storage<byte> pb)
			{
				data = pb.As<T>();
			}
			else if (pointers[name] is Storage<T> pt)
				data = pt;
			else
				throw new ArgumentNullException(nameof(pointers));
			if (size > 0)
			{
				if (pointers[name].LengthInBytes / Storage<T>.SizeOfT < size)
					throw new ArgumentException(Resource.VectorWrongSize, nameof(pointers));
				data = data.MakeSize(size);
			}
			return data;
		}
		#endregion
	}


	#region internal concrete factories
	internal sealed class DenseVectorFactory : IArrayFactory
	{
		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (size is null || size.Count != 1)
				throw new ArgumentNullException(nameof(size));
			return new DenseVector<T>(size[0], onHost);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (size is null || size.Count != 1)
				throw new ArgumentNullException(nameof(size));

			var data = PureArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName, size[0]);
			return new DenseVector<T>(data, size[0]);
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(DenseVector<T> vec) where T : struct, IComparable<T>
		{
			return new Dictionary<string, IStorage> 
			{ 
				[ValueArray<T>.StorageName] = vec.Storage
			};
		}
	}

	internal sealed class SparseVectorFactory : IArrayFactory
	{
		private const string IndexPointerName = nameof(SparseVector<int>.IndexPointer);
		// Ignore Spelling: nnz
		private const string NonZerosName = nameof(SparseVector<int>.NonZero);

		private static long Check(IReadOnlyList<long> size, IReadOnlyDictionary<string, object> otherInfo)
		{
			if (size is null || size.Count != 1)
				throw new ArgumentNullException(nameof(size));
			if (otherInfo is null || !otherInfo.ContainsKey(NonZerosName))
				throw new ArgumentNullException(nameof(otherInfo));
			var type = otherInfo[NonZerosName].GetType();
			long nnz = type == typeof(long) ? (long)otherInfo[NonZerosName] :
						type == typeof(int) ? (int)otherInfo[NonZerosName] :
							throw new ArgumentNullException(nameof(otherInfo));
			return nnz;
		}

		private static (Storage<T> value, Storage<int> index) Check<T>(IReadOnlyDictionary<string, IStorage> pointers) where T : struct, IComparable<T>
		{
			var value = PureArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName);
			var index = PureArrayFactory.CheckPointer<int>(pointers, IndexPointerName);
			return (value, index);
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			long nnz = Check(size, otherInfo);
			return new SparseVector<T>(size[0], nonZeros: nnz, onHost);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			var (value, index) = Check<T>(pointers);
			return new SparseVector<T>(size[0], value, index);
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(SparseVector<T> vec) where T : struct, IComparable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = vec.Storage,
				[IndexPointerName] = vec.IndexPointer
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(SparseVector<T> vec) where T : struct, IComparable<T>
		{
			return new Dictionary<string, object>
			{
				[NonZerosName] = vec.NonZero
			};
		}
	}

	internal sealed class DenseMatrixFactory : IArrayFactory
	{
		// Ignore Spelling: ld herm
		private const string HermitianName = nameof(DenseMatrix<int>.Hermitian);
		private const string LeadDimensionName = nameof(DenseMatrix<int>.LeadDim);

		private static bool Check(IReadOnlyList<long> size, IReadOnlyDictionary<string, object> otherInfo)
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				return false;
			var type = otherInfo[HermitianName].GetType();
			return type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
		}

		private static (long ld, bool herm, Storage<T> data) Check<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo) where T : struct, IComparable<T>
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));

			bool herm;
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				herm = false;
			else
			{
				var type = otherInfo[HermitianName].GetType();
				herm = type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
			}
			if (herm && size[0] != size[1])
				throw new ArgumentException(Resource.MatMustSquare, nameof(size));

			long ld;
			if (otherInfo is null || !otherInfo.ContainsKey(LeadDimensionName))
				ld = size[0];
			else
			{
				var type = otherInfo[LeadDimensionName].GetType();
				ld = type == typeof(long) ? (long)otherInfo[LeadDimensionName] :
						type == typeof(int) ? (int)otherInfo[LeadDimensionName] :
								throw new ArgumentNullException(nameof(otherInfo));
			}

			var data = PureArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName, ld * size[1]);
			return (ld, herm, data);
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			bool herm = Check(size, otherInfo);
			return new DenseMatrix<T>(size[0], size[1], onHost, herm);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			var (ld, herm, data) = Check<T>(size, pointers, otherInfo);
			return new DenseMatrix<T>(data, size[0], size[1], ld, herm);
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(DenseMatrix<T> mat) where T : struct, IComparable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = mat.Storage
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(DenseMatrix<T> mat) where T : struct, IComparable<T>
		{
			return new Dictionary<string, object>
			{
				[HermitianName] = mat.Hermitian,
				[LeadDimensionName] = mat.LeadDim
			};
		}
	}

	internal sealed class SparseMatrixFactory : IArrayFactory
	{
		private const string RowIndexName = nameof(SparseMatrix<int>.RowPointer);
		private const string ColumnIndexName = nameof(SparseMatrix<int>.ColumnPointer);

		private const string NonZerosName = nameof(SparseMatrix<int>.NonZero);
		private const string HermitianName = nameof(SparseMatrix<int>.Hermitian);
		private const string FormatName = nameof(SparseMatrix<int>.Format);

		private static (long nnz, SparseMatrixFormat format, bool herm) Check(IReadOnlyList<long> size, IReadOnlyDictionary<string, object> otherInfo)
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));

			bool herm;
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				herm = false;
			else
			{
				var type = otherInfo[HermitianName].GetType();
				herm = type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
			}

			SparseMatrixFormat format;
			if (otherInfo is null || !otherInfo.ContainsKey(FormatName))
				throw new ArgumentNullException(nameof(otherInfo));
			else
			{
				var type = otherInfo[FormatName].GetType();
				format = type == typeof(SparseMatrixFormat) ? (SparseMatrixFormat)otherInfo[FormatName] : throw new ArgumentNullException(nameof(otherInfo));
			}

			long nnz;
			if (otherInfo is null || !otherInfo.ContainsKey(NonZerosName))
				throw new ArgumentNullException(nameof(otherInfo));
			else
			{
				var type = otherInfo[NonZerosName].GetType();
				nnz = type == typeof(long) ? (long)otherInfo[NonZerosName] :
						type == typeof(int) ? (int)otherInfo[NonZerosName] :
								throw new ArgumentNullException(nameof(otherInfo));
			}

			return (nnz, format, herm);
		}

		private static (SparseMatrixFormat format, bool herm, Storage<T> value, Storage<int> row, Storage<int> col) Check<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo) where T : struct, IComparable<T>
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));

			bool herm;
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				herm = false;
			else
			{
				var type = otherInfo[HermitianName].GetType();
				herm = type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
			}
			if (herm && size[0] != size[1])
				throw new ArgumentException(Resource.MatMustSquare, nameof(size));

			SparseMatrixFormat format;
			if (otherInfo is null || !otherInfo.ContainsKey(FormatName))
				format = SparseMatrixFormat.Any;
			else
			{
				var type = otherInfo[FormatName].GetType();
				format = type == typeof(SparseMatrixFormat) ? (SparseMatrixFormat)otherInfo[FormatName] : throw new ArgumentNullException(nameof(otherInfo));
			}

			long nnz;
			if (otherInfo is null || !otherInfo.ContainsKey(NonZerosName))
				nnz = 0;
			else
			{
				var type = otherInfo[NonZerosName].GetType();
				nnz = type == typeof(long) ? (long)otherInfo[NonZerosName] :
						type == typeof(int) ? (int)otherInfo[NonZerosName] :
								throw new ArgumentNullException(nameof(otherInfo));
			}

			if (format == SparseMatrixFormat.Any && nnz == 0)
				throw new ArgumentNullException(nameof(otherInfo));

			var value = PureArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName);
			var row = PureArrayFactory.CheckPointer<int>(pointers, RowIndexName);
			var col = PureArrayFactory.CheckPointer<int>(pointers, ColumnIndexName);

			long rowLen = pointers[RowIndexName].LengthInBytes / sizeof(int);
			long colLen = pointers[ColumnIndexName].LengthInBytes / sizeof(int);

			if (format == SparseMatrixFormat.Any)
			{
				if (rowLen == nnz && colLen == nnz) // do not know COOC or COOR
					throw new ArgumentException(Resource.FormatNotAtomic, nameof(otherInfo));
				else if (rowLen == size[0] + 1 && colLen == nnz)
					format = SparseMatrixFormat.CSR;
				else if (colLen == size[1] + 1 && rowLen == nnz)
					format = SparseMatrixFormat.CSC;
				else
					throw new ArgumentException(Resource.NotSupportedFormat, nameof(otherInfo));
			}
			else if (nnz == 0)
			{
				switch (format)
				{
					case SparseMatrixFormat.COOR:
					case SparseMatrixFormat.COOC:
						if (rowLen == colLen)
							nnz = rowLen;
						else
							throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
						break;
					case SparseMatrixFormat.CSR:
						if (rowLen == size[0] + 1)
							nnz = colLen;
						else
							throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
						break;
					case SparseMatrixFormat.CSC:
						if (colLen == size[1] + 1)
							nnz = rowLen;
						else
							throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
						break;
					default:
						throw new ArgumentException(Resource.FormatNotAtomic, nameof(otherInfo));
				}
			}

			switch (format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					if (rowLen != colLen || rowLen != nnz)
						throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
					break;
				case SparseMatrixFormat.CSR:
					if (rowLen != size[0] + 1 || colLen != nnz)
						throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
					break;
				case SparseMatrixFormat.CSC:
					if (colLen != size[1] + 1 || rowLen != nnz)
						throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
					break;
				default:
					throw new ArgumentException(Resource.FormatNotAtomic, nameof(otherInfo));
			}

			return (format, herm, value, row, col);
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			var (nnz, format, herm) = Check(size, otherInfo);
			return new AbstractSparseMatrix<T>(size[0], size[1], nnz, format, onHost, herm);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			var (format, herm, value, row, col) = Check<T>(size, pointers, otherInfo);
			return new AbstractSparseMatrix<T>(size[0], size[1], value, row, col, format, herm);
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(SparseMatrix<T> mat) where T : struct, IComparable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = mat.Storage,
				[RowIndexName] = mat.RowPointer,
				[ColumnIndexName] = mat.ColumnPointer
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(SparseMatrix<T> mat) where T : struct, IComparable<T>
		{
			return new Dictionary<string, object>
			{
				[HermitianName] = mat.Hermitian,
				[NonZerosName] = mat.NonZero,
				[FormatName] = mat.Format
			};
		}
	}


	internal sealed class DenseTensorFactory : IArrayFactory
	{
		private const string LabelName = nameof(DenseTensor<int>.Label);

		private static IReadOnlyList<char> GetLabel(IReadOnlyDictionary<string, object> otherInfo)
		{
			if (otherInfo is null || !otherInfo.ContainsKey(LabelName))
				return null;
			if (otherInfo[LabelName] is IReadOnlyList<char> read)
				return read;
			if (otherInfo[LabelName] is char[] array)
				return array;
			if (otherInfo[LabelName] is IList<char> list)
			{
				var arr = new char[list.Count];
				list.CopyTo(arr, 0);
				return arr;
			}
			if (otherInfo[LabelName] is Newtonsoft.Json.Linq.JArray json)
				return json.ToObject<char[]>();
			return null; // cannot identify type
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (size is null || size.Count == 0)
				throw new ArgumentNullException(nameof(size));
			return new DenseTensor<T>(size, GetLabel(otherInfo), onHost);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (size is null || size.Count == 0)
				throw new ArgumentNullException(nameof(size));

			var data = PureArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName, size.Prod());
			var label = GetLabel(otherInfo);
			var result = new DenseTensor<T>(data, size);
			if (label != null)
				result.Label = label;
			return result;
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(DenseTensor<T> ten) where T : struct, IComparable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = ten.Storage
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(DenseTensor<T> ten) where T : struct, IComparable<T>
		{
			return new Dictionary<string, object>
			{
				[LabelName] = ten.Label
			};
		}
	}
	#endregion
}
