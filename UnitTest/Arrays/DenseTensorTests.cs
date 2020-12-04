using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using CudaCSharp.Linq;

using static CudaCSharp.Arrays.Tests.Utilities;

using MathNet.Numerics;
using MathS = MathNet.Numerics.LinearAlgebra.Single;
using MathD = MathNet.Numerics.LinearAlgebra.Double;
using MathC = MathNet.Numerics.LinearAlgebra.Complex32;
using MathZ = MathNet.Numerics.LinearAlgebra.Complex;

using RT = CudaCSharp.Runtime.API;


namespace CudaCSharp.Arrays.Tests
{
	[TestClass]
	public class DenseTensorTests
	{
		// used for start test in Linux
		static void Main(string[] args)
		{
			var obj = new DenseTensorTests();
			foreach (var item in typeof(DenseTensorTests).GetMethods())
			{
				if (item.GetCustomAttributes(inherit: true).Any() && Array.FindIndex(args, a => a == item.Name) >= 0)
				{
					try
					{
						Console.WriteLine($"Testing method {item.Name} ...");
						item.Invoke(obj, Array.Empty<object>());
					}
					catch (Exception e)
					{
						Console.WriteLine(e.ToString());
						Console.WriteLine("----------------------------------------------------------------------------");
						Console.WriteLine("----------------------------------------------------------------------------");
					}
				}
			}
		}

		[TestMethod]
		public void SetLabelTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			char[] label = new[] { 'x', 'y', 'z' };

			// Act
			denseTensor.Label = label;

			// Assert
			Assert.AreEqual(label, denseTensor.Label);
		}

