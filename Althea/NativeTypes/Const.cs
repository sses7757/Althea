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
		private static void ILConvert(ILGenerator IL, Type from, Type target)
		{
			if (from == target)
			{
				// do nothing
			}
			else if (from.IsPrimitive && target.IsPrimitive)
			{
				switch (Type.GetTypeCode(target))
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
						return;
				}
			}
			else
			{
				var field = typeof(ConstConvert<,>).MakeGenericType(from, target).GetField(nameof(ConstConvert<T, T>.ConvertDelegate), BindingFlags.Static | BindingFlags.NonPublic);
				if (field is null)
					throw new FieldAccessException();
				IL.DeclareLocal(from);
				IL.Emit(OpCodes.Stloc_0); // pop the result to a local variable
				IL.Emit(OpCodes.Ldsfld, field); // load the converter to be invoked
				IL.Emit(OpCodes.Ldloc_0); // load the result from local variable
				var invoke = typeof(Converter<,>).MakeGenericType(from, target).GetMethod(nameof(Converter<T, T>.Invoke));
				if (invoke is null)
					throw new FieldAccessException();
				IL.Emit(OpCodes.Callvirt, invoke); // call Delegate.Invoke to convert to target type
			}
		}

		private static (Converter<T, U>, Func<T, U>)? InternalConvert()
		{
			if (!typeof(T).IsPrimitive || !typeof(U).IsPrimitive)
				return null;
			DynamicMethod method = new(nameof(InternalConvert), typeof(U), new[] { typeof(T) });
			var IL = method.GetILGenerator();
			IL.Emit(OpCodes.Ldarg_0);
			ILConvert(IL, typeof(T), typeof(U));
			IL.Emit(OpCodes.Ret);
			return (method.CreateDelegate<Converter<T, U>>(), method.CreateDelegate<Func<T, U>>());
		}

		private static unsafe U DirectConvert(T v) => *(U*)&v;

		private static unsafe U DirectConvertComp2Comp<TT, UU>(T v) where TT : unmanaged where UU : unmanaged
		{
			U result = default;
			*(UU*)&result = ConstConvert<TT, UU>.ConvertDelegate.Invoke(*(TT*)&v);
			*(1 + (UU*)&result) = ConstConvert<TT, UU>.ConvertDelegate.Invoke(*(1 + (TT*)&v));
			return result;
		}

		private static unsafe U DirectConvertReal2Comp(T v)
		{
			U result = default;
			*(T*)&result = v;
			return result;
		}
		private static unsafe U DirectConvertReal2Comp2<UU>(T v) where UU : unmanaged
		{
			U result = default;
			*(UU*)&result = ConstConvert<T, UU>.ConvertDelegate.Invoke(v);
			return result;
		}

		private static void GetReflectionConverter(out Converter<T, U> converter, out Func<T, U> func)
		{
			if (typeof(T) == typeof(U))
			{
				converter = DirectConvert;
				func = DirectConvert;
				return;
			}
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
			{
				var res = InternalConvert();
				if (res.HasValue)
					(converter, func) = res.Value;
				else
					GetReflectionConverterComplex(out converter, out func);
			}
			else
			{
				converter = convert.CreateDelegate<Converter<T, U>>();
				func = convert.CreateDelegate<Func<T, U>>();
			}
		}

		private static void GetReflectionConverterComplex(out Converter<T, U> converter, out Func<T, U> func)
		{
			// T != U
			bool isComp1 = NativeTypeExtension.IsComplex<T>(), isComp2 = NativeTypeExtension.IsComplex<U>();
			if (isComp1 && isComp2)
			{	// complex to complex
				Type c1 = typeof(T), c2 = typeof(U), r1 = typeof(T).GenericTypeArguments[0], r2 = typeof(U).GenericTypeArguments[0];
				if (r1 == r2)
				{
					converter = DirectConvert;
					func = DirectConvert;
					return;
				}
				// else
				var method = typeof(ConstConvert<T, U>).GetMethod(nameof(DirectConvertComp2Comp), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(r1, r2);
				if (method is null)
					throw new MethodAccessException();
				converter = method.CreateDelegate<Converter<T, U>>();
				func = method.CreateDelegate<Func<T, U>>();
			}
			else if (isComp1 && !isComp2)
			{   // complex to real
				(converter, func) = GetComplexPart(realPart: true);
			}
			else if (!isComp1 && isComp2)
			{   // real to complex
				if (typeof(T) == typeof(U).GenericTypeArguments[0])
				{
					var method = typeof(ConstConvert<T, U>).GetMethod(nameof(DirectConvertReal2Comp), BindingFlags.NonPublic | BindingFlags.Static);
					if (method is null)
						throw new MethodAccessException();
					converter = method.CreateDelegate<Converter<T, U>>();
					func = method.CreateDelegate<Func<T, U>>();
				}
				else
				{
					var method = typeof(ConstConvert<T, U>).GetMethod(nameof(DirectConvertReal2Comp2), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(typeof(U).GenericTypeArguments[0]);
					if (method is null)
						throw new MethodAccessException();
					converter = method.CreateDelegate<Converter<T, U>>();
					func = method.CreateDelegate<Func<T, U>>();
				}
			}
			else
			{	// real to real, can only try dynamic
				converter = static v => (U)(dynamic)v;
				func = static v => (U)(dynamic)v;
			}
		}

		private static U GetDefault(T _) => default;

		private static (Converter<T, U>, Func<T, U>) GetComplexPart(bool realPart)
		{
			if (NativeTypeExtension.IsComplex<U>())
			{	// U is complex, do nothing
				return (GetDefault, GetDefault);
			}
			if (!NativeTypeExtension.IsComplex<T>())
			{	// T is not complex, directly return
				return realPart ? (ConvertDelegate, ConvertDelegate_) : (GetDefault, GetDefault);
			}
			else
			{
				Type realType = typeof(T).GenericTypeArguments[0];
				string name1 = realPart ? "Real" : "Imaginary", name2 = realPart ? "Real" : @"Imag", name3 = realPart ? "Re" : "Im";
				bool predicatorMethod(MethodInfo m) => (m.Name == name1 || m.Name == name2 || m.Name == name3) &&
														(m.ReturnType == realType || m.ReturnType == typeof(U)) &&
														m.GetParameters().Length == 0;
				bool predicatorProperty(PropertyInfo m) => (m.Name == name1 || m.Name == name2 || m.Name == name3) &&
															(m.PropertyType == realType || m.PropertyType == typeof(U)) &&
															m.CanRead;
				bool predicatorField(FieldInfo m) => (m.Name.Equals(name1, StringComparison.OrdinalIgnoreCase) ||
													  m.Name.Equals(name2, StringComparison.OrdinalIgnoreCase) ||
													  m.Name.Equals(name3, StringComparison.OrdinalIgnoreCase)) &&
													 (m.FieldType == realType || m.FieldType == typeof(U));

				DynamicMethod method = new("GetPart", typeof(U), new[] { typeof(T) });
				var IL = method.GetILGenerator();
				var prop = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(predicatorProperty).FirstOrDefault();
				if (prop is not null)
				{
					IL.Emit(OpCodes.Ldarg_0); // the object to call
					var propGet = prop.GetGetMethod();
					if (propGet is null)
						throw new FieldAccessException();
					IL.Emit(OpCodes.Call, propGet);
					ILConvert(IL, realType, typeof(U)); // convert the result to U type
					IL.Emit(OpCodes.Ret);
					return (method.CreateDelegate<Converter<T, U>>(), method.CreateDelegate<Func<T, U>>());
				}
				// else
				var field = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(predicatorField).FirstOrDefault();
				if (field is not null)
				{
					IL.Emit(OpCodes.Ldarg_0); // the object to call
					IL.Emit(OpCodes.Ldfld, field);
					ILConvert(IL, realType, typeof(U)); // convert the result to U type
					IL.Emit(OpCodes.Ret);
					return (method.CreateDelegate<Converter<T, U>>(), method.CreateDelegate<Func<T, U>>());
				}
				// else
				var func = typeof(T).GetMethods(BindingFlags.Public).Where(predicatorMethod).FirstOrDefault();
				if (func is not null)
				{
					IL.Emit(OpCodes.Ldarg_0); // the object to call
					IL.Emit(OpCodes.Call, func);
					ILConvert(IL, realType, typeof(U)); // convert the result to U type
					IL.Emit(OpCodes.Ret);
					return (method.CreateDelegate<Converter<T, U>>(), method.CreateDelegate<Func<T, U>>());
				}
				// try direct address get
				IL.Emit(OpCodes.Ldarga_S, 0); // load the address of input value of type T
				IL.Emit(OpCodes.Conv_U); // convert the address to 'size_t' of C
				if (!realPart)
				{	// add offset to get the address of complex part
					IL.Emit(OpCodes.Sizeof, realType);
					IL.Emit(OpCodes.Add);
				}
				IL.Emit(OpCodes.Ldobj, realType); // load the specified part
				ILConvert(IL, realType, typeof(U)); // convert to U
				IL.Emit(OpCodes.Ret);
				return (method.CreateDelegate<Converter<T, U>>(), method.CreateDelegate<Func<T, U>>());
			}
		}


		internal static readonly Converter<T, U> ConvertDelegate;

		internal static readonly Func<T, U> ConvertDelegate_;

		internal static readonly Converter<T, U> GetRealPartDelegate, GetImagPartDelegate;

		static ConstConvert()
		{
			GetReflectionConverter(out ConvertDelegate, out ConvertDelegate_);
			GetRealPartDelegate = GetComplexPart(realPart: true).Item1;
			GetImagPartDelegate = GetComplexPart(realPart: false).Item1;
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
		///  Get a <see cref="bool"/> indicating whether type <typeparamref name="T"/> is a primitive type of .NET or a pre-defined type in <see cref="Althea"/>.
		/// </summary>
		public static readonly bool IsPreDefined = NativeTypeExtension.IsSupported<T>() &&
													((typeof(T).IsPrimitive && typeof(T) != typeof(bool) && typeof(T) != typeof(char)) ||
													 typeof(T) == typeof(ComplexDouble) ||
														(NativeTypeExtension.IsComplex<T>() &&
														 typeof(T) == typeof(Complex<>).MakeGenericType(typeof(T).GenericTypeArguments[0])));

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
			One = ConstConvert<double, T>.ConvertDelegate.Invoke(1.0),
			Two = ConstConvert<double, T>.ConvertDelegate.Invoke(2.0),
			MinusOne = ConstConvert<double, T>.ConvertDelegate.Invoke(-1.0),
			Half = ConstConvert<double, T>.ConvertDelegate.Invoke(0.5),
			MinusHalf = ConstConvert<double, T>.ConvertDelegate.Invoke(-0.5);

		/// <summary>
		/// Get the machine precision of <typeparamref name="T"/>
		/// </summary>
		public static readonly double MachinePrecision = NativeTypeExtension.GetMachinePrecision<T>();

		/// <summary>
		/// Get the value of <see cref="MachinePrecision"/>^(1/2)
		/// </summary>
		public static readonly double MachinePrecisionHalf = Math.Sqrt(MachinePrecision);
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

		internal static readonly Func<T, T, bool>? EqualityDelegate, InequalityDelegate, GreaterThanDelegate, LessThanDelegate, GreaterThanOrEqualDelegate, LessThanOrEqualDelegate;

		internal static readonly Converter<T, double> ToDoubleDelegate;
		internal static readonly Converter<double, T> FromDoubleDelegate;
		internal static readonly Converter<T, long> ToLongDelegate;
		internal static readonly Converter<long, T> FromLongDelegate;

		internal static readonly Converter<T, double> RealPartDelegate;
		internal static readonly Converter<T, double> ImagPartDelegate;

		#region private reflection
		private enum BinaryOp
		{
			Addition,
			Subtraction,
			Multiply,
			Division,

			Equality,
			Inequality,
			GreaterThan,
			LessThan,
			GreaterThanOrEqual,
			LessThanOrEqual,
		}

		private const MethodAttributes ATTR = MethodAttributes.Public | MethodAttributes.Static;
		private const CallingConventions CALL = CallingConventions.Standard;
		private static readonly Module THIS = typeof(Const<T>).Module;

		private static Func<T, T, T> GetBinarySelf(BinaryOp op)
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

		private static Func<T, T, bool>? GetBinaryBool(BinaryOp op)
		{
			if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new(op.ToString(), ATTR, CALL, typeof(bool), new[] { typeof(T), typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldarg_1);
				switch (op)
				{
					case Const<T>.BinaryOp.Equality:
						IL.Emit(OpCodes.Ceq);
						break;
					case Const<T>.BinaryOp.Inequality:
						IL.Emit(OpCodes.Ceq);
						IL.Emit(OpCodes.Ldc_I4_0);
						IL.Emit(OpCodes.Ceq);
						break;
					case Const<T>.BinaryOp.GreaterThan:
						IL.Emit(OpCodes.Cgt);
						break;
					case Const<T>.BinaryOp.LessThan:
						IL.Emit(OpCodes.Clt);
						break;
					case Const<T>.BinaryOp.GreaterThanOrEqual:
						IL.Emit(OpCodes.Clt_Un);
						IL.Emit(OpCodes.Ldc_I4_0);
						IL.Emit(OpCodes.Ceq);
						break;
					case Const<T>.BinaryOp.LessThanOrEqual:
						IL.Emit(OpCodes.Cgt_Un);
						IL.Emit(OpCodes.Ldc_I4_0);
						IL.Emit(OpCodes.Ceq);
						break;
					default:
						return null;
				}
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T, bool>>();
			}
			else
			{
				bool predicator(MethodInfo m) => m.Name == $"op_{op}" && m.ReturnType == typeof(bool) &&
												 m.GetParameters().Length == 2 &&
												 m.GetParameters()[0].ParameterType == typeof(T) &&
												 m.GetParameters()[1].ParameterType == typeof(T);

				var func = typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(predicator).FirstOrDefault();
				return func?.CreateDelegate<Func<T, T, bool>>();
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
				static bool predicatorStatic(MethodInfo m) => m.Name == "Pow" || m.Name == "Power" && m.ReturnType == typeof(T) &&
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
				IL.Emit(OpCodes.Call, func);
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T, T>>();
			}
		}

		private static Func<T, double, T> GetPower1()
		{
			if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new("Power", ATTR, CALL, typeof(T), new[] { typeof(T), typeof(double) }, THIS, true);
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
				IL.Emit(OpCodes.Call, func);
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
				static bool predicatorNonStatic(MethodInfo m) => (m.Name == "Sqrt" || m.Name == "SquareRoot") && m.ReturnType == typeof(T) &&
																 m.GetParameters().Length == 0;
				static bool predicatorStatic(MethodInfo m) => (m.Name == $"Sqrt" || m.Name == "SquareRoot") && m.ReturnType == typeof(T) &&
																m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(T);

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
				IL.Emit(OpCodes.Call, func);
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
				static bool predicatorNonStatic(MethodInfo m) => (m.Name == "Conj" || m.Name == "Conjugate") && m.ReturnType == typeof(T) &&
																 m.GetParameters().Length == 0;
				static bool predicatorStatic(MethodInfo m) => (m.Name == $"Conj" || m.Name == "Conjugate") && m.ReturnType == typeof(T) &&
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
				IL.Emit(OpCodes.Call, func);
				IL.Emit(OpCodes.Ret);
				return method.CreateDelegate<Func<T, T>>();
			}
		}

		private static void ILToDouble(ILGenerator IL, Type realType)
		{
			if (realType == typeof(double))
			{
				// do nothing
			}
			else if (realType.IsPrimitive)
			{
				IL.Emit(OpCodes.Conv_R8);
			}
			else
			{
				var field = typeof(ConstConvert<,>).MakeGenericType(realType, typeof(double)).GetField(nameof(ConstConvert<T, T>.ConvertDelegate), BindingFlags.Static | BindingFlags.NonPublic);
				if (field is null)
					throw new FieldAccessException();
				IL.DeclareLocal(realType);
				IL.Emit(OpCodes.Stloc_0); // pop the result to a local variable
				IL.Emit(OpCodes.Ldsfld, field); // load the converter to be invoked
				IL.Emit(OpCodes.Ldloc_0); // load the result from local variable
				var invoke = typeof(Converter<,>).MakeGenericType(realType, typeof(double)).GetMethod(nameof(Converter<T, T>.Invoke));
				if (invoke is null)
					throw new FieldAccessException();
				IL.Emit(OpCodes.Callvirt, invoke); // call Delegate.Invoke to convert to double
			}
		}

		private static void GetAbsolute(out Func<T, double> result1, out Converter<T, double> result2)
		{
			if (DataTypeClass == DataTypeClassification.UnsignedInteger)
			{
				result1 = ConstConvert<T, double>.ConvertDelegate_;
				result2 = ConstConvert<T, double>.ConvertDelegate;
				return;
			}
			else if (typeof(T).IsPrimitive)
			{
				DynamicMethod method = new("Absolute", ATTR, CALL, returnType: typeof(double), new[] { typeof(T) }, THIS, true);
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
				Type realType = IsComplex ? typeof(T).GenericTypeArguments[0] : typeof(T);
				bool predicatorNonStatic(MethodInfo m) => (m.Name == "Abs" || m.Name == "Absolute") &&
														  (m.ReturnType == typeof(double) || m.ReturnType == realType) &&
														   m.GetParameters().Length == 0;
				bool predicatorStatic(MethodInfo m) => (m.Name == "Abs" || m.Name == "Absolute") &&
													   (m.ReturnType == typeof(double) || m.ReturnType == realType) &&
														m.GetParameters().Length == 1 &&
														m.GetParameters()[0].ParameterType == typeof(T);
				bool predicatorProperty(PropertyInfo m) => (m.Name == "Abs" || m.Name == "Absolute") &&
														   (m.PropertyType == typeof(double) || m.PropertyType == realType) &&
															m.CanRead;

				DynamicMethod method = new("Absolute", ATTR, CALL, typeof(double), new[] { typeof(T) }, THIS, true);
				var IL = method.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0); // the object to call

				var func = typeof(T).GetMethods(BindingFlags.Public).Where(predicatorNonStatic).FirstOrDefault();
				func ??= typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(predicatorStatic).FirstOrDefault();
				if (func is null)
				{
					// try property
					var prop = typeof(T).GetProperties(BindingFlags.Instance).Where(predicatorProperty).FirstOrDefault();
					if (prop is null)
					{	// try dynamic
						result1 = static v => ((dynamic)v).Abs();
						result2 = static v => ((dynamic)v).Abs();
						return;
					}
					// property get
					var propGet = prop.GetGetMethod();
					if (propGet is null)
						throw new FieldAccessException();
					IL.Emit(OpCodes.Call, propGet);
					ILToDouble(IL, realType); // convert the result to double type
					IL.Emit(OpCodes.Ret);
					result1 = method.CreateDelegate<Func<T, double>>();
					result2 = method.CreateDelegate<Converter<T, double>>();
					return;
				}
				if (func.IsStatic)
				{
					result1 = func.CreateDelegate<Func<T, double>>();
					result2 = func.CreateDelegate<Converter<T, double>>();
				}
				// object call
				IL.Emit(OpCodes.Call, func);
				ILToDouble(IL, realType);
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
			AddDelegate = GetBinarySelf(BinaryOp.Addition);
			SubtractDelegate = GetBinarySelf(BinaryOp.Subtraction);
			MultiplyDelegate = GetBinarySelf(BinaryOp.Multiply);
			DivideDelegate = GetBinarySelf(BinaryOp.Division);
			PowerDelegate1 = GetPower1();
			PowerDelegate2 = GetPower2();
			// binary compare
			EqualityDelegate = GetBinaryBool(BinaryOp.Equality);
			InequalityDelegate = GetBinaryBool(BinaryOp.Inequality);
			GreaterThanDelegate = GetBinaryBool(BinaryOp.GreaterThan);
			LessThanDelegate = GetBinaryBool(BinaryOp.LessThan);
			GreaterThanOrEqualDelegate = GetBinaryBool(BinaryOp.GreaterThanOrEqual);
			LessThanOrEqualDelegate = GetBinaryBool(BinaryOp.LessThanOrEqual);
			// unary arithmetics
			ReciprocalDelegate = static v => DivideDelegate.Invoke(One, v);
			NegateDelegate = GetNegate();
			SqrtDelegate = GetSqrt();
			ConjugateDelegate = GetConjugate();
			GetAbsolute(out AbsoluteDelegate, out AbsoluteDelegate_);
			RealPartDelegate = ConstConvert<T, double>.GetRealPartDelegate;
			ImagPartDelegate = ConstConvert<T, double>.GetImagPartDelegate;
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

		/// <summary>
		/// Get the delegate that retrieves the absolute value as a <see cref="double"/> of the input argument of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <returns>The delegate that retrieves the absolute value as a <see cref="double"/> of the input argument of type <typeparamref name="T"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		public static Func<T, double> GetAbsoluteGetter<T>() where T : unmanaged
		{
			return Const<T>.AbsoluteDelegate;
		}

		/// <summary>
		/// Check whether the given generic number <paramref name="a"/> is larger than the generic number <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The left input number to be compared</param>
		/// <param name="b">The right input number to be compared</param>
		/// <returns>True if type <typeparamref name="T"/> has pre-defined larger-than operator and <paramref name="a"/> &gt; <paramref name="b"/>; false otherwise.</returns>
		/// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GenericLargerThan<T>(this T a, T b) where T : unmanaged
		{
			return Const<T>.GreaterThanDelegate?.Invoke(a, b) ?? false;
		}

		/// <summary>
		/// Check whether the given generic number <paramref name="a"/> is less than the generic number <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The left input number to be compared</param>
		/// <param name="b">The right input number to be compared</param>
		/// <returns>True if type <typeparamref name="T"/> has pre-defined larger-than operator and <paramref name="a"/> &lt; <paramref name="b"/>; false otherwise.</returns>
		/// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GenericLessThan<T>(this T a, T b) where T : unmanaged
		{
			return Const<T>.LessThanDelegate?.Invoke(a, b) ?? false;
		}

		/// <summary>
		/// Check whether the given generic number <paramref name="a"/> is larger than or equals to the generic number <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The left input number to be compared</param>
		/// <param name="b">The right input number to be compared</param>
		/// <returns>True if type <typeparamref name="T"/> has pre-defined larger-than operator and <paramref name="a"/> ≥ <paramref name="b"/>; false otherwise.</returns>
		/// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GenericLargerThanOrEqual<T>(this T a, T b) where T : unmanaged
		{
			return Const<T>.GreaterThanOrEqualDelegate?.Invoke(a, b) ?? false;
		}

		/// <summary>
		/// Check whether the given generic number <paramref name="a"/> is less than or equals to the generic number <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The left input number to be compared</param>
		/// <param name="b">The right input number to be compared</param>
		/// <returns>True if type <typeparamref name="T"/> has pre-defined larger-than operator and <paramref name="a"/> ≤ <paramref name="b"/>; false otherwise.</returns>
		/// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GenericLessThanOrEqual<T>(this T a, T b) where T : unmanaged
		{
			return Const<T>.LessThanOrEqualDelegate?.Invoke(a, b) ?? false;
		}

		/// <summary>
		/// The enum for generic number binary compare operations
		/// </summary>
		public enum CompareOperation
		{
			/// <summary>
			/// Equality comparison
			/// </summary>
			Equality,
			/// <summary>
			/// Inequality comparison
			/// </summary>
			Inequality,
			/// <summary>
			/// Greater than comparison
			/// </summary>
			GreaterThan,
			/// <summary>
			/// Less than comparison
			/// </summary>
			LessThan,
			/// <summary>
			/// Greater than or equals to comparison
			/// </summary>
			GreaterThanOrEqual,
			/// <summary>
			/// Less than or equals to comparison
			/// </summary>
			LessThanOrEqual,
		}

		/// <summary>
		/// Get the compare operation delegate of the given <paramref name="operation"/> as a <see cref="Func{T1, T2, TResult}"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="operation">The given <see cref="CompareOperation"/></param>
		/// <returns>The delegate used to compare the generic numbers of type <typeparamref name="T"/> if type <typeparamref name="T"/> has pre-defined <paramref name="operation"/>; otherwise, null.</returns>
		/// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not a supported data type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Func<T, T, bool>? GetComparer<T>(this CompareOperation operation) where T : unmanaged
		{
			return operation switch
			{
				CompareOperation.Equality => Const<T>.EqualityDelegate,
				CompareOperation.Inequality => Const<T>.InequalityDelegate,
				CompareOperation.GreaterThan => Const<T>.GreaterThanDelegate,
				CompareOperation.LessThan => Const<T>.LessThanDelegate,
				CompareOperation.GreaterThanOrEqual => Const<T>.GreaterThanOrEqualDelegate,
				CompareOperation.LessThanOrEqual => Const<T>.LessThanOrEqualDelegate,
				_ => null,
			};
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

		/// <summary>
		/// Get the real part (or itself if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="a">The number to get real part</param>
		/// <returns>The real part as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double GenericRealPart<T>(this T a) where T : unmanaged => Const<T>.RealPartDelegate.Invoke(a);

		/// <summary>
		/// Get the imaginary part (or 0 if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="a">The number to get imaginary part</param>
		/// <returns>The imaginary part as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double GenericImagPart<T>(this T a) where T : unmanaged => Const<T>.ImagPartDelegate.Invoke(a);

		/// <summary>
		/// Get the real part (or itself if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The input data type</typeparam>
		/// <typeparam name="U">The output data type</typeparam>
		/// <param name="a">The number to get real part</param>
		/// <returns>The real part as a <typeparamref name="U"/> or 0 if <typeparamref name="U"/> is not a real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static U GenericRealPart<T, U>(this T a) where T : unmanaged where U : unmanaged => ConstConvert<T, U>.GetRealPartDelegate.Invoke(a);

		/// <summary>
		/// Get the imaginary part (or itself if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The input data type</typeparam>
		/// <typeparam name="U">The output data type</typeparam>
		/// <param name="a">The number to get real part</param>
		/// <returns>The imaginary part as a <typeparamref name="U"/> or 0 if <typeparamref name="U"/> is not a real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static U GenericImagPart<T, U>(this T a) where T : unmanaged where U : unmanaged => ConstConvert<T, U>.GetRealPartDelegate.Invoke(a);

		/// <summary>
		/// Get the generic converter from type <typeparamref name="T"/> to type <typeparamref name="U"/>
		/// </summary>
		/// <typeparam name="T">The input data type</typeparam>
		/// <typeparam name="U">The output data type</typeparam>
		/// <returns>The generic converter from type <typeparamref name="T"/> to type <typeparamref name="U"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Converter<T, U> GetGenericConverter<T, U>() where T : unmanaged where U : unmanaged => ConstConvert<T, U>.ConvertDelegate;

		/// <summary>
		/// Get the generic complex type <typeparamref name="T"/>'s real part getter whose output is of type <typeparamref name="U"/>
		/// </summary>
		/// <typeparam name="T">The input data type</typeparam>
		/// <typeparam name="U">The output data type</typeparam>
		/// <returns>The generic complex real part getter from type <typeparamref name="T"/> to type <typeparamref name="U"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Converter<T, U> GetRealPartGetter<T, U>() where T : unmanaged where U : unmanaged => ConstConvert<T, U>.GetRealPartDelegate;

		/// <summary>
		/// Get the generic complex type <typeparamref name="T"/>'s imaginary part getter whose output is of type <typeparamref name="U"/>
		/// </summary>
		/// <typeparam name="T">The input data type</typeparam>
		/// <typeparam name="U">The output data type</typeparam>
		/// <returns>The generic complex imaginary part getter from type <typeparamref name="T"/> to type <typeparamref name="U"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Converter<T, U> GetImagPartGetter<T, U>() where T : unmanaged where U : unmanaged => ConstConvert<T, U>.GetImagPartDelegate;
		#endregion
	}
}
