using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;


namespace Althea.NativeTypes
{
	#region converter class
	internal static class ConstConvert<T, U> where T : unmanaged where U : unmanaged
	{
		private static Converter<T1, T2>? InternalConvert<T1, T2>() where T1 : notnull where T2 : notnull, new()
		{
			if (!typeof(T1).IsPrimitive || !typeof(T2).IsPrimitive)
				return null;
			DynamicMethod method = new(nameof(InternalConvert), typeof(T2), new[] { typeof(T1) });
			var IL = method.GetILGenerator();
			IL.Emit(OpCodes.Ldarg_0);
			switch (Type.GetTypeCode(typeof(T2)))
			{
				case TypeCode.SByte:
					IL.Emit(OpCodes.Conv_I1);
					break;
				case TypeCode.Byte:
					IL.Emit(OpCodes.Conv_U1);
					break;
				case TypeCode.Int16:
					IL.Emit(OpCodes.Conv_I2);
					break;
				case TypeCode.Char:
				case TypeCode.UInt16:
					IL.Emit(OpCodes.Conv_U2);
					break;
				case TypeCode.Int32:
					IL.Emit(OpCodes.Conv_I4);
					break;
				case TypeCode.UInt32:
					IL.Emit(OpCodes.Conv_U4);
					break;
				case TypeCode.Int64:
					IL.Emit(OpCodes.Conv_I8);
					break;
				case TypeCode.UInt64:
					IL.Emit(OpCodes.Conv_U8);
					break;
				case TypeCode.Single:
					IL.Emit(OpCodes.Conv_R4);
					break;
				case TypeCode.Double:
					IL.Emit(OpCodes.Conv_R8);
					break;
				default:
					return null;
			}
			IL.Emit(OpCodes.Ret);
			return method.CreateDelegate<Converter<T1, T2>>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Converter<T, U> GetReflectionConverter()
		{
			if (typeof(T) == typeof(U))
				return static v => v is U vv ? vv : new();
			static bool predicator(MethodInfo m) => (m.Name == "op_Explicit" || m.Name == "op_Implicit") &&
														m.ReturnType == typeof(U) && m.GetParameters().Length == 1 &&
														m.GetParameters()[0].ParameterType == typeof(T);

			Type t1 = typeof(T), t2 = typeof(U);
			var convert = t1.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
							.Where(predicator)
							.FirstOrDefault();
			convert ??= t2.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
							.Where(predicator)
							.FirstOrDefault();
			if (convert is null)
				return InternalConvert<T, U>() ?? (static v => (U)(dynamic)v);
			else
				return convert.CreateDelegate<Converter<T, U>>();
		}

		internal static readonly Converter<T, U> ConvertDelegate;

		static ConstConvert()
		{
			ConvertDelegate = GetReflectionConverter();
		}
	}
	#endregion

