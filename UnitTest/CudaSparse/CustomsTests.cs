using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Numerics;

using CudaCSharp.Linq;
using CudaCSharp.Arrays;
using CudaCSharp.Memory;
using static CudaCSharp.Arrays.Tests.Utilities;

using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathS = MathNet.Numerics.LinearAlgebra.Single;
using MathD = MathNet.Numerics.LinearAlgebra.Double;
using MathC = MathNet.Numerics.LinearAlgebra.Complex32;
using MathZ = MathNet.Numerics.LinearAlgebra.Complex;

using RT = CudaCSharp.Runtime.API;


namespace CudaCSharp.SparseBlas.Tests
{
	[TestClass]
	public class CustomsTests
	{
		[TestMethod]
		public void IntIndexMinMaxTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			using var indexPtr = Storage<int>.Create(length: N);
			Rng.API.FillWithRandom(new DenseVector<int>(indexPtr, N));

			// Act
			var (min, max) = API.IndexMinMax(indexPtr, N);

			var host = RT.CopyOutArray(indexPtr, length: N);
			int realmin = host.Min(), realmax = host.Max();

			// Assert
			Assert.AreEqual(realmin, min);
			Assert.AreEqual(realmax, max);
		}

		[TestMethod]
		public void IntIndexMaxTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			using var indexPtr = Storage<int>.Create(length: N);
			Rng.API.FillWithRandom(new DenseVector<int>(indexPtr, N));

			// Act
			var max = API.IndexMax(indexPtr, N);

			var host = RT.CopyOutArray(indexPtr, length: N);
			int realmin = host.Min(), realmax = host.Max();

