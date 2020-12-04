using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using MathNet.Numerics;
using MathS = MathNet.Numerics.LinearAlgebra.Single;
using MathD = MathNet.Numerics.LinearAlgebra.Double;
using MathC = MathNet.Numerics.LinearAlgebra.Complex32;
using MathZ = MathNet.Numerics.LinearAlgebra.Complex;

using RT = CudaCSharp.Runtime.API;
using CudaCSharp.Linq;
using CudaCSharp.Memory;


namespace CudaCSharp.Arrays.Tests
{
	internal static class Utilities
	{
		internal static bool ApproxSame(this float a, float b)
		{
			return Math.Abs(1 - a / b) <= General.Common.SingleMachinePrecision * 500;
		}

		internal static bool ApproxSame(this FloatComplex a, FloatComplex b)
		{
			return (1 - a / b).Abs() <= General.Common.SingleMachinePrecision * 500;
		}

		internal static bool ApproxSame(this double a, double b)
		{
			return Math.Abs(1 - a / b) <= General.Common.SingleMachinePrecision;
		}

		internal static bool ApproxSame(this DoubleComplex a, DoubleComplex b)
		{
			return (1 - a / b).Abs() <= General.Common.SingleMachinePrecision;
		}

		internal static bool ApproxSame(this Complex32 a, FloatComplex b)
		{
			return (1 - a / new Complex32(b.Real(), b.Imaginary())).Magnitude <= General.Common.SingleMachinePrecision * 500;
		}

		internal static bool ApproxSame(this Complex a, DoubleComplex b)
		{
			return (1 - a / new Complex(b.Real(), b.Imaginary())).Magnitude <= General.Common.SingleMachinePrecision * 500;
		}