	/// <summary>
	/// Generic type constants
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public static class Const<T> where T : unmanaged
	{
		#region constants with simple initialization
		/// <summary>
		/// Get the <see cref="NativeTypes.DataType"/> of type <typeparamref name="T"/>
		/// </summary>
		public static readonly DataType DataType = DataTypeExtension.ToDataType<T>();

		/// <summary>
		/// Get the <see cref="DataTypeClassification"/> of type <typeparamref name="T"/>
		/// </summary>
		public static readonly DataTypeClassification DataTypeClass = NativeTypeExtension.GetClassification<T>();

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether type <typeparamref name="T"/> is an integral type or not
		/// </summary>
		public static readonly bool IsIntegralType = NativeTypeExtension.IsIntegralType<T>();

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether type <typeparamref name="T"/> is a complex type or not
		/// </summary>
		public static readonly bool IsComplex = NativeTypeExtension.IsComplex<T>();

		/// <summary>
		/// Get the size of <typeparamref name="T"/> in memory in bytes
		/// </summary>
		public static readonly unsafe int SizeT = sizeof(T);

		/// <summary>
		/// Generic type constant
		/// </summary>
		public static readonly T
			Zero = default,
			One = 1.0.GenericConvert<double, T>(),
			Two = 2.0.GenericConvert<double, T>(),
			MinusOne = (-1.0).GenericConvert<double, T>(),
			Half = 0.5.GenericConvert<double, T>(),
			MinusHalf = (-0.5).GenericConvert<double, T>(),
			E = Math.E.GenericConvert<double, T>(),
			Pi = Math.PI.GenericConvert<double, T>();

		/// <summary>
		/// Get the machine precision of <typeparamref name="T"/>
		/// </summary>
		public static readonly double MachinePrecision = NativeTypeExtension.GetMachinePrecision<T>();
		#endregion

		#region delegates
		internal static readonly Func<T, T> ReciprocalDelegate, NegateDelegate, SqrtDelegate, ConjugateDelegate;
		internal static readonly Func<T, double> AbsoluteDelegate;
		internal static readonly Converter<T, double> AbsoluteDelegate_;

		internal static readonly Func<T, T, T> AddDelegate;
		internal static readonly Func<T, T, T> SubtractDelegate;
		internal static readonly Func<T, T, T> MultiplyDelegate;
		internal static readonly Func<T, T, T> DivideDelegate;
		internal static readonly Func<T, double, T> PowerDelegate1;
		internal static readonly Func<T, T, T> PowerDelegate2;

		internal static readonly Converter<T, double> ToDoubleDelegate;
		internal static readonly Converter<double, T> FromDoubleDelegate;
		internal static readonly Converter<T, long> ToLongDelegate;
		internal static readonly Converter<long, T> FromLongDelegate;

		#region private reflection
		private enum BinaryOp
		{
			Addition,
			Subtraction,
			Multiply,
			Division
		}

		private const MethodAttributes ATTR = MethodAttributes.Public | MethodAttributes.Static;
		private const CallingConventions CALL = CallingConventions.Standard;
		private static readonly Module THIS = typeof(Const<T>).Module;

		private static Func<T, T, T> GetBinary(BinaryOp op)
		{
			if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new(op.ToString(), ATTR, CALL, typeof(T), new[] { typeof(T), typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldarg_1);
				switch (op)
				{
					case Const<T>.BinaryOp.Addition:
						IL.Emit(OpCodes.Add);
						break;
					case Const<T>.BinaryOp.Subtraction:
						IL.Emit(OpCodes.Sub);
						break;
					case Const<T>.BinaryOp.Multiply:
						IL.Emit(OpCodes.Mul);
						break;
					case Const<T>.BinaryOp.Division:
						IL.Emit(OpCodes.Div);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(op));
				}
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T, T>>();
			}
			else
			{
				bool predicator(MethodInfo m) => m.Name == $"op_{op}" && m.ReturnType == typeof(T) &&
												 m.GetParameters().Length == 2 &&
												 m.GetParameters()[0].ParameterType == typeof(T) &&
												 m.GetParameters()[1].ParameterType == typeof(T);

				var func = typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(predicator).FirstOrDefault();
				if (func is null)
				{
					return op switch
					{
						Const<T>.BinaryOp.Addition => static (a, b) => (dynamic)a + b,
						Const<T>.BinaryOp.Subtraction => static (a, b) => (dynamic)a - b,
						Const<T>.BinaryOp.Multiply => static (a, b) => (dynamic)a * b,
						Const<T>.BinaryOp.Division => static (a, b) => (dynamic)a / b,
						_ => throw new ArgumentOutOfRangeException(nameof(op)),
					};
				}
				return func.CreateDelegate<Func<T, T, T>>();
			}
		}

