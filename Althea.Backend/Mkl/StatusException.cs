using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace Althea.Backend.Mkl
{
	/// <summary>
	/// The exception that wraps MKL errors
	/// </summary>
	public class StatusException : AbstractStatusException
	{
		/// <summary>
		/// An empty <see cref="StatusException"/>
		/// </summary>
		public StatusException() : base() { }

		/// <summary>
		/// An status exception with only the overwritten <paramref name="message"/> given
		/// </summary>
		/// <param name="message"></param>
		public StatusException(string? message) : base(message) { }

		/// <summary>
		/// An status exception with only the overwritten <paramref name="message"/> and <paramref name="innerException"/> given
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public StatusException(string? message, Exception? innerException) : base(message, innerException) { }

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by an <see cref="Enum"/>.
		/// </summary>
		/// <param name="error">The error enum</param>
		/// <param name="trace">The customize stack trace, default null means creating a new one and skipping one frame</param>
		public StatusException(Enum error, StackTrace? trace = null) : base(error, trace) { }

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by two <see cref="Enum"/>s.
		/// </summary>
		/// <param name="error1">The first error enum</param>
		/// <param name="error2">The second error enum</param>
		/// <param name="trace">The customize stack trace, default null means creating a new one and skipping one frame</param>
		public StatusException(Enum error1, Enum error2, StackTrace? trace = null) : base(error1, error2, trace) { }

		/// <summary>
		/// statically get the module name (MKL) of the this exception
		/// </summary>
		protected override string ModuleName {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => "MKL";
		}
	}


	/// <summary>
	/// The static class for <see cref="StatusException"/>
	/// </summary>
	public static partial class StatusExtension
	{
		
	}
}
