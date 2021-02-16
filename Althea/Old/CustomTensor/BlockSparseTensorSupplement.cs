using System;
using System.Collections.Generic;

using CudaCSharp;
using CudaCSharp.Arrays;
using CudaCSharp.Linq;
using CudaCSharp.Memory;
using TensorCSharp.OneDimension.Dynamic;

namespace TensorCSharp.OneDimension.CustomTensor
{
	internal readonly struct BSTContractionInput<TC> : IEquatable<BSTContractionInput<TC>>
		where TC : struct, ICharge<TC>
	{
		internal readonly int[][] multiplicitiesA, multiplicitiesB;
		internal readonly TC[][] chargesA, chargesB;
		internal readonly bool[] flowA, flowB;
		internal readonly long[] blockIndexA, blockIndexB;

		internal BSTContractionInput(long[] blockIndexA, int[][] multiplicitiesA, TC[][] chargesA, bool[] flowA, long[] blockIndexB, int[][] multiplicitiesB, TC[][] chargesB, bool[] flowB)
		{
			this.blockIndexA = blockIndexA; this.blockIndexB = blockIndexB;
			this.multiplicitiesA = multiplicitiesA;this.multiplicitiesB = multiplicitiesB;
			this.chargesA = chargesA; this.chargesB = chargesB;
			this.flowA = flowA; this.flowB = flowB;
		}

		internal static int HashCodeOf(TC[][] charges, int[][] multiplicities, long[] blockIndex)
		{
			return HashCode.Combine(charges.HashCodeOfArray(c => c.HashCodeOfArray()), multiplicities.HashCodeOfArray(m => m.HashCodeOfArray()), blockIndex.HashCodeOfArray());
		}

		internal static bool MultiplicityEqual(int[] multiplicityA, int[] multiplicityB) => multiplicityA.SequenceEqual(multiplicityB);
		internal static bool ChargeEqual(TC[] chargeA, TC[] chargeB) => chargeA.SequenceEqual(chargeB);

		public bool Equals(BSTContractionInput<TC> other)
		{
			return this.multiplicitiesA.SequenceEqual(other.multiplicitiesA, MultiplicityEqual) &&
					this.multiplicitiesB.SequenceEqual(other.multiplicitiesB, MultiplicityEqual) &&
					this.chargesA.SequenceEqual(other.chargesA, ChargeEqual) &&
					this.chargesB.SequenceEqual(other.chargesB, ChargeEqual) &&
					this.blockIndexA.SequenceEqual(other.blockIndexA) &&
					this.blockIndexB.SequenceEqual(other.blockIndexB);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(HashCodeOf(this.chargesA, this.multiplicitiesA, this.blockIndexA), HashCodeOf(this.chargesB, this.multiplicitiesB, this.blockIndexB));
		}
	}

	internal readonly struct BSTContractionOutput<TC> : IDisposable where TC : struct, ICharge<TC>
	{
		// info of tensor C
		internal readonly char[] labelC;
		internal readonly bool[] flowC;
		internal readonly long[] sizeC, blockSizeC, blockSizeProdC;
		internal readonly long[] blockIndexC;
		internal readonly int[][] multiplicitiesC;
		internal readonly TC[][] chargesC;
		// info of contraction plan
		internal readonly ContractionPlan plan;

		internal BSTContractionOutput(bool[] flowC, long[] sizeC, char[] labelC, long[] blockSizeC, long[] blockSizeProdC)
		{
			this.flowC = flowC; this.labelC = labelC;
			this.sizeC = sizeC; this.blockSizeC = blockSizeC; this.blockSizeProdC = blockSizeProdC;

			this.blockIndexC = null;
			this.multiplicitiesC = null;
			this.chargesC = null;

			this.plan = default;
		}

		internal BSTContractionOutput(BSTContractionOutput<TC> basic, long[] blockIndexC, int[][] multiplicitiesC, TC[][] chargesC, ContractionPlan plan)
		{
			this.flowC = basic.flowC; this.labelC = basic.labelC;
			this.sizeC = basic.sizeC; this.blockSizeC = basic.blockSizeC; this.blockSizeProdC = basic.blockSizeProdC;

			this.blockIndexC = blockIndexC;
			this.multiplicitiesC = multiplicitiesC;
			this.chargesC = chargesC;

			this.plan = plan;
		}

		public void Dispose()
		{
			// do nothing
		}
	}

