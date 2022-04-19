using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace Althea.SourceGenerator
{
	/// <summary>
	/// Tells the source generator that the marked class is one that contains native methods to be extended.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class NativeMethodClassAttribute : Attribute
	{
	}

	/// <summary>
	/// Tells the source generator that the marked method is a native method to be extended.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class NativeMethodAttribute : Attribute
	{
		/// <summary>
		/// Create a new <see cref="NativeMethodAttribute"/> by indicating the type character position and whether it shall be upper case or not.
		/// </summary>
		/// <param name="typeCharPosition">The type character position, such as 6 for <c>cblas_?copy</c></param>
		/// <param name="typeCharUpper">Whether the type character in <paramref name="typeCharPosition"/> shall be of upper case or lower case</param>
		/// <param name="refComplexType">Whether the complex scalar input parameters shall have <c>in</c> modifiers or not</param>
		/// <param name="returnRealType">Whether to return real scalar type instead of complex scalar type</param>
		/// <param name="onlyReal">Whether to include only real types (true) or only complex types (false) or both (leave empty)</param>
		/// <remarks>This means that the types are <c>float, double, Complex&lt;float&gt;, Complex&lt;double&gt;</c> with type character <c>s, d, c, z</c>, respectively.</remarks>
		public NativeMethodAttribute(int typeCharPosition, bool typeCharUpper = false, bool refComplexType = false, bool returnRealType = false, bool onlyReal = false)
		{
		}
	}

	/// <summary>
	/// Tells the source generator that the marked method is a custom-typed native method to be extended.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	public sealed class CustomNativeMethodAttribute : Attribute
	{
		/// <summary>
		/// Create a new <see cref="NativeMethodAttribute"/> by indicating the type character position and the full type names and characters.
		/// </summary>
		/// <param name="typeCharPosition">The variable type character position, such as 6 for <c>cblas_?copy</c>. Must be the same across multiple attributes.</param>
		/// <param name="typeName">The full variable type name, such as <c>Complex&lt;float&gt;</c></param>
		/// <param name="typeChar">The variable type character in the method identifier</param>
		/// <param name="inputModifier">The input parameter modifier</param>
		/// <param name="returnName">The full return type name, empty means <paramref name="typeName"/></param>
		/// <param name="refReturn">Whether to return the result by reference or not</param>
		public CustomNativeMethodAttribute(int typeCharPosition, string typeName, string typeChar, string inputModifier = "", string returnName = "", bool refReturn = false)
		{
		}
	}

	[Generator]
	public class NativeMethodsGenerator : ISourceGenerator
	{
		private static readonly DiagnosticDescriptor InvalidNativeMethodError =
#pragma warning disable RS2008
			new DiagnosticDescriptor("GENAPI002",
									"Target method is not a native method",
									"Couldn't generate other native methods of '{0}' since it is not a valid native method",
									"Native Methods Generator",
									DiagnosticSeverity.Error,
									true);
#pragma warning restore RS2008

		public void Initialize(GeneratorInitializationContext context)
		{
#if DEBUG
			Debugger.Launch();
#endif
			// Register a factory that can create our custom syntax receiver
			context.RegisterForSyntaxNotifications(() => new NativeMethodClassSyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			NativeMethodClassSyntaxReceiver syntaxReceiver = (NativeMethodClassSyntaxReceiver)context.SyntaxReceiver;
			var classes = syntaxReceiver.NativeMethodClasses;
			var ids = syntaxReceiver.NativeMethodClassesID;

			// construct classes
			foreach (var (c, id) in classes.Zip(ids, (a, b) => (a, b)))
			{
				string className = c.Identifier.ToString().Replace("Template", "");
				var ns = c.Parent as NamespaceDeclarationSyntax;
				var usings = (ns.Parent as CompilationUnitSyntax).Usings;
				string usingStatements = string.Join(Environment.NewLine, usings.Where(u => !u.ToString().Contains("Althea.SourceGenerator")));
				string generated = $@"{usingStatements}

namespace {ns.Name}
{{
	public static unsafe partial class {className}
	{{
";
				void AddMethods(MethodDeclarationSyntax method, int typeCharPos, string[] typeNames, string[] typeChars, string[] typeInputModifer, string[] typeReturn, bool[] typeRefReturn, string attributeName)
				{
					int orgInd = 0;
					for (int i = 0; i < typeNames.Length; i++)
					{
						if (method.Identifier.ToString().Substring(typeCharPos, typeChars[i].Length) == typeChars[i])
						{
							orgInd = i;
							break;
						}
					}
					bool removeReturn = false;
					string methodMain = null, identifier = null;
					for (int i = 0; i < typeNames.Length; i++)
					{
						// change identifier
						identifier = method.Identifier.ToString();
						identifier = identifier.Substring(0, typeCharPos) + typeChars[i] + identifier.Substring(typeCharPos + typeChars[orgInd].Length);
						// add method declaration
						var newAttrs = method.RemoveAttribute(attributeName);
						var newMethod = method.WithAttributeLists(newAttrs);
						methodMain = newMethod.ToString();
						methodMain = methodMain.Replace(method.Identifier.ToString() + "(", identifier + "(")
											   .Replace(", " + typeNames[orgInd], ", " + typeNames[i])
											   .Replace("," + typeNames[orgInd], ", " + typeNames[i])
											   .Replace("(" + typeNames[orgInd], "(" + typeNames[i]);
						if (typeInputModifer[i] != "")
						{
							methodMain = Regex.Replace(methodMain, @", ?" + typeNames[i] + @"([^\*])", ", " + typeInputModifer[i] + typeNames[i] + @"$1");
							methodMain = Regex.Replace(methodMain, @"\(" + typeNames[i] + @"([^\*])", "(" + typeInputModifer[i] + typeNames[i] + @"$1");
						}
						if (typeRefReturn[i])
						{
							methodMain = methodMain.Replace(");", $", out {typeReturn[i]} result);")
												   .Replace(typeReturn[orgInd] + " " + identifier, "void " + identifier);
							removeReturn = true;
						}
						else
						{
							methodMain = methodMain.Replace(typeReturn[orgInd] + " " + identifier, typeReturn[i] + " " + identifier);
						}
						methodMain = Regex.Replace(methodMain, @"\[\]\r?\n", "");
						methodMain = Regex.Replace(methodMain, @"\t{3,}", "\t\t");
						generated += methodMain + Environment.NewLine + Environment.NewLine;
					}
					if (attributeName == nameof(NativeMethodAttribute) && methodMain.Contains("in " + typeNames[typeNames.Length - 1]))
					{
						string newIdentifier = method.Identifier.ToString();
						newIdentifier = newIdentifier.Substring(0, typeCharPos) + newIdentifier.Substring(typeCharPos + typeChars[orgInd].Length);
						string newParams = methodMain.Substring(methodMain.IndexOf(identifier + "(") + identifier.Length);
						newParams = newParams.Substring(0, newParams.Length - 1)
											 .Replace(", " + typeNames[typeNames.Length - 1], ", T")
											 .Replace("in " + typeNames[typeNames.Length - 1], "T");
						generated += $"\t\tinternal delegate {(removeReturn ? "void" : method.ReturnType.ToString())} {newIdentifier}<T>{newParams} where T : unmanaged, INumber<T>;";
						generated += Environment.NewLine + Environment.NewLine;
						newParams = methodMain.Substring(methodMain.IndexOf(identifier + "(") + identifier.Length);
						newParams = newParams.Substring(0, newParams.Length - 1)
											 .Replace(", " + typeNames[typeNames.Length - 1], ", T")
											 .Replace("in " + typeNames[typeNames.Length - 1], "in T");
						generated += $"\t\tinternal delegate {(removeReturn ? "void" : method.ReturnType.ToString())} {newIdentifier}_comp<T>{newParams} where T : unmanaged, INumber<T>;";
						generated += Environment.NewLine + Environment.NewLine;
					}
					else if (attributeName == nameof(NativeMethodAttribute) && methodMain.Contains(", " + typeNames[typeNames.Length - 1]))
					{
						string newIdentifier = method.Identifier.ToString();
						newIdentifier = newIdentifier.Substring(0, typeCharPos) + newIdentifier.Substring(typeCharPos + typeChars[orgInd].Length);
						string newParams = methodMain.Substring(methodMain.IndexOf(identifier + "(") + identifier.Length);
						newParams = newParams.Substring(0, newParams.Length - 1)
											 .Replace(", " + typeNames[typeNames.Length - 1], ", T")
											 .Replace("in " + typeNames[typeNames.Length - 1], "T");
						generated += $"\t\tinternal delegate {(removeReturn ? "void" : method.ReturnType.ToString())} {newIdentifier}<T>{newParams} where T : unmanaged, INumber<T>;";
						generated += Environment.NewLine + Environment.NewLine;
					}
				}


				var methods = c.ChildNodes().Where(s => s is MethodDeclarationSyntax m && m.HasAttribute(nameof(NativeMethodAttribute)));
				foreach (var methodNode in methods)
				{
					Location errLoc = Location.None;
					var method = (MethodDeclarationSyntax)methodNode;
					// get attribute
					var attr = method.GetAttribute(nameof(NativeMethodAttribute));
					if (attr.ArgumentList is null || !method.Modifiers.HasToken("static") || !method.Modifiers.HasToken("extern"))
					{
						errLoc = method.GetLocation();
						goto ERROR;
					}
					if (!int.TryParse(attr.ArgumentList.Arguments[0].ToString(), out var typeCharPos))
					{
						errLoc = attr.ArgumentList.Arguments[0].GetLocation();
						goto ERROR;
					}
					string[] typeNames = null, typeChars = null, typeInputModifer = null, typeReturn = null;
					bool[] typeRefReturn = new[] { false, false, false, false };
					if (attr.ArgumentList.Arguments.Count != 1 && attr.ArgumentList.Arguments[1].ToString() != "true" && attr.ArgumentList.Arguments[1].ToString() != "false")
					{
						errLoc = attr.ArgumentList.GetLocation();
						goto ERROR;
					}
					if (attr.ArgumentList.Arguments.Count >= 1)
					{
						typeNames = new[] { "float", "double", "Complex<float>", "Complex<double>" };
						typeChars = new[] { "s", "d", "c", "z" };
						typeInputModifer = new[] { "", "", "", "" };
						typeReturn = typeNames;
					}
					bool upper = false, refComp = false;
					if (attr.ArgumentList.Arguments.Count >= 2)
					{
						if (!bool.TryParse(attr.ArgumentList.Arguments[1].ToString(), out upper))
						{
							errLoc = attr.ArgumentList.Arguments[1].GetLocation();
							goto ERROR;
						}
						if (upper)
							typeChars = new[] { "S", "D", "C", "Z" };
					}
					if (attr.ArgumentList.Arguments.Count >= 3)
					{
						if (!bool.TryParse(attr.ArgumentList.Arguments[2].ToString(), out refComp))
						{
							errLoc = attr.ArgumentList.Arguments[2].GetLocation();
							goto ERROR;
						}
						if (refComp)
						{
							typeInputModifer = new[] { "", "", "in ", "in " };
							typeRefReturn = method.ReturnType.ToString().Contains("void") ? typeRefReturn : new[] { false, false, true, true };
						}
					}
					if (attr.ArgumentList.Arguments.Count >= 4)
					{
						if (!bool.TryParse(attr.ArgumentList.Arguments[3].ToString(), out bool retReal))
						{
							errLoc = attr.ArgumentList.Arguments[3].GetLocation();
							goto ERROR;
						}
						if (retReal)
						{
							typeReturn = new[] { "float", "double", "float", "double" };
							typeChars = upper ? new[] { "S", "D", "SC", "DZ" } : new[] { "s", "d", "sc", "dz" };
						}
					}
					if (attr.ArgumentList.Arguments.Count == 5)
					{
						bool? onlyReal = null;
						if (bool.TryParse(attr.ArgumentList.Arguments[4].ToString(), out bool or))
						{
							onlyReal = or;
						}
						if (onlyReal.HasValue)
						{
							int start = onlyReal.Value ? 0 : 2;
							typeNames = typeNames.AsSpan(start, 2).ToArray();
							typeChars = typeChars.AsSpan(start, 2).ToArray();
							typeInputModifer = typeInputModifer.AsSpan(start, 2).ToArray();
							typeReturn = typeReturn.AsSpan(start, 2).ToArray();
							typeRefReturn = typeRefReturn.AsSpan(start, 2).ToArray();
						}
					}
					if (attr.ArgumentList.Arguments.Count > 5)
					{
						errLoc = attr.ArgumentList.GetLocation();
						goto ERROR;
					}
					AddMethods(method, typeCharPos, typeNames, typeChars, typeInputModifer, typeReturn, typeRefReturn, nameof(NativeMethodAttribute));
					continue;

				ERROR:
					context.ReportDiagnostic(Diagnostic.Create(InvalidNativeMethodError, errLoc, method.Identifier));
				}

				methods = c.ChildNodes().Where(s => s is MethodDeclarationSyntax m && m.HasAttribute(nameof(CustomNativeMethodAttribute)));
				foreach (var methodNode in methods)
				{
					Location errLoc = Location.None;
					var method = (MethodDeclarationSyntax)methodNode;
					// get attribute
					var attrs = method.GetAttributes(nameof(CustomNativeMethodAttribute));
					if (!method.Modifiers.HasToken("static") || !method.Modifiers.HasToken("extern"))
					{
						errLoc = method.GetLocation();
						goto ERROR;
					}
					int typeCharPos = -1;
					string[] typeNames = new string[attrs.Length], typeChars = new string[attrs.Length], typeInputModifer = new string[attrs.Length], typeReturn = new string[attrs.Length];
					bool[] typeRefReturn = new bool[attrs.Length];
					for (int i = 0; i < attrs.Length; i++)
					{
						var attr = attrs[i];
						if (attr.ArgumentList.Arguments.Count < 3 || attr.ArgumentList.Arguments.Count > 6)
						{
							errLoc = attr.ArgumentList.GetLocation();
							goto ERROR;
						}
						if (!int.TryParse(attr.ArgumentList.Arguments[0].ToString(), out var _pos) || (typeCharPos >= 0 && typeCharPos != _pos))
						{
							errLoc = attr.ArgumentList.Arguments[0].GetLocation();
							goto ERROR;
						}
						typeCharPos = _pos;

						string str = attr.ArgumentList.Arguments[1].ToString();
						if (!str.StartsWith("\"") || !str.EndsWith("\""))
						{
							errLoc = attr.ArgumentList.Arguments[1].GetLocation();
							goto ERROR;
						}
						typeNames[i] = str.Substring(1, str.Length - 2).Trim();
						str = attr.ArgumentList.Arguments[2].ToString();
						if (!str.StartsWith("\"") || !str.EndsWith("\""))
						{
							errLoc = attr.ArgumentList.Arguments[1].GetLocation();
							goto ERROR;
						}
						typeChars[i] = str.Substring(1, str.Length - 2).Trim();

						typeInputModifer[i] = ""; typeReturn[i] = typeNames[i]; typeRefReturn[i] = false;
						if (attr.ArgumentList.Arguments.Count >= 4)
						{
							str = attr.ArgumentList.Arguments[3].ToString();
							if (!str.StartsWith("\"") || !str.EndsWith("\""))
							{
								errLoc = attr.ArgumentList.Arguments[1].GetLocation();
								goto ERROR;
							}
							typeInputModifer[i] = str.Substring(1, str.Length - 2).Trim();
							if (string.IsNullOrWhiteSpace(typeInputModifer[i]))
								typeInputModifer[i] = "";
							else
								typeInputModifer[i] += " ";
						}
						if (attr.ArgumentList.Arguments.Count >= 5)
						{
							str = attr.ArgumentList.Arguments[4].ToString();
							if (!str.StartsWith("\"") || !str.EndsWith("\""))
							{
								errLoc = attr.ArgumentList.Arguments[1].GetLocation();
								goto ERROR;
							}
							typeReturn[i] = str.Substring(1, str.Length - 2).Trim();
							if (string.IsNullOrWhiteSpace(typeReturn[i]))
								typeReturn[i] = typeNames[i];
						}
						if (attr.ArgumentList.Arguments.Count >= 6)
						{
							str = attr.ArgumentList.Arguments[5].ToString();
							if (!bool.TryParse(str, out typeRefReturn[i]))
							{
								errLoc = attr.ArgumentList.Arguments[1].GetLocation();
								goto ERROR;
							}
							if (method.ReturnType.ToString().Contains("void"))
								typeRefReturn[i] = false;
						}
					}
					AddMethods(method, typeCharPos, typeNames, typeChars, typeInputModifer, typeReturn, typeRefReturn, nameof(CustomNativeMethodAttribute));
					continue;

				ERROR:
					context.ReportDiagnostic(Diagnostic.Create(InvalidNativeMethodError, errLoc, method.Identifier));
				}

				generated += "	}" + Environment.NewLine + "}";
				// add the generated implementation to the compilation
				SourceText sourceText = SourceText.From(generated, Encoding.UTF8);
				context.AddSource($"{ns.Name}.{className}{(id == 0 ? "" : id.ToString())}.cs", sourceText);
			}
		}
	}

	class NativeMethodClassSyntaxReceiver : ISyntaxReceiver
	{
		public List<ClassDeclarationSyntax> NativeMethodClasses { get; } = new List<ClassDeclarationSyntax>();

		public List<int> NativeMethodClassesID { get; } = new List<int>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax cds && cds.HasAttribute(nameof(NativeMethodClassAttribute)))
			{
				int count = 0;
				for (int i = 0; i < NativeMethodClasses.Count; i++)
				{
					if (NativeMethodClasses[i].Identifier == cds.Identifier && (NativeMethodClasses[i].Parent as NamespaceDeclarationSyntax).Name == (cds.Parent as NamespaceDeclarationSyntax).Name)
						count++;
				}
				NativeMethodClasses.Add(cds);
				NativeMethodClassesID.Add(count);
			}
		}
	}

}
