using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;

namespace Althea.Backend.Mkl.LinearAlgebra.Dense;

internal enum CustomStatus : int
{
	Success = 0,
	NotSupported = -1,
	// positive ones are CudaError ones
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
	internal static extern CustomStatus vecStridedCopy(DataType type, long n, void* src, long strideSrc, void* dst, long strideDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecsEq(DataType type, long n, void* a, long strideA, void* b, long strideB, out bool eqs);


	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecUnary(DataType type, UnaryOperation op, long n, void* src, long strideSrc, void* dst, long strideDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecBinaryScalar(DataType type, BinaryScalarOperation op, void* scalar, long n, void* src, long strideSrc, void* dst, long strideDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecsBinary(DataType type, BinaryOperation op, long n, void* a, long strideA, void* b, long strideB, void* dst, long strideDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecUnaryReduce(DataType type, ReduceOperation op, long n, void* src, long strideSrc, void* result);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecArgReduce(DataType type, ReduceOperation op, long n, void* src, long strideSrc, out long result);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus vecScan(DataType type, ReduceOperation op, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);
	#endregion

	#region matrix math
	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matDataConvert(DataType srcType, DataType dstType, bool toRealByAbs, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matFillVal(DataType type, long m, long n, void* value, void* array, long ld);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matsEq(DataType type, long m, long n, void* a, long ldA, void* b, long ldB, out bool eqs);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matKron(DataType type, void* alpha, void* A, long ldA, long rowsA, long colsA, void* B, long ldB, long rowsB, long colsB, void* beta, void* C, long ldC);


	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matUnary(DataType type, UnaryOperation op, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matBinaryScalar(DataType type, BinaryScalarOperation op, void* scalar, long m, long n, void* src, long ldSrc, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matsBinary(DataType type, BinaryOperation op, long m, long n, void* a, long ldA, void* b, long ldB, void* dst, long ldDst);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matUnaryReduce(DataType type, ReduceOperation op, long m, long n, void* src, long ldSrc, void* result);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matArgReduce(DataType type, ReduceOperation op, long m, long n, void* src, long ldSrc, out long result);

	[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
	internal static extern CustomStatus matScan(DataType type, ReduceOperation op, bool inclusive, long m, long n, void* src, long ldSrc, void* dst, long ldDst);
	#endregion
}