	internal readonly struct ContractionPlan
	{
		internal readonly int[] indicesA, indicesB;
		internal readonly int[] indicesPermA, indicesPermB;
		internal readonly int commonLength;
		internal readonly int commonRank;

		internal ContractionPlan(int commonLength, int commonRank, int[] indicesPermA, int[] indicesPermB, int[] indicesA, int[] indicesB)
		{
			this.indicesPermA = indicesPermA; this.indicesPermB = indicesPermB;
			this.indicesA = indicesA; this.indicesB = indicesB;
			this.commonLength = commonLength;
			this.commonRank = commonRank;
		}
	}


	internal sealed class BlockSparseTensorFactory : IArrayFactory
	{
		internal const string	LengthName = "NonzeroValues", ChargeType = "ChargeType",
								ChargeName = "Charges", MultiplicityName = "Multiplicities", BlockIndexName = "BlockIndex";

		public PureArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (!otherInfo.ContainsKey(ChargeType) || !otherInfo.ContainsKey(LengthName))
				throw new ArgumentNullException(nameof(otherInfo));
			var chargeType = otherInfo[ChargeType] as Type;
			return typeof(BlockSparseTensor<T, U1Symmetry>)
					.MakeGenericType(typeof(T), chargeType)
					.GetConstructor(new[] { typeof(long[]), typeof(bool), typeof(IReadOnlyDictionary<string, object>) })
					.Invoke(new object[] { size.ToArray(), onHost, otherInfo }) as PureArray<T>;
		}

