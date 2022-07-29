using System;

using Althea.Array;
using Althea.Storage;
using Althea.Numerics;


namespace Althea.GeneralSolvers.Krylov.Array
{
	internal class DenseVector<T, TS> : Althea.Array.DenseVector<T, TS>, IKrylovVector<T, DenseVector<T, TS>>
		where T : unmanaged, IBaseNumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region create
		public DenseVector(Althea.Array.DenseVector<T, TS> baseVector) : base(baseVector)
		{
#pragma warning disable CA1816
			GC.SuppressFinalize(this);
#pragma warning restore CA1816
		}
		#endregion

		#region Krylov
		DenseVector<T, TS> ICreateAlike<DenseVector<T, TS>>.CreateAlike() => new(base.CreateAlike());

		public DenseVector<T, TS> Clone() => ((ICloneable<DenseVector<T, TS>>)this).Clone();

		object ICloneable.Clone() => this.Clone();

		static DenseVector<T, TS> IKrylovVector<T, DenseVector<T, TS>>.Empty => new(Empty);

		void IKrylovVector<T, DenseVector<T, TS>>.Normalize() => ((IValueArray<T, Althea.Array.DenseVector<T, TS>>)this).Normalize();

		T IKrylovVector<T, DenseVector<T, TS>>.Dot(DenseVector<T, TS> other) => DenseOperation<T, TS>.Dot(this, other);

		void IKrylovVector<T, DenseVector<T, TS>>.AddBy(DenseVector<T, TS> other, T scalar) => DenseOperation<T, TS>.AddBy(this, other, scalar);

		void IKrylovVector<T, DenseVector<T, TS>>.ReplaceBy(DenseVector<T, TS> other) => other.CopyTo(this);
		#endregion
	}
}
