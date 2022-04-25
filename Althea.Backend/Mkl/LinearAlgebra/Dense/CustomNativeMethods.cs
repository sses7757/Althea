using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.NativeTypes;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
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

		internal static extern CustomStatus vecProd(DataType type, long n, void* a, long stride, void* outProd);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecAbsProd(DataType type, long n, void* a, long stride, void* outProd);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecParSum(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecParProd(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);
		#endregion


		#region matrix math

		#endregion


		#region half matrix
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus triMatAdd(DataType type, bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, void* α, void* A, long lda, void* β, void* B, long ldb, void* C, long ldc);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus symmMatAdd(DataType type, bool upperA, bool upperB, bool upperC, MatrixOperation opA, MatrixOperation opB, long n, void* α, void* A, long lda, void* β, void* B, long ldb, void* C, long ldc);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus triMatMul(DataType type, bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, void* α, void* A, long lda, void* B, long ldb, void* β, void* C, long ldc);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus symmMatMul(DataType type, bool upperA, bool upperB, bool hermA, bool hermB, MatrixOperation opA, MatrixOperation opB, long n, void* α, void* A, long lda, void* B, long ldb, void* β, void* C, long ldc);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus matKron(DataType type, void* alpha, void* A, long ldA, long rowsA, long colsA, void* B, long ldB, long rowsB, long colsB, void* beta, void* C, long ldC);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus matMakeHerm(DataType type, bool upperStored, bool hermA, long n, void* A, long ld);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus matTriClear(DataType type, bool clearLower, bool clearDiag, long rows, long cols, void* A, long ld);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus matTriCopy(DataType type, bool upper, bool copyDiag, MatrixOperation opA, long m, long n, void* A, long lda, void* B, long ldb);
		#endregion


		#region half matrix math

		#endregion
	}
}
