using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Random;


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
			string descr = error.ToString();
			this.error = (type, code, descr);
		}

		/// <summary>
		/// Initialize the <see cref="AbstractStatusException"/> by two <see cref="Enum"/>s.
		/// </summary>
		/// <param name="error1">The first error enum</param>
		/// <param name="error2">The second error enum</param>
		/// <param name="trace">The customize stack trace, default null means creating a new one and skipping one frame</param>
		protected AbstractStatusException(Enum error1, Enum error2, StackTrace? trace = null)
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


	#region abstract distributions
	/// <summary>
	/// The abstract class for one-dimensional distributions which contain the <see cref="Displacement"/> and <see cref="ScaleFactor"/> of type <typeparamref name="T"/> as the parameters, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	public abstract class DisplaceScaleDistribution<T> : OneDimensionalFloatTypedDistribution<T>, IEquatable<DisplaceScaleDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the displacement (<c>a</c>) of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		public T Displacement {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the scaling factor (<c>β</c>) of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		public T ScaleFactor {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard <see cref="DisplaceScaleDistribution{T}"/> with a=0, β=1, and random seed is not set
		/// </summary>
		protected DisplaceScaleDistribution() : base(null)
		{
			this.Displacement = Const<T>.Zero; this.ScaleFactor = Const<T>.One;
		}

		/// <summary>
		/// Create an <see cref="DisplaceScaleDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		protected DisplaceScaleDistribution(T displacement, T scaleFactor, long? seed = null) : base(seed)
		{
			this.Displacement = displacement; this.ScaleFactor = scaleFactor;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="DisplaceScaleDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="DisplaceScaleDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(DisplaceScaleDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Displacement.IsEqual(other.Displacement) && this.ScaleFactor.IsEqual(other.ScaleFactor) && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as DisplaceScaleDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="DisplaceScaleDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Displacement, this.ScaleFactor, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Displacement)}={this.Displacement}, {nameof(this.ScaleFactor)}={this.ScaleFactor}";
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions which contain the extract <see cref="ShapeFactor"/> and the parameters of <see cref="DisplaceScaleDistribution{T}"/> of type <typeparamref name="T"/> as the parameters, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	public abstract class ShapeDisplaceScaleDistribution<T> : DisplaceScaleDistribution<T>, IEquatable<ShapeDisplaceScaleDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the shaping factor (<c>α</c>) of this <see cref="ShapeDisplaceScaleDistribution{T}"/>
		/// </summary>
		public T ShapeFactor {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard <see cref="ShapeDisplaceScaleDistribution{T}"/> with a=0, α=1, β=1, and random seed is not set
		/// </summary>
		public ShapeDisplaceScaleDistribution() : base()
		{
			this.ShapeFactor = Const<T>.One;
		}

		/// <summary>
		/// Create an <see cref="ShapeDisplaceScaleDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="shapeFactor"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="shapeFactor">The given shape factor α</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public ShapeDisplaceScaleDistribution(T displacement, T shapeFactor, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed)
		{
			this.ShapeFactor = shapeFactor;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="ShapeDisplaceScaleDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="ShapeDisplaceScaleDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ShapeDisplaceScaleDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.ShapeFactor.IsEqual(other.ShapeFactor) && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as ShapeDisplaceScaleDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="ShapeDisplaceScaleDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="ShapeDisplaceScaleDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.ShapeFactor, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="ShapeDisplaceScaleDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.ShapeFactor)}={this.ShapeFactor}";
	}
	#endregion

	#region one dimensional floating point distributions
	// Ignore Spelling: \dfrac \ln \det \lt \alpha' \mbox
	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="OneDimensionalFloatTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Normal distribution PDF: $$p_{\mu,\sigma}(x)=\frac{1}{\sigma\sqrt{2\pi}}\exp{\left( -\frac{(x-\mu)^2}{2\sigma^2} \right)}$$
	public class NormalDistribution<T> : OneDimensionalFloatTypedDistribution<T>, IEquatable<NormalDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the mean value of this normal distribution
		/// </summary>
		public T Mean {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the standard deviation value of this normal distribution
		/// </summary>
		public T StandardDeviation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard normal distribution with mean = 0 and standard deviation = 1, and random seed is not set
		/// </summary>
		public NormalDistribution() : base(null)
		{
			this.Mean = Const<T>.Zero; this.StandardDeviation = Const<T>.One;
		}

		/// <summary>
		/// Create a normal distribution with given <paramref name="mean"/>, <paramref name="stddev"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean value</param>
		/// <param name="stddev">The given standard deviation</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public NormalDistribution(T mean, T stddev, long? seed = null) : base(seed)
		{
			this.StandardDeviation = stddev; this.Mean = mean;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="NormalDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="NormalDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(NormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.RandomSeed == other.RandomSeed && this.Mean.IsEqual(other.Mean) && this.StandardDeviation.IsEqual(other.StandardDeviation);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as NormalDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="NormalDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="NormalDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Mean, this.StandardDeviation, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="NormalDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Mean)}={this.Mean}, {nameof(this.StandardDeviation)}={this.StandardDeviation}";
	}

	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:$\chi^2$ distribution PDF: $$p_{v}(x)=\begin{cases}\dfrac{x^{(v-2)/2}e^{-x/2}}{2^{v/2}\Gamma(v/2)} & x \ge 0 \\ 0 & x \lt 0 \end{cases}$$
	public class ChiSquareDistribution<T> : OneDimensionalFloatTypedDistribution<T>, IEquatable<ChiSquareDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the degree of freedom of this χ² distribution
		/// </summary>
		public int DegreeOfFreedom {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a <see cref="ChiSquareDistribution{T}"/> distribution with given <paramref name="DoF"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="DoF">The given degree of freedom</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public ChiSquareDistribution(int DoF, long? seed = null) : base(seed)
		{
			this.DegreeOfFreedom = DoF;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="ChiSquareDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="ChiSquareDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ChiSquareDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.RandomSeed == other.RandomSeed && this.DegreeOfFreedom == other.DegreeOfFreedom;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as ChiSquareDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="ChiSquareDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="ChiSquareDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.DegreeOfFreedom, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="ChiSquareDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.DegreeOfFreedom)}={this.DegreeOfFreedom}";
	}

	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Log normal distribution PDF: $$p_{\mu,\sigma,b,\beta}(x)=\frac{1}{\sigma(x-b)\sqrt{2\pi}}\exp{\left[ -\frac{\left( \ln{\frac{x-b}{\beta}} - \mu \right)^2}{2\sigma^2} \right]}$$
	public class LogNormalDistribution<T> : DisplaceScaleDistribution<T>, IEquatable<LogNormalDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the mean value (<c>μ</c>) of this log normal distribution's subject normal distribution
		/// </summary>
		public T Mean {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the standard deviation (<c>σ</c>) value of this log normal distribution's subject normal distribution
		/// </summary>
		public T StandardDeviation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard normal distribution with μ=0, σ=1, b=0, β=1, and random seed is not set
		/// </summary>
		public LogNormalDistribution() : base()
		{
			this.Mean = Const<T>.Zero; this.StandardDeviation = Const<T>.One;
		}

		/// <summary>
		/// Create a log normal distribution with given <paramref name="mean"/>, <paramref name="stddev"/>, <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean value μ</param>
		/// <param name="stddev">The given standard deviation σ</param>
		/// <param name="displacement">The given displacement b</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public LogNormalDistribution(T mean, T stddev, T displacement, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed)
		{
			this.Mean = mean; this.StandardDeviation = stddev;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="LogNormalDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="LogNormalDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(LogNormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Mean.IsEqual(other.Mean) && this.StandardDeviation.IsEqual(other.StandardDeviation) && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as LogNormalDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="LogNormalDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="LogNormalDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Mean, this.StandardDeviation, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="LogNormalDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Mean)}={this.Mean}, {nameof(this.StandardDeviation)}={this.StandardDeviation}";
	}

	/// <summary>
	/// The class for a one-dimensional exponential distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Exponential distribution PDF: $$p_{a,\beta}(x) =
	//\begin{cases} \dfrac{1}{\beta}\exp{\left( -\dfrac{x-a}{\beta} \right)} & x \ge a \\
	//0 & x \lt a \end{cases}$$
	public class ExponentialDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged { }

	/// <summary>
	/// The class for a one-dimensional Laplace distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Laplace distribution PDF: $$p_{a,\beta}(x) = \frac{1}{\sqrt{2\beta}} \exp{\left( -\frac{|x-a|}{\beta} \right)}$$
	public class LaplaceDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged { }

	/// <summary>
	/// The class for a one-dimensional Weibull distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Weibull distribution PDF: $$p_{a,\alpha,\beta}(x) =
	//\begin{cases} \dfrac{\alpha}{\beta^\alpha} (x-a)^{\alpha-1} \exp{\left[ -\left(\dfrac{x-a}{\beta}\right)^\alpha \right]} & x \ge a \\
	//0 & x \lt a\end{cases}$$
	public class WeibullDistribution<T> : ShapeDisplaceScaleDistribution<T> where T : unmanaged { }

	/// <summary>
	/// The class for a one-dimensional Cauchy distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Cauchy distribution PDF: $$p_{a,\beta}(x) = \frac{1}{\pi\beta\left[ 1 + \left( \frac{x-a}{\beta} \right)^2 \right]}$$
	public class CauchyDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged { }

	/// <summary>
	/// The class for a one-dimensional Rayleigh distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Rayleigh distribution PDF: $$p_{a,\beta}(x) = \frac{2(x-a)}{\beta^2}\exp{\left[ - \left( \frac{x-a}{\beta} \right)^2 \right]}$$
	public class RayleighDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged { }

	/// <summary>
	/// The class for a one-dimensional Gumbel distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Gumbel distribution PDF: $$p_{a,\beta}(x) = \frac{1}{\beta} \exp{\left( \frac{x-a}{\beta} \right)} \cdot
	//\exp{\left[ -\exp{\left( \frac{x-a}{\beta} \right)} \right]}$$
	public class GumbelDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged { }

	/// <summary>
	/// The class for a one-dimensional gamma distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Gamma distribution PDF: $$p_{a,\alpha,\beta}(x) = \begin{cases}
	//\dfrac{1}{\Gamma(\alpha)\beta^\alpha} (x-a)^{\alpha-1} \exp{\left( -\dfrac{x-a}{\beta} \right)} & x \ge a \\
	//0 & x \lt a \end{cases}$$
	//where $\Gamma(a)$ is the complete gamma function.
	public class GammaDistribution<T> : ShapeDisplaceScaleDistribution<T> where T : unmanaged { }

	/// <summary>
	/// The class for a one-dimensional beta distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Beta distribution PDF: $$p_{a,\alpha,\alpha',\beta}(x) = \begin{cases}
	//\dfrac{(x-a)^{\alpha-1} (\beta+a-x)^{\alpha'-1}}{B(\alpha,\alpha')\beta^{\alpha+\alpha'-1}} & a \le x \lt a+\beta \\
	//0 & x \lt a \mbox{ or } x \ge a+\beta \end{cases}$$
	//where $B(p,q)$ is the complete beta function.
	public class BetaDistribution<T> : ShapeDisplaceScaleDistribution<T>, IEquatable<BetaDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the second shaping factor (<c>α'</c>) of this <see cref="BetaDistribution{T}"/>
		/// </summary>
		public T ShapeFactorOther {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard <see cref="BetaDistribution{T}"/> with a=0, α=1, α'=1, β=1, and random seed is not set
		/// </summary>
		public BetaDistribution() : base()
		{
			this.ShapeFactorOther = Const<T>.One;
		}

		/// <summary>
		/// Create an <see cref="BetaDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="shapeFactor1"/>, <paramref name="shapeFactor2"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="shapeFactor1">The given first shape factor α</param>
		/// <param name="shapeFactor2">The given second shape factor α'</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public BetaDistribution(T displacement, T shapeFactor1, T shapeFactor2, T scaleFactor, long? seed = null) : base(displacement, shapeFactor1, scaleFactor, seed)
		{
			this.ShapeFactorOther = shapeFactor2;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="BetaDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="BetaDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(BetaDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.ShapeFactorOther.IsEqual(other.ShapeFactorOther) && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as BetaDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="BetaDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="BetaDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.ShapeFactorOther, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="BetaDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.ShapeFactorOther)}={this.ShapeFactorOther}";
	}
	#endregion

	#region one dimensional integer distributions
	/// <summary>
	/// The class for a one-dimensional Poisson distribution of type <typeparamref name="T"/>, implements <see cref="OneDimensionalIntegerTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Poisson distribution PDF: $$p_{\lambda}(k)=\frac{\lambda^k e^{-k}}{k!}$$
	public class PoissonDistribution<T> : OneDimensionalIntegerTypedDistribution<T>, IEquatable<PoissonDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the λ value of this Poisson distribution
		/// </summary>
		public double Lambda {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a Poisson distribution with given <paramref name="lambda"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="lambda">The given λ value</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public PoissonDistribution(double lambda, long? seed = null) : base(seed)
		{
			this.Lambda = lambda;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="PoissonDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="PoissonDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(PoissonDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Lambda.IsEqual(other.Lambda) && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as PoissonDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="PoissonDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="PoissonDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Lambda, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="PoissonDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Lambda)}={this.Lambda}";
	}


	#endregion

	#region multivariate distributions
	/// <summary>
	/// The class for a multi-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="RealTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Multi-dimensional normal distribution PDF:
	//$$p_{\vec\mu,\Sigma}(\vec x) = \frac{1}{(2\pi)^{D/2}}\frac{1}{\sqrt{\det(\Sigma)}}
	//\exp{\left( -\frac12(\vec x - \vec \mu)^T \Sigma^{-1} (\vec x - \vec \mu) \right)}$$
	//where $D$ is the number of dimensions, $\vec\mu$ is the mean values of all dimensions, $\Sigma$ is the covariance matrix (which is symmetric-definite).
	public class MultiNormalDistribution<T> : RealTypedDistribution<T>, IEquatable<MultiNormalDistribution<T>> where T : unmanaged
	{
		private readonly T[] mean, covariance;

		/// <summary>
		/// Get the rank / number of dimensions of this distribution
		/// </summary>
		public int Rank {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the mean values of this multi-dimensional normal distribution
		/// </summary>
		public ReadOnlySpan<T> Mean {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.mean;
		}

		/// <summary>
		/// Get the <see cref="StorageType"/> indicating how the <see cref="Covariance"/> is being stored
		/// </summary>
		public StorageType CovarianceStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the covariance matrix (or one of its equivalent storage) of this multi-dimensional normal distribution
		/// </summary>
		public ReadOnlySpan<T> Covariance {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.covariance;
		}

		/// <summary>
		/// Indicates how the <see cref="Covariance"/> is stored in the underlying array
		/// </summary>
		public enum StorageType
		{
			/// <summary>
			/// The whole covariance matrix is stored (row or column major is irrelevant due to the symmetry); the length of <see cref="Covariance"/> is <see cref="Rank"/> * <see cref="Rank"/>
			/// </summary>
			Full,
			/// <summary>
			/// The covariance matrix is a diagonal matrix and only the diagonal elements are stored; the length of <see cref="Covariance"/> is <see cref="Rank"/>
			/// </summary>
			Diagonal,
			/// <summary>
			/// The Cholesky factorization of the covariance matrix is stored; the length of <see cref="Covariance"/> is <see cref="Rank"/> * <see cref="Rank"/>
			/// </summary>
			CholeskyFull,
			/// <summary>
			/// The covariance matrix is a diagonal matrix and only the square roots of the diagonal elements are stored; the length of <see cref="Covariance"/> is <see cref="Rank"/>
			/// </summary>
			CholeskyDiagonal,
		}

		/// <summary>
		/// Create a standard normal distribution with means = 0 and covariance = identity, and random seed is not set
		/// </summary>
		/// <param name="rank">The number of dimensions</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rank"/> is less than 2</exception>
		public MultiNormalDistribution(int rank) : base(null)
		{
			if (rank < 2)
				throw new ArgumentOutOfRangeException(nameof(rank), rank, Resources.Parameter.InvalidValue);
			this.Rank = rank;
			this.mean = new T[rank];
			this.CovarianceStorage = StorageType.Diagonal;
			this.covariance = new T[rank];
			((Span<T>)this.covariance).Fill(Const<T>.One);
		}

		/// <summary>
		/// Create a (log) normal distribution with given <paramref name="mean"/>, <paramref name="covar"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean values</param>
		/// <param name="storageType">The <see cref="StorageType"/> of <paramref name="covar"/></param>
		/// <param name="covar">The given covariance matrix</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="storageType"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="mean"/> or <paramref name="covar"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="mean"/> and <paramref name="covar"/> have incompatible size, or indicate a rank less than 2</exception>
		public MultiNormalDistribution(T[] mean, StorageType storageType, T[] covar, long? seed = null) : base(seed)
		{
			if (storageType < StorageType.Full || storageType > StorageType.CholeskyDiagonal)
				throw new ArgumentOutOfRangeException(nameof(storageType), storageType, Resources.Parameter.InvalidValue);
			if (mean is null || mean.Length == 0)
				throw new ArgumentNullException(nameof(mean));
			if (covar is null || covar.Length == 0)
				throw new ArgumentNullException(nameof(covar));
			if ((storageType == StorageType.CholeskyDiagonal || storageType == StorageType.Diagonal) && mean.Length != covar.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(covar));
			if ((storageType == StorageType.CholeskyFull || storageType == StorageType.Full) && mean.Length * mean.Length != covar.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(covar));

			this.Rank = mean.Length;
			if (this.Rank < 2)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(mean));

			this.mean = (T[])mean.Clone();
			this.covariance = (T[])covar.Clone();
			this.CovarianceStorage = storageType;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of <typeparamref name="T"/>
		/// </summary>
		/// <param name="index">The index, must be 0</param>
		/// <returns>The <see cref="DataType"/> of <typeparamref name="T"/></returns>
		public override DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index < 0 || index >= this.Rank)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Same as <see cref="Rank"/>
		/// </summary>
		public override int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Rank;
		}

		/// <summary>
		/// Get the enumerator of the <see cref="DataType"/> of the dimensions of this distribution
		/// </summary>
		public override IEnumerator<DataType> GetEnumerator()
		{
			for (int i = 0; i < this.Rank; i++)
			{
				yield return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="MultiNormalDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="MultiNormalDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Equals(MultiNormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (this.Rank != other.Rank || this.CovarianceStorage != other.CovarianceStorage || this.RandomSeed != other.RandomSeed)
				return false;
			int length = this.CovarianceStorage == StorageType.Full || this.CovarianceStorage == StorageType.CholeskyFull ? this.Rank * this.Rank : this.Rank;
			fixed (T* m1 = this.mean, m2 = other.mean, c1 = this.covariance, c2 = other.covariance)
			{
				CSharp.LinearAlgebra.DenseApi.PointWiseEquals(new ManagedPureStorage<T>(m1, this.Rank), new ManagedPureStorage<T>(m2, this.Rank), out bool equals);
				if (!equals)
					return false;
				CSharp.LinearAlgebra.DenseApi.PointWiseEquals(new ManagedPureStorage<T>(c1, length), new ManagedPureStorage<T>(c2, length), out equals);
				return equals;
			}
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as MultiNormalDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="MultiNormalDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Rank, this.CovarianceStorage, this.Mean.HashCodeOfSpan(), this.Covariance.HashCodeOfSpan(), this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString {
			get {
				string b = base.PropertiesString;
				string means = $", {nameof(this.Mean)}={{{this.Mean.SpanJoin(',')}}}, ";
				int n = this.Rank;
				string covarName = nameof(this.Covariance) + "=";
				string prefix = new(' ', covarName.Length);
				string covar = this.CovarianceStorage switch
				{
					StorageType.Full => this.Covariance.ToMatrixString(n, prefix: prefix)[prefix.Length..],
					StorageType.Diagonal => $"diag{{{this.Covariance.SpanJoin(',')}}}",
					StorageType.CholeskyFull => ((ReadOnlySpan<T>)ExtensionHelper.MatrixMultiply(n, n, n, this.Covariance, this.Covariance, transRight: true)).ToMatrixString(n, prefix: prefix)[prefix.Length..],
					StorageType.CholeskyDiagonal => $"diag{{{string.Join(',', System.Linq.Enumerable.Select(this.covariance, static c => c.NativeMultiply(c)))}}}",
					_ => string.Empty,
				};
				return b + means + covarName + covar;
			}
		}
	}


	#endregion
}
