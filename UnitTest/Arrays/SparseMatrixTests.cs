using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Numerics;

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
	public class SparseMatrixTests
	{
		#region create tests
		[TestMethod()]
		public void SparseMatrixBaseCreateTest()
		{
			RT.Reset(); // Arrange
			// Act
			using var m = new SparseMatrix<double>(rows: 10, cols: 2, nonZeros: 5, format: SparseMatrixFormat.CSR, herm: false);
			// Assert
			Assert.AreEqual(10, m.NRows);
			Assert.AreEqual(2, m.NCols);
			Assert.AreEqual(5, m.NonZero);
			Assert.AreEqual(SparseMatrixFormat.CSR, m.Format);
			Assert.IsFalse(m.Hermitian);
		}

		[TestMethod()]
		public void SparseMatrixCopyCreateTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 2, nonZeros: 5, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			// Act
			using var mm = new SparseMatrix<double>(m, copyIndex: true);
			// Assert
			Assert.AreNotEqual(m, mm);
			Assert.IsTrue(m.ValueToFortranOrderArray().SequenceEqual(mm.ValueToFortranOrderArray()));
			Assert.IsTrue(m.IndexToIntArray().First().SequenceEqual(mm.IndexToIntArray().First()));
			Assert.IsTrue(m.IndexToIntArray().Last().SequenceEqual(mm.IndexToIntArray().Last()));
		}

		[TestMethod()]
		public void SparseMatrixFullCreateTest()
		{
			RT.Reset(); // Arrange
			using var val = Storage<double>.Create(length: 10);
			Rng.API.FillWithRandom(new DenseVector<double>(val, 10));
			using var ind1 = Storage<int>.Create(length: 10);
			Rng.API.FillWithRandom(new DenseVector<int>(ind1, 10));
			using var ind2 = Storage<int>.Create(length: 10);
			Rng.API.FillWithRandom(new DenseVector<int>(ind2, 10));

			// Act
			using var m = new SparseMatrix<double>(10, 10, value: val, rowPtr: ind1, colPtr: ind2, format: SparseMatrixFormat.COOC, herm: false);

			// Assert
			Assert.AreEqual(10, m.ActualLength);
			Assert.IsTrue(RT.CopyOutArray(val, 10).SequenceEqual(m.ValueToFortranOrderArray()));
			Assert.IsTrue(RT.CopyOutArray(ind1, 10).SequenceEqual(m.IndexToIntArray().First()));
			Assert.IsTrue(RT.CopyOutArray(ind2, 10).SequenceEqual(m.IndexToIntArray().Last()));
		}

		[TestMethod()]
		public void SparseMatrixRefCreateTest()
		{
			RT.Reset(); // Arrange
			using var m = new DenseMatrix<double>(rows: 10, cols: 2, herm: false);
			m.FillWithRandoms();
			// Act
			using var refm = new SparseMatrix<double>(refArray: m, rows: 5, cols: 10, format: SparseMatrixFormat.COOC);
			// Assert
			Assert.IsTrue(m.ToFortranOrderArray().SequenceEqual(refm.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void SparseMatrixRefCreateTest2()
		{
			RT.Reset(); // Arrange
			using var val = Storage<double>.Create(length: 10);
			Rng.API.FillWithRandom(new DenseVector<double>(val, 10));
			using var ind1 = Storage<int>.Create(length: 10);
			Rng.API.FillWithRandom(new DenseVector<int>(ind1, 10));
			using var ind2 = Storage<int>.Create(length: 10);
			Rng.API.FillWithRandom(new DenseVector<int>(ind2, 10));
			using var m = new SparseMatrix<double>(10, 10, value: val, rowPtr: ind1, colPtr: ind2, format: SparseMatrixFormat.COOC, herm: false);
			// Act
			using var refm = new SparseMatrix<double>(refArray: m, rows: 20, cols: 10, rowPtr: ind1, colPtr: ind2, format: SparseMatrixFormat.COOC, refRow: true, refCol: true, offsetRef: 2);
			// Assert
			Assert.IsTrue(RT.CopyOutArray(val, 8, 2).SequenceEqual(refm.ValueToFortranOrderArray()));
			Assert.IsTrue(RT.CopyOutArray(ind1, 8, 2).SequenceEqual(refm.IndexToIntArray().First()));
			Assert.IsTrue(RT.CopyOutArray(ind2, 8, 2).SequenceEqual(refm.IndexToIntArray().Last()));
		}
		#endregion


		#region reshape tests
		[TestMethod()]
		public void ToMatrixTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 4, cols: 16, nonZeros: 5, format: SparseMatrixFormat.COOC, herm: false);
			// Act
			var mm = m.ToMatrix();
			// Assert
			Assert.AreSame(m, mm);
		}
		#endregion


		#region to from C# array tests
		[TestMethod()]
		public void ToFromIntArrayTest()
		{
			RT.Reset(); // Arrange
			var hostv = new double[] { 5, 8, 36, 1, 7 };
			var hostrowInd = new int[] { 1, 6, 4, 3, 8 };
			var hostcolInd = new int[] { 2, 1, 6, 8, 0 };
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 6, format: SparseMatrixFormat.COOC, herm: false);
			// Act
			m.ValueFromFortranOrderArray(hostv, 1..);
			m.IndexFromIntArray(new[] { hostrowInd, hostcolInd }, 1.., 1..);
			// Assert
			Assert.IsTrue(hostv.SequenceEqual(m.ValueToFortranOrderArray(1..)));
			Assert.IsTrue(hostrowInd.SequenceEqual(m.IndexToIntArray(1.., 1..).First()));
			Assert.IsTrue(hostcolInd.SequenceEqual(m.IndexToIntArray(1.., 1..).Last()));
		}

		[TestMethod()]
		public void ToFromLongArrayTest()
		{
			RT.Reset(); // Arrange
			var hostv = new double[] { 5, 8, 36, 1, 7 };
			var hostrowInd = new long[] { 1, 6, 4, 3, 8 };
			var hostcolInd = new long[] { 2, 1, 6, 8, 0 };
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 6, format: SparseMatrixFormat.COOC, herm: false);
			// Act
			m.ValueFromFortranOrderArray(hostv, 1..);
			m.IndexFromLongArray(new[] { hostrowInd, hostcolInd }, 1.., 1..);
			// Assert
			Assert.IsTrue(hostv.SequenceEqual(m.ValueToFortranOrderArray(1..)));
			Assert.IsTrue(hostrowInd.SequenceEqual(m.IndexToLongArray(1.., 1..).First()));
			Assert.IsTrue(hostcolInd.SequenceEqual(m.IndexToLongArray(1.., 1..).Last()));
		}
		#endregion


		#region to dense tests
		public static void ToDenseTest(SparseMatrixFormat format, SparseMatrixToDenseAlgorithm algorithm)
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: format, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathD.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());
			// Act
			using var dm = m.ToDense(algorithm);
			// Assert
			Assert.IsTrue(hostm.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void CSRToDenseDefaultTest()
		{
			ToDenseTest(SparseMatrixFormat.CSR, SparseMatrixToDenseAlgorithm.Default);
		}

		[TestMethod()]
		public void CSCToDenseDefaultTest()
		{
			ToDenseTest(SparseMatrixFormat.CSC, SparseMatrixToDenseAlgorithm.Default);
		}

		[TestMethod()]
		public void COOCToDenseDefaultTest()
		{
			ToDenseTest(SparseMatrixFormat.COOC, SparseMatrixToDenseAlgorithm.Default);
		}

		[TestMethod()]
		public void COORToDenseDefaultTest()
		{
			ToDenseTest(SparseMatrixFormat.COOR, SparseMatrixToDenseAlgorithm.Default);
		}

		[TestMethod()]
		public void COOCToDenseViaVectorTest()
		{
			ToDenseTest(SparseMatrixFormat.COOC, SparseMatrixToDenseAlgorithm.ViaVector);
		}
		#endregion


		#region to sparse tests
		[TestMethod()]
		public void ToSparseTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);

			// Act
			var mm = m.ToSparse();
			// Assert
			Assert.AreSame(m, mm);
		}
		#endregion


		#region other tests
		[TestMethod()]
		public void PruneCSRTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var pm = m.Prune(threshold: 0.5f);

			// Assert
			var real = m.ValueToFortranOrderArray().Where(a => a > 0.5f).ToArray();
			Assert.IsTrue(real.ApproxEqual(pm.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void PruneCOOTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var pm = m.Prune(threshold: 0.5f);

			// Assert
			var real = m.ValueToFortranOrderArray().Where(a => a > 0.5f).ToArray();
			Assert.IsTrue(real.ApproxEqual(pm.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void NewArrayAlikeTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			// Act
			using var mm = m.NewArrayAlike() as SparseMatrix<double>;
			// Assert
			Assert.AreNotEqual(m, mm);
			Assert.AreEqual(m.NRows, mm.NRows);
			Assert.AreEqual(m.NCols, mm.NCols);
			Assert.AreEqual(m.NonZero, mm.NonZero);
			Assert.AreEqual(m.Format, mm.Format);
		}

		[TestMethod()]
		public void AsDenseVectorTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			// Act
			var v = m.AsDenseVector();
			// Assert
			Assert.AreEqual(m.NonZero, v.Length);
			Assert.IsTrue(m.ValueToFortranOrderArray().SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void DataTypeCastSameTypeTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);

			// act
			var newm = m.DataTypeCast<double>();

			// assert
			Assert.AreSame(m, newm);
		}

		[TestMethod()]
		public void DataTypeCastDifferentTypeTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<float>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();

			// act
			var newm = m.DataTypeCast<FloatComplex>() as SparseMatrix<FloatComplex>;

			// assert
			var host = m.ValueToFortranOrderArray();
			var hostnew = newm.ValueToFortranOrderArray().Select(v => v.Real());
			Assert.IsTrue(host.SequenceEqual(hostnew));
		}

		[TestMethod()]
		public void CopyUpperToLowerTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<float>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: true);
			// Act
			m.CopyUpperToLower();
			// Assert
			// cannot test here
		}
		#endregion


		#region range test

		#region get range and sub mat
		public static void GetRangeTest(Range rowcol, bool? isrow, SparseMatrixFormat format, bool toMat, bool toDense)
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: format, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var len = rowcol.GetOffsetAndCount(m.NCols).Length;
			SparseMatrix<double> mm = null;
			try
			{
				if (isrow.HasValue && !isrow.Value)
				{
					if (toMat)
					{
						if (toDense)
						{
							using var temp = new DenseMatrix<double>(rows: 10, cols: len, herm: false);
							// Act
							m.GetColumnRange(rowcol, temp as MatrixBase<double>);
							mm = temp.ToSparse(targetFormat: format);
						}
						else
						{
							mm = new SparseMatrix<double>(rows: 10, cols: len, nonZeros: len, format: format, herm: false);
							// Act
							m.GetColumnRange(rowcol, mm as MatrixBase<double>);
						}
					}
					else
					{
						// Act
						mm = m.GetColumnRange(rowcol, null as MatrixBase<double>) as SparseMatrix<double>;
					}
				}
				else if (isrow.HasValue && isrow.Value)
				{
					if (toMat)
					{
						if (toDense)
						{
							using var temp = new DenseMatrix<double>(rows: len, cols: 10, herm: false);
							// Act
							m.GetRowRange(rowcol, temp as MatrixBase<double>);
							mm = temp.ToSparse(targetFormat: format);
						}
						else
						{
							mm = new SparseMatrix<double>(rows: len, cols: 10, nonZeros: len, format: format, herm: false);
							// Act
							m.GetRowRange(rowcol, mm as MatrixBase<double>);
						}
					}
					else
					{
						// Act
						mm = m.GetRowRange(rowcol, null as MatrixBase<double>) as SparseMatrix<double>;
					}
				}
				else
				{
					if (toMat)
					{
						if (toDense)
						{
							using var temp = new DenseMatrix<double>(rows: len, cols: len, herm: false);
							// Act
							m.GetSubmatrix(rowcol, rowcol, temp as MatrixBase<double>);
							mm = temp.ToSparse(targetFormat: format);
						}
						else
						{
							var newFormat = format;
							if ((format & SparseMatrixFormat.Coordinated) != 0)
								newFormat = SparseMatrixFormat.Coordinated ^ format;	// bitwise XOR
							else if ((format & SparseMatrixFormat.Compressed) != 0)
								newFormat = SparseMatrixFormat.Compressed ^ format;		// bitwise XOR
							mm = new SparseMatrix<double>(rows: len, cols: len, nonZeros: len, format: newFormat, herm: false);
							// Act
							m.GetSubmatrix(rowcol, rowcol, mm as MatrixBase<double>);
							mm = mm.ToFormat(format, disposeThis: true);
						}
					}
					else
					{
						// Act
						mm = m.GetSubmatrix(rowcol, rowcol, null as MatrixBase<double>) as SparseMatrix<double>;
					}
				}
				// Assert
				Assert.IsTrue(m.ValueToFortranOrderArray(rowcol).SequenceEqual(mm.ValueToFortranOrderArray()));
				if (isrow.HasValue && !isrow.Value)
					Assert.IsTrue(m.IndexToIntArray(rowcol, rowcol).First().SequenceEqual(mm.IndexToIntArray().First()));
				else if (isrow.HasValue && isrow.Value)
					Assert.IsTrue(m.IndexToIntArray(rowcol, rowcol).Last().SequenceEqual(mm.IndexToIntArray().Last()));
			}
			finally
			{
				mm?.Dispose();
			}
		}


		[TestMethod()]
		public void GetColumnRangeCSCTest()
		{
			GetRangeTest(1..^1, false, SparseMatrixFormat.CSC, false, false);
		}

		[TestMethod()]
		public void GetColumnRangeCOOCTest()
		{
			GetRangeTest(1..^1, false, SparseMatrixFormat.COOC, false, false);
		}

		[TestMethod()]
		public void GetColumnRangeCSCToSparseMatTest()
		{
			GetRangeTest(1..^1, false, SparseMatrixFormat.CSC, true, false);
		}

		[TestMethod()]
		public void GetColumnRangeCOOCToSparseMatTest()
		{
			GetRangeTest(1..^1, false, SparseMatrixFormat.COOC, true, false);
		}

		[TestMethod()]
		public void GetColumnRangeCSCToDenseMatTest()
		{
			GetRangeTest(1..^1, false, SparseMatrixFormat.CSC, true, true);
		}

		[TestMethod()]
		public void GetRowRangeCSRTest()
		{
			GetRangeTest(1..^1, true, SparseMatrixFormat.CSR, false, false);
		}

		[TestMethod()]
		public void GetRowRangeCOORTest()
		{
			GetRangeTest(1..^1, true, SparseMatrixFormat.COOR, false, false);
		}

		[TestMethod()]
		public void GetRowRangeCSRToSparseMatTest()
		{
			GetRangeTest(1..^1, true, SparseMatrixFormat.CSR, true, false);
		}

		[TestMethod()]
		public void GetRowRangeCOORToSparseMatTest()
		{
			GetRangeTest(1..^1, true, SparseMatrixFormat.COOR, true, false);
		}

		[TestMethod()]
		public void GetRowRangeCSRToDenseMatTest()
		{
			GetRangeTest(1..^1, true, SparseMatrixFormat.CSR, true, true);
		}

		[TestMethod()]
		public void GetSubmatCSRTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.CSR, false, false);
		}

		[TestMethod()]
		public void GetSubmatCOORTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.COOR, false, false);
		}

		[TestMethod()]
		public void GetSubmatCSRToSparseMatTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.CSR, true, false);
		}

		[TestMethod()]
		public void GetSubmatCOORToSparseMatTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.COOR, true, false);
		}

		[TestMethod()]
		public void GetSubmatCSRToDenseMatTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.CSR, true, true);
		}

		[TestMethod()]
		public void GetSubmatCSCTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.CSC, false, false);
		}

		[TestMethod()]
		public void GetSubmatCOOCTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.COOC, false, false);
		}

		[TestMethod()]
		public void GetSubmatCSCToSparseMatTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.CSC, true, false);
		}

		[TestMethod()]
		public void GetSubmatCOOCToSparseMatTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.COOC, true, false);
		}

		[TestMethod()]
		public void GetSubmatCSCToDenseMatTest()
		{
			GetRangeTest(1..^1, null, SparseMatrixFormat.CSC, true, true);
		}
		#endregion

		#region get rows columns
		public static void GetRowsColumnsTest(Range rowcol, bool isrow, SparseMatrixFormat format, bool toVecs, bool toDense)
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: format, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var len = rowcol.GetOffsetAndCount(m.NCols).Length;
			var start = rowcol.GetOffsetAndCount(m.NCols).Offset;
			SparseVector<double>[] mm = null;
			try
			{
				if (!isrow)
				{
					if (toVecs)
					{
						if (toDense)
						{
							var temp = new DenseVector<double>[len];
							temp = temp.Select(a => new DenseVector<double>(length: 10)).ToArray();
							try
							{
								// Act
								m.GetColumns(rowcol, temp as VectorBase<double>[]);
								mm = temp.Select(a => a.ToSparse()).ToArray();
							}
							finally
							{
								temp.ForEach((a, i) => a.Dispose());
							}
						}
						else
						{
							mm = new SparseVector<double>[len];
							mm = mm.Select(a => new SparseVector<double>(length: 10, nonZeros: 1)).ToArray();
							// Act
							m.GetColumns(rowcol, mm as VectorBase<double>[]);
						}
					}
					else
					{
						// Act
						mm = m.GetColumns(rowcol, null as VectorBase<double>[]) as SparseVector<double>[];
					}
				}
				else
				{
					if (toVecs)
					{
						if (toDense)
						{
							var temp = new DenseVector<double>[len];
							temp = temp.Select(a => new DenseVector<double>(length: 10)).ToArray();
							try
							{
								// Act
								m.GetRows(rowcol, temp as VectorBase<double>[]);
								mm = temp.Select(a => a.ToSparse()).ToArray();
							}
							finally
							{
								temp.ForEach((a, i) => a.Dispose());
							}
						}
						else
						{
							mm = new SparseVector<double>[len];
							mm = mm.Select(a => new SparseVector<double>(length: 10, nonZeros: 1)).ToArray();
							// Act
							m.GetRows(rowcol, mm as VectorBase<double>[]);
						}
					}
					else
					{
						// Act
						mm = m.GetRows(rowcol, null as VectorBase<double>[]) as SparseVector<double>[];
					}
				}
				// Assert
				var real = m.ValueToFortranOrderArray();
				var test = mm.Select(a => a.ValueToFortranOrderArray()[0]).ToArray();
				for (long i = start, j = 0; i < len + start; i++, j++)
				{
					Assert.AreEqual(real[i], test[j]);
				}
			}
			finally
			{
				mm?.ForEach((a, i) => a.Dispose());
			}
		}

		[TestMethod()]
		public void GetColumnsTest()
		{
			GetRowsColumnsTest(1..^1, false, SparseMatrixFormat.CSC, false, false);
		}

		[TestMethod()]
		public void GetColumnsToVecTest()
		{
			GetRowsColumnsTest(1..^1, false, SparseMatrixFormat.CSC, true, false);
		}

		[TestMethod()]
		public void GetColumnsToVecDenseTest()
		{
			GetRowsColumnsTest(1..^1, false, SparseMatrixFormat.CSC, true, true);
		}

		[TestMethod()]
		public void GetRowsTest()
		{
			GetRowsColumnsTest(1..^1, true, SparseMatrixFormat.CSR, false, false);
		}

		[TestMethod()]
		public void GetRowsToVecTest()
		{
			GetRowsColumnsTest(1..^1, true, SparseMatrixFormat.CSR, true, false);
		}

		[TestMethod()]
		public void GetRowsToVecDenseTest()
		{
			GetRowsColumnsTest(1..^1, true, SparseMatrixFormat.CSR, true, true);
		}
		#endregion

		#region get row column at
		public static void GetRowColumnAtTest(Index rowcol, bool isrow, SparseMatrixFormat format, bool toVec, bool toDense)
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: format, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var pos = rowcol.GetPosition(m.NCols);
			SparseVector<double> mm = null;
			try
			{
				if (!isrow)
				{
					if (toVec)
					{
						if (toDense)
						{
							using var temp = new DenseVector<double>(10);
							// Act
							m.GetColumnAt(rowcol, temp as VectorBase<double>);
							mm = temp.ToSparse();
						}
						else
						{
							mm = new SparseVector<double>(length: 10, nonZeros: 1);
							// Act
							m.GetColumnAt(rowcol, mm as VectorBase<double>);
						}
					}
					else
					{
						// Act
						mm = m.GetColumnAt(rowcol, null as VectorBase<double>) as SparseVector<double>;
					}
				}
				else
				{
					if (toVec)
					{
						if (toDense)
						{
							using var temp = new DenseVector<double>(10);
							// Act
							m.GetRowAt(rowcol, temp as VectorBase<double>);
							mm = temp.ToSparse();
						}
						else
						{
							mm = new SparseVector<double>(length: 10, nonZeros: 1);
							// Act
							m.GetRowAt(rowcol, mm as VectorBase<double>);
						}
					}
					else
					{
						// Act
						mm = m.GetRowAt(rowcol, null as VectorBase<double>) as SparseVector<double>;
					}
				}
				// Assert
				var real = m.ValueToFortranOrderArray()[pos];
				var test = mm.ValueToFortranOrderArray()[0];
				Assert.AreEqual(real, test);
			}
			finally
			{
				mm?.Dispose();
			}
		}

		[TestMethod()]
		public void GetColumnAtCSCTest()
		{
			GetRowColumnAtTest(^4, false, SparseMatrixFormat.CSC, false, false);
		}

		[TestMethod()]
		public void GetColumnAtCOOCTest()
		{
			GetRowColumnAtTest(^4, false, SparseMatrixFormat.COOC, false, false);
		}

		[TestMethod()]
		public void GetColumnAtToVecCSCTest()
		{
			GetRowColumnAtTest(^4, false, SparseMatrixFormat.CSC, true, false);
		}

		[TestMethod()]
		public void GetColumnAtToVecCOOCTest()
		{
			GetRowColumnAtTest(^4, false, SparseMatrixFormat.COOC, true, false);
		}

		[TestMethod()]
		public void GetColumnAtToDenseVecCSCTest()
		{
			GetRowColumnAtTest(^4, false, SparseMatrixFormat.CSC, true, true);
		}

		[TestMethod()]
		public void GetColumnAtToVecDenseCOOCTest()
		{
			GetRowColumnAtTest(^4, false, SparseMatrixFormat.COOC, true, true);
		}

		[TestMethod()]
		public void GetRowAtCSRTest()
		{
			GetRowColumnAtTest(^4, true, SparseMatrixFormat.CSR, false, false);
		}

		[TestMethod()]
		public void GetRowAtCOORTest()
		{
			GetRowColumnAtTest(^4, true, SparseMatrixFormat.COOR, false, false);
		}

		[TestMethod()]
		public void GetRowAtToVecCSRTest()
		{
			GetRowColumnAtTest(^4, true, SparseMatrixFormat.CSR, true, false);
		}

		[TestMethod()]
		public void GetRowAtToVecCOORTest()
		{
			GetRowColumnAtTest(^4, true, SparseMatrixFormat.COOR, true, false);
		}

		[TestMethod()]
		public void GetRowAtToDenseVecCSRTest()
		{
			GetRowColumnAtTest(^4, true, SparseMatrixFormat.CSR, true, true);
		}

		[TestMethod()]
		public void GetRowAtToVecDenseCOORTest()
		{
			GetRowColumnAtTest(^4, true, SparseMatrixFormat.COOR, true, true);
		}
		#endregion

		#endregion


		#region clone test
		[TestMethod()]
		public void CloneTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var mm = m.Clone() as SparseMatrix<double>;

			// Assert
			Assert.AreNotEqual(m, mm);
			Assert.AreEqual(m.NRows, mm.NRows);
			Assert.AreEqual(m.NCols, mm.NCols);
			Assert.AreEqual(m.NonZero, mm.NonZero);
			Assert.AreEqual(m.Format, mm.Format);
			Assert.IsTrue(m.ValueToFortranOrderArray().SequenceEqual(mm.ValueToFortranOrderArray()));
			Assert.IsTrue(m.IndexToIntArray().First().SequenceEqual(mm.IndexToIntArray().First()));
			Assert.IsTrue(m.IndexToIntArray().Last().SequenceEqual(mm.IndexToIntArray().Last()));
		}
		#endregion


		#region format conversion tests
		[TestMethod()]
		public void COOToCompressedTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<double>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var mm = m.ToFormat(SparseMatrixFormat.Compressed);

			// Assert
			Assert.IsTrue(m.ValueToFortranOrderArray().SequenceEqual(mm.ValueToFortranOrderArray()));
			Assert.IsTrue(m.IndexToIntArray().First().SequenceEqual(mm.IndexToIntArray().First()));
		}
		#endregion



		#region conjugate tests
		[TestMethod()]
		public void ConjugateOutofPlaceTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			// Act
			using var mm = m.ConjugateOutOfPlace() as SparseMatrix<FloatComplex>;
			// Assert
			Assert.IsTrue(m.ValueToFortranOrderArray().Select(a => a.Conjugate()).ToArray().ApproxEqual(mm.ValueToFortranOrderArray()));
		}
		#endregion

		#region transpose tests
		[TestMethod()]
		public void TransposeTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var mm = m.Transpose(overwrite: null as MatrixBase<FloatComplex>) as SparseMatrix<FloatComplex>;

			// Assert
			Assert.AreEqual(SparseMatrixFormat.CSC, mm.Format);
			Assert.IsTrue(m.ValueToFortranOrderArray().ApproxEqual(mm.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void TransposeToMatTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			using var mm = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC, herm: false);

			// Act
			m.Transpose(mm as MatrixBase<FloatComplex>);

			// Assert
			Assert.IsTrue(m.ValueToFortranOrderArray().ApproxEqual(mm.ValueToFortranOrderArray()));
		}

		public static void TransposeToFormatTest(SparseMatrixFormat from, SparseMatrixFormat to)
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: from, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			using var mm = m.Transpose(target: to);

			// Assert
			Assert.AreEqual(to, mm.Format);
			Assert.IsTrue(m.ValueToFortranOrderArray().ApproxEqual(mm.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void TransposeCSRToCSRTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSR, SparseMatrixFormat.CSR);
		}
		[TestMethod()]
		public void TransposeCSRToCSCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSR, SparseMatrixFormat.CSC);
		}
		[TestMethod()]
		public void TransposeCSRToCOORTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSR, SparseMatrixFormat.COOR);
		}
		[TestMethod()]
		public void TransposeCSRToCOOCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSR, SparseMatrixFormat.COOC);
		}
		[TestMethod()]
		public void TransposeCSCToCSRTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSC, SparseMatrixFormat.CSR);
		}
		[TestMethod()]
		public void TransposeCSCToCSCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSC, SparseMatrixFormat.CSC);
		}
		[TestMethod()]
		public void TransposeCSCToCOORTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSC, SparseMatrixFormat.COOR);
		}
		[TestMethod()]
		public void TransposeCSCToCOOCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.CSC, SparseMatrixFormat.COOC);
		}
		[TestMethod()]
		public void TransposeCOORToCSRTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOR, SparseMatrixFormat.CSR);
		}
		[TestMethod()]
		public void TransposeCOORToCSCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOR, SparseMatrixFormat.CSC);
		}
		[TestMethod()]
		public void TransposeCOORToCOORTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOR, SparseMatrixFormat.COOR);
		}
		[TestMethod()]
		public void TransposeCOORToCOOCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOR, SparseMatrixFormat.COOR);
		}
		[TestMethod()]
		public void TransposeCOOCToCSRTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOC, SparseMatrixFormat.CSR);
		}
		[TestMethod()]
		public void TransposeCOOCToCSCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOC, SparseMatrixFormat.CSC);
		}
		[TestMethod()]
		public void TransposeCOOCToCOORTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOC, SparseMatrixFormat.COOR);
		}
		[TestMethod()]
		public void TransposeCOOCToCOOCTest()
		{
			TransposeToFormatTest(SparseMatrixFormat.COOC, SparseMatrixFormat.COOC);
		}
		#endregion

		#region conjugate transpose tests
		[TestMethod()]
		public void ConjugateTransposeTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());

			// Act
			using var mm = m.ConjugateTranspose(null as MatrixBase<FloatComplex>) as SparseMatrix<FloatComplex>;
			using var dm = mm.ToDense();
			var real = hostm.ConjugateTranspose();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ConjugateTransposeToMatTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());
			using var mm = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSC, herm: false);

			// Act
			m.ConjugateTranspose(mm as MatrixBase<FloatComplex>);
			using var dm = mm.ToDense();
			var real = hostm.ConjugateTranspose();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
		}
		#endregion


		#region from matrix add tests
		public static void FromDenseAddDenseTest(MatrixOperation op1, MatrixOperation op2)
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
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
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
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
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format1);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format2);
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
			m.From_αA_Add_βB(a as MatrixBase<FloatComplex>, b, 1, 1, op1, op2);
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format1);
			b.FillWithRandoms();
			b.FillIndexWithRange((0, 1), (0, 1));
			using var db = b.ToDense();
			MathNet.Numerics.LinearAlgebra.Matrix<Complex32> hostb = MathC.DenseMatrix.OfColumnMajor(10, 10, db.ToFortranOrderArray().ToMathNet());
			hostb = op2 == MatrixOperation.None ? hostb : op2 == MatrixOperation.Transpose ? hostb.Transpose() : hostb.ConjugateTranspose();

			// Act
			m.From_αA_Add_βB(null as MatrixBase<FloatComplex>, b, 0, 1, default, op2);
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());
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
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format1);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 14, format2);
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
			using var dm = m.ToDense();
			var real = hosta.Multiply(hostb).Add(hostm);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			using var a = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format1);
			using var b = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 14, format2);
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
			using var dm = m.ToDense();
			var real = hosta.Multiply(hostb);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());
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
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
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
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());
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
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
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

		#region Kronecker tests
		[TestMethod()]
		public void KroneckerProdTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			using var n = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOR, herm: false);
			n.FillWithRandoms();
			n.FillIndexWithRange((0, 1), (0, 1));

			using var dm = m.ToDense();
			using var dn = n.ToDense();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, dm.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, dn.ToFortranOrderArray().ToMathNet());

			// Act
			using var mm = m.KroneckerProd(n as MatrixBase<FloatComplex>, forceHerm: false) as SparseMatrix<FloatComplex>;
			using var dmm = mm.ToDense();
			var real = hostm.KroneckerProduct(hostn);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dmm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void KroneckerSumTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			using var n = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOR, herm: false);
			n.FillWithRandoms();
			n.FillIndexWithRange((0, 1), (0, 1));

			using var dm = m.ToDense();
			using var dn = n.ToDense();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, dm.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, dn.ToFortranOrderArray().ToMathNet());
			var hostI = MathC.DenseMatrix.CreateIdentity(10);

			// Act
			using var mm = m.KroneckerSum(n as MatrixBase<FloatComplex>, forceHerm: false) as SparseMatrix<FloatComplex>;
			using var dmm = mm.ToDense();
			var real = hostm.KroneckerProduct(hostI) + hostI.KroneckerProduct(hostn);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dmm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void KroneckerProdForceHermTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			using var n = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOR, herm: false);
			n.FillWithRandoms();
			n.FillIndexWithRange((0, 1), (0, 1));

			using var dm = m.ToDense();
			using var dn = n.ToDense();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, dm.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, dn.ToFortranOrderArray().ToMathNet());

			// Act
			using var mm = m.KroneckerProd(n as MatrixBase<FloatComplex>) as SparseMatrix<FloatComplex>;
			using var dmm = mm.ToDense();
			var real = (hostm.KroneckerProduct(hostn.ConjugateTranspose()) + hostm.ConjugateTranspose().KroneckerProduct(hostn)) / 2;

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dmm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void KroneckerSumForceHermTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			using var n = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOR, herm: false);
			n.FillWithRandoms();
			n.FillIndexWithRange((0, 1), (0, 1));

			using var dm = m.ToDense();
			using var dn = n.ToDense();

			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, dm.ToFortranOrderArray().ToMathNet());
			var hostn = MathC.DenseMatrix.OfColumnMajor(10, 10, dn.ToFortranOrderArray().ToMathNet());
			var hostI = MathC.DenseMatrix.CreateIdentity(10);

			// Act
			using var mm = m.KroneckerSum(n as MatrixBase<FloatComplex>) as SparseMatrix<FloatComplex>;
			using var dmm = mm.ToDense();
			var real = ((hostm + hostm.ConjugateTranspose()) / 2).KroneckerProduct(hostI) + hostI.KroneckerProduct((hostn + hostn.ConjugateTranspose()) / 2);

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dmm.ToFortranOrderArray()));
		}
		#endregion


		#region diag tests
		[TestMethod()]
		public void GetDiagTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());

			// Act
			using var v = m.GetDiag(0, null as VectorBase<FloatComplex>) as SparseVector<FloatComplex>;
			using var dv = v.ToDense();
			var real = hostm.Diagonal();

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void SetDiagTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();


			// Act
			m.SetDiag(0, v as VectorBase<FloatComplex>);
			hostm.SetDiagonal(v.ToFortranOrderArray().ToMathNet());
			using var dm = m.ToDense();

			// Assert
			Assert.IsTrue(hostm.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
		}
		#endregion


		#region indexer tests
		public static void SinglePositionIndexerTest(SparseMatrixFormat format)
		{
			RT.Reset(); // Arrange
			using var A = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: format, herm: false);
			A.FillWithRandoms();
			A.FillIndexWithRange((0, 1), (0, 1));

			// Act
			A[^2, ^2] = A[^2, ^2];

			// Assert
			Assert.IsTrue(A.ValueToFortranOrderArray().SequenceEqual(A.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void SinglePositionIndexerCSRTest()
		{
			SinglePositionIndexerTest(SparseMatrixFormat.CSR);
		}
		[TestMethod()]
		public void SinglePositionIndexerCSCTest()
		{
			SinglePositionIndexerTest(SparseMatrixFormat.CSC);
		}
		[TestMethod()]
		public void SinglePositionIndexerCOOCTest()
		{
			SinglePositionIndexerTest(SparseMatrixFormat.COOC);
		}
		[TestMethod()]
		public void SinglePositionIndexerCOORTest()
		{
			SinglePositionIndexerTest(SparseMatrixFormat.COOR);
		}


		public static void MultiplePositionIndexerTest(SparseMatrixFormat format)
		{
			RT.Reset(); // Arrange
			using var A = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: format, herm: false);
			A.FillWithRandoms();
			A.FillIndexWithRange((0, 1), (0, 1));

			// Act
			A[(^2, ^2), (5, 5)] = A[(^2, ^2), (5, 5)];

			// Assert
			Assert.IsTrue(A.ValueToFortranOrderArray().SequenceEqual(A.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void MultiplePositionIndexerCSRTest()
		{
			MultiplePositionIndexerTest(SparseMatrixFormat.CSR);
		}
		[TestMethod()]
		public void MultiplePositionIndexerCSCTest()
		{
			MultiplePositionIndexerTest(SparseMatrixFormat.CSC);
		}
		[TestMethod()]
		public void MultiplePositionIndexerCOOCTest()
		{
			MultiplePositionIndexerTest(SparseMatrixFormat.COOC);
		}
		[TestMethod()]
		public void MultiplePositionIndexerCOORTest()
		{
			MultiplePositionIndexerTest(SparseMatrixFormat.COOR);
		}
		#endregion


		#region host converter tests
		[TestMethod()]
		public void FromArrayCOOTest()
		{
			RT.Reset(); // Arrange
			var val = new double[] { 1, 2, 3, 4, 5, 6 };
			var row = new int[] { 1, 2, 4, 10, 13, 16 };
			var col = new int[] { 15, 5, 14, 5, 12, 1 };
			// Act
			using var M = (SparseMatrix<double>)(value: val, row, col, row.Max(), col.Max(), format: SparseMatrixFormat.COOC, false);
			// Assert
			Assert.AreEqual(16, M.NRows);
			Assert.AreEqual(15, M.NCols);
			Assert.AreEqual(6, M.NonZero);
			Assert.IsTrue(val.SequenceEqual(M.ValueToFortranOrderArray()));
			Assert.IsTrue(row.SequenceEqual(M.IndexToIntArray().First()));
			Assert.IsTrue(col.SequenceEqual(M.IndexToIntArray().Last()));
		}

		[TestMethod()]
		public void FromArrayCSRTest()
		{
			RT.Reset(); // Arrange
			var val = new double[] { 1, 2, 3, 4, 5, 6, 7 };
			var row = new int[] { 0, 1, 2, 4, 5, 7, 8, 9 };
			var col = new int[] { 15, 5, 14, 5, 12, 1, 6 };
			// Act
			using var M = (SparseMatrix<double>)(value: val, row, col, row.Length - 1, col.Max(), format: SparseMatrixFormat.CSR, false);
			// Assert
			Assert.AreEqual(7, M.NRows);
			Assert.AreEqual(15, M.NCols);
			Assert.AreEqual(7, M.NonZero);
			Assert.IsTrue(val.SequenceEqual(M.ValueToFortranOrderArray()));
			Assert.IsTrue(row.SequenceEqual(M.IndexToIntArray().First()));
			Assert.IsTrue(col.SequenceEqual(M.IndexToIntArray().Last()));
		}

		[TestMethod()]
		public void FromArrayCSCTest()
		{
			RT.Reset(); // Arrange
			var val = new double[] { 1, 2, 3, 4, 5, 6, 7 };
			var col = new int[] { 0, 1, 2, 4, 5, 7, 8, 9 };
			var row = new int[] { 15, 5, 14, 5, 12, 1, 6 };
			// Act
			using var M = (SparseMatrix<double>)(value: val, row, col, row.Max(), col.Length - 1, format: SparseMatrixFormat.CSC, false);
			// Assert
			Assert.AreEqual(15, M.NRows);
			Assert.AreEqual(7, M.NCols);
			Assert.AreEqual(7, M.NonZero);
			Assert.IsTrue(val.SequenceEqual(M.ValueToFortranOrderArray()));
			Assert.IsTrue(row.SequenceEqual(M.IndexToIntArray().First()));
			Assert.IsTrue(col.SequenceEqual(M.IndexToIntArray().Last()));
		}

		[TestMethod()]
		public void From2DArrayTest()
		{
			RT.Reset(); // Arrange
			var T = new double[,] { { 1, 2, 3 }, { 4, 5, 6 } };
			// Act
			using var M = (SparseMatrix<double>)(value: T, threshold: 2.0, false);
			// Assert
			Assert.AreEqual(T.GetLength(0), M.NRows);
			Assert.AreEqual(T.GetLength(1), M.NCols);
			Assert.IsTrue(new double[] { 3, 4, 5, 6 }.SequenceEqual(M.ValueToFortranOrderArray()));
		}
		#endregion


		[TestMethod()]
		public void PrintTest()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.CSR, false, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			Console.WriteLine(m.Print());

			// cannot be tested here
		}

		[TestMethod()]
		public void PrintTest2()
		{
			RT.Reset(); // Arrange
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format: SparseMatrixFormat.COOC, herm: false);
			m.FillWithRandoms();
			m.FillIndexWithRange((0, 1), (0, 1));

			// Act
			Console.WriteLine(m.Print());

			// cannot be tested here
		}
	}
}