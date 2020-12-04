using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

using CudaCSharp.Linq;
using CudaCSharp.Memory;
using static CudaCSharp.Arrays.Tests.Utilities;

using MathNet.Numerics;
using MathS = MathNet.Numerics.LinearAlgebra.Single;
using MathD = MathNet.Numerics.LinearAlgebra.Double;
using MathC = MathNet.Numerics.LinearAlgebra.Complex32;
using MathZ = MathNet.Numerics.LinearAlgebra.Complex;

using RT = CudaCSharp.Runtime.API;


namespace CudaCSharp.Arrays.Tests
{
	[TestClass()]
	public class DenseMatrixTests
	{
		[TestMethod()]
		public void DenseMatrixBaseCreateTest()
		{
			RT.Reset(); // Arrange
			// Act
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			// Assert
			Assert.AreEqual(10, m.NRows);
			Assert.AreEqual(2, m.NCols);
			Assert.IsFalse(m.Hermitian);
		}

		[TestMethod()]
		public void DenseMatrixCopyCreateTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			m.FillWithRandoms();
			// Act
			using var mm = new DenseMatrix<double>(matrix: m);
			// Assert
			Assert.AreNotEqual(m, mm);
			Assert.IsTrue(m.ToFortranOrderArray().SequenceEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void DenseMatrixFullCreateTest()
		{
			RT.Reset(); // Arrange
			using var val = Storage<double>.Create(length: 20);
			Rng.API.FillWithRandom(new DenseVector<double>(val, 20));

			// Act
			using var m = new DenseMatrix<double>(values: val, rows: 4, cols: 4, ld: 5, herm: false, offset: 0);

			var real = RT.CopyOutColumnMajorMatrix(val, leadDim: 5, copyCols: 4, copyRows: 4);

			// Assert
			Assert.AreEqual(20, m.ActualLength);
			Assert.IsTrue(real.SequenceEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void DenseMatrixRefCreateTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			m.FillWithRandoms();
			// Act
			using var refm = new DenseMatrix<double>(refArray: m, newRows: 4, newCols: 4, newLD: 5, herm: false, offset: 0);
			// Assert
			Assert.IsTrue(m.ToFortranOrderArray().SequenceEqual(RT.CopyOutArray(refm)));
		}

		[TestMethod()]
		public void ToMatrixTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			// Act
			var mm = m.ToMatrix();
			// Assert
			Assert.AreSame(m, mm);
		}

		[TestMethod()]
		public void FillWithZerosTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			// Act
			m.FillWithZeros();
			// Assert
			Assert.IsTrue(m.ToFortranOrderArray().All(a => a == 0));
		}

		[TestMethod()]
		public void FillWithRandomsTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			// Act
			m.FillWithRandoms();
			// Assert
			Assert.AreEqual(m.Length, m.ToFortranOrderArray().Distinct().Count());
		}