			// Assert
			Assert.AreEqual(realmax, max);
		}

		[TestMethod]
		public void IntIndexFindTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			int P = 5;
			using var indexPtr = Storage<int>.Create(length: N);
			Rng.API.FillWithRandom(new DenseVector<int>(indexPtr, N));

			// Act
			var find = API.IndexFind(indexPtr, N, RT.CopyOut(indexPtr, offset: P));

			// Assert
			Assert.AreEqual(P, find);
		}

		private static readonly Random rand = new Random();

		[TestMethod]
		public void IntIndexLowerBoundTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			int P = 5;
			using var indexPtr = Storage<int>.Create(length: N);
			var host = new int[N];
			host = host.Select(a => rand.Next(0, 2 * N)).OrderBy(a => a).ToArray();
			RT.CopyIntoArray(indexPtr, host);

			// Act
			var find = API.IndexLowerUpperBound(indexPtr, N, P, lowerBound: true);

			var real = host.Where(a => LowerUpperBoundFunc(P, true, a));

			// Assert
			Assert.AreEqual(real.Count(), find);
		}

		[TestMethod]
		public void IntIndexUpperBoundTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			int P = 5;
			using var indexPtr = Storage<int>.Create(length: N);
			var host = new int[N];
			host = host.Select(a => rand.Next(0, 2 * N)).OrderBy(a => a).ToArray();
			RT.CopyIntoArray(indexPtr, host);

			// Act
			var find = API.IndexLowerUpperBound(indexPtr, N, P, lowerBound: false);

			var real = host.Where(a => LowerUpperBoundFunc(P, false, a));

			// Assert
			Assert.AreEqual(N - real.Count(), find);
		}

		private static bool LowerUpperBoundFunc(int P, bool lower, int v)
		{
			if (lower)
			{
				return v < P;
			}
			else
			{
				return v > P;
			}
		}

		[TestMethod]
		public void IntFillWithRangeTest()
		{
			RT.Reset(); // Arrange
			int N = 10, start = 2, step = 3;
			using var indexPtr = Storage<int>.Create(length: N);

			// Act
			API.IndexFillWithRange(indexPtr, N, start, step);

			var real = ArrayLinq.Range(start, N, step).Select(a => (int)a);

			// Assert
			Assert.IsTrue(real.SequenceEqual(RT.CopyOutArray(indexPtr, N)));
		}

		[TestMethod]
		public void VectorIndexToMatrixCOOTest()
		{
			RT.Reset(); // Arrange
			int nnz = 10, leadDim = 10;
			using var vecIndex = Storage<int>.Create(length: nnz);
			var host = new int[nnz];
			host = host.Select(a => (int)rand.Next(0, (int)(nnz * nnz))).ToArray();
			RT.CopyIntoArray(vecIndex, host);
			using var vec = new SparseVector<float>(100, vecIndex);
			using var mat = new SparseMatrix<float>(10, 10, nnz, SparseMatrixFormat.COOC);

			// Act
			API.VectorToFromCOOMatrix(vec, mat, true);

			var realrow = host.Select(a => (int)(a % leadDim)).ToArray();
			var realcol = host.Select(a => (int)(a / leadDim)).ToArray();

			// Assert
			Assert.IsTrue(realrow.SequenceEqual(mat.IndexToIntArray().First()));
			Assert.IsTrue(realcol.SequenceEqual(mat.IndexToIntArray().Last()));
		}

		[TestMethod]
		public void VectorIndexFromMatrixCOOTest()
		{
			RT.Reset(); // Arrange
			int nnz = 10;
			int leadDim = 10;
			using var row = Storage<int>.Create(length: nnz);
			using var col = Storage<int>.Create(length: nnz);
			using var val = Storage<float>.Create(length: nnz);
			var hostrow = Enumerable.Range(0, nnz).OrderBy(a => rand.Next()).ToArray();
			RT.CopyIntoArray(row, hostrow);
			var hostcol = Enumerable.Range(0, nnz).OrderBy(a => rand.Next()).ToArray();
			RT.CopyIntoArray(col, hostcol);
			Rng.API.FillWithRandom(new DenseVector<float>(val, nnz));
			using var mat = new SparseMatrix<float>(10, 10, value: val, row, col, SparseMatrixFormat.COOC);
			using var vec = new SparseVector<float>(100, nnz);

			// Act
			API.VectorToFromCOOMatrix(vec, mat, false);

			var real = hostrow.Zip(hostcol, (r, c) => r + c * leadDim).ToArray();

			// Assert
			Assert.IsTrue(real.SequenceEqual(vec.IndexToIntArray().First()));
		}

		[TestMethod]
		public void VectorDenseToSparseZeroThresholdTest()
		{
			RT.Reset(); // Arrange
			int P = 4;
			using var v = new DenseVector<double>(length: 10);
			v.FillWithRandoms();
			v[P] = 0;
			float threshold = 0;

			// Act
			var result = API.VectorDenseToSparse(v, threshold);

			var real = RT.CopyOutArray(v).ToList();
			real.RemoveAt(P);

			// Assert
			Assert.AreEqual(9, result.NonZero);
			Assert.IsTrue(real.SequenceEqual(RT.CopyOutArray(result)));
		}

		[TestMethod]
		public void VectorDenseToSparseNonZeroThresholdTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			float threshold = 0.5f;

			// Act
			var result = API.VectorDenseToSparse(v, threshold);

			var real = RT.CopyOutArray(v).Where(a => a.Abs() > 0.5);

			// Assert
			Assert.IsTrue(real.SequenceEqual(RT.CopyOutArray(result)));
		}

		[TestMethod]
		public void VectorSparseDenseElementWiseMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var w = new DenseVector<FloatComplex>(length: 10);
			w.FillWithRandoms();
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			v.FillWithRandoms();
			v.FillIndexWithRange((1, 2));

			var hostw = MathC.DenseVector.OfArray(RT.CopyOutArray(w).ToMathNet());
			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());

			// Act
			API.VectorSparseDensePointWiseMultiplyDivide(v, w, multiply: true);
			using var dv = v.ToDense();

			var real = hostv.PointwiseMultiply(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod]
		public void VectorSparseDenseElementWiseDivideTest()
		{
			RT.Reset(); // Arrange
			using var w = new DenseVector<FloatComplex>(length: 10);
			w.FillWithRandoms();
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			v.FillWithRandoms();
			v.FillIndexWithRange((1, 2));

			var hostw = MathC.DenseVector.OfArray(RT.CopyOutArray(w).ToMathNet());
			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());

			// Act
			API.VectorSparseDensePointWiseMultiplyDivide(v, w, multiply: false);
			using var dv = v.ToDense();

			var real = hostv.PointwiseDivide(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod]
		public void VectorSparseAddSparseTest()
		{
			RT.Reset(); // Arrange
			using var a = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			a.FillWithRandoms();
			a.FillIndexWithRange((1, 2));
			using var b = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 3));

			var hosta = MathC.SparseVector.OfIndexedEnumerable(10, a.ToMathNetSparse());
			var hostb = MathC.SparseVector.OfIndexedEnumerable(10, b.ToMathNetSparse());

			// Act
			using var res = API.VectorSparseAddSparse(a, b);
			using var dv = res.ToDense();

			var real = hosta.Add(hostb);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod]
		public void VectorDenseAddbySparseTest()
		{
			RT.Reset(); // Arrange
			using var w = new DenseVector<FloatComplex>(length: 10);
			w.FillWithRandoms();
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			v.FillWithRandoms();
			v.FillIndexWithRange((1, 2));

			var hostw = MathC.DenseVector.OfArray(RT.CopyOutArray(w).ToMathNet());
			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());

			// Act
			API.VectorSparseAddToDense(w, v, 1);

			var real = hostv.Add(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(w.ToFortranOrderArray()));
		}

		[TestMethod]
		public void VectorDenseAddbySparseTest2()
		{
			RT.Reset(); // Arrange
			using var w = new DenseVector<FloatComplex>(length: 10);
			w.FillWithRandoms();
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			v.FillWithRandoms();
			v.FillIndexWithRange((1, 2));

			var hostw = MathC.DenseVector.OfArray(RT.CopyOutArray(w).ToMathNet());
			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());

			// Act
			API.VectorSparseAddToDense(w, v, 2);

			var real = hostw.Add(hostv.Multiply(2));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(w.ToFortranOrderArray()));
		}

		[TestMethod]
		public void SparseMatrixGetNEITest()
		{
			RT.Reset(); // Arrange
			using var M = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, SparseMatrixFormat.CSR);
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			// Act
			var nnei = API.SparseMatrixGetNEI(M);

			var real = Enumerable.Range(0, 10).ToArray();

			// Assert
			Assert.AreEqual(10L, nnei.Length);
			Assert.IsTrue(real.SequenceEqual(RT.CopyOutArray(nnei)));
		}

		[TestMethod]
		public void IdentityCSRTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			using var mat = new SparseMatrix<FloatComplex>(N, N, N, SparseMatrixFormat.CSR);

			// Act
			API.FillIdentity(mat);

			var realrow = Enumerable.Range(0, N + 1).ToArray();
			var realcol = Enumerable.Range(0, N).ToArray();
			var real = new FloatComplex[N];
			Array.Fill(real, 1);

			// Assert
			Assert.IsTrue(real.SequenceEqual(mat.ValueToFortranOrderArray()));
			var test = mat.IndexToIntArray();
			Assert.IsTrue(realrow.SequenceEqual(test.First()));
			Assert.IsTrue(realcol.SequenceEqual(test.Last()));
		}

		[TestMethod]
		public void IdentityCSCTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			using var mat = new SparseMatrix<FloatComplex>(N, N, N, SparseMatrixFormat.CSC);

			// Act
			API.FillIdentity(mat);

			var realrow = Enumerable.Range(0, N).ToArray();
			var realcol = Enumerable.Range(0, N + 1).ToArray();
			var real = new FloatComplex[N];
			Array.Fill(real, 1);

			// Assert
			Assert.IsTrue(real.SequenceEqual(mat.ValueToFortranOrderArray()));
			var test = mat.IndexToIntArray();
			Assert.IsTrue(realrow.SequenceEqual(test.First()));
			Assert.IsTrue(realcol.SequenceEqual(test.Last()));
		}

		[TestMethod]
		public void IdentityCOOTest()
		{
			RT.Reset(); // Arrange
			int N = 10;
			using var mat = new SparseMatrix<FloatComplex>(N, N, N, SparseMatrixFormat.COOC);

			// Act
			API.FillIdentity(mat);

			var realrow = Enumerable.Range(0, N).ToArray();
			var realcol = Enumerable.Range(0, N).ToArray();
			var real = new FloatComplex[N];
			Array.Fill(real, 1);

			// Assert
			Assert.IsTrue(real.SequenceEqual(mat.ValueToFortranOrderArray()));
			var test = mat.IndexToIntArray();
			Assert.IsTrue(realrow.SequenceEqual(test.First()));
			Assert.IsTrue(realcol.SequenceEqual(test.Last()));
		}

		[TestMethod]
		public void SparseVectorOuterProductConjTest()
		{
			RT.Reset(); // Arrange
			using var a = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			a.FillWithRandoms();
			a.FillIndexWithRange((1, 2));
			using var b = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 3));
			using var M = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 3 * 4, SparseMatrixFormat.COOC);

			var hosta = MathC.SparseVector.OfIndexedEnumerable(10, a.ToMathNetSparse());
			var hostb = MathC.SparseVector.OfIndexedEnumerable(10, b.ToMathNetSparse());

			// Act
			API.VectorSparseOuterSparse(a, b, M, conjugateB: true);
			using var dv = M.ToDense();

			var real = hosta.OuterProduct(hostb.Conjugate());

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod]
		public void SparseVectorOuterProductNonconjTest()
		{
			RT.Reset(); // Arrange
			using var a = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			a.FillWithRandoms();
			a.FillIndexWithRange((1, 2));
			using var b = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 3));
			using var M = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 3 * 4, SparseMatrixFormat.COOC);

			var hosta = MathC.SparseVector.OfIndexedEnumerable(10, a.ToMathNetSparse());
			var hostb = MathC.SparseVector.OfIndexedEnumerable(10, b.ToMathNetSparse());

			// Act
			API.VectorSparseOuterSparse(a, b, M, conjugateB: false);
			using var dv = M.ToDense();

			var real = hosta.OuterProduct(hostb);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod]
		public void SparseMatrixKronckerCOOCTest()
		{
			RT.Reset(); // Arrange
			using var A = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 20, SparseMatrixFormat.COOC);
			A.FillWithRandoms();
			var hostAidx = Enumerable.Range(0, 100).OrderBy(a => rand.Next()).Skip(100 - 20).ToArray();
			var hostArow = hostAidx.Select(a => a % 10).ToArray();
			var hostAcol = hostAidx.Select(a => a / 10).ToArray();
			A.IndexFromIntArray(new[] { hostArow, hostAcol });

			using var B = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 15, SparseMatrixFormat.COOC);
			B.FillWithRandoms();
			var hostBidx = Enumerable.Range(0, 100).OrderBy(a => rand.Next()).Skip(100 - 15).ToArray();
			var hostBrow = hostAidx.Select(a => a % 10).ToArray();
			var hostBcol = hostAidx.Select(a => a / 10).ToArray();
			B.IndexFromIntArray(new[] { hostBrow, hostBcol });

			using var M = new SparseMatrix<FloatComplex>(rows: 100, cols: 100, nonZeros: 20 * 15, format: SparseMatrixFormat.COOC);

			var hostA = MathC.SparseMatrix.OfIndexed(10, 10, A.ToMathNetSparse());
			var hostB = MathC.SparseMatrix.OfIndexed(10, 10, B.ToMathNetSparse());

			// Act
			API.SparseMatrixKronecker(A, B, M, targetCOOC: true);

			using var test = M.ToDense();

			var real = hostA.KroneckerProduct(hostB);

			// Assert
			Console.WriteLine(M.Print());
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(test.ToFortranOrderArray()));
		}

		[TestMethod]
		public void SparseMatrixKronckerCOORTest()
		{
			RT.Reset(); // Arrange
			using var A = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 20, SparseMatrixFormat.COOC);
			A.FillWithRandoms();
			var hostAidx = Enumerable.Range(0, 100).OrderBy(a => rand.Next()).Skip(100 - 20).ToArray();
			var hostArow = hostAidx.Select(a => a % 10).ToArray();
			var hostAcol = hostAidx.Select(a => a / 10).ToArray();
			A.IndexFromIntArray(new[] { hostArow, hostAcol });

			using var B = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 15, SparseMatrixFormat.COOC);
			B.FillWithRandoms();
			var hostBidx = Enumerable.Range(0, 100).OrderBy(a => rand.Next()).Skip(100 - 15).ToArray();
			var hostBrow = hostAidx.Select(a => a % 10).ToArray();
			var hostBcol = hostAidx.Select(a => a / 10).ToArray();
			B.IndexFromIntArray(new[] { hostBrow, hostBcol });

			using var M = new SparseMatrix<FloatComplex>(rows: 100, cols: 100, nonZeros: 20 * 15, format: SparseMatrixFormat.COOR);

			var hostA = MathC.SparseMatrix.OfIndexed(10, 10, A.ToMathNetSparse());
			var hostB = MathC.SparseMatrix.OfIndexed(10, 10, B.ToMathNetSparse());

			// Act
			API.SparseMatrixKronecker(A, B, M, targetCOOC: false);

			using var test = M.ToDense();

			var real = hostA.KroneckerProduct(hostB);

			// Assert
			Console.WriteLine(M.Print());
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(test.ToFortranOrderArray()));
		}
	}
}
