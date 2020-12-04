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
	public class SparseVectorTests
	{
		[TestMethod()]
		public void SparseVectorCreateFromAllocatedIndicesTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var host = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, host);

			// Act
			using var v = new SparseVector<double>(length: 10, ind);

			// Assert
			Assert.IsTrue(host.SequenceEqual(v.IndexToIntArray().First()));
		}

		[TestMethod()]
		public void SparseVectorCreateFromAllocatedIntIndicesTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var host = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, host);

			// Act
			using var v = new SparseVector<double>(length: 10, ind);

			// Assert
			Assert.IsTrue(host.SequenceEqual(v.IndexToIntArray().First()));
		}

		[TestMethod()]
		public void SparseVectorBaseCreateTest()
		{
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);
		}

		[TestMethod()]
		public void SparseVectorFullCreateTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);

			// Act
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(v.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(v.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void SparseVectorRefCreateTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			using var refv = new SparseVector<double>(v, newLength: 15, newNNZ: 4);

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(refv.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(refv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void SparseVectorRefCreateIntTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new DenseVector<double>(values: val, length: 10);

			// Act
			using var refv = new SparseVector<double>(v, newLength: 15, newNNZ: 4, ind);

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(refv.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(refv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void LastIndexTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var host = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, host);
			using var v = new SparseVector<double>(length: 10, ind);

			// Act
			var li = v.LastIndex;

			// Assert
			Assert.AreEqual(9, li);
		}

		[TestMethod()]
		public void ValueFromFortranOrderArrayTest()
		{
			RT.Reset(); // Arrange
			var hostval = new double[] { 2, 4, 6 };
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			v.FillWithZeros();
			v.ValueFromFortranOrderArray(hostval, 0..hostval.Length);

			// Assert
			Assert.IsTrue(hostval.Append(0).SequenceEqual(v.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void IndexFromIntArrayTest()
		{
			RT.Reset(); // Arrange
			var hostval = new int[] { 2, 4, 6 };
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			v.IndexFromIntArray(new[] { hostval }, 0..hostval.Length);

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(v.IndexToIntArray().First().Take(hostval.Length)));
		}

		[TestMethod()]
		public void IndexFromLongArrayTest()
		{
			RT.Reset(); // Arrange
			var hostval = new long[] { 2, 4, 6 };
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			v.IndexFromLongArray(new[] { hostval }, 0..hostval.Length);

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(v.IndexToLongArray().First().Take(hostval.Length)));
		}

		[TestMethod()]
		public void CloneTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			using var cv = v.Clone() as SparseVector<double>;

			// Assert
			Assert.AreNotEqual(v, cv);
			Assert.IsTrue(hostind.SequenceEqual(cv.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(cv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void CloneValueAloneTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			using var cv = v.Clone() as SparseVector<double>;

			// Assert
			Assert.AreNotEqual(v, cv);
			Assert.IsTrue(hostind.SequenceEqual(cv.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(cv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void CloneValueAndIndexTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			using var cv = v.Clone() as SparseVector<double>;

			// Assert
			Assert.AreNotEqual(v, cv);
			Assert.IsTrue(hostind.SequenceEqual(cv.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(cv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void ConjugateTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<FloatComplex>.Create(length: 4);
			var hostval = new FloatComplex[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<FloatComplex>(length: 10, val, ind);

			// Act
			using var cv = v.ConjugateOutOfPlace() as SparseVector<FloatComplex>;

			// Assert
			Assert.AreNotEqual(v, cv);
			Assert.IsTrue(hostind.SequenceEqual(cv.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(cv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void NewArrayAlikeTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			using var sv = v.NewArrayAlike() as SparseVector<double>;

			// Assert
			Assert.AreEqual(v.Length, sv.Length);
			Assert.AreEqual(v.NonZero, sv.NonZero);
		}

		[TestMethod()]
		public void ToMatrixTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			using var m = v.ToMatrix(leadDim: 2) as SparseMatrix<double>;

			var realrow = new[] { 0, 0, 1, 1 };
			var realcol = new[] { 1, 2, 2, 4 };

			// Assert
			Assert.IsTrue(realrow.SequenceEqual(m.IndexToIntArray().First()));
			Assert.IsTrue(realcol.SequenceEqual(m.IndexToIntArray().Last()));
			Assert.IsTrue(hostval.SequenceEqual(m.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void ToTensorTest()
		{
			// tenser is not supported now
		}

		[TestMethod()]
		public void FillWithZerosTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			v.FillWithZeros();

			// Assert
			Assert.IsTrue(v.ValueToFortranOrderArray().All(a => a == 0));
		}

		[TestMethod()]
		public void FillWithRandomsTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			v.FillWithRandoms();

			// Assert
			Assert.AreEqual(v.NonZero, v.ValueToFortranOrderArray().Distinct().Count());
		}

		[TestMethod()]
		public void FillIndexWithRangeTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			v.FillIndexWithRange((start: 1, step: 2));
			var real = ArrayLinq.Range(start: 1, count: 4, step: 2);

			// Assert
			Assert.IsTrue(real.SequenceEqual(v.IndexToIntArray().First()));
		}

		[TestMethod()]
		public void ToDenseTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			using var dv = v.ToDense();

			var real = new double[] { 0, 0, 2, 0, 4, 5, 0, 0, 0, 9 };

			// Assert
			Assert.IsTrue(real.SequenceEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ToSparseTest()
		{
			using var v = new SparseVector<double>(length: 10, nonZeros: 4);
			var vv = v.ToSparse();
			Assert.AreSame(v, vv);
		}

		[TestMethod()]
		public void AsDenseVectorTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			var dv = v.AsDenseVector();

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(dv.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void DataTypeCastTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			using var fv = v.DataTypeCast<float>() as SparseVector<float>;

			var real = hostval.Select(a => (float)a).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(fv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void ReplaceBySparseTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);
			using var cv = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			cv.ReplaceBy(v as VectorBase<double>);

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(cv.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(cv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void ReplaceByDenseTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<double>(length: 4);
			v.FillWithRandoms();
			using var cv = new SparseVector<double>(length: 10, nonZeros: 4);

			// Act
			cv.ReplaceBy(v as VectorBase<double>);

			// Assert
			Assert.IsTrue(v.ToFortranOrderArray().SequenceEqual(cv.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void ScaleTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			v.Scale(2);

			var real = hostval.Select(a => 2 * a).ToArray();

			// Assert
			Assert.IsTrue(real.ApproxEqual(v.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBySparseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());

			// Act
			v.AddBy_αx(w as VectorBase<FloatComplex>, 2);
			using var dr = v.ToDense();

			var real = hostv.Add(hostw.Multiply(2));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dr.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void DotSparseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());

			// Act
			var res = v.Dot(w as VectorBase<FloatComplex>, conjugateThis: true);

			var real = hostv.ConjugateDotProduct(hostw);

			// Assert
			Assert.IsTrue(real.ApproxSame(res));
		}

		[TestMethod()]
		public void DotDenseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());

			// Act
			var res = v.Dot(w as VectorBase<FloatComplex>, conjugateThis: true);

			var real = hostv.ConjugateDotProduct(hostw);

			// Assert
			Assert.IsTrue(real.ApproxSame(res));
		}

		[TestMethod()]
		public void PointWiseMultiplySparseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());

			// Act
			v.PointWiseMultiply(w as VectorBase<FloatComplex>);
			using var dr = v.ToDense();
			var real = hostv.PointwiseMultiply(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dr.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void PointWiseMultiplyDenseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());

			// Act
			v.PointWiseMultiply(w as VectorBase<FloatComplex>);
			using var dr = v.ToDense();
			var real = hostv.PointwiseMultiply(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dr.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void PointWiseDivisionDenseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());

			// Act
			v.PointWiseDivide(w as VectorBase<FloatComplex>);
			using var dr = v.ToDense();
			var real = hostv.PointwiseDivide(hostw);

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dr.ToFortranOrderArray()));
		}


		#region add by matrix multiply vector
		public static void AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat format, MatrixOperation op)
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));
			m.FillWithRandoms();
			m.FillIndexWithRange((start: 0, step: 1), (start: 0, step: 1));

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());

			// Act
			v.Mulβ_AddBy_αopAx(m as MatrixBase<FloatComplex>, w as VectorBase<FloatComplex>, 1, 1, op);
			using var dr = v.ToDense();
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dr.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBy_NonTransSparseCSRMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.CSR, MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_NonTransSparseCSCMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.CSC, MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_NonTransSparseCOOMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.COOC, MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_TransSparseCSRMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.CSR, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_TransSparseCSCMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.CSC, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_TransSparseCOOMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.COOC, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_ConjTransSparseCSRMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.CSR, MatrixOperation.ConjugateTranspose);
		}

		[TestMethod()]
		public void AddBy_ConjTransSparseCSCMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.CSC, MatrixOperation.ConjugateTranspose);
		}

		[TestMethod()]
		public void AddBy_ConjTransSparseCOOMatrix_Multiply_SparseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat.COOC, MatrixOperation.ConjugateTranspose);
		}

		public static void AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat format, MatrixOperation op)
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			m.FillWithRandoms();
			m.FillIndexWithRange((start: 0, step: 1), (start: 0, step: 1));

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());

			// Act
			using var res = v.Mulβ_AddBy_αopAx(m, w, 1, 1, op);
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(res.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBy_NonTransSparseCSRMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.CSR, MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_NonTransSparseCSCMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.CSC, MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_NonTransSparseCOOMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.COOC, MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_TransSparseCSRMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.CSR, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_TransSparseCSCMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.CSC, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_TransSparseCOOMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.COOC, MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_ConjTransSparseCSRMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.CSR, MatrixOperation.ConjugateTranspose);
		}

		[TestMethod()]
		public void AddBy_ConjTransSparseCSCMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.CSC, MatrixOperation.ConjugateTranspose);
		}

		[TestMethod()]
		public void AddBy_ConjTransSparseCOOMatrix_Multiply_DenseVector_Test()
		{
			AddBy_SparseMatrix_Multiply_DenseVector_Test(SparseMatrixFormat.COOC, MatrixOperation.ConjugateTranspose);
		}

		public static void AddBy_DenseMatrix_Multiply_SparseVector_Test(MatrixOperation op)
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));
			m.FillWithRandoms();

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			using var res = v.Mulβ_AddBy_αopAx(m, w, 1, 1, op);
			using var dr = res.ToDense();
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dr.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBy_NonTransDenseMatrix_Multiply_SparseVector_Test()
		{
			AddBy_DenseMatrix_Multiply_SparseVector_Test(MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_TransDenseMatrix_Multiply_SparseVector_Test()
		{
			AddBy_DenseMatrix_Multiply_SparseVector_Test(MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_ConjTransDenseMatrix_Multiply_SparseVector_Test()
		{
			AddBy_DenseMatrix_Multiply_SparseVector_Test(MatrixOperation.ConjugateTranspose);
		}

		public static void AddBy_DenseMatrix_Multiply_DenseVector_Test(MatrixOperation op)
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			m.FillWithRandoms();

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			using var res = v.Mulβ_AddBy_αopAx(m, w, 1, 1, op);
			using var dr = res.ToDense();
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(dr.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddBy_NonTransDenseMatrix_Multiply_DenseVector_Test()
		{
			AddBy_DenseMatrix_Multiply_DenseVector_Test(MatrixOperation.None);
		}

		[TestMethod()]
		public void AddBy_TransDenseMatrix_Multiply_DenseVector_Test()
		{
			AddBy_DenseMatrix_Multiply_DenseVector_Test(MatrixOperation.Transpose);
		}

		[TestMethod()]
		public void AddBy_ConjTransDenseMatrix_Multiply_DenseVector_Test()
		{
			AddBy_DenseMatrix_Multiply_DenseVector_Test(MatrixOperation.ConjugateTranspose);
		}
		#endregion


		[TestMethod()]
		public void OuterProductSparseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());

			// Act
			using var m = v.OuterProduct(w as VectorBase<FloatComplex>, conjugateOther: true);
			using var dm = m.ToDense();
			var real = hostv.OuterProduct(hostw.Conjugate());

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(dm.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void OuterProductDenseTest()
		{
			RT.Reset(); // Arrange
			using var v = new SparseVector<FloatComplex>(length: 10, nonZeros: 4);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			v.FillIndexWithRange((start: 1, step: 2));
			w.FillWithRandoms();

			var hostv = MathC.SparseVector.OfIndexedEnumerable(10, v.ToMathNetSparse());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());

			// Act
			using var m = v.OuterProduct(w as VectorBase<FloatComplex>, conjugateOther: true) as DenseMatrix<FloatComplex>;
			var real = hostv.OuterProduct(hostw.Conjugate());

			// Assert
			Assert.IsTrue(real.ToColumnMajorArray().ApproxEqual(m.ToFortranOrderArray()));
		}


		[TestMethod()]
		public void IndexerOneItemTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			v[^1] = v[^1];

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(v.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(v.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void IndexerMultiItemTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			v[^1, 2] = v[^1, 2];

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(v.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(v.ValueToFortranOrderArray()));
		}


		[TestMethod()]
		public void IndexerRangeItemTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			v[4..8] = v[4..8];

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(v.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(v.ValueToFortranOrderArray()));
		}


		[TestMethod()]
		public void IndexerMultiRangeItemTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);

			// Act
			v[2..3, 4..6] = v[2..3, 4..6];

			// Assert
			Assert.IsTrue(hostind.SequenceEqual(v.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(v.ValueToFortranOrderArray()));
		}


		[TestMethod()]
		public void GetHashCodeTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);

			// Act
			using var v = new SparseVector<double>(length: 10, val, ind);
			using var w = new SparseVector<double>(length: 10, val, ind);

			// Assert
			Assert.AreEqual(v.GetHashCode(), w.GetHashCode());
		}

		[TestMethod()]
		public void EqualsTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);

			// Act
			using var v = new SparseVector<double>(length: 10, val, ind);
			using var w = new SparseVector<double>(length: 10, val, ind);

			// Assert
			Assert.AreEqual(v, w);
		}

		[TestMethod()]
		public void EqualsTest2()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var val2 = Storage<double>.Create(length: 4);
			var hostval2 = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val2, hostval2);

			// Act
			using var v = new SparseVector<double>(length: 10, val, ind);
			using var w = new SparseVector<double>(length: 10, val2, ind);

			// Assert
			Assert.AreNotEqual(v, w);
		}

		[TestMethod()]
		public void EqualsTest3()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var ind2 = Storage<int>.Create(length: 4);
			var hostind2 = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind2, hostind2);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);

			// Act
			using var v = new SparseVector<double>(length: 10, val, ind);
			using var w = new SparseVector<double>(length: 10, val, ind2);

			// Assert
			Assert.AreNotEqual(v, w);
		}

		[TestMethod()]
		public void HostConverterTest()
		{
			RT.Reset(); // Arrange
			var hostind = new int[] { 2, 4, 5, 9 };
			var hostval = new double[] { 2, 4, 5, 9 };
			// Act
			using var v = (SparseVector<double>)(hostval, hostind, 10, false);
			// Assert
			Assert.AreEqual(10, v.Length);
			Assert.AreEqual(hostind.Length, v.NonZero);
			Assert.IsTrue(hostind.SequenceEqual(v.IndexToIntArray().First()));
			Assert.IsTrue(hostval.SequenceEqual(v.ValueToFortranOrderArray()));
		}

		[TestMethod()]
		public void PrintTest()
		{
			RT.Reset(); // Arrange
			using var ind = Storage<int>.Create(length: 4);
			var hostind = new int[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(ind, hostind);
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var v = new SparseVector<double>(length: 10, val, ind);
			// Act
			Console.WriteLine(v.Print());
			// cannot be tested here
		}
	}
}