using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

// Ignore Spelling: nameof

namespace Althea.SourceGenerator
{
	#region marking attributes
	[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class AbstractRuntimeApiAttribute : Attribute
	{
		public AbstractRuntimeApiAttribute()
		{ }
	}

	[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class AbstractApiMethodAttribute : Attribute
	{
		public AbstractApiMethodAttribute(bool duplicateTVariant = false)
		{ }
	}

	[System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
	public sealed class DuplicateTParameterAttribute : Attribute
	{
		public DuplicateTParameterAttribute()
		{ }
	}
	#endregion

	[Generator]
	public class ApiSelectorGenerator : ISourceGenerator
	{
		private static readonly DiagnosticDescriptor MultipleReturnsError =
#pragma warning disable RS2008
			new DiagnosticDescriptor(id: "GENAPI001",
									title: "Target API method has multiple returns",
									messageFormat: "Couldn't generate API selector method of '{0}' since it has multiple return parameters",
									category: "API Selector Generator",
									DiagnosticSeverity.Error,
									isEnabledByDefault: true);
#pragma warning restore RS2008

		public void Initialize(GeneratorInitializationContext context)
		{
#if DEBUG
			////Debugger.Launch();
#endif
			// Register a factory that can create our custom syntax receiver
			context.RegisterForSyntaxNotifications(() => new ApiClassSyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			ApiClassSyntaxReceiver syntaxReceiver = (ApiClassSyntaxReceiver)context.SyntaxReceiver;
			var apiClasses = syntaxReceiver.ApiClasses;

			// get type parameter T
			TypeParameterSyntax typeT = null;
			TypeParameterConstraintClauseSyntax typeTConstraint = null;
			foreach (var apiClass in apiClasses)
			{
				var methods = apiClass.ChildNodes().Where(s => s is MethodDeclarationSyntax m && m.HasAttribute(nameof(AbstractApiMethodAttribute)));
				foreach (var methodNode in methods)
				{
					if (!(methodNode is MethodDeclarationSyntax m))
						continue;
					if (!m.HasAttribute(nameof(AbstractApiMethodAttribute)))
						continue;

					var typeParams = m.TypeParameterList.Parameters.Zip(m.ConstraintClauses, (p, c) => (p, c)).Where(pc => pc.p.Identifier.ToString() == "T" && pc.c.Constraints.Where(cc => cc.ToString() == "unmanaged").Any());
					if (typeParams.Any())
					{
						typeT = typeParams.First().p;
						typeTConstraint = typeParams.First().c;
					}
				}
			}

			// construct classes
			foreach (var apiClass in apiClasses)
			{
				string classText = apiClass.ToString();
				var ns = apiClass.Parent as NamespaceDeclarationSyntax;
				var usings = (ns.Parent as CompilationUnitSyntax).Usings;
				string selectorName = apiClass.Identifier.ToString().Replace("Abstract", "") + "Selector";
				string usingStatements = string.Join(Environment.NewLine, usings.Where(u => !u.ToString().Contains("Althea.SourceGenerator")));
				if (!usingStatements.Contains("using Althea.NativeTypes;"))
					usingStatements += Environment.NewLine + "using Althea.NativeTypes;";
				if (!usingStatements.Contains("using Althea.Resources;"))
					usingStatements += Environment.NewLine + "using Althea.Resources;";
				int classDocStart = ns.ToString().IndexOf("/// <summary>"), classDocEnd = ns.ToString().IndexOf("	/// </summary>");
				string generated = $@"{usingStatements}

namespace {ns.Name}
{{
	{ns.ToString().Substring(classDocStart, classDocEnd - classDocStart).Replace("abstract class", "selector class")}	/// </summary>
	public sealed partial class {selectorName} : AbstractApiSelector<{apiClass.Identifier}>
	{{
";
				var methods = apiClass.ChildNodes().Where(s => s is MethodDeclarationSyntax m && m.HasAttribute(nameof(AbstractApiMethodAttribute)));
				foreach (var methodNode in methods)
				{
					// basic info
					var method = (MethodDeclarationSyntax)methodNode;
					var attr = method.GetAttribute(nameof(AbstractApiMethodAttribute));
					bool duplicateT = attr.ArgumentList != null && attr.ArgumentList.Arguments.Count == 1 && attr.ArgumentList.Arguments[0].ToString() == "true";
					var allParams = method.ParameterList.Parameters;
					var orgTypeParams = method.TypeParameterList;
				RESTART:
					var returnParams = allParams.Where(p => p.Modifiers.Count == 1 && p.Modifiers[0].Text == "out");
					if (returnParams.Count() > 1)
					{
						context.ReportDiagnostic(Diagnostic.Create(MultipleReturnsError, returnParams.Skip(1).First().GetLocation(), method.Identifier));
						continue;
					}
					bool hasReturn = returnParams.Any();
					var returnParam = returnParams.FirstOrDefault();

					// add document
					int methodPos = classText.IndexOf(method.ToString());
					string document = classText.Substring(0, methodPos);
					int docStart = document.LastIndexOf("\t\t/// <summary>");
					document = document.Substring(docStart);
					if (duplicateT)
					{
						document = document.Replace("in bytes", @"in <typeparamref name=""T""/>")
										   .Replace("</summary>", @"</summary>
		/// <typeparam name=""T"">Any unmanaged number struct as the data type</typeparam>");
					}
					if (hasReturn)
					{
						string retDoc = Regex.Match(document, $@"<param name=""{returnParam.Identifier}"">(.+?)</param>").Groups[1].Value;
						document = Regex.Replace(document, @"<returns>.+?</returns>", $@"<returns>{retDoc}</returns>");
						document = Regex.Replace(document, $@"\r?\n\t+/// ?<param name=""{returnParam.Identifier}"">.+?</param>", "");
					}
					else
					{
						document = Regex.Replace(document, @"\r?\n\t+/// ?<returns>.+?</returns>", "");
					}
					var upperCaseMatch = Regex.Match(document, @"When implemented by a derived class, (\w)").Groups[1].Value.ToUpper();
					document = Regex.Replace(document, @"When implemented by a derived class, \w", upperCaseMatch);
					generated += document + @"/// <exception cref=""InvalidOperationException"">If an error occurred during selecting the implementation</exception>" + Environment.NewLine;

					// add method declaration
					var newAttrs = method.RemoveAttribute(nameof(AbstractApiMethodAttribute));
					var retType = hasReturn ? returnParam.Type : syntaxReceiver.VoidReturnType;
					var newParams = hasReturn ? allParams.Remove(returnParam) : allParams;
					var typeParams = duplicateT ? orgTypeParams.WithParameters(default).AddParameters(typeT).AddParameters(orgTypeParams.Parameters.ToArray()) : orgTypeParams;
					var typeParamsConstrain = duplicateT ? new SyntaxList<TypeParameterConstraintClauseSyntax>().Add(typeTConstraint).AddRange(method.ConstraintClauses) : method.ConstraintClauses;
					method = method.Update(newAttrs, method.Modifiers, retType, method.ExplicitInterfaceSpecifier, method.Identifier, typeParams, method.ParameterList.WithParameters(newParams), typeParamsConstrain, null, null, method.SemicolonToken);
					string methodMain = method.ToString()
											   .Replace(" abstract ", " static ")
											   .Replace(" virtual ", " static ")
											   .Replace("unsafe ", "")
											   .Replace($"[{nameof(DuplicateTParameterAttribute).Replace("Attribute", "")}] ", "");
					methodMain = Regex.Replace(methodMain, @"<T,([^ ])", @"<T, $1");
					methodMain = Regex.Replace(methodMain, @"([^ ])where" , @"$1 where");
					methodMain = Regex.Replace(methodMain, @"\[\]\r\n?", "");
					string newParamsInvoke = string.Join(", ", newParams.Select(p => duplicateT && p.HasAttribute(nameof(DuplicateTParameterAttribute)) ? $"{p.Identifier} * Unmanaged<T>.Size" : p.Identifier.ToString()));
					if (duplicateT)
					{
						methodMain = Regex.Replace(methodMain, @" *;$", $" => {method.Identifier}{orgTypeParams}({newParamsInvoke})" + (returnParam.HasAttribute(nameof(DuplicateTParameterAttribute)) ? " * Unmanaged<T>.Size;" : ";"));
					}
					else if (hasReturn)
					{
						string allParamsInvoke = string.Join(", ", allParams.Select(p => p == returnParam ? $"out {p.Type} {p.Identifier}" : p.Identifier.ToString()));
						methodMain = Regex.Replace(methodMain, @" *;$", $@"
		{{
			foreach (var api in ApiEnumerable)
			{{
				if (api.{method.Identifier}{orgTypeParams}({allParamsInvoke}))
					return {returnParam.Identifier};
			}}
			throw new InvalidOperationException(Backend.NotAvailable);
		}}");
					}
					else
					{
						string allParamsInvoke = string.Join(", ", allParams.Select(p => p.Identifier));
						methodMain = Regex.Replace(methodMain, @" *;$", $@"
		{{
			foreach (var api in ApiEnumerable)
			{{
				if (api.{method.Identifier}{orgTypeParams}({allParamsInvoke}))
					return;
			}}
			throw new InvalidOperationException(Backend.NotAvailable);
		}}");
					}
					generated += methodMain + Environment.NewLine + Environment.NewLine;

					// restart with no additional T
					if (duplicateT)
					{
						duplicateT = false;
						method = (MethodDeclarationSyntax)methodNode;
						goto RESTART;
					}
				}

				generated += "	}" + Environment.NewLine + "}";
				// add the generated implementation to the compilation
				SourceText sourceText = SourceText.From(generated, Encoding.UTF8);
				context.AddSource($"{ns.Name}.{selectorName}.cs", sourceText);
			}
		}
	}

	class ApiClassSyntaxReceiver : ISyntaxReceiver
	{
		public List<ClassDeclarationSyntax> ApiClasses { get; } = new List<ClassDeclarationSyntax>();

		public TypeSyntax VoidReturnType { get; private set; }

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			// Business logic to decide what we're interested in goes here
			if (syntaxNode is ClassDeclarationSyntax cds && cds.HasAttribute(nameof(AbstractRuntimeApiAttribute)))
			{
				ApiClasses.Add(cds);
			}
			if (this.VoidReturnType is null && syntaxNode is MethodDeclarationSyntax mds && mds.ReturnType.ToString() == "void")
			{
				this.VoidReturnType = mds.ReturnType;
			}
		}
	}

	static class Extensions
	{
		public static bool HasAttribute(this ClassDeclarationSyntax cds, string attributeName)
		{
			attributeName = attributeName.Replace("Attribute", "");
			foreach (var attrList in cds.AttributeLists)
			{
				foreach (var attr in attrList.Attributes)
					if (attr.Name.ToString() == attributeName)
						return true;
			}
			return false;
		}
		public static bool HasAttribute(this MethodDeclarationSyntax mds, string attributeName)
		{
			attributeName = attributeName.Replace("Attribute", "");
			foreach (var attrList in mds.AttributeLists)
			{
				foreach (var attr in attrList.Attributes)
					if (attr.Name.ToString() == attributeName)
						return true;
			}
			return false;
		}
		public static AttributeSyntax GetAttribute(this MethodDeclarationSyntax mds, string attributeName)
		{
			attributeName = attributeName.Replace("Attribute", "");
			foreach (var attrList in mds.AttributeLists)
			{
				foreach (var attr in attrList.Attributes)
					if (attr.Name.ToString() == attributeName)
						return attr;
			}
			return null;
		}
		public static SyntaxList<AttributeListSyntax> RemoveAttribute(this MethodDeclarationSyntax mds, string attributeName)
		{
			attributeName = attributeName.Replace("Attribute", "");
			SyntaxList<AttributeListSyntax> result = new SyntaxList<AttributeListSyntax>();
			foreach (var attrList in mds.AttributeLists)
			{
				var newAttrList = attrList;
				foreach (var attr in attrList.Attributes)
					if (attr.Name.ToString() == attributeName)
						newAttrList = newAttrList.RemoveNode(attr, SyntaxRemoveOptions.KeepNoTrivia);
				result = result.Add(newAttrList);
			}
			return result;
		}
		public static bool HasAttribute(this ParameterSyntax pds, string attributeName)
		{
			attributeName = attributeName.Replace("Attribute", "");
			foreach (var attrList in pds.AttributeLists)
			{
				foreach (var attr in attrList.Attributes)
					if (attr.Name.ToString() == attributeName)
						return true;
			}
			return false;
		}
	}
}
