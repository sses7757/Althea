using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;

namespace Althea.Backend.Cuda.LinearAlgebra.Dense;

internal enum CustomStatus : int
{
	Success = 0,
	NotSupported = -1,
}

internal static class CustomStatusExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool Check(this CustomStatus status)
	{
		if (status == CustomStatus.Success)
			return true;
		else if (status == CustomStatus.NotSupported)
			return false;
		throw new StatusException(status, new(1));
	}
}

/// <summary>
/// The actual class for custom CUDA BLAS and SOLVER library APIs
/// </summary>
public unsafe class CustomNativeMethods
{
	#region vector
	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CudaError vecStridedCopy(DataType type, long n, void* src, long strideSrc, void* dst, long strideDst);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecFillVal(DataType type, long n, void* value, void* array, long stride);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecDataConvert(DataType srcType, DataType dstType, bool toRealByAbs, long n, void* src, long strideSrc, void* dst, long strideDst);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecConj(DataType type, long n, void* a, long strideA, void* b, long strideB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecAbs(DataType type, long n, void* a, long strideA, void* b, long strideB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecClip(DataType type, long n, void* threshold, void* a, long strideA, void* b, long strideB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecAddScalar(DataType type, long n, void* scalar, void* a, long strideA, void* b, long strideB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecMulScalar(DataType type, long n, void* scalar, void* a, long strideA, void* b, long strideB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecPowScalar(DataType type, long n, void* scalar, void* a, long strideA, void* b, long strideB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecsEq(DataType type, long n, void* a, long strideA, void* b, long strideB, out bool equals);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgMin(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgMax(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgAbsMin(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgAbsMax(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecAbsSum(DataType type, long n, void* a, long stride, void* outSum);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecSum(DataType type, long n, void* a, long stride, void* outSum);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecProd(DataType type, long n, void* a, long stride, void* outProd);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecAbsProd(DataType type, long n, void* a, long stride, void* outProd);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecParSum(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecParProd(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);
	#endregion


	#region matrix math
	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matConj(DataType type, long m, long n, void* A, long ldA, void* B, long ldB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matDataConvert(DataType srcType, DataType dstType, bool toRealByAbs, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matFillVal(DataType type, long m, long n, void* value, void* array, long ld);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matClip(DataType type, long m, long n, void* threshold, void* A, long ldA, void* B, long ldB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAddScalar(DataType type, long m, long n, void* scalar, void* A, long ldA, void* B, long ldB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matMulScalar(DataType type, long m, long n, void* scalar, void* A, long ldA, void* B, long ldB);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matsEq(DataType type, long m, long n, void* A, long ldA, void* B, long ldB, out bool equals);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgMin(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgMax(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgAbsMin(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgAbsMax(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matSum(DataType type, long m, long n, void* A, long ld, void* outSum);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAbsSum(DataType type, long m, long n, void* A, long ld, void* outSum);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matProd(DataType type, long m, long n, void* A, long ld, void* outProd);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAbsProd(DataType type, long m, long n, void* A, long ld, void* outProd);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAsVecNorm(DataType type, long m, long n, void* array, long ld, void* outNorm);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsSum(DataType type, long m, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsAbsSum(DataType type, long m, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsProd(DataType type, long m, long n, void* A, long ld, void* outProds, long strideOut);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsAbsProd(DataType type, long m, long n, void* A, long ld, void* outProds, long strideOut);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsNorm(DataType type, long m, long n, void* array, long ld, void* outNorms, long strideOut);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsParSum(DataType type, bool inclusive, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsParProd(DataType type, bool inclusive, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matKron(DataType type, void* alpha, void* A, long ldA, long rowsA, long colsA, void* B, long ldB, long rowsB, long colsB, void* beta, void* C, long ldC);
	#endregion
}
