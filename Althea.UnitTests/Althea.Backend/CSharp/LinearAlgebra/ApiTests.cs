using System;

using Althea.LinearAlgebra;
using Althea.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

using Newtonsoft.Json.Linq;

namespace Althea.Backend.CSharp.LinearAlgebra.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new();
	private static readonly Storage.Api storage = new();

	[TestMethod()]
	public void AbsoluteValueArgMaxTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		bool success = api.AbsoluteValueArgMax<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1, out long index);
		Assert.IsTrue(success);

		Span<double> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		long realIndex = 0; double max = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (Math.Abs(values[i]) > max)
			{
				realIndex = i; max = Math.Abs(values[i]);
			}
		}
		Assert.AreEqual(realIndex, index);
	}

	[TestMethod()]
	public void AbsoluteValueArgMinTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		bool success = api.AbsoluteValueArgMin<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1, out long index);
		Assert.IsTrue(success);

		Span<double> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		long realIndex = 0; double min = double.MaxValue;
		for (int i = 0; i < values.Length; i++)
		{
			if (Math.Abs(values[i]) < min)
			{
				realIndex = i; min = Math.Abs(values[i]);
			}
		}
		Assert.AreEqual(realIndex, index);
	}

	[TestMethod()]
	public void AbsoluteValueArgMaxComplexTest()
	{
		var _array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array = _array.As<Complex<Float64>>();
		bool success = api.AbsoluteValueArgMax<Complex<Float64>, PureStorage<Complex<Float64>, CpuMemoryPointer>>(array, 1, out long index);
		Assert.IsTrue(success);

		Span<Complex<Float64>> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		long realIndex = 0; double max = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (Complex<Float64>.Abs(values[i]).Real > max)
			{
				realIndex = i; max = Complex<Float64>.Abs(values[i]).Real;
			}
		}
		Assert.AreEqual(realIndex, index);
	}

	[TestMethod()]
	public void AbsoluteValueArgMinComplexTest()
	{
		var _array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array = _array.As<Complex<Float64>>();
		bool success = api.AbsoluteValueArgMin<Complex<Float64>, PureStorage<Complex<Float64>, CpuMemoryPointer>>(array, 1, out long index);
		Assert.IsTrue(success);

		Span<Complex<Float64>> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		long realIndex = 0; double min = double.MaxValue;
		for (int i = 0; i < values.Length; i++)
		{
			if (Complex<Float64>.Abs(values[i]).Real < min)
			{
				realIndex = i; min = Complex<Float64>.Abs(values[i]).Real;
			}
		}
		Assert.AreEqual(realIndex, index);
	}

	[TestMethod()]
	public void AbsoluteValueSumTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		bool success = api.AbsoluteValueSum(array, 1, out Float64 sum);
		Assert.IsTrue(success);

		Span<double> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		double real = 0;
		for (int i = 0; i < values.Length; i++)
		{
			real += double.Abs(values[i]);
		}
		ValueAssert.AreApproxEqual((Float64)real, sum);
	}

	[TestMethod()]
	public void AbsoluteValueSumComplexTest()
	{
		var _array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array = _array.As<Complex<Float64>>();
		bool success = api.AbsoluteValueSum(array, 1, out Complex<Float64> sum);
		Assert.IsTrue(success);

		Span<Complex<Float64>> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		Complex<Float64> real = default;
		for (int i = 0; i < values.Length; i++)
		{
			real += Complex<Float64>.Abs(values[i]);
		}
		ValueAssert.AreApproxEqual(real.Real, sum.Real);
	}

	[TestMethod()]
	public void NormTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		bool success = api.Norm(array, 1, out Float64 sum);
		Assert.IsTrue(success);

		Span<double> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		double real = 0;
		for (int i = 0; i < values.Length; i++)
		{
			real += values[i] * values[i];
		}
		real = double.Sqrt(real);
		ValueAssert.AreApproxEqual((Float64)real, sum);
	}

	[TestMethod()]
	public void NormComplexTest()
	{
		var _array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array = _array.As<Complex<Float64>>();
		bool success = api.Norm(array, 1, out Complex<Float64> sum);
		Assert.IsTrue(success);

		Span<Complex<Float64>> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		double real = default;
		for (int i = 0; i < values.Length; i++)
		{
			real += values[i].MagnitudeSquared;
		}
		real = double.Sqrt(real);
		ValueAssert.AreApproxEqual((Float64)real, sum.Real);
	}

	[TestMethod()]
	public void ScaleTest()
	{
		Float64 SCALAR = 5.0;

		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		Float64* values = stackalloc Float64[(int)array.Length];
		array.CopyToManaged(values);

		bool success = api.Scale(array, 1, SCALAR);
		Assert.IsTrue(success);

		Float64* test = (Float64*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			values[i] *= SCALAR;
			ValueAssert.AreApproxEqual(values[i], test[i]);
		}
	}

	[TestMethod()]
	public void AddTest()
	{
		Float64 SCALAR = 5.0;

		var array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		Float64* values1 = stackalloc Float64[(int)array1.Length];
		array1.CopyToManaged(values1);
		var array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		Float64* values2 = stackalloc Float64[(int)array2.Length];
		array2.CopyToManaged(values2);

		bool success = api.Add(SCALAR, array1, 1, array2, 1);
		Assert.IsTrue(success);

		Float64* test = (Float64*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			values2[i] += SCALAR * values1[i];
			ValueAssert.AreApproxEqual(values2[i], test[i]);
		}
	}

	[TestMethod()]
	public void AddComplexTest()
	{
		Complex<Float64> SCALAR = new(5.0, 4.0);

		var _array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array1 = _array1.As<Complex<Float64>>();
		Complex<Float64>* values1 = stackalloc Complex<Float64>[(int)array1.Length];
		array1.CopyToManaged(values1);
		var _array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = _array2.As<Complex<Float64>>();
		Complex<Float64>* values2 = stackalloc Complex<Float64>[(int)array2.Length];
		array2.CopyToManaged(values2);

		bool success = api.Add(SCALAR, array1, 1, array2, 1);
		Assert.IsTrue(success);

		Complex<Float64>* test = (Complex<Float64>*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			values2[i] += SCALAR * values1[i];
			ValueAssert.AreApproxEqual(values2[i], test[i]);
		}
	}

	[TestMethod()]
	public void DotTest()
	{
		var array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.Dot(true, array1, 1, array2, 1, out Float64 dot);
		Assert.IsTrue(success);

		double real = 0;
		double* test1 = (double*)array1.Pointer.Pointer.Pointer.ToPointer();
		double* test2 = (double*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			real += test1[i] * test2[i];
		}
		ValueAssert.AreApproxEqual((Float64)real, dot);
	}

	[TestMethod()]
	[DataRow(true)]
	[DataRow(false)]
	public void DotComplexTest(bool conj)
	{
		var _array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array1 = _array1.As<Complex<Float64>>();
		var _array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = _array2.As<Complex<Float64>>();

		bool success = api.Dot(conj, array1, 1, array2, 1, out Complex<Float64> dot);
		Assert.IsTrue(success);

		Complex<Float64> real = default;
		Complex<Float64>* test1 = (Complex<Float64>*)array1.Pointer.Pointer.Pointer.ToPointer();
		Complex<Float64>* test2 = (Complex<Float64>*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			real += conj ? test1[i].Conjugate * test2[i] : test1[i] * test2[i];
		}
		ValueAssert.AreApproxEqual(real, dot);
	}

	[TestMethod()]
	[DataRow(true)]
	[DataRow(false)]
	public void GeneralVectorsEqualTest(bool copy)
	{
		var array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		if (copy)
			array1.NoApiCopy(array2);
		array1 = array1.MakeReference(0, Math.Min(array1.Length, array2.Length));
		array2 = array2.MakeReference(0, Math.Min(array1.Length, array2.Length));

		bool success = api.GeneralVectorsEqual<Float64, PureStorage<Float64, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>>(array1, 1, array2, 1, out bool equals);
		Assert.IsTrue(success);
		Assert.AreEqual(copy, equals);
	}

	[TestMethod()]
	[DataRow(UnaryOperation.Negate)]
	public void GeneralVectorUnaryTest(UnaryOperation op)
	{
		var array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.GeneralVectorUnary<Float64, PureStorage<Float64, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>>(op, array1, 1, array2, 1);
		Assert.IsTrue(success);

		double* test1 = (double*)array1.Pointer.Pointer.Pointer.ToPointer();
		double* test2 = (double*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			double real = op switch
			{
				UnaryOperation.Negate => -test1[i],
				UnaryOperation.Conjugate => Math.Abs(test1[i]),
				_ => test1[i],
			};
			ValueAssert.AreApproxEqual(real, test2[i]);
		}
	}

	[TestMethod()]
	[DataRow(UnaryOperation.Negate)]
	[DataRow(UnaryOperation.Conjugate)]
	public void GeneralVectorUnaryComplexTest(UnaryOperation op)
	{
		var _array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array1 = _array1.As<Complex<Float64>>();
		var _array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = _array2.As<Complex<Float64>>();

		bool success = api.GeneralVectorUnary<Complex<Float64>, PureStorage<Complex<Float64>, CpuMemoryPointer>, PureStorage<Complex<Float64>, CpuMemoryPointer>>(op, array1, 1, array2, 1);
		Assert.IsTrue(success);

		Complex<Float64>* test1 = (Complex<Float64>*)array1.Pointer.Pointer.Pointer.ToPointer();
		Complex<Float64>* test2 = (Complex<Float64>*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			Complex<Float64> real = op switch
			{
				UnaryOperation.Negate => -test1[i],
				UnaryOperation.Conjugate => test1[i].Conjugate,
				_ => test1[i],
			};
			ValueAssert.AreApproxEqual(real, test2[i]);
		}
	}

	[TestMethod()]
	[DataRow(BinaryScalarOperation.Add)]
	[DataRow(BinaryScalarOperation.Multiply)]
	[DataRow(BinaryScalarOperation.Power)]
	[DataRow(BinaryScalarOperation.Truncate)]
	[DataRow(BinaryScalarOperation.Fill)]
	public void GeneralVectorBinaryScalarTest(BinaryScalarOperation op)
	{
		const double SCALAR = 0.5;
		Float64 scalar = SCALAR;

		var array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.GeneralVectorBinaryScalar(op, scalar, array1, 1, array2, 1);
		Assert.IsTrue(success);

		double* test1 = (double*)array1.Pointer.Pointer.Pointer.ToPointer();
		double* test2 = (double*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			double real = op switch
			{
				BinaryScalarOperation.Add => test1[i] + SCALAR,
				BinaryScalarOperation.Multiply => test1[i] * SCALAR,
				BinaryScalarOperation.Power => Math.Pow(test1[i], SCALAR),
				BinaryScalarOperation.Fill => SCALAR,
				BinaryScalarOperation.Truncate => Math.Abs(test1[i]) <= SCALAR ? 0 : test1[i],
				_ => test1[i],
			};
			ValueAssert.AreApproxEqual(real, test2[i]);
		}
	}

	[TestMethod()]
	[DataRow(BinaryScalarOperation.Add)]
	[DataRow(BinaryScalarOperation.Multiply)]
	////[DataRow(BinaryScalarOperation.Power)]
	[DataRow(BinaryScalarOperation.Truncate)]
	[DataRow(BinaryScalarOperation.Fill)]
	public void GeneralVectorBinaryScalarComplexTest(BinaryScalarOperation op)
	{
		Complex<Float64> SCALAR = new(0.5, 0.5);

		var _array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array1 = _array1.As<Complex<Float64>>();
		var _array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = _array2.As<Complex<Float64>>();

		bool success = api.GeneralVectorBinaryScalar(op, SCALAR, array1, 1, array2, 1);
		Assert.IsTrue(success);

		Complex<Float64>* test1 = (Complex<Float64>*)array1.Pointer.Pointer.Pointer.ToPointer();
		Complex<Float64>* test2 = (Complex<Float64>*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			Complex<Float64> real = op switch
			{
				BinaryScalarOperation.Add => test1[i] + SCALAR,
				BinaryScalarOperation.Multiply => test1[i] * SCALAR,
				////BinaryScalarOperation.Power => Complex<Float64>.Pow(test1[i], SCALAR),
				BinaryScalarOperation.Fill => SCALAR,
				BinaryScalarOperation.Truncate => test1[i].Magnitude <= SCALAR.Magnitude ? default : test1[i],
				_ => test1[i],
			};
			ValueAssert.AreApproxEqual(real, test2[i]);
		}
	}

	[TestMethod()]
	[DataRow(ReduceOperation.Add)]
	[DataRow(ReduceOperation.Multiply)]
	[DataRow(ReduceOperation.AddAbsolute)]
	[DataRow(ReduceOperation.MultiplyAbsolute)]
	[DataRow(ReduceOperation.Norm)]
	[DataRow(ReduceOperation.Maximum)]
	[DataRow(ReduceOperation.Mininum)]
	[DataRow(ReduceOperation.AbsoluteMaximum)]
	[DataRow(ReduceOperation.AbsoluteMininum)]
	public void GeneralVectorReduceTest(ReduceOperation op)
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.GeneralVectorReduce(op, array, 1, out Float64 result);
		Assert.IsTrue(success);

		double real = op switch
		{
			ReduceOperation.Multiply => 1,
			ReduceOperation.Maximum => double.MinValue,
			ReduceOperation.Mininum => double.MaxValue,
			ReduceOperation.AbsoluteMininum => double.MaxValue,
			ReduceOperation.MultiplyAbsolute => 1,
			_ => 0
		};
		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			switch (op)
			{
				case ReduceOperation.Add:
					real += test[i];
					break;
				case ReduceOperation.Multiply:
					real *= test[i];
					break;
				case ReduceOperation.AddAbsolute:
					real += Math.Abs(test[i]);
					break;
				case ReduceOperation.MultiplyAbsolute:
					real *= Math.Abs(test[i]);
					break;
				case ReduceOperation.Norm:
					real += test[i] * test[i];
					break;
				case ReduceOperation.Maximum:
					real = Math.Max(real, test[i]);
					break;
				case ReduceOperation.Mininum:
					real = Math.Min(real, test[i]);
					break;
				case ReduceOperation.AbsoluteMaximum:
					real = Math.Max(real, Math.Abs(test[i]));
					break;
				case ReduceOperation.AbsoluteMininum:
					real = Math.Min(real, Math.Abs(test[i]));
					break;
				default:
					break;
			}
		}
		if (op == ReduceOperation.Norm)
			real = Math.Sqrt(real);
		ValueAssert.AreApproxEqual(real, (double)result);
	}

	[TestMethod()]
	[DataRow(ReduceOperation.Add)]
	[DataRow(ReduceOperation.Multiply)]
	[DataRow(ReduceOperation.AddAbsolute)]
	[DataRow(ReduceOperation.MultiplyAbsolute)]
	[DataRow(ReduceOperation.Norm)]
	[DataRow(ReduceOperation.AbsoluteMaximum)]
	[DataRow(ReduceOperation.AbsoluteMininum)]
	public void GeneralVectorReduceComplexTest(ReduceOperation op)
	{
		var _array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array = _array.As<Complex<Float64>>();

		bool success = api.GeneralVectorReduce(op, array, 1, out Complex<Float64> result);
		Assert.IsTrue(success);

		Complex<Float64> real = op switch
		{
			ReduceOperation.Multiply => Complex<Float64>.One,
			ReduceOperation.AbsoluteMininum => (Float64)double.MaxValue,
			ReduceOperation.MultiplyAbsolute => Complex<Float64>.One,
			_ => default
		};
		Complex<Float64>* test = (Complex<Float64>*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			switch (op)
			{
				case ReduceOperation.Add:
					real += test[i];
					break;
				case ReduceOperation.Multiply:
					real *= test[i];
					break;
				case ReduceOperation.AddAbsolute:
					real += Complex<Float64>.Abs(test[i]);
					break;
				case ReduceOperation.MultiplyAbsolute:
					real *= Complex<Float64>.Abs(test[i]);
					break;
				case ReduceOperation.Norm:
					real += test[i].Conjugate * test[i];
					break;
				case ReduceOperation.AbsoluteMaximum:
					real = Complex<Float64>.Max(real, Complex<Float64>.Abs(test[i]));
					break;
				case ReduceOperation.AbsoluteMininum:
					real = Complex<Float64>.Min(real, Complex<Float64>.Abs(test[i]));
					break;
				default:
					break;
			}
		}
		if (op == ReduceOperation.Norm)
			real = Complex<Float64>.Sqrt(real);
		ValueAssert.AreApproxEqual(real, result);
	}

	[TestMethod()]
	[DataRow(ReduceOperation.Maximum)]
	[DataRow(ReduceOperation.Mininum)]
	[DataRow(ReduceOperation.AbsoluteMaximum)]
	[DataRow(ReduceOperation.AbsoluteMininum)]
	public void GeneralVectorArgReduceTest(ReduceOperation op)
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.GeneralVectorArgReduce<Float64, PureStorage<Float64, CpuMemoryPointer>>(op, array, 1, out long result);
		Assert.IsTrue(success);

		double real = op switch
		{
			ReduceOperation.Maximum => double.MinValue,
			ReduceOperation.Mininum => double.MaxValue,
			ReduceOperation.AbsoluteMininum => double.MaxValue,
			_ => 0
		};
		long realIdx = -1;
		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			switch (op)
			{
				case ReduceOperation.Maximum:
					if (test[i] > real)
					{
						real = test[i]; realIdx = i;
					}
					break;
				case ReduceOperation.Mininum:
					if (test[i] < real)
					{
						real = test[i]; realIdx = i;
					}
					break;
				case ReduceOperation.AbsoluteMaximum:
					if (Math.Abs(test[i]) > real)
					{
						real = Math.Abs(test[i]); realIdx = i;
					}
					break;
				case ReduceOperation.AbsoluteMininum:
					if (Math.Abs(Math.Abs(test[i])) < real)
					{
						real = Math.Abs(test[i]); realIdx = i;
					}
					break;
				default:
					break;
			}
		}
		Assert.AreEqual(realIdx, result);
	}

	[TestMethod()]
	[DataRow(ReduceOperation.AbsoluteMaximum)]
	[DataRow(ReduceOperation.AbsoluteMininum)]
	public void GeneralVectorArgReduceComplexTest(ReduceOperation op)
	{
		var _array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array = _array.As<Complex<Float64>>();

		bool success = api.GeneralVectorArgReduce<Complex<Float64>, PureStorage<Complex<Float64>, CpuMemoryPointer>>(op, array, 1, out long result);
		Assert.IsTrue(success);

		double real = op switch
		{
			ReduceOperation.AbsoluteMininum => double.MaxValue,
			_ => 0
		};
		long realIdx = -1;
		Complex<Float64>* test = (Complex<Float64>*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			switch (op)
			{
				case ReduceOperation.AbsoluteMaximum:
					if (test[i].MagnitudeSquared > real)
					{
						real = test[i].MagnitudeSquared; realIdx = i;
					}
					break;
				case ReduceOperation.AbsoluteMininum:
					if (test[i].MagnitudeSquared < real)
					{
						real = test[i].MagnitudeSquared; realIdx = i;
					}
					break;
				default:
					break;
			}
		}
		Assert.AreEqual(realIdx, result);
	}

	[TestMethod()]
	[DataRow(BinaryOperation.Add)]
	[DataRow(BinaryOperation.Multiply)]
	[DataRow(BinaryOperation.Divide)]
	public void GeneralVectorsBinaryTest(BinaryOperation op)
	{
		var array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array3 = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.GeneralVectorsBinary<Float64, PureStorage<Float64, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>>(op, array1, 1, array2, 1, array3, 1);
		Assert.IsTrue(success);

		double* test1 = (double*)array1.Pointer.Pointer.Pointer.ToPointer();
		double* test2 = (double*)array2.Pointer.Pointer.Pointer.ToPointer();
		double* test3 = (double*)array3.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			double real = op switch
			{
				BinaryOperation.Add => test1[i] + test2[i],
				BinaryOperation.Multiply => test1[i] * test2[i],
				BinaryOperation.Divide => test1[i] / test2[i],
				_ => test1[i],
			};
			ValueAssert.AreApproxEqual(real, test3[i]);
		}
	}

	[TestMethod()]
	[DataRow(BinaryOperation.Add)]
	[DataRow(BinaryOperation.Multiply)]
	[DataRow(BinaryOperation.Divide)]
	public void GeneralVectorsBinaryComplexTest(BinaryOperation op)
	{
		var _array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array1 = _array1.As<Complex<Float64>>();
		var _array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = _array2.As<Complex<Float64>>();
		var _array3 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array3 = _array3.As<Complex<Float64>>();

		bool success = api.GeneralVectorsBinary<Complex<Float64>, PureStorage<Complex<Float64>, CpuMemoryPointer>, PureStorage<Complex<Float64>, CpuMemoryPointer>, PureStorage<Complex<Float64>, CpuMemoryPointer>>(op, array1, 1, array2, 1, array3, 1);
		Assert.IsTrue(success);

		Complex<Float64>* test1 = (Complex<Float64>*)array1.Pointer.Pointer.Pointer.ToPointer();
		Complex<Float64>* test2 = (Complex<Float64>*)array2.Pointer.Pointer.Pointer.ToPointer();
		Complex<Float64>* test3 = (Complex<Float64>*)array3.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			Complex<Float64> real = op switch
			{
				BinaryOperation.Add => test1[i] + test2[i],
				BinaryOperation.Multiply => test1[i] * test2[i],
				BinaryOperation.Divide => test1[i] / test2[i],
				_ => test1[i],
			};
			ValueAssert.AreApproxEqual(real, test3[i]);
		}
	}

	[TestMethod()]
	[DataRow(ReduceOperation.Add, true)]
	[DataRow(ReduceOperation.Multiply, true)]
	[DataRow(ReduceOperation.Add, false)]
	[DataRow(ReduceOperation.Multiply, false)]
	public void GeneralVectorsScanTest(ReduceOperation op, bool inclusive)
	{
		var array1 = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array2 = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.GeneralVectorsScan<Float64, PureStorage<Float64, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>>(op, array1, 1, array2, 1, inclusive);
		Assert.IsTrue(success);

		double* test1 = (double*)array1.Pointer.Pointer.Pointer.ToPointer();
		double* test2 = (double*)array2.Pointer.Pointer.Pointer.ToPointer();
		double real = op == ReduceOperation.Add ? 0 : 1;
		if (inclusive)
		{
			real = test1[0];
		}
		ValueAssert.AreApproxEqual(real, test2[0]);
		for (int i = 1; i < Math.Min(array1.Length, array2.Length); i++)
		{
			real = op switch
			{
				ReduceOperation.Add => real + test1[inclusive ? i : i - 1],
				ReduceOperation.Multiply => real * test1[inclusive ? i : i - 1],
				_ => test1[i],
			};
			ValueAssert.AreApproxEqual(real, test2[i]);
		}
	}

	[TestMethod()]
	public void GeneralVectorsCastTest()
	{
		var array1 = UnitTests.Helpers.GenerateFloatData(-10, 10);
		var array2 = UnitTests.Helpers.GenerateIntData(-1, 1);

		bool success = api.GeneralVectorsCast<Float64, SignedInt32, PureStorage<Float64, CpuMemoryPointer>, PureStorage<SignedInt32, CpuMemoryPointer>>(array1, 1, array2, 1);
		Assert.IsTrue(success);

		double* test1 = (double*)array1.Pointer.Pointer.Pointer.ToPointer();
		int* test2 = (int*)array2.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
		{
			int real = (int)test1[i];
			Assert.AreEqual(real, test2[i]);
		}
	}

	[TestMethod()]
	public void SortTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		Float64* values = stackalloc Float64[(int)array.Length];
		array.CopyToManaged(values);

		bool success = api.Sort<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1);
		Assert.IsTrue(success);

		Span<double> span = new(values, (int)array.Length);
		span.Sort();
		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			Assert.AreEqual(span[i], test[i]);
		}
	}

	[TestMethod()]
	public void SortByKeyTest()
	{
		var arrayVal = UnitTests.Helpers.GenerateFloatData(-1, 1);
		Float64* values = stackalloc Float64[(int)arrayVal.Length];
		arrayVal.CopyToManaged(values);
		var arrayKey = UnitTests.Helpers.GenerateIntData(-100, 100);
		SignedInt32* keys = stackalloc SignedInt32[(int)arrayKey.Length];
		arrayKey.CopyToManaged(keys);
		int len = (int)Math.Min(arrayKey.Length, arrayVal.Length);
		arrayVal = arrayVal.MakeReference(0, len);
		arrayKey = arrayKey.MakeReference(0, len);

		bool success = api.Sort<SignedInt32, Float64, PureStorage<SignedInt32, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>>(arrayKey, 1, arrayVal, 1);
		Assert.IsTrue(success);

		Span<int> keySpan = new(keys, len);
		Span<double> valSpan = new(values, len);
		keySpan.Sort(valSpan);
		double* testVal = (double*)arrayVal.Pointer.Pointer.Pointer.ToPointer();
		int* testKey = (int*)arrayKey.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < len; i++)
		{
			Assert.AreEqual(keySpan[i], testKey[i]);
			Assert.AreEqual(valSpan[i], testVal[i]);
		}
	}

	[TestMethod()]
	public void IndexOfTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		long real = System.Random.Shared.Next((int)array.Length);
		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();

		bool success = api.IndexOf<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1, false, test[real], out long find);
		Assert.IsTrue(success);
		Assert.AreEqual(real, find);

		api.Sort<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1);

		success = api.IndexOf<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1, false, test[real], out find);
		Assert.IsTrue(success);
		Assert.AreEqual(real, find);
	}

	[TestMethod()]
	[DataRow(true)]
	[DataRow(false)]
	public void BoundOfTest(bool lower)
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		long real = System.Random.Shared.Next((int)array.Length);
		api.Sort<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1);

		bool success = api.BoundOf<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1, test[real], lower, out long find);
		Assert.IsTrue(success);
		Assert.AreEqual(lower ? real : real + 1, find);
	}

	[TestMethod()]
	[DataRow(15.0, 0.5)]
	public void FillWithRangeTest(double start, double step)
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);

		bool success = api.FillWithRange<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1, start, step);
		Assert.IsTrue(success);

		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			ValueAssert.AreApproxEqual(start + i * step, test[i]);
		}
	}

	[TestMethod()]
	public void FillWithRangeComplexTest()
	{
		Complex<Float64> start = new(15.0, 5.0), step = new(0.5, 0.25);

		var _array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var array = _array.As<Complex<Float64>>();

		bool success = api.FillWithRange(array, 1, start, step);
		Assert.IsTrue(success);

		Complex<Float64>* test = (Complex<Float64>*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			ValueAssert.AreApproxEqual(start + i * step, test[i]);
		}
	}

	[TestMethod()]
	[DataRow(5.0)]
	public void VectorSetAllValuesAtTest(double value)
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var indexArray = UnitTests.Helpers.GenerateIntData(0, (int)array.Length).MakeReference(0, array.Length / 2);

		bool success = api.VectorSetValuesAt<Float64, SignedInt32, PureStorage<Float64, CpuMemoryPointer>, PureStorage<SignedInt32, CpuMemoryPointer>>(array, value, indexArray);
		Assert.IsTrue(success);

		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		int* testInd = (int*)indexArray.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < indexArray.Length; i++)
		{
			Assert.AreEqual(value, test[testInd[i]]);
		}
	}

	[TestMethod()]
	public void VectorSetValuesAtTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var indexArray = UnitTests.Helpers.GenerateIntData(0, (int)array.Length).MakeReference(0, 10);
		var valueArray = UnitTests.Helpers.GenerateFloatData(-5, 5).MakeReference(0, 10);

		bool success = api.VectorSetValuesAt<Float64, SignedInt32, PureStorage<Float64, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>, PureStorage<SignedInt32, CpuMemoryPointer>>(array, valueArray, indexArray);
		Assert.IsTrue(success);

		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		double* testVal = (double*)valueArray.Pointer.Pointer.Pointer.ToPointer();
		int* testInd = (int*)indexArray.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < indexArray.Length; i++)
		{
			Assert.AreEqual(testVal[i], test[testInd[i]]);
		}
	}

	[TestMethod()]
	public void VectorGatherValuesAtTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData(-1, 1);
		var indexArray = UnitTests.Helpers.GenerateIntData(0, (int)array.Length).MakeReference(0, 10);
		var valueArray = UnitTests.Helpers.GenerateFloatData(-5, 5).MakeReference(0, 10);

		bool success = api.VectorGatherValuesAt<Float64, SignedInt32, PureStorage<Float64, CpuMemoryPointer>, PureStorage<Float64, CpuMemoryPointer>, PureStorage<SignedInt32, CpuMemoryPointer>>(array, valueArray, indexArray);
		Assert.IsTrue(success);

		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		double* testVal = (double*)valueArray.Pointer.Pointer.Pointer.ToPointer();
		int* testInd = (int*)indexArray.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < indexArray.Length; i++)
		{
			Assert.AreEqual(test[testInd[i]], testVal[i]);
		}
	}

	[TestMethod()]
	public void VectorSparseToDenseTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void VectorDenseToSparseTest()
	{
		Assert.Fail();
	}

	#region already tested
	[TestMethod()]
	public void EigenStandardMatrixHermitianTest()
	{
		return;
	}

	[TestMethod()]
	public void SchurDecompositionTest()
	{
		return;
	}

	[TestMethod()]
	public void SchurReorderTest()
	{
		return;
	}

	[TestMethod()]
	public void EigenStandardMatrixGeneralTest()
	{
		return;
	}

	[TestMethod()]
	public void LinearSolveGeneralTest()
	{
		return;
	}

	[TestMethod()]
	public void QRDecompositionTest()
	{
		return;
	}

	[TestMethod()]
	public void LeastSquareSolveTest()
	{
		return;
	}
	#endregion
}