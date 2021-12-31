using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;


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
	}
	#endregion

	#region native types
	/// <summary>
	/// The static class for primitive and custom native types
	/// </summary>
	/// <typeparam name="T">The type of number</typeparam>
	public static class NumberTypes<T> where T : unmanaged
	{
		private static readonly Type? interfaceType = null;

		static NumberTypes()
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

		/// <summary>
		/// Get the <see cref="DataTypeClassification"/> of type <typeparamref name="T"/>. 0 means unknown.
		/// </summary>
		public static DataTypeClassification Classification
		{
			get
			{
				var val = (DataTypeClassification?)interfaceType?.GetProperty(nameof(ICustomNativeType<Complex<float>>.Classification), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
				if (val.HasValue)
					return val.Value;
				return default(T) switch
				{
					byte or ushort or uint or ulong or nuint or UIntPtr => DataTypeClassification.UnsignedInteger,
					sbyte or short or int or long or uint or IntPtr => DataTypeClassification.SignedInteger,
					Half or float or double => DataTypeClassification.FloatPoint_IEEE754,
					_ => 0,
				};
			}
		}

		/// <summary>
		/// Get the machine precision of type <typeparamref name="T"/>. 0 means unknown.
		/// </summary>
		public static double MachinePrecision
		{
			get
			{
				var val = (double?)interfaceType?.GetProperty(nameof(ICustomNativeType<Complex<float>>.MachinePrecision), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
				if (val.HasValue)
					return val.Value;
				return default(T) switch
				{
					byte or ushort or uint or ulong or nuint or UIntPtr or sbyte or short or int or long or uint or IntPtr => 1,
					Half => 0.0009765625,
					float => 1.1920928955078125E-07,
					double => 2.220446049250313E-16,
					_ => 0,
				};
			}
		}
	}
	#endregion
}
