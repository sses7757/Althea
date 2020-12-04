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
	public class CustomsTests
	{
		[TestMethod()]
		public void PointWiseDivisionTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			w.FillWithRandoms();
			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());

			// Act
			API.PointWiseDivision(v, w);
			var real = hostv.PointwiseDivide(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void PointWiseMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			w.FillWithRandoms();
			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());

			// Act
			API.PointWiseMultiply(v, w);
			var real = hostv.PointwiseMultiply(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void PointWisePowerIntTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());

			// Act
			API.PointWisePower(v, 2);
			var real = hostv.PointwisePower(2);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void PointWisePowerFloatTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<double>(length: 10);
			v.FillWithRandoms();
			var hostv = MathD.DenseVector.OfArray(v.ToFortranOrderArray());

			// Act
			API.PointWisePower(v, 3.5);
			var real = hostv.PointwisePower(3.5);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ArrayConjugateTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());

			// Act
			API.PointWiseConjugate(v);
			var real = hostv.Conjugate();

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void UpcastTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());

			// Act
			using var w = new DenseVector<DoubleComplex>(length: 10);
			API.PointWiseUpcast(v, w);
			var real = hostv.Select(a => new Complex(a.Real, a.Imaginary)).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(w.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ToCompexTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<float>(length: 10);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = MathS.DenseVector.OfArray(v.ToFortranOrderArray());

			// Act
			API.PointWiseToComplex(v, w);
			var real = hostv.Select(a => new Complex32(a, 0)).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(w.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void IdentityTest()
		{
			RT.Reset(); // Arrange
			using var identity = new DenseMatrix<FloatComplex>(10, 10);
			// Act
			API.FillIdentity(identity);
			// Assert
			using var d = identity.GetDiag(0);
			Assert.IsTrue(d.ToFortranOrderArray().All(a => a == 1));
		}

		[TestMethod()]
		public void AllOneArrayTest()
		{
			// Assert
			using var ones = new DenseVector<FloatComplex>(10);
			using var onesmat = new DenseMatrix<FloatComplex>(10, 10);
			// Act
			API.FillWithOnes(ones); API.FillWithOnes(onesmat);
			// Assert
			Assert.IsTrue(ones.ToFortranOrderArray().All(a => a == 1));
			Assert.IsTrue(onesmat.ToFortranOrderArray().All(a => a == 1));
		}

		[TestMethod()]
		public void SetArrayValuesHostPosSingeValTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var pos = new int[] { 2, 6, 3 };
			FloatComplex val = 3;

			// Act
			API.SetArrayValues(v, pos, val);

			// Assert
			foreach (var item in pos)
			{
				Assert.AreEqual(val, v[(int)item]);
			}
		}

		[TestMethod()]
		public void TruncateTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = v.ToFortranOrderArray();

			// Act
			API.Truncate(v, 0.5f);

			// Assert
			var real = hostv.Select(a => a > 0.5f ? a : 0).ToArray();
			Assert.IsTrue(real.SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void TruncateSparseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 50, nonZeros: 10);
			v.FillWithRandoms();
			var hostv = v.ValueToFortranOrderArray();

			// Act
			API.Truncate(v, 0.5f);

			// Assert
			var real = hostv.Select(a => a > 0.5f ? a : 0).ToArray();
			Assert.IsTrue(real.SequenceEqual(v.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void MatrixKroneckerTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 5);
			using var B = new DenseMatrix<FloatComplex>(rows: 7, cols: 8);
			A.FillWithRandoms();
			B.FillWithRandoms();
			var hostA = MathC.DenseMatrix.OfColumnMajor(10, 5, A.ToFortranOrderArray().ToMathNet());
			var hostB = MathC.DenseMatrix.OfColumnMajor(7, 8, B.ToFortranOrderArray().ToMathNet());
			using var dest = new DenseMatrix<FloatComplex>(rows: 10 * 7, cols: 5 * 8);

			// Act
			API.MatrixKronecker(A, B, dest);
			var real = hostA.KroneckerProduct(hostB);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dest.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void MatrixCopyUpperPartToLowerTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			A.FillWithRandoms();

			// Act
			API.MatrixCopyUpperPartToLower(A);

			// Assert
			Assert.IsTrue(A.ToFortranOrderArray().Make2DArray(10, 10).IsHermitian());
		}
	}
}