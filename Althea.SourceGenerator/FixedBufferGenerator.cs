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
				if (!unmanagedGenerated && file.Path.EndsWith(@"FixedUnmanagedBuffer.txt"))
				{
					// generate FixedUnmanagedBuffers
					string formatUnmanagedBufferDefine = File.ReadAllText(file.Path);
					var unmanagedSizes = new int[7];
					for (int i = 8, j = 0; i <= 256; i *= 2, j++)
					{
						unmanagedSizes[j] = i;
					}
					unmanagedSizes[6] = 12;
					foreach (var s in unmanagedSizes)
					{
						string ss = s.ToString();
						string realBufferDefine = formatUnmanagedBufferDefine.Replace("__placeholder__", ss);
						context.AddSource($"FixedBuffer_{s}.cs", realBufferDefine);
					}
					unmanagedGenerated = true;
				}
				else if (!classGenerated && file.Path.EndsWith(@"FixedClassBuffer.txt"))
				{
					// generate FixedClassBuffers
					string formatClassBufferDefine = File.ReadAllText(file.Path);
					var classSizes = new int[4];
					for (int i = 2, j = 0; i <= 16; i *= 2, j++)
					{
						classSizes[j] = i;
					}
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
