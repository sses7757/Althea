using Althea.Linq;


namespace Althea.Helpers
{
	/// <summary>
	/// The static class for generic enum helper methods
	/// </summary>
	public static class EnumHelper
	{
		private static class NameCacher<T> where T : struct, Enum
		{
			internal static Dictionary<T, string> names = new();
		}

		private static class MethodCacher<TEnum, TDelegate> where TEnum : struct, Enum where TDelegate : Delegate
		{
			internal static Dictionary<TEnum, TDelegate> methods = new();
		}

		/// <summary>
		/// Get the name / string representation of the given enum value
		/// </summary>
		/// <typeparam name="T">The type of the enum</typeparam>
		/// <param name="e">The value of the enum</param>
		/// <returns><paramref name="e"/>'s name / string representation</returns>
		public static string GetName<T>(this T e) where T : struct, Enum
		{
			if (!NameCacher<T>.names.TryGetValue(e, out string? name))
				name = null;
			return name ?? e.ToString();
		}

		/// <summary>
		/// Set the name / string representation of the given enum value
		/// </summary>
		/// <typeparam name="T">The type of the enum</typeparam>
		/// <param name="e">The value of the enum</param>
		/// <param name="name">The name / string representation of the given enum value <paramref name="e"/></param>
		/// <exception cref="InvalidOperationException">If <paramref name="e"/> is already defined</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is null or empty or it contains space</exception>
		public static void SetName<T>(this T e, string name) where T : struct, Enum
		{
			if (Enum.IsDefined(e))
				throw new InvalidOperationException(Resources.Parameter.InvalidValue);
			if (string.IsNullOrEmpty(name) || name.Contains(' '))
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(name));
			NameCacher<T>.names[e] = name;
		}

		/// <summary>
		/// Tries to parse the given enum <paramref name="name"/> to a enum value
		/// </summary>
		/// <typeparam name="T">The type of the enum</typeparam>
		/// <param name="e">The output value of the enum</param>
		/// <param name="name">The name / string representation of the enum to parse</param>
		/// <returns>Success or not</returns>
		public static bool TryParse<T>(string name, out T e) where T : struct, Enum
		{
			if (Enum.TryParse(name, out e))
				return true;
			if (!NameCacher<T>.names.ContainsValue(name))
				return false;
			e = NameCacher<T>.names.FirstOrDefault(kv => kv.Value == name).Key;
			return true;
		}

		/// <summary>
		/// Get the method delegate of type <typeparamref name="TDelegate"/> associated to the given enum value
		/// </summary>
		/// <typeparam name="TEnum">The type of the enum</typeparam>
		/// <typeparam name="TDelegate">The type of the delegate of the method</typeparam>
		/// <param name="e">The value of the enum</param>
		/// <returns><paramref name="e"/>'s associated method delegate, null for not existing</returns>
		public static TDelegate? GetMethod<TEnum, TDelegate>(this TEnum e) where TEnum : struct, Enum where TDelegate : Delegate
		{
			if (!MethodCacher<TEnum, TDelegate>.methods.TryGetValue(e, out TDelegate? method))
				method = null;
			return method;
		}

		/// <summary>
		/// Set the method delegate of type <typeparamref name="TDelegate"/> associated to the given enum value
		/// </summary>
		/// <typeparam name="TEnum">The type of the enum</typeparam>
		/// <typeparam name="TDelegate">The type of the delegate of the method</typeparam>
		/// <param name="e">The value of the enum</param>
		/// <param name="method">The delegate of the method to set</param>
		/// <exception cref="ArgumentException">If there are exited delegates whose parameters are the same as <typeparamref name="TDelegate"/>'s while <typeparamref name="TDelegate"/> != that delegate</exception>
		public static void SetMethod<TEnum, TDelegate>(this TEnum e, TDelegate method) where TEnum : struct, Enum where TDelegate : Delegate
		{
			if (MethodCacher<TEnum, TDelegate>.methods.Count != 0)
			{
				MethodCacher<TEnum, TDelegate>.methods[e] = method;
				return;
			}
			
		}
	}
}
