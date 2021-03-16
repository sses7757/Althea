using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.TensorAlgebra.Dense;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using MEM = Althea.Storage.AbstractApi;
using TAD = Althea.TensorAlgebra.Dense.AbstractApi;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract array class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage. There may be more pointer(s) for different indices in a sparse array that inherits <see cref="ValueArray{T}"/>, but they shall be immutable.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public abstract class ValueArray<T> : AbstractArray<T>, ICheckValid where T : unmanaged
	{
		#region properties
		private readonly Storage<T> m_orginalStorage;

		/// <summary>
		/// Get the raw storage of this array
		/// </summary>
		public Storage<T> Storage { get; }

		/// <summary>
		/// When implemented by a derived class, get the total number of the visible values in memory, in <typeparamref name="T"/> rather than bytes. The default implementation simply returns <see cref="Storage"/>.<see cref="Storage{T}.Length">Length</see>.
		/// </summary>
		public virtual long ActualLength {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Storage.Length;
		}

		/// <summary>
		/// When implemented by a derived class, check whether this array is a valid one or not. The default implementation only checks <see cref="AbstractArray{T}.Length"/> and <see cref="Storage"/>.
		/// </summary>
		/// <returns>The validness of this array</returns>
		public virtual bool IsValid() => this.Length > 0 && this.Storage is not null && this.Storage.IsValid();
		#endregion

		#region initialize and destroy
		/// <summary>
		/// Create a new <see cref="ValueArray{T}"/> using preallocated <paramref name="storage"/> and given <paramref name="size"/>
		/// </summary>
		/// <param name="storage">The preallocated <see cref="Storage{T}"/> (can be a <see cref="ReferenceStorage{T}"/>) as the underlying <see cref="Storage"/> of this array</param>
		/// <param name="size">The presenting size of this array</param>
		/// <param name="actualLength">The actual length of this array, default 0 means the length of <paramref name="storage"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="size"/> is not 0 while <paramref name="storage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="actualLength"/> is out of the length range of <paramref name="storage"/> or <paramref name="size"/></exception>
		protected ValueArray(Storage<T> storage, ReadOnlySpan<long> size, long actualLength = 0) : base(size)
		{
			if (size.Length == 1 && size[0] == 0)
			{
				this.m_orginalStorage = this.Storage = Storage<T>.Empty;
				return;
			}
			// checks
			if (storage is null || !storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (actualLength == 0)
				actualLength = storage.Length;
			if (actualLength < 0)
				throw new ArgumentOutOfRangeException(nameof(actualLength), actualLength, Resources.Parameter.CannotNegative);
			if (actualLength > storage.Length)
				throw new ArgumentOutOfRangeException(nameof(actualLength), actualLength, Resources.Parameter.InvalidValue);
			if (actualLength > this.Length)
				throw new ArgumentOutOfRangeException(nameof(actualLength), actualLength, Resources.Parameter.WrongSize);

			this.Storage = storage.MakeReference(newLength: actualLength);
			this.m_orginalStorage = storage;
		}

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="Storage"/>
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			if (this.Disposed || this.Length == 0 || this.Storage is null)
			{
				return;
			}
			this.m_orginalStorage.Dispose();
		}
		#endregion

		#region concrete operation helpers
		#region matrix case
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ApplyToColumns<TVal>(long rows, long cols, long ld, Action<Storage<T>, TVal> action, TVal value)
		{
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				action.Invoke(column, value);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TRet ApplyToColumns<TRet>(long rows, long cols, long ld, Func<Storage<T>, TRet> function, Func<TRet, TRet, TRet> aggregator, TRet init)
		{
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				TRet here = function.Invoke(column);
				init = aggregator.Invoke(init, here);
			}
			return init;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ApplyToColumns<TVal>(long rows, long cols, long ld, Action<Storage<T>, int, TVal> stridedAction, TVal value)
		{
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				stridedAction.Invoke(column, 1, value);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TRet ApplyToColumns<TRet>(long rows, long cols, long ld, Func<Storage<T>, int, TRet> stridedFunction, Func<TRet, TRet, TRet> aggregator, TRet init)
		{
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				TRet here = stridedFunction.Invoke(column, 1);
				init = aggregator.Invoke(init, here);
			}
			return init;
		}
		#endregion

		#region general case
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long IncreasePos(Span<long> jaggedSize, Span<long> sizeProd, Span<long> position, int maxPosRankInd)
		{
			long offset = 0;
			position[0]++;
			for (int i = 0; i < maxPosRankInd; i++)
			{
				if (position[i] == jaggedSize[i + 1])
				{
					position[i] = 0;
					position[i + 1]++;
				}
				offset += position[i] * sizeProd[i];
			}
			offset += position[maxPosRankInd] * sizeProd[maxPosRankInd];
			return offset;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ApplyToFirstDims<TVal>(Span<long> jaggedSize, Span<long> jaggedOuterSize, Action<Storage<T>, TVal> action, TVal value)
		{
			var storage = this.Storage; int rank = jaggedSize.Length, maxPosRankInd = rank - 2;
			long maxLength = storage.Length, firstDimSize = jaggedSize[0];
			Span<long> sizeProd = jaggedOuterSize.AccumulateProd(stackalloc long[rank], inclusive: false);
			Span<long> position = stackalloc long[maxPosRankInd + 1];
			long offset = 0;
			while (true)
			{
				// action
				action.Invoke(storage.MakeReference(offset, firstDimSize), value);
				// increase position and offset
				offset = IncreasePos(jaggedSize, sizeProd, position, maxPosRankInd);
				if (offset >= maxLength)
					break;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TRet ApplyToFirstDims<TRet>(Span<long> jaggedSize, Span<long> jaggedOuterSize, Func<Storage<T>, TRet> function, Func<TRet, TRet, TRet> aggregator, TRet init)
		{
			var storage = this.Storage; int rank = jaggedSize.Length, maxPosRankInd = rank - 2;
			long maxLength = storage.Length, firstDimSize = jaggedSize[0];
			Span<long> sizeProd = jaggedOuterSize.AccumulateProd(stackalloc long[rank], inclusive: false);
			Span<long> position = stackalloc long[maxPosRankInd + 1];
			long offset = 0;
			while (true)
			{
				// function
				TRet now = function.Invoke(storage.MakeReference(offset, firstDimSize));
				init = aggregator(init, now);
				// increase position and offset
				offset = IncreasePos(jaggedSize, sizeProd, position, maxPosRankInd);
				if (offset >= maxLength)
					break;
			}
			return init;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ApplyToFirstDims<TVal>(Span<long> jaggedSize, Span<long> jaggedOuterSize, Action<Storage<T>, int, TVal> stridedAction, TVal value)
		{
			var storage = this.Storage; int rank = jaggedSize.Length, maxPosRankInd = rank - 2;
			long maxLength = storage.Length, firstDimSize = jaggedSize[0];
			Span<long> sizeProd = jaggedOuterSize.AccumulateProd(stackalloc long[rank], inclusive: false);
			Span<long> position = stackalloc long[maxPosRankInd + 1];
			long offset = 0;
			while (true)
			{
				// action
				stridedAction.Invoke(storage.MakeReference(offset, firstDimSize), 1, value);
				// increase position and offset
				offset = IncreasePos(jaggedSize, sizeProd, position, maxPosRankInd);
				if (offset >= maxLength)
					break;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TRet ApplyToFirstDims<TRet>(Span<long> jaggedSize, Span<long> jaggedOuterSize, Func<Storage<T>, int, TRet> stridedFunction, Func<TRet, TRet, TRet> aggregator, TRet init)
		{
			var storage = this.Storage; int rank = jaggedSize.Length, maxPosRankInd = rank - 2;
			long maxLength = storage.Length, firstDimSize = jaggedSize[0];
			Span<long> sizeProd = jaggedOuterSize.AccumulateProd(stackalloc long[rank], inclusive: false);
			Span<long> position = stackalloc long[maxPosRankInd + 1];
			long offset = 0;
			while (true)
			{
				// function
				TRet now = stridedFunction.Invoke(storage.MakeReference(offset, firstDimSize), 1);
				init = aggregator(init, now);
				// increase position and offset
				offset = IncreasePos(jaggedSize, sizeProd, position, maxPosRankInd);
				if (offset >= maxLength)
					break;
			}
			return init;
		}
		#endregion

		#region judge
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetJagged(IPitchedArray<T> pitched, int orgRank, ref Span<long> jaggedSize, ref Span<long> jaggedOuterSize)
		{
			var orgSize = pitched.Size; var orgOuterSize = pitched.OuterSize;
			jaggedSize[0] = jaggedOuterSize[0] = 1;
			int rank = 0;
			for (int i = 0; i < orgRank; i++)
			{
				if (orgSize[i] == orgOuterSize[i])
				{
					jaggedSize[rank] *= orgSize[i];
					jaggedOuterSize[rank] *= orgSize[i];
				}
				else
				{
					jaggedSize[rank] *= orgSize[i];
					jaggedOuterSize[rank] *= orgOuterSize[i];
					rank++;
					if (rank == orgRank)
						break;
					jaggedSize[rank] = jaggedOuterSize[rank] = 1;
				}
			}
			jaggedSize = jaggedSize[..rank]; jaggedOuterSize = jaggedOuterSize[..rank];
			return rank;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EditPitchedInPlace<TVal>(IPitchedArray<T> pitched, Action<Storage<T>, TVal> action, TVal value)
		{
			// get jagged size
			int orgRank = this.Rank;
			Span<long> jaggedSize = stackalloc long[orgRank];
			Span<long> jaggedOuterSize = stackalloc long[orgRank];
			int rank = GetJagged(pitched, orgRank, ref jaggedSize, ref jaggedOuterSize);
			// switch different cases
			if (jaggedSize[1..].Prod() <= 1000)
			{   // The estimate overhead of one API call is around 1 microsecond
				// Typically, we do not want a total overhead larger than 1 millisecond
				if (rank == 2)
					this.ApplyToColumns(jaggedSize[0], jaggedSize[1], jaggedOuterSize[0], action, value);
				else
					this.ApplyToFirstDims(jaggedSize, jaggedOuterSize, action, value);
			}
			else
			{   // tensor algebra API fill and copy
				using var temp = Storage<T>.Create(this.Storage[0].Location, jaggedSize.Prod());
				// edit the temp array
				action.Invoke(temp, value);
				DenseTensorWrapper<T> tempWrapper = new(temp, jaggedSize, jaggedSize),
									  thisWrapper = new(this.Storage, jaggedSize, jaggedOuterSize);
				// copy to this pitched array
				TAD.Permute(tempWrapper, thisWrapper, stackalloc int[rank].FillWithRange(0));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EditPitchedInPlace<TVal>(IPitchedArray<T> pitched, Action<Storage<T>, int, TVal> stridedAction, TVal value)
		{
			// get jagged size
			int orgRank = this.Rank;
			Span<long> jaggedSize = stackalloc long[orgRank];
			Span<long> jaggedOuterSize = stackalloc long[orgRank];
			int rank = GetJagged(pitched, orgRank, ref jaggedSize, ref jaggedOuterSize);
			// switch different cases
			if (jaggedSize[0] == 1 && rank == 2)
			{   // linear algebra API with given vector stride
				stridedAction.Invoke(this.Storage, checked((int)jaggedOuterSize[0]), value);
			}
			else if (jaggedSize[1..].Prod() <= 1000)
			{   // The estimate overhead of one API call is around 1 microsecond
				// Typically, we do not want a total overhead larger than 1 millisecond
				if (rank == 2)
					this.ApplyToColumns(jaggedSize[0], jaggedSize[1], jaggedOuterSize[0], stridedAction, value);
				else
					this.ApplyToFirstDims(jaggedSize, jaggedOuterSize, stridedAction, value);
			}
			else
			{   // tensor algebra API fill and copy
				using var temp = Storage<T>.Create(this.Storage[0].Location, jaggedSize.Prod());
				// edit the temp array
				stridedAction.Invoke(temp, 1, value);
				DenseTensorWrapper<T> tempWrapper = new(temp, jaggedSize, jaggedSize),
									  thisWrapper = new(this.Storage, jaggedSize, jaggedOuterSize);
				// copy to this pitched array
				TAD.Permute(tempWrapper, thisWrapper, stackalloc int[rank].FillWithRange(0));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TRet AggregatePitched<TRet>(IPitchedArray<T> pitched, Func<Storage<T>, int, TRet> stridedFunction, Func<TRet, TRet, TRet> aggregator, TRet init)
		{
			// get jagged size
			int orgRank = this.Rank;
			Span<long> jaggedSize = stackalloc long[orgRank];
			Span<long> jaggedOuterSize = stackalloc long[orgRank];
			int rank = GetJagged(pitched, orgRank, ref jaggedSize, ref jaggedOuterSize);
			// switch different cases
			if (jaggedSize[0] == 1 && rank == 2)
			{   // linear algebra API with given vector stride
				return aggregator.Invoke(init, stridedFunction.Invoke(this.Storage, checked((int)jaggedOuterSize[0])));
			}
			else if (jaggedSize[1..].Prod() <= 1000)
			{   // The estimate overhead of one API call is around 1 microsecond
				// Typically, we do not want a total overhead larger than 1 millisecond
				if (rank == 2)
					return this.ApplyToColumns(jaggedSize[0], jaggedSize[1], jaggedOuterSize[0], stridedFunction, aggregator, init);
				else
					return this.ApplyToFirstDims(jaggedSize, jaggedOuterSize, stridedFunction, aggregator, init);
			}
			else
			{   // tensor algebra API fill and copy
				using var temp = Storage<T>.Create(this.Storage[0].Location, jaggedSize.Prod());
				// copy the temp array
				DenseTensorWrapper<T> tempWrapper = new(temp, jaggedSize, jaggedSize),
									  thisWrapper = new(this.Storage, jaggedSize, jaggedOuterSize);
				TAD.Permute(thisWrapper, tempWrapper, stackalloc int[rank].FillWithRange(0));
				// aggregate on temp array
				return aggregator.Invoke(init, stridedFunction.Invoke(temp, 1));
			}
		}
		#endregion
		#endregion

		#region point-wise concrete operations
		/// <summary>
		/// When implemented by a derived class, fill this array's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="MEM.FillWithValue{T}(Storage{T}, T)"/>, which is also valid if the actual derived class is a <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="value">The value as <typeparamref name="T"/> to fill</param>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual void FillWith(T value)
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				MEM.FillWithValue(this.Storage, value);
				if (this is ISparseArray<T> sparse)
				{
					sparse.DefaultValue = value;
				}
			}
			else
			{
				this.EditPitchedInPlace(pitched, MEM.FillWithValue, value);
			}
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place add this array's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.PointWiseAddScalar{T}"/>, which is also valid if the actual derived class is a <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to add</param>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual void AddScalar(T value)
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				LAD.PointWiseAddScalar(this.Storage, 1, value);
				if (this is ISparseArray<T> sparse && !value.IsZero())
				{
					sparse.DefaultValue = sparse.DefaultValue.GenericAdd(value);
				}
			}
			else
			{
				this.EditPitchedInPlace(pitched, LAD.PointWiseAddScalar, value);
			}
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this array's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.Scale{T}"/>, which is also valid if the actual derived class is a <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual void Scale(T value)
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				LAD.Scale(this.Storage, 1, value);
				if (this is ISparseArray<T> sparse && !value.IsOne())
				{
					sparse.DefaultValue = sparse.DefaultValue.GenericMultiply(value);
				}
			}
			else
			{
				this.EditPitchedInPlace(pitched, LAD.Scale, value);
			}
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place conjugate this array's <see cref="Storage"/>. The default implementation utilizes <see cref="LAD.PointWiseConjugate{T}"/>, which is also valid if the actual derived class is a <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual void Conjugate()
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				LAD.PointWiseConjugate(this.Storage, 1);
				if (this is ISparseArray<T> sparse)
				{
					sparse.DefaultValue = sparse.DefaultValue.GenericConjugate();
				}
			}
			else
			{
				this.EditPitchedInPlace(pitched, static (s, i, _) => LAD.PointWiseConjugate(s, i), 0);
			}
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, double)"/>, which is also valid if the actual derived class is a <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="power">The power as a <see cref="double"/></param>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual void Power(double power)
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				LAD.PointWisePower(this.Storage, 1, power);
				if (this is ISparseArray<T> sparse && power != 1)
				{
					sparse.DefaultValue = sparse.DefaultValue.GenericPower(power);
				}
			}
			else
			{
				this.EditPitchedInPlace(pitched, LAD.PointWisePower, power);
			}
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>, which is also valid if the actual derived class is a <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual void Power(T power)
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				LAD.PointWisePower(this.Storage, 1, power);
				if (this is ISparseArray<T> sparse && !power.IsOne())
				{
					sparse.DefaultValue = sparse.DefaultValue.GenericPower(power);
				}
			}
			else
			{
				this.EditPitchedInPlace(pitched, LAD.PointWisePower, power);
			}
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place truncate this array's <see cref="Storage"/> by comparing with given <paramref name="threshold"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>, which is also valid if the actual derived class is a <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="threshold">The threshold as a <see cref="double"/>. Any element in <see cref="Storage"/> whose absolute value ≤ <paramref name="threshold"/> will be set to 0.</param>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual void Truncate(double threshold)
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				LAD.TruncateArray(this.Storage, threshold);
				if (this is ISparseArray<T> sparse && !sparse.DefaultValue.IsZero())
				{
					double abs = Const<T>.AbsoluteDelegate.Invoke(sparse.DefaultValue);
					if (abs <= threshold)
						sparse.DefaultValue = default;
				}
			}
			else
			{
				this.EditPitchedInPlace(pitched, LAD.TruncateArray, threshold);
			}
		}
		#endregion

		#region simple aggregation operations
		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes <see cref="LAD.AggregateSum{T}"/>.
		/// </summary>
		/// <returns>The aggregate sum of this array</returns>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual T Sum()
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				T sum = LAD.AggregateSum(this.Storage, 1);
				if (this.Length == this.ActualLength || this is not ISparseArray<T> sparse || sparse.DefaultValue.IsZero())
					return sum;
				// else
				T len = Const<T>.FromLongDelegate.Invoke(this.Length - this.ActualLength);
				T defMulLen = Const<T>.MultiplyDelegate.Invoke(len, sparse.DefaultValue);
				return Const<T>.AddDelegate.Invoke(defMulLen, sum);
			}
			else
			{
				return this.AggregatePitched(pitched, LAD.AggregateSum, Const<T>.AddDelegate, Const<T>.Zero);
			}
		}

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes <see cref="LAD.AbsoluteValueSum{T}"/>.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this array</returns>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual double AbsSum()
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				double sum = LAD.AbsoluteValueSum(this.Storage, 1);
				if (this.Length == this.ActualLength || this is not ISparseArray<T> sparse || sparse.DefaultValue.IsZero())
					return sum;
				else
					return (this.Length - this.ActualLength) * Const<T>.AbsoluteDelegate.Invoke(sparse.DefaultValue) + sum;
			}
			else
			{
				return this.AggregatePitched(pitched, LAD.AbsoluteValueSum, Const<double>.AddDelegate, 0.0);
			}
		}

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes <see cref="LAD.Norm{T}"/>.
		/// </summary>
		/// <returns>The 2-norm of this array</returns>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual double Norm()
		{
			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				double norm = LAD.Norm(this.Storage, 1);
				if (this.Length == this.ActualLength || this is not ISparseArray<T> sparse || sparse.DefaultValue.IsZero())
				{
					return norm;
				}
				else
				{
					norm *= norm;
					double abs = Const<T>.AbsoluteDelegate.Invoke(sparse.DefaultValue);
					norm += abs * abs * (this.Length - this.ActualLength);
					return Math.Sqrt(norm);
				}
			}
			else
			{
				double normSquare = this.AggregatePitched(pitched, LAD.Norm, static (pre, now) => pre + now * now, 0.0);
				return Math.Sqrt(normSquare);
			}
		}

		/// <summary>
		/// When implemented by a derived class, in-place scale this array's <see cref="Storage"/> such that its 2-norm (Euclidean norm) is 1, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes the <see cref="Norm()"/> and <see cref="Scale(T)"/>.
		/// </summary>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		public virtual void Normalize()
		{
			if (this.Length == this.ActualLength || this is not ISparseArray<T> sparse || sparse.DefaultValue.IsZero())
			{
				double norm = this.Norm();
				if (norm == 0)
					throw new DivideByZeroException();
				this.Scale(Const<T>.FromDoubleDelegate.Invoke(1 / norm));
			}
			else
			{
				T def = sparse.DefaultValue;
				double d = Const<T>.AbsoluteDelegate.Invoke(def);
				double defaultNormDouble = (this.Length - this.ActualLength) * d * d;
				double norm = this.Norm() + defaultNormDouble;
				if (norm == 0)
					throw new DivideByZeroException();
				T normInv = Const<T>.FromDoubleDelegate.Invoke(1 / norm);
				// scale both stored and not stored
				this.Scale(normInv);
				sparse.DefaultValue = Const<T>.MultiplyDelegate.Invoke(def, normInv);
			}
		}

		/// <summary>
		/// When implemented by a derived class, get the maximum one of all absolute values of the elements in this array. The default implementation only get the maximum absolute value of <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes <see cref="LAD.AbsoluteValueArgMax{T}"/>.
		/// </summary>
		/// <returns>The maximum one of all absolute values of the elements in this array</returns>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual double AbsMax()
		{
			static double GetAbsMax(Storage<T> storage, int stride)
				=> Const<T>.AbsoluteDelegate.Invoke(MEM.ToManaged(storage + LAD.AbsoluteValueArgMax(storage, stride)));

			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				double max = GetAbsMax(this.Storage, 1);
				if (this.Length == this.ActualLength || this is not ISparseArray<T> sparse)
					return max;
				else
					return Math.Max(Const<T>.AbsoluteDelegate.Invoke(sparse.DefaultValue), max);
			}
			else
			{
				return this.AggregatePitched(pitched, GetAbsMax, static (pre, now) => Math.Max(pre, now), 0.0);
			}
		}

		/// <summary>
		/// When implemented by a derived class, get the minimum one of all absolute values of the elements in this array. The default implementation only get the maximum absolute value of <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes <see cref="LAD.AbsoluteValueArgMin{T}"/>.
		/// </summary>
		/// <returns>The minimum one of all absolute values of the elements in this array</returns>
		/// <remarks>If this array is an <see cref="IPitchedArray{T}"/> and <see cref="IPitchedArray{T}.HasPitch"/>, this method may loops over the first few contiguous dimensions or create temporary storage, which may lead to performance loss.</remarks>
		public virtual double AbsMin()
		{
			static double GetAbsMin(Storage<T> storage, int stride)
				=> Const<T>.AbsoluteDelegate.Invoke(MEM.ToManaged(storage + LAD.AbsoluteValueArgMin(storage, stride)));

			if (this is not IPitchedArray<T> pitched || !pitched.HasPitch)
			{
				double min = GetAbsMin(this.Storage, 1);
				if (this.Length == this.ActualLength || this is not ISparseArray<T> sparse)
					return min;
				else
					return Math.Min(Const<T>.AbsoluteDelegate.Invoke(sparse.DefaultValue), min);
			}
			else
			{
				return this.AggregatePitched(pitched, GetAbsMin, static (pre, now) => Math.Min(pre, now), double.MaxValue);
			}
		}
		#endregion

		#region reshape (mostly abstract)
		/// <summary>
		/// Check the new size (dimensionality) to reshape to with respect to the original <paramref name="array"/> and find out the uncertain dimension.
		/// </summary>
		/// <param name="array">The original array as a <see cref="ValueArray{T}"/> to check</param>
		/// <param name="newSize">The new size as a <see cref="Span{T}"/> to check which can have at most one uncertain dimension indicated by a non-positive number. Overwritten by the new size without uncertain dimension at exit.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="newSize"/> is of length 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="newSize"/> is of length 2 and are all non-positive while the length of <paramref name="array"/> is not a perfect square; or <paramref name="newSize"/> has more than one uncertain dimensions</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the product of <paramref name="newSize"/> is not the same as the presenting length of <paramref name="array"/></exception>
		protected static void CheckSize(ValueArray<T> array, Span<long> newSize)
		{
			if (newSize.Length == 0)
				throw new ArgumentNullException(nameof(newSize));
			// shortcut
			if (newSize.SequenceEqual(array.Size))
				return;

			if (newSize.Length == 2 && newSize[0] <= 0 && newSize[1] <= 0)
			{	// try to convert to a square matrix
				if (!array.Length.IsPerfectSquare())
				{
					throw new ArgumentException(Resources.Other.PerfectSquare, nameof(array));
				}
				var leadDim = Convert.ToInt64(Math.Sqrt(array.Length));
				newSize[0] = newSize[1] = leadDim;
			}
			int firstFind = newSize.IndexOf(static r => r <= 0);
			if (firstFind < 0)
			{	// no uncertain index
				if (newSize.Prod() != array.Length)
					throw new ArgumentOutOfRangeException(nameof(newSize), newSize.Prod(), Resources.Parameter.InvalidValue);
				return;
			}
			int lastFind = newSize.LastIndexOf(static r => r <= 0);
			if (lastFind == firstFind)
			{	// only one uncertainty
				newSize[firstFind] = 1;
				var prod = newSize.Prod();
				var remain = array.Length % prod;
				if (remain != 0)
					throw new ArgumentOutOfRangeException(nameof(newSize), remain, Resources.Parameter.InvalidValue);
				else
					newSize[firstFind] = array.Length / prod;
			}
			else
			{	// more than one uncertain indices
				throw new ArgumentException(Resources.Parameter.UnexpectedValue, nameof(newSize));
			}
		}

		/// <summary>
		/// When implemented by a derived class, reshape the array to a <paramref name="newSize"/>.
		/// </summary>
		/// <param name="newSize">The new size/dimensionality. You can have at most one uncertain dimension, indicated by a non-positive number.</param>
		/// <returns>The reshaped array which may be a referenced array or may not</returns>
		public virtual ValueArray<T> Reshape(ReadOnlySpan<long> newSize)
		{
			Span<long> size = stackalloc long[newSize.Length];
			newSize.CopyTo(size);
			CheckSize(this, size);
			if (this.Size == newSize)
				return this;
			return newSize.Length switch
			{
				0 => throw new ArgumentException(Resources.Parameter.ZeroSize, nameof(newSize)),
				1 => this.ToVector(),
				2 => this.ToMatrix(newSize[0]),
				_ => this.ToTensor(size: size),
			};
		}

		/// <summary>
		/// When implemented by a derived class, reshape this array to a vector
		/// </summary>
		/// <returns>The referenced vector reshaped from this array</returns>
		public abstract ValueArray<T> ToVector();

		/// <summary>
		/// When implemented by a derived class, reshape the array to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public abstract ValueArray<T> ToMatrix(long rows = 0);

		/// <summary>
		/// When implemented by a derived class, reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public abstract ValueArray<T> ToTensor(ReadOnlySpan<long> size);
		#endregion

		#region new methods and overrides
		/// <summary>
		/// When implemented by a derived class, check if this <see cref="ValueArray{T}"/> share some storage with the <paramref name="other"/> one. The default implementation only compares the <see cref="Storage"/>s.
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public virtual bool OverlapWith(ValueArray<T> other)
		{
			if (other is ValueArray<T> arr)
			{
				if (ReferenceEquals(this, arr))
					return true;
				else
					return this.Storage.OverlapWith(arr.Storage);
			}
			else
				return false;
		}

		/// <summary>
		/// The string representation terms
		/// </summary>
		protected enum StringTerms
		{
			/// <summary>
			/// Add the term for the string representation of the current data type(s)
			/// </summary>
			DataType,
			/// <summary>
			/// Add the term for the string representation of the all storages obtained from <see cref="GetStorages"/>
			/// </summary>
			Storages,
			/// <summary>
			/// Add the term for the string representation of the current presenting size
			/// </summary>
			Size
		}

		/// <summary>
		/// Get the string representation of this array with new terms and existed ones (existed ones are shown at first).
		/// </summary>
		/// <param name="terms">The additional terms, null means all pairs in <see cref="GetMetaData"/></param>
		/// <param name="include">The include terms, default null means all</param>
		/// <returns>The string representation</returns>
		protected string ToString(IReadOnlyDictionary<string, object>? terms, params StringTerms[] include)
		{
			// default values
			if (include is null || include.Length == 0)
				include = new[] { StringTerms.DataType, StringTerms.Size, StringTerms.Storages };
			terms ??= this.GetMetaData();
			// get type name of this array
			var type = this.GetType();
			string? name;
			if (include.Contains(StringTerms.DataType))
			{
				name = type.GetGenericString(full: true);
			}
			else
			{
				name = type.FullName;
				if (type.IsGenericType)
				{
					name = name?.Replace($"`{type.GenericTypeArguments.Length}", "");
				}
			}
			// output include terms and other terms
			StringBuilder output = new(name);
			output.Append(this.Disposed ? " (disposed) " : " ");
			// start
			output.Append('[');
			foreach (var item in include)
			{
				if (item == StringTerms.Size)
				{
					output.Append($"Size={string.Join('x', this.m_size)}");
				}
				else if (item == StringTerms.Storages)
				{
					output.Append('{');
					foreach (var s in this.GetStorages())
					{
						output.Append(s.Key).Append("={").Append(s.Value).Append("}, ");
					}
					output.Remove(output.Length - 2, 2).Append('}');
				}
				else
					continue;
				output.Append(", ");
			}
			// other terms
			foreach (var item in terms)
			{
				output.Append(item.Key).Append('=').Append(item.Value).Append(", ");
			}
			output.Remove(output.Length - 2, 2);
			// end
			output.Append(']');
			return output.ToString();
		}

		/// <summary>
		/// Override <see cref="AbstractArray{T}.ToString"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return this.ToString(terms: null);
		}

		/// <summary>
		/// When implemented by a derived class, get the hash code this array. The default implementation only takes <see cref="Storage"/> and <see cref="AbstractArray{T}.Size"/> into account.
		/// </summary>
		/// <returns>The hash code computed by <see cref="Storage"/> and <see cref="AbstractArray{T}.Size"/></returns>
		public override int GetHashCode() => HashCode.Combine(this.Storage, this.Size.HashCodeOfSpan());

		/// <summary>
		/// When implemented by a derived class, check whether this object is equal to another one. The default implementation utilizes <see cref="AbstractArray{T}.Equals(object?)"/> and additionally compares <see cref="Storage"/>s.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			if (obj is null || obj is not ValueArray<T> a)
				return false;
			else
				return base.Equals(obj) && this.Storage == a.Storage;
		}
		#endregion

		#region clone relate
		/// <summary>
		/// When implemented by a derived class, deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override abstract ValueArray<T> Clone();

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new array alike this one</returns>
		public abstract ValueArray<T> NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new array alike this one</returns>
		public abstract ValueArray<TOut> NewArrayAlike<TOut>() where TOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The new <see cref="ValueArray{TOut}"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public override ValueArray<TOut> DataTypeCast<TOut>()
		{
			if (typeof(T) == typeof(TOut))
			{
#pragma warning disable CS8603 // the 'as' here cannot return null
				return this as ValueArray<TOut>;
#pragma warning restore CS8603
			}
			var alike = this.NewArrayAlike<TOut>();
			try
			{
				LAD.PointWiseCast(this.Storage, 1, alike.Storage, 1);
				return alike;
			}
			catch (Exception)
			{
				alike?.Dispose();
				throw;
			}
		}
		#endregion

		#region override operators
		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator ==(ValueArray<T> array, T value)
		{
			if (array is null || !array.IsValid())
				return false;
			long index;
			if (!value.IsZero())
			{
				using var clone = array.Storage.Clone();
				LAD.PointWiseAddScalar(clone, 1, value.GenericNegate());
				index = LAD.AbsoluteValueArgMax(clone, 1);
			}
			else
			{
				index = LAD.AbsoluteValueArgMax(array.Storage, 1);
			}
			double val = Const<T>.AbsoluteDelegate.Invoke(MEM.ToManaged(array.Storage + index))
							.ToDouble();
			return val <= 1E-6;
		}

		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator ==(T value, ValueArray<T> array) => array == value;

		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if any element in <paramref name="array"/>'s <see cref="Storage"/> is not with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator !=(ValueArray<T> array, T value) => !(array == value);

		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if any element in <paramref name="array"/>'s <see cref="Storage"/> is not with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator !=(T value, ValueArray<T> array) => !(array == value);
		#endregion

		#region serialization
		/// <summary>
		/// The pointer name that <b>shall</b> be used in <see cref="GetStorages"/>.
		/// </summary>
		public const string StorageName = nameof(Storage);

		/// <summary>
		/// When implemented by a derived class, get all the storages of this array. The <see cref="Storage"/> must be associated with key <see cref="StorageName"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public abstract IReadOnlyDictionary<string, IStorage> GetStorages();

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array</returns>
		public abstract IReadOnlyDictionary<string, object> GetMetaData();
		#endregion
	}
}
