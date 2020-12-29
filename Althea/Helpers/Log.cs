using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;


namespace Althea.Log
{
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

	/// <summary>
	/// The static class provides basic write log file and output to console feature
	/// </summary>
	public static class Log
	{
		// TODO: add destroyer
		static Log()
		{
			dynamic settings = JsonSerializer.Deserialize<Log>(File.ReadAllText("Althea.json"));

			LogPath = settings.Trace.LogPath + LogPath;

			WrapLimit = settings.Trace.WrapLimit;

			BufferSize = settings.Trace.BufferSize;
			buffers = new Queue<(string msg, string category, LogLevel level)>(BufferSize);

			var levels = new List<LogLevel>();
			if ((bool)settings.Trace.PrintError) levels.Add(LogLevel.Error);
			if ((bool)settings.Trace.PrintWarning) levels.Add(LogLevel.Warning);
			if ((bool)settings.Trace.PrintInformation) levels.Add(LogLevel.Information);
			if ((bool)settings.Trace.PrintTrace) levels.Add(LogLevel.Trace);
#if DEBUG
			levels.Add(LogLevel.Debug);
#endif
			OutputLevel = levels.ToArray();
			levels.Clear();

			if ((bool)settings.Trace.SuppressLog)
			{
				SuppressLog = true;
				return;
			}

			if ((bool)settings.Trace.BufferError) levels.Add(LogLevel.Error);
			if ((bool)settings.Trace.BufferWarning) levels.Add(LogLevel.Warning);
			if ((bool)settings.Trace.BufferInformation) levels.Add(LogLevel.Information);
			if ((bool)settings.Trace.BufferTrace) levels.Add(LogLevel.Trace);
			BufferLevel = levels.ToArray();

			var listener = new LogTraceListener();
			if (!Trace.Listeners.Contains(listener))
			{
				Trace.Listeners.Clear();
				Trace.Listeners.Add(listener);
			}
		}

		/// <summary>
		/// Whether the log file output should be suppressed
		/// </summary>
		public static bool SuppressLog { get; set; } = false;

		/// <summary>
		/// The directory and file name of the log file
		/// </summary>
		public static string LogPath { get; } = "Althea.log";

		/// <summary>
		/// Buffer size used for output
		/// </summary>
		public static int BufferSize { get; } = 1024;

		/// <summary>
		/// The maximum width (in characters) of a printed line
		/// </summary>
		public static int WrapLimit { get; set; } = 125;

		private static ICollection<LogLevel> OutputLevel { get; } = new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error };

		private static ICollection<LogLevel> BufferLevel { get; } = new[] { LogLevel.Trace };

		/// <summary>
		/// Time interval between two Lanczos information level output
		/// </summary>
		internal static TimeSpan LanczosInfoInterval { private set; get; } = TimeSpan.Parse("0:0:10.0", Resource.Culture);

		private static readonly Queue<(string msg, string category, LogLevel level)> buffers =
							new Queue<(string msg, string category, LogLevel level)>(BufferSize);

		private static int maxCategoryLength = 10;

		/// <summary>
		/// Write the message of certain category and message level.
		/// </summary>
		/// <param name="msg">message to write</param>
		/// <param name="category">if <paramref name="category"/> is empty or null, the calling method name will be filled; if <paramref name="category"/> is an empty string, the prefix will not be printed to console</param>
		/// <param name="level">message log level</param>
		public static async void Write(string msg, [CallerMemberName] string category = null, LogLevel level = LogLevel.Information)
		{
			bool showHeader = true;
			if (string.IsNullOrEmpty(category))
			{
				if (category != null && category.Length == 0)
					showHeader = false;
				else
				{
					// Get calling method name
					StackTrace stackTrace = new StackTrace();
					category = stackTrace.GetFrame(1).GetMethod().Name;
				}
			}
			if (!BufferLevel.Contains(level) && OutputLevel.Contains(level))
			{
				if (showHeader)
					maxCategoryLength = Math.Max(maxCategoryLength, category.Length);
				if (level == LogLevel.Error)
				{
					if (showHeader)
						Console.Error.WriteLine(category.PadRight(maxCategoryLength) + $" {level,11/*length of 'Information'*/}: {msg.Wrap(30, WrapLimit)}");
					else
						Console.Error.WriteLine(msg);
				}
				else
				{
					if (showHeader)
						Console.Out.WriteLine(category.PadRight(maxCategoryLength) + $" { level,11/*length of 'Information'*/}: {msg.Wrap(30, WrapLimit)}");
					else
						Console.Out.WriteLine(msg);
				}
			}
			// only console write but not log to file
			if (SuppressLog) return;

			buffers.Enqueue((msg, category, level));
			if (!writing && buffers.Count >= BufferSize * 2 / 3)
				await BufferWrite();
		}

		private static string Wrap(this string sentence, int indent, int limit)
		{
			string[] words = sentence.Split(' ');
			string indentStr = new string(' ', indent);

			StringBuilder newSentence = new StringBuilder();
			StringBuilder line = new StringBuilder();
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

		private static bool writing = false;

		private static async Task BufferWrite()
		{
			while (writing)
				await Task.Delay(100).ConfigureAwait(true);

			writing = true;
			if (buffers.Count == 0)
			{
				return;
			}
			while (buffers.Count >= BufferSize / 5)
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
						Trace.TraceInformation("Method {0}: {1}", category, msg);
						break;
					case LogLevel.Warning:
						Trace.TraceWarning("Method {0}: {1}", category, msg);
						break;
					case LogLevel.Error:
						Trace.TraceError("Method {0}: {1}", category, msg);
						break;
					case LogLevel.Debug:
						Debug.WriteLine(msg, category);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(level));
				}
			};
			await Task.Run(run).ConfigureAwait(true);
		}


		private static object lockObj = null;

		private static async Task GlobalLock<T>(this Func<T, Task> func, T parameter)
		{
			while (lockObj != null)
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

		private static async Task GlobalLock(this Func<Task> func)
		{
			while (lockObj != null)
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

		private class LogTraceListener : TraceListener
		{
			private readonly Timer timer = new Timer(3000);

			private readonly StreamWriter stream = null;

			~LogTraceListener()
			{
				this.timer.Dispose();
				this.stream?.Dispose();
			}

			internal LogTraceListener()
			{
				if (!File.Exists(LogPath) || this.stream is null)
					this.stream = new StreamWriter(File.Create(LogPath, 1 << 15, FileOptions.Asynchronous));
				timer.Enabled = true;
				timer.Elapsed += Timer_Elapsed;
			}

			private void Timer_Elapsed(object sender, ElapsedEventArgs e)
			{
				_ = GlobalLock(stream.FlushAsync);
			}

			public override void Write(string message)
			{
				_ = GlobalLock(stream.WriteAsync, message);
			}

			public override void WriteLine(string message)
			{
				_ = GlobalLock(stream.WriteLineAsync, message);
			}
		}
	}
}
