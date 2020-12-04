using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Numerics;

using CudaCSharp.Arrays;
using static CudaCSharp.Arrays.Tests.Utilities;

using MathNet.Numerics;
using MathS = MathNet.Numerics.LinearAlgebra.Single;
using MathD = MathNet.Numerics.LinearAlgebra.Double;
using MathC = MathNet.Numerics.LinearAlgebra.Complex32;
using MathZ = MathNet.Numerics.LinearAlgebra.Complex;

using RT = CudaCSharp.Runtime.API;


namespace CudaCSharp.Blas.Tests
{
	[TestClass()]
	public class APITests
	{
		[TestMethod()]
		public void VectorOuterProductTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<FloatComplex>(length: 10);
			using var vec2 = new DenseVector<FloatComplex>(length: 20);
			vec1.FillWithRandoms();
			vec2.FillWithRandoms();
			var hostvec1 = MathC.DenseVector.OfArray(RT.CopyOutArray(vec1).ToMathNet());
			var hostvec2 = MathC.DenseVector.OfArray(RT.CopyOutArray(vec2).ToMathNet());
			using var mat = new DenseMatrix<FloatComplex>(rows: 10, cols: 20);
			// act
			API.VectorOuterProduct(vec1, vec2, mat, 1);
			var hostmat = RT.CopyOutArray(mat);
			var realmat = hostvec1.OuterProduct(hostvec2.Conjugate());
			// assert
			Assert.IsTrue(realmat.ToColumnMajorArray().ApproxEqual(hostmat));
		}

