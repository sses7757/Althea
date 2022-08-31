using System;

using Althea.Array;
using Althea.Helpers;
using Althea.Numerics;
using Althea.Random;
using Althea.Storage;

using CpuMem = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Storage.CpuMemoryPointer>;
using GpuMem = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>>;
using GpuMemInt = Althea.Storage.PureStorage<Althea.Numerics.SignedInt32, Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>>;


namespace Althea.UnitTests;

internal static class BasicDemonstration
{
	static BasicDemonstration()
	{
		// load all default backends
		Settings.Initialize();
	}

	public static void SumMatrixRowsGpuSimple()
	{
		// create a storage that occupies 1024 * Float64.Size bytes on GPU0, 1024 can be a runtime variable
		var s1 = GpuMem.Create(1024);
		// fill vector with ones, 1.0 can be a runtime variable, and if it is composed of same bytes, fast initialization will be used
		using var vector = new DenseVector<Float64, GpuMem>(s1, s1.Length);
		vector.FillWith(1.0);

		var s2 = GpuMem.Create(1024 * 1024);
		// a standard normal distribution struct on stack
		var dist = new NormalDistribution<Float64>();
		// fill matrix with random values generated from `dist`
		// since fill with random is not well defined in all arrays, no easy access point is defined
		Random.ApiSelector.FillWithRandom<Float64, GpuMem, NormalDistribution<Float64>>(s2, dist);
		using var matrix = new DenseMatrix<Float64, GpuMem>(s2, 1024, 1024);

		// Multiply `matrix` and `vector` and create a new storage and vector to store the result
		// Since `GpuMem` is used, the underlying API selector will automatically select CUDA backend
		// If there were OpenGL backend as well, the one being set more recently will be selected
		using var sumRows = matrix * vector;

		// print the contents of the resulting vector to console
		Console.WriteLine(sumRows.Print());
	}

	public static void SumMatrixRowsCpuSimple()
	{
		var s1 = CpuMem.Create(1024);
		using var vector = new DenseVector<Float64, CpuMem>(s1, s1.Length);
		vector.FillWith(1.0);

		var s2 = CpuMem.Create(1024 * 1024);
		var dist = new NormalDistribution<Float64>();
		Random.ApiSelector.FillWithRandom<Float64, CpuMem, NormalDistribution<Float64>>(s2, dist);
		using var matrix = new DenseMatrix<Float64, CpuMem>(s2, 1024, 1024);

		// since `CpuMem` is used, the underlying API selector will automatically select MKL backend
		// If there were OpenCL backend as well, the one being set more recently will be selected
		using var sumRows = matrix * vector;

		Console.WriteLine(sumRows.Print());
	}

	public static void SumMatrixRowsGpuNoWrapper()
	{
		// when out of scope, using variables' Dispose() method will be automatically invoked
		using var s1 = GpuMem.Create(1024);
		s1.FillWith((Float64)1.0);
		using var s2 = GpuMem.Create(1024 * 1024);
		var dist = new NormalDistribution<Float64>();
		Random.ApiSelector.FillWithRandom<Float64, GpuMem, NormalDistribution<Float64>>(s2, dist);
		using var sumRows = GpuMem.CreateAlike(s1);
		LinearAlgebra.Dense.BlasApiSelector.GeneralMatrixMultiplyVector(LinearAlgebra.MatrixOperation.None, 1024, 1024, (Float64)1.0, s2, 1024, s1, 1, (Float64)0.0, sumRows, 1);
		Console.WriteLine(string.Join(Environment.NewLine, sumRows));
	}

	public static void MultiplySubmatries()
	{
		using IBaseMatrix<Float64, DenseMatrix<Float64, GpuMem>> matrix1 = new DenseMatrix<Float64, GpuMem>(GpuMem.Create(1024 * 1024), 1024, 1024);
		using IBaseMatrix<Float64, DenseMatrix<Float64, GpuMem>> matrix2 = new DenseMatrix<Float64, GpuMem>(GpuMem.Create(1024 * 10), 1024, 10);

		// fill values ...

		// `sub`, `temp` and `vector` are new DenseMatrix/DenseVector with REF storages on GPU
		using var sub = matrix1[256..768, 256..768];
		using var temp = matrix2[512..1024, 2..3];
		using var vector = new DenseVector<Float64, GpuMem>(temp.Storage, temp.Storage.Length);

		// as above, `result` is a new vector with NEW storage on GPU
		using var result = sub * vector;
		Console.WriteLine(result.Print());
	}

