using System;
using System.Collections.Generic;

using Althea.Storage;


namespace Althea.Arrays.Matrices
{
	/// <summary>
	/// The dense matrix interface whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="Storage"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IDenseVector{T, TS, TSelf}"/></typeparam>
	public interface IDenseMatrix<T, TS, TSelf> : IBaseVector<T, TSelf>, ISingleValueStorageArray<T, TS, TSelf>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
		where TSelf : class, IDenseVector<T, TS, TSelf>
	{
	}
}