		[TestMethod()]
		public void ToDenseTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			// Act
			var mm = m.ToDense();
			// Assert
			Assert.AreSame(m, mm);
		}

		[TestMethod()]
		public void ConjugateOutofPlaceTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 2, herm: false);
			m.FillWithRandoms();
			// Act
			using var mm = m.ConjugateOutOfPlace() as DenseMatrix<FloatComplex>;
			// Assert
			Assert.IsTrue(m.ToFortranOrderArray().Select(a => a.Conjugate()).ToArray().ApproxEqual(mm.ToFortranOrderArray()));
		}

		#region to sparse tests
		public static void ToSparseTest(float thre, SparseMatrixFormat format, DenseMatrixToSparseAlgorithm algorithm)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 4, cols: 5, herm: false);
			m.FillWithRandoms();
			if (thre == 0)
			{
				var sub = m.GetSubmatrix(rowRange: .., columnRange: 1..^2);
				sub.FillWithZeros();
			}
			// Act
			using var sp = m.ToSparse(thre, format, algorithm);
			using var dn = sp.ToDense();
			var real = m.ToFortranOrderArray().Select(a => Math.Abs(a) <= thre ? 0 : a).ToArray();
			// Assert
			Assert.IsTrue(real.ApproxEqual(dn.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ToSparseCSRZeroThresholdDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCSCZeroThresholdDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOORZeroThresholdDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOOCZeroThresholdDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCSRNonZeroThresholdDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCSCNonZeroThresholdDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOORNonZeroThresholdDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOOCNonZeroThresholdDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.ZeroThresholdDefault);
		}

		////

		[TestMethod()]
		public void ToSparseCSRZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCSCZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOORZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOOCZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCSRNonZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCSCNonZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOORNonZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		[TestMethod()]
		public void ToSparseCOOCNonZeroThresholdNonZeroDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault);
		}

		////

		[TestMethod()]
		public void ToSparseCSRZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		[TestMethod()]
		public void ToSparseCSCZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		[TestMethod()]
		public void ToSparseCOORZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		[TestMethod()]
		public void ToSparseCOOCZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		[TestMethod()]
		public void ToSparseCSRNonZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		[TestMethod()]
		public void ToSparseCSCNonZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		[TestMethod()]
		public void ToSparseCOORNonZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		[TestMethod()]
		public void ToSparseCOOCNonZeroThresholdRealDefaultTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.RealDefault);
		}

		////

		[TestMethod()]
		public void ToSparseCSRZeroThresholdViaVectorTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.ViaVector);
		}

		[TestMethod()]
		public void ToSparseCSCZeroThresholdViaVectorTest()
		{
			ToSparseTest(0, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.ViaVector);
		}

		[TestMethod()]
		public void ToSparseCOORZeroThresholdViaVectorTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.ViaVector);
		}

		[TestMethod()]
		public void ToSparseCOOCZeroThresholdViaVectorTest()
		{
			ToSparseTest(0, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.ViaVector);
		}

		[TestMethod()]
		public void ToSparseCSRNonZeroThresholdViaVectorTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSR, DenseMatrixToSparseAlgorithm.ViaVector);
		}

		[TestMethod()]
		public void ToSparseCSCNonZeroThresholdViaVectorTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.CSC, DenseMatrixToSparseAlgorithm.ViaVector);
		}

		[TestMethod()]
		public void ToSparseCOORNonZeroThresholdViaVectorTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOR, DenseMatrixToSparseAlgorithm.ViaVector);
		}

		[TestMethod()]
		public void ToSparseCOOCNonZeroThresholdViaVectorTest()
		{
			ToSparseTest(0.5f, SparseMatrixFormat.COOC, DenseMatrixToSparseAlgorithm.ViaVector);
		}
		#endregion

		[TestMethod()]
		public void NewArrayAlikeTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			// Act
			using var mm = m.NewArrayAlike() as DenseMatrix<double>;
			// Assert
			Assert.AreNotEqual(m, mm);
			Assert.AreEqual(m.NRows, mm.NRows);
			Assert.AreEqual(m.NCols, mm.NCols);
		}

		[TestMethod()]
		public void AsDenseVectorTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			// Act
			var v = m.AsDenseVector();
			// Assert
			Assert.AreEqual(m.Length, v.Length);
		}

		[TestMethod()]
		public void DataTypeCastSameTypeTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 4, cols: 5);

			// act
			var newm = m.DataTypeCast<double>();

			// assert
			Assert.AreSame(m, newm);
		}

		[TestMethod()]
		public void DataTypeCastDifferentTypeTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<float>(rows: 4, cols: 5);
			m.FillWithRandoms();

			// act
			var newm = m.DataTypeCast<FloatComplex>() as DenseMatrix<FloatComplex>;

			// assert
			var host = RT.CopyOutArray(m);
			var hostnew = RT.CopyOutArray(newm).Select(v => v.Real());
			Assert.IsTrue(host.SequenceEqual(hostnew));
		}

		[TestMethod()]
		public void CopyUpperToLowerRealTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 5, cols: 5, herm: true);
			m.FillWithRandoms();
			// Act
			m.CopyUpperToLower();
			// Assert
			Assert.IsTrue(Extensions.IsHermitian(RT.CopyOutMatrix(m)));
		}

		[TestMethod()]
		public void CopyUpperToLowerComplexTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 5, cols: 5, herm: true);
			m.FillWithRandoms();
			// Act
			m.CopyUpperToLower();
			// Assert
			Assert.IsTrue(Extensions.IsHermitian(RT.CopyOutMatrix(m)));
		}

		#region range test
		public static void GetRangeTest(Range row, Range col, bool toMat)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var lencol = col.GetOffsetAndCount(m.NCols).Length;
			var lenrow = row.GetOffsetAndCount(m.NRows).Length;
			DenseMatrix<double> mm = null;
			try
			{
				if (row.Equals(Range.All))
				{
					if (toMat)
					{
						mm = new DenseMatrix<double>(rows: 10, cols: lencol, herm: false);
						// Act
						m.GetColumnRange(col, mm as MatrixBase<double>);
					}
					else
					{
						// Act
						mm = m.GetColumnRange(col, null as MatrixBase<double>) as DenseMatrix<double>;
					}
				}
				else if (col.Equals(Range.All))
				{
					if (toMat)
					{
						mm = new DenseMatrix<double>(rows: lenrow, cols: 10, herm: false);
						// Act
						m.GetRowRange(row, mm as MatrixBase<double>);
					}
					else
					{
						// Act
						mm = m.GetRowRange(row, null as MatrixBase<double>) as DenseMatrix<double>;
					}
				}
				else
				{
					if (toMat)
					{
						mm = new DenseMatrix<double>(rows: lenrow, cols: lencol, herm: false);
						// Act
						m.GetSubmatrix(row, col, mm as MatrixBase<double>);
					}
					else
					{
						// Act
						mm = m.GetSubmatrix(row, col, null as MatrixBase<double>) as DenseMatrix<double>;
					}
				}
				// Assert
				if (!toMat)
					Assert.AreEqual(m.LeadDim, mm.LeadDim);
				Assert.AreEqual(lenrow, mm.NRows);
				Assert.AreEqual(lencol, mm.NCols);
				Assert.IsTrue(m.ToFortranOrderArray(row, col).SequenceEqual(mm.ToFortranOrderArray()));
			}
			finally
			{
				mm?.Dispose();
			}
		}

		[TestMethod()]
		public void GetColumnRangeTest()
		{
			GetRangeTest(.., 1..^1, false);
		}

		[TestMethod()]
		public void GetColumnRangeToMatTest()
		{
			GetRangeTest(.., 1..^1, true);
		}

		[TestMethod()]
		public void GetRowRangeTest()
		{
			GetRangeTest(1..^1, .., false);
		}

		[TestMethod()]
		public void GetRowRangeToMatTest()
		{
			GetRangeTest(1..^1, .., true);
		}

		[TestMethod()]
		public void GetSubMatrixTest()
		{
			GetRangeTest(1..^1, 1..^1, false);
		}

		[TestMethod()]
		public void GetSubMatrixToMatTest()
		{
			GetRangeTest(1..^1, 1..^1, true);
		}


		[TestMethod()]
		public void GetColumnsTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();

			// Act
			var vs = m.GetColumns(overwrite: null as VectorBase<double>[]) as DenseVector<double>[];
			try
			{
				// Assert
				var bs = vs.Select((v, i) => m.ToFortranOrderArray(.., i..(i + 1)).SequenceEqual(v.ToFortranOrderArray()));
				Assert.IsTrue(bs.AllTrue());
			}
			finally
			{
				Array.ForEach(vs, v => v?.Dispose());
			}
		}

		[TestMethod()]
		public void GetColumnsToVecTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var vs = new DenseVector<double>[10];
			vs = vs.Select(v => new DenseVector<double>(10)).ToArray();

			// Act
			m.GetColumns(overwrite: vs as VectorBase<double>[]);
			try
			{
				// Assert
				var bs = vs.Select((v, i) => m.ToFortranOrderArray(.., i..(i + 1)).SequenceEqual(v.ToFortranOrderArray()));
				Assert.IsTrue(bs.AllTrue());
			}
			finally
			{
				Array.ForEach(vs, v => v?.Dispose());
			}
		}

		[TestMethod()]
		public void GetRowsTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();

			// Act
			var vs = m.GetRows(overwrite: null as VectorBase<double>[]) as DenseVector<double>[];
			try
			{
				// Assert
				var bs = vs.Select((v, i) => m.ToFortranOrderArray(i..(i + 1), ..).SequenceEqual(v.ToFortranOrderArray()));
				Assert.IsTrue(bs.AllTrue());
			}
			finally
			{
				Array.ForEach(vs, v => v?.Dispose());
			}
		}

		[TestMethod()]
		public void GetRowsToVecTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var vs = new DenseVector<double>[10];
			vs = vs.Select(v => new DenseVector<double>(10)).ToArray();

			// Act
			m.GetRows(overwrite: vs as VectorBase<double>[]);
			try
			{
				// Assert
				var bs = vs.Select((v, i) => m.ToFortranOrderArray(i..(i + 1), ..).SequenceEqual(v.ToFortranOrderArray()));
				Assert.IsTrue(bs.AllTrue());
			}
			finally
			{
				Array.ForEach(vs, v => v?.Dispose());
			}
		}

		[TestMethod()]
		public void GetColumnAtTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();

			// Act
			using var v = m.GetColumnAt(2, overwrite: null as VectorBase<double>) as DenseVector<double>;

			// Assert
			Assert.IsTrue(m.ToFortranOrderArray(.., 2..3).SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void GetColumnAtToVecTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			using var v = new DenseVector<double>(length: 10);

			// Act
			m.GetColumnAt(2, overwrite: v as VectorBase<double>);

			// Assert
			Assert.IsTrue(m.ToFortranOrderArray(.., 2..3).SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void GetRowAtTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();

			// Act
			using var v = m.GetRowAt(2, overwrite: null as VectorBase<double>) as DenseVector<double>;

			// Assert
			Assert.IsTrue(m.ToFortranOrderArray(2..3, ..).SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void GetRowAtToVecTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			using var v = new DenseVector<double>(length: 10);

			// Act
			m.GetRowAt(2, overwrite: v as VectorBase<double>);

			// Assert
			Assert.IsTrue(m.ToFortranOrderArray(2..3, ..).SequenceEqual(v.ToFortranOrderArray()));
		}
		#endregion


		[TestMethod()]
		public void CloneTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();

			// Act
			using var mm = m.Clone() as DenseMatrix<double>;

			// Assert
			Assert.AreNotEqual(m, mm);
			Assert.AreEqual(m.NRows, mm.NRows);
			Assert.AreEqual(m.NCols, mm.NCols);
			Assert.IsTrue(m.ToFortranOrderArray().SequenceEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void TransposeTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathD.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray());

			// Act
			using var mm = m.Transpose(null as MatrixBase<double>) as DenseMatrix<double>;

			var real = hostm.Transpose();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void TransposeToMatTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathD.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray());
			using var mm = new DenseMatrix<double>(rows: 10, cols: 10);

			// Act
			m.Transpose(mm as MatrixBase<double>);

			var real = hostm.Transpose();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ConjugateTransposeTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			using var mm = m.ConjugateTranspose(null as MatrixBase<FloatComplex>) as DenseMatrix<FloatComplex>;

			var real = hostm.ConjugateTranspose();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ConjugateTransposeToMatTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			using var mm = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);

			// Act
			m.ConjugateTranspose(mm as MatrixBase<FloatComplex>);

			var real = hostm.ConjugateTranspose();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}


		#region from matrix add tests
		public static void FromDenseAddDenseTest(MatrixOperation op1, MatrixOperation op2)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			using var a = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			using var b = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			a.FillWithRandoms();
			b.FillWithRandoms();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.DenseMatrix.OfColumnMajor(10, 10, a.ToFortranOrderArray().ToMathNet());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, b.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.From_αA_Add_βB(a as MatrixBase<FloatComplex>, b, 1, 1, op1, op2);
			var real = hosta.Add(hostb);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void FromDenseNonTransAddDenseNonTransTest()
		{
			FromDenseAddDenseTest(MatrixOperation.None, MatrixOperation.None);
		}

		[TestMethod()]
		public void FromDenseNonTransAddDenseTransTest()
		{
			FromDenseAddDenseTest(MatrixOperation.None, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void FromDenseNonTransAddDenseConjTransTest()
		{
			FromDenseAddDenseTest(MatrixOperation.None, MatrixOperation.ConjugateTranspose);
		}

		[TestMethod()]
		public void FromDenseTransAddDenseTransTest()
		{
			FromDenseAddDenseTest(MatrixOperation.Transpose, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void FromDenseTransAddDenseConjTransTest()
		{
			FromDenseAddDenseTest(MatrixOperation.Transpose, MatrixOperation.ConjugateTranspose);
		}

		[TestMethod()]
		public void FromDenseConjTransAddDenseConjTransTest()
		{
			FromDenseAddDenseTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose);
		}


		public static void FromSparseAddDenseTest(MatrixOperation op1, MatrixOperation op2, SparseMatrixFormat format)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format);
			using var b = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			a.FillWithRandoms();
			a.FillIndexWithRange((0, 1), (0, 1));
			b.FillWithRandoms();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.SparseMatrix.OfIndexed(10, 10, a.ToMathNetSparse());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, b.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.From_αA_Add_βB(a as MatrixBase<FloatComplex>, b, 1, 1, op1, op2);
			var real = hosta.Add(hostb);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void FromSparseNonTransAddDenseNonTransTest()
		{
			FromSparseAddDenseTest(MatrixOperation.None, MatrixOperation.None, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void FromSparseNonTransAddDenseTransTest()
		{
			FromSparseAddDenseTest(MatrixOperation.None, MatrixOperation.Transpose, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void FromSparseNonTransAddDenseConjTransTest()
		{
			FromSparseAddDenseTest(MatrixOperation.None, MatrixOperation.ConjugateTranspose, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void FromSparseTransAddDenseTransTest()
		{
			FromSparseAddDenseTest(MatrixOperation.Transpose, MatrixOperation.Transpose, SparseMatrixFormat.CSC);
		}

		[TestMethod()]
		public void FromSparseTransAddDenseConjTransTest()
		{
			FromSparseAddDenseTest(MatrixOperation.Transpose, MatrixOperation.ConjugateTranspose, SparseMatrixFormat.CSC);
		}

		[TestMethod()]
		public void FromSparseConjTransAddDenseConjTransTest()
		{
			FromSparseAddDenseTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose, SparseMatrixFormat.CSC);
		}


		public static void FromSparseAddSparseTest(MatrixOperation op1, MatrixOperation op2, SparseMatrixFormat format1, SparseMatrixFormat format2)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format1);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 14, format2);
			a.FillWithRandoms();
			a.FillIndexWithRange((0, 1), (0, 1));
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 1), (0, 1));
			using var db = b.ToDense();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.SparseMatrix.OfIndexed(10, 10, a.ToMathNetSparse());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, db.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.From_αA_Add_βB(a, b, 1, 1, op1, op2);
			var real = hosta.Add(hostb);
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void FromSparseNonTransAddSparseNonTransTest()
		{
			FromSparseAddSparseTest(MatrixOperation.None, MatrixOperation.None, SparseMatrixFormat.CSR, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void FromSparseNonTransAddSparseTransTest()
		{
			FromSparseAddSparseTest(MatrixOperation.None, MatrixOperation.Transpose, SparseMatrixFormat.CSR, SparseMatrixFormat.CSC);
		}

		[TestMethod()]
		public void FromSparseNonTransAddSparseConjTransTest()
		{
			FromSparseAddSparseTest(MatrixOperation.None, MatrixOperation.ConjugateTranspose, SparseMatrixFormat.CSR, SparseMatrixFormat.CSC);
		}

		[TestMethod()]
		public void FromSparseTransAddSparseTransTest()
		{
			FromSparseAddSparseTest(MatrixOperation.Transpose, MatrixOperation.Transpose, SparseMatrixFormat.CSC, SparseMatrixFormat.CSC);
		}

		[TestMethod()]
		public void FromSparseTransAddSparseConjTransTest()
		{
			FromSparseAddSparseTest(MatrixOperation.Transpose, MatrixOperation.ConjugateTranspose, SparseMatrixFormat.CSC, SparseMatrixFormat.CSC);
		}

		[TestMethod()]
		public void FromSparseConjTransAddSparseConjTransTest()
		{
			FromSparseAddSparseTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose, SparseMatrixFormat.CSC, SparseMatrixFormat.CSC);
		}


		public static void FromSparseAddSparseZeroATest(MatrixOperation op2, SparseMatrixFormat format1)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 14, format1);
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 1), (0, 1));
			using var db = b.ToDense();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, db.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.From_αA_Add_βB(null, b, 0, 1, default, op2);
			var real = hostb;
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void FromSparseZeroAddSparseNonTransTest()
		{
			FromSparseAddSparseZeroATest(MatrixOperation.None, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void FromSparseZeroAddSparseTransTest()
		{
			FromSparseAddSparseZeroATest(MatrixOperation.Transpose, SparseMatrixFormat.CSC);
		}

		[TestMethod()]
		public void FromSparseZeroAddSparseConjTransTest()
		{
			FromSparseAddSparseZeroATest(MatrixOperation.ConjugateTranspose, SparseMatrixFormat.CSC);
		}
		#endregion



		#region add by matrix multiply tests
		public static void AddByDenseMultiplyDenseTest(MatrixOperation op1, MatrixOperation op2)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			using var a = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			using var b = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			a.FillWithRandoms();
			b.FillWithRandoms();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.DenseMatrix.OfColumnMajor(10, 10, a.ToFortranOrderArray().ToMathNet());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, b.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.Mulβ_AddBy_αAB(a as MatrixBase<FloatComplex>, b, 1, 1, op1, op2);
			var real = hosta.Multiply(hostb).Add(hostm);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddByDenseNonTransMultiplyDenseNonTransTest()
		{
			AddByDenseMultiplyDenseTest(MatrixOperation.None, MatrixOperation.None);
		}

		[TestMethod()]
		public void AddByDenseTransMultiplyDenseTransTest()
		{
			AddByDenseMultiplyDenseTest(MatrixOperation.Transpose, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddByDenseConjTransMultiplyDenseConjTransTest()
		{
			AddByDenseMultiplyDenseTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose);
		}

		public static void AddBySparseMultiplySparseTest(MatrixOperation op1, MatrixOperation op2, SparseMatrixFormat format1 = SparseMatrixFormat.CSC, SparseMatrixFormat format2 = SparseMatrixFormat.CSC)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format1);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format2);
			a.FillWithRandoms();
			a.FillIndexWithRange((0, 1), (0, 1));
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 1), (0, 1));
			using var da = a.ToDense();
			using var db = b.ToDense();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.DenseMatrix.OfColumnMajor(10, 10, da.ToFortranOrderArray().ToMathNet());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, db.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.Mulβ_AddBy_αAB(a as MatrixBase<FloatComplex>, b, 1, 1, op1, op2);
			var real = hosta.Multiply(hostb).Add(hostm);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBySparseNonTransMultiplySparseNonTransTest()
		{
			AddBySparseMultiplySparseTest(MatrixOperation.None, MatrixOperation.None, SparseMatrixFormat.CSR, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void AddBySparseTransMultiplySparseTransTest()
		{
			AddBySparseMultiplySparseTest(MatrixOperation.Transpose, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBySparseConjTransMultiplySparseConjTransTest()
		{
			AddBySparseMultiplySparseTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose);
		}


		public static void AddBySparseMultiplySparseNoAddTest(MatrixOperation op1, MatrixOperation op2, SparseMatrixFormat format1 = SparseMatrixFormat.CSC, SparseMatrixFormat format2 = SparseMatrixFormat.CSC)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format1);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format2);
			a.FillWithRandoms();
			a.FillIndexWithRange((0, 1), (0, 1));
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 1), (0, 1));
			using var da = a.ToDense();
			using var db = b.ToDense();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.DenseMatrix.OfColumnMajor(10, 10, da.ToFortranOrderArray().ToMathNet());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, db.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.Mulβ_AddBy_αAB(a as MatrixBase<FloatComplex>, b, 1, 0, op1, op2);
			var real = hosta.Multiply(hostb);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBySparseNonTransMultiplySparseNonTransNoAddTest()
		{
			AddBySparseMultiplySparseNoAddTest(MatrixOperation.None, MatrixOperation.None, SparseMatrixFormat.CSR, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void AddBySparseTransMultiplySparseTransNoAddTest()
		{
			AddBySparseMultiplySparseNoAddTest(MatrixOperation.Transpose, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBySparseConjTransMultiplySparseConjTransNoAddTest()
		{
			AddBySparseMultiplySparseNoAddTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose);
		}



		public static void AddBySparseMultiplyDenseTest(MatrixOperation op1, MatrixOperation op2, SparseMatrixFormat format = SparseMatrixFormat.CSC)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format);
			using var b = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			a.FillWithRandoms();
			a.FillIndexWithRange((0, 1), (0, 1));
			b.FillWithRandoms();
			using var da = a.ToDense();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.DenseMatrix.OfColumnMajor(10, 10, da.ToFortranOrderArray().ToMathNet());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, b.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.Mulβ_AddBy_αAB(a as MatrixBase<FloatComplex>, b, 1, 1, op1, op2);
			var real = hosta.Multiply(hostb).Add(hostm);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBySparseNonTransMultiplyDenseNonTransTest()
		{
			AddBySparseMultiplyDenseTest(MatrixOperation.None, MatrixOperation.None, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void AddBySparseTransMultiplyDenseTransTest()
		{
			AddBySparseMultiplyDenseTest(MatrixOperation.Transpose, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBySparseConjTransMultiplyDenseConjTransTest()
		{
			AddBySparseMultiplyDenseTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose);
		}


		public static void AddByDenseMultiplySparseTest(MatrixOperation op1, MatrixOperation op2, SparseMatrixFormat format = SparseMatrixFormat.CSC)
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format);
			using var a = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 1), (0, 1));
			a.FillWithRandoms();
			using var db = b.ToDense();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hosta = MathC.DenseMatrix.OfColumnMajor(10, 10, a.ToFortranOrderArray().ToMathNet());
			hosta = op1 == MatrixOperation.None ? hosta : op1 == MatrixOperation.Transpose ? hosta.Transpose() : hosta.ConjugateTranspose();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, db.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();
			
			// Act
			m.Mulβ_AddBy_αAB(a as MatrixBase<FloatComplex>, b, 1, 1, op1, op2);
			var real = hosta.Multiply(hostb).Add(hostm);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddByDenseNonTransMultiplySparseNonTransTest()
		{
			AddByDenseMultiplySparseTest(MatrixOperation.None, MatrixOperation.None, SparseMatrixFormat.CSR);
		}

		[TestMethod()]
		public void AddByDenseTransMultiplySparseTransTest()
		{
			AddByDenseMultiplySparseTest(MatrixOperation.Transpose, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddByDenseConjTransMultiplySparseConjTransTest()
		{
			AddByDenseMultiplySparseTest(MatrixOperation.ConjugateTranspose, MatrixOperation.ConjugateTranspose);
		}
		#endregion


		#region eigensystem hermitian tests
		[TestMethod()]
		public void EigenHermitianDoubleComplexTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<DoubleComplex>(rows: 10, cols: 10, herm: true);
			m.FillWithRandoms();
			m.CopyUpperToLower();
			var hostm = MathZ.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			var (values, vectors) = m.EigensystemHerm();
			var val = values; var vec = vectors;
			try
			{
				var real = hostm.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Hermitian);

				// Assert
				Assert.IsTrue(real.EigenValues.Select(a => (float)a.Real).ToArray().ApproxEqual(Array.ConvertAll(val.ToFortranOrderArray(), a => (float)a)));
				Assert.IsTrue(real.EigenVectors.ToColumnMajorArray().Select(a => (Complex32)a).ToArray().ApproxEqual(vec.ToFortranOrderArray().Select(a => (FloatComplex)a).ToArray()));
			}
			finally
			{
				values.Dispose();
				//vectors.Dispose();
			}
		}

		[TestMethod()]
		public void EigenHermitianSingleComplexTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: true);
			m.FillWithRandoms();
			m.CopyUpperToLower();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			var (values, vectors) = m.EigensystemHerm();
			var val = values; var vec = vectors;
			try
			{
				var real = hostm.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Hermitian);

				// Assert
				Assert.IsTrue(real.EigenValues.Select(a => (FloatComplex)a).ToArray().ApproxEqual(val.ToFortranOrderArray()));
				Assert.IsTrue(real.EigenVectors.ToColumnMajorArray().ToArray().ApproxEqual(vec.ToFortranOrderArray()));
			}
			finally
			{
				values.Dispose();
				//vectors.Dispose();
			}
		}

		[TestMethod()]
		public void EigenHermitianRealTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 10, herm: true);
			m.FillWithRandoms();
			m.CopyUpperToLower();
			var hostm = MathD.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray());

			// Act
			var (values, _, vectors) = m.Eigensystem<double>();
			var val = values as DenseVector<double>; var vec = vectors as DenseMatrix<double>;
			try
			{
				var real = hostm.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Hermitian);

				// Assert
				Assert.IsTrue(real.EigenValues.Select(a => (float)a.Real).ToArray().ApproxEqual(val.ToFortranOrderArray().Select(a => (float)a).ToArray()));
				Assert.IsTrue(real.EigenVectors.ToColumnMajorArray().Select(a => (float)a).ToArray().ApproxEqual(vec.ToFortranOrderArray().Select(a => (float)a).ToArray()));
			}
			finally
			{
				values.Dispose();
				//vectors.Dispose();
			}
		}
		// CUDA general eigen have problems
		#endregion



		[TestMethod()]
		public void EigenvalueTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols:10, herm: true);
			m.FillWithRandoms();
			m.CopyUpperToLower();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			using var values = m.Eigenvalue<float>();
			var real = hostm.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Hermitian);

			// Assert
			Assert.IsTrue(real.EigenValues.Select(a => (float)a.Real).ToArray().ApproxEqual(values.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void SVDTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 20);
			m.FillWithRandoms();
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 20, m.ToFortranOrderArray().ToMathNet());

			// Act
			var (S, U, Vct) = m.SingularValues();
			var SS = S as DenseVector<FloatComplex>;
			var UU = U as DenseMatrix<FloatComplex>;
			var VV = Vct as DenseMatrix<FloatComplex>;

			var real = hostm.Svd();

			// Assert
			Assert.IsTrue(real.S.Select(a => (float)a.Real).ToArray().ApproxEqual(Array.ConvertAll(SS.ToFortranOrderArray(), a => a.Real())));
			Assert.IsTrue(real.U.ToColumnMajorArray().ApproxEqual(UU.ToFortranOrderArray()));
			Assert.IsTrue(real.VT.ToColumnMajorArray().ApproxEqual(VV.ToFortranOrderArray()));
		}


		#region Kronecker tests
		[TestMethod()]
		public void KroneckerProdTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			using var n = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			n.FillWithRandoms();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, n.ToFortranOrderArray().ToMathNet());

			// Act
			using var mm = m.KroneckerProd(n as MatrixBase<FloatComplex>, forceHerm: false) as DenseMatrix<FloatComplex>;
			var real = hostm.KroneckerProduct(hostn);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void KroneckerSumTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			using var n = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			n.FillWithRandoms();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, n.ToFortranOrderArray().ToMathNet());
			var hostI = MathC.DenseMatrix.CreateIdentity(10);

			// Act
			using var mm = m.KroneckerSum(n as MatrixBase<FloatComplex>, forceHerm: false) as DenseMatrix<FloatComplex>;
			var real = hostm.KroneckerProduct(hostI) + hostI.KroneckerProduct(hostn);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void KroneckerProdForceHermTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			using var n = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			n.FillWithRandoms();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, n.ToFortranOrderArray().ToMathNet());

			// Act
			using var mm = m.KroneckerProd(n as MatrixBase<FloatComplex>) as DenseMatrix<FloatComplex>;
			var real = (hostm.KroneckerProduct(hostn.ConjugateTranspose()) + hostm.ConjugateTranspose().KroneckerProduct(hostn)) / 2;

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void KroneckerSumForceHermTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();
			using var n = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			n.FillWithRandoms();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, n.ToFortranOrderArray().ToMathNet());
			var hostI = MathC.DenseMatrix.CreateIdentity(10);

			// Act
			using var mm = m.KroneckerSum(n as MatrixBase<FloatComplex>) as DenseMatrix<FloatComplex>;
			var real = ((hostm + hostm.ConjugateTranspose()) / 2).KroneckerProduct(hostI) + hostI.KroneckerProduct((hostn + hostn.ConjugateTranspose()) / 2);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(mm.ToFortranOrderArray()));
		}
		#endregion


		////[TestMethod()]
		////public void From_AdiagXTest()
		////{
		////	RT.Reset(); // Arrange
		////	using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
		////	A.FillWithRandoms();
		////	using var x = new DenseVector<FloatComplex>(length: 10);
		////	x.FillWithRandoms();
		////	using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);

		////	var hostA = MathC.DenseMatrix.OfColumnMajor(10, 10, A.ToFortranOrderArray().ToMathNet());
		////	var hostx = MathC.DiagonalMatrix.OfDiagonal(10, 10, x.ToFortranOrderArray().ToMathNet());

		////	// Act
		////	m.From_AdiagX(A, x);
		////	var real = hostA.Multiply(hostx);

		////	// Assert
		////	Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		////}

		[TestMethod()]
		public void RankKUpdateTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: true);
			m.FillWithRandoms();
			m.CopyUpperToLower();

			var hostA = MathC.DenseMatrix.OfColumnMajor(10, 10, A.ToFortranOrderArray().ToMathNet());
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			m.RankKUpdate(A, 1, 1);
			var real = hostA.ConjugateTransposeAndMultiply(hostA).Add(hostm);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}


		#region diag tests
		[TestMethod()]
		public void GetDiagTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();

			var hostA = MathC.DenseMatrix.OfColumnMajor(10, 10, A.ToFortranOrderArray().ToMathNet());

			// Act
			using var v = A.GetDiag(0, null as VectorBase<FloatComplex>) as DenseVector<FloatComplex>;
			var real = hostA.Diagonal();

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void GetDiagToVecTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();
			using var v = new DenseVector<FloatComplex>(length: 10);
			var hostA = MathC.DenseMatrix.OfColumnMajor(10, 10, A.ToFortranOrderArray().ToMathNet());

			// Act
			A.GetDiag(0, v as VectorBase<FloatComplex>);
			var real = hostA.Diagonal();

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void SetDiagDenseTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();

			var hostA = MathC.DenseMatrix.OfColumnMajor(10, 10, A.ToFortranOrderArray().ToMathNet());

			// Act
			A.SetDiag(0, v as VectorBase<FloatComplex>);
			hostA.SetDiagonal(v.ToFortranOrderArray().ToMathNet());

			// Assert
			Assert.IsTrue(hostA.ToColumnMajorArray().ApproxEqual(A.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void SetDiagSparseTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			v.FillIndexWithRange((1, 3));
			using var dv = v.ToDense();
			var hostA = MathC.DenseMatrix.OfColumnMajor(10, 10, A.ToFortranOrderArray().ToMathNet());

			// Act
			A.SetDiag(0, v as VectorBase<FloatComplex>);
			hostA.SetDiagonal(dv.ToFortranOrderArray().ToMathNet());

			// Assert
			Assert.IsTrue(hostA.ToColumnMajorArray().ApproxEqual(A.ToFortranOrderArray()));
		}
		#endregion


		#region indexer tests
		[TestMethod()]
		public void SinglePositionIndexerTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();

			var hostA = A.ToFortranOrderArray();

			// Act
			A[^2, ^2] = A[^2, ^2];

			// Assert
			Assert.IsTrue(hostA.SequenceEqual(A.ToFortranOrderArray()));
		}


		[TestMethod()]
		public void MultiplePositionIndexerTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();

			var hostA = A.ToFortranOrderArray();

			// Act
			A[(^2, ^2), (5, 5)] = A[(^2, ^2), (5, 5)];

			// Assert
			Assert.IsTrue(hostA.SequenceEqual(A.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void RangeIndexerTest()
		{
			RT.Reset(); // Arrange
			using var A = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			A.FillWithRandoms();

			var hostA = A.ToFortranOrderArray();

			// Act
			var sub = A[5..^2, 3..^2] as DenseMatrix<FloatComplex>;
			using var s = sub.Clone() as DenseMatrix<FloatComplex>;
			A[5..^2, 3..^2] = s;

			// Assert
			Assert.AreEqual(A.LeadDim, sub.LeadDim);
			Assert.IsTrue(hostA.SequenceEqual(A.ToFortranOrderArray()));
		}
		#endregion


		#region host converter tests
		[TestMethod()]
		public void From2DArrayTest()
		{
			RT.Reset(); // Arrange
			var T = new double[,] { { 1, 2, 3 }, { 4, 5, 6 } };
			// Act
			using var M = (DenseMatrix<double>)(T, false);
			// Assert
			Assert.AreEqual(T.GetLength(0), M.NRows);
			Assert.AreEqual(T.GetLength(1), M.NCols);
			Assert.IsTrue(T.ColumnTake().SequenceEqual(M.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void From1DArrayTest()
		{
			RT.Reset(); // Arrange
			var T = new double[] { 1, 2, 3, 4, 5, 6 };
			// Act
			using var M = (DenseMatrix<double>)(value: T, leadDim: 3, onHost: false);
			// Assert
			Assert.AreEqual(3, M.NRows);
			Assert.AreEqual(T.Length / 3, M.NCols);
			Assert.IsTrue(T.SequenceEqual(M.ToFortranOrderArray()));
		}
		#endregion


		[TestMethod()]
		public void PrintTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10, herm: false);
			m.FillWithRandoms();

			// Act
			Console.WriteLine(m.Print());

			m.CopyUpperToLower();
			Console.WriteLine(m.Print());

			// cannot be tested here
		}
	}
}