		[TestMethod]
		public void FillWithZerosTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));

			// Act
			denseTensor.FillWithZeros();

			var host = RT.CopyOutArray(denseTensor);

			// Assert
			Assert.IsTrue(host.All(a => a == 0));
		}

		[TestMethod]
		public void FillWithRandomsTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));

			// Act
			denseTensor.FillWithRandoms();

			var host = RT.CopyOutArray(denseTensor);

			// Assert
			Assert.IsTrue(host.Skip(1).All(a => a != host[0]));
		}

		[TestMethod]
		public void ToFortranOrderArrayTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			Range[] ranges = new[] { .., .., .. };

			// Act
			var result = denseTensor.ToFortranOrderArray(ranges);

			var host = RT.CopyOutArray(denseTensor);

			// Assert
			Assert.IsTrue(host.SequenceEqual(result));
		}

		[TestMethod]
		public void FromFortranOrderArrayTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			double[] values = new[] { 1.0, 2, 3, 4, 5, 6, 7, 8 };
			Range[] ranges = new[] { .., .., .. };

			// Act
			denseTensor.FromFortranOrderArray(values, ranges);

			var host = RT.CopyOutArray(denseTensor);

			// Assert
			Assert.IsTrue(host.SequenceEqual(values));
		}


		[TestMethod]
		public void AsDenseVectorTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));

			// Act
			var result = denseTensor.AsDenseVector();

			var real = denseTensor.ToVector();

			// Assert
			Assert.AreEqual(real, result);
		}

		[TestMethod]
		public void CloneTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			denseTensor.FillWithRandoms();
			var real = denseTensor.ToFortranOrderArray();

			// Act
			var result = denseTensor.Clone();
			var test = (result as DenseTensor<double>).ToFortranOrderArray();

			// Assert
			Assert.IsTrue(real.SequenceEqual(test));
		}

		[TestMethod]
		public void DataTypeCastTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));

			var host = denseTensor.ToFortranOrderArray();

			// Act
			var result = denseTensor.DataTypeCast<float>() as DenseTensor<float>;

			var test = result.ToFortranOrderArray();

			// Assert
			Assert.IsTrue(host.Select(a => (float)a).SequenceEqual(test));
		}

		[TestMethod]
		public void NewArrayAlikeTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));

			// Act
			var result = denseTensor.NewArrayAlike();

			// Assert
			Assert.IsTrue(new long[] { 2, 2, 2 }.SequenceEqual(result.Size));
		}

		[TestMethod]
		public void ToTheOtherMemoryTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			denseTensor.FillWithRandoms();
			var real = denseTensor.ToFortranOrderArray();

			// Act
			var result = denseTensor.ToTheOtherMemory() as DenseTensor<double>;

			var test = result.ToFortranOrderArray();

			// Assert
			Assert.IsTrue(result.OnHost);
			Assert.IsTrue(real.SequenceEqual(test));
		}

		[TestMethod]
		public void OperatorAddTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			var right = new DenseTensor<double>((2, 2, 2));
			denseTensor.FillWithRandoms();
			right.FillWithRandoms();

			// Act
			using var result = denseTensor + right;

			using var real = denseTensor.AsDenseVector() + right.AsDenseVector() as DenseVector<double>;

			// Assert
			Assert.IsTrue(real.ToFortranOrderArray().SequenceEqual(result.ToFortranOrderArray()));
		}

		[TestMethod]
		public void OperatorSubTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			var right = new DenseTensor<double>((2, 2, 2));
			denseTensor.FillWithRandoms();
			right.FillWithRandoms();

			// Act
			using var result = denseTensor - right;

			using var real = denseTensor.AsDenseVector() - right.AsDenseVector() as DenseVector<double>;

			// Assert
			Assert.IsTrue(real.ToFortranOrderArray().SequenceEqual(result.ToFortranOrderArray()));
		}

		[TestMethod]
		public void OperatorNegateTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			denseTensor.FillWithRandoms();
			var real = denseTensor.ToFortranOrderArray();

			// Act
			var result = -denseTensor;

			var test = result.ToFortranOrderArray().Select(a => -a);

			// Assert
			Assert.IsTrue(real.SequenceEqual(test));
		}

		[TestMethod]
		public void OperatorScaleTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			double α = 2;
			denseTensor.FillWithRandoms();
			var real = denseTensor.ToFortranOrderArray();

			// Act
			var result = denseTensor * α;

			var test = result.ToFortranOrderArray().Select(a => α * a);

			// Assert
			Assert.IsTrue(real.SequenceEqual(test));
		}

		[TestMethod]
		public void OperatorContractTest()
		{
			RT.Reset(); // Arrange
			var left = new DenseTensor<double>((200, 4, 400), onHost: true);
			var right = new DenseTensor<double>((400, 200, 4), onHost: true);
			double[] values = ArrayLinq.Range(1, 200 * 4 * 400).Select(a => (double)a).ToArray();
			left.FromFortranOrderArray(values);
			right.FromFortranOrderArray(values);

			left.SetLabel('a', 'b', 'c');
			right.SetLabel('c', 'a', 'd');

			// Act
			var result = left.OperatorContract(right);

			double[] real = new double[] { 512007980020000, 512647988020000, 513287996020000, 513928004020000, 1534091180020000, 1536011188020000, 1537931196020000, 1539851204020000, 2556174380020000, 2559374388020000, 2562574396020000, 2565774404020000, 3578257580020000, 3582737588020000, 3587217596020000, 3591697604020000 };

			// Assert
			Assert.IsTrue(new long[] { 4, 4 }.SequenceEqual(result.Size));
			Assert.IsTrue(real.SequenceEqual(result.ToFortranOrderArray()));
		}

		[TestMethod]
		public void TensorProductTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			var other = new DenseTensor<double>((2, 2, 2));
			denseTensor.FillWithRandoms();
			other.FillWithRandoms();

			// Act
			using var result = denseTensor.TensorProduct(other, 1.0);

			using var real = denseTensor.AsDenseVector().OuterProduct(other.AsDenseVector());

			// Assert
			Assert.IsTrue(real.ToFortranOrderArray().SequenceEqual(result.ToFortranOrderArray()));
		}

		[TestMethod]
		public void PermuteTest()
		{
			string a = typeof(TTGT.CuTT.CUTensor).AssemblyQualifiedName;
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 3, 4), onHost: true);
			var hostVal = Enumerable.Range(1, 2 * 3 * 4).Select(a => (double)a).ToArray();
			denseTensor.FromFortranOrderArray(hostVal);
			double α = 1.0;
			UnitaryOperation op = UnitaryOperation.Negate;
			TensorOrder newOrder = (1, 0, 2);

			// Act
			using var result = denseTensor.Permute(α, op, newOrder);

			var real = new double[] { 1, 3, 5, 2, 4, 6, 7, 9, 11, 8, 10, 12, 13, 15, 17, 14, 16, 18, 19, 21, 23, 20, 22, 24 };

			// Assert
			Assert.IsTrue(new long[] { 3, 2, 4 }.SequenceEqual(result.Size));
			Assert.IsTrue(real.ApproxEqual(result.ToFortranOrderArray()));
		}

		[TestMethod]
		public void ReduceTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2));
			denseTensor.SetLabel('a', 'b');
			BinaryOperation reduction = BinaryOperation.Add;
			double α = 1;
			UnitaryOperation opA = UnitaryOperation.Identity;
			var A = new DenseTensor<double>((2, 2, 2));
			A.FillWithRandoms();
			A.SetLabel('a', 'b', 'c');

			// Act
			denseTensor.Reduce(reduction, α, opA, A);

			var real = A.GetSpan(2, 0).AsDenseVector() + A.GetSpan(2, 1).AsDenseVector() as DenseVector<double>;

			// Assert
			Assert.IsTrue(real.ToFortranOrderArray().SequenceEqual(denseTensor.ToFortranOrderArray()));
		}

		[TestMethod]
		public void ContractTest()
		{
			// tested in operation *
		}

		[TestMethod]
		public void PrintTest()
		{
			RT.Reset(); // Arrange
			var denseTensor = new DenseTensor<double>((2, 2, 2));
			denseTensor.FillWithRandoms();

			// Act
			var result = denseTensor.Print();

			// Assert
			Console.WriteLine(result);
		}
	}
}
