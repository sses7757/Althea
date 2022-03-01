using System;
using System.Reflection;

using Althea.Resources;


namespace Althea.Helpers
{
	/// <summary>
	/// A static class that contains helper functions using reflections
	/// </summary>
	public static class ReflectionHelper
	{
		/// <summary>
		/// Get the name string representation of given <paramref name="type"/> together with its generic parameters
		/// </summary>
		/// <param name="type">The given <see cref="Type"/> to get name</param>
		/// <param name="full">Whether to use <see cref="Type.FullName"/> or only <see cref="MemberInfo.Name"/></param>
		/// <returns>The name string representation of given <paramref name="type"/> or null if the given <paramref name="type"/>'s name cannot be obtained.</returns>
		public static string? GetGenericString(this Type type, bool full = false)
		{
			string? name = full ? type.FullName : type.Name;
			if (name is null)
				return null;
			if (type.IsGenericType)
			{
				var args = type.GenericTypeArguments;
				name += $"<{string.Join(", ", args.Select(a => a.GetGenericString(full)).ToArray())}>";
			}
			return name;
		}

		internal static Type? GetTypeWithPostfix(this Type type, string postfix, int skipGeneric = 0)
		{
			Type[] generics = type.GenericTypeArguments;
			string fullName = type.AssemblyQualifiedName ?? throw new ArgumentException(Parameter.UnexpectedValue, nameof(type));
			int genericStart = fullName.IndexOf('`');
			string postfixedName;
			if (genericStart >= 0)
			{
				int genericEnd = fullName.IndexOf("]]");
				if (genericEnd < 0)
					throw new ArgumentException(Parameter.UnexpectedValue, nameof(type));
				genericEnd += 2;
				if (generics.Length > skipGeneric)
				{
					generics = generics[skipGeneric..];
					var genericNames = generics.Select(static g => g.AssemblyQualifiedName).ToArray();
					postfixedName = fullName[..genericStart] + $"`{generics.Length}[[{string.Join("],[", genericNames)}]]" + fullName[genericEnd..];
				}
				else
				{
					postfixedName = fullName[..genericStart] + fullName[genericEnd..];
				}
			}
			else
			{
				postfixedName = fullName;
			}
			postfixedName += postfix;
			// return
			return Type.GetType(postfixedName) ?? throw new TypeAccessException();
		}

		/*
		/// <summary>
		/// Compile a given <b>static</b> function and return the <see cref="MethodInfo"/> of the compiled function.
		/// </summary>
		/// <param name="namespaceName">The name of the function's name space</param>
		/// <param name="className">The name of the function's static class</param>
		/// <param name="funtionName">The name of the function which must be unique</param>
		/// <param name="functionCode">The full code of the function, like<code>
		/// public static long AddFunction&lt;T&gt;(T a, T b) where T : struct
		/// {
		///		// the codes
		/// }
		/// </code></param>
		/// <param name="references">The type references whose assemblies will be referenced as well</param>
		/// <returns>The <see cref="MethodInfo"/> of the compiled method.</returns>
		/// <exception cref="InvalidOperationException">If compilation error(s) occurred</exception>
		public static MethodInfo CompileMethod(string namespaceName, string className, string funtionName, string functionCode, params Type[] references)
		{
			var usings = references.Select(static r => r.Namespace)
								   .Where(static r => !string.IsNullOrWhiteSpace(r))
								   .Distinct();
			string usingAll = string.Join(";" + Environment.NewLine + "using ", usings);
			functionCode = $@"using System;
using {usingAll};

namespace {namespaceName}
{{
	public static partial class {className}
	{{
		{functionCode}
	}}
}}";
			var syntaxTree = CSharpSyntaxTree.ParseText(functionCode);
			var refs = references.Select(static r => r.Assembly.Location)
								 .Append(typeof(object).GetTypeInfo().Assembly.Location)
								 .Append(Assembly.Load("System.Runtime").Location)
								 .Where(static r => !string.IsNullOrWhiteSpace(r))
								 .Distinct()
								 .Select(static r => MetadataReference.CreateFromFile(r));
			var compilation = CSharpCompilation.Create(namespaceName, new[] { syntaxTree }, refs, new(OutputKind.DynamicallyLinkedLibrary));

			using MemoryStream ms = new();
			EmitResult result = compilation.Emit(ms);
			if (!result.Success)
			{
				StringBuilder message = new();
				var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
				foreach (Diagnostic diagnostic in failures)
				{
					message.Append(diagnostic.Id).Append(": ").AppendLine(diagnostic.GetMessage());
				}
				throw new InvalidOperationException(message.ToString());
			}
			ms.Seek(0, SeekOrigin.Begin);
			AssemblyLoadContext context = AssemblyLoadContext.Default;
			Assembly assembly = context.LoadFromStream(ms);
			try
			{
				var mappingFunction = assembly.DefinedTypes.First().GetMethod(funtionName);
				if (mappingFunction is null)
					throw new InvalidOperationException();
				return mappingFunction;
			}
			catch (System.Exception e)
			{
				throw new InvalidOperationException(null, e);
			}
		}
		*/
	}
}
