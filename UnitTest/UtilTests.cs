using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using RT = CudaCSharp.Runtime.API;


namespace CudaCSharp.Linq.Tests
{
	[TestClass()]
	public class UtilTests
	{
		[TestMethod()]
		public void ColumnTakeTest()
		{
			RT.Reset(); // Arrange
			int[,] hostMat = new int[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 }, { 10, 11, 12 } };
			int[] hostVec = new int[] { 1, 4, 7, 10, 2, 5, 8, 11, 3, 6, 9, 12 };
			// act
			var testVec = hostMat.ColumnTake();
			// assert
			Assert.IsTrue(hostVec.SequenceEqual(testVec));
		}

		[TestMethod()]
		public void GetPositionTest()
		{
			RT.Reset(); // Arrange
			int last = 5;
			Index ind = ^last;
			long len = 2 * (long)int.MaxValue;
			// act
			var pos = ind.GetPosition(len);
			var real = len - last;
			// assert
			Assert.AreEqual(real, pos);
		}

		[TestMethod()]
		public void GetOffsetAndCountTest()
		{
			RT.Reset(); // Arrange
			int first = int.MaxValue, last = 5;
			Index indFirst = ^first;
			Index indLast = ^last;
			long len = 4 * (long)int.MaxValue;
			Range range = indFirst..indLast;
			// act
			var (offset, count) = range.GetOffsetAndCount(len);
			long offsetReal = len - first, countReal = first - last;
			// assert
			Assert.AreEqual(offsetReal, offset);
			Assert.AreEqual(countReal, count);
		}

		[TestMethod()]
		public void ToCudaDataTypeTest()
		{
			RT.Reset(); // Arrange
			DataType dataType = (DataType)(1 << 10);
			// act and assert
			Assert.ThrowsException<NotSupportedException>(() => dataType.ToCudaDataType());
		}

		[TestMethod()]
		public void IsPerfectSquareTest()
		{
			RT.Reset(); // Arrange
			long square = 100, notSquare = 1000;
			// act
			bool isSquare1 = square.IsPerfectSquare(), isSquare2 = notSquare.IsPerfectSquare();
			// assert
			Assert.IsTrue(isSquare1);
			Assert.IsFalse(isSquare2);
		}

		[TestMethod()]
		public void IsPowerOfTwoTest()
		{
			RT.Reset(); // Arrange
			long pow = 1 << 10, notPow = -1023, notPow2 = -1024, pow2 = 1;
			// act
			bool isPow1 = pow.IsPowerOfTwo(), isPow2 = notPow.IsPowerOfTwo(), isPow3 = notPow2.IsPowerOfTwo(), isPow4 = pow2.IsPowerOfTwo();
			// assert
			Assert.IsTrue(isPow1);
			Assert.IsFalse(isPow2);
			Assert.IsFalse(isPow3);
			Assert.IsTrue(isPow4);
		}

		[TestMethod()]
		public void NearestPowerOf2Test()
		{
			RT.Reset(); // Arrange
			long num1 = 1 << 10, num3 = 1000;
			// act
			long pow1 = num1.NearestPowerOfTwo(), pow3 = num3.NearestPowerOfTwo();
			// assert
			Assert.AreEqual(num1, pow1);
			Assert.AreEqual(1024, pow3);
		}

		[TestMethod()]
		public void AccumulateTest()
		{
			RT.Reset(); // Arrange
			var array = new[] { 1, 2, 3, 4 };
			// act
			var accu = array.Accumulate((v, a) => v + a, 10);
			// assert
			var real = new[] { 10, 11, 13, 16, 20 };
			Assert.IsTrue(real.SequenceEqual(accu));
		}

		[TestMethod()]
		public void ProdTest()
		{
			RT.Reset(); // Arrange
			var array = new long[] { 2, 3, 4 };
			// act
			var prod = array.Prod();
			// assert
			var real = 24;
			Assert.AreEqual(real, prod);
		}

		static readonly Random rand = new Random();

		[TestMethod()]
		public void GenericReciprocalTest()
		{
			RT.Reset(); // Arrange
			float a = (float)rand.NextDouble();
			double b = rand.NextDouble();
			FloatComplex c = new FloatComplex((float)rand.NextDouble(), (float)rand.NextDouble());
			DoubleComplex d = new DoubleComplex(rand.NextDouble(), rand.NextDouble());
			// act
			var ar = a.GenericReciprocal();
			var br = b.GenericReciprocal();
			var cr = c.GenericReciprocal();
			var dr = d.GenericReciprocal();
			var aReal = 1 / a;
			var bReal = 1 / b;
			var cReal = 1 / c;
			var dReal = 1 / d;
			// assert
			Assert.AreEqual(aReal, ar);
			Assert.AreEqual(bReal, br);
			Assert.AreEqual(cReal, cr);
			Assert.AreEqual(dReal, dr);
		}

