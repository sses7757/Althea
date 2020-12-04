using Microsoft.VisualStudio.TestTools.UnitTesting;
using CudaCSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace CudaCSharp.Tests
{
	[TestClass()]
	public class LogTests
	{
		[TestMethod()]
		public void WriteTest()
		{
			Log.Write("test");
		}
	}
}