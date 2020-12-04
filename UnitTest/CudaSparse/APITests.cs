using CudaCSharp.SparseBlas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Numerics;

using CudaCSharp.Arrays;
using static CudaCSharp.Arrays.Tests.Utilities;

using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathS = MathNet.Numerics.LinearAlgebra.Single;
using MathD = MathNet.Numerics.LinearAlgebra.Double;
using MathC = MathNet.Numerics.LinearAlgebra.Complex32;
using MathZ = MathNet.Numerics.LinearAlgebra.Complex;

using RT = CudaCSharp.Runtime.API;
using CudaCSharp;

namespace CudaCSharp.SparseBlas.Tests
{
	[TestClass]
	public class APITests
	{
		[TestMethod]
		public void SparseVectorToDenseTest()
		{
#pragma warning disable CA1303 // Do not pass literals as localized parameters
			Console.WriteLine("the obsolete method will not be tested");
			////API.SparseVectorToDense()
		}

		[TestMethod]
		public void VectorDenseAddBySparseTest()
		{
			Console.WriteLine("the obsolete method will not be tested");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
			////API.VectorDenseAddBySparse(y, x, α);
		}


		#region dot test
		[TestMethod]
		public void VectorSparseConjDotDenseTest()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<FloatComplex>(length: 20);
			y.FillWithRandoms();
			using var x = new SparseVector<FloatComplex>(length: 20, nonZeros: 5);
			x.FillWithRandoms();
			x.FillIndexWithRange((2, 3));
			bool? conjugateX = true;

			var hostY = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());
			var hostX = MathC.SparseVector.OfIndexedEnumerable(length: 20, x.ToMathNetSparse());

			// Act
			var result = API.VectorSparseDotDense(x, y, conjugateX);
			var real = hostX.ConjugateDotProduct(hostY);

