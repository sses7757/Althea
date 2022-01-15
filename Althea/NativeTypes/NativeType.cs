using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Althea.NativeTypes
{
	#region custom native type interface
	/// <summary>
	/// The interface for custom native types
	/// </summary>
	/// <typeparam name="TSelf">The type of actual struct that implement this interface</typeparam>
	public interface ICustomNativeType<TSelf> where TSelf : ICustomNativeType<TSelf>
	{
		/// <summary>
		/// Statically get the <see cref="DataTypeClassification"/> of <typeparamref name="TSelf"/>
		/// </summary>
		abstract static DataTypeClassification Classification { get; }

		/// <summary>
		/// Statically get the machine precision of <typeparamref name="TSelf"/>
		/// </summary>
		abstract static double MachinePrecision { get; }

		/// <summary>
		/// Statically get whether <typeparamref name="TSelf"/> is a complex type or not
		/// </summary>
		abstract static bool IsComplex { get; }
	}
	#endregion

	#region native types
	/// <summary>
	/// The static class for primitive and custom native types' meta data
	/// </summary>
	/// <typeparam name="T">An unmanaged struct which implements <see cref="INumber{TSelf}"/> as the number type</typeparam>
	public static class NativeType<T> where T : unmanaged, INumber<T>
	{
		private static readonly Type? interfaceType = null;

		static NativeType()
		{
			try
			{
				if (!typeof(T).IsPrimitive)
					interfaceType = typeof(ICustomNativeType<>).MakeGenericType(typeof(T));
			}
			catch (Exception)
			{
				throw new InvalidOperationException(Resources.Support.DataType);
			}
			if (!typeof(T).IsPrimitive && !typeof(T).IsAssignableTo(interfaceType))
				throw new InvalidOperationException(Resources.Support.DataType);
		}

		private const System.Reflection.BindingFlags PUBLIC_STATIC = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;

		private static DataTypeClassification? classification = null;
		/// <summary>
		/// Get the <see cref="DataTypeClassification"/> of type <typeparamref name="T"/>. 0 means unknown.
		/// </summary>
		public static DataTypeClassification Classification
		{
			get
			{
				if (classification is not null)
					return classification.Value;
				var val = (DataTypeClassification?)interfaceType?.GetProperty(nameof(ICustomNativeType<Complex<float>>.Classification), PUBLIC_STATIC)?.GetValue(null);
				if (val.HasValue)
				{
					classification = val.Value;
					return val.Value;
				}
				classification = default(T) switch
				{
					byte or ushort or uint or ulong or nuint => DataTypeClassification.UnsignedInteger,
					sbyte or short or int or long or nint => DataTypeClassification.SignedInteger,
					Half or float or double => DataTypeClassification.FloatPoint_IEEE754,
					_ => 0,
				};
				return classification.Value;
			}
		}

		private static double? machinePrecision = null;
		/// <summary>
		/// Get the machine precision of type <typeparamref name="T"/>. 0 means unknown.
		/// </summary>
		public static double MachinePrecision
		{
			get
			{
				if (machinePrecision is not null)
					return machinePrecision.Value;
				var val = (double?)interfaceType?.GetProperty(nameof(ICustomNativeType<Complex<float>>.MachinePrecision), PUBLIC_STATIC)?.GetValue(null);
				if (val.HasValue)
				{
					machinePrecision = val.Value;
					return val.Value;
				}
				machinePrecision = default(T) switch
				{
					byte or ushort or uint or ulong or nuint or sbyte or short or int or long or nint => 1,
					Half => 0.0009765625,
					float => 1.1920928955078125E-07,
					double => 2.220446049250313E-16,
					_ => 0,
				};
				return machinePrecision.Value;
			}
		}

		private static bool? isComplex = null;
		/// <summary>
		/// Get a <see cref="bool"/> indicating whether <typeparamref name="T"/> is a complex type or not.
		/// </summary>
		public static bool IsComplex
		{
			get
			{
				if (isComplex is not null)
					return isComplex.Value;
				var val = (bool?)interfaceType?.GetProperty(nameof(ICustomNativeType<Complex<float>>.IsComplex), PUBLIC_STATIC)?.GetValue(null);
				isComplex = val ?? false;
				return isComplex.Value;
			}
		}

		/// <summary>
		/// Get the size of type <typeparamref name="T"/> (in bytes).
		/// </summary>
		public static unsafe int Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => sizeof(T);
		}

		/// <summary>
		/// Get the <see cref="NativeTypes.DataType"/> of <typeparamref name="T"/>.
		/// </summary>
		/// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not a supported data type</exception>
		public static DataType DataType => DataTypeExtension.ToDataType<T>();
	}
	#endregion
}
