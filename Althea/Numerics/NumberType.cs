using System.Reflection;
using System.Runtime.CompilerServices;


namespace Althea.Numerics
{
	#region native types
	internal static class ComplexConverter
	{
		private static unsafe TComp DirectConvertComplexConj<TComp, TReal>(TComp value)
			where TComp : unmanaged, INumber<TComp> where TReal : unmanaged, INumber<TReal>
		{
			TComp result = value;
			TReal real = *(TReal*)&value;
			((TReal*)&result)[0] = real;
			return result;
		}

		private static unsafe bool DirectConvertComp2Comp<TFrom, TTFrom, TTo, TTTo>(TFrom value, out TTo result)
			where TFrom : unmanaged, INumber<TFrom> where TTFrom : unmanaged, INumber<TTFrom>
			where TTo : unmanaged, INumber<TTo> where TTTo : unmanaged, INumber<TTTo>
		{
			result = default;
			TTo temp = default;
			if (!NumberConvert.TryConvert(*(TTFrom*)&value, out TTTo real))
				return false;
			if (!NumberConvert.TryConvert(*(1 + (TTFrom*)&value), out TTTo imag))
				return false;
			*(TTTo*)&temp = real; *(1 + (TTTo*)&temp) = imag;
			result = temp;
			return true;
		}

		internal static class Conjugater<T> where T : INumber<T>
		{
			internal delegate T DelegateComplexConjuagte(T value);

			internal static readonly DelegateComplexConjuagte? Default;

			static Conjugater()
			{
				if (!typeof(T).IsGenericType || typeof(T).GenericTypeArguments.Length != 1 || !NumberType<T>.IsComplex)
				{
					Default = null;
					return;
				}
				try
				{
					var method = typeof(ComplexConverter).GetMethod(nameof(ComplexConverter.DirectConvertComplexConj), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(typeof(T), typeof(T).GenericTypeArguments[0]);
					Default = method?.CreateDelegate<DelegateComplexConjuagte>();
				}
				catch (Exception)
				{
					Default = null;
				}
			}
		}

		private static unsafe TTo SatConvertComp2Comp<TFrom, TTFrom, TTo, TTTo>(TFrom v)
			where TFrom : unmanaged, INumber<TFrom> where TTFrom : unmanaged, INumber<TTFrom>
			where TTo : unmanaged, INumber<TTo> where TTTo : unmanaged, INumber<TTTo>
		{
			TTTo real = INumberBase<TTTo>.CreateSaturating(*(TTFrom*)&v), imag = INumberBase<TTTo>.CreateSaturating(*(1 + (TTFrom*)&v));
			TTo result = default;
			*(TTTo*)&result = real; *(1 + (TTTo*)&result) = imag;
			return result;
		}
		private static unsafe TTo TruConvertComp2Comp<TFrom, TTFrom, TTo, TTTo>(TFrom v)
			where TFrom : unmanaged, INumber<TFrom> where TTFrom : unmanaged, INumber<TTFrom>
			where TTo : unmanaged, INumber<TTo> where TTTo : unmanaged, INumber<TTTo>
		{
			TTTo real = INumberBase<TTTo>.CreateSaturating(*(TTFrom*)&v), imag = INumberBase<TTTo>.CreateSaturating(*(1 + (TTFrom*)&v));
			TTo result = default;
			*(TTTo*)&result = real; *(1 + (TTTo*)&result) = imag;
			return result;
		}

		internal static class Converter<TTo, TFrom> where TTo : INumber<TTo> where TFrom : INumber<TFrom>
		{
			internal delegate bool DelegateConvertToComplex(TFrom value, out TTo result);
			internal delegate TTo DelegateNonDirectConvertToComplex(TFrom value);

			internal static readonly DelegateConvertToComplex? Default;
			internal static readonly DelegateNonDirectConvertToComplex? Saturating, Truncating;