			// Assert
			Assert.IsTrue(real.ApproxSame(result));
		}

		[TestMethod]
		public void VectorSparseNoneConjDotDenseTest()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<FloatComplex>(length: 20);
			y.FillWithRandoms();
			using var x = new SparseVector<FloatComplex>(length: 20, nonZeros: 5);
			x.FillWithRandoms();
			x.FillIndexWithRange((2, 3));
			bool? conjugateX = false;

			var hostY = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());
			var hostX = MathC.SparseVector.OfIndexedEnumerable(length: 20, x.ToMathNetSparse());

			// Act
			var result = API.VectorSparseDotDense(x, y, conjugateX);
			var real = hostX.DotProduct(hostY);

			// Assert
			Assert.IsTrue(real.ApproxSame(result));
		}
		#endregion


		#region sparse matrix multiply dense vector test
		[TestMethod]
		public void SparseCSRMatrixNoneTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<double>(length: 10);
			using var x = new DenseVector<double>(length: 10);
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.None;
			

			var hostvec1 = MathD.DenseVector.OfArray(RT.CopyOutArray(y));
			var hostvec2 = MathD.DenseVector.OfArray(RT.CopyOutArray(x));
			using var dnmat = M.ToDense();
			var hostmat = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat));

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.Multiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCSCMatrixNoneTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<double>(length: 10);
			using var x = new DenseVector<double>(length: 10);
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.None;
			

			var hostvec1 = MathD.DenseVector.OfArray(RT.CopyOutArray(y));
			var hostvec2 = MathD.DenseVector.OfArray(RT.CopyOutArray(x));
			using var dnmat = M.ToDense();
			var hostmat = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat));

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.Multiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCOOMatrixNoneTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<double>(length: 10);
			using var x = new DenseVector<double>(length: 10);
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.None;
			

			var hostvec1 = MathD.DenseVector.OfArray(RT.CopyOutArray(y));
			var hostvec2 = MathD.DenseVector.OfArray(RT.CopyOutArray(x));
			using var dnmat = M.ToDense();
			var hostmat = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat));

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.Multiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCSRMatrixTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<double>(length: 10);
			using var x = new DenseVector<double>(length: 10);
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.Transpose;
			

			var hostvec1 = MathD.DenseVector.OfArray(RT.CopyOutArray(y));
			var hostvec2 = MathD.DenseVector.OfArray(RT.CopyOutArray(x));
			using var dnmat = M.ToDense();
			var hostmat = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat));

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.TransposeThisAndMultiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCSCMatrixTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<double>(length: 10);
			using var x = new DenseVector<double>(length: 10);
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.Transpose;
			

			var hostvec1 = MathD.DenseVector.OfArray(RT.CopyOutArray(y));
			var hostvec2 = MathD.DenseVector.OfArray(RT.CopyOutArray(x));
			using var dnmat = M.ToDense();
			var hostmat = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat));

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.TransposeThisAndMultiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCOOMatrixTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<double>(length: 10);
			using var x = new DenseVector<double>(length: 10);
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.Transpose;
			

			var hostvec1 = MathD.DenseVector.OfArray(RT.CopyOutArray(y));
			var hostvec2 = MathD.DenseVector.OfArray(RT.CopyOutArray(x));
			using var dnmat = M.ToDense();
			var hostmat = MathD.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat));

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.TransposeThisAndMultiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCSRMatrixConjTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<FloatComplex>(length: 10);
			using var x = new DenseVector<FloatComplex>(length: 10);
			using var M = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.ConjugateTranspose;
			

			var hostvec1 = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());
			var hostvec2 = MathC.DenseVector.OfArray(RT.CopyOutArray(x).ToMathNet());
			using var dnmat = M.ToDense();
			var hostmat = MathC.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat).ToMathNet());

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.ConjugateTransposeThisAndMultiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCSCMatrixConjTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<FloatComplex>(length: 10);
			using var x = new DenseVector<FloatComplex>(length: 10);
			using var M = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.ConjugateTranspose;
			

			var hostvec1 = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());
			var hostvec2 = MathC.DenseVector.OfArray(RT.CopyOutArray(x).ToMathNet());
			using var dnmat = M.ToDense();
			var hostmat = MathC.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat).ToMathNet());

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.ConjugateTransposeThisAndMultiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void SparseCOOMatrixConjTrans_Multiply_DenseVector_Test()
		{
			RT.Reset(); // Arrange
			using var y = new DenseVector<FloatComplex>(length: 10);
			using var x = new DenseVector<FloatComplex>(length: 10);
			using var M = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC);
			y.FillWithRandoms();
			x.FillWithRandoms();
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			MatrixOperation opM = MatrixOperation.ConjugateTranspose;
			

			var hostvec1 = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());
			var hostvec2 = MathC.DenseVector.OfArray(RT.CopyOutArray(x).ToMathNet());
			using var dnmat = M.ToDense();
			var hostmat = MathC.DenseMatrix.OfColumnMajor(10, 10, RT.CopyOutArray(dnmat).ToMathNet());

			// Act
			API.SparseMatrixDenseVectorMultiply(M, x, y, 1, 1, opM);

			var test = RT.CopyOutArray(y);
			var real = hostmat.ConjugateTransposeThisAndMultiply(hostvec2).Add(hostvec1).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}
		#endregion


		#region dense matrix multiply sparse vector test
		[TestMethod]
		public void DenseMatrixNonTransSparseVectorMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var M = new DenseMatrix<FloatComplex>(rows: 20, cols: 20);
			using var x = new SparseVector<FloatComplex>(length: 20, nonZeros: 8);
			using var y = new DenseVector<FloatComplex>(length: 20);
			M.FillWithRandoms();
			x.FillWithRandoms();
			x.FillIndexWithRange((2, 2));
			y.FillWithRandoms();

			MatrixOperation opM = MatrixOperation.None;

			var hostM = MathC.DenseMatrix.OfColumnMajor(20, 20, RT.CopyOutArray(M).ToMathNet());
			var hostx = MathC.SparseVector.OfIndexedEnumerable(20, x.ToMathNetSparse());
			var hosty = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());

			// Act
			API.DenseMatrixSparseVectorMultiply(M, x, y, 1, 1, opM);
			var test = RT.CopyOutArray(y);

			var real = hostM.Multiply(hostx).Add(hosty).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void DenseMatrixTransSparseVectorMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var M = new DenseMatrix<FloatComplex>(rows: 20, cols: 20);
			using var x = new SparseVector<FloatComplex>(length: 20, nonZeros: 8);
			using var y = new DenseVector<FloatComplex>(length: 20);
			M.FillWithRandoms();
			x.FillWithRandoms();
			x.FillIndexWithRange((2, 2));
			y.FillWithRandoms();

			MatrixOperation opM = MatrixOperation.Transpose;

			var hostM = MathC.DenseMatrix.OfColumnMajor(20, 20, RT.CopyOutArray(M).ToMathNet());
			var hostx = MathC.SparseVector.OfIndexedEnumerable(20, x.ToMathNetSparse());
			var hosty = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());

			// Act
			API.DenseMatrixSparseVectorMultiply(M, x, y, 1, 1, opM);
			var test = RT.CopyOutArray(y);

			var real = hostM.TransposeThisAndMultiply(hostx).Add(hosty).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}

		[TestMethod]
		public void DenseMatrixConjTransSparseVectorMultiplyTest()
		{
			RT.Reset(); // Arrange
			using var M = new DenseMatrix<FloatComplex>(rows: 20, cols: 20);
			using var x = new SparseVector<FloatComplex>(length: 20, nonZeros: 8);
			using var y = new DenseVector<FloatComplex>(length: 20);
			M.FillWithRandoms();
			x.FillWithRandoms();
			x.FillIndexWithRange((2, 2));
			y.FillWithRandoms();

			MatrixOperation opM = MatrixOperation.ConjugateTranspose;

			var hostM = MathC.DenseMatrix.OfColumnMajor(20, 20, RT.CopyOutArray(M).ToMathNet());
			var hostx = MathC.SparseVector.OfIndexedEnumerable(20, x.ToMathNetSparse());
			var hosty = MathC.DenseVector.OfArray(RT.CopyOutArray(y).ToMathNet());

			// Act
			API.DenseMatrixSparseVectorMultiply(M, x, y, 1, 1, opM);
			var test = RT.CopyOutArray(y);

			var real = hostM.ConjugateTransposeThisAndMultiply(hostx).Add(hosty).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(test));
		}
		#endregion


		#region to dense test
		[TestMethod]
		public void MatrixSparseCSRToDenseTest()
		{
			RT.Reset(); // Arrange
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));
			using var dest = new DenseMatrix<double>(rows: 10, cols: 10);

			// Act
			API.MatrixSparseCSRToDense(dest, M);

			using var vec = new DenseVector<double>(length: 10);
			RT.CopyTo(source: M, dest: vec, length: 10);
			using var real = new DenseMatrix<double>(rows: 10, cols: 10);
			real.SetDiag(0, vec);

			// Assert
			Assert.IsTrue(RT.CopyOutArray(real).ApproxEqual(RT.CopyOutArray(dest)));
		}

		[TestMethod]
		public void MatrixSparseCSCToDenseTest()
		{
			RT.Reset(); // Arrange
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC);
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));
			using var dest = new DenseMatrix<double>(rows: 10, cols: 10);

			// Act
			API.MatrixSparseCSCToDense(dest, M);

			using var vec = new DenseVector<double>(length: 10);
			RT.CopyTo(source: M, dest: vec, length: 10);
			using var real = new DenseMatrix<double>(rows: 10, cols: 10);
			real.SetDiag(0, vec);

			// Assert
			Assert.IsTrue(RT.CopyOutArray(real).ApproxEqual(RT.CopyOutArray(dest)));
		}
		#endregion


		#region to sparse test
		[TestMethod]
		public void MatrixDenseToSparseCSRTest()
		{
			RT.Reset(); // Arrange
			using var M = new DenseMatrix<double>(rows: 10, cols: 10);
			using var vec = new DenseVector<double>(length: 9);
			vec.FillWithRandoms();
			M.SetDiag(1, vec);
			M.SetDiag(2, vec);

			// Act
			using var sp = API.MatrixDenseToSparseCSR(M);

			using var test = sp.ToDense();

			// Assert
			Assert.IsTrue(RT.CopyOutArray(M).ApproxEqual(RT.CopyOutArray(test)));
		}

		[TestMethod]
		public void MatrixDenseToSparseCSCTest()
		{
			RT.Reset(); // Arrange
			using var M = new DenseMatrix<double>(rows: 10, cols: 10);
			using var vec = new DenseVector<double>(length: 9);
			vec.FillWithRandoms();
			M.SetDiag(1, vec);
			M.SetDiag(2, vec);

			// Act
			using var sp = API.MatrixDenseToSparseCSC(M);

			using var test = sp.ToDense();

			// Assert
			Assert.IsTrue(RT.CopyOutArray(M).ApproxEqual(RT.CopyOutArray(test)));
		}

		[TestMethod]
		public void MatrixDensePruneToSparseCSRTest()
		{
			RT.Reset(); // Arrange
			using var M = new DenseMatrix<double>(rows: 10, cols: 10);
			using var vec = new DenseVector<double>(length: 9);
			vec.FillWithRandoms();
			M.SetDiag(1, vec);
			M.SetDiag(2, vec);

			// Act
			using var sp = API.MatrixDensePruneToCSR(M, 0.5f);

			using var test = sp.ToDense();

			// Assert
			var real = RT.CopyOutArray(M).Select(a => a <= 0.5 ? 0 : a).ToArray();
			Assert.IsTrue(real.ApproxEqual(RT.CopyOutArray(test)));
		}

		[TestMethod]
		public void MatrixCSCPruneTest()
		{
			RT.Reset(); // Arrange
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC);
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var sp = API.MatrixCompressedPrune(M, 0.5f);

			// Assert
			var real = RT.CopyOutArray(M).Where(a => a > 0.5).ToArray();
			Assert.IsTrue(real.ApproxEqual(RT.CopyOutArray(sp)));
		}

		[TestMethod]
		public void MatrixCSRPruneTest()
		{
			RT.Reset(); // Arrange
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var sp = API.MatrixCompressedPrune(M, 0.5f);

			// Assert
			var real = RT.CopyOutArray(M).Where(a => a > 0.5).ToArray();
			Assert.IsTrue(real.ApproxEqual(RT.CopyOutArray(sp)));
		}
		#endregion


		#region format conversion test
		private static void MatrixFormatConvertTest(SparseMatrixFormat from, SparseMatrixFormat to, MatrixOperation op)
		{
			RT.Reset(); // Arrange
			using var M = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: from);
			M.FillWithRandoms();
			M.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var temp = API.MatrixSparseFormatConvert(M, to, op);
			using var result = API.MatrixSparseFormatConvert(temp, from, op);

			// Assert
			Assert.AreEqual(to, temp.Format);
			Assert.IsTrue(M.ValueToFortranOrderArray().SequenceEqual(result.ValueToFortranOrderArray()));
			var Mind = M.IndexToIntArray();
			var Rind = result.IndexToIntArray();
			Assert.IsTrue(Mind.First().SequenceEqual(Rind.First()));
			Assert.IsTrue(Mind.Last().SequenceEqual(Rind.Last()));
		}

		[TestMethod]
		public void MatrixFormatConvertTest()
		{
			var formats = new[] { SparseMatrixFormat.CSR, SparseMatrixFormat.CSC, SparseMatrixFormat.COOC, SparseMatrixFormat.COOR };
			var ops = new[] { MatrixOperation.None, MatrixOperation.Transpose, MatrixOperation.ConjugateTranspose };
			var combs = from @from in formats
						from to in formats
						from op in ops
						where @from != to
						select (@from, to, op);
			foreach (var (from, to, op) in combs)
			{
				MatrixFormatConvertTest(from, to, op);
			}
		}
		#endregion


		#region matrix add test
		[TestMethod]
		public void MatrixSparseAddSparseTest()
		{
			RT.Reset(); // Arrange
			using var A = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			A.FillWithRandoms();
			A.FillIndexWithRange((0, 1), (0, 1));
			using var B = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			B.FillWithRandoms();
			B.FillIndexWithRange((0, 1), (0, 1));
			using var result = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);

			var hostM = MathD.SparseMatrix.OfIndexed(10, 10, A.ToMathNetSparse());
			var hostN = MathD.SparseMatrix.OfIndexed(10, 10, B.ToMathNetSparse());

			// Act
			API.MatrixSparseAddSparse(A, B, result, 1, 1);
			using var test = result.ToDense();

			var real = hostM.Add(hostN);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(test.ToFortranOrderArray()));
		}
		#endregion


		#region matrix multiply test
		[TestMethod]
		public void MatrixSparseMultiplySparseTest()
		{
			RT.Reset(); // Arrange
			using var A = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			A.FillWithRandoms();
			A.FillIndexWithRange((0, 1), (0, 1));
			using var B = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			B.FillWithRandoms();
			B.FillIndexWithRange((0, 1), (0, 1));
			using var D = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			D.FillWithRandoms();
			D.FillIndexWithRange((0, 1), (0, 1));

			var hostA = MathD.SparseMatrix.OfIndexed(10, 10, A.ToMathNetSparse());
			var hostB = MathD.SparseMatrix.OfIndexed(10, 10, B.ToMathNetSparse());
			var hostD = MathD.SparseMatrix.OfIndexed(10, 10, D.ToMathNetSparse());

			// Act
			API.MatrixSparseMultiplySparse(A, B, D, 1, 1);
			using var test = D.ToDense();

			var real = hostA.Multiply(hostB).Add(hostD);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(test.ToFortranOrderArray()));
		}

		[TestMethod]
		public void MatrixDenseMultiplySparseTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<double>(rows: 10, cols: 10);
			A.FillWithRandoms();
			using var B = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC);
			B.FillWithRandoms();
			B.FillIndexWithRange((0, 1), (0, 1));
			using var D = new DenseMatrix<double>(rows: 10, cols: 10);
			D.FillWithRandoms();

			var hostA = MathD.DenseMatrix.OfColumnMajor(10, 10, A.ToFortranOrderArray());
			var hostB = MathD.SparseMatrix.OfIndexed(10, 10, B.ToMathNetSparse());
			var hostD = MathD.DenseMatrix.OfColumnMajor(10, 10, D.ToFortranOrderArray());

			// Act
			API.MatrixDenseMultiplySparse(A, B, D, 1, 1);

			var real = hostA.Multiply(hostB).Add(hostD);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(D.ToFortranOrderArray()));
		}

		[TestMethod]
		public void MatrixSparseNonTransMultiplyDenseNonTransTest()
		{
			RT.Reset(); // Arrange
			using var B = new DenseMatrix<double>(rows: 10, cols: 10);
			B.FillWithRandoms();
			using var A = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			A.FillWithRandoms();
			A.FillIndexWithRange((0, 1), (0, 1));
			using var D = new DenseMatrix<double>(rows: 10, cols: 10);
			D.FillWithRandoms();

			var hostB = MathD.DenseMatrix.OfColumnMajor(10, 10, B.ToFortranOrderArray());
			var hostA = MathD.SparseMatrix.OfIndexed(10, 10, A.ToMathNetSparse());
			var hostD = MathD.DenseMatrix.OfColumnMajor(10, 10, D.ToFortranOrderArray());

			var opA = MatrixOperation.None;
			var opB = MatrixOperation.None;

			// Act
			API.MatrixSparseMultiplyDense(A, B, D, 1, 1, opA, opB);

			var real = hostA.Multiply(hostB).Add(hostD);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(D.ToFortranOrderArray()));
		}

		[TestMethod]
		public void MatrixSparseTransMultiplyDenseTransTest()
		{
			RT.Reset(); // Arrange
			using var B = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			B.FillWithRandoms();
			using var A = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			A.FillWithRandoms();
			A.FillIndexWithRange((0, 1), (0, 1));
			using var D = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			D.FillWithRandoms();

			var hostB = MathC.DenseMatrix.OfColumnMajor(10, 10, B.ToFortranOrderArray().ToMathNet());
			var hostA = MathC.SparseMatrix.OfIndexed(10, 10, A.ToMathNetSparse());
			var hostD = MathC.DenseMatrix.OfColumnMajor(10, 10, D.ToFortranOrderArray().ToMathNet());

			var opA = MatrixOperation.Transpose;
			var opB = MatrixOperation.Transpose;

			// Act
			API.MatrixSparseMultiplyDense(A, B, D, 1, 1, opA, opB);

			var real = hostA.TransposeThisAndMultiply(hostB.Transpose()).Add(hostD);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(D.ToFortranOrderArray()));
		}

		[TestMethod]
		public void MatrixSparseConjTransMultiplyDenseConjTransTest()
		{
			RT.Reset(); // Arrange
			using var B = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			B.FillWithRandoms();
			using var A = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR);
			A.FillWithRandoms();
			A.FillIndexWithRange((0, 1), (0, 1));
			using var D = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			D.FillWithRandoms();

			var hostB = MathC.DenseMatrix.OfColumnMajor(10, 10, B.ToFortranOrderArray().ToMathNet());
			var hostA = MathC.SparseMatrix.OfIndexed(10, 10, A.ToMathNetSparse());
			var hostD = MathC.DenseMatrix.OfColumnMajor(10, 10, D.ToFortranOrderArray().ToMathNet());

			var opA = MatrixOperation.ConjugateTranspose;
			var opB = MatrixOperation.ConjugateTranspose;

			// Act
			API.MatrixSparseMultiplyDense(A, B, D, 1, 1, opA, opB);

			var real = hostA.ConjugateTransposeThisAndMultiply(hostB.ConjugateTranspose()).Add(hostD);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(D.ToFortranOrderArray()));
		}
		#endregion
	}
}
