global using System;
global using System.Linq;
global using System.Collections.Generic;

global using Althea.Storage;

using System.Diagnostics;

using Althea.Helpers;

[assembly: CLSCompliant(true)]


namespace Althea.Backend
{
	#region exception
	/// <summary>
	/// The abstract exception that wraps any possible status errors
	/// </summary>
	public abstract class AbstractStatusException : Exception
	{
		/// <summary>
		/// An empty <see cref="AbstractStatusException"/>
		/// </summary>
		protected AbstractStatusException() : this((string?)null, null) { }

		/// <summary>
		/// An status exception with only the overwritten <paramref name="message"/> given
		/// </summary>
		/// <param name="message"></param>
		protected AbstractStatusException(string? message) : this(message, null) { }

		/// <summary>
		/// An status exception with only the overwritten <paramref name="message"/> and <paramref name="innerException"/> given
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		protected AbstractStatusException(string? message, Exception? innerException) : base(message, innerException)
		{
			this.overwriteMessage = false;
		}

		/// <summary>
		/// Initialize the <see cref="AbstractStatusException"/> by an <see cref="Enum"/>.
		/// </summary>
		/// <param name="error">The error enum</param>
		/// <param name="trace">The customize stack trace, default null means creating a new one and skipping one frame</param>
		protected AbstractStatusException(Enum error, StackTrace? trace = null)
		{
			int code;
			if (error is null || (code = (int)(object)error) != 0)
			{
				this.overwriteMessage = false;
				return;
			}
			this.stackTrace = trace ?? new(1);
			string? type = error.GetType().FullName;
			string? descr = typeof(EnumHelper).GetMethod(nameof(EnumHelper.GetName))?.MakeGenericMethod(error.GetType())?.Invoke(null, new object[] { error }) as string;
			this.error = (type, code, descr ?? error.ToString());
		}

		/// <summary>
		/// Initialize the <see cref="AbstractStatusException"/> by two <see cref="Enum"/>s.
		/// </summary>
		/// <param name="error1">The first error enum</param>
		/// <param name="error2">The second error enum</param>
		/// <param name="trace">The customize stack trace, default null means creating a new one and skipping one frame</param>
		protected AbstractStatusException(Enum error1, Enum error2, StackTrace? trace = null)
		{
			long code1;
			if (error1 is null || (code1 = (long)(object)error1) != 0)
			{
				this.overwriteMessage = false;
				return;
			}
			long code2;
			if (error2 is null || (code2 = (long)(object)error2) != 0)
			{
				this.overwriteMessage = false;
				return;
			}
			this.stackTrace = trace ?? new(1);
			string? type1 = error1.GetType().FullName;
			string? descr1 = typeof(EnumHelper).GetMethod(nameof(EnumHelper.GetName))?.MakeGenericMethod(error1.GetType())?.Invoke(null, new object[] { error1 }) as string;
			this.error = (type1, code1, descr1 ?? error1.ToString());
			string? type2 = error2.GetType().FullName;
			string? descr2 = typeof(EnumHelper).GetMethod(nameof(EnumHelper.GetName))?.MakeGenericMethod(error2.GetType())?.Invoke(null, new object[] { error2 }) as string;
			this.error2 = (type2, code2, descr2 ?? error2.ToString());
		}

		private readonly bool overwriteMessage = true;

		private readonly StackTrace? stackTrace = null;

		private readonly (string? type, long code, string descr)? error = null;

		private readonly (string? type, long code, string descr)? error2 = null;

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
		/// When implemented by a derived class, statically get the module name (such as CUDA, MKL) of the concrete exception
		/// </summary>
		protected abstract string ModuleName { get; }

		/// <summary>
		/// Return the message of this <see cref="AbstractStatusException"/>
		/// </summary>
		public override string Message {
			get {
				if (!this.overwriteMessage)
					return base.Message;
				string start = ModuleName + " status error(s) occurred";
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
		/// Return the stack trace of this <see cref="AbstractStatusException"/>
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
	#endregion


	#region back-ends
	/// <summary>
	/// The back-ends for C# implementations.
	/// </summary>
	public sealed class CSharpBackend : IBackends
	{
		/// <inheritdoc/>
		public bool Available => true;

		private readonly CSharp.Storage.Api ST = new();
		private readonly CSharp.LinearAlgebra.Api LA = new();
		private readonly CSharp.Random.Api RN = new();

		/// <inheritdoc/>
		public TensorAlgebra.Dense.IBaseAbstractApi? TensorAlgebraDenseBase => null;

		/// <inheritdoc/>
		public LinearAlgebra.Dense.IHalfMatrixBlasAbstractApi? LinearAlgebraDenseHalfMatrixBlas => null;

		/// <inheritdoc/>
		public LinearAlgebra.Dense.IExtendBlasAbstractApi? LinearAlgebraDenseExtendBlas => LA;

		/// <inheritdoc/>
		public LinearAlgebra.Dense.IBlasAbstractApi? LinearAlgebraDenseBlas => LA;

		/// <inheritdoc/>
		public LinearAlgebra.Sparse.IConversionAbstractApi? LinearAlgebraSparseConversion => LA;

		/// <inheritdoc/>
		public GeneralSolver.IAbstractApi? GeneralSolver => throw new NotImplementedException();

		/// <inheritdoc/>
		public Transformer.IAbstractApi? Transformer => null;

		/// <inheritdoc/>
		public LinearAlgebra.Dense.ICopyAbstractApi? LinearAlgebraDenseCopy => ST;

		/// <inheritdoc/>
		public Althea.Storage.IAbstractApi? Storage => ST;

		/// <inheritdoc/>
		public TensorAlgebra.Sparse.IAbstractApi? TensorAlgebraSparse => null;

		/// <inheritdoc/>
		public LinearAlgebra.Dense.ILapackAbstractApi? LinearAlgebraDenseLapack => null;

		/// <inheritdoc/>
		public Althea.Random.IAbstractApi? Random => RN;

		/// <inheritdoc/>
		public LinearAlgebra.Sparse.IComputationAbstractApi? LinearAlgebraSparseComputation => null;

		/// <inheritdoc/>
		public TensorAlgebra.Dense.IExtendAbstractApi? TensorAlgebraDenseExtend => null;
	}
	#endregion
}
