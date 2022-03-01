using System;
using System.Collections.Generic;

using Althea.NativeTypes;
using Althea.Storage;


namespace Althea.Helpers
{
	/// <summary>
	/// The exception to be thrown when one generic type does not fit given regulations or two generic types are mismatched for some reason
	/// </summary>
	[Serializable]
	public sealed class TypeMismatchException : Exception
	{
		/// <summary>
		/// The enum to indicate the type mismatch reason
		/// </summary>
		public enum MismatchReason
		{
			/// <summary>
			/// Cannot convert the first type to the second one
			/// </summary>
			CannotConvert,
			/// <summary>
			/// The second type is not a real type correspondence of the first one
			/// </summary>
			IsNotRealCorrespondence,
			/// <summary>
			/// The second type is not a complex type correspondence of the first one
			/// </summary>
			IsNotComplexCorrespondence,
			/// <summary>
			/// The given type is not an integral type
			/// </summary>
			NotInteger,
			/// <summary>
			/// The given type is not an floating point type
			/// </summary>
			NotFloat,
			/// <summary>
			/// The given type is not a real type
			/// </summary>
			NotReal,
		}

		/// <summary>
		/// Empty <see cref="TypeMismatchException"/>
		/// </summary>
		public TypeMismatchException() { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch type and mismatch reason and inner exception
		/// </summary>
		/// <param name="type">The mismatch type</param>
		/// <param name="storages">The list of <see cref="IStorage"/> to get the real <see cref="Type"/> corresponding of <paramref name="type"/></param>
		/// <param name="reason">The mismatch reason</param>
		/// <param name="inner">The inner exception</param>
		public TypeMismatchException(DataType type, IReadOnlyList<IStorage> storages, MismatchReason reason, Exception? inner = null) :
			base(GetMessage(GetTypeFrom(type, storages), null, reason), inner)
		{ }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch type and mismatch reason
		/// </summary>
		/// <param name="type">The mismatch type</param>
		/// <param name="reason">The mismatch reason</param>
		public TypeMismatchException(Type type, MismatchReason reason) : base(GetMessage(type, null, reason), null) { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch type and mismatch reason and inner exception
		/// </summary>
		/// <param name="type">The mismatch type</param>
		/// <param name="reason">The mismatch reason</param>
		/// <param name="inner">The inner exception</param>
		public TypeMismatchException(Type type, MismatchReason reason, Exception? inner = null) : base(GetMessage(type, null, reason), inner) { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch types and mismatch reason and inner exception
		/// </summary>
		/// <param name="from">The first mismatch type</param>
		/// <param name="to">The second mismatch type</param>
		/// <param name="reason">The mismatch reason</param>
		/// <param name="inner">The inner exception</param>
		public TypeMismatchException(Type from, Type to, MismatchReason reason, Exception? inner = null) : base(GetMessage(from, to, reason), inner) { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch types and mismatch reason
		/// </summary>
		/// <param name="from">The first mismatch type</param>
		/// <param name="to">The second mismatch type</param>
		/// <param name="reason">The mismatch reason</param>
		public TypeMismatchException(Type from, Type to, MismatchReason reason) : this(from, to, reason, null) { }

		private static Type GetTypeFrom(DataType type, IReadOnlyList<IStorage> storages)
		{
			if (storages is null || storages.Count == 0)
				return new object().GetType();
			for (int i = 0; i < storages.Count; i++)
			{
				Type t = storages[i].GetType();
				if (t.ToDataType() == type)
					return t;
			}
			return storages[0].GetType();
		}

		private static string GetMessage(Type from, Type? to, MismatchReason reason)
		{
			string format = reason switch
			{
				MismatchReason.CannotConvert => Resources.Exception.MismatchCannotConvert,
				MismatchReason.IsNotRealCorrespondence => Resources.Exception.MismatchNotRealCorrespondence,
				MismatchReason.IsNotComplexCorrespondence => Resources.Exception.MismatchNotComplexCorrespondence,
				MismatchReason.NotInteger => Resources.Exception.MismatchNotInteger,
				MismatchReason.NotFloat => Resources.Exception.MismatchNotFloat,
				MismatchReason.NotReal => Resources.Exception.MismatchNotReal,
				_ => Resources.Exception.MismatchOtherReason
			};
			string? fromString = from.GetGenericString(), toString = to?.GetGenericString();
			return toString is null ? string.Format(format, fromString) : string.Format(format, fromString, toString);
		}
	}
}