		public PureArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IPointer> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : struct, IComparable<T>
		{
			if (!otherInfo.ContainsKey(ChargeType))
				throw new ArgumentNullException(nameof(otherInfo));
			var chargeType = otherInfo[ChargeType] as Type;
			long actualLength = otherInfo.ContainsKey(LengthName) ? (long)otherInfo[LengthName] : 0;
			var storage = PureArrayFactory.CheckPointer<T>(pointers, name: PureArray<T>.PointerName, size: actualLength);
			return typeof(BlockSparseTensor<T, U1Symmetry>)
					.MakeGenericType(typeof(T), chargeType)
					.GetConstructor(new[] { typeof(Storage<T>), typeof(long[]), typeof(IReadOnlyDictionary<string, object>) })
					.Invoke(new object[] { storage, size.ToArray(), otherInfo }) as PureArray<T>;
		}
	}


	/// <summary>
	/// The class for block sparse tensor <see cref="BlockSparseTensor{T, TC}"/>'s Hamiltonian MPO that inherits <see cref="AbstractHamiltonianMPO{TTen, T}"/>
	/// </summary>
	/// <typeparam name="T">the data type</typeparam>
	/// <typeparam name="TC">the charge type</typeparam>
	public sealed class BlockSparseMPO<T, TC> : AbstractHamiltonianMPO<BlockSparseTensor<T, TC>, T>
		where T : struct, IComparable<T>
		where TC : struct, ICharge<TC>
	{
		#region basics
		private readonly BlockSparseTensor<T, TC>[][,] tensors;

		private readonly TC[][] charges; // the row charges of each site

		/// <summary>
		/// The (row) charges of each site of this MPO
		/// </summary>
		public IReadOnlyList<IReadOnlyList<TC>> Charges => this.charges;

		/// <summary>
		/// Whether this MPO's matrix form is a upper triangular matrix or a lower one
		/// </summary>
		public override bool UpperTriangle { get; }

		private BlockSparseMPO(BlockSparseTensor<T, TC>[][,] tensors, TC[][] charges, bool upper) : base(tensors.Length, tensors[0].GetLength(0), (int)tensors[0][0,0].Size[0], periodic: true)
		{
			this.tensors = tensors;
			this.charges = charges;
			this.UpperTriangle = upper;
		}

		private static void ClearTensors<TT>(BlockSparseTensor<TT, TC>[][,] tensors) where TT : struct, IComparable<TT>
		{
			for (int n = 0; n < tensors.Length; n++)
				for (int i = 0; i < tensors[0].GetLength(0); i++)
					for (int j = 0; j < tensors[0].GetLength(1); j++)
						tensors[n][i, j]?.Dispose();
		}

		/// <summary>
		/// The function that actually implements the dispose functionality.
		/// </summary>
		/// <param name="disposing">dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				////this.charges = null;
			}
			ClearTensors(this.tensors);
		}
		#endregion

		#region convert from dense
		/// <summary>
		/// Create a new <see cref="BlockSparseMPO{T, TC}"/> from existing dense MPO <see cref="AbstractHamiltonianMPO{TTen, T}"/> and its sub-matrix operator's charges and multiplicities (values of row shall equal to values of column).
		/// </summary>
		/// <typeparam name="TTen">the dense tensor type</typeparam>
		/// <param name="MPO">the dense tensor MPO to create from</param>
		/// <param name="subMatrixCharge">the sorted charges of MPO's sub-matrix operator's row or column</param>
		/// <param name="subMatrixMultiplicity">the multiplicities sorted with charges of MPO's sub-matrix operator's row or column</param>
		/// <returns>the created <see cref="BlockSparseMPO{T, TC}"/> from <paramref name="MPO"/> with zero-valued sub-matrices of <paramref name="MPO"/> stored as null</returns>
		public static BlockSparseMPO<T, TC> CreateFromDense<TTen>(AbstractHamiltonianMPO<TTen, T> MPO, TC[] subMatrixCharge, int[] subMatrixMultiplicity)
			where TTen : PureArray<T>, ITensor<TTen, T>, IDenseArray<T>, new()
		{
			if (subMatrixCharge is null)
				throw new ArgumentNullException(nameof(subMatrixCharge));
			if (subMatrixMultiplicity is null)
				throw new ArgumentNullException(nameof(subMatrixMultiplicity));
			if (subMatrixMultiplicity.Length != subMatrixCharge.Length)
				throw new ArgumentException(Resource.LengthNotSame, nameof(subMatrixMultiplicity));
			if (MPO.BondDim != subMatrixCharge.Length)
				throw new ArgumentOutOfRangeException(nameof(MPO));

			BlockSparseTensor<T, TC>[][,] tensors = new BlockSparseTensor<T, TC>[MPO.NSite][,];
			TC[][] rowCharges = new TC[MPO.NSite][], colCharges = new TC[MPO.NSite][];
			try
			{
				// edge indices
				int i0 = DMRGUtilities.GetPartIndexOfEnvironment(MPO.UpperTriangle, right: true, identityMatrixPart: true).GetOffset(MPO.BondDim);
				int j0 = DMRGUtilities.GetPartIndexOfEnvironment(MPO.UpperTriangle, right: true, identityMatrixPart: false).GetOffset(MPO.BondDim);
				// loops
				for (int n = 0; n < MPO.NSite; n++)
				{
					tensors[n] = new BlockSparseTensor<T, TC>[MPO.BondDim, MPO.BondDim];
					rowCharges[n] = new TC[MPO.BondDim]; colCharges[n] = new TC[MPO.BondDim];
					// row charge
					for (int j = 0; j < MPO.BondDim; j++)
					{
						if (MPO.IsPartZero(n, i0, j))
							continue;
						tensors[n][i0, j] = ToBlockSparseTensor(MPO[n, i0, j], subMatrixCharge, subMatrixMultiplicity, out rowCharges[n][j]);
					}
					// column charge
					for (int i = 0; i < MPO.BondDim; i++)
					{
						if (MPO.IsPartZero(n, i, j0))
							continue;
						tensors[n][i, j0] = ToBlockSparseTensor(MPO[n, i, j0], subMatrixCharge, subMatrixMultiplicity, out colCharges[n][i]);
					}
					// check other charges
					for (int i = 0; i < MPO.BondDim; i++)
					{
						for (int j = 0; j < MPO.BondDim; j++)
						{
							if (i == i0 || j == j0 || MPO.IsPartZero(n, i, j))
								continue;
							tensors[n][i, j] = ToBlockSparseTensor(MPO[n, i, j], subMatrixCharge, subMatrixMultiplicity, out TC chargeIJ);
							if (!chargeIJ.Equals(rowCharges[n][i].Add(colCharges[n][j])))
								throw new ArgumentOutOfRangeException(nameof(MPO));
						}
					}
				}
				// check row column charges' consistency
				for (int n = 0; n < MPO.NSite; n++)
				{
					if (!colCharges[n].SequenceEqual(rowCharges[(n + 1) % MPO.NSite], (a, b) => a.Dual().Equals(b)))
						throw new ArgumentOutOfRangeException(nameof(MPO));
				}
				// return
				return new BlockSparseMPO<T, TC>(tensors, rowCharges, MPO.UpperTriangle);
			}
			catch (Exception)
			{
				ClearTensors(tensors);
				throw;
			}
		}

		internal static BlockSparseTensor<T, TC> ToBlockSparseTensor<TTen>(TTen input, TC[] charge, int[] multiplicity, out TC chargeSum)
			where TTen : PureArray<T>, ITensor<TTen, T>, IDenseArray<T>, new()
		{
			// get accumulate sum of multiplicity
			Span<int> multAccu = stackalloc int[charge.Length + 1];
			for (int i = 0; i < charge.Length; i++)
			{
				multAccu[i + 1] = multiplicity[i] + multiplicity[i];
			}
			// create arrays and block position set
			T[] array = input.ToFortranOrderArray();
			TC? quantumSum = null;
			HashSet<(int x, int y)> blocks = new HashSet<(int, int)>();
			// add to block position set
			for (int j = 0; j < input.Size[0]; j++) // column
			{
				for (int i = 0; i < input.Size[0]; i++) // row
				{
					if (!array[i + j * input.Size[0]].IsZero())
					{
						// get block position
						int qx = multAccu.BinarySearch(i, Comparer<int>.Default);
						if (qx < 0) qx = (~qx) - 1;
						int qy = multAccu.BinarySearch(j, Comparer<int>.Default);
						if (qy < 0) qy = (~qy) - 1;
						// check if sums of charges are same for all blocks
						if (quantumSum.HasValue && !charge[qx].Sub(charge[qy]).Equals(quantumSum.Value))
							throw new ArgumentOutOfRangeException(nameof(input));
						// add to block position set
						quantumSum = charge[qx].Sub(charge[qy]);
						blocks.Add((qx, qy));
					}
				}
			}
			// return null if all zero
			if (!quantumSum.HasValue)
			{
				chargeSum = default;
				return null;
			}
			else
			{
				chargeSum = quantumSum.Value;
			}
			var blockList = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(blocks, b => new[] { b.x, b.y }));
			var tensor = new BlockSparseTensor<T, TC>(flows: new[] { false, true },
													  charges: new[] { charge, charge },
													  multiplicities: new[] { multiplicity, multiplicity },
													  onHost: input.OnHost, nonZeroBlockPositions: blockList);
			try
			{
				for (int i = 0; i < blockList.Length; i++)
				{
					int blockPosRow = blockList[i][0], blockPosCol = blockList[i][1];
					int sizeRow = multiplicity[blockPosRow], sizeCol = multiplicity[blockPosCol];
					var pos = tensor.GetDensePositionAt(blockPosRow, blockPosCol);
					using var temp = PureArrayFactory.Create<TTen, T>(new long[] { sizeRow, sizeCol }, input.OnHost);
					CudaCSharp.Runtime.API.CopyMatrixTo(source: input, dest: temp,
														srcLD: input.Size[0], dstLD: sizeRow,
														copyNRows: sizeRow, copyNCols: sizeCol,
														offsetSouceRow: pos[0], offsetSouceCol: pos[1]);
					tensor.SetBlockAt(temp, blockPosRow, blockPosCol);
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

		#region indexer
		/// <summary>
		/// Indexer to get a MPO tensor as a <see cref="BlockSparseTensor{T, TC}"/> at site <paramref name="site"/>.
		/// </summary>
		/// <param name="site">the index of site</param>
		/// <returns>Null since this MPO does not support this operation.</returns>
		public override BlockSparseTensor<T, TC> this[Index site] => null;

		/// <summary>
		/// Get or set the tensors within a certain range as a new <see cref="AbstractMPO{TTen, T}"/>
		/// </summary>
		/// <param name="range">the site range</param>
		/// <returns>an <see cref="AbstractMPO{TTen, T}"/> within the range</returns>
		public override AbstractMPO<BlockSparseTensor<T, TC>, T> this[Range range] => new BlockSparseMPO<T, TC>(this.tensors[range], this.charges[range], this.UpperTriangle);

		/// <summary>
		/// Indexer of partial operator at site <paramref name="site"/> and sub-matrix index (<paramref name="x"/>, <paramref name="y"/>) of this <see cref="AbstractHamiltonianMPO{TTen, T}"/>
		/// </summary>
		/// <param name="site">the index of site</param>
		/// <param name="x">the index of row of sub-matrix at tensor of site <paramref name="site"/></param>
		/// <param name="y">the index of column of sub-matrix at tensor of site <paramref name="site"/></param>
		/// <returns>The referenced partial operator at <paramref name="site"/>, <paramref name="x"/>, <paramref name="y"/>. Shall return null if </returns>
		public override BlockSparseTensor<T, TC> this[Index site, Index x, Index y] => throw new NotImplementedException();
		#endregion

		#region AbstractHamiltonianMPO
		/// <summary>
		/// Deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override object Clone()
		{
			TC[][] charges = this.charges.Clone() as TC[][];
			var tensors = new BlockSparseTensor<T, TC>[this.NSite][,];
			try
			{
				for (int n = 0; n < this.NSite; n++)
					for (int i = 0; i < this.BondDim; i++)
						for (int j = 0; j < this.BondDim; j++)
							tensors[n][i, j] = this.tensors[n][i, j].Clone() as BlockSparseTensor<T, TC>;
				return new BlockSparseMPO<T, TC>(tensors, charges, this.UpperTriangle);
			}
			catch (Exception)
			{
				ClearTensors(tensors);
				throw;
			}
		}

		/// <summary>
		/// Cast this array into another data type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">the data type to cast to</typeparam>
		/// <returns>The casted <see cref="AbstractArray{T}"/>.</returns>
		public override AbstractArray<TOut> DataTypeCast<TOut>()
		{
			var outType = typeof(TOut);
			if (typeof(T) == outType)
				return this as AbstractArray<TOut>;
			var tensors = new BlockSparseTensor<TOut, TC>[this.NSite][,];
			try
			{
				for (int n = 0; n < this.NSite; n++)
					for (int i = 0; i < this.BondDim; i++)
						for (int j = 0; j < this.BondDim; j++)
							tensors[n][i, j] = this.tensors[n][i, j].DataTypeCast<TOut>() as BlockSparseTensor<TOut, TC>;
				return new BlockSparseMPO<TOut, TC>(tensors, this.charges, this.UpperTriangle);
			}
			catch (Exception)
			{
				ClearTensors(tensors);
				throw;
			}
		}

		/// <summary>
		/// Create a new array with same properties as this one.
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public override AbstractArray<T> NewArrayAlike() => throw new InvalidOperationException(Resource.MPOReadonly);

		/// <summary>
		/// Contract this tensor network state and reshape into a single matrix to represent the physical operator.
		/// </summary>
		/// <returns>a new <see cref="IMatrix{T}"/> representing the same physical operator as this one</returns>
		/// <exception cref="InvalidOperationException">if the output operator is expected to be too large to fit in the memory</exception>
		public override IMatrix<T> ToMatrixOperator() => throw new InvalidOperationException(Resource.MPOReadonly);

		/// <summary>
		/// Get the string representation of this MPO
		/// </summary>
		/// <returns>the string representation</returns>
		public override string ToString()
		{
			return $"One-dimensional {(this.Periodic ? "periodic" : "local")} block sparse MPO [size={this.NSite}, bond_dim={this.BondDim}]";
		}
		#endregion
	}
}
