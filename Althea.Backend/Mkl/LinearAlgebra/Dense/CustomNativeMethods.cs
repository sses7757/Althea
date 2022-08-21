using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense;

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

internal static unsafe class CustomNativeMethods
{
	#region vector
	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecDataConvert(DataType srcType, DataType dstType, bool toRealByAbs, long n, void* src, long strideSrc, void* dst,  long strideDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecFillVal(DataType type, long n, void* value, void* array, long stride);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecStridedCopy(DataType type, void* src, void* dst, long N, long strideSrc, long strideDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecClip(DataType type, long n, void* threshold, void* a, long strideA, void* b, long strideB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecAddScalar(DataType type, long n, void* scalar, void* a, long strideA, void* b, long strideB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecMulScalar(DataType type, long n, void* scalar, void* a, long strideA, void* b, long strideB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecsEq(DataType type, long n, void* a, long strideA, void* b, long strideB, out bool equals);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgMin(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgMax(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgAbsMin(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgAbsMax(DataType type, long n, void* a, long stride, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecAbsSum(DataType type, long n, void* a, long stride, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecSum(DataType type, long n, void* a, long stride, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecProd(DataType type, long n, void* a, long stride, void* outProd);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecAbsProd(DataType type, long n, void* a, long stride, void* outProd);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecParSum(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecParProd(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);
	#endregion


	#region matrix math
	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matConj(DataType type, long m, long n, void* A, long ldA, void* B, long ldB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matDataConvert(DataType srcType, DataType dstType, bool toRealByAbs, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matFillVal(DataType type, long m, long n, void* value, void* array, long ld);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matClip(DataType type, long m, long n, void* threshold, void* A, long ldA, void* B, long ldB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAddScalar(DataType type, long m, long n, void* scalar, void* A, long ldA, void* B, long ldB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matMulScalar(DataType type, long m, long n, void* scalar, void* A, long ldA, void* B, long ldB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matsEq(DataType type, long m, long n, void* A, long ldA, void* B, long ldB, out bool equals);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgMin(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgMax(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgAbsMin(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgAbsMax(DataType type, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matSum(DataType type, long m, long n, void* A, long ld, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAbsSum(DataType type, long m, long n, void* A, long ld, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matProd(DataType type, long m, long n, void* A, long ld, void* outProd);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAbsProd(DataType type, long m, long n, void* A, long ld, void* outProd);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matAsVecNorm(DataType type, long m, long n, void* array, long ld, void* outNorm);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsSum(DataType type, long m, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsAbsSum(DataType type, long m, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsProd(DataType type, long m, long n, void* A, long ld, void* outProds, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsAbsProd(DataType type, long m, long n, void* A, long ld, void* outProds, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsNorm(DataType type, long m, long n, void* array, long ld, void* outNorms, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsParSum(DataType type, bool inclusive, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matColsParProd(DataType type, bool inclusive, long m, long n, void* src, long ldSrc, void* dst, long ldDst);
	#endregion


	#region half matrix
	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matKron(DataType type, void* alpha, void* A, long ldA, long rowsA, long colsA, void* B, long ldB, long rowsB, long colsB, void* beta, void* C, long ldC);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatAdd(DataType type, bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, void* α, void* A, long lda, void* β, void* B, long ldb, void* C, long ldc);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatAdd(DataType type, bool upperA, bool upperB, bool upperC, MatrixOperation opA, MatrixOperation opB, long n, void* α, void* A, long lda, void* β, void* B, long ldb, void* C, long ldc);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatMul(DataType type, bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, void* α, void* A, long lda, void* B, long ldb, void* β, void* C, long ldc);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatMul(DataType type, bool upperA, bool upperB, bool hermA, bool hermB, MatrixOperation opA, MatrixOperation opB, long n, void* α, void* A, long lda, void* B, long ldb, void* β, void* C, long ldc);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matMakeHerm(DataType type, bool upperStored, bool hermA, long n, void* A, long ld);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatClear(DataType type, bool clearLower, bool clearDiag, long rows, long cols, void* A, long ld);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatMulCopy(DataType type, bool upper, bool copyDiag, MatrixOperation opA, long m, long n, void* scalar, void* A, long lda, void* B, long ldb);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatAddCopy(DataType type, bool upper, bool copyDiag, MatrixOperation opA, long m, long n, void* scalar, void* A, long lda, void* B, long ldb);
	#endregion


	#region half matrix math
	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatFillVal(DataType type, bool upper, bool ignoreDiag, long m, long n, void* value, void* A, long lda);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatDataConvert(DataType srcType, DataType dstType, bool toRealByAbs, bool upper, bool ignoreDiag, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatClip(DataType type, bool upper, bool ignoreDiag, long m, long n, void* threshold, void* A, long ldA, void* B, long ldB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatAddScalar(DataType type, bool upper, bool ignoreDiag, long m, long n, void* scalar, void* A, long ldA, void* B, long ldB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatMulScalar(DataType type, bool upper, bool ignoreDiag, long m, long n, void* scalar, void* A, long ldA, void* B, long ldB);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatsEq(DataType type, bool upper, bool ignoreDiag, long m, long n, void* A, long ldA, void* B, long ldB, out bool equals);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatArgMin(DataType type, bool upper, bool ignoreDiag, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatArgMax(DataType type, bool upper, bool ignoreDiag, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatArgAbsMin(DataType type, bool upper, bool ignoreDiag, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatArgAbsMax(DataType type, bool upper, bool ignoreDiag, long m, long n, void* A, long ld, out long index);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatSum(DataType type, bool upper, bool unitDiag, long m, long n, void* A, long ld, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatAbsSum(DataType type, bool upper, bool unitDiag, long m, long n, void* A, long ld, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatSum(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatAbsSum(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outSum);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatProd(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outProd);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatAbsProd(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outProd);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatAsVecNorm(DataType type, bool upper, bool unitDiag, long m, long n, void* array, long ld, void* outNorm);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatAsVecNorm(DataType type, bool upper, bool herm, long n, void* array, long ld, void* outNorm);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatColsSum(DataType type, bool upper, bool unitDiag, long m, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatColsAbsSum(DataType type, bool upper, bool unitDiag, long m, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatColsSum(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatColsAbsSum(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outSums, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatColsProd(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outProds, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatColsAbsProd(DataType type, bool upper, bool herm, long n, void* A, long ld, void* outProds, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatColsNorm(DataType type, bool upper, bool unitDiag, long m, long n, void* array, long ld, void* outNorms, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatColsNorm(DataType type, bool upper, bool herm, long n, void* array, long ld, void* outNorms, long strideOut);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus triMatColsParSum(DataType type, bool inclusive, bool upper, bool unitDiag, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatColsParSum(DataType type, bool inclusive, bool upper, bool herm, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus symmMatColsParProd(DataType type, bool inclusive, bool upper, bool herm, long n, void* src, long ldSrc, void* dst, long ldDst);
	#endregion
}
