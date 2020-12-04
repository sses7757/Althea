using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

using MathNet.Numerics.Random;

using CudaCSharp.Linq;
using CudaCSharp.Runtime;
using CudaCSharp.Arrays;

using static CudaCSharp.Arrays.Tests.Utilities;
using CudaCSharp.Memory;

namespace TensorCSharp.OneDimension.CustomTensor.Tests
{
	[TestClass()]
	public class BlockSparseTensorTests
	{
		static readonly Random rand = new Random();

		static void Main(string[] args)
		{
			new BlockSparseTensorTests().OperatorContractGCTest();
		}

		private static BlockSparseTensor<double, U1Symmetry> CreateZeroSumRank3(int rank12, int rank3, int mul1 = 5, int mul2 = 5, int mul3 = 2)
		{
			var charges = ArrayLinq.Range(0, rank12).Select(c => new U1Symmetry[] { c, c });
			charges = charges.SelectMany(c => ArrayLinq.Range(0, rank3).Where(r => (int)c[0] - r >= 0).Select(r => new U1Symmetry[] { (int)c[0] - r, c[1], r }));
			var multiplicities = ArrayLinq.Range(0, charges.Count).Select(c => new[] { mul1, mul2, mul3 });
			return new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false, true }, onHost: true);
		}

		private static BlockSparseTensor<double, U1Symmetry> CreateBlockDiagonal(int blocks, out IReadOnlyList<U1Symmetry[]> charges, out IReadOnlyList<int[]> multiplicities, out IReadOnlyList<int> offsets)
		{
			charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(10, 20), rand.Next(10, 20) });
			offsets = multiplicities.Select(m => m[0] * m[1]).AccumulateSum();
			return new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
		}

		[TestMethod()]
		public void BlockSparseTensorCreateTest()
		{
			int blocks = 20;
			// Arrange
			using var tensor = CreateBlockDiagonal(blocks, out var charges, out var multiplicities, out var offsets);

			// Act
			tensor.FillWithRandoms();
			var host = tensor.CopyOutArray();

			// Assert
			Assert.IsTrue(tensor.Charges[0].SequenceEqual(charges.Select(c => c[0])));
			Assert.IsTrue(tensor.Charges[1].SequenceEqual(charges.Select(c => c[1])));
			Assert.IsTrue(tensor.Multiplicities[0].SequenceEqual(multiplicities.Select(m => m[0])));
			Assert.IsTrue(tensor.Multiplicities[1].SequenceEqual(multiplicities.Select(m => m[1])));
			for (int i = 0; i < blocks; i++)
			{
				using var block = tensor.GetBlockAt(null as DenseTensor<double>, i, i);
				Assert.IsTrue(block.ToFortranOrderArray().SequenceEqual(host.TakeRange(offsets[i], (int)block.Length)));
			}
		}

		[TestMethod()]
		public void GetSetBlockOfTest()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40), charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge1.Distinct().Count != blocks)
				charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge2.Distinct().Count != blocks)
				charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges = charge1.Zip(charge2, (c1, c2) => new U1Symmetry[] { c1, c2 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(10, 20), rand.Next(10, 20) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			var host = SystemRandomSource.Default.NextDoubles((int)tensor.ActualLength);
			int[] offsets = new int[blocks + 1];
			for (int i = 0; i < blocks; i++)
			{
				using var block = tensor.GetBlockOf(null as DenseTensor<double>, charges[i]);
				block.FromFortranOrderArray(host.TakeRange(offsets[i], (int)block.ActualLength).ToArray());
				tensor.SetBlockOf(block, charges[i]);
				offsets[i + 1] = offsets[i] + (int)block.ActualLength;
			}

			// Assert
			for (int i = 0; i < blocks; i++)
			{
				using var block = tensor.GetBlockOf(null as DenseTensor<double>, charges[i]);
				Assert.IsTrue(block.ToFortranOrderArray().SequenceEqual(host.TakeRange(offsets[i], (int)block.Length)));
			}
		}

		[TestMethod()]
		public void ToDenseTensorTest()
		{
			int blocks = 20;
			// Arrange
			using var tensor = CreateBlockDiagonal(blocks, out var charges, out var multiplicities, out var offsets);
			var offsetRow = multiplicities.Select(m => m[0]).AccumulateSum();
			var offsetCol = multiplicities.Select(m => m[1]).AccumulateSum();

			// Act
			tensor.FillWithRandoms();
			using var dense = tensor.ToDenseTensor();

			// Assert
			for (int i = 0; i < blocks; i++)
			{
				using var block = tensor.GetBlockOf(null as DenseTensor<double>, charges[i]);
				var denseHost = dense.CopyOutColumnMajorMatrix(dense.Size[0], copyRows: block.Size[0], copyCols: block.Size[1], offset: offsetRow[i] + dense.Size[0] * offsetCol[i]);
				Assert.IsTrue(block.ToFortranOrderArray().SequenceEqual(denseHost));
			}
		}

		[TestMethod()]
		public void ToDenseTensorTest2()
		{
			int rank12 = 5; int rank3 = 3;
			// Arrange
			using var tensor = CreateZeroSumRank3(rank12, rank3);

			// Act
			tensor.FillWithRandoms();
			using var dense = tensor.ToDenseTensor();

			// Assert
			Console.WriteLine(dense.Print());
		}

		[TestMethod()]
		public void HasSameBlockStructureTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			var multiplicities2 = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities2, new[] { true, false }, onHost: true);
			using var tensorTrans = new BlockSparseTensor<double, U1Symmetry>(charges.Select(c => c.Reverse()), multiplicities.Select(m => m.Reverse()), new[] { true, false }, onHost: true);

			// Act
			bool same1 = tensor.HasSameBlockStructure(tensor2);
			bool same2 = tensor.HasSameBlockStructure(tensorTrans, (1, 0));

			// Assert
			Assert.IsFalse(same1);
			Assert.IsTrue(same2);
		}

		[TestMethod()]
		public void MakeReferenceOfThisTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			using var refTensor = tensor.MakeReference();

			// Assert
			Assert.AreNotSame(refTensor, tensor);
			Assert.IsTrue(tensor.HasSameBlockStructure(refTensor));
		}

		[TestMethod()]
		public void ReshapeTest()
		{
			int blocks = 4;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge1.Distinct().Count != blocks)
			////	charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge2.Distinct().Count != blocks)
			////	charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge3.Distinct().Count != blocks)
			////	charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge4.Distinct().Count != blocks)
			////	charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges = charge1.Zip(charge2).Zip(charge3, charge4, (c12, c3, c4) => new U1Symmetry[] { c12.First, c12.Second, c3, c4 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 6), rand.Next(2, 4), rand.Next(2, 4), rand.Next(5, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { false, true, false, true }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			using var reshape = tensor.Reshape(tensor.Size[0], tensor.Size[1] * tensor.Size[2], tensor.Size[3]);
			using var temp = tensor.Reshape(tensor.Size[0] * tensor.Size[1] * tensor.Size[2], tensor.Size[3]);
			using var real = temp.ToDenseTensor();
			using var test = reshape.ToDenseTensor();

			// Assert
			Assert.IsTrue(real.CopyOutArray().SequenceEqual(test.CopyOutArray()));
		}

		[TestMethod()]
		public void CloneTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			using var clone = tensor.Clone() as BlockSparseTensor<double, U1Symmetry>;

			// Assert
			Assert.AreNotSame(clone, tensor);
			Assert.IsTrue(tensor.HasSameBlockStructure(clone));
			Assert.IsTrue(tensor.CopyOutArray().SequenceEqual(clone.CopyOutArray()));
		}

		[TestMethod()]
		public void NewArrayAlikeTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			using var newTensor = tensor.NewArrayAlike() as BlockSparseTensor<double, U1Symmetry>;

			// Assert
			Assert.AreNotSame(newTensor, tensor);
			Assert.IsTrue(tensor.HasSameBlockStructure(newTensor));
			Assert.IsFalse(tensor.CopyOutArray().SequenceEqual(newTensor.CopyOutArray()));
		}

		[TestMethod()]
		public void NewArrayAlikeDifferentTypeTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			using var newTensor = tensor.NewArrayAlike<float>() as BlockSparseTensor<float, U1Symmetry>;

			// Assert
			Assert.AreNotSame(newTensor, tensor);
		}

		[TestMethod()]
		public void ToTheOtherMemoryTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			using var clone = tensor.ToTheOtherMemory() as BlockSparseTensor<double, U1Symmetry>;

			// Assert
			Assert.AreNotSame(clone, tensor);
			Assert.AreNotEqual(clone.OnHost, tensor.OnHost);
			Assert.IsTrue(tensor.HasSameBlockStructure(clone));
			Assert.IsTrue(tensor.CopyOutArray().SequenceEqual(clone.CopyOutArray()));
		}

		[TestMethod()]
		public void ScaleTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			var host = tensor.CopyOutArray();
			tensor.Scale(2);
			var host2 = tensor.CopyOutArray();

			// Assert
			Assert.IsTrue(host.Select(h => h * 2).ToArray().ApproxEqual(host2));
		}

		[TestMethod()]
		public void NormTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			var host = tensor.CopyOutArray();
			var real = Math.Sqrt(host.Aggregate((h, s) => s + h * h, 0.0));

			// Assert
			Assert.IsTrue(real.ApproxSame(tensor.Norm()));
		}

		[TestMethod()]
		public void NormalizeTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			var host = tensor.CopyOutArray();
			tensor.Normalize();
			var norm = Math.Sqrt(host.Aggregate((h, s) => s + h * h, 0.0));
			host = host.Select(h => h / norm).ToArray();

			// Assert
			Assert.IsTrue(host.ApproxEqual(tensor.CopyOutArray()));
		}

		[TestMethod()]
		public void KrylovVectorCheckTest()
		{
			// same as HasSameBlockStructureTest
		}

		[TestMethod()]
		public void DotTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			tensor2.FillWithRandoms();
			var dot = tensor.Dot(tensor2);
			var real = tensor.CopyOutArray().Zip(tensor2.CopyOutArray(), (t1, t2) => t1 * t2).Sum();

			// Assert
			Assert.IsTrue(real.ApproxSame(dot));
		}

		[TestMethod()]
		public void AddBy_αxTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			tensor2.FillWithRandoms();
			var host = tensor.CopyOutArray();
			var host2 = tensor2.CopyOutArray();
			tensor.AddBy_αx(tensor2, 2);
			var real = host.Zip(host2, (t1, t2) => t1 + 2 * t2).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(tensor.CopyOutArray()));
		}

		[TestMethod()]
		public void OperateOnTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			using var tensor3 = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			using var tensor4 = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor2.FillWithRandoms();
			tensor3.FillWithRandoms();
			tensor4.FillWithRandoms();
			using var res = tensor.OperateOn(new[] { tensor2, tensor3, tensor4 }, new[] { 1.0, 2.0, 3.0 });
			var real = tensor2.CopyOutArray().Zip(tensor3.CopyOutArray(), tensor4.CopyOutArray(), (t2, t3, t4) => t2 + 2 * t3 + 3 * t4).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(res.CopyOutArray()));
		}

		[TestMethod()]
		public void ReplaceByTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor2.FillWithRandoms();
			tensor.ReplaceBy(tensor2);

			// Assert
			Assert.IsTrue(tensor.CopyOutArray().ApproxEqual(tensor2.CopyOutArray()));
		}

		[TestMethod()]
		public void DualInPlaceTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			var org = tensor.FlowDirection.ToCopiedArray();
			tensor.DualInPlace();

			// Assert
			Assert.IsTrue(org.Select(o => !o).SequenceEqual(tensor.FlowDirection));
		}

		[TestMethod()]
		public void PermuteTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);
			using var tensorTrans = new BlockSparseTensor<double, U1Symmetry>(charges.Select(c => c.Reverse()), multiplicities.Select(m => m.Reverse()), new[] { true, false }, onHost: true);

			// Act
			tensorTrans.FillWithRandoms();
			tensorTrans.Permute(tensor, (1, 0));

			// Assert
			Assert.IsTrue(tensor.HasSameBlockStructure(tensorTrans, (1, 0)));
			for (int i = 0; i < blocks; i++)
			{
				using var blockTrans = tensorTrans.GetBlockAt(null as DenseTensor<double>, i, i);
				using var block = tensor.GetBlockAt(null as DenseTensor<double>, i, i);
				using var mat1 = blockTrans.ToMatrix(blockTrans.Size[0]) as DenseMatrix<double>;
				using var mat2 = block.ToMatrix(block.Size[0]) as DenseMatrix<double>;
				using var mat2Trans = mat2.Transpose();
				Assert.IsTrue(mat1.CopyOutArray().ApproxEqual(mat2Trans.CopyOutArray()));
			}
		}

		[TestMethod()]
		public void OperatorPermuteTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7) });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			using var tensorTrans = tensor.OperatorPermute((1, 0));

			// Assert
			Assert.IsTrue(tensor.HasSameBlockStructure(tensorTrans, (1, 0)));
			for (int i = 0; i < blocks; i++)
			{
				using var blockTrans = tensorTrans.GetBlockAt(null as DenseTensor<double>, i, i);
				using var block = tensor.GetBlockAt(null as DenseTensor<double>, i, i);
				using var mat1 = blockTrans.ToMatrix(blockTrans.Size[0]) as DenseMatrix<double>;
				using var mat2 = block.ToMatrix(block.Size[0]) as DenseMatrix<double>;
				using var mat2Trans = mat2.Transpose();
				Assert.IsTrue(mat1.CopyOutArray().ApproxEqual(mat2Trans.CopyOutArray()));
			}
		}

		[TestMethod()]
		public void HasSamePartialBlockStructureTest()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge1.Distinct().Count != blocks)
			////	charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge2.Distinct().Count != blocks)
			////	charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge3.Distinct().Count != blocks)
			////	charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			////while (charge4.Distinct().Count != blocks)
			////	charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges1 = charge1.Zip(charge2, charge3, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var charges2 = charge3.Zip(charge2, charge4, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(4, 7), rand.Next(4, 7), rand.Next(4, 7), rand.Next(4, 7) });
			var multiplicities1 = multiplicities.Select(m => m.ReOrder(0, 1, 2));
			var multiplicities2 = multiplicities.Select(m => m.ReOrder(2, 1, 3));
			using var tensor1 = new BlockSparseTensor<double, U1Symmetry>(charges1, multiplicities1, new[] { false, true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges2, multiplicities2, new[] { false, true, true }, onHost: true);

			// Act
			// Assert
			tensor1.HasSamePartialBlockStructure(tensor2, (1, 2), (1, 0));
		}

		[TestMethod()]
		public void ContractTest()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge1.Distinct().Count != blocks)
				charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge2.Distinct().Count != blocks)
				charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge3.Distinct().Count != blocks)
				charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge4.Distinct().Count != blocks)
				charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges1 = charge1.Zip(charge2, charge3, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var charges2 = charge3.Zip(charge2, charge4, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(5, 7), rand.Next(3, 5), rand.Next(3, 5), rand.Next(5, 7) });
			var multiplicities1 = multiplicities.Select(m => m.ReOrder(0, 1, 2));
			var multiplicities2 = multiplicities.Select(m => m.ReOrder(2, 1, 3));
			using var tensor1 = new BlockSparseTensor<double, U1Symmetry>(charges1, multiplicities1, new[] { false, true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges2, multiplicities2, new[] { false, true, true }, onHost: true);
			var charges3 = charge1.Zip(charge4, (c1, c2) => new U1Symmetry[] { c1, c2 });
			var multiplicities3 = multiplicities.Select(m => m.ReOrder(0, 3));
			using var tensor3 = new BlockSparseTensor<double, U1Symmetry>(charges3, multiplicities3, new[] { false, true }, onHost: true);

			// Act
			tensor1.FillWithRandoms();
			tensor2.FillWithRandoms();
			tensor3.FillWithRandoms();
			tensor1.SetLabel('i', 'a', 'b');
			tensor2.SetLabel('b', 'a', 'j');
			tensor3.SetLabel('i', 'j');
			tensor3.Contract(2, tensor1, tensor2, 1);

			// Assert
			Console.WriteLine(tensor3.Print());
			////Assert.IsTrue(real.CopyOutArray().ApproxEqual(test.CopyOutArray()));
		}

		[TestMethod()]
		public void OperatorContractTest()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge1.Distinct().Count != blocks)
				charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge2.Distinct().Count != blocks)
				charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge3.Distinct().Count != blocks)
				charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge4.Distinct().Count != blocks)
				charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges1 = charge1.Zip(charge2, charge3, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var charges2 = charge3.Zip(charge2, charge4, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(5, 7), rand.Next(3, 5), rand.Next(3, 5), rand.Next(5, 7) });
			var multiplicities1 = multiplicities.Select(m => m.ReOrder(0, 1, 2));
			var multiplicities2 = multiplicities.Select(m => m.ReOrder(2, 1, 3));
			using var tensor1 = new BlockSparseTensor<double, U1Symmetry>(charges1, multiplicities1, new[] { false, true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges2, multiplicities2, new[] { false, true, true }, onHost: true);

			// Act
			tensor1.FillWithRandoms();
			tensor2.FillWithRandoms();
			tensor1.SetLabel('i', 'a', 'b');
			tensor2.SetLabel('b', 'a', 'j');
			using var tensor3 = tensor1.OperatorContract(tensor2, 'i', 'j');

			// Assert
			Console.WriteLine(tensor1.Print());
			Console.WriteLine(tensor2.Print());
			Console.WriteLine(tensor3.Print());
			////Assert.IsTrue(real.CopyOutArray().ApproxEqual(test.CopyOutArray()));
		}

		[TestMethod()]
		public void OperatorContractGCTest()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge1.Distinct().Count != blocks)
				charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge2.Distinct().Count != blocks)
				charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge3.Distinct().Count != blocks)
				charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge4.Distinct().Count != blocks)
				charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges1 = charge1.Zip(charge2, charge3, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var charges2 = charge3.Zip(charge2, charge4, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(5, 7), rand.Next(3, 5), rand.Next(3, 5), rand.Next(5, 7) });
			var multiplicities1 = multiplicities.Select(m => m.ReOrder(0, 1, 2));
			var multiplicities2 = multiplicities.Select(m => m.ReOrder(2, 1, 3));
			using var tensor1 = new BlockSparseTensor<double, U1Symmetry>(charges1, multiplicities1, new[] { false, true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges2, multiplicities2, new[] { false, true, true }, onHost: true);

			// Act
			tensor1.FillWithRandoms();
			tensor2.FillWithRandoms();
			tensor1.SetLabel('i', 'a', 'b');
			tensor2.SetLabel('b', 'a', 'j');
			using var tensorA = tensor1.OperatorContract(tensor2, 'i', 'j');
			tensorA.Dispose();
			for (int i = 0; i < 40000; i++)
			{
				using var tensor3 = tensor1.OperatorContract(tensor2, 'i', 'j');
			}
		}

		[TestMethod()]
		public void OperatorContractTest2()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge1.Distinct().Count != blocks)
				charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge2.Distinct().Count != blocks)
				charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge3.Distinct().Count != blocks)
				charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges1 = charge1.Zip(charge2, (c1, c2) => new U1Symmetry[] { c1, c2 });
			var charges2 = charge2.Zip(charge3, (c2, c3) => new U1Symmetry[] { c2, c3 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(5, 7), rand.Next(3, 5), rand.Next(5, 7) });
			var multiplicities1 = multiplicities.Select(m => m.ReOrder(0, 1));
			var multiplicities2 = multiplicities.Select(m => m.ReOrder(1, 2));
			using var tensor1 = new BlockSparseTensor<double, U1Symmetry>(charges1, multiplicities1, new[] { true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges2, multiplicities2, new[] { false, true }, onHost: true);

			// Act
			tensor1.FillWithRandoms();
			tensor2.FillWithRandoms();
			tensor1.SetLabel('i', 'k');
			tensor2.SetLabel('k', 'j');
			using var tensor3 = tensor1.OperatorContract(tensor2, 'i', 'j');
			using var test = tensor3.ToDenseTensor();
			using var dense1 = tensor1.ToDenseTensor();
			using var dense2 = tensor2.ToDenseTensor();
			using var real = dense1.OperatorContract(dense2, 'i', 'j');

			// Assert
			Assert.IsTrue(real.CopyOutArray().ApproxEqual(test.CopyOutArray()));
		}

		[TestMethod()]
		public void GetSpanTest()
		{
			int rank12 = 10, rank3 = 4;
			// Arrange
			using var tensor = CreateZeroSumRank3(rank12, rank3);

			// Act
			tensor.FillWithRandoms();
			using var part1 = tensor.GetSpan(2, 0);
			using var part2 = tensor.GetSpan(2, 1);
			using var part3 = tensor.GetSpan(2, 2);

			// Assert
			Assert.IsFalse(part1.CopyOutArray().SequenceEqual(tensor.CopyOutArray(part1.ActualLength)));
		}

		[TestMethod()]
		public void SetSpanTest()
		{
			int rank12 = 10, rank3 = 4;
			// Arrange
			using var tensor = CreateZeroSumRank3(rank12, rank3);

			// Act
			tensor.FillWithRandoms();
			using var part = tensor.GetSpan(2, 1);
			part.FillWithRandoms();
			tensor.SetSpan(part, 2, 1);

			// Assert
			Assert.IsFalse(part.CopyOutArray().SequenceEqual(tensor.CopyOutArray(part.ActualLength)));
		}

		[TestMethod()]
		public void OperatorMatrixMultiplyTest()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge1.Distinct().Count != blocks)
				charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge2.Distinct().Count != blocks)
				charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge3.Distinct().Count != blocks)
				charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge4.Distinct().Count != blocks)
				charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges1 = charge1.Zip(charge2, charge3, (c1, c2, c3) => new U1Symmetry[] { c1, c2, c3 });
			var charges2 = charge2.Zip(charge3, charge4, (c2, c3, c4) => new U1Symmetry[] { c2, c3, c4 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(5, 7), rand.Next(3, 5), rand.Next(3, 5), rand.Next(5, 7) });
			var multiplicities1 = multiplicities.Select(m => m.ReOrder(0, 1, 2));
			var multiplicities2 = multiplicities.Select(m => m.ReOrder(1, 2, 3));
			using var tensor1 = new BlockSparseTensor<double, U1Symmetry>(charges1, multiplicities1, new[] { false, true, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges2, multiplicities2, new[] { true, false, true }, onHost: true);

			// Act
			tensor1.FillWithRandoms();
			tensor2.FillWithRandoms();
			tensor1.SetLabel('i', 'a', 'b');
			tensor2.SetLabel('a', 'b', 'j');
			using var tensor3 = tensor1.OperatorContract(tensor2, 'i', 'j');
			using var test = tensor1.OperatorMatrixMultiply(tensor2, 1, 2);

			// Assert
			Assert.IsTrue(test.CopyOutArray().ApproxEqual(tensor3.CopyOutArray()));
		}

		[TestMethod()]
		public void OperatorMatrixMultiplyTest2()
		{
			int blocks = 20;
			// Arrange
			int[] charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40),
				  charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge1.Distinct().Count != blocks)
				charge1 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge2.Distinct().Count != blocks)
				charge2 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge3.Distinct().Count != blocks)
				charge3 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			while (charge4.Distinct().Count != blocks)
				charge4 = SystemRandomSource.Default.NextInt32s(blocks, 0, 40);
			var charges1 = charge1.Zip(charge2, charge3, (c1, c2, c3) => new U1Symmetry[] { c2, c3, c1 });
			var charges2 = charge2.Zip(charge3, charge4, (c2, c3, c4) => new U1Symmetry[] { c2, c3, c4 });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { rand.Next(5, 7), rand.Next(3, 5), rand.Next(3, 5), rand.Next(5, 7) });
			var multiplicities1 = multiplicities.Select(m => m.ReOrder(1, 2, 0));
			var multiplicities2 = multiplicities.Select(m => m.ReOrder(1, 2, 3));
			using var tensor1 = new BlockSparseTensor<double, U1Symmetry>(charges1, multiplicities1, new[] { true, false, false }, onHost: true);
			using var tensor2 = new BlockSparseTensor<double, U1Symmetry>(charges2, multiplicities2, new[] { true, false, true }, onHost: true);

			// Act
			tensor1.FillWithRandoms();
			tensor2.FillWithRandoms();
			tensor1.SetLabel('a', 'b', 'i');
			tensor2.SetLabel('a', 'b', 'j');
			using var tensor3 = tensor1.OperatorContract(tensor2, 'i', 'j');
			using var test = tensor1.OperatorMatrixMultiply(tensor2, 2, 2, leftOp: CudaCSharp.MatrixOperation.Transpose);

			// Assert
			Assert.IsTrue(test.CopyOutArray().ApproxEqual(tensor3.CopyOutArray()));
		}

		[TestMethod()]
		public void DiagonalOperationCheckTest()
		{
			// not necessary
		}

		[TestMethod()]
		public void SingularValuesTest()
		{
			int blocks = 20;
			// Arrange
			using var matrix = CreateBlockDiagonal(blocks, out _, out _, out _);
			matrix.FillWithRandoms();

			// Act
			var (S, U, V) = matrix.SingularValues(1);
			using var dense = matrix.ToDenseTensor();
			var matDense = dense.ToMatrix(dense.Size[0]) as DenseMatrix<double>;
			var (Sreal1, Ureal, Vreal) = matDense.SingularValues();
			double[] Sreal;
			using (Sreal1)
				Sreal = Sreal1.ToFortranOrderArray();

			// Assert
			using (U) using (V) using (Ureal) using (Vreal)
			{
				using var denseU = U.ToDenseTensor();
				using var denseV = V.ToDenseTensor();
				using var matU = denseU.ToMatrix(U.Size[0]) as DenseMatrix<double>;
				using var matV = denseV.ToMatrix(V.Size[0]) as DenseMatrix<double>;
				var Ucols = matU.GetColumns();
				var Vrows = matV.GetRows();
				var UrealCols = Ureal.GetColumns();
				var VrealRows = Vreal.GetRows();
				try
				{
					S.SortCopyWith(Ucols); S.SortWith(Vrows);
					Sreal.SortCopyWith(UrealCols); Sreal.SortWith(VrealRows);
					// compare S
					Sreal = Sreal[^S.Length..];
					Assert.IsTrue(S.ApproxEqual(Sreal));
					// compare U
					UrealCols = UrealCols[^S.Length..];
					Assert.IsTrue(Ucols.SequenceEqual(UrealCols, (u1, u2) => u1.CopyOutArray().ApproxEqual(u2.CopyOutArray())));
					// compare V
					VrealRows = VrealRows[^S.Length..];
					Assert.IsTrue(Vrows.SequenceEqual(VrealRows, (v1, v2) => v1.CopyOutArray().ApproxEqual(v2.CopyOutArray())));
				}
				finally
				{
					Ucols.ClearList();
					Vrows.ClearList();
					UrealCols.ClearList();
					VrealRows.ClearList();
				}
			}
		}

		[TestMethod()]
		public void SingularValuesTruncateTest()
		{
			int blocks = 20;
			// Arrange
			using var matrix = CreateBlockDiagonal(blocks, out _, out _, out _);
			matrix.FillWithRandoms();

			// Act
			var (S, U, V) = matrix.SingularValuesTruncate(1, 20);
			using var dense = matrix.ToDenseTensor();
			var (Sreal, Ureal, Vreal) = dense.SingularValuesTruncate(1, 20);

			// Assert
			using (S) using (U) using (V) using (Sreal) using (Ureal) using (Vreal)
			{
				Console.WriteLine(S.Print());
				Console.WriteLine(Sreal.Print());
				Console.WriteLine();
			}
		}

		[TestMethod()]
		public void QRTest()
		{
			// not implemented
		}

		[TestMethod()]
		public void TraceTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { 10, 10 });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			var trace = tensor.Trace();

			// Assert
			using var dense = tensor.ToDenseTensor();
			Assert.IsTrue(trace.ApproxSame(dense.Trace()));
		}

		[TestMethod()]
		public void EigenvalueShiftTest()
		{
			int blocks = 20;
			// Arrange
			var charges = ArrayLinq.Range(0, blocks).Select(c => new U1Symmetry[] { c, c });
			var multiplicities = ArrayLinq.Range(0, blocks).Select(c => new[] { 10, 10 });
			using var tensor = new BlockSparseTensor<double, U1Symmetry>(charges, multiplicities, new[] { true, false }, onHost: true);

			// Act
			tensor.FillWithRandoms();
			using var dense = tensor.ToDenseTensor();
			tensor.EigenvalueShift(1);
			var trace = tensor.Trace();
			dense.EigenvalueShift(1);
			var real = dense.Trace();

			// Assert
			Assert.IsTrue(trace.ApproxSame(real));
		}
	}
}