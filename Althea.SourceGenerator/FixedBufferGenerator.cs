using Microsoft.CodeAnalysis;

using System.Diagnostics;
using System.IO;


namespace Althea.SourceGenerator
{
	[Generator]
    public class FixedBufferGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
#if DEBUG
			////Debugger.Launch();
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			bool unmanagedGenerated = false, classGenerated = false;

			foreach (var file in context.AdditionalFiles)
			{
				if (!unmanagedGenerated && file.Path.EndsWith(@"FixedUnmanagedBuffer.cs"))
				{
					// generate FixedUnmanagedBuffers
					string formatUnmanagedBufferDefine = File.ReadAllText(file.Path);
					var unmanagedSizes = new int[] { 8, 12, 16, 32, 64, 128, 256 };
					foreach (var s in unmanagedSizes)
					{
						string ss = s.ToString();
						string realBufferDefine = formatUnmanagedBufferDefine.Replace("__placeholder__", ss);
						context.AddSource($"FixedBuffer_{s}.cs", realBufferDefine);
					}
					unmanagedGenerated = true;
				}
				else if (!classGenerated && file.Path.EndsWith(@"FixedClassBuffer.cs"))
				{
					// generate FixedClassBuffers
					string formatClassBufferDefine = File.ReadAllText(file.Path);
					var classSizes = new int[] { 2, 4, 8, 16 };
					foreach (var s in classSizes)
					{
						string ss = s.ToString();
						string realBufferDefine = formatClassBufferDefine.Replace("__placeholder__", ss);
						context.AddSource($"FixedClassBuffer_{s}.cs", realBufferDefine);
					}
					classGenerated = true;
				}
			}
		}
	}
}