		private static Func<T, T, T> GetPower2()
		{
			if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new("Power", ATTR, CALL, typeof(T), new[] { typeof(T), typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				if (typeof(T) == typeof(double))
				{
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Ldarg_1);
				}
				else
				{
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Conv_R8);
					IL.Emit(OpCodes.Ldarg_1);
					IL.Emit(OpCodes.Conv_R8);
				}
				IL.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Pow), new[] { typeof(double), typeof(double) }) ?? throw new NotSupportedException());
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T, T>>();
			}
			else
			{
				static bool predicatorNonStatic(MethodInfo m) => m.Name == "Pow" || m.Name == "Power" && m.ReturnType == typeof(T) &&
																 m.GetParameters().Length == 1 &&
																 m.GetParameters()[0].ParameterType == typeof(T);
				static bool predicatorStatic(MethodInfo m) => m.Name == $"Pow" || m.Name == "Power" && m.ReturnType == typeof(T) &&
															  m.GetParameters().Length == 2 &&
															  m.GetParameters()[0].ParameterType == typeof(T) &&
															  m.GetParameters()[1].ParameterType == typeof(T);

				var func = typeof(T).GetMethods(BindingFlags.Public).Where(predicatorNonStatic).FirstOrDefault();
				func ??= typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(predicatorStatic).FirstOrDefault();
				if (func is null)
					return static (a, p) => ((dynamic)a).Pow(p);
				if (func.IsStatic)
					return func.CreateDelegate<Func<T, T, T>>();
				// object call
				DynamicMethod method = new("Power", ATTR, CALL, typeof(T), new[] { typeof(T), typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0); // the object to call
				IL.Emit(OpCodes.Ldarg_1); // parameter
				IL.Emit(OpCodes.Callvirt, func);
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T, T>>();
			}
		}

		private static Func<T, double, T> GetPower1()
		{
			if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new("Power", ATTR, CALL, typeof(T), new[] { typeof(T), typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				if (typeof(T) == typeof(double))
				{
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Ldarg_1);
				}
				else
				{
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Conv_R8);
					IL.Emit(OpCodes.Ldarg_1);
				}
				IL.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Pow), new[] { typeof(double), typeof(double) }) ?? throw new NotSupportedException());
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, double, T>>();
			}
			else
			{
				static bool predicatorNonStatic(MethodInfo m) => m.Name == "Pow" || m.Name == "Power" && m.ReturnType == typeof(T) &&
																 m.GetParameters().Length == 1 &&
																 m.GetParameters()[0].ParameterType == typeof(double);
				static bool predicatorStatic(MethodInfo m) => m.Name == $"Pow" || m.Name == "Power" && m.ReturnType == typeof(T) &&
															  m.GetParameters().Length == 2 &&
															  m.GetParameters()[0].ParameterType == typeof(T) &&
															  m.GetParameters()[1].ParameterType == typeof(double);

				var func = typeof(T).GetMethods(BindingFlags.Public).Where(predicatorNonStatic).FirstOrDefault();
				func ??= typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(predicatorStatic).FirstOrDefault();
				if (func is null)
					return static (a, p) => ((dynamic)a).Pow(p);
				if (func.IsStatic)
					return func.CreateDelegate<Func<T, double, T>>();
				// object call
				DynamicMethod method = new("Power", ATTR, CALL, typeof(T), new[] { typeof(T), typeof(double) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0); // the object to call
				IL.Emit(OpCodes.Ldarg_1); // parameter
				IL.Emit(OpCodes.Callvirt, func);
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, double, T>>();
			}
		}

		private static Func<T, T> GetNegate()
		{
			if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new("Negation", ATTR, CALL, typeof(T), new[] { typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Neg);
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T>>();
			}
			else
			{
				static bool predicator(MethodInfo m) => m.Name == $"op_UnaryNegation" && m.ReturnType == typeof(T) &&
														 m.GetParameters().Length == 1 &&
														 m.GetParameters()[0].ParameterType == typeof(T);

				var func = typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(predicator).FirstOrDefault();
				if (func is null)
				{
					return static v => -(dynamic)v;
				}
				return func.CreateDelegate<Func<T, T>>();
			}
		}

		private static Func<T, T> GetSqrt()
		{
			if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new("Sqrt", ATTR, CALL, typeof(T), new[] { typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0);
				if (typeof(T) != typeof(double))
					IL.Emit(OpCodes.Conv_R8);
				IL.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Sqrt), new[] { typeof(double) }) ?? throw new NotSupportedException());
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T>>();
			}
			else
			{
				static bool predicatorNonStatic(MethodInfo m) => m.Name == "Sqrt" || m.Name == "SquareRoot" && m.ReturnType == typeof(T) &&
																 m.GetParameters().Length == 0;
				static bool predicatorStatic(MethodInfo m) => m.Name == $"Sqrt" || m.Name == "SquareRoot" && m.ReturnType == typeof(T) &&
															  m.GetParameters().Length == 1 &&
															  m.GetParameters()[0].ParameterType == typeof(T);

				var func = typeof(T).GetMethods(BindingFlags.Public).Where(predicatorNonStatic).FirstOrDefault();
				func ??= typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(predicatorStatic).FirstOrDefault();
				if (func is null)
					return static v => ((dynamic)v).Sqrt();
				if (func.IsStatic)
					return func.CreateDelegate<Func<T, T>>();
				// object call
				DynamicMethod method = new("Sqrt", ATTR, CALL, typeof(T), new[] { typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0); // the object to call
				IL.Emit(OpCodes.Callvirt, func);
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T>>();
			}
		}

		private static Func<T, T> GetConjugate()
		{
			if (typeof(T).IsPrimitive || !IsComplex)
			{
				return static v => v;
			}
			else
			{
				static bool predicatorNonStatic(MethodInfo m) => m.Name == "Conj" || m.Name == "Conjugate" && m.ReturnType == typeof(T) &&
																 m.GetParameters().Length == 0;
				static bool predicatorStatic(MethodInfo m) => m.Name == $"Conj" || m.Name == "Conjugate" && m.ReturnType == typeof(T) &&
															  m.GetParameters().Length == 1 &&
															  m.GetParameters()[0].ParameterType == typeof(T);

				var func = typeof(T).GetMethods(BindingFlags.Public).Where(predicatorNonStatic).FirstOrDefault();
				func ??= typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(predicatorStatic).FirstOrDefault();
				if (func is null)
					return static v => ((dynamic)v).Sqrt();
				if (func.IsStatic)
					return func.CreateDelegate<Func<T, T>>();
				// object call
				DynamicMethod method = new("Conjugate", ATTR, CALL, typeof(T), new[] { typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0); // the object to call
				IL.Emit(OpCodes.Callvirt, func);
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T>>();
			}
		}

		private static void GetAbsolute(out Func<T, double> result1, out Converter<T, double> result2)
		{
			if (DataTypeClass == DataTypeClassification.UnsignedInteger)
			{
				result1 = static v => ConstConvert<T, double>.ConvertDelegate.Invoke(v);
				result2 = ConstConvert<T, double>.ConvertDelegate;
				return;
			}
			else if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new("Absolute", ATTR, CALL, typeof(T), new[] { typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(T) }) ?? throw new NotSupportedException());
				IL.Emit(OpCodes.Conv_R8);
				IL.Emit(OpCodes.Ret);
				result1 = method.CreateDelegate<Func<T, double>>();
				result2 = method.CreateDelegate<Converter<T, double>>();
			}
			else
			{
				static bool predicatorNonStatic(MethodInfo m) => m.Name == "Abs" || m.Name == "Absolute" && m.ReturnType == typeof(double) &&
																 m.GetParameters().Length == 0;
				static bool predicatorStatic(MethodInfo m) => m.Name == $"Abs" || m.Name == "Absolute" && m.ReturnType == typeof(double) &&
															  m.GetParameters().Length == 1 &&
															  m.GetParameters()[0].ParameterType == typeof(T);

				var func = typeof(T).GetMethods(BindingFlags.Public).Where(predicatorNonStatic).FirstOrDefault();
				func ??= typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(predicatorStatic).FirstOrDefault();
				if (func is null)
				{
					result1 = static v => ((dynamic)v).Sqrt();
					result2 = static v => ((dynamic)v).Sqrt();
					return;
				}
				if (func.IsStatic)
				{
					result1 = func.CreateDelegate<Func<T, double>>();
					result2 = func.CreateDelegate<Converter<T, double>>();
				}
				// object call
				DynamicMethod method = new("Absolute", ATTR, CALL, typeof(double), new[] { typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0); // the object to call
				IL.Emit(OpCodes.Callvirt, func);
				IL.Emit(OpCodes.Ret);
				result1 = method.CreateDelegate<Func<T, double>>();
				result2 = method.CreateDelegate<Converter<T, double>>();
			}
		}
		#endregion

		static Const()
		{
			if (!NativeTypeExtension.IsSupported<T>())
				throw new InvalidOperationException();
			// conversions
			ToDoubleDelegate = ConstConvert<T, double>.ConvertDelegate;
			FromDoubleDelegate = ConstConvert<double, T>.ConvertDelegate;
			ToLongDelegate = ConstConvert<T, long>.ConvertDelegate;
			FromLongDelegate = ConstConvert<long, T>.ConvertDelegate;
			// binary arithmetics
			AddDelegate = GetBinary(BinaryOp.Addition);
			SubtractDelegate = GetBinary(BinaryOp.Subtraction);
			MultiplyDelegate = GetBinary(BinaryOp.Multiply);
			DivideDelegate = GetBinary(BinaryOp.Division);
			PowerDelegate1 = GetPower1();
			PowerDelegate2 = GetPower2();
			// unary arithmetics
			ReciprocalDelegate = static v => DivideDelegate.Invoke(One, v);
			NegateDelegate = GetNegate();
			SqrtDelegate = GetSqrt();
			ConjugateDelegate = GetConjugate();
			GetAbsolute(out AbsoluteDelegate, out AbsoluteDelegate_);
		}
		#endregion
	}


	/// <summary>
	/// The static class for extension methods utilizing <see cref="Const{T}"/>
	/// </summary>
	public static class ConstExtension
	{
		#region generic type arithmetics
		/// <summary>
		/// Generic type number reciprocal.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input number</param>
		/// <returns>The reciprocal of the <paramref name="a"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericReciprocal<T>(this T a) where T : unmanaged
		{
			if (a.IsZero())
				throw new DivideByZeroException();
			if (a.IsOne())
				return a;
			return Const<T>.ReciprocalDelegate.Invoke(a);
		}

		/// <summary>
		/// Generic type number negation.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input number</param>
		/// <returns>The negation of the <paramref name="a"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericNegate<T>(this T a) where T : unmanaged
		{
			if (a.IsZero())
				return default;
			return Const<T>.NegateDelegate.Invoke(a);
		}

		/// <summary>
		/// Generic type numbers addition.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input left number</param>
		/// <param name="b">The input right number</param>
		/// <returns>The sum of <paramref name="a"/> and <paramref name="b"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericAdd<T>(this T a, T b) where T : unmanaged
		{
			if (a.IsZero())
				return b;
			if (b.IsZero())
				return a;
			return Const<T>.AddDelegate.Invoke(a, b);
		}

		/// <summary>
		/// Generic type numbers subtraction.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input left number</param>
		/// <param name="b">The input right number</param>
		/// <returns>The subtraction of <paramref name="a"/> and <paramref name="b"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericMinus<T>(this T a, T b) where T : unmanaged
		{
			if (a.IsZero())
				return b;
			if (b.IsZero())
				return a;
			return Const<T>.SubtractDelegate.Invoke(a, b);
		}

		/// <summary>
		/// Generic type numbers multiplication.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input left number</param>
		/// <param name="b">The input right number</param>
		/// <returns>The product of <paramref name="a"/> and <paramref name="b"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericMultiply<T>(this T a, T b) where T : unmanaged
		{
			if (a.IsZero() || b.IsZero())
				return default;
			if (a.IsOne())
				return b;
			if (b.IsOne())
				return a;
			return Const<T>.MultiplyDelegate.Invoke(a, b);
		}

		/// <summary>
		/// Generic type numbers division.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input left number</param>
		/// <param name="b">The input right number</param>
		/// <returns>The division of <paramref name="a"/> and <paramref name="b"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericDivide<T>(this T a, T b) where T : unmanaged
		{
			if (a.IsZero() || b.IsZero())
				return default;
			if (a.IsOne())
				return b;
			if (b.IsOne())
				return a;
			return Const<T>.DivideDelegate.Invoke(a, b);
		}

		/// <summary>
		/// Generic type number square root.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input number</param>
		/// <returns>The square root of <paramref name="a"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericSqrt<T>(this T a) where T : unmanaged
		{
			if (a.IsZero() || a.IsOne())
				return a;
			return Const<T>.SqrtDelegate.Invoke(a);
		}

		/// <summary>
		/// Generic type number conjugation.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input number</param>
		/// <returns>The complex conjugate of <paramref name="a"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericConjugate<T>(this T a) where T : unmanaged
		{
			if (a.IsZero() || a.IsOne())
				return a;
			return Const<T>.ConjugateDelegate.Invoke(a);
		}

		/// <summary>
		/// Generic type number power.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input number</param>
		/// <param name="power">The power as a <see cref="double"/></param>
		/// <returns>The complex conjugate of <paramref name="a"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericPower<T>(this T a, double power) where T : unmanaged
		{
			if (power == 0)
				return Const<T>.One;
			if (power == 1 || a.IsZero() || a.IsOne())
				return a;
			return Const<T>.PowerDelegate1.Invoke(a, power);
		}


		/// <summary>
		/// Generic type number power.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input number</param>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		/// <returns>The complex conjugate of <paramref name="a"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GenericPower<T>(this T a, T power) where T : unmanaged
		{
			if (power.IsZero())
				return Const<T>.One;
			if (power.IsZero() || a.IsZero() || a.IsOne())
				return a;
			return Const<T>.PowerDelegate2.Invoke(a, power);
		}

		/// <summary>
		/// Generic type number absolute value.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The input number</param>
		/// <returns>The absolute value of <paramref name="a"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double GenericAbsolute<T>(this T a) where T : unmanaged
		{
			if (a.IsZero())
				return 0;
			if (a.IsOne())
				return 1;
			return Const<T>.AbsoluteDelegate.Invoke(a);
		}
		#endregion

		#region generic type conversions
		/// <summary>
		/// Generically convert <paramref name="obj"/> of type <typeparamref name="T1"/> to type <typeparamref name="T2"/> by finding possible explicit or implicit conversion operators or by utilizing default primitive type converters.
		/// </summary>
		/// <typeparam name="T1">The input type</typeparam>
		/// <typeparam name="T2">The output type</typeparam>
		/// <param name="obj">The input object to be converted</param>
		/// <returns>The <typeparamref name="T2"/> object converted by explicit or implicit operators</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T2 GenericConvert<T1, T2>(this T1 obj) where T1 : unmanaged where T2 : unmanaged
		{
			if (obj is T2 a)
				return a;
			else
				return ConstConvert<T1, T2>.ConvertDelegate.Invoke(obj);
		}

		/// <summary>
		/// Generic numeric value converter from any type to <see cref="double"/>.
		/// </summary>
		/// <typeparam name="T">The convert source type</typeparam>
		/// <param name="a">The number to convert</param>
		/// <returns>The converted number as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double ToDouble<T>(this T a) where T : unmanaged => Const<T>.ToDoubleDelegate.Invoke(a);

		/// <summary>
		/// Generic numeric value converter from <see cref="double"/> to any type.
		/// </summary>
		/// <typeparam name="T">The convert target type</typeparam>
		/// <param name="a">The number to convert</param>
		/// <returns>The converted number as <typeparamref name="T"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T FromDouble<T>(this double a) where T : unmanaged => Const<T>.FromDoubleDelegate.Invoke(a);

		/// <summary>
		/// Generic numeric value converter from any integral type to <see cref="long"/>.
		/// </summary>
		/// <typeparam name="T">The convert source type, must be an integral type</typeparam>
		/// <param name="a">The number to convert</param>
		/// <returns>The converted number as a <see cref="long"/></returns>
		/// <exception cref="InvalidCastException">If <typeparamref name="T"/> is not an integral type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ToLong<T>(this T a) where T : unmanaged
		{
			if (!Const<T>.IsIntegralType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
			return Const<T>.ToLongDelegate.Invoke(a);
		}

		/// <summary>
		/// Generic numeric value converter from <see cref="long"/> to any integral type.
		/// </summary>
		/// <typeparam name="T">The convert target type, must be an integral type</typeparam>
		/// <param name="a">The number to convert</param>
		/// <returns>The converted number as <typeparamref name="T"/></returns>
		/// <exception cref="InvalidCastException">If <typeparamref name="T"/> is not an integral type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T FromLong<T>(this long a) where T : unmanaged
		{
			if (!Const<T>.IsIntegralType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
			return Const<T>.FromLongDelegate.Invoke(a);
		}
		#endregion
	}
}
