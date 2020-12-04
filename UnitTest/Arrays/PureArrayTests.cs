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


namespace CudaCSharp.Arrays.Tests
{
	[TestClass()]
	public class PureArrayTests
	{
		[TestMethod()]
		public void TruncateTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = v.ToFortranOrderArray();

			// Act
			v.Truncate(0.5f);

			// Assert
			var real = hostv.Select(a => a > 0.5f/10 ? a : 0).ToArray();
			Assert.IsTrue(real.SequenceEqual(v.ToFortranOrderArray()));
		}

		[TestMethod()]
		public void AbsSumTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = v.ToFortranOrderArray();

			// Act
			var test = (float)v.AbsSum();
			var real = hostv.Sum(a => Math.Abs(a.Real()) + Math.Abs(a.Imaginary()));

			// Assert
			Assert.AreEqual(real, test);
		}

		[TestMethod()]
		public void ArgMaxAbsTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = v.ToFortranOrderArray();

			// Act
			var test = (int)v.ArgMaxAbs();
			long real = -1;
			float max = 0;
			for (var i = 0; i < hostv.Length; i++)
			{
				if (hostv[i].Abs() > max)
				{
					real = i;
					max = hostv[i].Abs();
				}
			}

			// Assert
			Assert.AreEqual(real, test);
		}

		[TestMethod()]
		public void ArgMinAbsTest()
		{
			RT.Reset(); // Arrange
			using var v = new DenseVector<FloatComplex>(length: 10);
			v.FillWithRandoms();
			var hostv = v.ToFortranOrderArray();

			// Act
			var test = (int)v.ArgMinAbs();
			long real = -1;
			float min = 1000;
			for (var i = 0; i < hostv.Length; i++)
			{
				if (hostv[i].Abs() < min)
				{
					real = i;
					min = hostv[i].Abs();
				}
			}

			// Assert
			Assert.AreEqual(real, test);
		}
	}
}