using System;
using System.Reflection;
using System.Diagnostics;


namespace Althea
{
	internal static class CudaStatusUtilities
	{
		internal static void Check(this CudaError err, string name)
		{
			if (err != CudaError.Success)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this CudaError err)
		{
			if (err != CudaError.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		internal static void Check(this Blas.Cuda.Status err, string name)
		{
			if (err != Blas.Cuda.Status.Success)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this Blas.Cuda.Status err)
		{
			if (err != Blas.Cuda.Status.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		internal static void Check(this Rng.Cuda.Status err, string name)
		{
			if (err != Rng.Cuda.Status.Success)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this Rng.Cuda.Status err)
		{
			if (err != Rng.Cuda.Status.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		internal static void Check(this Rng.Mkl.Status err, string name)
		{
			if (err != Rng.Mkl.Status.VSL_STATUS_OK)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this Rng.Mkl.Status err)
		{
			if (err != Rng.Mkl.Status.VSL_STATUS_OK)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		internal static void Check(this Solver.Cuda.Status err, string name)
		{
			if (err != Solver.Cuda.Status.Success)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this Solver.Cuda.Status err)
		{
			if (err != Solver.Cuda.Status.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		internal static void Check(this SparseBlas.Cuda.Status err, string name)
		{
			if (err != SparseBlas.Cuda.Status.Success)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this SparseBlas.Cuda.Status err)
		{
			if (err != SparseBlas.Cuda.Status.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		internal static void Check(this Tensor.Cuda.Status err, string name)
		{
			if (err != Tensor.Cuda.Status.Success)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this Tensor.Cuda.Status err)
		{
			if (err != Tensor.Cuda.Status.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		internal static void Check(this Storage.ReadFileEnum err, string name)
		{
			if (err != Storage.ReadFileEnum.Success)
			{
				throw new StatusException(err, name);
			}
		}
		internal static void Check(this Storage.ReadFileEnum err)
		{
			if (err != Storage.ReadFileEnum.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}
	}

	/// <summary>
	/// The exception that wraps CUDA / MKL errors: <see cref="CudaError"/>, <see cref="Blas.Cuda.Status"/>, <see cref="Rng.Cuda.Status"/>, <see cref="Rng.Mkl.Status"/>, <see cref="Solver.Cuda.Status"/>.
	/// </summary>
	public sealed class StatusException : Exception
	{
		/// <summary>
		/// An empty status exception
		/// </summary>
		public StatusException()
		{
		}

		/// <summary>
		/// An empty status exception
		/// </summary>
		/// <param name="message"></param>
		public StatusException(string message) : base(message)
		{
		}

		/// <summary>
		/// An empty status exception
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public StatusException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by a <see cref="Enum"/>.
		/// </summary>
		/// <param name="error">The error enum</param>
		/// <param name="methodName">The method name, can be null or empty for not showing it</param>
		/// <param name="trace">customize stack trace</param>
		/// <exception cref="ArgumentException">if <paramref name="error"/> does not represent an error status</exception>
		public StatusException(Enum error, string methodName, StackTrace trace = null)
		{
			if (error is null)
				throw new ArgumentNullException(nameof(error));
			if ((int)(object)error == 0)
				throw new ArgumentException(Resource.NotErrors);
			this.stackTrace = trace;
			this.errorCode = (int)(object)error;
			this.errorString = error.ToString();
			this.methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
			this.typeString = error.GetType().FullName;
		}

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by a <see cref="Enum"/>.
		/// </summary>
		/// <param name="error">The error enum</param>
		/// <param name="trace">customize <see cref="System.Diagnostics.StackTrace"/></param>
		/// <exception cref="ArgumentException">if <paramref name="error"/> does not represent an error status</exception>
		public StatusException(Enum error, StackTrace trace = null)
		{
			if (error is null)
				throw new ArgumentNullException(nameof(error));
			if ((int)(object)error == 0)
				throw new ArgumentException(Resource.NotErrors);
			var method = trace?.GetFrame(1).GetMethod();
			if (trace is null)
				trace = new StackTrace();
			method ??= trace.GetFrame(1).GetMethod();

			this.stackTrace = trace;
			this.errorCode = (int)(object)error;
			this.errorString = error.ToString();
			this.methodName = method.DeclaringType.FullName + "." + method.Name;
			this.typeString = error.GetType().FullName;
		}

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by a <see cref="Enum"/> while the method is the calling method obtained by <see cref="StackTrace.GetFrame(int)"/>.
		/// </summary>
		/// <param name="error">The error enum</param>
		/// <exception cref="ArgumentException">if <paramref name="error"/> does not represent an error status</exception>
		public StatusException(Enum error)
		{
			if (error is null)
				throw new ArgumentNullException(nameof(error));
			if ((int)(object)error == 0)
				throw new ArgumentException(Resource.NotErrors);
			var method = new StackTrace().GetFrame(1).GetMethod();

			this.errorCode = (int)(object)error;
			this.errorString = error.ToString();
			this.methodName = method.DeclaringType.FullName + "." + method.Name;
			this.typeString = error.GetType().FullName;
		}

		/// <summary>
		/// Initialize the <see cref="StatusException"/> by two <see cref="Enum"/>s.
		/// </summary>
		/// <param name="error1">The error enum 1</param>
		/// <param name="error2">The error enum 2</param>
		/// <param name="method">The <see cref="MethodBase"/> to indicate the current method</param>
		/// <exception cref="ArgumentException">if <paramref name="error1"/> or <paramref name="error2"/> does not represent an error status</exception>
		public StatusException(MethodBase method, Enum error1, Enum error2)
		{
			if (error1 is null)
				throw new ArgumentNullException(nameof(error1));
			if (error2 is null)
				throw new ArgumentNullException(nameof(error2));
			if (method is null)
				throw new ArgumentNullException(nameof(method));
			if ((int)(object)error1 != 0 && (int)(object)error2 != 0)
				throw new ArgumentException(Resource.NotErrors);
			if ((int)(object)error1 == 0)
			{
				this.errorCode = (int)(object)error2;
				this.errorString = error2.ToString();
				this.methodName = method.DeclaringType.FullName + "." + method.Name;
				this.typeString = error2.GetType().FullName;
			}
			else if ((int)(object)error1 == 0)
			{
				this.errorCode = (int)(object)error1;
				this.errorString = error1.ToString();
				this.methodName = method.DeclaringType.FullName + "." + method.Name;
				this.typeString = error1.GetType().FullName;
			}
			else
			{
				this.errorCode = (int)(object)error1;
				this.errorString = error1.ToString();
				this.errorCodeSupplement = (int)(object)error2;
				this.errorStringSupplement = error2.ToString();
				this.methodName = method.DeclaringType.FullName + "." + method.Name;
				this.typeString = error1.GetType().FullName;
			}
		}

		private readonly StackTrace stackTrace = null;

		private readonly string typeString;

		private readonly int errorCode;

		private readonly string errorString;

		private readonly int errorCodeSupplement;

		private readonly string errorStringSupplement;

		private readonly string methodName;

		/// <summary>
		/// Return the message of this exception
		/// </summary>
		public override string Message {
			get {
				string end = string.IsNullOrWhiteSpace(methodName) ? "." : $" at method `{methodName}`.";
				if (string.IsNullOrWhiteSpace(errorStringSupplement))
				{
					return $"Status error occurred with code '{errorCode}' and string '{errorString}' of '{typeString}'" + end;
				}
				else
				{
					return $"Status errors occurred with (code = '{errorCode}', string = '{errorString}') and (code = '{errorCodeSupplement}', string = '{errorStringSupplement}') of '{typeString}'" + end;
				}
			}
		}

		/// <summary>
		/// Return the stack trace
		/// </summary>
		public override string StackTrace {
			get {
				if (this.stackTrace is null)
					return base.StackTrace;
				else
					return this.stackTrace.ToString();
			}
		}
	}

	#region PostSharp things obsoleted
	/////// <summary>
	/////// Function decorator on CUDA native / custom kernel methods that
	/////// <list type="bullet">
	/////// <item>
	/////// <description>checks all CUDA error types and throws them when error actually happens</description>
	/////// </item>
	/////// <item>
	/////// <description>print memory difference between the start and the end of the method if necessary</description>
	/////// </item>
	/////// <item>
	/////// <description>print the calling of the method if necessary</description>
	/////// </item>
	/////// </list>
	/////// </summary>
	/////// <remarks>This is actually an Aspect Oriented Programming (AOP) design principle that helps to separate concerns of computation,  checking and monitoring</remarks>
	////[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	////public class NativeMethodBoundaryAttribute : OnMethodBoundaryAspect
	////{
	////	private Type ReturnType;

	////	private long FreeMemAtBegin;

	////	private int FindErrorType(Type type)
	////	{
	////		for (int i = 0; i < Global.StatusTypes.Length; ++i)
	////		{
	////			if (type.IsAssignableFrom(Global.StatusTypes[i]))
	////				return i;
	////		}
	////		return -1;
	////	}

	////	/// <summary>
	////	/// Override <see cref="MethodLevelAspect.CompileTimeValidate(MethodBase)"/>
	////	/// </summary>
	////	/// <param name="method"></param>
	////	public override bool CompileTimeValidate(MethodBase method)
	////	{
	////		if (method is null)
	////			throw new ArgumentNullException(nameof(method));
	////		base.CompileTimeValidate(method);
	////		if (FindErrorType((method as MethodInfo).ReturnType) < 0)
	////		{
	////			Message.Write(MessageLocation.Unknown, SeverityType.Error, "DMRG_ERROR_CheckCudaError", "Can not apply [CheckCudaError] to method {0} because it does not return a type that is assignable from any CudaError.", method.Name);
	////			return false;
	////		}
	////		return true;
	////	}

	////	/// <summary>
	////	/// Override <see cref="MethodLevelAspect.CompileTimeInitialize(MethodBase, AspectInfo)"/>
	////	/// </summary>
	////	/// <param name="method"></param>
	////	/// <param name="aspectInfo"></param>
	////	public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
	////	{
	////		if (method is null)
	////			throw new ArgumentNullException(nameof(method));
	////		base.CompileTimeInitialize(method, aspectInfo);
	////		this.ReturnType = Global.StatusTypes[FindErrorType((method as MethodInfo).ReturnType)];
	////	}

	////	/// <summary>
	////	/// Override <see cref="OnMethodBoundaryAspect.OnEntry(MethodExecutionArgs)"/>
	////	/// </summary>
	////	/// <param name="args"></param>
	////	public override void OnEntry(MethodExecutionArgs args)
	////	{
	////		if (args is null)
	////			throw new ArgumentNullException(nameof(args));
	////		base.OnEntry(args);
	////		if (Log.TrackMemory)
	////			this.FreeMemAtBegin = CudaRuntime.API.AvailableMemory;
	////		if (Log.TrackMethod)
	////			Log.Write($"starts", category: args.Method.Name, level: MsgLevel.Trace);
	////	}

	////	/// <summary>
	////	/// Override <see cref="OnMethodBoundaryAspect.OnExit(MethodExecutionArgs)"/>
	////	/// </summary>
	////	/// <param name="args"></param>
	////	public override void OnExit(MethodExecutionArgs args)
	////	{
	////		if (args is null)
	////			throw new ArgumentNullException(nameof(args));
	////		base.OnExit(args);
	////		if (Log.TrackMemory)
	////		{
	////			long diff = CudaRuntime.API.AvailableMemory - this.FreeMemAtBegin;
	////			if (diff > 0)
	////				Log.Write($"used {(diff / 1024.0 / 1024.0):N2}MiB memory", category: args.Method.Name, level: MsgLevel.Trace);
	////			else
	////				Log.Write($"freed {(diff / 1024.0 / 1024.0):N2}MiB memory", category: args.Method.Name, level: MsgLevel.Trace);
	////		}
	////		if (Log.TrackMethod)
	////			Log.Write($"finished", category: args.Method.Name, level: MsgLevel.Trace);
	////	}

	////	/// <summary>
	////	/// Override <see cref="OnMethodBoundaryAspect.OnSuccess(MethodExecutionArgs)"/>
	////	/// </summary>
	////	/// <param name="args"></param>
	////	/// <exception cref="CudaStatusException">if <see cref="Global.CheckCudaErrors"/> is true and the underlying method's return value is not a success status</exception>
	////	public override void OnSuccess(MethodExecutionArgs args)
	////	{
	////		if (args is null)
	////			throw new ArgumentNullException(nameof(args));
	////		base.OnSuccess(args);
	////		if (Global.CheckCudaErrors)
	////		{
	////			// here, err is a unknown Enum type and hence can be casted into int
	////			var err = Convert.ChangeType(args.ReturnValue, this.ReturnType, Resource.Culture);
	////			if ((int)err != 0)
	////			{
	////				// since the ToString() method of object is virtual, the actual ToString() that invoked is the Enum.ToString()
	////				throw new CudaStatusException((Enum)err, new StackTrace(skipFrames: 1));
	////			}
	////		}
	////	}
	////}
	#endregion
}
