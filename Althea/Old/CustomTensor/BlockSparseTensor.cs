using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Linq = System.Linq.Enumerable;

using Althea;
using Althea.Arrays;
using Althea.Linq;



namespace TensorCSharp.OneDimension.CustomTensor
{
	/// <summary>
	/// The class for block sparse tensor with charge, inherits <see cref="PureArray{T}"/> and implements <see cref="ITensor{TTen, T}"/>
	/// </summary>
	/// <typeparam name="T">the data type</typeparam>
	/// <typeparam name="TC">the charge type</typeparam>
	/// <remarks>For performance issues, most operations from <see cref="Althea.General.IKrylovVector{TVec, T}"/> and <see cref="ITensorAsMatrix{TTen, T}"/> are not checked before calculations. If you want to manually check them, call <see cref="KrylovVectorCheck"/> and <see cref="DiagonalOperationCheck"/></remarks>
	public sealed class BlockSparseTensor<T, TC> : ValueArray<T>, ITensor<BlockSparseTensor<T, TC>, T>, ITensorAsMatrix<BlockSparseTensor<T, TC>, T>
		where T : unmanaged, IFormattable, IEquatable<T>
		where TC : unmanaged, ICharge<TC>
	{
		#region label
		/// <summary>
		/// Get the rank of this tensor
		/// </summary>
		public int Rank => this.Size.Count;

		private char[] _label;

		/// <summary>
		/// The label to mark each index of this tensor
		/// </summary>
		public IReadOnlyList<char> Label {
			get {
				if (this._label is null)
				{   // lazy initialization
					this._label = ArrayLinq.Range('a', this.Rank).ToArray();
				}
				return this._label;
			}
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (value.Count != this.Rank)
					throw new ArgumentException(Resource.SizeInconsistent, nameof(value));
				if (value.Distinct().Count != value.Count)
					throw new ArgumentException(Resource.LabelInconsistant, nameof(value));
				this._label = value.ToArray();
			}
		}

		/// <summary>
		/// Set the label to mark each index of this tensor
		/// </summary>
		/// <param name="label">label to set</param>
		public void SetLabel(params char[] label) => this.Label = label;
		#endregion


		#region members and dispose
		// following private members starts with '_' means that you SHALL NOT use reflection to alter them
		// and they are not set to read-only since they are large and may be referenced by other BlockSparseTensor<T, TC>
		// when disposing managed resources, they will be set to null to decrease the underlying reference counting

		// the necessary members are '_multiplicities', '_charges' and '_blockIndex'
		// '_charges' and '_blockIndex' are always sorted to preserve uniqueness

		// for the following two jagged arrays:
		// outer array (the first []) accounts for each dimension, the inner arrays account for each sparse indices' max value
		// e.g. _multiplicities[i].Length == _charges[i].Length == blockSize[i]
		private int[][] _multiplicities;
		private TC[][] _charges;

		// accumulated sum of _multiplicities for each inner array, _multiplicitiesAccu[i].Length == _multiplicities[i].Length + 1
		private int[][] _multiplicitiesAccu;

		// non-empty block position as a single integer (as if the block tensor with size 'BlockSize' is a vector)
		private long[] _blockIndex;

		// offset array of each non-empty block's start element's offset in 'Pointer', _blockOffset[i].Length == _blockIndex[i].Length + 1
		private long[] _blockOffset;

		// the block sparse array's block size, i.e. the size of '_offsets', '_subTensorLength'
		private readonly long[] blockSize;

		// accumulate product of 'blockSize', redundancy compared to 'blockSize', Length == Rank + 1
		private readonly long[] blockSizeProd;

		// the flows of each dimension
		private readonly bool[] flow;

		/// <summary>
		/// The function that actually implements the dispose functionality
		/// </summary>
		/// <param name="disposing">dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._multiplicities = null; this._charges = null; this._blockIndex = null; // necessary members
				this._blockOffset = null; this._multiplicitiesAccu = null; // redundant members
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Get the charges of this tensor. The outer array accounts for each dimension, the inner array accounts for charges of that dimension.
		/// </summary>
		public IReadOnlyList<IReadOnlyList<TC>> Charges => this._charges;

		/// <summary>
		/// Get the multiplicities of this tensor. The outer array accounts for each dimension, the inner array accounts for multiplicities of that dimension.
		/// </summary>
		public IReadOnlyList<IReadOnlyList<int>> Multiplicities => this._multiplicities;

		/// <summary>
		/// The number of non-zero blocks (sub-tensors) of this <see cref="BlockSparseTensor{T, TC}"/>
		/// </summary>
		public long NonZeroBlocks => this._blockIndex.LongLength;

		/// <summary>
		/// Get the size of tensor formed by / whose elements are the blocks of this <see cref="BlockSparseTensor{T, TC}"/>
		/// </summary>
		public IReadOnlyList<long> BlockSize => this.blockSize;

		/// <summary>
		/// The flow directions of this tensor for each leg. True means in, false means out.
		/// </summary>
		public IReadOnlyList<bool> FlowDirection => this.flow;
		#endregion


		#region creations
		/// <summary>
		/// Create a empty <see cref="BlockSparseTensor{T, TC}"/> with all properties 0 or null
		/// </summary>
		public BlockSparseTensor() : base(0, new[] { 0L }, true) { }

		private static long IndicesToActualLength(IEnumerable<IReadOnlyList<TC>> charges, IEnumerable<IReadOnlyList<int>> multiplicities, IReadOnlyList<bool> flows)
		{
			if (flows is null || flows.Count == 0)
				throw new ArgumentNullException(nameof(flows));
			if (charges is null || Linq.Count(charges) == 0)
				throw new ArgumentNullException(nameof(charges));
			if (Linq.Any(charges, l => l is null || l.Count != flows.Count))
				throw new ArgumentOutOfRangeException(nameof(charges));
			if (multiplicities is null || Linq.Count(charges) != Linq.Count(multiplicities))
				throw new ArgumentNullException(nameof(multiplicities));
			if (Linq.Any(Linq.Zip(multiplicities, charges, (m, l) => m.Count == l.Count), a => !a))
				throw new ArgumentOutOfRangeException(nameof(multiplicities));
			if (flows.Count > 1 && (flows.AllTrue() || flows.AllFalse()))
				throw new ArgumentOutOfRangeException(nameof(flows));

			return Linq.Sum(multiplicities, m => (long)m.Prod());
		}

		private static long[] IndicesToSize(IEnumerable<IReadOnlyList<TC>> charges, IEnumerable<IReadOnlyList<int>> multiplicities, int rank)
		{
			List<TC>[] distinctCharges = new List<TC>[rank];
			for (int i = 0; i < rank; i++)
			{
				distinctCharges[i] = new List<TC>();
			}
			Span<long> size = stackalloc long[rank];
			foreach (var (c, m) in Linq.Zip(charges, multiplicities))
			{
				for (int i = 0; i < rank; i++)
				{
					if (!distinctCharges[i].Contains(c[i]))
					{
						size[i] += m[i];
						distinctCharges[i].Add(c[i]);
					}
				}
			}
			return size.ToArray();
		}

		private static (int[][] mutiplicityCharges, TC[][] charges, long[] blockIndex, long[] blockOffsets, long[] blockSize) IndicesToOffsets(IEnumerable<IReadOnlyList<TC>> chargesEnum, IEnumerable<IReadOnlyList<int>> multiplicitiesEnum, int rank)
		{
			var charges = Linq.ToArray(chargesEnum);
			var multiplicities = Linq.ToArray(multiplicitiesEnum);
			// get block size and charge indices first
			Span<long> blockSize = stackalloc long[rank];
			Span<long> sizeProd = stackalloc long[rank + 1];
			int[][] chargeIndex = new int[rank][];
			sizeProd[0] = 1;
			for (int i = 0; i < rank; i++)
			{
				chargeIndex[i] = charges.Select(l => l[i]).ToDistinctIndex();
				blockSize[i] = chargeIndex[i].Max() + 1;
				sizeProd[i + 1] = sizeProd[i] * blockSize[i];
			}
			// get unsorted sparse representations and dense charges and corresponding size
			long[] blockIndex = new long[charges.Length];
			long[] sparseSubLength = new long[charges.Length];
			int[][] mutiplicityCharges = new int[rank][];
			TC[][] denseCharges = new TC[rank][];
			for (int i = 0; i < rank; i++)
			{
				mutiplicityCharges[i] = new int[blockSize[i]];
				denseCharges[i] = new TC[blockSize[i]];
			}
			for (int j = 0; j < charges.Length; j++)
			{
				for (int i = 0; i < rank; i++)
				{
					int chargeInd = chargeIndex[i][j];
					// update offset of sub-tensor
					blockIndex[j] += chargeInd * sizeProd[i];
					// update size corresponding to charge index
					// update charge
					if (mutiplicityCharges[i][chargeInd] != 0)
					{
						if (mutiplicityCharges[i][chargeInd] != multiplicities[j][i])
							throw new ArgumentOutOfRangeException(nameof(multiplicities), Resource.SizeInconsistent);
						if (!denseCharges[i][chargeInd].Equals(charges[j][i]))
							throw new ArgumentOutOfRangeException(nameof(charges), Resource.LabelInconsistant);
					}
					else
					{
						mutiplicityCharges[i][chargeInd] = multiplicities[j][i];
						denseCharges[i][chargeInd] = charges[j][i];
					}
				}
				// update length of sub-tensor
				sparseSubLength[j] = multiplicities[j].Prod();
			}
			// sort by offset in block-sized dense tensor
			blockIndex.SortWith(sparseSubLength);
			// return
			return (mutiplicityCharges, denseCharges, blockIndex, sparseSubLength.AccumulateSum().ToArray(), blockSize.ToArray());
		}

		private long[] GetBlockSizeProd() => this.blockSize.AccumulateProd().ToArray();

		/// <summary>
		/// Create a arbitrary valued charge-labeled tensor from given enumerable pairs <paramref name="charges"/> and <paramref name="multiplicities"/> to indicate the charge and multiplicity of each non-zero sparse tensor block.
		/// </summary>
		/// <param name="charges">The charges of each non-zero sparse tensor block</param>
		/// <param name="multiplicities">the multiplicities of <paramref name="charges"/></param>
		/// <param name="flows">the flow directions of the tensor to create, one for each rank</param>
		/// <param name="onHost">memory position, default on device</param>
		public BlockSparseTensor(IEnumerable<IReadOnlyList<TC>> charges, IEnumerable<IReadOnlyList<int>> multiplicities, IReadOnlyList<bool> flows, bool onHost = false) : base(IndicesToActualLength(charges, multiplicities, flows), IndicesToSize(charges, multiplicities, flows.Count), onHost)
		{
			this.flow = flows.ToCopiedArray();
			(this._multiplicities, this._charges, this._blockIndex, this._blockOffset, this.blockSize) = IndicesToOffsets(charges, multiplicities, this.Rank);
			this.UpdateMultiplicityAccu();
			this.blockSizeProd = this.GetBlockSizeProd();
		}

		// full alike array constructor
		private BlockSparseTensor(BlockSparseTensor<T, TC> alike, bool changeOnHost) : base(alike.ActualLength, alike.Size, changeOnHost ? !alike.OnHost : alike.OnHost)
		{
			this.flow = alike.flow.ToCopiedArray();
			this._multiplicities = alike._multiplicities;
			this._charges = alike._charges;
			this._blockIndex = alike._blockIndex;
			this._blockOffset = alike._blockOffset;
			this.blockSize = alike.blockSize;
			this._multiplicitiesAccu = alike._multiplicitiesAccu;
			this.blockSizeProd = alike.blockSizeProd;
			this._label = alike._label;
		}
		#endregion


		#region convert to / from DenseTensor<T>
		#region from dense
		private static long GetActualLength(IReadOnlyList<int[]> multiplicities, int[][] nonZeroBlockPositions)
		{
			if (multiplicities is null || multiplicities.Count == 0 || multiplicities.Any(m => m is null || m.Length == 0))
				throw new ArgumentNullException(nameof(multiplicities));
			if (multiplicities.Any(m => m.Any(mm => mm <= 0)))
				throw new ArgumentOutOfRangeException(nameof(multiplicities));
			int rank = multiplicities.Count;
			if (nonZeroBlockPositions is null || nonZeroBlockPositions.Length == 0 || nonZeroBlockPositions.Any(p => p is null || p.Length != rank))
				throw new ArgumentNullException(nameof(nonZeroBlockPositions));

			long length = 0;
			for (int i = 0; i < nonZeroBlockPositions.Length; i++)
			{
				long size = 1;
				for (int n = 0; n < rank; n++)
				{
					int pos = nonZeroBlockPositions[i][n];
					if (pos < 0)
						throw new ArgumentOutOfRangeException(nameof(nonZeroBlockPositions));
					size *= multiplicities[n][i]; 
				}
				length += size;
			}
			return length;
		}

		/// <summary>
		/// Create a arbitrary valued charge-labeled tensor from given <paramref name="flows"/>, <paramref name="charges"/> and <paramref name="multiplicities"/> of each dimension (for outer lists), and non-zero block' positions <paramref name="nonZeroBlockPositions"/>.
		/// </summary>
		/// <param name="flows">the flows of each dimension</param>
		/// <param name="charges">the charges of each dimension</param>
		/// <param name="multiplicities">the multiplicities of each dimension</param>
		/// <param name="onHost">the memory position, on host (CPU memory) or device (GPU memory)</param>
		/// <param name="nonZeroBlockPositions">the position list of all non-zero blocks (outer list accounts for non-zero blocks, inner one accounts for position array)</param>
		public BlockSparseTensor(IReadOnlyList<bool> flows, IReadOnlyList<TC[]> charges, IReadOnlyList<int[]> multiplicities, bool onHost = false, params int[][] nonZeroBlockPositions) : base(actualLength: GetActualLength(multiplicities, nonZeroBlockPositions), size: multiplicities.Select(m => (long)m.Sum()), onHost)
		{
			try
			{
				// other checks
				if (charges.Count != multiplicities.Count || !charges.SequenceEqual(multiplicities, (c, m) => c.Length == m.Length))
					throw new ArgumentException(Resource.SizeInconsistent, nameof(charges));
				if (charges.Any(c => !c.ElementsUnique()))
					throw new ArgumentException(Resource.DuplicateIndex, nameof(charges));
				// block size
				this.blockSize = Array.ConvertAll(this._multiplicities, m => m.LongLength);
				this.blockSizeProd = this.GetBlockSizeProd();
				// calculate and check block index
				this._blockIndex = new long[nonZeroBlockPositions.Length];
				for (int i = 0; i < nonZeroBlockPositions.Length; i++)
				{
					for (int n = 0; n < this.Rank; n++)
					{
						int pos = nonZeroBlockPositions[i][n];
						this._blockIndex[i] += pos * this.blockSizeProd[n];
					}
					if (this._blockIndex[..i].Contains(this._blockIndex[i]))
						throw new ArgumentException(Resource.DuplicateIndex, nameof(nonZeroBlockPositions));
				}
				// charge info
				this.flow = flows.ToCopiedArray();
				this._multiplicities = multiplicities.ToArray();
				this._charges = charges.ToArray();
				this.UpdateMultiplicityAccu();
				this.UpdateBlockOffset();
			}
			catch (Exception)
			{
				this.Dispose();
				throw;
			}
		}
		#endregion

		#region to dense
		/// <summary>
		/// Convert this tensor to <see cref="DenseTensor{T}"/>
		/// </summary>
		/// <returns>a new <see cref="DenseTensor{T}"/> that represents identical content as this one</returns>
		public DenseTensor<T> ToDenseTensor() => this.ToDenseTensor<DenseTensor<T>>();

