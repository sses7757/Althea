using System.Runtime.InteropServices;

using Althea.NativeTypes;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	internal enum CustomStatus : int
	{
		Success = 0,
		NotSupported = -1,
	}

	internal static unsafe class CustomNativeMethods
	{
		#region vector
		/// <summary>
		/// Convert the <paramref name="src"/> vector of <paramref name="srcType"/> to the <paramref name="dst"/> vector of <paramref name="dstType"/>
		/// </summary>
		/// <param name="srcType">The <see cref="DataType"/> of <paramref name="src"/></param>
		/// <param name="dstType">The <see cref="DataType"/> of <paramref name="dst"/></param>
		/// <param name="toRealByAbs">If the conversion converts a complex type to a real type, whether the down grade elements be of the complexes's absolute values or their real parts</param>
		/// <param name="src">The source vector of <paramref name="srcType"/></param>
		/// <param name="dst">The destination vector of <paramref name="dstType"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="strideSrc">The spacing between consecutive elements of <paramref name="src"/></param>
		/// <param name="strideDst">The spacing between consecutive elements of <paramref name="dst"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecDataConvert(DataType srcType, DataType dstType, bool toRealByAbs, long n, void* src, long strideSrc, void* dst,  long strideDst);

		/// <summary>
		/// Fill the <paramref name="array"/> with given <paramref name="value"/> of <paramref name="type"/>
		/// </summary>
		/// <param name="type">The data type of the array and value</param>
		/// <param name="array">The array to be filled</param>
		/// <param name="value">The pointer to the value of <paramref name="type"/> to be filled</param>
		/// <param name="n">The number of elements of <paramref name="array"/>, in <paramref name="type"/></param>
		/// <param name="stride">The stride between two consecutive elements to be operated in <paramref name="array"/></param>
		/// <remarks>Strided filling reduce the performance greatly.</remarks>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecFillVal(DataType type, long n, void* value, void* array, long stride);

		/// <summary>
		/// Set the values in <paramref name="a"/> whose absolute values are less than or equal to the absolute value of <paramref name="threshold"/> to 0 and write the result to <paramref name="b"/>.
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="threshold">The pointer to the threshold used to clip the vector <paramref name="a"/></param>
		/// <param name="a">The vector to operate of <paramref name="type"/></param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="b">The vector to be overwritten of <paramref name="type"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecClip(DataType type, long n, void* threshold, void* a, long strideA, void* b, long strideB);

		/// <summary>
		/// Add all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/> and write the result to <paramref name="b"/>.
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="scalar">The pointer to the scalar to add of <paramref name="type"/></param>
		/// <param name="a">The vector to operate of <paramref name="type"/></param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="b">The vector to be overwritten of <paramref name="type"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecAddScalar(DataType type, long n, void* scalar, void* a, long strideA, void* b, long strideB);

		/// <summary>
		/// Multiplies all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/> and write the result to <paramref name="b"/>.
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="scalar">The pointer to the scalar to add of <paramref name="type"/></param>
		/// <param name="a">The vector to operate of <paramref name="type"/></param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="b">The vector to be overwritten of <paramref name="type"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecMulScalar(DataType type, long n, void* scalar, void* a, long strideA, void* b, long strideB);

		/// <summary>
		/// Check whether the two vectors <paramref name="a"/> and <paramref name="b"/> are element-wise equal
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> and <paramref name="b"/></param>
		/// <param name="a">The first vector to compare of <paramref name="type"/></param>
		/// <param name="b">The second vector to compare of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		/// <param name="equals">Output the two vectors are element-wise equal or not</param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecsEq(DataType type, long n, void* a, long strideA, void* b, long strideB, out bool equals);

		/// <summary>
		/// Get the index of the element with minimum value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="index">Output the index of the element in <paramref name="type"/> compared to <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecArgMin(DataType type, long n, void* a, long stride, out long index);

		/// <summary>
		/// Get the index of the element with maximum value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="index">Output the index of the element in <paramref name="type"/> compared to <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecArgMax(DataType type, long n, void* a, long stride, out long index);

		/// <summary>
		/// Get the index of the element with minimum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="index">Output the index of the element in <paramref name="type"/> compared to <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecArgAbsMin(DataType type, long n, void* a, long stride, out long index);

		/// <summary>
		/// Get the index of the element with maximum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="index">Output the index of the element in <paramref name="type"/> compared to <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecArgAbsMax(DataType type, long n, void* a, long stride, out long index);

		/// <summary>
		/// Sums all the elements's absolute values in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outSum">The output sum as a pointer of <paramref name="type"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecAbsSum(DataType type, long n, void* a, long stride, void* outSum);

		/// <summary>
		/// Sums all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outSum">The output sum as a pointer of <paramref name="type"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecSum(DataType type, long n, void* a, long stride, void* outSum);

		/// <summary>
		/// Multiplies all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be multiplied of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outProd">The output product as a pointer of <paramref name="type"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecProd(DataType type, long n, void* a, long stride, void* outProd);

		/// <summary>
		/// Multiplies all the absolute values of elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be multiplied of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outProd">The output product as a pointer of <paramref name="type"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecAbsProd(DataType type, long n, void* a, long stride, void* outProd);

		/// <summary>
		/// Performs the partial sum from vector <paramref name="src"/> to vector <paramref name="dst"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="src"/> and <paramref name="dst"/></param>
		/// <param name="src">The source vector of <paramref name="type"/></param>
		/// <param name="dst">The destination vector of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="inclusive">Perform inclusive (the first element is <paramref name="src"/>[0]) or exclusive (the first element is 0)</param>
		/// <param name="strideSrc">The spacing between consecutive elements of <paramref name="src"/></param>
		/// <param name="strideDst">The spacing between consecutive elements of <paramref name="dst"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecParSum(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);

		/// <summary>
		/// Performs the partial sum from vector <paramref name="src"/> to vector <paramref name="dst"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="src"/> and <paramref name="dst"/></param>
		/// <param name="src">The source vector of <paramref name="type"/></param>
		/// <param name="dst">The destination vector of <paramref name="type"/></param>
		/// <param name="n">The number of elements to be operated</param>
		/// <param name="inclusive">Perform inclusive (the first element is <paramref name="src"/>[0]) or exclusive (the first element is 1)</param>
		/// <param name="strideSrc">The spacing between consecutive elements of <paramref name="src"/></param>
		/// <param name="strideDst">The spacing between consecutive elements of <paramref name="dst"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus vecParProd(DataType type, bool inclusive, long n, void* src, long strideSrc, void* dst, long strideDst);
		#endregion

		#region matrix

		#endregion

		#region matrix supplement
		/// <summary>
		/// Performs the Kronecker product of matrix <paramref name="A"/> and <paramref name="B"/> and add the result to <paramref name="C"/> in-place
		/// </summary>
		/// <param name="type">The data type of all arrays and scalars</param>
		/// <param name="A">The input left matrix</param>
		/// <param name="ldA">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rowsA"/></param>
		/// <param name="rowsA">The number of rows of <paramref name="A"/></param>
		/// <param name="colsA">The number of columns of <paramref name="A"/></param>
		/// <param name="B">The input right matrix</param>
		/// <param name="ldB">The leading dimension of <paramref name="B"/>, must be at least <paramref name="rowsB"/></param>
		/// <param name="rowsB">The number of rows of <paramref name="B"/></param>
		/// <param name="colsB">The number of columns of <paramref name="B"/></param>
		/// <param name="C">The destination matrix</param>
		/// <param name="ldC">The leading dimension of <paramref name="C"/></param>
		/// <param name="alpha">The scalar to multiply to <paramref name="A"/> or <paramref name="B"/>'s elements during the computation</param>
		/// <param name="beta">The scalar to multiply to <paramref name="C"/>'s elements during the computation</param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus matKron(DataType type, void* alpha, void* A, long ldA, long rowsA, long colsA, void* B, long ldB, long rowsB, long colsB, void* beta, void* C, long ldC);

		/// <summary>
		/// Makes the matrix <paramref name="A"/> hermitian or symmetric by copying its upper part to/from its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="upperStored">Whether <paramref name="A"/>'s upper part or its lower part is stored</param>
		/// <param name="hermA">If <paramref name="type"/> is a complex type, make <paramref name="A"/> hermitian or symmetric</param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus matMakeHerm(DataType type, bool upperStored, bool hermA, long n, void* A, long ld);

		/// <summary>
		/// Clear (set to 0) the matrix <paramref name="A"/>'s upper part or its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rows"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/></param>
		/// <param name="clearLower">Whether <paramref name="A"/>'s upper part or its lower part shall be preserved</param>
		/// <param name="clearDiag">Whether <paramref name="A"/>'s diagonal elements shall be cleared or preserved</param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern CustomStatus matTriClear(DataType type, bool clearLower, bool clearDiag, long rows, long cols, void* A, long ld);
		#endregion
	}
}
