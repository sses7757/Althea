using Microsoft.VisualStudio.TestTools.UnitTesting;

using CudaCSharp.Arrays;


namespace CudaCSharp.Memory.Tests
{
	[TestClass]
	public class ManagerTests
	{
		[TestMethod]
		public async System.Threading.Tasks.Task FileTestAsync()
		{
			// to file
			using var mat = new DenseMatrix<double>(20, 20, onHost: false, herm: true);
			mat.FillWithIdentity();
			var file = await mat.ToFileAsync(@"D:\Works\git\tempSave\", overrideCheck: true, compress: true);

			// from file
			using var f = await file.FromFileAsync<double>();

			// same test is done in check
		}
	}
}