		[TestMethod()]
		public void GenericNegateTest()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void GenericConjugateTest()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void GenericConvertTest()
		{
			RT.Reset(); // Arrange
			int a = rand.Next();
			// act
			float s = a.GenericConvert<float, int>();
			// assert
			Assert.AreEqual(a, s);
		}

		[TestMethod()]
		public void ArrayToLongTest()
		{
			RT.Reset(); // Arrange
			double[] arr = new double[] { 1, 2, 3, 4 };
			// act
			var arrLong = arr.ToLongs();
			// assert
			long[] arrReal = new long[] { 1, 2, 3, 4 };
			Assert.IsTrue(arrReal.SequenceEqual(arrLong));
		}

		[TestMethod()]
		public void ToComplexArrayTest()
		{
			RT.Reset(); // Arrange
			float[] arr = new float[] { 1, 2, 3, 4 };
			// act 
			var complex = arr.ToComplexArray();
			// assert
			var real = new[] { new FloatComplex(1, 2), new FloatComplex(3, 4) };
			Assert.IsTrue(real.SequenceEqual(complex));
		}

		[TestMethod()]
		public void ToComplexArrayTest1()
		{
			RT.Reset(); // Arrange
			double[] arr = new double[] { 1, 2, 3, 4 };
			// act 
			var complex = arr.ToComplexArray();
			// assert
			var real = new[] { new DoubleComplex(1, 2), new DoubleComplex(3, 4) };
			Assert.IsTrue(real.SequenceEqual(complex));
		}

		[TestMethod()]
		public void ForEachTest()
		{
			RT.Reset(); // Arrange
			double[] arr = new double[] { 2, 0, 1, 4 };
			// act
			double result = 0.0;
			arr.ForEach((v, i) => result += v * i);
			// assert
			double real = 2 * 1 + 3 * 4;
			Assert.AreEqual(real, result);
		}

		[TestMethod()]
		public void ToVectorStringTest()
		{
			RT.Reset(); // Arrange
			double[] arr = new double[] { 1, -2, 3, -4 };
			// act
			string str = arr.ToVectorString(precision: 2);
			// assert
			string nl = Environment.NewLine;
			string real = $" {arr[0]:e2}{nl}{arr[1]:e2}{nl} {arr[2]:e2}{nl}{arr[3]:e2}";
			Assert.AreEqual(real, str);
		}

		[TestMethod()]
		public void ToSparseVectorStringTest()
		{
			RT.Reset(); // Arrange
			double[] arr = new double[] { 1, -2, 3, -4 };
			int[] ind = new int[] { 4, 9, 16, 40 };
			// act
			string str = arr.ToSparseVectorString(ind, precision: 2);
			// assert
			string nl = Environment.NewLine;
			string real = $"{ind[0]} → {arr[0]:e2}{nl}{ind[1]} → {arr[1]:e2}{nl}{ind[2]} → {arr[2]:e2}{nl}{ind[3]} → {arr[3]:e2}";
			Assert.AreEqual(real, str);
		}

		// Must be done after column take test
		[TestMethod()]
		public void Make2DArrayTest()
		{
			RT.Reset(); // Arrange
			int[,] hostMat = new int[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 }, { 10, 11, 12 } };
			int[] hostVec = new int[] { 1, 4, 7, 10, 2, 5, 8, 11, 3, 6, 9, 12 };
			// act
			int[,] res = hostVec.Make2DArray(rows: 4, columns: 3);
			// assert
			int[] oneDmat = new int[hostMat.Length];
			Buffer.BlockCopy(hostMat, 0, oneDmat, 0, hostMat.Length);
			int[] oneDvec = new int[res.Length];
			Buffer.BlockCopy(res, 0, oneDvec, 0, res.Length);
			Assert.IsTrue(oneDmat.SequenceEqual(oneDvec));
		}

		[TestMethod()]
		public void GetRowColumnsTest()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void SelectTest()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void ForEachTest1()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void ForEachTest2()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void ForEachTest3()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void ForEachTest4()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void ForEachEarlyStopTest()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void SelectTest1()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void SelectTest2()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void IsHermitianTest()
		{
			// it cannot be wrong
		}

		[TestMethod()]
		public void ToMatrixStringTest()
		{
			// tested
		}

		[TestMethod()]
		public void ToSparseMatrixStringTest()
		{
			// tested
		}

		[TestMethod()]
		public void GenerateRangeTest()
		{
			// act
			var range = ArrayLinq.Range(10, 5, step: -2).ToArray();
			// assert
			var real = new int[] { 10, 8, 6, 4, 2 };
			Assert.IsTrue(real.SequenceEqual(range));
		}
	}
}