		internal static bool ApproxEqual(this float[] x, float[] y)
		{
			if (x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
			{
				float a = x[i], b = y[i];
				if (Math.Abs(a - b) <= General.Common.SingleMachinePrecision * 500)
					continue;
				var div = Math.Abs(a / b);
				if (1 - div > General.Common.SingleMachinePrecision * 500)
					return false;
			}
			return true;
		}

		internal static bool ApproxEqualLowPrecision(this float[] x, float[] y)
		{
			if (x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
			{
				float a = x[i], b = y[i];
				if (Math.Abs(a - b) <= General.Common.SingleMachinePrecision * 500)
					continue;
				var div = Math.Abs(a / b);
				if (1 - div > General.Common.SingleMachinePrecision * 500)
					return false;
			}
			return true;
		}

		internal static bool ApproxEqual(this double[] x, double[] y)
		{
			if (x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
			{
				double a = x[i], b = y[i];
				if (Math.Abs(a - b) <= General.Common.SingleMachinePrecision)
					continue;
				var div = Math.Abs(a / b);
				if (1 - div > General.Common.SingleMachinePrecision)
					return false;
			}
			return true;
		}

		internal static bool ApproxEqual(this FloatComplex[] x, FloatComplex[] y)
		{
			if (x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
			{
				FloatComplex a = x[i], b = y[i];
				if ((a - b).Abs() <= General.Common.SingleMachinePrecision * 500)
					continue;
				var div = (a / b).Abs();
				if (1 - div > General.Common.SingleMachinePrecision * 500)
					return false;
			}
			return true;
		}

		internal static bool ApproxEqual(this DoubleComplex[] x, DoubleComplex[] y)
		{
			if (x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
			{
				DoubleComplex a = x[i], b = y[i];
				if ((a - b).Abs() <= General.Common.SingleMachinePrecision)
					continue;
				var div = (a / b).Abs();
				if (1 - div > General.Common.SingleMachinePrecision)
					return false;
			}
			return true;
		}

		internal static bool ApproxEqual(this Complex32[] x, FloatComplex[] y)
		{
			if (x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
			{
				Complex32 a = x[i], b = new Complex32(y[i].Real(), y[i].Imaginary());
				if ((a - b).Magnitude <= General.Common.SingleMachinePrecision * 500)
					continue;
				var div = (a / b).Magnitude;
				if (1 - div > General.Common.SingleMachinePrecision * 50)
					return false;
			}
			return true;
		}

		internal static bool ApproxEqual(this Complex[] x, DoubleComplex[] y)
		{
			if (x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
			{
				Complex a = x[i], b = new Complex(y[i].Real(), y[i].Imaginary());
				if ((a - b).Magnitude <= General.Common.SingleMachinePrecision)
					continue;
				var div = (a / b).Magnitude;
				if (1 - div > General.Common.SingleMachinePrecision)
					return false;
			}
			return true;
		}

		internal static Complex32[] ToMathNet(this FloatComplex[] floats)
		{
			var output = new Complex32[floats.Length];
			for (int i = 0; i < floats.Length; i++)
			{
				output[i] = new Complex32(floats[i].Real(), floats[i].Imaginary());
			}
			return output;
		}

		internal static Complex[] ToMathNet(this DoubleComplex[] floats)
		{
			var output = new Complex[floats.Length];
			for (int i = 0; i < floats.Length; i++)
			{
				output[i] = new Complex(floats[i].Real(), floats[i].Imaginary());
			}
			return output;
		}

		internal static IEnumerable<Tuple<int, Complex32>> ToMathNetSparse(this SparseVector<FloatComplex> x)
		{
			var val = x.ValueToFortranOrderArray();
			var ind = x.IndexToIntArray().First();
			return ind.Zip(val, (i, v) => new Tuple<int, Complex32>(i, new Complex32(v.Real(), v.Imaginary())));
		}

		internal static IEnumerable<Tuple<int, int, double>> ToMathNetSparse(this SparseMatrix<double> x)
		{
			var val = x.ValueToFortranOrderArray();
			using var COO = x.ToFormat(SparseMatrixFormat.Coordinated);
			var ind = COO.IndexToIntArray();
			return ind.First().Zip(ind.Last()).Zip(val, (ii, v) => new Tuple<int, int, double>(ii.First, ii.Second, v));
		}

		internal static IEnumerable<Tuple<int, int, Complex32>> ToMathNetSparse(this SparseMatrix<FloatComplex> x)
		{
			var val = x.ValueToFortranOrderArray();
			var COO = x.ToFormat(SparseMatrixFormat.Coordinated);
			try
			{
				var ind = COO.IndexToIntArray();
				var rowInd = ind.First(); var colInd = ind.Last();
				return rowInd.Zip(colInd, (r, c) => (row: r, col: c)).Zip(val, (ii, v) => Tuple.Create(ii.row, ii.col, new Complex32(v.Real(), v.Imaginary())));
			}
			finally
			{
				COO.DisposeComparedTo(x);
			}
		}
	}

	[TestClass()]
	public class DenseVectorTests
	{
		[TestMethod()]
		public void DenseVectorCreateTest()
		{
			RT.Reset(); // Arrange
			var free = RT.DeviceFreeMemory;
			// act
			using var dnvec = new DenseVector<FloatComplex>(length: 5000);
			// assert
			var freeNow = RT.DeviceFreeMemory;
			Assert.AreNotEqual(free, freeNow);
		}

		[TestMethod()]
		public void DenseVectorCopyCreateTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<float>(length: 10);
			vec1.FillWithRandoms();
			// act
			using var vec2 = new DenseVector<float>(vector: vec1);
			// assert
			Assert.AreNotEqual(vec1, vec2);
			var host1 = RT.CopyOutArray(vec1);
			var host2 = RT.CopyOutArray(vec2);
			Assert.IsTrue(host1.SequenceEqual(host2));
		}

		[TestMethod()]
		public void DenseVectorFullCreateTest()
		{
			RT.Reset(); // Arrange
			using var ptr = Storage<float>.Create(10);
			float real = 1.2f;
			RT.CopyInto(ptr, real);
			// act
			using var vec = new DenseVector<float>(values: ptr, length: 10);
			// assert
			var copyout = RT.CopyOut(vec);
			Assert.AreEqual(real, copyout);
		}

		[TestMethod()]
		public void DenseVectorRefCreateTest()
		{
			RT.Reset(); // Arrange
			using var vecref = new DenseVector<float>(length: 20);
			// act
			using var vec = new DenseVector<float>(refArray: vecref, newLength: 10, offset: 10);
			// assert
			float real = 1.2f;
			RT.CopyInto(vecref, real, offset: 10);
			var copyout = RT.CopyOut(vec);
			Assert.AreEqual(real, copyout);
		}


		[TestMethod()]
		public void LastIndexTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<float>(length: 10);

			// Act
			var li = v.LastIndex;

			// Assert
			Assert.AreEqual(9, li);
		}

		[TestMethod()]
		public void FromFortranOrderArrayTest()
		{
			RT.Reset(); // Arrange
			var hostval = new float[] { 2, 4, 6 };
			using var v = new DenseVector<float>(length: 10);

			// Act
			v.FromFortranOrderArray(hostval, 0..hostval.Length);

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(v.ToFortranOrderArray(0..hostval.Length)));
		}

		[TestMethod()]
		public void CloneTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<DoubleComplex>(length: 20);
			vec.FillWithRandoms();
			// act
			using var another = vec.Clone() as DenseVector<DoubleComplex>;
			// assert
			var host1 = RT.CopyOutArray(vec);
			var host2 = RT.CopyOutArray(another);
			Assert.IsTrue(host1.ApproxEqual(host2));
		}

		[TestMethod()]
		public void ConjugateTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();

			// Act
			using var cv = v.ConjugateOutOfPlace() as DenseVector<FloatComplex>;

			var real = v.ToFortranOrderArray().Select(a => a.Conjugate()).ToArray();

			// Assert
			Assert.AreNotEqual(v, cv);
			Assert.IsTrue(real.ApproxEqual(cv.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void NewArrayAlikeTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<double>(length: 10);

			// Act
			using var sv = v.NewArrayAlike() as DenseVector<double>;

			// Assert
			Assert.AreEqual(v.Length, sv.Length);
		}

		[TestMethod()]
		public void ToMatrixTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<double>(length: 20);
			v.FillWithRandoms();

			// Act
			using var m = v.ToMatrix(leadDim: 4) as DenseMatrix<double>;

			// Assert
			Assert.IsTrue(v.ToFortranOrderArray().SequenceEqual(m.ToFortranOrderArray()));
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
			using var v = new DenseVector<double>(length: 10);

			// Act
			v.FillWithZeros();

			// Assert
			Assert.IsTrue(v.ToFortranOrderArray().All(a => a == 0));
		}

		[TestMethod()]
		public void FillWithRandomsTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<double>(length: 10);

			// Act
			v.FillWithRandoms();

			// Assert
			Assert.AreEqual(v.Length, v.ToFortranOrderArray().Distinct().Count());
		}

		[TestMethod()]
		public void ToDenseTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<double>(length: 10);

			// Act
			var vv = v.ToDense();

			// Assert
			Assert.AreSame(v, vv);
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
			using var v = new DenseVector<double>(length: 10);

			// Act
			var vv = v.AsDenseVector();

			// Assert
			Assert.AreSame(v, vv);
		}

		[TestMethod()]
		public void ToSparseZeroThresholdTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 10);
			vec.FillWithZeros();
			Index[] indices = new Index[] { 1, 3, 5, 8 };
			double[] values = new double[] { 2, 4, 8, 16 };
			vec[indices] = (DenseVector<double>)(values, false);

