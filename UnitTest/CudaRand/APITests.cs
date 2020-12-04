using Microsoft.VisualStudio.TestTools.UnitTesting;

using CudaCSharp.Arrays;
using CudaCSharp.Memory;


namespace CudaCSharp.Rng.Tests
{
	[TestClass()]
	public class APITests
	{
		[TestMethod()]
		public void SetSeedTest()
		{
			int len = 10;
			// Arrange
			using var devPtr1 = Storage<double>.Create(len);
			// act
			API.FillWithRandom(new DenseVector<double>(devPtr1, len));
			// assert

		}
	}
}