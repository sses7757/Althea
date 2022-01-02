using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;


namespace Althea.Helpers
{
	#region setting classes
	/// <summary>
	/// The flag for logging levels
	/// </summary>
	[Flags]
	public enum LogLevel
	{
		/// <summary>
		/// The most detailed level of logging message
		/// </summary>
		Trace = 1 << 0,
		/// <summary>
		/// The normal information logging message
		/// </summary>
		Information = 1 << 1,
		/// <summary>
		/// The warning message
		/// </summary>
		Warning = 1 << 2,
		/// <summary>
		/// The error message
		/// </summary>
		Error = 1 << 3,
		/// <summary>
		/// The debug message
		/// </summary>
		Debug = 1 << 4,
	}

	// for JSON serialization
	internal record LogSettings
	{
		public bool Suppress { get; set; }
		public int BufferSize { get; set; }
		public int WrapLimit { get; set; }
		public string Path { get; set; }

		public LogLevel PrintLevels { get; set; }
		public LogLevel BufferLevels { get; set; }

		internal LogSettings()
		{
			Suppress = false; BufferSize = 1024; WrapLimit = 125; Path = "Althea.log";
			PrintLevels = LogLevel.Error | LogLevel.Warning;
			BufferLevels = LogLevel.Trace | LogLevel.Debug;
		}

		[System.Text.Json.Serialization.JsonConstructor]
		internal LogSettings(bool suppress, int bufferSize, int wrapLimit, string path, string[] printLevels, string[] bufferLevels)
		{
			Suppress = suppress; BufferSize = bufferSize; WrapLimit = wrapLimit; Path = path;
			PrintLevels = 0;
			foreach (var p in printLevels)
			{
				PrintLevels |= Enum.Parse<LogLevel>(p);
			}
			BufferLevels = 0;
			foreach (var b in bufferLevels)
			{
				BufferLevels |= Enum.Parse<LogLevel>(b);
			}
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

		private readonly Queue<(string msg, string category, LogLevel level)> buffers = new(Log.BufferSize);

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
			if ((Log.PrintLevels & level) != 0)
			{
				if (showHeader)
					maxCategoryLength = Math.Max(maxCategoryLength, category.Length);
				if (level == LogLevel.Error)
				{
					if (showHeader)
						Console.Error.WriteLine(category.PadRight(maxCategoryLength) + $" {level,11/*length('Information')*/}: {Wrap(msg, 30, Log.WrapLimit)}");
					else
						Console.Error.WriteLine(msg);
				}
				else
				{
					if (showHeader)
						Console.Out.WriteLine(category.PadRight(maxCategoryLength) + $" { level,11/*length('Information')*/}: {Wrap(msg, 30, Log.WrapLimit)}");
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
		private readonly System.Timers.Timer timer = new(3000);

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

		private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
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
		private const LogLevel ALL_LEVELS = LogLevel.Trace | LogLevel.Information | LogLevel.Warning | LogLevel.Error | LogLevel.Debug;

		/// <summary>
		/// Check whether the given <see cref="LogLevel"/> <paramref name="flags"/> is a valid one
		/// </summary>
		/// <param name="flags">The <see cref="LogLevel"/> flags to check</param>
		/// <returns>Whether <paramref name="flags"/> is a valid one or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsValid(this LogLevel flags)
		{
			return (flags | ALL_LEVELS) == ALL_LEVELS;
		}

		/// <summary>
		/// Get or set whether the log file output should be suppressed
		/// </summary>
		public static bool SuppressLog {
			get => Settings.settings.LogSettings.Suppress;
			set {
				Settings.settings.LogSettings.Suppress = value;
				Settings.UpdateSingletonSetting();
			}
		}

		/// <summary>
		/// Get or set the directory and file name of the log file
		/// </summary>
		public static string FilePath {
			get => Settings.settings.LogSettings.Path;
			set {
				Settings.settings.LogSettings.Path = value;
				Settings.UpdateSingletonSetting();
			}
}

		/// <summary>
		/// Get or set the buffer size used for output
		/// </summary>
		public static int BufferSize {
			get => Settings.settings.LogSettings.BufferSize;
			set {
				Settings.settings.LogSettings.BufferSize = value;
				Settings.UpdateSingletonSetting();
			}
		}

		/// <summary>
		/// Get or set the maximum width (in characters) of a printed line
		/// </summary>
		public static int WrapLimit {
			get => Settings.settings.LogSettings.WrapLimit;
			set {
				Settings.settings.LogSettings.WrapLimit = value;
				Settings.UpdateSingletonSetting();
			}
		}

		/// <summary>
		/// Get or set the <see cref="LogLevel"/>s to print to console immediately
		/// </summary>
		public static LogLevel PrintLevels {
			get => Settings.settings.LogSettings.PrintLevels;
			set {
				if (!value.IsValid())
					throw new ArgumentOutOfRangeException(nameof(value), value, Resources.Parameter.InvalidValue);
				Settings.settings.LogSettings.PrintLevels = value;
				Settings.UpdateSingletonSetting();
			}
		}

		/// <summary>
		/// Get or set the <see cref="LogLevel"/>s to buffer
		/// </summary>
		public static LogLevel BufferLevels {
			get => Settings.settings.LogSettings.BufferLevels;
			set {
				if (!value.IsValid())
					throw new ArgumentOutOfRangeException(nameof(value), value, Resources.Parameter.InvalidValue);
				Settings.settings.LogSettings.BufferLevels = value;
				Settings.UpdateSingletonSetting();
			}
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
			PrintLevels |= LogLevel.Debug;
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
		/// Asynchronously write the message of certain category and message level.
		/// </summary>
		/// <param name="msg">The message to write</param>
		/// <param name="category">If <paramref name="category"/> is empty or null, the calling method name will be filled; if <paramref name="category"/> is an empty string, the prefix will not be printed to console</param>
		/// <param name="level">The message <see cref="LogLevel"/>, must be atomic</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="level"/> is of invalid value</exception>
		public static async void Write(string msg, [CallerMemberName] string? category = null, LogLevel level = LogLevel.Information)
		{
			if (!level.IsValid() || !((int)level).IsPowerOfTwo())
				throw new ArgumentOutOfRangeException(nameof(level), level, Resources.Parameter.InvalidValue);
			await logger.Write(msg, category, level);
		}
	}
}
