using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

using CudaCSharp.Memory;
using RT = CudaCSharp.Runtime.API;


namespace CudaCSharp.Runtime.Tests
{
	[TestClass()]
	public class APITests
	{
		[TestMethod()]
		public void ResetTest()
		{
			API.Reset();
		}

		[TestMethod()]
		public void DeviceCountTest()
		{
			var count = API.DeviceCount;
			Assert.AreEqual(1, count);
		}

		[TestMethod()]
		public void AvailableMemoryTest()
		{
			var mem = API.DeviceFreeMemory;
			Assert.AreNotEqual(0, mem);
			var mem2 = API.DeviceFreeMemory;
			Assert.AreEqual(mem, mem2);
		}

		[TestMethod()]
		public void TotalMemoryTest()
		{
			var mem = API.DeviceTotalMemory;
			Assert.AreNotEqual(0, mem);
			var mem2 = API.DeviceTotalMemory;
			Assert.AreEqual(mem, mem2);
		}

		public const int LEN = 10;

		[TestMethod()]
		public void SetValueTest()
		{
			RT.Reset(); // Arrange
			using var devPtr = Storage<byte>.Create(LEN);
			byte[] hostArr = new byte[LEN];
			// act
			devPtr.SetValue(10, LEN);
			hostArr = hostArr.Select(e => (byte)10).ToArray();
			var hostPtr = devPtr.CopyOutArray(LEN);
			// assert
			Assert.IsTrue(hostArr.SequenceEqual(hostPtr));
		}

		[TestMethod()]
		public void CopyToTest()
		{
			RT.Reset(); // Arrange
			using var devPtr = Storage<byte>.Create(LEN);
			using var devPtr2 = Storage<byte>.Create(LEN);
			// act
			devPtr.SetValue(10, LEN);
			devPtr.CopyTo(devPtr2, LEN);
			var hostPtr = devPtr.CopyOutArray(LEN);
			var hostPtr2 = devPtr2.CopyOutArray(LEN);
			// assert
			Assert.IsTrue(hostPtr.SequenceEqual(hostPtr2));
		}

		[TestMethod()]
		public void CopyMatrixToTest()
		{
			RT.Reset(); // Arrange
			int[,] hostMat1 = new int[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 }, { 10, 11, 12 } };
			int[,] hostMat2 = new int[,] { { 0, 5, 6 }, { 0, 8, 9 } };
			using var devPtr1 = Storage<int>.Create(hostMat1.Length);
			using var devPtr2 = Storage<int>.Create(hostMat2.Length);
			devPtr2.SetValue(0, hostMat2.Length);
			// act
			devPtr1.CopyIntoArray(hostMat1.ColumnTake(), hostMat1.Length);
			devPtr1.CopyMatrixTo(dest: devPtr2, hostMat1.GetLength(0), hostMat1.GetLength(1), hostMat2.GetLength(0), hostMat2.GetLength(1), offsetSouceRow: 1, offsetSouceCol: 1, offsetDestRow: 0, offsetDestCol: 1);
			int[,] hostArr = devPtr2.CopyOutMatrix(hostMat2.GetLength(0), hostMat2.GetLength(1));
			// assert
			var real = new int[hostMat2.Length];
			Buffer.BlockCopy(hostMat2, 0, real, 0, hostMat2.Length);
			var test = new int[hostArr.Length];
			Buffer.BlockCopy(hostArr, 0, test, 0, hostArr.Length);
			Assert.IsTrue(real.SequenceEqual(test));
		}

		[TestMethod()]
		public void CopyInOutTest()
		{
			RT.Reset(); // Arrange
			double valueToSet = 8.0;
			using var ptr = Storage<double>.Create(4);
			// act
			ptr.CopyInto(valueToSet, offset: 2);
			var copyout = ptr.CopyOut(offset: 2);
			// assert
			Assert.AreEqual(valueToSet, copyout);
		}

		[TestMethod()]
		public void CopyInOutArrayTest()
		{
			RT.Reset(); // Arrange
			int[] host = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			using var ptr = Storage<int>.Create(20);
			// act
			ptr.CopyIntoArray(host, length: 5, offset: 2);
			var copyout = ptr.CopyOutArray(length: 5, offset: 2);
			// assert
			Assert.IsTrue(host.Take(5).SequenceEqual(copyout));
		}

		[TestMethod()]
		public void CopyIntoColumnMajorMatrixTest()
		{
			RT.Reset(); // Arrange
			int[,] hostMat = new int[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 }, { 10, 11, 12 } };
			using var devPtr = Storage<int>.Create(2 * 2);
			// act
			devPtr.CopyIntoColumnMajorMatrix(hostMat.ColumnTake(), destLeadDim: 2, sourceLeadDim: 4, copyCols: 2, copyRows: 2);
			var copyout = devPtr.CopyOutArray(length: 2 * 2);
			// assert
			Assert.IsTrue(new int[,] { { 1, 2 }, { 4, 5 } }.ColumnTake().SequenceEqual(copyout));
		}

		[TestMethod()]
		public void CopyOutMatrixTest()
		{
			RT.Reset(); // Arrange
			int[,] hostMat = new int[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 }, { 10, 11, 12 } };
			using var devPtr = Storage<int>.Create(hostMat.Length);
			// act
			devPtr.CopyIntoArray(hostMat.ColumnTake());
			var copyout = devPtr.CopyOutMatrix(hostMat.GetLongLength(0), copyCols: hostMat.GetLongLength(1), copyRows: hostMat.GetLongLength(0));
			// assert
			Assert.IsTrue(hostMat.ColumnTake().SequenceEqual(copyout.ColumnTake()));
		}

	}
}