using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Mono.Options;

using Althea.Linq;

// TODO: rewrite
namespace Althea.PrerequisiteCompiler
{
	internal static class Env
	{
		internal static readonly string DllExt = Helpers.InternalHelper.IsWindows ? @"dll" : @"so";
	}

	class PrerequisiteCompiler
	{
		private const string Description =
@"Usage: dotnet CudaCShap.dll <The source code folder> [OPTIONS]
The prerequisite compiler program for Althea.
	For both Linux and Windows users, the environment variable 'LD_LIBRARY_PATH' (or 'PATH' for Windows) (contains the folders of dynamic link libraries, e.g. [lib]cublas.[so|dll], [lib]cusolve.[so|dll], [lib]cutensor.[so|dll]) should contain the MKL, CUDA, cuBLAS and cuTensor pathes. To call the 'Althea' library functions, you must have x86_64 CPUs.";

		static void Main(string[] args)
		{
			#region configure options
			bool show_help = false;
			var p = new OptionSet() {
				{ "h|help", "Show help message and exit.", v => show_help = v != null },
			};

			List<string> extra;
			string sourceFolder = "";
			bool isSuperFolder = false;
			try
			{
				extra = p.Parse(args);

				if (show_help)
				{
					// show APP description message
					Console.WriteLine(Description);
					// output the options
					Console.WriteLine("Options:");
					p.WriteOptionDescriptions(Console.Out);
					return;
				}

				if (extra.Count == 0)
					throw new ArgumentException("Source code folder not specified.");
				if (extra.Count > 1)
					throw new ArgumentException("Unexpected argument(s).");
				sourceFolder = extra[0];
				if (!Directory.Exists(sourceFolder))
					throw new DirectoryNotFoundException($"{sourceFolder} is not a folder");
				isSuperFolder = Directory.GetDirectories(sourceFolder).Length != 0 && 
								Directory.GetDirectories(sourceFolder).All(dir => Directory.GetFiles(dir, "*.cpp").Length != 0 ||
																				Directory.GetFiles(dir, "*.cu").Length != 0);
				bool isSrcFolder = Directory.GetFiles(sourceFolder, "*.cpp").Length != 0 || Directory.GetFiles(sourceFolder, "*.cu").Length != 0;
				if (isSuperFolder == isSrcFolder)
					throw new DirectoryNotFoundException($"{sourceFolder} contains both source code and source folder(s) or contains none of them.");
			}
			catch (Exception e)
			{
				Console.Error.Write("Althea: ");
				Console.Error.WriteLine(e.Message);
				Console.WriteLine("Try 'Althea --help' for more information.");
				throw;
			}
			#endregion

			#region compile
			if (isSuperFolder)
			{
				int maxLen = Directory.GetDirectories(sourceFolder).Select(f => f.Length).Max();
				foreach (var f in Directory.GetDirectories(sourceFolder))
				{
					Console.WriteLine($"{"Source code folder",18} {f.PadRight(maxLen)} Found");
					CompileKernel(f, Path.GetFileName(f));
				}
			}
			else
			{
				CompileKernel(sourceFolder, Path.GetDirectoryName(sourceFolder));
			}
			#endregion
		}

		static Action<object, DataReceivedEventArgs> OutputHandler(bool isError)
		{
			void outHandler(object o, DataReceivedEventArgs outline)
			{
				var writer = isError ? Console.Error : Console.Out;
				if (outline.Data != null && outline.Data.Trim().Replace("\t", "").Length != 0)
					writer.WriteLine("\t" + outline.Data);
			}
			return (o, outline) => outHandler(o, outline);
		}

		private static void ProcessRun(string name, string arguments, string errorOut, Action postAction = null)
		{
			Console.WriteLine($"Executing command:{Environment.NewLine}\t{name} {arguments}");
			Process proc = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = name,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};
			try
			{
				// output and error (asynchronous) handlers
				proc.OutputDataReceived += new DataReceivedEventHandler(OutputHandler(false));
				proc.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler(true));
				// Start process and handlers
				proc.Start();
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();
				proc.WaitForExit();
				// exit code to check success or not
				if (proc.ExitCode != 0)
					throw new ApplicationException($"{errorOut} with exit code {proc.ExitCode}");
			}
			finally
			{
				proc.Dispose();
				postAction?.Invoke();
			}
			Console.WriteLine("Execution success");
		}

		static void TryDelete(params string[] path)
		{
			foreach (var p in path)
			{
				try
				{
					File.Delete(p);
				}
				catch (IOException) { }
			}
		}

		// Ignore Spelling: ln lcublas lcusparse Xcompiler nvcc
		private static void CompileKernel(string path, string name)
		{
			if (File.Exists($"{name}.{Env.DllExt}"))
			{
				Console.WriteLine($"{"Compilation result",18} {name}.{Env.DllExt} already exist");
				return;
			}
			var cuFiles = Directory.GetFiles(path, "*.cu").Concat(Directory.GetFiles(path, "*.cpp"));
			int maxLen = cuFiles.Select(f => f.Length).Max();
			foreach (var f in cuFiles)
			{
				Console.WriteLine($"{"File",18} {f.PadRight(maxLen)} Found");
			}
			var allFiles = string.Join(" --shared ", cuFiles.Select(s => $"\"{s}\""));
			string arguments = $"{(cuFiles.Any(f => f.Contains(".cu")) ? "-lcublas " : "")}-o {name}.{Env.DllExt} {allFiles} {(Helpers.InternalHelper.IsWindows ? "" : "-Xcompiler \"-fPIC\"")}";
			ProcessRun("nvcc", arguments, "NVCC compile failed", () => TryDelete($"{name}.lib", $"{name}.a", $"{name}.exp"));
		}
	}
}