		/// <summary>
		/// Convert this <see cref="BlockSparseTensor{T, TC}"/> to a concrete dense tensor <typeparamref name="TTen"/>.
		/// </summary>
		/// <typeparam name="TTen">the concrete dense tensor type</typeparam>
		/// <returns>a new <typeparamref name="TTen"/> that represents same content as this one</returns>
		/// <remarks>Currently, the returned <typeparamref name="TTen"/> will always be a matrix.</remarks>
		public TTen ToDenseTensor<TTen>()
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			// TODO: implement by slice setter for arbitrary-dimensional tensor
			int partition = ArrayLinq.Range(0, this.Rank).MinBy(p => Math.Abs(this.SizeProd[p] - this.Length / this.SizeProd[p]));
			using var matrix = this.Reshape(this.SizeProd[partition], this.Length / this.SizeProd[partition]);
			long leadDim = matrix.Size[0];
			var tensor = PureArrayFactory.Create<TTen, T>(matrix.Size, this.OnHost);
			try
			{
				if (this.Rank == 2)
				{   // can set label
					tensor.Label = this.Label;
				}
				tensor.FillWithZeros();
				for (int i = 0; i < matrix.NonZeroBlocks; i++)
				{
					long denseOffset = matrix.BlockIndexToDenseIndex(matrix._blockIndex[i]);
					long blockOffset = matrix._blockOffset[i];
					var (copyRows, copyCols) = matrix.BlockIndexToBlockSize_Matrix(matrix._blockIndex[i]);
					RT.CopyMatrixTo(source: matrix, dest: tensor,
									srcLD: copyRows, dstLD: leadDim,
									copyNRows: copyRows, copyNCols: copyCols,
									offsetSouceRow: blockOffset % copyRows, offsetSouceCol: blockOffset / copyRows,
									offsetDestRow: denseOffset % leadDim, offsetDestCol: denseOffset / leadDim);
				}
				return tensor;
			}
			catch (Exception)
			{
				tensor?.Dispose();
				throw;
			}
		}
		#endregion
		#endregion


		#region private methods
		#region updates
		private void UpdateMultiplicityAccu()
		{
			this._multiplicitiesAccu = new int[this.Rank][];
			for (int i = 0; i < this.Rank; i++)
			{
				this._multiplicitiesAccu[i] = this._multiplicities[i].AccumulateSum().ToArray();
			}
		}

		private void UpdateBlockOffset()
		{
			this._blockOffset = new long[this.NonZeroBlocks + 1];
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				long sizeProd = 1;
				for (int k = 0; k < this.Rank; k++)
				{
					long pos = GetPosition(this._blockIndex[i], blockSizeProd, blockSize, k);
					sizeProd *= this._multiplicities[k][pos];
				}
				this._blockOffset[i + 1] = this._blockOffset[i] + sizeProd;
			}
		}
		#endregion

		#region permute
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetBlockSizePermProd(long[] blockSize, int[] perm, Span<long> blockSizePermProd)
		{
			blockSizePermProd[0] = 1;
			for (int n = 0; n < perm.Length; n++)
			{
				blockSizePermProd[n + 1] = blockSizePermProd[n] * blockSize[perm[n]];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long BlockIndexPermute(long blockIndex, int[] perm, ReadOnlySpan<long> blockSizePermProd)
		{
			long newBlockIndex = 0;
			for (int i = 0; i < perm.Length; i++)
			{
				long ind = GetPosition(blockIndex, this.blockSizeProd, this.blockSize, perm[i]);
				newBlockIndex += ind * blockSizePermProd[i];
			}
			return newBlockIndex;
		}

		private long[] AllBlockIndexPermute(int[] perm)
		{
			if (perm.Length != this.Rank)
				throw new ArgumentOutOfRangeException(nameof(perm));

			Span<long> blockSizePermProd = stackalloc long[perm.Length + 1];
			GetBlockSizePermProd(this.blockSize, perm, blockSizePermProd);
			long[] newBlockIndex = new long[this.NonZeroBlocks];
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				newBlockIndex[i] = this.BlockIndexPermute(this._blockIndex[i], perm, blockSizePermProd);
			}
			Array.Sort(newBlockIndex);
			return newBlockIndex;
		}
		#endregion

		#region base position obtain
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetPosition(long index, long[] sizeProd, long[] size, int i)
		{
			return (index / sizeProd[i]) % size[i];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetPosition(long index, ReadOnlySpan<long> sizeProd, ReadOnlySpan<long> size, int i)
		{
			return (index / sizeProd[i]) % size[i];
		}
		#endregion

		#region get block info from block index
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void BlockIndexToBlockPosition(long blockIndex, Span<long> pos)
		{
			for (int i = 0; i < this.Rank; i++)
			{
				long ind = GetPosition(blockIndex, this.blockSizeProd, this.blockSize, i);
				pos[i] = ind;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void BlockIndexToBlockSize(long blockIndex, Span<long> size)
		{
			for (int i = 0; i < this.Rank; i++)
			{
				long ind = GetPosition(blockIndex, this.blockSizeProd, this.blockSize, i);
				size[i] = this._multiplicities[i][ind];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void BlockIndexToBlockCharge(long blockIndex, TC[] charge)
		{
			for (int i = 0; i < this.Rank; i++)
			{
				long ind = GetPosition(blockIndex, this.blockSizeProd, this.blockSize, i);
				charge[i] = this._charges[i][ind];
			}
		}
		#endregion
		#endregion


		#region equality
		private static bool StructureEquals(Lazy<long[]> blockIndexA, int[][] multiplicitiesA, TC[][] chargesA, bool[] flowA, Lazy<long[]> blockIndexB, int[][] multiplicitiesB, TC[][] chargesB, bool[] flowB)
		{
			return  // first check for fast comparison, if false, do the detailed comparison
					flowA.SequenceEqual(flowB) &&
					// detailed comparison
					(multiplicitiesA.SequenceEqual(multiplicitiesB) || multiplicitiesA.SequenceEqual(multiplicitiesB, BSTContractionInput<TC>.MultiplicityEqual)) &&
					(chargesA.SequenceEqual(chargesB) || chargesA.SequenceEqual(chargesB, BSTContractionInput<TC>.ChargeEqual)) &&
					// check the indices that may require complex computations
					blockIndexA.Value.SequenceEqual(blockIndexB.Value);
		}

		private static bool StructureEquals(long[] blockSizeA, Lazy<long[]> blockIndexA, int[][] multiplicitiesA, TC[][] chargesA, bool[] flowA, long[] blockSizeB, Lazy<long[]> blockIndexB, int[][] multiplicitiesB, TC[][] chargesB, bool[] flowB)
		{
			return blockSizeA.SequenceEqual(blockSizeB) &&
					StructureEquals(blockIndexA, multiplicitiesA, chargesA, flowA, blockIndexB, multiplicitiesB, chargesB, flowB);
		}

		private static bool StructureEquals(long[] blockIndexA, int[][] multiplicitiesA, TC[][] chargesA, bool[] flowA, long[] blockIndexB, int[][] multiplicitiesB, TC[][] chargesB, bool[] flowB)
		{
			return  // first check for fast comparison, if false, do the detailed comparison
					flowA.SequenceEqual(flowB) &&
					// detailed comparison
					(multiplicitiesA.SequenceEqual(multiplicitiesB) || multiplicitiesA.SequenceEqual(multiplicitiesB, BSTContractionInput<TC>.MultiplicityEqual)) &&
					(chargesA.SequenceEqual(chargesB) || chargesA.SequenceEqual(chargesB, BSTContractionInput<TC>.ChargeEqual)) &&
					// check the indices that may require complex computations
					blockIndexA.SequenceEqual(blockIndexB);
		}

		private static bool StructureEquals(long[] blockSizeA, long[] blockIndexA, int[][] multiplicitiesA, TC[][] chargesA, bool[] flowA, long[] blockSizeB, long[] blockIndexB, int[][] multiplicitiesB, TC[][] chargesB, bool[] flowB)
		{
			return blockSizeA.SequenceEqual(blockSizeB) &&
					StructureEquals(blockIndexA, multiplicitiesA, chargesA, flowA, blockIndexB, multiplicitiesB, chargesB, flowB);
		}

		/// <summary>
		/// Whether this tensor is equal to another <see cref="BlockSparseTensor{T, TC}"/>
		/// </summary>
		/// <param name="obj">another <see cref="BlockSparseTensor{T, TC}"/></param>
		public override bool Equals(object obj)
		{
			if (!(obj is BlockSparseTensor<T, TC>) || !base.Equals(obj))
				return false;
			var another = obj as BlockSparseTensor<T, TC>;
			return this.HasSameBlockStructure(another);
		}

		/// <summary>
		/// Get the hash code of this <see cref="BlockSparseTensor{T, TC}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(base.GetHashCode(), this.GetHashCodeOfStructure());
		}

		/// <summary>
		/// Get the hash code of the structure of this <see cref="BlockSparseTensor{T, TC}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public int GetHashCodeOfStructure()
		{
			return BSTContractionInput<TC>.HashCodeOf(this._charges, this._multiplicities, this._blockIndex);
		}

		/// <summary>
		/// Check if this <see cref="BlockSparseTensor{T, TC}"/> has same sparse block structure as <paramref name="another"/> tensor after permutation <paramref name="permuteOrder"/>.
		/// </summary>
		/// <param name="another">another <see cref="BlockSparseTensor{T, TC}"/> to check</param>
		/// <param name="permuteOrder">the permute order of <paramref name="another"/> as a <see cref="TensorOrder"/>, default means identity permutation</param>
		/// <returns>true if they share same sparse block structure</returns>
		public bool HasSameBlockStructure(BlockSparseTensor<T, TC> another, TensorOrder permuteOrder = default)
		{
			if (another is null || another.Length == 0)
				return false; // null check
			if (permuteOrder == default || permuteOrder == TensorOrder.Identity)
			{
				return ReferenceEquals(this, another) ||
					StructureEquals(this.blockSize, this._blockIndex, this._multiplicities, this._charges, this.flow,
									another.blockSize, another._blockIndex, another._multiplicities, another._charges, another.flow);
			}
			var order = permuteOrder.GetIntArrayOrder(another);
			return StructureEquals(this.blockSize, new Lazy<long[]>(this._blockIndex), this._multiplicities, this._charges, this.flow,
								another.blockSize.ReOrder(order), new Lazy<long[]>(() => another.AllBlockIndexPermute(order)),
								another._multiplicities.ReOrder(order), another._charges.ReOrder(order), another.flow.ReOrder(order));
		}
		#endregion


		#region clone and new array alike
		/// <summary>
		/// Deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override object Clone()
		{
			var clone = new BlockSparseTensor<T, TC>(this, changeOnHost: false);
			try
			{
				RT.CopyTo(this, clone);
				return clone;
			}
			catch (Exception)
			{
				clone?.Dispose();
				throw;
			}
		}


		private BlockSparseTensor(bool[] flow, long[] blockSize, long actualLength, IReadOnlyList<long> size, bool onHost) : base(actualLength, size, onHost)
		{
			this.flow = flow.ToCopiedArray();
			this.blockSize = blockSize;
			this.blockSizeProd = this.blockSize.AccumulateProd().ToArray();
		}

		/// <summary>
		/// Create a new array like this one (with same type and other info) while the data type is <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the new data type</typeparam>
		/// <returns>the new array</returns>
		public override PureArray<TOut> NewArrayAlike<TOut>()
		{
			var clone = new BlockSparseTensor<TOut, TC>(this.flow, this.blockSize, this.ActualLength, this.Size, this.OnHost);
			try
			{
				clone._charges = this._charges;
				clone._multiplicities = this._multiplicities;
				clone._multiplicitiesAccu = this._multiplicitiesAccu;
				clone._blockIndex = this._blockIndex;
				clone._blockOffset = this._blockOffset;
				clone._label = this._label;
				return clone;
			}
			catch (Exception)
			{
				clone?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new array with same sparse shape as this one.
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public override AbstractArray<T> NewArrayAlike()
		{
			return new BlockSparseTensor<T, TC>(this, changeOnHost: false);
		}

		/// <summary>
		/// Convert this array to the other memory.
		/// </summary>
		/// <returns>a new <see cref="PureArray{T}"/> with same value as this one</returns>
		public override PureArray<T> ToTheOtherMemory()
		{
			var clone = new BlockSparseTensor<T, TC>(this, changeOnHost: true);
			try
			{
				RT.CopyTo(this, clone);
				return clone;
			}
			catch (Exception)
			{
				clone?.Dispose();
				throw;
			}
		}
		#endregion


		#region linear algebra
		/// <summary>
		/// The last index of the vector
		/// </summary>
		public long LastIndex => this._blockIndex[^1];

		/// <summary>
		/// Scale this vector <b>in-place</b>, i.e. $\vec{v}_{\text{this}} = \alpha \vec{v}_{\text{this}}$.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public void Scale(T α) => BLAS.VectorScale(this, α);

		/// <summary>
		/// 2-norm of this vector, i.e. $\|\vec{v}\| = \sqrt{\sum_i{\vec{v}_i^2}}$.
		/// </summary>
		/// <returns>The 2-norm of this vector.</returns>
		public double Norm() => BLAS.VectorNorm(this);

		/// <summary>
		/// Normalize this vector <b>in-place</b> to make it norm-one, i.e. $\vec{v} = \vec{v} / \|\vec{v}\|$.
		/// </summary>
		public void Normalize() => BLAS.VectorScale(this, (1 / this.Norm()).FromDouble<T>());

		/// <summary>
		/// Manually check the block structures between this and <paramref name="other"/> for <see cref="Althea.General.IKrylovVector{TVec, T}"/> operations.
		/// </summary>
		/// <param name="other">another <see cref="BlockSparseTensor{T, TC}"/> to check block structure with</param>
		/// <returns>True if the block structures are the same, false otherwise.</returns>
		public bool KrylovVectorCheck(BlockSparseTensor<T, TC> other)
		{
			return this.HasSameBlockStructure(other);
		}

		/// <summary>
		/// Vector inner product.
		/// </summary>
		/// <param name="other">the other <see cref="BlockSparseTensor{T, TC}"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is symmetric (semi-symmetric, e.g. the conjugate relation, when data type is a complex type) for this vector and the other vector.</remarks>
		public T Dot(BlockSparseTensor<T, TC> other, bool? conjugateThis = null)
		{
			return BLAS.VectorDot(this, other, conjugateThis);
		}

		/// <summary>
		/// Compute in-place addition.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public void AddBy_αx(BlockSparseTensor<T, TC> x, T α)
		{
			BLAS.VectorAddBy(this, x, α);
		}

		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result "vector" <see cref="BlockSparseTensor{T, TC}"/>.
		/// </summary>
		/// <param name="notJoinedVecs">the columns of the matrix to operate</param>
		/// <param name="input">the input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <see cref="BlockSparseTensor{T, TC}"/>.</returns>
		/// <remarks>this method is actually static</remarks>
		public BlockSparseTensor<T, TC> OperateOn(IReadOnlyList<BlockSparseTensor<T, TC>> notJoinedVecs, T[] input)
		{
			if (notJoinedVecs is null || notJoinedVecs.Count == 0)
				throw new ArgumentNullException(nameof(notJoinedVecs));
			if (input is null || input.Length != notJoinedVecs.Count)
				throw new ArgumentNullException(nameof(input));

			// sort first
			var vecs = notJoinedVecs.ToCopiedArray();
			input = input.Clone() as T[];
			input.SortWith(vecs);
			// add then
			var tensor = this.NewArrayAlike() as BlockSparseTensor<T, TC>;
			try
			{
				tensor.FillWithZeros();
				for (int i = 0; i < input.Length; i++)
				{
					if (!input[i].IsZero())
						tensor.AddBy_αx(vecs[i], input[i]);
				}
				return tensor;
			}
			catch (Exception)
			{
				tensor?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Replace this tensor's content with <paramref name="another"/> <b>in-place</b>.
		/// </summary>
		/// <param name="another">another <see cref="BlockSparseTensor{T, TC}"/> to replace from</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="another"/> has different block structure as this one</exception>
		public void ReplaceBy(BlockSparseTensor<T, TC> another)
		{
			RT.CopyTo(source: another, dest: this);
		}

		/// <summary>
		/// The <b>out-of-place</b> conjugate operator for this tensor.
		/// </summary>
		/// <returns>the conjugate tensor, if <typeparamref name="T"/> is a real type, this tensor itself will be returned</returns>
		BlockSparseTensor<T, TC> ITensor<BlockSparseTensor<T, TC>, T>.ConjugateOutOfPlace()
		{
			var tensor = this.ConjugateOutOfPlace() as BlockSparseTensor<T, TC>;
			try
			{
				if (this.IsRealType)
					tensor = this.MakeReference(); // copy the FlowDirection
				tensor.DualInPlace();
				return tensor;
			}
			catch (Exception)
			{
				tensor?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// <b>In-place</b> dual this tensor without conjugate
		/// </summary>
		public void DualInPlace()
		{
			var flow = this.FlowDirection as bool[];
			for (int i = 0; i < this.Rank; i++)
			{
				flow[i] = !flow[i];
			}
		}
		#endregion


		#region reshape
		#region creations
		private void UpdateBlockOffset_Matrix()
		{
			this._blockOffset = new long[this.NonZeroBlocks + 1];
			ReadOnlySpan<long> blockSize = stackalloc[] { this.blockSize[0], this.blockSize[1] };
			ReadOnlySpan<long> blockSizeProd = stackalloc[] { this.blockSizeProd[0], this.blockSizeProd[1] };
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				#region unrolled loop of 2
				long pos = GetPosition(this._blockIndex[i], blockSizeProd, blockSize, 0);
				long sizeProd = this._multiplicities[0][pos];
				pos = GetPosition(this._blockIndex[i], blockSizeProd, blockSize, 1);
				sizeProd *= this._multiplicities[1][pos];
				#endregion
				this._blockOffset[i + 1] = this._blockOffset[i] + sizeProd;
			}
		}

		private static long GetActualLength_Matrix(int[][] multiplicities, long[] blockIndex)
		{
			int nonzeroBlocks = blockIndex.Length;
			ReadOnlySpan<long> blockSize = stackalloc[] { multiplicities[0].LongLength, multiplicities[1].LongLength };
			ReadOnlySpan<long> blockSizeProd = stackalloc[] { 1, multiplicities[0].LongLength };
			long length = 0;
			for (int i = 0; i < nonzeroBlocks; i++)
			{
				#region unrolled loop of 2
				long pos = GetPosition(blockIndex[i], blockSizeProd, blockSize, 0);
				long sizeProd = multiplicities[0][pos];
				pos = GetPosition(blockIndex[i], blockSizeProd, blockSize, 1);
				sizeProd *= multiplicities[1][pos];
				#endregion
				length += sizeProd;
			}
			return length;
		}

		// reshape to temporary matrix constructor
		private BlockSparseTensor(long length, bool onHost, long rows, long cols, long[] newBlockSize, long[] newBlockSizeProd, bool[] newFlows, TC[][] newCharges, int[][] newMultiplicities, long[] newBlockIndex) : base(actualLength: length, size: new[] { rows, cols }, onHost: onHost)
		{
			// block size
			this.blockSize = newBlockSize; this.blockSizeProd = newBlockSizeProd;
			// main
			this.flow = newFlows;
			this._charges = newCharges; this._multiplicities = newMultiplicities;
			this.UpdateMultiplicityAccu();
			// index
			this._blockIndex = newBlockIndex;
			// use special matrix one
			this.UpdateBlockOffset_Matrix();
		}

		// reshape to matrix constructor
		private BlockSparseTensor(bool onHost, long[] size, bool[] newFlows, TC[][] newCharges, int[][] newMultiplicities, long[] newBlockIndex) : base(actualLength: GetActualLength_Matrix(newMultiplicities, newBlockIndex), size: size, onHost: onHost)
		{
			// main
			this.flow = newFlows;
			this._charges = newCharges; this._multiplicities = newMultiplicities;
			this.UpdateMultiplicityAccu();
			// block size
			this.blockSize = new[] { newMultiplicities[0].LongLength, newMultiplicities[1].LongLength };
			this.blockSizeProd = this.GetBlockSizeProd();
			// index
			this._blockIndex = newBlockIndex;
			this.UpdateBlockOffset_Matrix();
		}

		// reshape to tensor constructor
		private BlockSparseTensor(long length, bool onHost, long[] size, bool[] newFlows, TC[][] newCharges, int[][] newMultiplicities) : base(actualLength: length, size: size, onHost: onHost)
		{
			// main
			this.flow = newFlows;
			this._charges = newCharges; this._multiplicities = newMultiplicities;
			this.UpdateMultiplicityAccu();
			// block size
			this.blockSize = Array.ConvertAll(newMultiplicities, m => m.LongLength);
			this.blockSizeProd = this.GetBlockSizeProd();
		}
		#endregion

		#region helper methods
		private static (TC[][] newCharge, int[][] newMultiplicity, bool[] newFlow) GetReshapeTensorChargeInfo(ReadOnlySpan<int> splits, bool[] flow, TC[][] charge, int[][] multiplicity, bool sortAndRemoveDuplicates = false)
		{
			int newRank = splits.Length - 1;
			TC[][] newCharges = new TC[newRank][];
			int[][] newMultiplicities = new int[newRank][];
			bool[] newFlows = new bool[newRank];
			for (int i = 0; i < newRank; i++)
			{
				// charges and multiplicities are simple
				TC[][] oldCharges;
				int[][] oldMultiplicities;
				// use largest charge' sign as the new flow
				bool[] oldFlow;
				oldCharges = charge[splits[i]..splits[i + 1]];
				oldMultiplicities = multiplicity[splits[i]..splits[i + 1]];
				oldFlow = flow[splits[i]..splits[i + 1]];
				TC largestCharge = default, smallestCharge = default;
				for (int j = 0; j < oldFlow.Length; j++)
				{
					largestCharge = oldFlow[j] ? largestCharge.Add(oldCharges[j][^1]) : largestCharge.Sub(oldCharges[j][0]);
					smallestCharge = oldFlow[j] ? smallestCharge.Add(oldCharges[j][0]) : smallestCharge.Sub(oldCharges[j][^1]);
				}
				newFlows[i] = largestCharge.CompareTo(smallestCharge.Dual()) >= 0;
				if (!newFlows[i])
				{
					oldFlow = Array.ConvertAll(oldFlow, f => !f);
				}
				// calculate outer add / product
				(newCharges[i], newMultiplicities[i]) = ChargeOperations.Outer(oldCharges, oldMultiplicities, oldFlow);
				// sort by charge and remove duplicates
				if (sortAndRemoveDuplicates && splits[i + 1] - splits[i] > 1)
				{	// can use unstable sort here
					newCharges[i].SortWith(newMultiplicities[i]);
					(newCharges[i], newMultiplicities[i], _) = RemoveDuplicates(newCharges[i], newMultiplicities[i]);
				}
			}
			return (newCharges, newMultiplicities, newFlows);
		}

		private static (TC[][] matrixCharge, int[][] matrixMultiplicity, bool[] matrixFlow, int[][] matrixPermutation) GetReshapeTempMatrix(int rank, int partition, bool[] flow, TC[][] charge, int[][] multiplicity)
		{
			ReadOnlySpan<int> splits = stackalloc[] { 0, partition, rank };
			// outer
			var (newCharges, newMultiplicities, newFlows) = GetReshapeTensorChargeInfo(splits, flow, charge, multiplicity);
			// sort
			int[][] perms = new int[2][];
			for (int n = 0; n < 2; n++)
			{
				perms[n] = newCharges[n].StableSortWithIndex();
				newMultiplicities[n] = newMultiplicities[n].ReOrder(perms[n]);
				// now, newCharge[i].ReOrder(perms[i]) == sortedNewCharge[i]
			}
			// return
			return (newCharges, newMultiplicities, newFlows, perms);
		}

		private static (TC[][] matrixCharge, int[][] matrixMultiplicity, bool[] matrixFlow, int[][] matrixPermutation) GetReshapeTempMatrix(IEnumerable<int[]> splits, bool[] flow, TC[][] charge, int[][] multiplicity, bool inversePerm)
		{
			// the multi-step approach is used since the temporary matrix generated in reshaping from original tensor must match the one generated in reshaping from destination tensor's
			int[][] perms = null;
			foreach (var split in splits)
			{
				// outer
				var (newCharges, newMultiplicities, newFlows) = GetReshapeTensorChargeInfo(split, flow, charge, multiplicity);
				// sort
				int rankNew = split.Length - 1;
				int[][] newPerms = new int[rankNew][];
				for (int n = 0; n < rankNew; n++)
				{
					if (split[n + 1] - split[n] == 1)
					{   // trivial permutation
						newPerms[n] = perms is null ? ArrayLinq.Range(0, newCharges[n].Length).ToArray() : perms[split[n]];
						continue;
					}
					// make newCharge[i].ReOrder(perms[i]) == sortedNewCharge[i]
					newPerms[n] = newCharges[n].StableSortWithIndex();
					newMultiplicities[n] = newMultiplicities[n].ReOrder(newPerms[n]);
					if (!(perms is null))
					{
						int[] permOrg2This = ChargeOperations.Outer(perms[split[n]..split[n + 1]]);
						// original --(permOrg2This)--> this --(newPerms[n])--> new
						newPerms[n] = permOrg2This.ReOrder(newPerms[n]);
					}
				}
				(charge, multiplicity, flow, perms) = (newCharges, newMultiplicities, newFlows, newPerms);
				if (rankNew == 2)
					break;
			}
			if (perms.Length != 2)
				throw new InvalidOperationException();
			if (inversePerm)
			{
				perms[0] = perms[0].InversePermutation();
				perms[1] = perms[1].InversePermutation();
			}
			return (charge, multiplicity, flow, perms);
		}

		private static (TC[] charge, int[] multiplicity, long[] blockRange) RemoveDuplicates(TC[] charge, int[] multiplicity)
		{
			int tempBlockSize = charge.Length;
			var newCharge = new List<TC>(tempBlockSize) { charge[0] };
			var newMultiplicity = new List<int>(tempBlockSize) { multiplicity[0] };
			var newBlockRange = new List<long>(charge.Length + 1) { 0 };
			for (int i = 1; i < tempBlockSize; i++)
			{
				if (newCharge[^1].Equals(charge[i]))
				{
					newMultiplicity[^1] += multiplicity[i];
				}
				else
				{
					newCharge.Add(charge[i]);
					newMultiplicity.Add(multiplicity[i]);
					newBlockRange.Add(i);
				}
			}
			newBlockRange.Add(tempBlockSize); // form a full range
			return (newCharge.ToArray(), newMultiplicity.ToArray(), newBlockRange.ToArray());
		}

		private static (TC[][] matrixCharge, int[][] matrixMultiplicity, long[][] matrixBlockRange) GetReshapeOutputMatrix(TC[][] tempCharge, int[][] tempMultiplicity, bool calculateChargeMultiplicity)
		{
			if (calculateChargeMultiplicity)
			{
				var charges = new TC[2][]; var multiplicities = new int[2][];
				var blockRange = new long[2][];
				for (int n = 0; n < 2; n++)
				{
					(charges[n], multiplicities[n], blockRange[n]) = RemoveDuplicates(tempCharge[n], tempMultiplicity[n]);
				}
				return (charges, multiplicities, blockRange);
			}
			else
			{
				var blockRange = new long[2][];
				for (int n = 0; n < 2; n++)
				{
					int tempBlockSize = tempCharge[n].Length;
					var newBlockRange = new List<long>(tempCharge[n].Length + 1) { 0 };
					TC lastCharge = tempCharge[n][0];
					for (int i = 1; i < tempBlockSize; i++)
					{
						if (!lastCharge.Equals(tempCharge[n][i]))
						{
							newBlockRange.Add(i);
							lastCharge = tempCharge[n][i];
						}
					}
					newBlockRange.Add(tempBlockSize); // form a full range
					blockRange[n] = newBlockRange.ToArray();
				}
				return (null, null, blockRange);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long row, long col) BlockIndexToBlockPosition_Matrix(long blockIndex)
		{
			return (GetPosition(blockIndex, this.blockSizeProd, this.blockSize, 0),
					GetPosition(blockIndex, this.blockSizeProd, this.blockSize, 1));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long row, long col) BlockIndexToBlockSize_Matrix(long blockIndex)
		{
			return (this._multiplicities[0][GetPosition(blockIndex, this.blockSizeProd, this.blockSize, 0)],
					this._multiplicities[1][GetPosition(blockIndex, this.blockSizeProd, this.blockSize, 1)]);
		}
		#endregion

		#region reshaping to matrix
		private BlockSparseTensor<T, TC> ReshapeToMatrix(long rows, long cols, IEnumerable<int[]> splits)
		{
			#region basics
			// warning if reshape to vector
			if (rows == 1 || cols == 1)
				Log.Write($"It is not recommended to reshape a {nameof(BlockSparseTensor<T, TC>)} to a vector since the charges will completely outer product and occupy way more memory than actual values.", level: LogLevel.Warning);
			#endregion

			#region get the partition position
			long newBlockNRows = 1, newBlockNCols = 0;
			{
				long prod = 1;
				for (int i = 0; i < this.Rank; i++)
				{
					prod *= this.Size[i];
					newBlockNRows *= this.blockSize[i];
					if (prod == rows)
					{
						newBlockNCols = this.blockSizeProd[^1] / newBlockNRows;
						break;
					}
				}
			}
			#endregion

			BlockSparseTensor<T, TC> temp = null;
			try
			{
				#region calculate new charges via additive outer product and new multiplicities via normal outer product
				var (newCharges, newMultiplicities, newFlows, permsThis2Temp) = GetReshapeTempMatrix(splits, this.flow, this._charges, this._multiplicities, inversePerm: true);
				#endregion

				#region get sparse vector index
				ReadOnlySpan<long> newBlockSize = stackalloc[] { newBlockNRows, newBlockNCols };
				ReadOnlySpan<long> newBlockSizeProd = stackalloc[] { 1, newBlockNRows, newBlockNRows * newBlockNCols };
				long[] newBlockIndex = new long[this.NonZeroBlocks];
				for (int i = 0; i < this.NonZeroBlocks; i++)
				{
					#region unrolled loop of 2
					// 'ind' is the row / column position of temporary matrix before sorting charges
					long ind = GetPosition(this._blockIndex[i], newBlockSizeProd, newBlockSize, 0);
					long copyToIndex = permsThis2Temp[0][ind] * newBlockSizeProd[0];
					ind = GetPosition(this._blockIndex[i], newBlockSizeProd, newBlockSize, 1);
					copyToIndex += permsThis2Temp[1][ind] * newBlockSizeProd[1];
					#endregion
					newBlockIndex[i] = copyToIndex;
				}
				#endregion

				#region sort and create
				// sort
				int[] permTemp2Org = newBlockIndex.SortWithIndex();
				// create temporary matrix
				temp = new BlockSparseTensor<T, TC>(length: this.ActualLength, this.OnHost, rows, cols, newBlockSize.ToArray(), newBlockSizeProd.ToArray(), newFlows, newCharges, newMultiplicities, newBlockIndex);
				temp.Pointer.Resize(temp._blockOffset[^1]); // since this.ActualLength may not equals to this._blockOffset[^1]
				#endregion

				#region copy to new temporary matrix
				for (int i = 0; i < temp.NonZeroBlocks; i++)
				{
					RT.CopyTo(source: this, dest: temp, length: temp._blockOffset[i + 1] - temp._blockOffset[i], offsetSource: this._blockOffset[permTemp2Org[i]], offsetDest: temp._blockOffset[i]);
				}
				#endregion
			}
			catch (Exception)
			{
				temp?.Dispose();
				throw;
			}

			using (temp)
			{
				#region get actual charges and multiplicities that merge duplicities
				var (charges, multiplicities, blockRange) = GetReshapeOutputMatrix(temp._charges, temp._multiplicities, calculateChargeMultiplicity: true);
				#endregion

				#region get actual sparse vector index
				long[][] permOutput2Temp; // the first element of sub-arrays of 'permOutput2Temp' is the super-block starting vector index at 'temp'
				long[] blockIndex;
				{
					long outputBlockNRows = charges[0].LongLength, outputBlockNCols = charges[1].LongLength;
					//tex: $\text{complexity} = O(n^\text{non-zero}_\text{block}(\text{this})\log{n_\text{all blocks}(\text{output})})$
					#region calculate
					var permNewTempDict = new Dictionary<long, List<long>>((int)temp.NonZeroBlocks);
					for (int i = 0; i < temp.NonZeroBlocks; i++)
					{
						var (posRow, posCol) = temp.BlockIndexToBlockPosition_Matrix(temp._blockIndex[i]);
						#region unroll loop of 2
						// n == 0
						int find = Array.BinarySearch(blockRange[0], posRow);
						if (find < 0) // see Array.BinarySearch
							find = ~find - 1;
						long superBlockSizeInTempBlocks = blockRange[0][find + 1] - blockRange[0][find];
						long newIndex = find * 1; // 1 == output.BlockSizeProd[0]
						long tempIndex = blockRange[0][find] * temp.blockSizeProd[0];
						// n == 1
						find = Array.BinarySearch(blockRange[1], posCol);
						if (find < 0) // see Array.BinarySearch
							find = ~find - 1;
						superBlockSizeInTempBlocks *= blockRange[1][find + 1] - blockRange[1][find];
						newIndex += find * outputBlockNRows; // outputBlockNRows == output.BlockSizeProd[1]
						tempIndex += blockRange[1][find] * temp.blockSizeProd[1];
						#endregion
						if (!permNewTempDict.ContainsKey(newIndex))
						{
							permNewTempDict.Add(newIndex, new List<long>(1 + (int)superBlockSizeInTempBlocks) { tempIndex });
						}
						permNewTempDict[newIndex].Add(i);
					}
					#endregion
					#region sort
					blockIndex = new long[permNewTempDict.Count];
					permNewTempDict.Keys.CopyTo(blockIndex, 0);
					var permOutput2TempList = new List<long>[permNewTempDict.Count];
					permNewTempDict.Values.CopyTo(permOutput2TempList, 0);
					permOutput2Temp = Array.ConvertAll(permOutput2TempList, l => l.ToArray());
					blockIndex.SortWith(permOutput2Temp);
					#endregion
				}
				#endregion

				#region create returning matrix and copy to it
				var output = new BlockSparseTensor<T, TC>(this.OnHost, new[] { rows, cols }, temp.flow, charges, multiplicities, blockIndex);
				try
				{
					output.FillWithZeros();

					#region copy to output matrix
					for (int i = 0; i < permOutput2Temp.Length; i++) // or output.NonZeroBlocks
					{
						var (superBlockSizeRow, superBlockSizeCol) = output.BlockIndexToBlockSize_Matrix(output._blockIndex[i]);
						var (posRow, posCol) = temp.BlockIndexToBlockPosition_Matrix(permOutput2Temp[i][0]);
						long destRowPosOrg = temp._multiplicitiesAccu[0][posRow], destColPosOrg = temp._multiplicitiesAccu[1][posCol];
						for (int j = 1; j < permOutput2Temp[i].Length; j++)
						{
							var (posNowRow, posNowCol) = temp.BlockIndexToBlockPosition_Matrix(temp._blockIndex[permOutput2Temp[i][j]]);
							// copy
							long copyRows = temp._multiplicities[0][posNowRow], copyCols = temp._multiplicities[1][posNowCol];
							long destRowPosNow = temp._multiplicitiesAccu[0][posNowRow], destColPosNow = temp._multiplicitiesAccu[1][posNowCol];
							RT.CopyMatrixTo(source: temp.Pointer + temp._blockOffset[permOutput2Temp[i][j]],
											dest: output.Pointer + output._blockOffset[i],
											srcLD: copyRows, dstLD: superBlockSizeRow,
											copyNRows: copyRows, copyNCols: copyCols,
											offsetDestRow: destRowPosNow - destRowPosOrg,
											offsetDestCol: destColPosNow - destColPosOrg);
							// offsets of source are always 0
						}
					}
					#endregion

					return output;
				}
				catch (Exception)
				{
					output?.Dispose();
					throw;
				}
				#endregion
			}
		}
		#endregion

		#region reshaping from matrix
		private static BlockSparseTensor<T, TC> ReshapeFromMatrix(BlockSparseTensor<T, TC> matrix, long originalLength, int partition, long[] size, bool[] flow, TC[][] charge, int[][] multiplicity)
		{
			#region get block ranges of row and column of temporary matrix
			int rank = size.Length;
			var (tempCharges, tempMultiplicities, tempFlows, permsTemp2Out) = GetReshapeTempMatrix(rank, partition, flow, charge, multiplicity);
			var (_, _, blockRange) = GetReshapeOutputMatrix(tempCharges, tempMultiplicities, calculateChargeMultiplicity: false);
			long[] blockRangeRow = blockRange[0], blockRangeCol = blockRange[1];
			// check if blockRange will be traversed
			if (blockRangeRow.Length != matrix.blockSize[0] + 1 || blockRangeCol.Length != matrix.blockSize[1] + 1)
				throw new ArgumentOutOfRangeException(nameof(charge), Resource.DuplicateIndex);
			#endregion

			#region copy to temporary matrix-like tensor
			long[] outputBlockIndex;
			// 2 * originalLength == estimated temp matrix's actual length
			using var temp = new BlockSparseTensor<T, TC>(2 * originalLength, matrix.OnHost, matrix.Size.ToArray(), tempFlows, tempCharges, tempMultiplicities);
			{
				int estimateTempNonzeroBlocks = 2 * (int)matrix.NonZeroBlocks;
				List<long> outputBlockIndexList = new(estimateTempNonzeroBlocks);
				List<long> tempBlockIndex = new(estimateTempNonzeroBlocks);
				List<long> tempBlockOffsets = new(estimateTempNonzeroBlocks + 1) { 0 };
				Althea.Blas.IBlas.DelegateAbsSum<T> absSumFunc = matrix.OnHost ? new Althea.Blas.IBlas.DelegateAbsSum<T>(BLAS.CPU.AbsSum) : BLAS.GPU.AbsSum;
				for (int i = 0; i < matrix.NonZeroBlocks; i++)
				{
					var (superBlockSizeRow, superBlockSizeCol) = matrix.BlockIndexToBlockSize_Matrix(matrix._blockIndex[i]);
					var (superBlockPosRow, superBlockPosCol) = matrix.BlockIndexToBlockPosition_Matrix(matrix._blockIndex[i]);
					int rowStart = (int)blockRangeRow[superBlockPosRow], rowEnd = (int)blockRangeRow[superBlockPosRow + 1];
					int colStart = (int)blockRangeCol[superBlockPosCol], colEnd = (int)blockRangeCol[superBlockPosCol + 1];
					long colOffset = 0;
					for (int c = colStart; c < colEnd; c++)
					{
						long blockSizeCol = tempMultiplicities[1][c];
						long rowOffset = 0;
						for (int r = rowStart; r < rowEnd; r++)
						{
							long blockSizeRow = tempMultiplicities[0][r];
							int copyLength = checked((int)(blockSizeRow * blockSizeCol));
							// enlarge temp.Pointer if needed
							while (tempBlockOffsets[^1] + copyLength > temp.ActualLength)
								temp.Pointer.Resize(temp.ActualLength * 2);
							// copy, offsets of destination are 0
							RT.CopyMatrixTo(source: matrix.Pointer + matrix._blockOffset[i],
											dest: temp.Pointer + tempBlockOffsets[^1],
											srcLD: superBlockSizeRow, dstLD: blockSizeRow,
											copyNRows: blockSizeRow, copyNCols: blockSizeCol,
											offsetSouceRow: rowOffset, offsetSouceCol: colOffset);
							// update sparse index and offset
							if (absSumFunc(copyLength, temp.Pointer + tempBlockOffsets[^1], 1).CompareTo(Scalars<T>.Zero) != 0)
							{   // only non-zero blocks are copied
								outputBlockIndexList.Add(permsTemp2Out[0][r] + permsTemp2Out[1][c] * tempCharges[0].Length); // ??
								tempBlockOffsets.Add(tempBlockOffsets[^1] + copyLength);
								tempBlockIndex.Add(r + c * tempCharges[0].Length);
							}
							// update row offset of matrix
							rowOffset += blockSizeRow;
						}
						// update column offset of matrix
						colOffset += blockSizeCol;
					}
				}
				// resize temporary matrix's storage
				temp.Pointer.Resize(tempBlockOffsets[^1]);
				temp._blockIndex = tempBlockIndex.ToArray();
				temp._blockOffset = tempBlockOffsets.ToArray();
				outputBlockIndex = outputBlockIndexList.ToArray();
			}
			#endregion

			#region copy to output tensor
			var output = new BlockSparseTensor<T, TC>(length: temp.ActualLength, matrix.OnHost, size, flow, charge, multiplicity);
			try
			{
				int nonzeroBlocks = outputBlockIndex.Length;
				// sort and update sparse info of output tensor
				output._blockIndex = outputBlockIndex;
				int[] permOutput2Temp = output._blockIndex.SortWithIndex();
				output.UpdateBlockOffset();
				// copy
				for (int i = 0; i < nonzeroBlocks; i++)
				{
					RT.CopyTo(source: temp, dest: output, length: output._blockOffset[i + 1] - output._blockOffset[i], offsetSource: temp._blockOffset[permOutput2Temp[i]], offsetDest: output._blockOffset[i]);
				}
				return output;
			}
			catch (Exception)
			{
				output?.Dispose();
				throw;
			}
			#endregion
		}
		#endregion

		#region API
		/// <summary>
		/// Reshape this tensor to a new presenting <paramref name="size"/>.
		/// </summary>
		/// <param name="size">size/extent of new tensor</param>
		/// <returns>the reshaped <b>new</b> <see cref="BlockSparseTensor{T, TC}"/> with given <paramref name="size"/></returns>
		/// <remarks>Only combinations of contiguous indices are valid for <see cref="BlockSparseTensor{T, TC}"/>. Notice that reshape is <b>path dependent</b> since zeros may be filled or multiple blocks may be spliced in the output tensor to match the block sizes directly obtained from this one.</remarks>
		/// <example>For example, if you want to combine rank-2 to rank-4 for a rank-5 tensor, you shall use:
		/// <code>
		/// var reshape = tensor.Reshape(tensor.Size[0], tensor.Size[1] * tensor.Size[2] * tensor.Size[3], tensor.Size[4]);
		/// </code>
		/// Rather than:
		/// <code>
		/// var reshape1 = tensor.Reshape(tensor.Size[0], tensor.Size[1] * tensor.Size[2], tensor.Size[3], tensor.Size[4]);
		/// var reshape = reshape1.Reshape(reshape1.Size[0], reshape1.Size[1] * reshape1.Size[2], reshape1.Size[3]);
		/// </code>
		/// The latter one may incorrectly add zeros between numbers that should be contiguous.</example>
		public new BlockSparseTensor<T, TC> Reshape(params long[] size)
		{
			#region check and get the split indices
			if (size.SequenceEqual(this.Size))
				return this.MakeReference();

			if (size.Prod() != this.Length)
				throw new ArgumentOutOfRangeException(nameof(size), Resource.LengthNotSame);
			if (size.Any(s => s <= 1))
				throw new ArgumentOutOfRangeException(nameof(size));

			int rank = size.Length;
			Span<int> splits = stackalloc int[rank + 1]; splits[0] = 0;
			{
				List<long> newBlockSizeList = new(rank + 1) { 1 };
				long prod = 1;
				int sizeInd = 0;
				for (int i = 0; i < this.Rank; i++)
				{
					prod *= this.Size[i];
					newBlockSizeList[^1] *= this.blockSize[i];
					if (prod > size[sizeInd])
						throw new ArgumentOutOfRangeException(nameof(size));
					if (prod == size[sizeInd])
					{
						splits[sizeInd + 1] = i + 1;
						newBlockSizeList.Add(1);
						prod = 1;
						sizeInd++;
					}
				}
				if (sizeInd != rank || newBlockSizeList.Prod() != this.blockSizeProd[^1])
					throw new ArgumentOutOfRangeException(nameof(size));
			}
			#endregion

			#region this tensor to matrix
			if (rank == 2)
			{
				var mat = this.ReshapeToMatrix(size[0], size[1], new[] { splits.ToArray() });
				mat.Trim();
				return mat;
			}
			// else
			// to make sure the output tensor can be reshaped to matrix of this size, we shall find partition in 'splits'
			// find best partition, a.k.a. the most square (in presenting and blocking) matrix partition
			long rows, cols;
			int partition = 0;
			double min = double.MaxValue;
			for (int i = 1; i < rank; i++)
			{
				double current = Math.Abs(this.SizeProd[splits[i]] - this.Length / this.SizeProd[splits[i]]) // for size
								+ this.blockSizeProd[splits[i]] + this.blockSizeProd[^1] / this.blockSizeProd[splits[i]]; // for block size
				if (current < min)
				{
					min = current; partition = i;
				}
			}
			rows = this.SizeProd[splits[partition]]; cols = this.Length / rows;
			// two-step reshape
			BlockSparseTensor<T, TC> matrix = this.ReshapeToMatrix(rows, cols, new[] { splits.ToArray(), new[] { 0, partition, rank } });
			#endregion

			#region output tensor from matrix
			using (matrix)
			{
				// get output tensor's flows, charges and multiplicities
				var (newCharge, newMultiplicity, newFlow) = GetReshapeTensorChargeInfo(splits, this.flow, this._charges, this._multiplicities, sortAndRemoveDuplicates: true);
				// reconstruct output tensor from intermediate matrix
				return ReshapeFromMatrix(matrix, this.ActualLength, partition, size, newFlow, newCharge, newMultiplicity);
			}
			#endregion
		}
		#endregion

		#region other API
		// full reference constructor
		private BlockSparseTensor(BlockSparseTensor<T, TC> reference) : base(reference, reference.ActualLength, reference.Size)
		{
			this.flow = reference.flow.ToCopiedArray();
			this._multiplicities = reference._multiplicities;
			this._charges = reference._charges;
			this._blockIndex = reference._blockIndex;
			this._blockOffset = reference._blockOffset;
			this.blockSize = reference.blockSize;
			this._multiplicitiesAccu = reference._multiplicitiesAccu;
			this.blockSizeProd = reference.blockSizeProd;
			this._label = reference._label;
		}

		/// <summary>
		/// Return a reference <see cref="BlockSparseTensor{T, TC}"/> of this one with same properties
		/// </summary>
		/// <returns>A reference <see cref="BlockSparseTensor{T, TC}"/> of this one</returns>
		public BlockSparseTensor<T, TC> MakeReference() => new(this);

		/// <summary>
		/// Flatten the array to a vector.
		/// </summary>
		/// <returns>The flattened vector as a <see cref="BlockSparseTensor{T, TC}"/></returns>
		public override PureArray<T> ToVector()
		{
			return this.Reshape(this.Length);
		}

		/// <summary>
		/// Reshape the array to a <see cref="BlockSparseTensor{T, TC}"/> with shape <c>(<paramref name="leadDim"/>, <see cref="AbstractArray{T}.Length">length</see> / <paramref name="leadDim"/>)</c>. Override <see cref="PureArray{T}.ToMatrix(long)"/>
		/// </summary>
		/// <param name="leadDim">leading dimension of matrix; if leadDim ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length">length</see>)</c>.</param>
		/// <returns>The reshaped matrix as a <see cref="BlockSparseTensor{T, TC}"/></returns>
		public override PureArray<T> ToMatrix(long leadDim = 0)
		{
			if (leadDim == 0)
				leadDim = Convert.ToInt64(Math.Sqrt(this.Length));
			return this.Reshape(leadDim, this.Length / leadDim);
		}

		/// <summary>
		/// Reshape the array to a <see cref="BlockSparseTensor{T, TC}"/> with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new dimensions. You can have one or zero uncertain dimension, indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor as a <see cref="BlockSparseTensor{T, TC}"/></returns>
		public override PureArray<T> ToTensor(params long[] size)
		{
			return this.Reshape(size);
		}
		#endregion

		#region trim
		/// <summary>
		/// Trim excess charges (charges with no block) of both ends for each dimension.
		/// </summary>
		public void Trim()
		{
			#region initial block size
			Span<long> blockSize = stackalloc long[this.Rank];
			for (int n = 0; n < this.Rank; n++)
			{
				blockSize[n] = this.blockSize[n];
			}
			#endregion

			#region trim
			// initialize max min
			Span<int> minPos = stackalloc int[this.Rank], maxPos = stackalloc int[this.Rank];
			for (int n = 0; n < this.Rank; n++)
			{
				minPos[n] = int.MaxValue; maxPos[n] = int.MinValue;
			}
			// calculate max min
			Span<long> pos = stackalloc long[this.Rank];
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				this.BlockIndexToBlockPosition(this._blockIndex[i], pos);
				for (int n = 0; n < this.Rank; n++)
				{
					if (pos[n] < minPos[n])
						minPos[n] = (int)pos[n];
					if (pos[n] > maxPos[n])
						maxPos[n] = (int)pos[n];
				}
			}
			// trim charges to [min, max]
			for (int n = 0; n < this.Rank; n++)
			{
				this._charges[n] = this._charges[n][minPos[n]..(maxPos[n] + 1)];
				this._multiplicities[n] = this._multiplicities[n][minPos[n]..(maxPos[n] + 1)];
			}
			this.UpdateMultiplicityAccu();
			// trim block size
			for (int n = 0; n < this.Rank; n++)
			{
				this.blockSize[n] = blockSize[n];
				this.blockSizeProd[n + 1] = this.blockSizeProd[n] * blockSize[n];
			}
			// trim sparse index
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				this.BlockIndexToBlockPosition(this._blockIndex[i], pos);
				long newIndex = 0;
				for (int n = 0; n < this.Rank; n++)
				{
					pos[n] -= minPos[n];
					newIndex += pos[n] * this.blockSizeProd[n];
				}
				this._blockIndex[i] = newIndex;
			}
			#endregion
		}
		#endregion
		#endregion


		#region indexer
		#region helper methods
		private (long blockIndex, long offset) DenseIndexToBlockIndex(long denseIndex)
		{
			Span<long> pos = stackalloc long[this.Rank];
			long[] sizeProd = this.SizeProd.ToArray(), size = this.Size.ToArray();
			for (int i = 0; i < this.Rank; i++)
			{
				pos[i] = GetPosition(denseIndex, sizeProd, size, i);
			}
			return this.DensePositionToBlockIndex(pos);
		}

		private (long blockIndex, long offset) DensePositionToBlockIndex(ReadOnlySpan<long> densePosition, int exclusiveFrom)
		{
			long blockIndex = 0, offset = 0;
			for (int n = 0; n < this.Rank; n++)
			{
				long posI = densePosition[n];
				int search = Array.BinarySearch(this._multiplicitiesAccu[n], (int)posI);
				if (search == ~this._multiplicitiesAccu[n].Length) // see Array.BinarySearch
					throw new ArgumentOutOfRangeException(nameof(densePosition));
				if (search < 0)
				{
					if (n < exclusiveFrom)
						search = (~search) - 1;
					else
						search = (~search);
				}
				if (search < 0)
					throw new ArgumentOutOfRangeException(nameof(densePosition));
				// this._multiplicitiesAccu[i][search] is just smaller than posI, which is the current sparse block's index at dimension 'i'
				blockIndex += search * this.blockSizeProd[n];
				long currentOffset = posI - this._multiplicitiesAccu[n][search];
				for (int j = 0; j < n; j++)
				{
					currentOffset *= this._multiplicities[j][search];
				}
				offset += currentOffset;
			}
			return (blockIndex, offset);
		}

		private (long blockIndex, long offset) DensePositionToBlockIndex(ReadOnlySpan<long> densePosition)
		{
			return this.DensePositionToBlockIndex(densePosition, exclusiveFrom: this.Rank);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int FindBlockIndex(long blockIndex)
		{
			return Array.BinarySearch(this._blockIndex, blockIndex);
		}

		private long GetIndexOffset(Index index)
		{
			long ind = index.GetPosition(this.Length);
			var (blockIndex, offset) = this.DenseIndexToBlockIndex(ind);
			int find = this.FindBlockIndex(blockIndex);
			if (find < 0)
				return -1;
			else
				return this._blockOffset[find] + offset;
		}
		#endregion

		#region dense indexers
		/// <summary>
		/// Get or set one element of this tensor at vector index (flattened array's index) <paramref name="index"/>.
		/// </summary>
		/// <param name="index">position in <see cref="Index"/> form</param>
		/// <returns>an instance of the data type <typeparamref name="T"/></returns>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position.</remarks>
		public T this[Index index] {
			get {
				long offset = this.GetIndexOffset(index);
				if (offset < 0)
					return default;
				else
					return RT.CopyOut(this, offset);
			}
			set {
				long offset = this.GetIndexOffset(index);
				if (offset < 0)
					throw new InvalidOperationException(Resource.InsertSparse);
				else
					RT.CopyInto(this, value, offset);
			}
		}

		private long GetIndexOffset(Index[] positions)
		{
			if (positions is null || positions.Length != this.Rank)
				throw new ArgumentNullException(nameof(positions));
			Span<long> pos = stackalloc long[this.Rank];
			for (int i = 0; i < this.Rank; i++)
			{
				pos[i] = positions[i].GetPosition(this.Size[i]);
			}
			var (blockIndex, offset) = this.DensePositionToBlockIndex(pos);
			int find = this.FindBlockIndex(blockIndex);
			if (find < 0)
				return -1;
			else
				return this._blockOffset[find] + offset;
		}

		/// <summary>
		/// Get or set an element in of this tensor.
		/// </summary>
		/// <param name="positions">the indices of each rank</param>
		/// <returns>the value at <paramref name="positions"/></returns>
		public T this[params Index[] positions] {
			get {
				long offset = this.GetIndexOffset(positions);
				if (offset < 0)
					return default;
				else
					return RT.CopyOut(this, offset);
			}
			set {
				long offset = this.GetIndexOffset(positions);
				if (offset < 0)
					throw new InvalidOperationException(Resource.InsertSparse);
				else
					RT.CopyInto(this, value, offset);
			}
		}
		#endregion

		#region block indexers
		private void GetSetBlockAt<TTen>(long index, ref TTen value, bool get)
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			var orgValue = value;
			// check exist
			int find = this.FindBlockIndex(index);
			if (find < 0)
			{
				if (get)
				{
					value = null; return;
				}
				else
				{
					throw new InvalidOperationException();
				}
			}
			// check size
			Span<long> size = stackalloc long[this.Rank];
			this.BlockIndexToBlockSize(index, size);
			bool copy = true;
			if (!size.SequenceEqual(value?.Size))
			{
				if (get)
				{
					value = PureArrayFactory.Reconstruct<TTen, T>(size.ToArray(),
						new Dictionary<string, IPointer>
						{
							[PointerName] = this.Pointer + this._blockOffset[find]
						}
					);
					copy = false;
				}
				else
				{
					throw new ArgumentException(Resource.SizeInconsistent, nameof(value));
				}
			}
			// copy
			try
			{
				if (get && copy)
				{
					RT.CopyTo(source: this, dest: value, offsetSource: this._blockOffset[find]);
				}
				else
				{
					RT.CopyTo(source: value, dest: this, offsetDest: this._blockOffset[find]);
				}
			}
			catch (Exception)
			{
				if (value != orgValue) value?.Dispose();
				throw;
			}
		}

		private long BlockIndexToDenseIndex(long blockIndex)
		{
			long denseIndex = 0;
			for (int i = 0; i < this.Rank; i++)
			{
				long posI = GetPosition(blockIndex, this.blockSizeProd, this.blockSize, i);
				denseIndex += this._multiplicitiesAccu[i][posI] * this.SizeProd[i];
			}
			return denseIndex;
		}

		/// <summary>
		/// Get the block's starting element's index in dense tensor (as a vector) of block at <paramref name="blockIndex"/>.
		/// </summary>
		/// <param name="blockIndex">the index of block in "vector of blocks"</param>
		/// <returns>the block's starting element's index as a <see cref="long"/></returns>
		public long BlockIndexToDenseIndex(Index blockIndex)
		{
			long index = blockIndex.GetPosition(this.blockSizeProd[^1]);
			return this.BlockIndexToDenseIndex(index);
		}

		/// <summary>
		/// Get the block at <paramref name="vectorIndex"/> which is the index of the block of all blocks reshaped to a general vector.
		/// </summary>
		/// <typeparam name="TTen">the output dense tensor type</typeparam>
		/// <param name="vectorIndex">the index of block in "vector of blocks"</param>
		/// <param name="overwrite">The <typeparamref name="TTen"/> to overwrite to. If it is null or it cannot be overwritten, a new <typeparamref name="TTen"/> will be return.</param>
		/// <returns>A <b>referenced</b> <typeparamref name="TTen"/> as the block or the <paramref name="overwrite"/>. If there is no block at <paramref name="vectorIndex"/>, returns null.</returns>
		public TTen GetBlockAtVectorIndex<TTen>(Index vectorIndex, TTen overwrite = null)
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			long index = vectorIndex.GetPosition(this.blockSizeProd[^1]);
			this.GetSetBlockAt(index, ref overwrite, get: true);
			return overwrite;
		}

		/// <summary>
		/// Set the block at <paramref name="vectorIndex"/> which is the index of the block of all blocks reshaped to a general vector.
		/// </summary>
		/// <typeparam name="TTen">the input dense tensor type</typeparam>
		/// <param name="vectorIndex">the index of block in "vector of blocks"</param>
		/// <param name="value">the <typeparamref name="TTen"/> to set</param>
		/// <exception cref="InvalidOperationException">if there is no block at <paramref name="vectorIndex"/></exception>
		/// <exception cref="ArgumentException">if the shape of <paramref name="value"/> mismatches the block to overwrite</exception>
		public void SetBlockAtVectorIndex<TTen>(Index vectorIndex, TTen value)
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			long index = vectorIndex.GetPosition(this.blockSizeProd[^1]);
			this.GetSetBlockAt(index, ref value, get: false);
		}

		/// <summary>
		/// Get the block's starting element's position in dense tensor of block at <paramref name="position"/>.
		/// </summary>
		/// <param name="position">the position of block in "tensor of blocks"</param>
		/// <returns>the block's starting element's position as a <see cref="IReadOnlyList{T}"/> of <see cref="long"/></returns>
		public IReadOnlyList<long> GetDensePositionAt(params Index[] position)
		{
			Span<long> index = stackalloc long[this.Rank];
			for (int n = 0; n < this.Rank; n++)
			{
				long pos = position[n].GetPosition(this.blockSize[n]);
				index[n] = this._multiplicitiesAccu[n][pos];
			}
			return index.ToArray();
		}

		/// <summary>
		/// Get the size of block at <paramref name="position"/>.
		/// </summary>
		/// <param name="position">the position of block in "tensor of blocks"</param>
		/// <returns>the block's size as a <see cref="IReadOnlyList{T}"/> of <see cref="int"/></returns>
		public IReadOnlyList<int> GetBlockSizeAt(params Index[] position)
		{
			Span<int> size = stackalloc int[this.Rank];
			for (int n = 0; n < this.Rank; n++)
			{
				long pos = position[n].GetPosition(this.blockSize[n]);
				size[n] = this._multiplicities[n][pos];
			}
			return size.ToArray();
		}

		/// <summary>
		/// Get the block at <paramref name="position"/> which is the position of the block of all blocks as a general tensor.
		/// </summary>
		/// <typeparam name="TTen">the output dense tensor type</typeparam>
		/// <param name="position">the position of block in "tensor of blocks"</param>
		/// <param name="overwrite">The <typeparamref name="TTen"/> to overwrite to. If it is null or it cannot be overwritten, a new <typeparamref name="TTen"/> will be return.</param>
		/// <returns>A <b>referenced</b> <typeparamref name="TTen"/> as the block or the <paramref name="overwrite"/>. If there is no block at <paramref name="position"/>, returns null.</returns>
		public TTen GetBlockAt<TTen>(TTen overwrite = null, params Index[] position)
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			long index = 0;
			for (int n = 0; n < this.Rank; n++)
			{
				long pos = position[n].GetPosition(this.blockSize[n]);
				index += this.blockSizeProd[n] * pos;
			}
			this.GetSetBlockAt(index, ref overwrite, get: true);
			return overwrite;
		}

		/// <summary>
		/// Set the block at <paramref name="position"/> which is the index of the block of all blocks as a general tensor.
		/// </summary>
		/// <typeparam name="TTen">the input dense tensor type</typeparam>
		/// <param name="position">the position of block in "tensor of blocks"</param>
		/// <param name="value">the <typeparamref name="TTen"/> to set</param>
		/// <exception cref="InvalidOperationException">if there is no block at <paramref name="position"/></exception>
		/// <exception cref="ArgumentException">if the shape of <paramref name="value"/> mismatches the block to overwrite</exception>
		public void SetBlockAt<TTen>(TTen value, params Index[] position)
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			long index = 0;
			for (int n = 0; n < this.Rank; n++)
			{
				long pos = position[n].GetPosition(this.blockSize[n]);
				index += this.blockSizeProd[n] * pos;
			}
			this.GetSetBlockAt(index, ref value, get: false);
		}

		/// <summary>
		/// Get the block position of corresponding <paramref name="charges"/>.
		/// </summary>
		/// <param name="charges">the charges of block</param>
		/// <returns>the block position as a <see cref="IReadOnlyList{T}"/> of <see cref="int"/></returns>
		public IReadOnlyList<int> GetBlockPositionOf(params TC[] charges)
		{
			int[] index = new int[this.Rank];
			for (int n = 0; n < this.Rank; n++)
			{
				int pos = Array.BinarySearch(this._charges[n], charges[n]);
				if (pos < 0)
					throw new ArgumentOutOfRangeException(nameof(charges));
				index[n] = pos;
			}
			return index;
		}

		/// <summary>
		/// Get the block with same <paramref name="charges"/>.
		/// </summary>
		/// <typeparam name="TTen">the output dense tensor type</typeparam>
		/// <param name="charges">the charges of block to get</param>
		/// <param name="overwrite">The <typeparamref name="TTen"/> to overwrite to. If it is null or it cannot be overwritten, a new <typeparamref name="TTen"/> will be return.</param>
		/// <returns>A <b>referenced</b> <typeparamref name="TTen"/> as the block or the <paramref name="overwrite"/>. If there is no block of same <paramref name="charges"/>, returns null.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if there are not block of same <paramref name="charges"/></exception>
		public TTen GetBlockOf<TTen>(TTen overwrite = null, params TC[] charges)
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			long index = 0;
			for (int n = 0; n < this.Rank; n++)
			{
				int pos = Array.BinarySearch(this._charges[n], charges[n]);
				if (pos < 0)
					throw new ArgumentOutOfRangeException(nameof(charges));
				index += this.blockSizeProd[n] * pos;
			}
			this.GetSetBlockAt(index, ref overwrite, get: true);
			return overwrite;
		}

		/// <summary>
		/// Set the block with same <paramref name="charges"/>.
		/// </summary>
		/// <typeparam name="TTen">the input dense tensor type</typeparam>
		/// <param name="charges">the charges of block to set</param>
		/// <param name="value">the <typeparamref name="TTen"/> to set</param>
		/// <exception cref="ArgumentOutOfRangeException">if there are not block of same <paramref name="charges"/></exception>
		/// <exception cref="InvalidOperationException">if the block of same <paramref name="charges"/> is empty and not stored</exception>
		/// <exception cref="ArgumentException">if the shape of <paramref name="value"/> mismatches the block to overwrite</exception>
		public void SetBlockOf<TTen>(TTen value, params TC[] charges)
			where TTen : PureArray<T>, ITensor<T>, IDenseArray<T>, new()
		{
			long index = 0;
			for (int n = 0; n < this.Rank; n++)
			{
				int pos = Array.BinarySearch(this._charges[n], charges[n]);
				if (pos < 0)
					throw new ArgumentOutOfRangeException(nameof(charges));
				index += this.blockSizeProd[n] * pos;
			}
			this.GetSetBlockAt(index, ref value, get: false);
		}
		#endregion

		#region tensor span indexer
		private (int indexFrom, int indexTo) CheckIndex(int firstNRank, Index[] restPos, Span<long> restPosition)
		{
			if (firstNRank <= 0 || firstNRank >= this.Rank)
				throw new ArgumentOutOfRangeException(nameof(firstNRank));

			// get start sparse index
			Span<long> densePosition = stackalloc long[this.Rank];
			for (int i = firstNRank, j = 0; i < this.Rank; i++, j++)
			{
				restPosition[j] = restPos[j].GetPosition(this.Size[i]);
				densePosition[i] = restPosition[j];
			}
			var (blockIndexStart, _) = this.DensePositionToBlockIndex(densePosition/*, exclusiveFrom: firstNRank*/);

			// increase position
			densePosition[firstNRank]++;
			for (int i = firstNRank; i < this.Rank - 1; i++)
			{
				if (densePosition[i] == this.Size[i])
				{
					densePosition[i] = 0;
					densePosition[i + 1]++;
				}
				else
					break;
			}
			// get end sparse index
			var (blockIndexEnd, _) = this.DensePositionToBlockIndex(densePosition, exclusiveFrom: firstNRank);

			// all blockIndex with value inside [blockIndexStart, blockIndexEnd) are the sub-tensors required
			int indexFrom = this.FindBlockIndex(blockIndexStart);
			if (indexFrom < 0) // set Array.BinarSearch
				indexFrom = (~indexFrom);
			int indexTo = this.FindBlockIndex(blockIndexEnd);
			if (indexTo < 0) // set Array.BinarSearch
				indexTo = (~indexTo);
			return (Math.Max(indexFrom, 0), Math.Max(indexTo, 0));
		}

		// span part to real span constructor
		private BlockSparseTensor(BlockSparseTensor<T, TC> original, int indexFrom, int indexTo, int firstNRank, long actualLength) : base(actualLength: actualLength, size: original.Size.Take(firstNRank), onHost: original.OnHost)
		{
			// basics
			this.flow = original.flow[..firstNRank];
			this.Label = original.Label.Take(firstNRank);
			// block size
			this.blockSize = original.blockSize[..firstNRank];
			this.blockSizeProd = original.blockSizeProd[..(firstNRank + 1)];
			// charge info
			this._multiplicities = original._multiplicities[..firstNRank];
			this._multiplicitiesAccu = original._multiplicitiesAccu[..firstNRank];
			this._charges = original._charges[..firstNRank];
			// block indices
			this._blockIndex = original._blockIndex[indexFrom..indexTo];
			this.UpdateBlockOffset();
		}

		/// <summary>
		/// Get the sub tensor formed by the first N rank of this tensor.
		/// </summary>
		/// <param name="firstNRank">first N ranks to set or get</param>
		/// <param name="restPos">rest of the tensor's rank's position <see cref="Index"/></param>
		/// <returns>the sub <see cref="BlockSparseTensor{T, TC}"/> of the <paramref name="firstNRank"/> at <paramref name="restPos"/></returns>
		public BlockSparseTensor<T, TC> GetSpan(int firstNRank, params Index[] restPos)
		{
			Span<long> restPosition = stackalloc long[restPos.Length];
			var (indexFrom, indexTo) = this.CheckIndex(firstNRank, restPos, restPosition);
			if (indexFrom == indexTo)
				return null;
			// get new output tensor's actual length
			long length = 0;
			for (int i = indexFrom; i < indexTo; i++)
			{
				Span<long> size = stackalloc long[this.Rank];
				this.BlockIndexToBlockSize(this._blockIndex[i], size);
				length += size[..firstNRank].Prod();
			}
			// get offset rest position
			{
				Span<long> blockPos = stackalloc long[this.Rank];
				this.BlockIndexToBlockPosition(this._blockIndex[indexFrom], blockPos);
				for (int n = firstNRank; n < this.Rank; n++)
				{
					restPosition[n - firstNRank] -= this._multiplicitiesAccu[n][blockPos[n]];
				}
			}
			// get new output tensor
			var output = new BlockSparseTensor<T, TC>(this, indexFrom, indexTo, firstNRank, length);
			try
			{
				Span<long> size = stackalloc long[this.Rank];
				// copy to this tensor
				for (int i = 0; i < output.NonZeroBlocks; i++)
				{
					// relative offset this block
					this.BlockIndexToBlockSize(this._blockIndex[i + indexFrom], size);
					long relativeOffset = 0, sizeProd = 1;
					for (int n = 0; n < this.Rank; n++)
					{
						if (n >= firstNRank)
						{
							relativeOffset += restPosition[n - firstNRank] * sizeProd;
						}
						sizeProd *= size[n];
					}
					RT.CopyTo(source: this, dest: output, length: output._blockOffset[i + 1] - output._blockOffset[i], offsetSource: this._blockOffset[i + indexFrom] + relativeOffset, offsetDest: output._blockOffset[i]);
				}
				return output;
			}
			catch (Exception)
			{
				output?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Set the sub tensor formed by the first N rank of this tensor.
		/// </summary>
		/// <param name="value">the value to set</param>
		/// <param name="firstNRank">first N ranks to set or get</param>
		/// <param name="restPos">rest of the tensor's rank's position <see cref="Index"/></param>
		/// <returns>the sub <see cref="BlockSparseTensor{T, TC}"/> of the <paramref name="firstNRank"/> at <paramref name="restPos"/></returns>
		public void SetSpan(BlockSparseTensor<T, TC> value, int firstNRank, params Index[] restPos)
		{
			Span<long> restPosition = stackalloc long[restPos.Length];
			var (indexFrom, indexTo) = this.CheckIndex(firstNRank, restPos, restPosition);
			// check
			var blockIndex = this._blockIndex[indexFrom..indexTo];
			var multiplicities = this._multiplicities[..firstNRank];
			var charges = this._charges[..firstNRank];
			var flow = this.flow[..firstNRank];
			if (!StructureEquals(blockIndex, multiplicities, charges, flow,
								value._blockIndex, value._multiplicities, value._charges, value.flow))
				throw new ArgumentException(Resource.SizeInconsistent, nameof(value));
			// copy to this tensor
			Span<long> size = stackalloc long[this.Rank];
			// copy to this tensor
			for (int i = 0; i < value.NonZeroBlocks; i++)
			{
				// relative offset this block
				this.BlockIndexToBlockSize(this._blockIndex[i + indexFrom], size);
				long relativeOffset = 0, sizeProd = 1;
				for (int n = 0; n < this.Rank; n++)
				{
					if (n >= firstNRank)
					{
						relativeOffset += restPosition[n - firstNRank] * sizeProd;
					}
					sizeProd *= size[n];
				}
				RT.CopyTo(source: value, dest: this, length: value._blockOffset[i + 1] - value._blockOffset[i], offsetSource: value._blockOffset[i], offsetDest: this._blockOffset[i + indexFrom] + relativeOffset);
			}
		}
		#endregion
		#endregion


		#region permute
		#region helper methods
		private (long[] blockIndex, long[] blockOffset) AllSparseInfoPermute(int[] perm)
		{
			if (perm.Length != this.Rank)
				throw new ArgumentOutOfRangeException(nameof(perm));

			Span<long> blockSizePermProd = stackalloc long[perm.Length + 1];
			GetBlockSizePermProd(this.blockSize, perm, blockSizePermProd);
			long[] newBlockIndex = new long[this.NonZeroBlocks];
			long[] newBlockOffset = new long[this.NonZeroBlocks];
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				newBlockIndex[i] = this.BlockIndexPermute(this._blockIndex[i], perm, blockSizePermProd);
				newBlockOffset[i] = this._blockOffset[i];
			}
			newBlockIndex.SortWith(newBlockOffset);
			return (newBlockIndex, newBlockOffset);
		}

		// permute constructor
		private BlockSparseTensor(BlockSparseTensor<T, TC> original, int[] permuteOrder, long[] newBlockIndex) : base(original.ActualLength, original.Size.ReOrder(permuteOrder), original.OnHost)
		{
			this.flow = original.flow.ReOrder(permuteOrder);
			this.blockSize = original.blockSize.ReOrder(permuteOrder);
			this.blockSizeProd = this.GetBlockSizeProd();
			this._blockIndex = newBlockIndex;
			this._charges = original._charges.ReOrder(permuteOrder);
			this._multiplicities = original._multiplicities.ReOrder(permuteOrder);
			this.UpdateBlockOffset();
			this.UpdateMultiplicityAccu();
		}
		#endregion

		#region API
		/// <summary>
		/// Permute <paramref name="other"/> by <paramref name="order"/> and replace to this tensor
		/// </summary>
		/// <param name="other">the tensor to be permuted</param>
		/// <param name="order">the new permutation <see cref="TensorOrder"/>, zero-based</param>
		/// <exception cref="ArgumentException">if <paramref name="other"/> under permute <paramref name="order"/> has incompatible size as this one</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="other"/> has incompatible block structure as this one</exception>
		public void Permute(BlockSparseTensor<T, TC> other, TensorOrder order)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			if (other.Rank != this.Rank)
				throw new ArgumentException(Resource.SizeInconsistent, nameof(other));

			int[] permuteOrder = order.GetIntArrayOrder(other);
			if (!other.Size.ReOrder(permuteOrder).SequenceEqual(this.Size))
				throw new ArgumentException(Resource.SizeInconsistent, nameof(other));
			if (!this.HasSameBlockStructure(other, permuteOrder))
				throw new ArgumentException(Resource.SizeInconsistent, nameof(other));

			var (_, blockOffset) = other.AllSparseInfoPermute(permuteOrder);
			long[] thisBlockSizeArray = new long[this.Rank];
			long[] otherBlockSizeArray = new long[this.Rank];
			Althea.Tensor.ITensor.DelegatePermute<T> func = this.OnHost ? new Althea.Tensor.ITensor.DelegatePermute<T>(TENSOR.CPU.Permute) : TENSOR.GPU.Permute;
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				// block of other tensor at blockOffset[i] permute to block of this tensor at this._blockOffset[i]
				Span<long> thisBlockSize = stackalloc long[this.Rank];
				Span<long> otherBlockSize = stackalloc long[this.Rank];
				this.BlockIndexToBlockSize(this._blockIndex[i], thisBlockSize);
				thisBlockSize.ReOrderTo(otherBlockSize, permuteOrder);
				thisBlockSize.CopyTo(thisBlockSizeArray); otherBlockSize.CopyTo(otherBlockSizeArray);
				// permute block
				func(A: other.Pointer + blockOffset[i], sizeA: otherBlockSizeArray, α: Scalars<T>.One, op: UnitaryOperation.Identity, B: this.Pointer + this._blockOffset[i], sizeB: thisBlockSizeArray, permAToB: permuteOrder);
			}
		}

		/// <summary>
		/// The permute operator of this tensor.
		/// </summary>
		/// <param name="order">the new permutation <see cref="TensorOrder"/>, zero-based</param>
		/// <returns>the result tensor, a new <see cref="BlockSparseTensor{T, TC}"/></returns>
		public BlockSparseTensor<T, TC> OperatorPermute(TensorOrder order)
		{
			int[] permuteOrder = order.GetIntArrayOrder(this);
			var (newBlockIndex, blockOffset) = this.AllSparseInfoPermute(permuteOrder);
			var tensor = new BlockSparseTensor<T, TC>(this, permuteOrder, newBlockIndex);
			try
			{
				Althea.Tensor.ITensor.DelegatePermute<T> func = this.OnHost ? new Althea.Tensor.ITensor.DelegatePermute<T>(TENSOR.CPU.Permute) : TENSOR.GPU.Permute;
				long[] thisBlockSizeArray = new long[this.Rank];
				long[] newBlockSizeArray = new long[this.Rank];
				for (int i = 0; i < this.NonZeroBlocks; i++)
				{
					// block of this tensor at blockOffset[i] permute to block of new tensor at its _blockOffset[i]
					Span<long> thisBlockSize = stackalloc long[this.Rank];
					Span<long> newBlockSize = stackalloc long[this.Rank];
					this.BlockIndexToBlockSize(this._blockIndex[i], thisBlockSize);
					thisBlockSize.ReOrderTo(newBlockSize, permuteOrder);
					thisBlockSize.CopyTo(thisBlockSizeArray); newBlockSize.CopyTo(newBlockSizeArray);
					// permute block
					func(A: this.Pointer + blockOffset[i], sizeA: thisBlockSizeArray, α: Scalars<T>.One, op: UnitaryOperation.Identity, B: tensor.Pointer + tensor._blockOffset[i], sizeB: newBlockSizeArray, permAToB: permuteOrder);
				}
				return tensor;
			}
			catch (Exception)
			{
				tensor?.Dispose();
				throw;
			}
		}
		#endregion
		#endregion


		#region contract
		#region structure compare
		/// <summary>
		/// Check if this <see cref="BlockSparseTensor{T, TC}"/> after partial permutation <paramref name="permuteOrderThis"/> has same sparse block structure as <paramref name="another"/> tensor after partial permutation <paramref name="permuteOrderAnother"/>.
		/// </summary>
		/// <param name="another">another <see cref="BlockSparseTensor{T, TC}"/> to check</param>
		/// <param name="permuteOrderThis">the permute order of this tensor, can have less elements than this tensor's rank</param>
		/// <param name="permuteOrderAnother">the permute order of <paramref name="another"/>, can have less elements than <paramref name="another"/>'s rank</param>
		/// <param name="sameFlow">whether this and <paramref name="another"/> shall have element-wise same or element-wise not <see cref="FlowDirection"/></param>
		/// <returns>true if they share same sparse block structure</returns>
		public bool HasSamePartialBlockStructure(BlockSparseTensor<T, TC> another, TensorOrder permuteOrderThis, TensorOrder permuteOrderAnother, bool sameFlow = true)
		{
			// null check
			if (another is null || another.Length == 0)
				return false;
			// permute order check
			var orderThis = permuteOrderThis.GetIntArrayOrder(this, allowPartial: true);
			if (orderThis.Length == 0)
				throw new ArgumentNullException(nameof(permuteOrderThis));
			var orderAnother = permuteOrderAnother.GetIntArrayOrder(this, allowPartial: true);
			if (orderAnother.Length == 0)
				throw new ArgumentNullException(nameof(permuteOrderAnother));

			var flowAnother = another.flow.ReOrder(orderAnother);
			if (!sameFlow)
				flowAnother = Array.ConvertAll(flowAnother, f => !f);
			return StructureEquals(this.blockSize.ReOrder(orderThis),
								   new Lazy<long[]>(() => this.BlockIndexPartialPermute(orderThis)),
								   this._multiplicities.ReOrder(orderThis),
								   this._charges.ReOrder(orderThis),
								   this.flow.ReOrder(orderThis),
								   another.blockSize.ReOrder(orderAnother),
								   new Lazy<long[]>(() => another.BlockIndexPartialPermute(orderAnother)),
								   another._multiplicities.ReOrder(orderAnother),
								   another._charges.ReOrder(orderAnother),
								   flowAnother);
		}

		private long[] BlockIndexPartialPermute(int[] perm)
		{
			Span<long> blockSizePermProd = stackalloc long[perm.Length + 1];
			GetBlockSizePermProd(this.blockSize, perm, blockSizePermProd);
			List<long> newBlockIndex = new((int)this.NonZeroBlocks);
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				long newVal = this.BlockIndexPermute(this._blockIndex[i], perm, blockSizePermProd);
				// since all missing indices in 'perm' are mapped to same sparse vector index now
				if (!newBlockIndex.Contains(newVal))
					newBlockIndex.Add(newVal);
			}
			newBlockIndex.Sort();
			return newBlockIndex.ToArray();
		}

		private long[] BlockIndexPartialPermute(int[] perm, Span<long> blockSizePermProd)
		{
			GetBlockSizePermProd(this.blockSize, perm, blockSizePermProd);
			List<long> newBlockIndex = new((int)this.NonZeroBlocks);
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				long newVal = this.BlockIndexPermute(this._blockIndex[i], perm, blockSizePermProd);
				// since all missing indices in 'perm' are mapped to same sparse vector index now
				if (!newBlockIndex.Contains(newVal))
					newBlockIndex.Add(newVal);
			}
			newBlockIndex.Sort();
			return newBlockIndex.ToArray();
		}
		#endregion

		#region helper method
		private int[] IndexOfBlockIndexPermute(int[] innerPerm, int[] outerPerm, out int freeLength)
		{
			// get size prod
			int len = innerPerm.Length + outerPerm.Length;
			Span<long> blockSizePermProd = stackalloc long[len + 1];
			blockSizePermProd[0] = 1;
			for (int n = 0; n < innerPerm.Length; n++)
			{
				blockSizePermProd[n + 1] = blockSizePermProd[n] * blockSize[innerPerm[n]];
			}
			freeLength = (int)blockSizePermProd[innerPerm.Length];
			for (int n = 0; n < outerPerm.Length; n++)
			{
				int realN = n + innerPerm.Length;
				blockSizePermProd[realN + 1] = blockSizePermProd[realN] * blockSize[outerPerm[n]];
			}
			// get index order
			long[] newBlockIndex = new long[this.NonZeroBlocks];
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				long newBlockIndexNow = 0;
				for (int n = 0; n < innerPerm.Length; n++)
				{
					long ind = GetPosition(this._blockIndex[i], this.blockSizeProd, this.blockSize, innerPerm[n]);
					newBlockIndexNow += ind * blockSizePermProd[n];
				}
				for (int n = 0; n < outerPerm.Length; n++)
				{
					int realN = n + innerPerm.Length;
					long ind = GetPosition(this._blockIndex[i], this.blockSizeProd, this.blockSize, outerPerm[n]);
					newBlockIndexNow += ind * blockSizePermProd[realN];
				}
				newBlockIndex[i] = newBlockIndexNow;
			}
			int[] index = newBlockIndex.SortWithIndex();
			return index;
		}
		#endregion

		#region get information of contraction output tensor
		private static BSTContractionOutput<TC> GetContractOutputTensor(BlockSparseTensor<T, TC> A, BlockSparseTensor<T, TC> B, Althea.Tensor.ContractionInput input, BSTContractionOutput<TC> basicInfo)
		{
			#region basic
			var labelC = basicInfo.labelC;
			var sizeC = basicInfo.sizeC;
			var blockSizeProdC = basicInfo.blockSizeProdC;
			Span<long> freeBlockSizeA = stackalloc long[input.LeftFreeIndex.Length];
			Span<long> freeBlockSizeB = stackalloc long[input.RightFreeIndex.Length];
			A.blockSize.ReOrderTo(freeBlockSizeA, input.LeftFreeIndex);
			B.blockSize.ReOrderTo(freeBlockSizeB, input.RightFreeIndex);
			#endregion

			#region check common / contraction parts' block structure
			var commonLabel = A.Label.Intersect(B.Label);
			int[] commonA = A.Label.FindPermutation(commonLabel), commonB = B.Label.FindPermutation(commonLabel);
			if (!A.HasSamePartialBlockStructure(B, commonA, commonB, sameFlow: false))
				throw new ArgumentException(Resource.SizeInconsistent);
			#endregion

			#region get multiplicities and charges of C
			int[][] multiplicitiesC = new int[sizeC.Length][];
			TC[][] chargesC = new TC[sizeC.Length][];
			for (int i = 0; i < input.LeftFreeIndex.Length; i++)
			{
				multiplicitiesC[input.OutLeftFreeIndex[i]] = A._multiplicities[input.LeftFreeIndex[i]];
				chargesC[input.OutLeftFreeIndex[i]] = A._charges[input.LeftFreeIndex[i]];
			}
			for (int i = 0; i < input.RightFreeIndex.Length; i++)
			{
				multiplicitiesC[input.OutRightFreeIndex[i]] = B._multiplicities[input.RightFreeIndex[i]];
				chargesC[input.OutRightFreeIndex[i]] = B._charges[input.RightFreeIndex[i]];
			}
			#endregion

			#region get the indices of 'blockIndex' of A and B when traversing contraction common loop
			int[] indicesA = A.IndexOfBlockIndexPermute(input.LeftFreeIndex, commonA, out int freeLengthA);
			int[] indicesB = B.IndexOfBlockIndexPermute(input.RightFreeIndex, commonB, out int freeLengthB);
			int commonLength = (int)A.NonZeroBlocks / freeLengthA;
			#endregion

			#region get sparse vector index of C
			List<long> blockIndexCList = new(freeLengthA * freeLengthB);
			List<int> indicesPermAList = new(freeLengthA * freeLengthB);
			List<int> indicesPermBList = new(freeLengthA * freeLengthB);
			// temp array on stack
			Span<long> posA = stackalloc long[A.Rank], posB = stackalloc long[B.Rank];
			Span<long> freePosA = stackalloc long[input.LeftFreeIndex.Length], freePosB = stackalloc long[input.RightFreeIndex.Length];
			Span<long> commonPosA = stackalloc long[commonA.Length], commonPosB = stackalloc long[commonB.Length];
			int rankC = basicInfo.flowC.Length;
			Span<long> posC = stackalloc long[rankC];
			// start nested loops
			for (int i = 0; i < freeLengthA; i++) // level 1, for (free part of A)
			{
				for (int j = 0; j < freeLengthB; j++) // level 2, for (free part of B)
				{
					int indexA = indicesA[i], indexB = indicesB[j];
					A.BlockIndexToBlockPosition(A._blockIndex[indexA], posA);
					B.BlockIndexToBlockPosition(B._blockIndex[indexB], posB);
					// check if this combination is permitted
					posA.ReOrderTo(commonPosA, commonA);
					posB.ReOrderTo(commonPosB, commonB);
					if (!commonPosA.SequenceEqual(commonPosB))
					{
						continue;
					}
					// if permitted
					posA.ReOrderTo(freePosA, input.LeftFreeIndex);
					posB.ReOrderTo(freePosB, input.RightFreeIndex);
					freePosA.InverseOrderTo(posC, input.OutLeftFreeIndex);
					freePosB.InverseOrderTo(posC, input.OutRightFreeIndex);
					long indexC = 0;
					for (int n = 0; n < rankC; n++)
					{
						indexC += posC[n] * basicInfo.blockSizeProdC[n];
					}
					blockIndexCList.Add(indexC);
					indicesPermAList.Add(i); indicesPermBList.Add(j);
				}
			}
			long[] blockIndexC = blockIndexCList.ToArray();
			int[] indicesPermA = indicesPermAList.ToArray(), indicesPermB = indicesPermBList.ToArray();
			blockIndexC.SortCopyWith(indicesPermA); blockIndexC.SortWith(indicesPermB);
			#endregion

			return new BSTContractionOutput<TC>(basicInfo, blockIndexC, multiplicitiesC, chargesC,
						new ContractionPlan(commonLength, input.LeftContractIndex.Length, indicesPermA, indicesPermB, indicesA, indicesB));
		}
		#endregion

		#region check of out-of-place contraction
		private static ContractionPlan CheckContraction(BlockSparseTensor<T, TC> A, BlockSparseTensor<T, TC> B, char[] labelC, out BlockSparseTensor<T, TC> C)
		{
			#region get flow, label and size of C
			var (tempLabelC, sizeC) = TENSOR.OutOfPlaceContractCheck(Scalars<T>.One, A, B, out int commonRank);
			if (labelC is null || labelC.Length == 0)
				labelC = tempLabelC;
			if (labelC.Length != tempLabelC.Length)
				throw new ArgumentException(Resource.LabelInconsistant, nameof(labelC));
			if (!tempLabelC.SequenceEqual(labelC))
			{
				Span<int> permute = stackalloc int[labelC.Length];
				bool success = tempLabelC.FindPermutationTo(labelC, permute);
				if (!success)
					throw new ArgumentException(Resource.LabelInconsistant, nameof(labelC));
				sizeC = sizeC.ReOrder(permute);
			}
			// get permutations
			Span<int> concA = stackalloc int[commonRank], concB = stackalloc int[commonRank];
			Span<int> freeA = stackalloc int[A.Rank - commonRank], freeCA = stackalloc int[freeA.Length];
			Span<int> freeB = stackalloc int[B.Rank - commonRank], freeCB = stackalloc int[freeB.Length];
			TENSOR.ContractCheck(A.Size, A.Label, B.Size, B.Label, sizeC, labelC, concA, concB, freeA, freeCA, freeB, freeCB);
			// get flow
			bool[] flowC = new bool[sizeC.Length];
			{
				Span<bool> temp = stackalloc bool[freeA.Length];
				A.FlowDirection.ReOrderTo(temp, freeA);
				temp.InverseOrderTo(flowC, freeCA);
				temp = stackalloc bool[freeB.Length];
				B.FlowDirection.ReOrderTo(temp, freeB);
				temp.InverseOrderTo(flowC, freeCB);
			}
			#endregion

			#region look up cache or add to cache
			BSTContractionInput<TC> addition = new(A._blockIndex, A._multiplicities, A._charges, A.flow, B._blockIndex, B._multiplicities, B._charges, B.flow);
			Althea.Tensor.ContractionCache<BSTContractionInput<TC>, BSTContractionOutput<TC>>.TryGet(A.Size.ToArray(), B.Size.ToArray(), sizeC, concA, concB, freeA, freeCA, freeB, freeCB, addition, out var planNullable, out var input);
			BSTContractionOutput<TC> outputInfo;
			if (planNullable.HasValue)
			{	// block structure of A and B checked when adding to cache
				outputInfo = planNullable.Value;
			}
			else
			{
				// get block size of C
				Span<long> freeBlockSizeA = stackalloc long[freeA.Length];
				Span<long> freeBlockSizeB = stackalloc long[freeB.Length];
				A.blockSize.ReOrderTo(freeBlockSizeA, freeA);
				B.blockSize.ReOrderTo(freeBlockSizeB, freeB);
				long[] blockSizeC = new long[freeBlockSizeA.Length + freeBlockSizeB.Length];
				for (int i = 0; i < freeBlockSizeA.Length; i++)
				{
					blockSizeC[freeCA[i]] = freeBlockSizeA[i];
				}
				for (int i = 0; i < freeBlockSizeB.Length; i++)
				{
					blockSizeC[freeCB[i]] = freeBlockSizeB[i];
				}
				long[] blockSizeProdC = blockSizeC.AccumulateProd().ToArray();
				// check structure of A and B and get output info
				outputInfo = GetContractOutputTensor(A, B, input, new BSTContractionOutput<TC>(flowC, sizeC, labelC, blockSizeC, blockSizeProdC));
				// put to cache
				Althea.Tensor.ContractionCache<BSTContractionInput<TC>, BSTContractionOutput<TC>>.Add(input, addition, outputInfo);
			}
			#endregion

			#region create C and return plan
			C = new BlockSparseTensor<T, TC>(flowC, labelC, sizeC, outputInfo.blockSizeC, outputInfo.blockSizeProdC, outputInfo.blockIndexC, outputInfo.multiplicitiesC, outputInfo.chargesC, A.OnHost);

			return outputInfo.plan;
			#endregion
		}
		#endregion

		#region create new block sparse tensor for contraction
		private static long BlockIndexAndMultiplicityToLength(long[] blockSize, long[] blockSizeProd, long[] blockIndex, int[][] multiplicity)
		{
			long length = 0;
			for (int i = 0; i < blockIndex.Length; i++)
			{
				long sizeProd = 1;
				for (int k = 0; k < multiplicity.Length; k++)
				{
					long pos = GetPosition(blockIndex[i], blockSizeProd, blockSize, k);
					sizeProd *= multiplicity[k][pos];
				}
				length += sizeProd;
			}
			return length;
		}

		private BlockSparseTensor(bool[] flow, char[] label, long[] size, long[] blockSize, long[] blockSizeProd, long[] blockIndex, int[][] multiplicity, TC[][] charge, bool onHost) : base(BlockIndexAndMultiplicityToLength(blockSize, blockSizeProd, blockIndex, multiplicity), size, onHost)
		{
			this.flow = flow; this._label = label;
			this.blockSize = blockSize; this.blockSizeProd = blockSizeProd;
			this._blockIndex = blockIndex;
			this._multiplicities = multiplicity;
			this.UpdateBlockOffset();
			this.UpdateMultiplicityAccu();
			this._charges = charge;
		}
		#endregion

		#region check of in-place contraction
		private static ContractionPlan CheckContraction(BlockSparseTensor<T, TC> A, BlockSparseTensor<T, TC> B, BlockSparseTensor<T, TC> C, int commonRank)
		{
			Span<int> concA = stackalloc int[commonRank], concB = stackalloc int[commonRank];
			Span<int> freeA = stackalloc int[A.Rank - commonRank], freeCA = stackalloc int[freeA.Length];
			Span<int> freeB = stackalloc int[B.Rank - commonRank], freeCB = stackalloc int[freeB.Length];
			TENSOR.ContractCheck(A.Size, A.Label, B.Size, B.Label, C.Size, C.Label, concA, concB, freeA, freeCA, freeB, freeCB);
			BSTContractionInput<TC> addition = new(A._blockIndex, A._multiplicities, A._charges, A.flow, B._blockIndex, B._multiplicities, B._charges, B.flow);
			Althea.Tensor.ContractionCache<BSTContractionInput<TC>, BSTContractionOutput<TC>>.TryGet(A.Size.ToArray(), B.Size.ToArray(), C.Size.ToArray(), concA, concB, freeA, freeCA, freeB, freeCB, addition, out var planNullable, out var input);
			bool cached = planNullable.HasValue;
			BSTContractionOutput<TC> outputInfo;
			if (cached)
			{   // block structure of A and B checked when adding to cache
				outputInfo = planNullable.Value;
			}
			else
			{
				try
				{
					outputInfo = GetContractOutputTensor(A, B, input, new BSTContractionOutput<TC>(C.flow, C.Size.ToArray(), C.Label.ToArray(), C.blockSize, C.blockSizeProd));
				}
				catch (Exception e)
				{
					throw new ArgumentException(Resource.SizeInconsistent, nameof(C), e);
				}
			}
			// check output info
			if (!StructureEquals(outputInfo.blockIndexC, outputInfo.multiplicitiesC, outputInfo.chargesC, outputInfo.flowC,
								 C._blockIndex, C._multiplicities, C._charges, C.flow))
				throw new ArgumentException(Resource.SizeInconsistent, nameof(C));
			if (!cached)
			{	// put to cache
				Althea.Tensor.ContractionCache<BSTContractionInput<TC>, BSTContractionOutput<TC>>.Add(input, addition, outputInfo);
			}
			return outputInfo.plan;
		}
		#endregion

		#region actual contraction
		private static void CalculateContract(BlockSparseTensor<T, TC> A, BlockSparseTensor<T, TC> B, BlockSparseTensor<T, TC> C, BlockSparseTensor<T, TC> D, T α, T β, ContractionPlan plan)
		{
			#region basics
			// re-calculate to get permutations on stack
			Span<int> concA = stackalloc int[plan.commonRank], concB = stackalloc int[plan.commonRank];
			Span<int> freeA = stackalloc int[A.Rank - plan.commonRank], freeCA = stackalloc int[C.Rank - freeA.Length];
			Span<int> freeB = stackalloc int[B.Rank - plan.commonRank], freeCB = stackalloc int[C.Rank - freeB.Length];
			TENSOR.ContractCheck(A.Size, A.Label, B.Size, B.Label, C.Size, C.Label, concA, concB, freeA, freeCA, freeB, freeCB);
			// other
			int freeLengthA = (int)A.NonZeroBlocks / plan.commonLength, freeLengthB = (int)B.NonZeroBlocks / plan.commonLength;
			Althea.Tensor.ITensor.DelegateContract<T> func = D.OnHost ? new Althea.Tensor.ITensor.DelegateContract<T>(TENSOR.CPU.Contract) : TENSOR.GPU.Contract;
			#endregion

			#region contraction
			long[] sizeArrayA = new long[A.Rank], sizeArrayB = new long[B.Rank], sizeArrayC = new long[D.Rank];
			// start nested loops
			for (int i = 0; i < D.NonZeroBlocks; i++)
			{
				// current offset and size of C
				long offsetC = D._blockOffset[i];
				Span<long> sizeC = stackalloc long[C.Rank];
				D.BlockIndexToBlockSize(D._blockIndex[i], sizeC);
				sizeC.CopyTo(sizeArrayC);
				// deal with C and D
				T beta = β;
				var pointerC = C.Pointer + offsetC;
				var pointerD = D.Pointer + offsetC;
				// loop for (common part of A and B)
				for (int m = 0; m < plan.commonLength; m++)
				{
					// current offset and size of A and B
					int indexA = plan.indicesA[m * freeLengthA + plan.indicesPermA[i]], indexB = plan.indicesB[m * freeLengthB + plan.indicesPermB[i]];
					long offsetA = A._blockOffset[indexA], offsetB = B._blockOffset[indexB];
					Span<long> sizeA = stackalloc long[A.Rank];
					Span<long> sizeB = stackalloc long[B.Rank];
					A.BlockIndexToBlockSize(A._blockIndex[indexA], sizeA);
					B.BlockIndexToBlockSize(B._blockIndex[indexB], sizeB);
					sizeA.CopyTo(sizeArrayA); sizeB.CopyTo(sizeArrayB);
					// actual contraction
					func(α, A.Pointer + offsetA, B.Pointer + offsetB, beta, pointerC, pointerD, sizeArrayA, sizeArrayB, sizeArrayC, concA, concB, freeA, freeCA, freeB, freeCB);
					// for summation
					if (m == 0)
					{
						beta = Scalars<T>.One;
						pointerC = pointerD;
					}
				}
			}
			#endregion
		}
		#endregion

		#region API
		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $\text{this}_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A"><see cref="BlockSparseTensor{T, TC}"/> A</param>
		/// <param name="B"><see cref="BlockSparseTensor{T, TC}"/> B</param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="C"><see cref="BlockSparseTensor{T, TC}"/> C, default null means this</param>
		/// <remarks>If <paramref name="C"/> is null, or <paramref name="β"/> is zero, this tensor itself will be used instead of <paramref name="C"/>.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="C"/> has different block structure as this one or <paramref name="A"/> and <paramref name="B"/> have incompatible block structure</exception>
		public void Contract(T α, BlockSparseTensor<T, TC> A, BlockSparseTensor<T, TC> B, T β = default, BlockSparseTensor<T, TC> C = null)
		{
			// quick check
			var D = this;
			TENSOR.InPlaceContractCheck(α, A, B, β, ref C, ref D, out int commonRank);
			// C, D structure equality check
			if (!this.HasSameBlockStructure(C))
				throw new ArgumentException(Resource.SizeInconsistent, nameof(C));
			// contraction check
			var plan = CheckContraction(A, B, this, commonRank);
			CalculateContract(A, B, C, D, α, β, plan);
		}

		/// <summary>
		/// Contraction operator for two tensors: this as left and <paramref name="right"/>.
		/// </summary>
		/// <param name="right">right operand</param>
		/// <param name="order">the order of the result tensor; if this parameter is null or empty, the order will be determined within</param>
		/// <returns>the contraction result, out-of-place</returns>
		/// <remarks>the <see cref="ITensor.Label"/> of operands will be utilized</remarks>
		public BlockSparseTensor<T, TC> OperatorContract(BlockSparseTensor<T, TC> right, params char[] order)
		{
			var plan = CheckContraction(this, right, order, out BlockSparseTensor<T, TC> C);
			try
			{
				CalculateContract(this, right, C, C, Scalars<T>.One, Scalars<T>.Zero, plan);
				return C;
			}
			catch (Exception)
			{
				C?.Dispose();
				throw;
			}
		}
		#endregion
		#endregion


		#region define operators
		/// <summary>
		/// Addition operator for two tensors.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="right">right operand</param>
		/// <returns>the addition result</returns>
		/// <remarks>the <see cref="Label"/> of operands will be ignored</remarks>
		public static BlockSparseTensor<T, TC> operator +(BlockSparseTensor<T, TC> left, BlockSparseTensor<T, TC> right)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left));
			if (right is null || right == EmptyDnTen)
				throw new ArgumentNullException(nameof(left));
			if (left.OnHost != right.OnHost)
				throw new ArgumentException(Resource.PosInconsistent);
			if (!left.HasSameBlockStructure(right))
				throw new ArgumentException(Resource.SizeInconsistent);

			return left.ApplyToClone(l =>
			{
				l.AddBy_αx(right, Scalars<T>.One);
			});
		}

		/// <summary>
		/// Subtraction operator for two tensors.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="right">right operand</param>
		/// <returns>the subtraction result</returns>
		/// <remarks>the <see cref="Label"/> of operands will be ignored</remarks>
		public static BlockSparseTensor<T, TC> operator -(BlockSparseTensor<T, TC> left, BlockSparseTensor<T, TC> right)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left));
			if (right is null || right == EmptyDnTen)
				throw new ArgumentNullException(nameof(left));
			if (left.OnHost != right.OnHost)
				throw new ArgumentException(Resource.PosInconsistent);
			if (!left.HasSameBlockStructure(right))
				throw new ArgumentException(Resource.SizeInconsistent);

			return left.ApplyToClone(l =>
			{
				l.AddBy_αx(right, Scalars<T>.MinusOne);
			});
		}

		/// <summary>
		/// Negation operator for a tensor.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <returns>the negation result</returns>
		/// <remarks>the <see cref="Label"/> of operand will be ignored</remarks>
		public static BlockSparseTensor<T, TC> operator -(BlockSparseTensor<T, TC> left)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left));

			return left.ApplyToClone(result => result.Scale(Scalars<T>.MinusOne));
		}

		/// <summary>
		/// Scaling operator for a tensor.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="α">the scalar to multiply</param>
		/// <returns>the scaling result</returns>
		/// <remarks>the <see cref="Label"/> of operand will be ignored</remarks>
		public static BlockSparseTensor<T, TC> operator *(BlockSparseTensor<T, TC> left, T α)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left));

			return left.ApplyToClone(result => result.Scale(α));
		}

		/// <summary>
		/// Scaling operator for a tensor.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="α">the scalar to multiply</param>
		/// <returns>the scaling result</returns>
		/// <remarks>the <see cref="Label"/> of operand will be ignored</remarks>
		public static BlockSparseTensor<T, TC> operator *(T α, BlockSparseTensor<T, TC> left) => left * α;

		/// <summary>
		/// Contraction operator for two tensors, <b>out-of-place</b>.
		/// </summary>
		/// <param name="left">left operand</param>
		/// <param name="right">right operand</param>
		/// <returns>the contraction result</returns>
		/// <remarks>the <see cref="Label"/> of operands will utilized</remarks>
		public static BlockSparseTensor<T, TC> operator *(BlockSparseTensor<T, TC> left, BlockSparseTensor<T, TC> right)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left));
			return left.OperatorContract(right);
		}
		#endregion


		#region matrix methods
		#region basics
		/// <summary>
		/// Multiply this tensor as a matrix with the <paramref name="right"/> tensor as another matrix.
		/// </summary>
		/// <param name="right">the other <see cref="BlockSparseTensor{T, TC}"/> as a matrix</param>
		/// <param name="partitionLeft">a <see cref="Index"/> to indicate the first <paramref name="partitionLeft"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="partitionRight">a <see cref="Index"/> to indicate the first <paramref name="partitionRight"/> (exclude) indices of tensor <paramref name="right"/> will be regarded as the row and others column</param>
		/// <param name="leftOp">the <see cref="MatrixOperation"/> to apply on this one</param>
		/// <param name="rightOp">the <see cref="MatrixOperation"/> to apply on <paramref name="right"/></param>
		/// <returns>The <b>out-of-place</b> multiplication result as a tensor whose <see cref="AbstractArray{T}.Size">size</see> is the same as corresponding <see cref="ITensor{TTen, T}.OperatorContract(TTen, char[])"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partitionLeft"/> or <paramref name="partitionRight"/> is out of range</exception>
		public BlockSparseTensor<T, TC> OperatorMatrixMultiply(BlockSparseTensor<T, TC> right, Index partitionLeft, Index partitionRight, MatrixOperation leftOp = MatrixOperation.None, MatrixOperation rightOp = MatrixOperation.None)
		{
			#region basics
			// simple check
			if (this.OnHost != right.OnHost)
				throw new ArgumentException(Resource.PosInconsistent, nameof(right));
			int pl = (int)partitionLeft.GetPosition(this.Rank);
			int pr = (int)partitionRight.GetPosition(this.Rank);
			// check size
			{
				var (m, n) = (this.SizeProd[pl], this.Length / this.SizeProd[pl]);
				long kl = leftOp == MatrixOperation.None ? n : m;
				var (p, q) = (right.SizeProd[pr], right.Length / right.SizeProd[pr]);
				long kr = rightOp == MatrixOperation.None ? p : q;
				if (kl != kr)
					throw new ArgumentException(Resource.SizeInconsistent, nameof(right));
			}
			// block structure checked in CheckContractBlockStructure
			#endregion

			#region set labels
			var storeLabelL = this.Label; var storeLabelR = right.Label;
			this.SetLabel(ArrayLinq.Range('a', this.Rank).ToArray());
			if (leftOp == MatrixOperation.None && rightOp == MatrixOperation.None)
				right.SetLabel(ArrayLinq.Range((char)('a' + pl), right.Rank).ToArray());
			else if (leftOp != MatrixOperation.None && rightOp == MatrixOperation.None)
				right.SetLabel(ArrayLinq.Range('a', pr).Concat(ArrayLinq.Range((char)('a' + this.Rank), right.Rank - pr)).ToArray());
			else if (leftOp == MatrixOperation.None && rightOp != MatrixOperation.None)
				right.SetLabel(ArrayLinq.Range((char)('a' + this.Rank), right.Rank - pr).Concat(ArrayLinq.Range((char)('a' + pl), pr)).ToArray());
			else
				right.SetLabel(ArrayLinq.Range((char)('a' + this.Rank), right.Rank - pr).Concat(ArrayLinq.Range('a', pl)).ToArray());
			char[] outLabel = this.Label.Except(right.Label).Concat(right.Label.Except(this.Label)).ToArray();
			#endregion

			#region calculate
			var plan = CheckContraction(this, right, outLabel, out BlockSparseTensor<T, TC> output);
			try
			{
				#region basics
				var A = this; var B = right; var C = output;
				int freeLengthA = (int)A.NonZeroBlocks / plan.commonLength, freeLengthB = (int)B.NonZeroBlocks / plan.commonLength;
				Althea.Blas.IBlas.DelegateGeneralMatricesMultiply<T> func = C.OnHost ? new Althea.Blas.IBlas.DelegateGeneralMatricesMultiply<T>(BLAS.CPU.GeneralMatricesMultiply) : BLAS.GPU.GeneralMatricesMultiply;
				#endregion

				#region contraction
				// start nested loops
				Span<long> sizeA = stackalloc long[A.Rank];
				Span<long> sizeB = stackalloc long[B.Rank];
				for (int i = 0; i < C.NonZeroBlocks; i++)
				{
					// current offset and size of C
					long offsetC = C._blockOffset[i];
					// deal with C and D
					T beta = Scalars<T>.Zero;
					var pointerC = C.Pointer + offsetC;
					// loop for (common part of A and B)
					for (int j = 0; j < plan.commonLength; j++)
					{
						// current offset and size of A and B
						int indexA = plan.indicesA[j * freeLengthA + plan.indicesPermA[i]], indexB = plan.indicesB[j * freeLengthB + plan.indicesPermB[i]];
						long offsetA = A._blockOffset[indexA], offsetB = B._blockOffset[indexB];
						A.BlockIndexToBlockSize(A._blockIndex[indexA], sizeA);
						B.BlockIndexToBlockSize(B._blockIndex[indexB], sizeB);
						var (rowA, colA) = (sizeA[..pl].Prod(), sizeA[pl..].Prod());
						var (rowB, colB) = (sizeB[..pr].Prod(), sizeB[pr..].Prod());
						int m = (int)(leftOp == MatrixOperation.None ? rowA : colA);
						int n = (int)(rightOp == MatrixOperation.None ? colB : rowB);
						int k = (int)(rowA * colA / m);
						// actual contraction
						func(leftOp, rightOp, m, n, k, Scalars<T>.One, A.Pointer + offsetA, (int)rowA, B.Pointer + offsetB, (int)rowB, beta, pointerC, m);
						if (j == 0)
						{   // for summation
							beta = Scalars<T>.One;
						}
					}
				}
				#endregion

				return C;
			}
			catch (Exception)
			{
				output?.Dispose();
				throw;
			}
			finally
			{
				this.Label = storeLabelL; right.Label = storeLabelR;
			}
			#endregion
		}

		/// <summary>
		/// (Conjugate) transpose this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="conjugate">conjugate or not, default null means true for complex type (<see cref="IComplex{T}"/>)</param>
		/// <returns>the (conjugate) transpose of this tensor with <c>Size = this.Size[<paramref name="partition"/>..] concatenate this.Size[..<paramref name="partition"/>]</c></returns>
		public BlockSparseTensor<T, TC> Transpose(Index partition, bool? conjugate = null)
		{
			int p = (int)partition.GetPosition(this.Rank);
			MatrixOperation op = (conjugate ?? !this.IsRealType) ? MatrixOperation.ConjugateTranspose : MatrixOperation.Transpose;
			int[] permuteOrder = ArrayLinq.Range(p, this.Rank - p).Concat(ArrayLinq.Range(0, p)).ToArray();
			var (newBlockIndex, blockOffset) = this.AllSparseInfoPermute(permuteOrder);
			var tensor = new BlockSparseTensor<T, TC>(this, permuteOrder, newBlockIndex);
			try
			{
				Althea.Blas.IBlas.DelegateGeneralMatricesAdd<T> func = this.OnHost ? new Althea.Blas.IBlas.DelegateGeneralMatricesAdd<T>(BLAS.CPU.GeneralMatricesAdd) : BLAS.GPU.GeneralMatricesAdd;
				for (int i = 0; i < this.NonZeroBlocks; i++)
				{
					// block of this tensor at blockOffset[i] permute to block of new tensor at its _blockOffset[i]
					Span<long> thisBlockSize = stackalloc long[this.Rank];
					this.BlockIndexToBlockSize(this._blockIndex[i], thisBlockSize);
					int ldc = (int)thisBlockSize[p..].Prod(), sdc = (int)thisBlockSize[..p].Prod();
					// permute block
					func(opA: op, opB: MatrixOperation.None, m: ldc, n: sdc,
						 α: Scalars<T>.One, A: this.Pointer + this._blockOffset[i], lda: sdc,
						 β: Scalars<T>.Zero, B: default, ldb: 1,
						 C: tensor.Pointer + this._blockOffset[i], ldc: ldc);
				}
				return tensor;
			}
			catch (Exception)
			{
				tensor?.Dispose();
				throw;
			}
		}
		#endregion

		#region checks
		/// <summary>
		/// Manually check the block diagonal structure of this matrix-like tensor. Requires the multiplicities to be the same. Shall be invoked before calling <see cref="Trace"/> or <see cref="EigenvalueShift(T)"/> if you want to check first.
		/// </summary>
		/// <returns>True if this matrix-like tensor is block diagonal, false otherwise.</returns>
		public bool DiagonalOperationCheck()
		{
			return InnerDiagonalOperationCheck(true);
		}

		private bool InnerDiagonalOperationCheck(bool equalMultiplicity = true, bool? allRowLargerThanCol = null)
		{
			if (this.Rank != 2)
				return false;
			if (equalMultiplicity && this.Size[0] != this.Size[1])
				return false;
			if (this.blockSize[0] != this.blockSize[1] || this.blockSize[0] != this.NonZeroBlocks)
				return false;
			if (!this._charges[0].SequenceEqual(this._charges[1]))
				return false;
			if (equalMultiplicity && !this._multiplicities[0].SequenceEqual(this._multiplicities[1]))
				return false;
			if (allRowLargerThanCol.HasValue)
			{
				if (allRowLargerThanCol.Value && !this._multiplicities[0].SequenceEqual(this._multiplicities[1], (r, c) => r >= c))
					return false;
				if (!allRowLargerThanCol.Value && !this._multiplicities[0].SequenceEqual(this._multiplicities[1], (r, c) => r <= c))
					return false;
			}
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				var (rowPos, colPos) = this.BlockIndexToBlockPosition_Matrix(this._blockIndex[i]);
				if (rowPos != colPos)
					return false;
			}
			return true;
		}
		#endregion

		#region decompositions
		private (DenseVector<T>[] S, BlockSparseTensor<T, TC> U, BlockSparseTensor<T, TC> Vct) SingularValues(int p, bool calcU, bool calcV, bool returnMatrixUV = false)
		{
			#region reshape to matrix and check block diagonal
			using var matrix = this.Reshape(this.SizeProd[p], this.Length / this.SizeProd[p]);
			matrix.InnerDiagonalOperationCheck(equalMultiplicity: false);
			#endregion

			#region get empty block sparse matrix U and V
			BlockSparseTensor<T, TC> matrixU = null, matrixV = null;
			try
			{
				int[] multiplicityS = matrix._multiplicities[0].Zip(matrix._multiplicities[1], (a, b) => Math.Min(a, b)).ToArray();
				long lengthU = matrix._multiplicities[0].Zip(multiplicityS, (a, b) => a * b).Sum();
				long lengthV = multiplicityS.Zip(matrix._multiplicities[1], (a, b) => a * b).Sum();
				long middleSize = multiplicityS.Sum();
				matrixU = calcU ? new BlockSparseTensor<T, TC>(
									length: lengthU, onHost: this.OnHost,
									rows: matrix.Size[0], cols: middleSize,
									newBlockSize: matrix.blockSize, newBlockSizeProd: matrix.blockSizeProd,
									newFlows: matrix.flow, newCharges: matrix._charges,
									newMultiplicities: new[] { matrix._multiplicities[0], multiplicityS },
									newBlockIndex: matrix._blockIndex)
							: null;
				matrixV = calcV ? new BlockSparseTensor<T, TC>(
									length: lengthV, onHost: this.OnHost,
									rows: middleSize, cols: matrix.Size[1],
									newBlockSize: matrix.blockSize, newBlockSizeProd: matrix.blockSizeProd,
									newFlows: matrix.flow, newCharges: matrix._charges,
									newMultiplicities: new[] { multiplicityS, matrix._multiplicities[1] },
									newBlockIndex: matrix._blockIndex)
							: null;
			}
			catch (Exception)
			{
				matrixU?.Dispose(); matrixV?.Dispose();
				throw;
			}
			#endregion

			#region SVD each diagonal block
			var S = new DenseVector<T>[matrix.NonZeroBlocks];
			try
			{
				for (int i = 0; i < matrix.NonZeroBlocks; i++)
				{
					var (rows, cols) = matrix.BlockIndexToBlockSize_Matrix(matrix._blockIndex[i]);
					long min = Math.Min(rows, cols);
					using var block = new DenseMatrix<T>(refArray: matrix, newRows: rows, newCols: cols, offset: matrix._blockOffset[i]);
					using var refBlockU = new DenseMatrix<T>(refArray: matrixU, newRows: rows, newCols: min, offset: matrixU._blockOffset[i]);
					using var refBlockV = new DenseMatrix<T>(refArray: matrixV, newRows: min, newCols: cols, offset: matrixV._blockOffset[i]);
					S[i] = new DenseVector<T>(length: min, onHost: this.OnHost);
					block.SingularValues(overwriteS: S[i], overwriteU: refBlockU, overwriteVct: refBlockV, calcU, calcV);
				}
			}
			catch (Exception)
			{
				S.ClearList();
				matrixU?.Dispose(); matrixV?.Dispose();
				throw;
			}
			finally
			{
				matrix?.Dispose();
			}
			#endregion

			#region reconstruct U and conjugate-transposed V (may be equal to matrixU and matrixV)
			if (returnMatrixUV)
				return (S, matrixU, matrixV);
			matrix.Dispose();
			var (U, Vct) = SingularVectorFromMatrix(p, calcU, calcV, matrixU, matrixV);
			return (S, U, Vct);
			#endregion
		}

		private (BlockSparseTensor<T, TC> U, BlockSparseTensor<T, TC> Vct) SingularVectorFromMatrix(int p, bool calcU, bool calcV, BlockSparseTensor<T, TC> matrixU, BlockSparseTensor<T, TC> matrixV)
		{
			BlockSparseTensor<T, TC> U = null, V = null;
			try
			{
				#region create U and V and return
				// get U
				long[] sizeU = this.Size.Take(p).Concat(new[] { matrixU.Size[1] }).ToArray();
				if (!calcU || sizeU.SequenceEqual(matrixU.Size))
				{
					U = matrixU;
				}
				else
				{
					TC[][] chargeU = new TC[p + 1][];
					int[][] multiplicityU = new int[p + 1][];
					bool[] flowU = new bool[p + 1];
					for (int i = 0; i < p; i++)
					{
						chargeU[i] = this._charges[i];
						multiplicityU[i] = this._multiplicities[i];
						flowU[i] = this.FlowDirection[i];
					}
					chargeU[p] = matrixU._charges[1]; // or matrixV._charges[0]
					multiplicityU[p] = matrixU._multiplicities[1]; // or matrixV._multiplicities[0]
					flowU[p] = matrixU.flow[1]; // or !matrixV.flow[0]
					U = ReshapeFromMatrix(matrixU, originalLength: matrixU.ActualLength, partition: p, sizeU, flowU, chargeU, multiplicityU);
				}
				// get V
				long[] sizeV = new[] { matrixU.Size[1] }.Concat(this.Size.TakeLast(this.Rank - p)).ToArray();
				if (!calcV || sizeV.SequenceEqual(matrixV.Size))
				{
					V = matrixV;
				}
				else
				{
					TC[][] chargeV = new TC[1 + (this.Rank - p)][];
					int[][] multiplicityV = new int[1 + (this.Rank - p)][];
					bool[] flowV = new bool[1 + (this.Rank - p)];
					flowV[0] = matrixV.flow[0]; // or !matrixU.flow[1]
					for (int i = 1, j = p; i < chargeV.Length; i++, j++)
					{
						chargeV[i] = this._charges[j];
						multiplicityV[i] = this._multiplicities[j];
						flowV[i] = this.FlowDirection[j];
					}
					chargeV[0] = matrixU._charges[1]; // or matrixV._charges[0]
					multiplicityV[0] = matrixU._multiplicities[1]; // or matrixV._multiplicities[0]
					V = ReshapeFromMatrix(matrixV, originalLength: matrixU.ActualLength, partition: 1, sizeV, flowV, chargeV, multiplicityV);
				}
				return (U, V);
				#endregion
			}
			#region dispose
			catch (Exception)
			{
				U?.Dispose(); V?.Dispose();
				throw;
			}
			finally
			{
				if (!ReferenceEquals(U, matrixU))
					matrixU?.Dispose();
				if (!ReferenceEquals(V, matrixV))
					matrixV?.Dispose();
			}
			#endregion
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix. Not necessarily sorted descending by singular values.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>The singular values as a <see cref="double"/> array and left, right singular vectors.
		/// <list type="table">
		/// <listheader><term>Singular vectors</term><description>  Size</description></listheader>
		/// <item><term>Left</term><description>  <c><see cref="AbstractArray{T}.Size">size</see>[..<paramref name="partition"/>]</c> append <c>min(prod(<see cref="AbstractArray{T}.Size">size</see>[<paramref name="partition"/>..]), prod(<see cref="AbstractArray{T}.Size">size</see>[..<paramref name="partition"/>]))</c></description></item>
		/// <item><term>Right</term><description>  <c><see cref="AbstractArray{T}.Size">size</see>[<paramref name="partition"/>..]</c> prepend <c>min(prod(<see cref="AbstractArray{T}.Size">size</see>[<paramref name="partition"/>..]), prod(<see cref="AbstractArray{T}.Size">size</see>[..<paramref name="partition"/>]))</c></description></item>
		/// </list>
		/// </returns>
		/// <exception cref="InvalidOperationException">if this tensor is in fact a vector</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported types</exception>
		/// <exception cref="InvalidOperationException">if the reshaped matrix is not block diagonal or has different charges between row and column (which ensures that the output tensor $U$ can contract with $S$ and / or $V^*$)</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		public (double[] S, BlockSparseTensor<T, TC> U, BlockSparseTensor<T, TC> Vct) SingularValues(Index partition, bool calcU = true, bool calcV = true)
		{
			if (this.Rank < 2)
				throw new InvalidOperationException();
			int p = (int)partition.GetPosition(this.Rank);
			var (S, U, Vct) = this.SingularValues(p, calcU, calcV);
			try
			{
				T[] arrayS = S.SelectMany(s => s.ToFortranOrderArray()).ToArray();
				if (default(T) is double)
					return (arrayS as double[], U, Vct);
				else
					return (Array.ConvertAll(arrayS, s => s.ToDouble()), U, Vct);
			}
			finally
			{
				S.ClearList();
			}
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix. Not necessarily sorted descending by singular values. Then truncate the singular values $S$ and vectors $U$, $V^*$ to preserve at most <paramref name="maxPreserve"/> entries.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="maxPreserve">the maximum number of singular values and vectors to preserve, must be positive</param>
		/// <returns>The singular values and left, right singular vectors with at most <paramref name="maxPreserve"/> entries.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		public (BlockSparseTensor<T, TC> S, BlockSparseTensor<T, TC> U, BlockSparseTensor<T, TC> Vct) SingularValuesTruncate(Index partition, int maxPreserve)
		{
			#region basic
			if (this.Rank < 2)
				throw new InvalidOperationException();
			int p = (int)partition.GetPosition(this.Rank);
			#endregion

			if (maxPreserve >= this.SizeProd[p] || maxPreserve >= this.Length / this.SizeProd[p])
			{
				#region no truncate
				var (S, U, Vct) = this.SingularValues(p, true, true, returnMatrixUV: false);
				long N = U.blockSize[^1];
				long actualLength = S.Sum(s => s.Length);
				// return S as a block sparse tensor
				BlockSparseTensor<T, TC> returnS = null;
				try
				{
					// create S
					returnS = new BlockSparseTensor<T, TC>(
								length: actualLength, onHost: this.OnHost,
								rows: U._multiplicitiesAccu[^1][^1], cols: U._multiplicitiesAccu[^1][^1],
								newBlockSize: new[] { N, N }, newBlockSizeProd: new[] { 1, N, N * N },
								newFlows: new[] { !U.flow[^1], U.flow[^1] },
								newCharges: new[] { U._charges[^1], U._charges[^1] },
								newMultiplicities: new[] { U._multiplicities[^1], U._multiplicities[^1] },
								newBlockIndex: ArrayLinq.Range(start: 0, count: N, step: N + 1).ToArray());
					returnS.FillWithZeros();
					// set diagonal elements of S block by block
					for (int i = 0; i < returnS.NonZeroBlocks; i++)
					{
						using var refMat = new DenseMatrix<T>(refArray: returnS, newRows: returnS._multiplicities[0][i], newCols: returnS._multiplicities[0][i], offset: returnS._blockOffset[i]);
						refMat.SetDiag(0, S[i]);
					}
					return (returnS, U, Vct);
				}
				catch (Exception)
				{
					returnS?.Dispose();
					throw;
				}
				finally
				{
					S.ClearList();
				}
				#endregion
			}
			else
			{
				#region need truncation
				#region create arrays to be sorted
				var (S, U, Vct) = this.SingularValues(p, true, true, returnMatrixUV: true);
				var arrayS = S.Select(s => s.ToFortranOrderArray()).ToArray();
				DenseVector<T>[][] arrayU = new DenseVector<T>[U.NonZeroBlocks][], arrayV = new DenseVector<T>[Vct.NonZeroBlocks][];
				#endregion
				try
				{
					#region get columns of U and rows of conjugate-transposed V
					for (int i = 0; i < U.NonZeroBlocks; i++)
					{
						using var refMatU = new DenseMatrix<T>(refArray: U, newRows: U._multiplicities[0][i], newCols: U._multiplicities[1][i], offset: U._blockOffset[i]);
						arrayU[i] = refMatU.GetColumns();
						using var refMatVct = new DenseMatrix<T>(refArray: Vct, newRows: Vct._multiplicities[0][i], newCols: Vct._multiplicities[1][i], offset: Vct._blockOffset[i]);
						arrayV[i] = refMatVct.GetRows();
					}
					#endregion
					#region get flat entries for sorting
					var flatS = arrayS.SelectMany(s => s).ToArray();
					var flatUVIndex = ArrayLinq.Range(0, (int)U.NonZeroBlocks)
											   .SelectMany(i => ArrayLinq.Range(0, arrayS[i].Length)
																		 .Select(j => (blockInd: i, rowCol: j)))
											   .ToArray();
					#endregion
					#region sort and truncate
					Array.Sort(keys: flatS, items: flatUVIndex);
					////flatS = flatS.Reverse().ToArray();
					flatUVIndex = flatUVIndex.Reverse().ToArray();
					////flatS = flatS[..maxPreserve];
					flatUVIndex = flatUVIndex[..maxPreserve];
					#endregion
					#region count preserve number for each block
					long actualLengthNewU = 0, actualLengthNewV = 0;
					int[] newMultiplicity; TC[] newCharge; int[] indices;
					{
						var preserveCount = new int[U.NonZeroBlocks];
						int newBlockLength = 0;
						for (int i = 0; i < flatUVIndex.Length; i++)
						{
							if (preserveCount[flatUVIndex[i].blockInd] == 0)
								newBlockLength++;
							preserveCount[flatUVIndex[i].blockInd]++;
							actualLengthNewU += U._multiplicities[0][flatUVIndex[i].blockInd];
							actualLengthNewV += Vct._multiplicities[1][flatUVIndex[i].blockInd];
						}
						if (newBlockLength == U.NonZeroBlocks)
						{
							newMultiplicity = preserveCount;
							newCharge = U._charges[1];
							indices = ArrayLinq.Range(0, newBlockLength).ToArray();
						}
						else
						{
							newMultiplicity = new int[newBlockLength];
							newCharge = new TC[newBlockLength];
							indices = new int[newBlockLength];
							int c = 0;
							for (int i = 0; i < preserveCount.Length; i++)
							{
								if (preserveCount[i] == 0)
									continue;
								newMultiplicity[c] = preserveCount[i];
								newCharge[c] = U._charges[1][i];
								indices[c++] = i;
							}
						}
					}
					#endregion
					BlockSparseTensor<T, TC> returnS = null, matrixU = null, matrixV = null;
					try
					{
						#region create S, new matrix U and V
						long N = newMultiplicity.Length;
						returnS = new BlockSparseTensor<T, TC>(
									length: newMultiplicity.Sum(m => m * m), onHost: this.OnHost,
									rows: U._multiplicitiesAccu[^1][^1], cols: U._multiplicitiesAccu[^1][^1],
									newBlockSize: new[] { N, N }, newBlockSizeProd: new[] { 1, N, N * N },
									newFlows: new[] { !U.flow[^1], U.flow[^1] },
									newCharges: new[] { newCharge, newCharge },
									newMultiplicities: new[] { newMultiplicity, newMultiplicity },
									newBlockIndex: ArrayLinq.Range(start: 0, count: N, step: N + 1).ToArray());
						var blockSizeU = new[] { U.blockSize[0], newMultiplicity.Length };
						matrixU = new BlockSparseTensor<T, TC>(
									length: actualLengthNewU, onHost: this.OnHost,
									rows: U.Size[0], cols: maxPreserve,
									newBlockSize: blockSizeU,
									newBlockSizeProd: blockSizeU.AccumulateProd().ToArray(),
									newFlows: U.flow, newCharges: new[] { U._charges[0], newCharge },
									newMultiplicities: new[] { U._multiplicities[0], newMultiplicity },
									newBlockIndex: indices.Select((ind, i) => ind + i * blockSizeU[0]).ToArray());
						var blockSizeV = new[] { newMultiplicity.Length, Vct.blockSize[1] };
						matrixV = new BlockSparseTensor<T, TC>(
									length: actualLengthNewV, onHost: this.OnHost,
									rows: maxPreserve, cols: Vct.Size[1],
									newBlockSize: blockSizeV,
									newBlockSizeProd: blockSizeV.AccumulateProd().ToArray(),
									newFlows: Vct.flow, newCharges: new[] { newCharge, Vct._charges[1] },
									newMultiplicities: new[] { newMultiplicity, Vct._multiplicities[1] },
									newBlockIndex: indices.Select((ind, i) => i + ind * blockSizeV[0]).ToArray());
						#endregion
						#region copy to S, matrix U and V
						// group by block
						var blocks = flatUVIndex.GroupBy(uv => uv.blockInd, uv => uv.rowCol);
						// copy
						for (int i = 0; i < blocks.Count; i++)
						{
							// copy to U
							var (rowPos, _) = U.BlockIndexToBlockPosition_Matrix(i);
							using var matBlockU = new DenseMatrix<T>(rows: U._multiplicities[0][rowPos], cols: blocks[i].Count, onHost: this.OnHost);
							matBlockU.FromColumnVectors(arrayU[blocks[i].Key].ReOrder(blocks[i]));
							RT.CopyTo(source: matBlockU, dest: matrixU, offsetDest: U._blockOffset[i]);
							// copy to V
							var (_, colPos) = U.BlockIndexToBlockPosition_Matrix(i);
							using var matBlockV = new DenseMatrix<T>(rows: Vct._multiplicities[1][colPos], cols: blocks[i].Count, onHost: this.OnHost);
							matBlockV.FromRowVectors(arrayV[blocks[i].Key].ReOrder(blocks[i]));
							RT.CopyTo(source: matBlockV, dest: matrixV, offsetDest: Vct._blockOffset[i]);
							// copy to S
							using var vecBlockS = new DenseVector<T>(blocks[i].Count, this.OnHost);
							vecBlockS.FromFortranOrderArray(arrayS[blocks[i].Key].ReOrder(blocks[i]));
							using var refMat = new DenseMatrix<T>(refArray: returnS, newRows: newMultiplicity[colPos], newCols: newMultiplicity[colPos], offset: returnS._blockOffset[colPos]);
							refMat.SetDiag(0, vecBlockS);
						}
						#endregion
					}
					#region dispose when exception
					catch (Exception)
					{
						returnS?.Dispose(); matrixU?.Dispose(); matrixV?.Dispose();
						throw;
					}
					#endregion
					#region reconstruct U and V and return
					var (returnU, returnVct) = SingularVectorFromMatrix(p, true, true, matrixU, matrixV);
					// return
					return (returnS, returnU, returnVct);
					#endregion
				}
				#region final dispositions
				finally
				{
					S.ClearList(); U?.Dispose(); Vct?.Dispose();
					arrayU?.ForEach(a => a.ClearList()); arrayV?.ForEach(a => a.ClearList());
				}
				#endregion
				#endregion
			}
		}


		/// <summary>
		/// QR factorize this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="full">perform full factorization or not</param>
		/// <returns>The (column) orthogonal Q matrix and upper-triangular R matrix.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		public (BlockSparseTensor<T, TC> Q, BlockSparseTensor<T, TC> R) QR(Index partition, bool full = false)
		{
			////int p = (int)partition.GetPosition(this.Rank);
			////using var matrix = this.ToMatrix(this.SizeProd[p]) as BlockSparseTensor<T, TC>;
			////matrix.InnerDiagonalOperationCheck(equalMultiplicity: false, allRowLargerThanCol: true);
			throw new NotImplementedException();
			// TODO: QR of block diagonal matrices is not trivial (at least not as trivial as the SVD of block diagonal matrices)
		}
		#endregion

		#region diagonal related
		/// <summary>
		/// Calculate the trace of this tensor as a matrix.
		/// </summary>
		/// <returns>the trace of this tensor as a matrix</returns>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public T Trace()
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();

			// calculate
			using var tempVec = new DenseVector<T>(this._multiplicities[0].Max(), this.OnHost);
			T sum = Scalars<T>.Zero;
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{   // must be square, number_of_rows = number_of_columns = this._multiplicities[0][i]
				int N = this._multiplicities[0][i];
				using var refMat = new DenseMatrix<T>(this, newRows: N, newCols: N, offset: this._blockOffset[i]);
				using var refVec = new DenseVector<T>(tempVec, N);
				refMat.GetDiag(0, overwrite: refVec);
				sum = sum.GenericAdd(refVec.Sum());
			}
			return sum;
		}

		/// <summary>
		/// Shift all the eigenvalues of this tensor by adding <paramref name="shift"/> to each diagonal elements of this tensor as a matrix.
		/// </summary>
		/// <param name="shift">the shift value, if it is zero, no operation shall be performed</param>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public void EigenvalueShift(T shift)
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();
			// shortcut
			if (shift.CompareTo(Scalars<T>.Zero) == 0)
				return;

			// shift
			using var ones = new DenseVector<T>(this._multiplicities[0].Max(), this.OnHost);
			ones.FillWithOnes();
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{   // must be square, number_of_rows = number_of_columns = this._multiplicities[0][i]
				int N = this._multiplicities[0][i];
				using var refMat = new DenseMatrix<T>(this, newRows: N, newCols: N, offset: this._blockOffset[i]);
				BLAS.VectorAddBy(y: refMat, x: ones, α: shift, strideY: N + 1);
			}
		}
		#endregion
		#endregion


		#region print
		/// <summary>
		/// Override <see cref="PureArray{T}.ToString()"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return ToString(terms: new Dictionary<string, object>
			{
				["data_type"] = typeof(T).Name,
				["charge_type"] = typeof(TC).Name,
				["non_zero_values"] = this.ActualLength,
				["non_zero_blocks"] = this.NonZeroBlocks,
				["tensor_of_blocks_size"] = string.Join('x', this.BlockSize),
				["flow"] = "{" + string.Join(',', this.FlowDirection.Select(f => f ? "in" : "out")) + "}",
				["label"] = "{" + string.Join(',', this.Label) + "}",
			}, include: new[] { StringTerms.Address, StringTerms.Size });
		}

		/// <summary>
		/// Print out the array.
		/// </summary>
		/// <param name="overrideSetting"><see cref="AbstractArray{T}.Print(IReadOnlyDictionary{PrintSetting, int})"/></param>
		/// <returns>String representation</returns>
		public override string Print(IReadOnlyDictionary<PrintSetting, int> overrideSetting = null)
		{
			if (this.Disposed)
				return this.ToString();
			// else
			StringBuilder builder = new();
			builder.Append(this.ToString());
			builder.AppendLine(":");
			long[] posArr = new long[this.Rank], sizeArr = new long[this.Rank];
			TC[] charge = new TC[this.Rank]; // cannot stack allocate in .net core 3.1
			for (int i = 0; i < this.NonZeroBlocks; i++)
			{
				Span<long> position = stackalloc long[this.Rank];
				Span<long> size = stackalloc long[this.Rank];
				// basic info of current block
				this.BlockIndexToBlockPosition(this._blockIndex[i], position);
				this.BlockIndexToBlockSize(this._blockIndex[i], size);
				position.CopyTo(posArr); size.CopyTo(sizeArr);
				this.BlockIndexToBlockCharge(this._blockIndex[i], charge);
				charge = charge.Select((c, i) => this.FlowDirection[i] ? c : c.Dual()).ToArray();
				builder.AppendLine($"Block at ({string.Join(',', posArr)}) [size={string.Join('x', sizeArr)}, charge=({string.Join(',', charge)})] ->");
				builder.Append('\t');
				// values of current block
				using var refTensor = new DenseTensor<T>(refArray: this, newSize: sizeArr, offset: this._blockOffset[i]);
				string[] tensorDescr = refTensor.Print(overrideSetting).Split(Environment.NewLine);
				builder.AppendLine(string.Join(Environment.NewLine + "\t", tensorDescr[1..^1]));
				tensorDescr[^1] = tensorDescr[^1].TrimEnd();
				if (!string.IsNullOrEmpty(tensorDescr[^1]))
					builder.AppendLine(tensorDescr[^1]);
			}
			return builder.ToString();
		}
		#endregion


		#region serialization
		/// <summary>
		/// Get the pointer in the class-defined order.
		/// </summary>
		/// <returns>the pointers</returns>
		public override IReadOnlyDictionary<string, IPointer> GetPointers()
		{
			return new Dictionary<string, IPointer>
			{
				[PointerName] = this.Pointer
			};
		}

		/// <summary>
		/// Get other requisite informations for re-constructing this array, in the class-defined order
		/// </summary>
		/// <returns>other requisite informations</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo()
		{
			return new Dictionary<string, object>
			{
				[BlockSparseTensorFactory.LengthName] = this.ActualLength,
				[BlockSparseTensorFactory.ChargeType] = typeof(TC),
				[BlockSparseTensorFactory.ChargeName] = Array.ConvertAll(this._charges, c => default(TC).SerializeArray(c)),
				[BlockSparseTensorFactory.MultiplicityName] = this._multiplicities,
				[BlockSparseTensorFactory.BlockIndexName] = this._blockIndex,
			};
		}

		internal BlockSparseTensor(Storage<T> storage, long[] size, IReadOnlyDictionary<string, object> otherInfo) : base(storage, size)
		{
			this._charges = Array.ConvertAll(otherInfo[BlockSparseTensorFactory.ChargeName] as Array[], s => default(TC).DeserializeArray(s));
			this._multiplicities = otherInfo[BlockSparseTensorFactory.MultiplicityName] as int[][];
			this._blockIndex = otherInfo[BlockSparseTensorFactory.BlockIndexName] as long[];
			this.UpdateMultiplicityAccu(); this.UpdateBlockOffset();
			this.blockSize = Array.ConvertAll(this._charges, c => c.LongLength);
			this.blockSizeProd = this.GetBlockSizeProd();
			// checks
			if (this._blockIndex.Length > this.blockSizeProd[^1])
				throw new ArgumentOutOfRangeException(nameof(otherInfo));
			if (!this._charges.SequenceEqual(this._multiplicities, (c, m) => c.Length == m.Length))
				throw new ArgumentOutOfRangeException(nameof(otherInfo));
			if (this._charges.Any(c => c.Distinct().Count != c.Length))
				throw new ArgumentOutOfRangeException(nameof(otherInfo));
		}

		internal BlockSparseTensor(long[] size, bool onHost, IReadOnlyDictionary<string, object> otherInfo) : base((long)otherInfo[BlockSparseTensorFactory.LengthName], size, onHost)
		{
			this._charges = Array.ConvertAll(otherInfo[BlockSparseTensorFactory.ChargeName] as Array[], s => default(TC).DeserializeArray(s));
			this._multiplicities = otherInfo[BlockSparseTensorFactory.MultiplicityName] as int[][];
			this._blockIndex = otherInfo[BlockSparseTensorFactory.BlockIndexName] as long[];
			this.UpdateMultiplicityAccu(); this.UpdateBlockOffset();
			this.blockSize = Array.ConvertAll(this._charges, c => c.LongLength);
			this.blockSizeProd = this.GetBlockSizeProd();
			// checks
			if (this._blockIndex.Length > this.blockSizeProd[^1])
				throw new ArgumentOutOfRangeException(nameof(otherInfo));
			if (!this._charges.SequenceEqual(this._multiplicities, (c, m) => c.Length == m.Length))
				throw new ArgumentOutOfRangeException(nameof(otherInfo));
			if (this._charges.Any(c => c.Distinct().Count != c.Length))
				throw new ArgumentOutOfRangeException(nameof(otherInfo));
		}
		#endregion
	}
}