			static Converter()
			{
				if (!typeof(TFrom).IsGenericType || typeof(TFrom).GenericTypeArguments.Length != 1)
				{
					Default = null;
					Saturating = Truncating = null;
					return;
				}
				try
				{
					var method = typeof(ComplexConverter).GetMethod(nameof(ComplexConverter.DirectConvertComp2Comp), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(typeof(TFrom), typeof(TFrom).GenericTypeArguments[0], typeof(TTo), typeof(TTo).GenericTypeArguments[0]);
					Default = method?.CreateDelegate<DelegateConvertToComplex>();
					method = typeof(ComplexConverter).GetMethod(nameof(ComplexConverter.SatConvertComp2Comp), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(typeof(TFrom), typeof(TFrom).GenericTypeArguments[0], typeof(TTo), typeof(TTo).GenericTypeArguments[0]);
					Saturating = method?.CreateDelegate<DelegateNonDirectConvertToComplex>();
					method = typeof(ComplexConverter).GetMethod(nameof(ComplexConverter.TruConvertComp2Comp), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(typeof(TFrom), typeof(TFrom).GenericTypeArguments[0], typeof(TTo), typeof(TTo).GenericTypeArguments[0]);
					Truncating = method?.CreateDelegate<DelegateNonDirectConvertToComplex>();
				}
				catch (Exception)
				{
					Default = null;
					Saturating = Truncating = null;
				}
			}
		}
	}

	/// <summary>
	/// The static class for primitive and custom number types' conversion
	/// </summary>
	public static class NumberConvert
	{
		/// <summary>
		/// Create a number of type <typeparamref name="T2"/> from the given number of type <typeparamref name="T1"/>.
		/// </summary>
		/// <typeparam name="T1">The input number type</typeparam>
		/// <typeparam name="T2">The output number type</typeparam>
		/// <param name="x">The input number</param>
		/// <param name="y">The output number</param>
		/// <returns>Success or not.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryConvert<T1, T2>(T1 x, out T2 y) where T1 : INumber<T1> where T2 : INumber<T2>
		{
			if (INumber<T2>.CreateSaturating(x, out y))
				return true;
			var createT2 = NumberType<T2>.GetTryCreateOther<T1>();
			if (createT2 is null)
				return false;
			return createT2(x, out y);
		}

		/// <summary>
		/// Create a number of type <typeparamref name="T2"/> from the given number of type <typeparamref name="T1"/>.
		/// </summary>
		/// <typeparam name="T1">The input number type</typeparam>
		/// <typeparam name="T2">The output number type</typeparam>
		/// <param name="x">The input number</param>
		/// <returns><paramref name="x"/> as <typeparamref name="T2"/></returns>
		/// <exception cref="NotSupportedException">If the conversion is not available</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T2 As<T1, T2>(this T1 x) where T1 : INumber<T1> where T2 : INumber<T2>
		{
			if (TryConvert(x, out T2 y))
				return y;
			else
				throw new NotSupportedException(Resources.ArithmeticError.DataTypeNotAllow);
		}

		/// <summary>
		/// Get the conjugate of a number <paramref name="x"/> of any number type if it is a complex type, otherwise <paramref name="x"/> itself.
		/// </summary>
		/// <typeparam name="T">The number type</typeparam>
		/// <param name="x">The number to get conjugate</param>
		/// <returns>The conjugate of <paramref name="x"/> if it is a complex type, otherwise <paramref name="x"/> itself.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Conjugate<T>(this T x) where T : unmanaged, INumber<T>
		{
			if (!NumberType<T>.IsComplex)
				return x;
			if (ComplexConverter.Conjugater<T>.Default is null)
				throw new NotSupportedException(Resources.ArithmeticError.DataTypeNotAllow);
			return ComplexConverter.Conjugater<T>.Default.Invoke(x);
		}

		/// <summary>
		/// Get the real part of a number <paramref name="x"/> of any number type if it is a complex type, otherwise <paramref name="x"/> itself.
		/// </summary>
		/// <typeparam name="T">The number type</typeparam>
		/// <param name="x">The number to get real part</param>
		/// <returns>The real part of <paramref name="x"/> if it is a complex type, otherwise <paramref name="x"/> itself.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ToReal<T>(this T x) where T : unmanaged, INumber<T> => (x + x.Conjugate()) / (T.One + T.One);

		/// <summary>
		/// Get the real part of a number <paramref name="x"/> of any number type if it is a complex type, otherwise <paramref name="x"/> itself.
		/// </summary>
		/// <typeparam name="T">The number type</typeparam>
		/// <param name="x">The number to get real part</param>
		/// <returns>The real part of <paramref name="x"/> if it is a complex type, otherwise <paramref name="x"/> itself.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ToImag<T>(this T x) where T : unmanaged, INumber<T> => (x - x.Conjugate()) / (T.One + T.One);
	}

	/// <summary>
	/// The static class for unmanaged primitive and custom number types' constant meta data
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class Unmanaged<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the size of type <typeparamref name="T"/> (in bytes).
		/// </summary>
		public static unsafe int Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => sizeof(T);
		}