			// act
			var spvec = vec.ToSparse();

			// assert
			Assert.AreEqual(4, spvec.NonZero);
			var host = RT.CopyOutArray(spvec[indices]);
			Assert.IsTrue(values.SequenceEqual(host));
		}

		[TestMethod()]
		public void ToSparseNonZeroThresholdTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 10);
			vec.FillWithZeros();
			Index[] indices = new Index[] { 1, 3, 7, 8 };
			double[] values = new double[] { 2, 4, 8, 16 };
			vec[indices] = (DenseVector<double>)(values, false);

			// act
			using var spvec = vec.ToSparse(threshold: 2);

			// assert
			Assert.AreEqual(3, spvec.NonZero);
			var host = RT.CopyOutArray(spvec[indices]);
			values[0] = 0;
			Assert.IsTrue(values.SequenceEqual(host));
		}

		[TestMethod()]
		public void DataTypeCastSameTypeTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 10);

			// act
			var newvec = vec.DataTypeCast<double>();

			// assert
			Assert.AreSame(vec, newvec);
		}

		[TestMethod()]
		public void DataTypeCastDifferentTypeTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<float>(length: 10);
			vec.FillWithRandoms();

			// act
			var newvec = vec.DataTypeCast<FloatComplex>() as DenseVector<FloatComplex>;

			// assert
			var host = RT.CopyOutArray(vec);
			var hostnew = RT.CopyOutArray(newvec).Select(v => v.Real());
			Assert.IsTrue(host.SequenceEqual(hostnew));
		}

		[TestMethod()]
		public void ScaleTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<float>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);

			// act
			vec.Scale(Scalars<float>.Half);

			// assert
			var newhostvecMul2 = RT.CopyOutArray(vec).Select(v => 2 * v).ToArray();
			Assert.IsTrue(hostvec.ApproxEqual(newhostvecMul2));
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
			using var vec = new DenseVector<double>(length: 10);

			// Act
			vec.ReplaceBy(v as VectorBase<double>);

			var real = new double[] { 0, 0, 2, 0, 4, 5, 0, 0, 0, 9 };

			// Assert
			Assert.IsTrue(real.SequenceEqual(vec.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void ReplaceByDenseTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<double>(length: 10);
			v.FillWithRandoms();
			using var vec = new DenseVector<double>(length: 10);

			// Act
			vec.ReplaceBy(v as VectorBase<double>);

			// Assert
			Assert.IsTrue(v.ToFortranOrderArray().SequenceEqual(vec.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AddByDenseVecTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);

			// act
			vec.AddBy_αx(vec as VectorBase<double>, 1f);

			// assert
			var newhostvecDiv2 = RT.CopyOutArray(vec).Select(v => v / 2).ToArray();
			Assert.IsTrue(hostvec.ApproxEqual(newhostvecDiv2));
		}

		[TestMethod()]
		public void AddBySparseVecTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 10);
			vec.FillWithRandoms();
			var hostvec = MathD.DenseVector.OfArray(RT.CopyOutArray(vec));
			using var spvec = new SparseVector<double>(length: 10, nonZeros: 3);
			spvec.FillWithRandoms();
			spvec.FillIndexWithRange((1, 3));
			using var dnvec = spvec.ToDense();
			var hostspvec = MathD.DenseVector.OfArray(RT.CopyOutArray(dnvec));

			// act
			vec.AddBy_αx(spvec as VectorBase<double>, 1.0);
			var hostnewvec = RT.CopyOutArray(vec);
			hostvec.Add(hostspvec, hostvec);

			// assert
			Assert.IsTrue(hostvec.ToArray().ApproxEqual(hostnewvec));
		}

		[TestMethod()]
		public void DenseDotDenseTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<FloatComplex>(length: 10);
			using var vec2 = new DenseVector<FloatComplex>(length: 10);
			vec1.FillWithRandoms();
			vec2.FillWithRandoms();
			var hostvec1 = RT.CopyOutArray(vec1);
			var hostvec2 = RT.CopyOutArray(vec2);

			// act
			FloatComplex dot = vec1.Dot(vec2);

			// assert
			FloatComplex real = hostvec1.Zip(hostvec2, (a, b) => a.Conjugate() * b).Sum();
			Assert.IsTrue(real.ApproxSame(dot));
		}

		[TestMethod()]
		public void DenseDotSparseTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<FloatComplex>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);
			using var spvec = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			spvec.FillWithRandoms();
			spvec.FillIndexWithRange((1, 3));
			using var dnvec = spvec.ToDense();
			var hostspvec = RT.CopyOutArray(dnvec);

			// act
			var dot = vec.Dot(spvec);
			var real = hostvec.Zip(hostspvec, (a, b) => a.Conjugate() * b).Sum();

			// assert
			Assert.IsTrue(real.ApproxSame(dot));
		}

		[TestMethod()]
		public void PointwiseMultiplyDenseTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<FloatComplex>(length: 10);
			using var vec2 = new DenseVector<FloatComplex>(length: 10);
			vec1.FillWithRandoms();
			vec2.FillWithRandoms();
			var hostvec1 = RT.CopyOutArray(vec1);
			var hostvec2 = RT.CopyOutArray(vec2);
			// act
			vec1.PointWiseMultiply(vec2);
			// assert
			var host = RT.CopyOutArray(vec1);
			var real = hostvec1.Zip(hostvec2, (a, b) => a * b).ToArray();
			Assert.IsTrue(real.ApproxEqual(host));
		}

		[TestMethod()]
		public void PointwiseMultiplySparseTest()
		{
			RT.Reset(); // Arrange
			using var vec = new DenseVector<double>(length: 10);
			vec.FillWithRandoms();
			var hostvec = RT.CopyOutArray(vec);
			using var spvec = new SparseVector<double>(length: 10, nonZeros: 3);
			spvec.FillWithRandoms();
			spvec.FillIndexWithRange((1, 3));
			using var dnvec = spvec.ToDense();
			var hostspvec = RT.CopyOutArray(dnvec);
			// act
			vec.PointWiseMultiply(spvec);
			using var dnnewvec = spvec.ToDense();
			var hostnewvec = RT.CopyOutArray(dnnewvec);
			hostvec = hostvec.Zip(hostspvec, (a, b) => a * b).ToArray();
			// assert
			Assert.IsTrue(hostvec.ApproxEqual(hostnewvec));
		}

		[TestMethod()]
		public void PointwiseDivisionTest()
		{
			RT.Reset(); // Arrange
			using var vec1 = new DenseVector<FloatComplex>(length: 10);
			using var vec2 = new DenseVector<FloatComplex>(length: 10);
			vec1.FillWithRandoms();
			vec2.FillWithRandoms();
			var hostvec1 = RT.CopyOutArray(vec1);
			var hostvec2 = RT.CopyOutArray(vec2);
			// act
			vec1.PointWiseDivide(vec2);
			// assert
			var host = RT.CopyOutArray(vec1);
			var real = hostvec1.Zip(hostvec2, (a, b) => a / b).ToArray();
			Assert.IsTrue(host.ApproxEqual(real));
		}


		#region add by matrix multiply vector
		public static void AddBy_SparseMatrix_Multiply_SparseVector_Test(SparseMatrixFormat format, MatrixOperation op)
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));
			m.FillWithRandoms();
			m.FillIndexWithRange((start: 0, step: 1), (start: 0, step: 1));

			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());

			// Act
			v.Mulβ_AddBy_αopAx(m, w, 1, 1, op);
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
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
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var m = new SparseMatrix<FloatComplex>(rows: 10, cols: 10, nonZeros: 10, format);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			w.FillWithRandoms();
			m.FillWithRandoms();
			m.FillIndexWithRange((start: 0, step: 1), (start: 0, step: 1));

			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());
			var hostm = MathC.SparseMatrix.OfIndexed(10, 10, m.ToMathNetSparse());

			// Act
			v.Mulβ_AddBy_αopAx(m, w, 1, 1, op);
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
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
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));
			m.FillWithRandoms();

			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
			var hostw = MathC.SparseVector.OfIndexedEnumerable(10, w.ToMathNetSparse());
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			v.Mulβ_AddBy_αopAx(m, w, 1, 1, op);
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
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
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var m = new DenseMatrix<FloatComplex>(rows: 10, cols: 10);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			w.FillWithRandoms();
			m.FillWithRandoms();

			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
			var hostw = MathC.DenseVector.OfArray(w.ToFortranOrderArray().ToMathNet());
			var hostm = MathC.DenseMatrix.OfColumnMajor(10, 10, m.ToFortranOrderArray().ToMathNet());

			// Act
			v.Mulβ_AddBy_αopAx(m, w, 1, 1, op);
			var real = op == MatrixOperation.None ? hostv.Add(hostm.Multiply(hostw)) :
					op == MatrixOperation.Transpose ? hostv.Add(hostm.TransposeThisAndMultiply(hostw)) :
														hostv.Add(hostm.ConjugateTransposeThisAndMultiply(hostw));

			// Assert
			Assert.IsTrue(real.ToArray().ApproxEqual(v.ToFortranOrderArray()));
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
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var w = new SparseVector<FloatComplex>(length: 10, nonZeros: 3);
			v.FillWithRandoms();
			w.FillWithRandoms();
			w.FillIndexWithRange((start: 0, step: 3));

			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
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
			using var v = new DenseVector<FloatComplex>(length: 10);
			using var w = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			w.FillWithRandoms();

			var hostv = MathC.DenseVector.OfArray(v.ToFortranOrderArray().ToMathNet());
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
			using var v = new DenseVector<FloatComplex>(length: 10);
			var hostval = new FloatComplex[] { 1, 3, 65, 23, 23, 4, 32, 3, 34, 4 };
			v.FromFortranOrderArray(hostval);

			// Act
			v[^1] = v[^1];

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void IndexerMultiItemTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			var hostval = new FloatComplex[] { 1, 3, 65, 23, 23, 4, 32, 3, 34, 4 };
			v.FromFortranOrderArray(hostval);

			// Act
			v[^1, 2] = v[^1, 2];

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(v.ToFortranOrderArray()));
		}


		[TestMethod()]
		public void IndexerRangeItemTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			var hostval = new FloatComplex[] { 1, 3, 65, 23, 23, 4, 32, 3, 34, 4 };
			v.FromFortranOrderArray(hostval);

			// Act
			v[2..^4] = v[2..^4];

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(v.ToFortranOrderArray()));
		}


		[TestMethod()]
		public void IndexerMultiRangeItemTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			var hostval = new FloatComplex[] { 1, 3, 65, 23, 23, 4, 32, 3, 34, 4 };
			v.FromFortranOrderArray(hostval);

			// Act
			v[1..4, 7..^1] = v[1..4, 7..^1];

			// Assert
			Assert.IsTrue(hostval.SequenceEqual(v.ToFortranOrderArray()));
		}


		[TestMethod()]
		public void GetHashCodeTest()
		{
			RT.Reset(); // Arrange
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var val2 = Storage<double>.Create(length: 4);
			var hostval2 = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val2, hostval2);

			// Act
			using var v = new DenseVector<double>(val, length: 10);
			using var w = new DenseVector<double>(val2, length: 10);

			// Assert
			Assert.AreNotEqual(v.GetHashCode(), w.GetHashCode());
		}

		[TestMethod()]
		public void EqualsTest()
		{
			RT.Reset(); // Arrange
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);

			// Act
			using var v = new DenseVector<double>(val, length: 10);
			using var w = new DenseVector<double>(val, length: 10);

			// Assert
			Assert.AreEqual(v, w);
		}

		[TestMethod()]
		public void EqualsTest2()
		{
			RT.Reset(); // Arrange
			using var val = Storage<double>.Create(length: 4);
			var hostval = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val, hostval);
			using var val2 = Storage<double>.Create(length: 4);
			var hostval2 = new double[] { 2, 4, 5, 9 };
			RT.CopyIntoArray(val2, hostval2);

			// Act
			using var v = new DenseVector<double>(val, length: 10);
			using var w = new DenseVector<double>(val2, length: 10);

			// Assert
			Assert.AreNotEqual(v, w);
		}

		[TestMethod()]
		public void HostConverterTest()
		{
			RT.Reset(); // Arrange
			var hostval = new double[] { 2, 4, 5, 9 };
			// Act
			using var v = (DenseVector<double>)(hostval, false);
			// Assert
			Assert.AreEqual(hostval.Length, v.Length);
			Assert.IsTrue(hostval.SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void PrintTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			// Act
			Console.WriteLine(v.Print());
			// cannot be tested here
		}
	}
}