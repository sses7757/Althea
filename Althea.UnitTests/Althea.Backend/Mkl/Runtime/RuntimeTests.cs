using Althea.Backend.Mkl;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Althea.Backend.Mkl.Tests;

[TestClass()]
public class RuntimeTests
{
	[TestMethod()]
	public void GetDriverVersionTest()
	{
		Console.WriteLine(Runtime.GetDriverVersion());
	}

	[TestMethod()]
	public void NumberOfThreadsTest()
	{
		////try
		////{
		////	Runtime.Verbose = true;
		////	Console.WriteLine(Runtime.NumberOfThreads);
		////}
		////catch (Exception e)
		////{
		////	throw;
		////}

		Runtime.NumberOfThreads = 8;
	}

	[TestMethod()]
	public void InstructionTest()
	{
		Console.WriteLine(Runtime.Instruction);

		Runtime.Instruction = Instruction.AVX2;
	}
}