		/// <summary>
		/// Get the <see cref="Numerics.DataType"/> of <typeparamref name="T"/>.
		/// </summary>
		/// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not a supported data type</exception>
		public static DataType DataType => DataTypeExtension.ToDataType<T>();
	}

	/// <summary>
	/// The static class for primitive and custom number types' meta data
	/// </summary>
	/// <typeparam name="T">An unmanaged number which implements <see cref="INumber{TSelf}"/> as the number type</typeparam>
	public static class NumberType<T> where T : INumber<T>
	{
		private static readonly Type? interfaceType = null;

		static NumberType()
		{
			if (!typeof(T).IsValueType)
				throw new InvalidOperationException(Resources.ArithmeticError.DataTypeNotAllow);
			try
			{
				if (!typeof(T).IsPrimitive && typeof(T) != typeof(Half))
					interfaceType = typeof(ICustomNumberType<>).MakeGenericType(typeof(T));
			}
			catch (Exception)
			{
				throw new InvalidOperationException(Resources.ArithmeticError.DataTypeNotAllow);
			}
			if (interfaceType is null)
			{
				Classification = default(T) switch
				{
					byte or ushort or uint or ulong or nuint => DataTypeClassification.UnsignedInteger,
					sbyte or short or int or long or nint => DataTypeClassification.SignedInteger,
					Half or float or double => DataTypeClassification.FloatPoint_IEEE754,
					_ => 0,
				};
				MachinePrecision = default(T) switch
				{
					byte or ushort or uint or ulong or nuint or sbyte or short or int or long or nint => 1,
					Half => 0.0009765625,
					float => 1.1920928955078125E-07,
					double => 2.220446049250313E-16,
					_ => 0,
				};
				IsComplex = false;
			}
			else
			{
#pragma warning disable CS8605
				Classification = (DataTypeClassification)typeof(T).GetProperty(nameof(ICustomNumberType<Complex<float>>.Classification), ANY_STATIC)!.GetValue(null);
				MachinePrecision = (double)typeof(T).GetProperty(nameof(ICustomNumberType<Complex<float>>.MachinePrecision), ANY_STATIC)!.GetValue(null);
				IsComplex = (bool)typeof(T).GetProperty(nameof(ICustomNumberType<Complex<float>>.IsComplex), ANY_STATIC)!.GetValue(null);
#pragma warning restore CS8605
			}
		}

		/// <summary>
		/// Get whether type <typeparamref name="T"/> is a primitive type or not.
		/// </summary>
		public static bool IsPrimitive => interfaceType == null;

		private const BindingFlags ANY_STATIC = BindingFlags.Public | BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;

		/// <summary>
		/// Get the <see cref="DataTypeClassification"/> of type <typeparamref name="T"/>. 0 means unknown.
		/// </summary>
		public static readonly DataTypeClassification Classification;

		/// <summary>
		/// Get the machine precision of type <typeparamref name="T"/>. 0 means unknown.
		/// </summary>
		public static readonly double MachinePrecision;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether <typeparamref name="T"/> is a complex type or not.
		/// </summary>
		public static readonly bool IsComplex;

		internal static readonly Dictionary<RuntimeTypeHandle, Delegate> createOthers = new();

		internal delegate bool TryCreateOtherDelegate<TOther>(TOther from, out T to) where TOther : INumber<TOther>;

		internal static TryCreateOtherDelegate<TOther>? GetTryCreateOther<TOther>() where TOther : INumber<TOther>
		{
			var handle = typeof(TOther).TypeHandle;
			if (createOthers.TryGetValue(handle, out var func))
				return (TryCreateOtherDelegate<TOther>)func;
			if (interfaceType is null)
				return null;
			ParameterModifier p = new(2);
			p[0] = false; p[1] = true;
			var method = typeof(T).GetMethod(nameof(ICustomNumberType<Complex<float>>.TryCreateOther), 1, ANY_STATIC, null, new[] { typeof(TOther), typeof(T) }, new[] { p });
			func = method?.CreateDelegate<TryCreateOtherDelegate<TOther>>();
			return (TryCreateOtherDelegate<TOther>?)func;
		}
	}
	#endregion
}
