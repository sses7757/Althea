using System;

using Althea.Array;


namespace Althea.Rng
{
	/// <summary>
	/// The random number generator API library wrapper
	/// </summary>
	public static class API
	{
		#region base
		/// <summary>
		/// Static class initializer
		/// </summary>
		static API()
		{
			if (GlobalSettings.RandGPU != null)
				GPUconstructor = GlobalSettings.RandGPU.GetConstructor(Array.Empty<Type>());
			else
				GPUconstructor = typeof(Cuda.CudaRand).GetConstructor(Array.Empty<Type>());
			if (GlobalSettings.RandCPU != null)
				CPUconstructor = GlobalSettings.RandCPU.GetConstructor(Array.Empty<Type>());
			else
				CPUconstructor = typeof(Mkl.MklRand).GetConstructor(Array.Empty<Type>());
			Initialize();
		}

		/// <summary>
		/// Reset the RNG libraries
		/// </summary>
		public static void Reset()
		{
			try
			{
				GPU.Dispose();
				CPU.Dispose();
			}
			catch (StatusException e)
			{
				Log.Write($"Error at reseting RNG library \"{e.Message}\":" + Environment.NewLine + e.StackTrace, level: LogLevel.Error);
			}
			finally
			{
				Initialize();
			}
		}

		/// <summary>
		/// Singleton RNG API of GPU routine
		/// </summary>
		public static IRand GPU => _GPUInit.Value;

		/// <summary>
		/// Singleton RNG API of CPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static IRand CPU => _CPUInit.Value;

		private static readonly System.Reflection.ConstructorInfo GPUconstructor, CPUconstructor;

		private static Lazy<IRand> _GPUInit, _CPUInit;

		private static void Initialize()
		{
			_GPUInit = new Lazy<IRand>(() => { var v = GPUconstructor.Invoke(Array.Empty<object>()) as IRand; v.SetSeed(_seed); return v; }, true);
			_CPUInit = new Lazy<IRand>(() => { var v = CPUconstructor.Invoke(Array.Empty<object>()) as IRand; v.SetSeed(_seed); return v; }, true);
		}
		#endregion

		#region generate
		private static int _seed = new Random().Next();

		/// <summary>
		/// Set seed for all random number generators
		/// </summary>
		/// <param name="seed">The seed, default null means a random one</param>
		public static void SetSeed(int? seed = null)
		{
			seed ??= (new Random().Next());
			_seed = seed.Value;
			if (_GPUInit.IsValueCreated)
				GPU.SetSeed(_seed);
			if (_CPUInit.IsValueCreated)
				CPU.SetSeed(_seed);
		}

		/// <summary>
		/// Fill the array with random number ∈ (0.0, 1.0] if the <typeparamref name="T"/> is a float type or all bit random if it is a integer type. If the <typeparamref name="T"/> is of complex type, each of the components are filled with random number ∈ (0.0, 1.0].
		/// </summary>
		/// <param name="array">array to be filled</param>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types, also, the <see cref="int"/> and <see cref="long"/> are supported</typeparam>
		/// <exception cref="ArgumentNullException">if <paramref name="array"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="array"/>'s length ≤ 0</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public static void FillWithRandom<T>(PureArray<T> array) where T : struct, IComparable<T>
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array), Resource.ArrayCannotNull);
			if (array.ActualLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(array), Resource.ArraySize);

			var onHost = CudaCSharpHelpers.CheckOnHost(array);
			long length = array.ActualLength;
			Action<IntPtr, long> func;
			if (typeof(T) == typeof(long) || typeof(T) == typeof(long) || !array.IsRealType)
				length *= 2;
			IRand rand = onHost ? CPU : GPU;
			if (typeof(T) == typeof(long) || typeof(T) == typeof(long))
				func = rand.GenerateInt;
			func = (default(T).ToDataType()) switch
			{
				DataType.RealInt32 => rand.GenerateInt,
				DataType.RealSingle => rand.GenerateSingle,
				DataType.RealDouble => rand.GenerateDouble,
				DataType.ComplexSingle => rand.GenerateSingle,
				DataType.ComplexDouble => rand.GenerateDouble,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(array.Pointer, length);
		}
		#endregion
	}
}
