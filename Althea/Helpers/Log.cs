using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Althea.Linq;


namespace Althea.Helpers
{
	#region setting classes
	/// <summary>
	/// The logging level enumerate
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// The most detailed level of logging message
		/// </summary>
		Trace = 0,
		/// <summary>
		/// The normal information logging message
		/// </summary>
		Information = 1,
		/// <summary>
		/// The warning message
		/// </summary>
		Warning = 2,
		/// <summary>
		/// The error message
		/// </summary>
		Error = 3,
		/// <summary>
		/// The debug message
		/// </summary>
		Debug = -1
	}

	// for JSON serialization
	internal record LogSettings
	{
		public bool Suppress { get; set; }
		public int BufferSize { get; set; }
		public int WrapLimit { get; set; }
		public string Path { get; set; }

		public LogLevel[] PrintLevels { get; set; }
		public LogLevel[] BufferLevels { get; set; }

		internal LogSettings()
		{
			Suppress = false; BufferSize = 1024; WrapLimit = 125; Path = "Althea.log";
			PrintLevels = new[] { LogLevel.Error, LogLevel.Warning };
			BufferLevels = new[] { LogLevel.Trace, LogLevel.Debug };
		}

		[System.Text.Json.Serialization.JsonConstructor]
		internal LogSettings(bool suppress, int bufferSize, int wrapLimit, string path, LogLevel[] printLevels, LogLevel[] bufferLevels)
		{
			Suppress = suppress; BufferSize = bufferSize; WrapLimit = wrapLimit; Path = path;
			PrintLevels = printLevels; BufferLevels = bufferLevels;
		}
	}
	#endregion

	#region logger classes
	internal sealed class Logger : IDisposable
	{
		public void Dispose()
		{
			while (buffers.Count != 0)
			{
				var (msg, category, level) = buffers.Dequeue();
				// wait synchronously
				ActualWrite(msg, category, level).GetAwaiter().GetResult();
			}
			GC.SuppressFinalize(this);
		}

		~Logger()
		{
			this.Dispose();
		}

		private readonly Queue<(string msg, string category, LogLevel level)> buffers =
							new(Log.BufferSize);

		private int maxCategoryLength = 10;

		internal async Task Write(string msg, string? category = null, LogLevel level = LogLevel.Information)
		{
			bool showHeader = true;
			if (string.IsNullOrWhiteSpace(category))
			{
				if (category is not null)
				{
					showHeader = false;
				}
				else
				{	// Get calling method name
					category = new StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "";
				}
			}
			if (!Log.BufferLevels.Contains(level) && Log.PrintLevels.Contains(level))
			{
				if (showHeader)
					maxCategoryLength = Math.Max(maxCategoryLength, category.Length);
				if (level == LogLevel.Error)
				{
					if (showHeader)
						Console.Error.WriteLine(category.PadRight(maxCategoryLength) + $" {level,11/*length of 'Information'*/}: {Wrap(msg, 30, Log.WrapLimit)}");
					else
						Console.Error.WriteLine(msg);
				}
				else
				{
					if (showHeader)
						Console.Out.WriteLine(category.PadRight(maxCategoryLength) + $" { level,11/*length of 'Information'*/}: {Wrap(msg, 30, Log.WrapLimit)}");
					else
						Console.Out.WriteLine(msg);
				}
			}
			// only console write but not log to file
			if (Log.SuppressLog)
				return;

			buffers.Enqueue((msg, category, level));
			if (!writing && buffers.Count >= Log.BufferSize * 2 / 3)
				await BufferWrite();
		}

		private static string Wrap(string sentence, int indent, int limit)
		{
			string[] words = sentence.Split(' ');
			string indentStr = new(' ', indent);

			StringBuilder newSentence = new();
			StringBuilder line = new();
			for (int i = 0; i < words.Length; i++)
			{
				if (line.Length + words[i].Length > limit)
				{
					newSentence.Append(line);
					newSentence.Append(Environment.NewLine);
					newSentence.Append(indentStr);
					line.Clear();
				}
				line.Append(words[i]); line.Append(' ');
			}

			if (line.Length > 0)
				newSentence.Append(line);

			return newSentence.ToString();
		}

		private bool writing = false;

		private async Task BufferWrite()
		{
			while (writing)
				await Task.Delay(100).ConfigureAwait(true);

			writing = true;
			if (buffers.Count == 0)
			{
				return;
			}
			while (buffers.Count >= Log.BufferSize / 5)
			{
				var (msg, category, level) = buffers.Dequeue();
				await ActualWrite(msg, category, level).ConfigureAwait(true);
			}
			writing = false;
		}

		private static async Task ActualWrite(string msg, string category, LogLevel level)
		{
			void run()
			{
				switch (level)
				{
					case LogLevel.Trace:
						Trace.WriteLine($"Trace\tMethod {category}: {msg}");
						break;
					case LogLevel.Information:
						Trace.TraceInformation($"Method {category}: {msg}");
						break;
					case LogLevel.Warning:
						Trace.TraceWarning($"Method {category}: {msg}");
						break;
					case LogLevel.Error:
						Trace.TraceError($"Method {category}: {msg}");
						break;
					case LogLevel.Debug:
						Debug.WriteLine(msg, category);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(level), level, Resources.Parameter.InvalidValue);
				}
			};
			await Task.Run(run).ConfigureAwait(true);
		}
	}

	internal class LogTraceListener : TraceListener
	{
		private readonly Timer timer = new(3000);

		private readonly StreamWriter stream;

		~LogTraceListener()
		{
			this.timer.Dispose();
			this.stream?.Dispose();
		}

		internal LogTraceListener()
		{
			if (!File.Exists(Log.FilePath) || this.stream is null)
				this.stream = new StreamWriter(File.Create(Log.FilePath, 1 << 15, FileOptions.Asynchronous));
			timer.Enabled = true;
			timer.Elapsed += Timer_Elapsed;
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_ = GlobalLock(stream.FlushAsync);
		}

		public override void Write(string? message)
		{
			_ = GlobalLock(stream.WriteAsync, message);
		}

		public override void WriteLine(string? message)
		{
			_ = GlobalLock(stream.WriteLineAsync, message);
		}


		private static object? lockObj = null;

		private static async Task GlobalLock<T>(Func<T, Task> func, T parameter)
		{
			while (lockObj is not null)
			{
				await Task.Delay(100).ConfigureAwait(true);
			}
			//Debug.WriteLine($"Global lock of {func.Method.Name} start:" +
			//				$"\t{DateTime.Now.Second}:{DateTime.Now.Millisecond}");
			lockObj = new object();
			await func(parameter).ConfigureAwait(true);
			lockObj = null;
			//Debug.WriteLine($"Global lock of {func.Method.Name} end:" +
			//				$"\t{DateTime.Now.Second}:{DateTime.Now.Millisecond}");
		}

		private static async Task GlobalLock(Func<Task> func)
		{
			while (lockObj is not null)
			{
				await Task.Delay(100).ConfigureAwait(true);
			}
			//Debug.WriteLine($"Global lock of {func.Method.Name} start:" +
			//				$"\t{DateTime.Now.Second}:{DateTime.Now.Millisecond}");
			lockObj = new object();
			await func().ConfigureAwait(true);
			lockObj = null;
			//Debug.WriteLine($"Global lock of {func.Method.Name} end:" +
			//				$"\t{DateTime.Now.Second}:{DateTime.Now.Millisecond}");
		}
	}
	#endregion

	/// <summary>
	/// The static class provides basic write log file and output to console feature
	/// </summary>
	public static class Log
	{
		#region log settings
		/// <summary>
		/// Get or set whether the log file output should be suppressed
		/// </summary>
		public static bool SuppressLog {
			get => Settings.singletonSettings.LogSettings.Suppress;
			set => Settings.singletonSettings.LogSettings.Suppress = value;
		}

		/// <summary>
		/// Get or set the directory and file name of the log file
		/// </summary>
		public static string FilePath {
			get => Settings.singletonSettings.LogSettings.Path;
			set => Settings.singletonSettings.LogSettings.Path = value;
		}

		/// <summary>
		/// Get or set the buffer size used for output
		/// </summary>
		public static int BufferSize {
			get => Settings.singletonSettings.LogSettings.BufferSize;
			set => Settings.singletonSettings.LogSettings.BufferSize = value;
		}

		/// <summary>
		/// Get or set the maximum width (in characters) of a printed line
		/// </summary>
		public static int WrapLimit {
			get => Settings.singletonSettings.LogSettings.WrapLimit;
			set => Settings.singletonSettings.LogSettings.WrapLimit = value;
		}

		/// <summary>
		/// Get or set the <see cref="LogLevel"/>s to print
		/// </summary>
		public static IReadOnlyList<LogLevel> PrintLevels {
			get => Settings.singletonSettings.LogSettings.PrintLevels;
			set => Settings.singletonSettings.LogSettings.PrintLevels = value.ToArray();
		}

		/// <summary>
		/// Get or set the <see cref="LogLevel"/>s to buffer
		/// </summary>
		public static IReadOnlyList<LogLevel> BufferLevels {
			get => Settings.singletonSettings.LogSettings.BufferLevels;
			set => Settings.singletonSettings.LogSettings.BufferLevels = value.ToArray();
		}
		#endregion

		#region singleton logger
		private static readonly Logger logger = new();

		static Log()
		{
			if (SuppressLog)
			{
				return;
			}
#if DEBUG
			if (!PrintLevels.Contains(LogLevel.Debug))
				PrintLevels = PrintLevels.Append(LogLevel.Debug).ToArray();
#endif
			// configure the System.Diagnostics.Trace
			var listener = new LogTraceListener();
			if (!Trace.Listeners.Contains(listener))
			{
				Trace.Listeners.Clear();
				Trace.Listeners.Add(listener);
			}
		}
		#endregion

		/// <summary>
		/// Write the message of certain category and message level.
		/// </summary>
		/// <param name="msg">message to write</param>
		/// <param name="category">if <paramref name="category"/> is empty or null, the calling method name will be filled; if <paramref name="category"/> is an empty string, the prefix will not be printed to console</param>
		/// <param name="level">message log level</param>
		public static async void Write(string msg, [CallerMemberName] string? category = null, LogLevel level = LogLevel.Information)
		{
			await logger.Write(msg, category, level);
		}
	}
}