	public static void ContractSubtensors()
	{
		Span<long> size1 = stackalloc long[] { 32, 32, 16, 16 };
		Span<long> size2 = stackalloc long[] { 16, 16, 16, 16 };
		using IBaseTensor<Float64, DenseTensor<Float64, GpuMem>> tensor1 = new DenseTensor<Float64, GpuMem>(GpuMem.Create(size1.Prod()), size1, size1);
		using IBaseTensor<Float64, DenseTensor<Float64, GpuMem>> tensor2 = new DenseTensor<Float64, GpuMem>(GpuMem.Create(size2.Prod()), size2, size2);

		// fill values ...

		// `sub` and `sub2` are new DenseTensor with REF storages on GPU
		using var sub1 = tensor1[16.., 16.., ..8, ..8];
		using var sub2 = tensor2[8.., 8.., .., ..];
		// set tensor labels for contraction
		sub1.SetLabels('a', 'b', 'c', 'd');
		sub2.SetLabels('c', 'd', 'e', 'f');

		// as above, `result` is a new tensor with NEW storage on GPU
		using var result = sub1 * sub2;
		Console.WriteLine(result.Print());
	}

	public static void MultiplySparseMatrixWithVector()
	{
		// create a new COO format (column major) sparse matrix
		var sValues = GpuMem.Create(1024);
		var sRowIdx = GpuMemInt.Create(1024);
		var sColIdx = GpuMemInt.Create(1024);
		using var cooMatrix = new CoordinateSparseMatrix<Float64, SignedInt32, GpuMem, GpuMemInt>(false, 1024, 1024, sValues, sRowIdx, sColIdx, nnz: 0);

		// sparse matrix is MUTABLE, which is convenient for adding values
		// However, it is relatively slow to add values one by one, and therefore not recommended
		cooMatrix[10, 20] = 56.0;
		// add more values or fill storages ...

		var sVector = GpuMem.Create(1024);
		using var vector = new DenseVector<Float64, GpuMem>(sVector, sVector.Length);

		// automatically invoke cuSPARSE to compute dense vector sparse matrix multiplication
		using var result = vector * cooMatrix;
	}

	public static void MultiplySparseMatrices()
	{
		// create a new CSR format sparse matrix
		var sValues = GpuMem.Create(1024);
		var sRowIdx = GpuMemInt.Create(1024 + 1);
		var sColIdx = GpuMemInt.Create(1024);
		using var csrMatrix = new CompressSparseMatrix<Float64, SignedInt32, GpuMem, GpuMemInt>(true, 1024, 1024, sValues, sRowIdx, sColIdx, nnz: 0);

		csrMatrix[10, 20] = 56.0;
		// add more values or fill storages ...

		// automatically invoke cuSPARSE to compute sparse matrices multiplication with result being a NEW sparse matrix
		using var result = csrMatrix * csrMatrix;
	}

	public static void SolveMatrix()
	{
		var s1 = CpuMem.Create(1024 * 1024);
		var s2 = CpuMem.Create(1024 * 1024);
		var s3 = CpuMem.Create(1024);
		var dist = new NormalDistribution<Float64>();
		Random.ApiSelector.FillWithRandom<Float64, CpuMem, NormalDistribution<Float64>>(s1, dist);
		using var matrix = new DenseMatrix<Float64, CpuMem>(s1, 1024, 1024);
		using var eigvecs = new DenseMatrix<Float64, CpuMem>(s2, 1024, 1024);
		using var eigvals = new DenseVector<Float64, CpuMem>(s3, 1024);

		// this operation makes matrix symmetric
		DenseOperation<Float64, CpuMem>.AddMatrices(matrix, 0.5, matrix, 0.5, matrix, LinearAlgebra.MatrixOperation.Transpose);

		using var symm = new SymmetricMatrix<Float64, CpuMem>(true, matrix.Storage, 1024);

		// automatically invoke MKL symmetric eigen-solver
		// If not present, the C# implementation will be invoked
		DenseSolvers<Float64, CpuMem>.StandardEigenSolve(symm, eigvals, null, eigvecs, null);
	}
}

internal static class AdvancedDemonstration
{

}