		[TestMethod()]
		public void VectorAddByTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<float>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);
			// act
			API.VectorAddBy(vec, vec, 1);
			// assert
			var real = hostvec.Select(v => v * 2).ToArray();
			var get = RT.CopyOutArray(vec);
			Assert.IsTrue(real.ApproxEqual(get));
		}

		[TestMethod()]
		public void VectorScaleTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<float>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);
			// act
			API.VectorScale(vec, 3);
			// assert
			var real = hostvec.Select(v => v * 3).ToArray();
			var get = RT.CopyOutArray(vec);
			Assert.IsTrue(real.ApproxEqual(get));
		}

		[TestMethod()]
		public void VectorAbsSumTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<FloatComplex>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);
			// act
			var res = API.VectorAbsSum(vec);
			// assert
			var real = hostvec.Sum(a => Math.Abs(a.Real()) + Math.Abs(a.Imaginary()));
			Assert.IsTrue(real.ApproxSame((float)res));
		}

		[TestMethod()]
		public void VectorAbsArgmaxTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<FloatComplex>(length: 100);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);
			// act
			var res = API.VectorAbsArgmax(vec);
			// assert
			int real = 0;
			for (int i = 1; i < hostvec.Length; i++)
			{
				if (hostvec[i] > hostvec[real])
					real = i;
			}
			Assert.AreEqual(real, res);
		}

		[TestMethod()]
		public void VectorAbsArgminTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<float>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);
			// act
			var res = API.VectorAbsArgmin(vec);
			// assert
			int real = 0;
			for (int i = 1; i < hostvec.Length; i++)
			{
				if (hostvec[i] < hostvec[real])
					real = i;
			}
			Assert.AreEqual(real, res);
		}

		[TestMethod()]
		public void VectorDotTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<FloatComplex>(length: 10);
			using var vec2 = new DenseVector<FloatComplex>(length: 10);
			vec1.FillWithRandoms();
			vec2.FillWithRandoms();
			var hostvec1 = MathC.DenseVector.OfArray(RT.CopyOutArray(vec1).ToMathNet());
			var hostvec2 = MathC.DenseVector.OfArray(RT.CopyOutArray(vec2).ToMathNet());
			// act
			var res = API.VectorDot(vec1, vec2);
			var real = hostvec1.ConjugateDotProduct(hostvec2);
			// assert
			Assert.IsTrue(real.ApproxSame(res));
		}

		[TestMethod()]
		public void VectorNormTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<FloatComplex>(length: 10);
			vec1.FillWithRandoms();
			var hostvec1 = MathC.DenseVector.OfArray(RT.CopyOutArray(vec1).ToMathNet());
			// act
			var res = API.VectorNorm(vec1);
			var real = hostvec1.L2Norm();
			// assert
			Assert.IsTrue(((float)real).ApproxSame((float)res));
		}

		[TestMethod()]
		public void VectorGenralCopyTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<FloatComplex>(length: 10);
			using var vec2 = new DenseVector<FloatComplex>(length: 5);
			vec1.FillWithRandoms();
			vec2.FillWithZeros();
			var hostvec1 = RT.CopyOutArray(vec1);
			// act
			API.VectorGenralCopy(vec2, vec1, 5, strideSrc: 2);
			// assert
			var real = hostvec1.Where((a, i) => i % 2 == 0).ToArray();
			var get = RT.CopyOutArray(vec2);
			Assert.IsTrue(real.ApproxEqual(get));
		}

		[TestMethod()]
		public void HermitianMatrixVectorMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 10);
			using var vecout = new DenseVector<double>(length: 10);
			using var mat = new DenseMatrix<double>(rows: 10, cols: 10, herm: true);
			vec.FillWithRandoms();
			mat.FillWithRandoms();
			mat.CopyUpperToLower();
			vecout.FillWithZeros();
			var hostvec = MathD.DenseVector.OfArray(RT.CopyOutArray(vec));
			var hostmat = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(mat));
			// act
			API.MatrixVectorMultiply(mat, vec, vecout, 1);
			var real = hostmat.Multiply(hostvec);
			// assert
			var get = RT.CopyOutArray(vecout);
			Assert.IsTrue(real.ToArray().ApproxEqual(get));
		}

		[TestMethod()]
		public void GeneralMatrixVectorMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 20);
			using var vecout = new DenseVector<double>(length: 10);
			using var mat = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			vec.FillWithRandoms();
			mat.FillWithRandoms();
			vecout.FillWithZeros();
			var hostvec = MathD.DenseVector.OfArray(RT.CopyOutArray(vec));
			var hostmat = MathD.DenseMatrix.OfColumnMajor(20, 10, RT.CopyOutArray(mat));
			// act
			API.MatrixVectorMultiply(mat, vec, vecout, 1, opA: MatrixOperation.ConjugateTranspose);
			var real = hostmat.ConjugateTranspose().Multiply(hostvec);
			// assert
			var get = RT.CopyOutArray(vecout);
			Assert.IsTrue(real.ToArray().ApproxEqual(get));
		}

		[TestMethod()]
		public void MatrixGeneralAddTest()
		{
			RT.Reset(); // Arrange
			using var mat1 = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			using var mat2 = new DenseMatrix<double>(rows: 10, cols: 20, herm: false);
			mat1.FillWithRandoms();
			mat2.FillWithRandoms();
			var hostmat1 = MathD.DenseMatrix.OfColumnMajor(20, 10, RT.CopyOutArray(mat1));
			var hostmat2 = MathD.DenseMatrix.OfColumnMajor(10, 20, RT.CopyOutArray(mat2));
			// act
			using var resmat = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			API.MatrixGeneralAdd(mat1, mat2, resmat, 1, 1, opA: MatrixOperation.None, opB: MatrixOperation.ConjugateTranspose);
			var res = RT.CopyOutArray(resmat);
			var real = hostmat1.Add(hostmat2.ConjugateTranspose());
			// assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(res));
		}

		[TestMethod()]
		public void MatrixHermitianMultiplyGeneralTest()
		{
			RT.Reset(); // Arrange
			using var mat1 = new DenseMatrix<double>(rows: 10, cols: 10, herm: true);
			using var mat2 = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			mat1.FillWithRandoms();
			mat1.CopyUpperToLower(); // make Hermitian
			mat2.FillWithRandoms();
			var hostmat1 = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(mat1));
			var hostmat2 = MathD.DenseMatrix.OfColumnMajor(20, 10, RT.CopyOutArray(mat2));
			// act
			using var resmat = new DenseMatrix<double>(rows: 10, cols: 20, herm: false);
			API.MatrixMultiply(mat1, mat2, resmat, 1, 0, opB: MatrixOperation.ConjugateTranspose);
			var res = RT.CopyOutArray(resmat);
			var real = hostmat1.ConjugateTransposeAndMultiply(hostmat2);
			// assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(res));
		}

		[TestMethod()]
		public void MatrixGeneralMultiplyHermitianTest()
		{
			RT.Reset(); // Arrange
			using var mat1 = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			using var mat2 = new DenseMatrix<double>(rows: 10, cols: 10, herm: true);
			mat1.FillWithRandoms();
			mat2.FillWithRandoms();
			mat2.CopyUpperToLower(); // make Hermitian
			var hostmat1 = MathD.DenseMatrix.OfColumnMajor(20, 10, RT.CopyOutArray(mat1));
			var hostmat2 = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(mat2));
			// act
			using var resmat = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			API.MatrixMultiply(mat1, mat2, resmat, 1, 0, opB: MatrixOperation.ConjugateTranspose);
			var res = RT.CopyOutArray(resmat);
			var real = hostmat1.ConjugateTransposeAndMultiply(hostmat2);
			// assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(res));
		}

		[TestMethod()]
		public void MatrixGeneralMultiplyGeneralTest()
		{
			RT.Reset(); // Arrange
			using var mat1 = new DenseMatrix<double>(rows: 10, cols: 20, herm: false);
			using var mat2 = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			mat1.FillWithRandoms();
			mat2.FillWithRandoms();
			var hostmat1 = MathD.DenseMatrix.OfColumnMajor(10, 20, RT.CopyOutArray(mat1));
			var hostmat2 = MathD.DenseMatrix.OfColumnMajor(20, 10, RT.CopyOutArray(mat2));
			// act
			using var resmat = new DenseMatrix<double>(rows: 20, cols: 20, herm: false);
			API.MatrixMultiply(mat1, mat2, resmat, 1, 0, opA: MatrixOperation.ConjugateTranspose, opB: MatrixOperation.ConjugateTranspose);
			var res = RT.CopyOutArray(resmat);
			var real = hostmat1.ConjugateTranspose().ConjugateTransposeAndMultiply(hostmat2);
			// assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(res));
		}

		[TestMethod()]
		public void DiagMatrixMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 20);
			using var mat = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			vec.FillWithRandoms();
			mat.FillWithRandoms();
			var hostvec = MathD.DiagonalMatrix.OfDiagonal(20, 20, RT.CopyOutArray(vec));
			var hostmat = MathD.DenseMatrix.OfColumnMajor(20, 10, RT.CopyOutArray(mat));
			// act
			using var resmat = new DenseMatrix<double>(rows: 20, cols: 10, herm: false);
			API.DiagonalMatrixMultiply(mat, vec, resmat, side: SideMode.Left);
			// assert
			var realmat = hostvec.Multiply(hostmat);
			var hostres = RT.CopyOutArray(resmat);
			Assert.IsTrue(realmat.ToColumnMajorArray().ApproxEqual(hostres));
		}

		[TestMethod()]
		public void RankKUpdateTest()
		{
			RT.Reset(); // Arrange
			using var matres = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: true);
			using var mat = new DenseMatrix<FloatComplex>(rows: 10, cols: 20, herm: false);
			matres.FillWithRandoms();
			matres.CopyUpperToLower(); // make Hermitian
			mat.FillWithRandoms();
			var hostmatres = MathC.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(matres).ToMathNet());
			var hostmat = MathC.DenseMatrix.OfColumnMajor(10, 20, RT.CopyOutArray(mat).ToMathNet());
			// act
			API.RankKUpdate(mat, matres, 1, 0, opA: MatrixOperation.None);
			matres.CopyUpperToLower(); // make Hermitian again
			var res = RT.CopyOutArray(matres);
			var real = hostmat.ConjugateTransposeAndMultiply(hostmat);
			// assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(res));
		}
	}
}