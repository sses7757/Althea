using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Althea.Helpers;


namespace Althea.Backend.Cuda
{
	/// <summary>
	/// The exception that wraps CUDA errors: <see cref="CudaError"/>, <see cref="Cuda.Storage.CudaFileError"/>, ...
	/// </summary>
	public class StatusException : Exception
	{
		/// <summary>
		/// An empty <see cref="StatusException"/>
		/// </summary>
		public StatusException() : this((string?)null, null) { }

		/// <summary>
		/// An status exception with only the overwritten <paramref name="message"/> given
		/// </summary>
		/// <param name="message"></param>
		public StatusException(string? message): this(message, null) { }

		/// <summary>
		/// An status exception with only the overwritten <paramref name="message"/> and <paramref name="innerException"/> given
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public StatusException(string? message, Exception? innerException) : base(message, innerException)
		{
			this.overwriteMessage = false;
		}

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by an <see cref="Enum"/>.
		/// </summary>
		/// <param name="error">The error enum</param>
		/// <param name="trace">The customize stack trace, default null means creating a new one and skipping one frame</param>
		public StatusException(Enum error, StackTrace? trace = null)
		{
			int code;
			if (error is null || (code = (int)(object)error) != 0)
			{
				this.overwriteMessage = false;
				return;
			}
			this.stackTrace = trace ?? new(1);
			string? type = error.GetType().FullName;
			string descr = error.ToString();
			this.error = (type, code, descr);
		}

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by two <see cref="Enum"/>s.
		/// </summary>
		/// <param name="error1">The first error enum</param>
		/// <param name="error2">The second error enum</param>
		/// <param name="trace">The customize stack trace, default null means creating a new one and skipping one frame</param>
		public StatusException(Enum error1, Enum error2, StackTrace? trace = null)
		{
			int code1;
			if (error1 is null || (code1 = (int)(object)error1) != 0)
			{
				this.overwriteMessage = false;
				return;
			}
			int code2;
			if (error2 is null || (code2 = (int)(object)error2) != 0)
			{
				this.overwriteMessage = false;
				return;
			}
			this.stackTrace = trace ?? new(1);
			string? type1 = error1.GetType().FullName;
			string descr1 = error1.ToString();
			this.error = (type1, code1, descr1);
			string? type2 = error2.GetType().FullName;
			string descr2 = error2.ToString();
			this.error2 = (type2, code2, descr2);
		}

		private readonly bool overwriteMessage = true;

		private readonly StackTrace? stackTrace = null;

		private readonly (string? type, int code, string descr)? error = null;

		private readonly (string? type, int code, string descr)? error2 = null;

		private string? MethodString {
			get {
				var method = this.stackTrace?.GetFrame(0)?.GetMethod();
				if (method is null)
					return null;
				var type = method.DeclaringType?.GetGenericString();
				if (type is null)
					return null;
				return type + "." + method;
			}
		}

		/// <summary>
		/// Return the message of this <see cref="StatusException"/>
		/// </summary>
		public override string Message {
			get {
				if (!this.overwriteMessage)
					return base.Message;
				string start = "CUDA status error(s) occurred";
				string end = this.MethodString is null ? "." : $" at method '{this.MethodString}'.";
				if (this.error.HasValue)
				{
					start += $", {this.error.Value.type}[Code={this.error.Value.code}, Description=\"{this.error.Value.descr}\"]";
					if (this.error2.HasValue)
						start += $"{this.error2.Value.type}[Code={this.error2.Value.code}, Description=\"{this.error2.Value.descr}\"]";
					start += end;
				}
				return start;
			}
		}

		/// <summary>
		/// Return the stack trace of this <see cref="StatusException"/>
		/// </summary>
		public override string? StackTrace {
			get {
				if (this.stackTrace is null)
					return base.StackTrace;
				else
					return this.stackTrace.ToString();
			}
		}
	}


	/// <summary>
	/// The static class containing extension methods for <see cref="StatusException"/> and <see cref="CudaError"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaError"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaError"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this CudaError err)
		{
			if (err != CudaError.Success)
			{
				if (err == CudaError.ErrorOutOfMemory)
					throw new OutOfMemoryException();
				throw new StatusException(err, new StackTrace(0));
			}
		}
	}
}
