// self defined macro
#include "blasSupp.h"

////#include "C:\Program Files\NVIDIA GPU Computing Toolkit\cuTENSOR\v1.2\include\cutensor.h"

#pragma region test
////#include "cuda_fp16.hpp"
////#include "cuda_fp16.h"
////#include "cuda_bf16.h"
////#include "cuda_bf16.hpp"
////void GetHalf()
////{
////	__half a = __float2half(0.5f);
////	__half b = __double2half(0.5);
////	nv_bfloat16 c = __float2bfloat16(0.5f);
////}
#include "mkl.h"

void Test()
{
	
}
#pragma endregion



#pragma region element-wise multiply and divide
auto counting0 = thrust::make_counting_iterator((size_t)0);

template<typename T, bool multiply, bool hasStrideSrc, bool hasStrideDst>
struct foreachMulDiv_functor
{
	T* a;
	const T* b;
	const unsigned int strideA, strideB;

	foreachMulDiv_functor(void* a, const void* b, const unsigned int strideA, const unsigned int strideB) :
		a((T*)a), b((const T*)b),
		strideA(strideA), strideB(strideB) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideA, indB = ind * strideB;
			if constexpr (multiply)
				a[indA] = a[indA] * b[indB];
			else
				a[indA] = a[indA] / b[indB];
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideA;
			if constexpr (multiply)
				a[indA] = a[indA] * b[ind];
			else
				a[indA] = a[indA] / b[ind];
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideB;
			if constexpr (multiply)
				a[ind] = a[ind] * b[indB];
			else
				a[ind] = a[ind] / b[indB];
		}
		else
		{
			if constexpr (multiply)
				a[ind] = a[ind] * b[ind];
			else
				a[ind] = a[ind] / b[ind];
		}
	}
};

template<typename T>
inline int vectorsElementWiseMultiplyDivide(void* av, const void* bv, const size_t N, const unsigned int sa, const unsigned int sb, bool multiply)
{
	if (sa == 1 && sb == 1)
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, true, false, false>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, false, false, false>(av, bv, sa, sb));
	}
	else if(sa == 1 && sb != 1)
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, true, false, true>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, false, false, true>(av, bv, sa, sb));
	}
	else if (sa != 1 && sb == 1)
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, true, true, false>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, false, true, false>(av, bv, sa, sb));
	}
	else
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, true, true, true>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachMulDiv_functor<T, false, true, true>(av, bv, sa, sb));
	}
	return 0;
}

DLLEXP
int vecsMulDiv(const Datatype::DataType type, void* a, const void* b, const size_t N, const unsigned int strideA, const unsigned int strideB, bool multiply)
{
	AUTO_ALLTYPE_FUNC(vectorsElementWiseMultiplyDivide, type, int, a, b, N, strideA, strideB, multiply);
}
#pragma endregion


#pragma region vectors add
template<typename T, bool hasStrideSrc, bool hasStrideDst, bool hasScalar>
struct foreachAddTwo_functor
{
	const T* a;
	T* b;
	const T scalar;
	const unsigned int strideA, strideB;

	foreachAddTwo_functor(const void* a, void* b, const void* scalar, const unsigned int strideA, const unsigned int strideB) :
		a((const T*)a), b((T*)b),
		scalar(*(const T*)scalar),
		strideA(strideA), strideB(strideB) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideA, indB = ind * strideB;
			if constexpr (hasScalar)
				b[indB] = std::fma(a[indA], scalar, b[indB]);
			else
				b[indB] = a[indA] + b[indB];
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideA;
			if constexpr (hasScalar)
				b[ind] = std::fma(a[indA], scalar, b[ind]);
			else
				b[ind] = a[indA] + b[ind];
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideB;
			if constexpr (hasScalar)
				b[indB] = std::fma(a[ind], scalar, b[indB]);
			else
				b[indB] = a[ind] + b[indB];
		}
		else
		{
			if constexpr (hasScalar)
				b[ind] = std::fma(a[ind], scalar, b[ind]);
			else
				b[ind] = a[ind] + b[ind];
		}
	}
};

template<typename T>
inline int vectorsGeneralAdd(const void* scalar, const void* av, void* bv, const size_t N, const unsigned int sa, const unsigned int sb)
{
	bool scalarNotOne = T(1) != *(const T*)scalar;
	if (sa == 1 && sb == 1)
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, false, false, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, false, false, false>(av, bv, scalar, sa, sb));
	}
	else if (sa == 1 && sb != 1)
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, false, true, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, false, true, false>(av, bv, scalar, sa, sb));
	}
	else if (sa != 1 && sb == 1)
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, true, false, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, true, false, false>(av, bv, scalar, sa, sb));
	}
	else
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, true, true, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachAddTwo_functor<T, true, true, false>(av, bv, scalar, sa, sb));
	}
	return 0;
}

DLLEXP
int vecsAdd(const Datatype::DataType type, const void* scalar, const void* a, void* b, const size_t N, const unsigned int strideA, const unsigned int strideB)
{
	AUTO_ALLTYPE_FUNC(vectorsGeneralAdd, type, int, scalar, a, b, N, strideA, strideB);
}
#pragma endregion


#pragma region vectors equal
template<typename T>
inline bool vectorsEqual(const void* av, const void* bv, const size_t N, const unsigned int sa, const unsigned int sb)
{
	if (av == bv && sa == sb)
		return true;
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	if (sa == 1 && sb == 1)
	{
		return thrust::equal(THRUST_PAR, a, a + N, b);
	}
	else if (sa == 1 && sb != 1)
	{
		auto strideB = make_strided_range(b, N, sb);
		return thrust::equal(THRUST_PAR, a, a + N, strideB.begin());
	}
	else if (sa != 1 && sb == 1)
	{
		auto strideA = make_strided_range(a, N, sa);
		return thrust::equal(THRUST_PAR, strideA.begin(), strideA.end(), b);
	}
	else
	{
		auto strideA = make_strided_range(a, N, sa);
		auto strideB = make_strided_range(b, N, sb);
		return thrust::equal(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin());
	}
	// fake return for NVCC
	return false;
}

DLLEXP
bool vecsEq(const Datatype::DataType type, const void* a, const void* b, const size_t N, const unsigned int strideA, const unsigned int strideB)
{
	AUTO_ALLTYPE_FUNC(vectorsEqual, type, bool, a, b, N, strideA, strideB);
}
#pragma endregion


#pragma region element-wise power
template<typename T, typename U>
struct foreachPower_functor
{
	T* a;
	const U p;

	foreachPower_functor(void* a, const void* p) : a((T*)a), p(*(const U*)p) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = std::pow(a[ind], p);
	}
};
template<typename T, typename U>
struct foreachPower_strided_functor
{
	T* a;
	const U p;
	const unsigned int stride;

	foreachPower_strided_functor(void* a, const void* p, const unsigned int stride) : a((T*)a), p(*(const U*)p), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = std::pow(a[indA], p);
	}
};

template<typename T>
inline int vectorElementWisePowerSameType(void* av, const void* pv, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachPower_functor<T, T>(av, pv));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachPower_strided_functor<T, T>(av, pv, stride));
	}
	return 0;
}

template<typename T>
inline int vectorElementWiseRealPower(void* av, const void* pv, const size_t N, const unsigned int stride)
{
	using realT = typename T::value_type;
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachPower_functor<T, realT>(av, pv));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachPower_strided_functor<T, realT>(av, pv, stride));
	}
	return 0;
}

DLLEXP
int vecPowSameType(const Datatype::DataType type, void* a, const void* p, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorElementWisePowerSameType, type, int, a, p, N, stride);
}

DLLEXP
int vecPowRealType(const Datatype::DataType type, void* a, const void* p, const size_t N, const unsigned int stride)
{
	AUTO_COMPLEX_TYPE_FUNC(vectorElementWiseRealPower, type, int, a, p, N, stride);
}
#pragma endregion


#pragma region fill array with value
template<typename T>
struct foreachFill_strided_functor
{
	T* a;
	const T val;
	const unsigned int stride;

	foreachFill_strided_functor(void* a, const void* val, const unsigned int stride) : a((T*)a), val(*(const T*)val), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind * stride] = val;
	}
};
template<typename T>
struct foreachFill_functor
{
	T* a;
	const T val;

	foreachFill_functor(void* a, const void* val) : a((T*)a), val(*(const T*)val) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = val;
	}
};

template<typename T>
inline int vectorFillWith(void* av, const void* valv, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachFill_functor<T>(av, valv));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachFill_strided_functor<T>(av, valv, stride));
	}
	return 0;
}

DLLEXP
int vecFillVal(const Datatype::DataType type, void* a, const void* val, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorFillWith, type, int, a, val, N, stride);
}
#pragma endregion


#pragma region array conjugate
template<typename T>
struct foreachConj_strided_functor
{
	T* a;
	const unsigned int stride;

	foreachConj_strided_functor(void* a, const unsigned int stride) : a((T*)a), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = std::conj(a[indA]);
	}
};
template<typename T>
struct foreachConj_functor
{
	T* a;

	foreachConj_functor(void* a) : a((T*)a) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = std::conj(a[ind]);
	}
};

template<typename T>
inline int vecConjugate(void* av, const size_t N, const unsigned int stride)
{
	if constexpr (std::is_scalar<T>::value)
		return;
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachConj_functor<T>(av));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachConj_strided_functor<T>(av, stride));
	}
	return 0;
}

DLLEXP
int vecConj(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride)
{
	AUTO_SIGNED_TYPE_FUNC(vecConjugate, type, int, a, N, stride);
}
#pragma endregion


#pragma region strided copy
template<typename T, bool hasStrideSrc, bool hasStrideDst>
struct foreachCopy_functor
{
	const T* src;
	T* dst;
	const unsigned int strideSrc, strideDst;

	foreachCopy_functor(const void* src, void* dst, const unsigned int strideSrc, const unsigned int strideDst) : src((const T*)src), dst((T*)dst), strideSrc(strideSrc), strideDst(strideDst) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideSrc, indB = ind * strideDst;
			dst[indB] = src[indA];
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideSrc;
			dst[ind] = src[indA];
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideDst;
			dst[indB] = src[ind];
		}
		else
		{
			dst[ind] = src[ind];
		}
	}
};

template <typename T>
inline ERROR_RETURN vectorStridedCopy(const void* srcv, void* dstv, const size_t N, const unsigned int strideSrc, const unsigned int strideDst)
{
	if (strideSrc == 1 && strideDst == 1)
	{
#ifdef CPU
		return memcpy(dstv, srcv, N * sizeof(T));
#else
		return cudaMemcpy(dstv, srcv, N * sizeof(T), cudaMemcpyDeviceToDevice);
#endif // CPU
	}
	else if (strideSrc == 1 && strideDst != 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachCopy_functor<T, false, true>(srcv, dstv, strideSrc, strideDst));
	}
	else if (strideSrc != 1 && strideDst == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachCopy_functor<T, true, false>(srcv, dstv, strideSrc, strideDst));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachCopy_functor<T, true, true>(srcv, dstv, strideSrc, strideDst));
	}
	return ERROR_RETURN();
}

DLLEXP
ERROR_RETURN vecStridedCopy(const Datatype::DataType type, const void* src, void* dst, const size_t N, const unsigned int strideSrc, const unsigned int strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorStridedCopy, type, ERROR_RETURN, src, dst, N, strideSrc, strideDst);
}
#pragma endregion


#pragma region data type cast
template <typename RealIn, typename RealOut, bool hasStrideSrc, bool hasStrideDst>
struct realTypeConvert_functor
{
	const RealIn* src;
	RealOut* dst;
	const unsigned int strideSrc, strideDst;

	realTypeConvert_functor(const void* src, void* dst, const unsigned int strideSrc, const unsigned int strideDst) :
		src((const RealIn*)src),
		dst((RealOut*)dst),
		strideSrc(strideSrc), strideDst(strideDst) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideSrc, indB = ind * strideDst;
			dst[indB] = (RealOut)src[indA];
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideSrc;
			dst[ind] = (RealOut)src[indA];
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideDst;
			dst[indB] = (RealOut)src[ind];
		}
		else
		{
			dst[ind] = (RealOut)src[ind];
		}
	}
};
template <typename RealIn, typename RealOut, bool hasStrideSrc, bool hasStrideDst>
struct realToComplex_functor
{
	const RealIn* src;
	BlasSupp::complex<RealOut>* dst;
	const unsigned int strideSrc, strideDst;

	realToComplex_functor(const void* src, void* dst, const unsigned int strideSrc, const unsigned int strideDst) :
		src((const RealIn*)src),
		dst((BlasSupp::complex<RealOut>*)dst),
		strideSrc(strideSrc), strideDst(strideDst) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideSrc, indB = ind * strideDst;
			dst[indB] = BlasSupp::complex<RealOut>((RealOut)src[indA]);
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideSrc;
			dst[ind] = BlasSupp::complex<RealOut>((RealOut)src[indA]);
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideDst;
			dst[indB] = BlasSupp::complex<RealOut>((RealOut)src[ind]);
		}
		else
		{
			dst[ind] = BlasSupp::complex<RealOut>((RealOut)src[ind]);
		}
	}
};
template <typename RealIn, typename RealOut, bool hasStrideSrc, bool hasStrideDst, bool byAbs>
struct complexToReal_functor
{
	const BlasSupp::complex<RealIn>* src;
	RealOut* dst;
	const unsigned int strideSrc, strideDst;

	complexToReal_functor(const void* src, void* dst, const unsigned int strideSrc, const unsigned int strideDst) :
		src((const BlasSupp::complex<RealIn>*)src),
		dst((RealOut*)dst),
		strideSrc(strideSrc), strideDst(strideDst) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideSrc, indB = ind * strideDst;
			if constexpr (byAbs)
				dst[indB] = (RealOut)std::abs(src[indA]);
			else
				dst[indB] = (RealOut)src[indA].real();
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideSrc;
			if constexpr (byAbs)
				dst[ind] = (RealOut)std::abs(src[indA]);
			else
				dst[ind] = (RealOut)src[indA].real();
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideDst;
			if constexpr (byAbs)
				dst[indB] = (RealOut)std::abs(src[ind]);
			else
				dst[indB] = (RealOut)src[ind].real();
		}
		else
		{
			if constexpr (byAbs)
				dst[ind] = (RealOut)std::abs(src[ind]);
			else
				dst[ind] = (RealOut)src[ind].real();
		}
	}
};

template <typename RealIn, typename RealOut>
inline ERROR_RETURN vectorComplexToReal(const void* srcv, void* dstv, const size_t N, const unsigned int s1, const unsigned int s2, bool toRealByAbs)
{
	if (s1 == 1 && s2 == 1)
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, false, false, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, false, false, false>(srcv, dstv, s1, s2));
	}
	else if (s1 == 1 && s2 != 1)
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, false, true, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, false, true, false>(srcv, dstv, s1, s2));
	}
	else if (s1 != 1 && s2 == 1)
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, true, false, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, true, false, false>(srcv, dstv, s1, s2));
	}
	else
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, true, true, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, N, complexToReal_functor<RealIn, RealOut, true, true, false>(srcv, dstv, s1, s2));
	}
	return ERROR_RETURN();
}

template <typename RealIn, typename RealOut>
inline ERROR_RETURN vectorRealToComplex(const void* srcv, void* dstv, const size_t N, const unsigned int s1, const unsigned int s2, const bool toRealByAbs)
{
	if (s1 == 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realToComplex_functor<RealIn, RealOut, false, false>(srcv, dstv, s1, s2));
	}
	else if(s1 == 1 && s2 != 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realToComplex_functor<RealIn, RealOut, false, true>(srcv, dstv, s1, s2));
	}
	else if (s1 != 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realToComplex_functor<RealIn, RealOut, true, false>(srcv, dstv, s1, s2));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realToComplex_functor<RealIn, RealOut, true, true>(srcv, dstv, s1, s2));
	}
	return ERROR_RETURN();
}

template <typename RealIn, typename RealOut>
inline ERROR_RETURN vectorRealConvert(const void* srcv, void* dstv, const size_t N, const unsigned int s1, const unsigned int s2, const bool toRealByAbs)
{
	if (s1 == 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realTypeConvert_functor<RealIn, RealOut, false, false>(srcv, dstv, s1, s2));
	}
	else if (s1 == 1 && s2 != 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realTypeConvert_functor<RealIn, RealOut, false, true>(srcv, dstv, s1, s2));
	}
	else if (s1 != 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realTypeConvert_functor<RealIn, RealOut, true, false>(srcv, dstv, s1, s2));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, realTypeConvert_functor<RealIn, RealOut, true, true>(srcv, dstv, s1, s2));
	}
	return ERROR_RETURN();
}

DLLEXP
ERROR_RETURN vecDataConvert(const Datatype::DataType srcType, const Datatype::DataType dstType, const void* src, void* dst, const size_t N, const unsigned int strideSrc, const unsigned int strideDst, const bool toRealByAbs)
{
	// copy if no data conversion
	if (srcType == dstType)
	{
		AUTO_ALLTYPE_FUNC(vectorStridedCopy, srcType, ERROR_RETURN, src, dst, N, strideSrc, strideDst);
		// return is inside the auto generated switch
	}

// define inner switch
#ifndef HAS_LDBL
#define CONVERT_INNER_SWITCH(type, convert) do { \
		switch (dstType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			convertFunc = convert<type, float>; break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			convertFunc = convert<type, double>; break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			convertFunc = convert<type, char>; break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			convertFunc = convert<type, short>; break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			convertFunc = convert<type, int>; break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			convertFunc = convert<type, long long>; break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			convertFunc = convert<type, unsigned char>; break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			convertFunc = convert<type, unsigned short>; break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			convertFunc = convert<type, unsigned int>; break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			convertFunc = convert<type, unsigned long long>; break; \
		default: \
			UNSUPPORT(vecDataConvert, dstType, ERROR_RETURN); \
		} \
	} while (0)
#else
#define CONVERT_INNER_SWITCH(type, convert) do { \
		switch (dstType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			convertFunc = convert<type, float>; break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			convertFunc = convert<type, double>; break; \
		case Datatype::RealLongDouble: \
		case Datatype::ComplexLongDouble: \
			convertFunc = convert<type, long double>; break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			convertFunc = convert<type, char>; break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			convertFunc = convert<type, short>; break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			convertFunc = convert<type, int>; break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			convertFunc = convert<type, long long>; break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			convertFunc = convert<type, unsigned char>; break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			convertFunc = convert<type, unsigned short>; break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			convertFunc = convert<type, unsigned int>; break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			convertFunc = convert<type, unsigned long long>; break; \
		default: \
			UNSUPPORT(vecDataConvert, dstType, ERROR_RETURN); \
		} \
	} while (0)
#endif

// define outer switch
#ifndef HAS_LDBL
#define CONVERT_OUTER_SWITCH(convert) do { \
		switch (srcType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			CONVERT_INNER_SWITCH(float, convert); break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			CONVERT_INNER_SWITCH(double, convert); break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			CONVERT_INNER_SWITCH(char, convert); break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		default: \
			UNSUPPORT(vecDataConvert, srcType, ERROR_RETURN); \
		} \
	} while (0)
#else
#define CONVERT_OUTER_SWITCH(convert) do { \
		switch (srcType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			CONVERT_INNER_SWITCH(float, convert); break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			CONVERT_INNER_SWITCH(double, convert); break; \
		case Datatype::RealLongDouble: \
		case Datatype::ComplexLongDouble: \
			CONVERT_INNER_SWITCH(long double, convert); break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			CONVERT_INNER_SWITCH(char, convert); break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		default: \
			UNSUPPORT(vecDataConvert, srcType, ERROR_RETURN); \
		} \
	} while (0)
#endif

	// the convert function
	ERROR_RETURN (*convertFunc)(const void* src, void* dst, const size_t N, const unsigned int strideSrc, const unsigned int strideDst, const bool toRealByAbs);
	if (Datatype::isreal(srcType) && Datatype::isreal(dstType))
	{	// real convert
		CONVERT_OUTER_SWITCH(vectorRealConvert);
	}
	else if (Datatype::isreal(srcType))
	{	// real to complex
		CONVERT_OUTER_SWITCH(vectorRealToComplex);
	}
	else if (Datatype::isreal(dstType))
	{	// complex to real, 'toRealByAbs' is only used here
		CONVERT_OUTER_SWITCH(vectorComplexToReal);
	}
	else
	{	// all complex, use the real convert of each part instead
		if (strideSrc == 1 && strideDst == 1)
		{
			return vecDataConvert(Datatype::realCorrespond(srcType), Datatype::realCorrespond(dstType), src, dst, N * 2, 1, 1, true);
		}
		else
		{
			// the real parts
			auto ret1 = vecDataConvert(Datatype::realCorrespond(srcType), Datatype::realCorrespond(dstType), src, dst, N * 2, strideSrc * 2, strideDst * 2, true);
			if (ret1 != ERROR_RETURN())
				return ret1;
			// increase pointers
			const int sizeSrc = Datatype::size(srcType), sizeDst = Datatype::size(dstType);
			const void* srcInc = (const char*)src + sizeSrc;
			void* dstInc = (char*)dst + sizeDst;
			// the imaginary parts
			return vecDataConvert(Datatype::realCorrespond(srcType), Datatype::realCorrespond(dstType), srcInc, dstInc, N * 2, strideSrc * 2, strideDst * 2, true);
		}
	}
#undef CONVERT_OUTER_SWITCH
#undef CONVERT_INNER_SWITCH
	// calculate
	convertFunc(src, dst, N, strideSrc, strideDst, toRealByAbs);
}
#pragma endregion


#pragma region dense vector set values with small absolutes to zero
template<typename T, typename U>
struct foreachClip_strided_functor
{
	T* a;
	const U val;
	const unsigned int stride;

	foreachClip_strided_functor(void* a, const void* val, const unsigned int stride) : a((T*)a), val(*(const U*)val), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = std::abs(a[indA]) < val ? T() : a[indA];
	}
};
template<typename T, typename U>
struct foreachClip_functor
{
	T* a;
	const U val;

	foreachClip_functor(void* a, const void* val) : a((T*)a), val(*(const U*)val) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = std::abs(a[ind]) < val ? T() : a[ind];
	}
};

template<typename T>
inline int vectorClip(void* av, const void* threshold, const size_t N, const unsigned int stride)
{
	if constexpr (std::is_scalar<T>::value)
	{
		if (stride == 1)
		{
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachClip_functor<T, T>(av, threshold));
		}
		else
		{
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachClip_strided_functor<T, T>(av, threshold, stride));
		}
	}
	else
	{
		using U = typename T::value_type;
		if (stride == 1)
		{
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachClip_functor<T, U>(av, threshold));
		}
		else
		{
			thrust::for_each_n(THRUST_PAR, counting0, N, foreachClip_strided_functor<T, U>(av, threshold, stride));
		}
	}
	return 0;
}

DLLEXP
int vecClip(const Datatype::DataType type, void* a, const void* threshold, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorClip, type, int, a, threshold, N, stride);
}
#pragma endregion


#pragma region vector add scalar
template<typename T>
struct foreachAdd_strided_functor
{
	T* a;
	const T val;
	const unsigned int stride;

	foreachAdd_strided_functor(void* a, const void* val, const unsigned int stride) : a((T*)a), val(*(const T*)val), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = a[indA] + val;
	}
};
template<typename T>
struct foreachAdd_functor
{
	T* a;
	const T val;

	foreachAdd_functor(void* a, const void* val) : a((T*)a), val(*(const T*)val) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = a[ind] + val;
	}
};

template<typename T>
inline int vectorAddedByScalar(void* av, const void* scalar, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachAdd_functor<T>(av, scalar));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, N, foreachAdd_strided_functor<T>(av, scalar, stride));
	}
	return 0;
}

DLLEXP
int vecAddScalar(const Datatype::DataType type, void* a, const void* scalar, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorAddedByScalar, type, int, a, scalar, N, stride);
}
#pragma endregion


#pragma region vector aggregate -- abs sum
template<typename T>
struct realAbsPlus_functor
{
	__host__ __device__ const T operator()(const T& x, const T& y) const
	{
		return x + std::abs(y);
	}
};

template<typename T>
struct compAbsPlus_functor
{
	__host__ __device__ const T operator()(const T& x, const BlasSupp::complex<T>& y) const
	{
		return x + std::abs(y);
	}
};

template<typename T>
struct realSquarePlus_functor
{
	__host__ __device__ const T operator()(const T& x, const T& y) const
	{
		return std::fma(y, y, x);
	}
};

template<typename T>
struct compSquarePlus_functor
{
	__host__ __device__ const T operator()(const T& x, const BlasSupp::complex<T>& y) const
	{
		return x + y.absSquare();
	}
};

template<typename T>
inline double vectorAbsoluteSum(const void* av, const size_t N, const unsigned int stride)
{
	const T* a = (const T*)av;
	if constexpr (std::is_scalar<T>::value)
	{
		T outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + N, T(), realAbsPlus_functor<T>());
		}
		else
		{
			auto strideA = make_strided_range(a, N, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), realAbsPlus_functor<T>());
		}
		return (double)outSum;
	}
	else
	{
		using realT = typename T::value_type;
		realT outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + N, realT(), compAbsPlus_functor<realT>());
		}
		else
		{
			auto strideA = make_strided_range(a, N, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), realT(), compAbsPlus_functor<realT>());
		}
		return (double)outSum;
	}
}

template<typename T>
inline double vectorNorm(const void* av, const size_t N, const unsigned int stride)
{
	const T* a = (const T*)av;
	if constexpr (std::is_scalar<T>::value)
	{
		T outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + N, T(), realSquarePlus_functor<T>());
		}
		else
		{
			auto strideA = make_strided_range(a, N, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), realSquarePlus_functor<T>());
		}
		return std::sqrt((double)outSum);
	}
	else
	{
		using realT = typename T::value_type;
		realT outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + N, realT(), compSquarePlus_functor<realT>());
		}
		else
		{
			auto strideA = make_strided_range(a, N, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), realT(), compSquarePlus_functor<realT>());
		}
		return std::sqrt((double)outSum);
	}
}

DLLEXP
double vecAbsSum(const Datatype::DataType type, const void* a, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorAbsoluteSum, type, double, a, N, stride);
}

DLLEXP
double vecNorm(const Datatype::DataType type, const void* a, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorNorm, type, double, a, N, stride);
}
#pragma endregion


#pragma region vector dot
template<typename T>
inline int vectorsInner(const void* av, const void* bv, const size_t N, const unsigned int sa, const unsigned int sb, void* result)
{
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	if (sa == 1 && sb == 1)
	{
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + N, b, T());
	}
	else if (sa == 1 && sb != 1)
	{
		auto strideB = make_strided_range(b, N, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + N, strideB.begin(), T());
	}
	else if (sa != 1 && sb == 1)
	{
		auto strideA = make_strided_range(a, N, sa);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), b, T());
	}
	else
	{
		auto strideA = make_strided_range(a, N, sa);
		auto strideB = make_strided_range(b, N, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), T());
	}
	return 0;
}

template <typename T>
struct conjMultiply_functor
{
	__host__ __device__ const T operator()(const T& x, const T& y) const
	{
		return std::conj(x) * y;
	}
};

template<typename T>
inline int vectorsInnerConjugateA(const void* av, const void* bv, const size_t N, const unsigned int sa, const unsigned int sb, void* result)
{
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	if (sa == 1 && sb == 1)
	{
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + N, b, T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	else if (sa == 1 && sb != 1)
	{
		auto strideB = make_strided_range(b, N, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + N, strideB.begin(), T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	else if (sa != 1 && sb == 1)
	{
		auto strideA = make_strided_range(a, N, sa);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), b, T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, N, sa);
		auto strideB = make_strided_range(b, N, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	return 0;
}

DLLEXP
int vecDot(const Datatype::DataType type, const void* a, const void* b, const size_t N, const unsigned int strideA, const unsigned int strideB, void* result)
{
	AUTO_ALLTYPE_FUNC(vectorsInner, type, int, a, b, N, strideA, strideB, result);
}

DLLEXP
int vecDotc(const Datatype::DataType type, const void* a, const void* b, const size_t N, const unsigned int strideA, const unsigned int strideB, void* result)
{
	AUTO_COMPLEX_TYPE_FUNC(vectorsInnerConjugateA, type, int, a, b, N, strideA, strideB, result);
}
#pragma endregion


#pragma region vector min max
template<typename T>
struct absCompare_functor
{
	__host__ __device__ const bool operator()(const T& x, const T& y) const
	{
		return std::abs(x) < std::abs(y);
	}
};

template<typename T>
inline size_t vectorArgAbsMin(const void* av, const size_t N, const unsigned int stride)
{
	const T* a = (const T*)av;
	if (stride == 1)
	{
		const T* elemPtr = thrust::min_element(THRUST_PAR, a, a + N, absCompare_functor<T>());
		return elemPtr - a;
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		auto elemPtr = thrust::min_element(THRUST_PAR, strideA.begin(), strideA.end(), absCompare_functor<T>());
		return stride * (elemPtr - strideA.begin());
	}
	// fake return for NVCC
	return 0;
}

template<typename T>
inline size_t vectorArgAbsMax(const void* av, const size_t N, const unsigned int stride)
{
	const T* a = (const T*)av;
	if (stride == 1)
	{
		const T* elemPtr = thrust::max_element(THRUST_PAR, a, a + N, absCompare_functor<T>());
		return elemPtr - a;
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		auto elemPtr = thrust::max_element(THRUST_PAR, strideA.begin(), strideA.end(), absCompare_functor<T>());
		return stride * (elemPtr - strideA.begin());
	}
	// fake return for NVCC
	return 0;
}

DLLEXP
size_t vecArgAbsMin(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorArgAbsMin, type, size_t, a, N, stride);
}

DLLEXP
size_t vecArgAbsMax(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorArgAbsMax, type, size_t, a, N, stride);
}
#pragma endregion


#pragma region vector aggregate -- sum
template<typename T>
inline int vectorSum(const void* av, const size_t N, const unsigned int stride, void* outv)
{
	const T* a = (const T*)av;
	T* outSum = (T*)outv;
	if (stride == 1)
	{
		*outSum = thrust::reduce(THRUST_PAR, a, a + N, T(), plus_functor<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		*outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), plus_functor<T>());
	}
	return 0;
}

DLLEXP
int vecSum(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride, void* outSum)
{
	AUTO_ALLTYPE_FUNC(vectorSum, type, int, a, N, stride, outSum);
}
#pragma endregion


#pragma region vector aggregate -- product
template<typename T>
inline int vectorAccumulateProduct(const void* av, const size_t N, const unsigned int stride, void* outv)
{
	const T* a = (const T*)av;
	T* outProd = (T*)outv;
	if (stride == 1)
	{
		*outProd = thrust::reduce(THRUST_PAR, a, a + N, T(1), thrust::multiplies<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		*outProd = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(1), thrust::multiplies<T>());
	}
	return 0;
}

DLLEXP
int vecProd(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride, void* outProd)
{
	AUTO_ALLTYPE_FUNC(vectorAccumulateProduct, type, int, a, N, stride, outProd);
}
#pragma endregion


#pragma region vector aggregate -- partial sum
template<typename T>
inline int vectorPartialSum(const void* srcv, void* dstv, const size_t N, const bool inclusive, const unsigned int strideSrc, const unsigned int strideDst)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	if (strideSrc == 1 && strideDst == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + N, dst, plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + N, dst, T(), plus_functor<T>());
	}
	else if (strideSrc == 1 && strideDst != 1)
	{
		auto stridedDst = make_strided_range(dst, N, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + N, stridedDst.begin(), plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + N, stridedDst.begin(), T(), plus_functor<T>());
	}
	else if (strideSrc != 1 && strideDst == 1)
	{
		auto stridedSrc = make_strided_range(src, N, strideSrc);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, T(), plus_functor<T>());
	}
	else
	{
		auto stridedSrc = make_strided_range(src, N, strideSrc);
		auto stridedDst = make_strided_range(dst, N, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), T(), plus_functor<T>());
	}
	return 0;
}

DLLEXP
int vecParSum(const Datatype::DataType type, const void* src, void* dst, const size_t N, const bool inclusive, const unsigned int strideSrc, const unsigned int strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorPartialSum, type, int, src, dst, N, inclusive, strideSrc, strideDst);
}
#pragma endregion


#pragma region vector aggregate -- partial product
template<typename T>
inline int vectorPartialProduct(const void* srcv, void* dstv, const size_t N, const bool inclusive, const unsigned int strideSrc, const unsigned int strideDst)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	if (strideSrc == 1 && strideDst == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + N, dst, thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + N, dst, T(1), thrust::multiplies<T>());
	}
	else if (strideSrc == 1 && strideDst != 1)
	{
		auto stridedDst = make_strided_range(dst, N, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + N, stridedDst.begin(), thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + N, stridedDst.begin(), T(1), thrust::multiplies<T>());
	}
	else if (strideSrc != 1 && strideDst == 1)
	{
		auto stridedSrc = make_strided_range(src, N, strideSrc);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, T(1), thrust::multiplies<T>());
	}
	else
	{
		auto stridedSrc = make_strided_range(src, N, strideSrc);
		auto stridedDst = make_strided_range(dst, N, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), T(1), thrust::multiplies<T>());
	}
	return 0;
}

DLLEXP
int vecParProd(const Datatype::DataType type, const void* src, void* dst, const size_t N,const bool inclusive, const unsigned int strideSrc, const unsigned int strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorPartialProduct, type, int, src, dst, N, inclusive, strideSrc, strideDst);
}
#pragma endregion


#pragma region int operations
template<typename T>
inline int vectorSort(void* vec, const size_t N, const unsigned int stride)
{
	T* v = (T*)vec;
	if (stride == 1)
	{
		thrust::sort(THRUST_PAR, v, v + N);
	}
	else
	{
		auto stridedVec = make_strided_range(v, N, stride);
		thrust::sort(THRUST_PAR, stridedVec.begin(), stridedVec.end());
	}
	return 0;
}

DLLEXP
int vecSort(const Datatype::DataType type, void* vec, const size_t N, const unsigned int stride)
{
	AUTO_REALTYPE_FUNC(vectorSort, type, int, vec, N, stride);
}

template<typename TKey, typename TVal>
inline int vectorSortBy(void* keys, void* vals, const size_t N, const unsigned int strideKey, const unsigned int strideVal)
{
	TKey* k = (TKey*)keys;
	TVal* v = (TVal*)vals;
	if (strideKey == 1 && strideVal == 1)
	{
		thrust::sort_by_key(THRUST_PAR, k, k + N, v);
	}
	else if (strideKey == 1 && strideVal != 1)
	{
		auto stridedVal = make_strided_range(v, N, strideVal);
		thrust::sort_by_key(THRUST_PAR, k, k + N, stridedVal.begin());
	}
	else if (strideKey != 1 && strideVal == 1)
	{
		auto stridedKey = make_strided_range(k, N, strideKey);
		thrust::sort_by_key(THRUST_PAR, stridedKey.begin(), stridedKey.end(), v);
	}
	else
	{
		auto stridedKey = make_strided_range(k, N, strideKey);
		auto stridedVal = make_strided_range(v, N, strideVal);
		thrust::sort_by_key(THRUST_PAR, stridedKey.begin(), stridedKey.end(), stridedVal.begin());
	}
	return 0;
}

template<typename T>
inline int vectorSortBy(const Datatype::DataType valType, void* keys, void* vals, const size_t N, const unsigned int strideKey, const unsigned int strideVal)
{
#ifndef HAS_LDBL
#define __ALLTYPE_FUNC(dataType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return vectorSortBy<T, float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return vectorSortBy<T, double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return vectorSortBy<T, BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return vectorSortBy<T, BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::RealInt8: \
			return vectorSortBy<T, int8_t>(__VA_ARGS__); \
		case Datatype::RealInt16: \
			return vectorSortBy<T, int16_t>(__VA_ARGS__); \
		case Datatype::RealInt32: \
			return vectorSortBy<T, int32_t>(__VA_ARGS__); \
		case Datatype::RealInt64: \
			return vectorSortBy<T, int64_t>(__VA_ARGS__); \
		case Datatype::RealUInt8: \
			return vectorSortBy<T, uint8_t>(__VA_ARGS__); \
		case Datatype::RealUInt16: \
			return vectorSortBy<T, uint16_t>(__VA_ARGS__); \
		case Datatype::RealUInt32: \
			return vectorSortBy<T, uint32_t>(__VA_ARGS__); \
		case Datatype::RealUInt64: \
			return vectorSortBy<T, uint64_t>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return vectorSortBy<T, BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return vectorSortBy<T, BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return vectorSortBy<T, BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return vectorSortBy<T, BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt8: \
			return vectorSortBy<T, BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt16: \
			return vectorSortBy<T, BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt32: \
			return vectorSortBy<T, BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt64: \
			return vectorSortBy<T, BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(vectorSortBy, dataType, int); \
		} \
	} while (0)
#else
#define __ALLTYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return vectorSortBy<T, float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return vectorSortBy<T, double>(__VA_ARGS__); \
		case Datatype::RealLongDouble: \
			return vectorSortBy<T, long double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return vectorSortBy<T, BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return vectorSortBy<T, BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::ComplexLongDouble: \
			return vectorSortBy<T, BlasSupp::complex<long double>>(__VA_ARGS__); \
		case Datatype::RealInt8: \
			return vectorSortBy<T, int8_t>(__VA_ARGS__); \
		case Datatype::RealInt16: \
			return vectorSortBy<T, int16_t>(__VA_ARGS__); \
		case Datatype::RealInt32: \
			return vectorSortBy<T, int32_t>(__VA_ARGS__); \
		case Datatype::RealInt64: \
			return vectorSortBy<T, int64_t>(__VA_ARGS__); \
		case Datatype::RealUInt8: \
			return vectorSortBy<T, uint8_t>(__VA_ARGS__); \
		case Datatype::RealUInt16: \
			return vectorSortBy<T, uint16_t>(__VA_ARGS__); \
		case Datatype::RealUInt32: \
			return vectorSortBy<T, uint32_t>(__VA_ARGS__); \
		case Datatype::RealUInt64: \
			return vectorSortBy<T, uint64_t>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return vectorSortBy<T, BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return vectorSortBy<T, BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return vectorSortBy<T, BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return vectorSortBy<T, BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt8: \
			return vectorSortBy<T, BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt16: \
			return vectorSortBy<T, BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt32: \
			return vectorSortBy<T, BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt64: \
			return vectorSortBy<T, BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(vectorSortBy, dataType, int); \
		} \
	} while (0)
#endif
	
__ALLTYPE_FUNC(valType, keys, vals, N, strideKey, strideVal);

#undef __ALLTYPE_FUNC
}

DLLEXP
ERROR_RETURN intMinMax(const int* v, const size_t N, int& min, int& max)
{
	auto result = thrust::minmax_element(THRUST_PAR, v, v + N);
#ifdef CPU
	max = *result.first;
	max = *result.second;
#else
	cudaError err = cudaMemcpy(&min, result.first, sizeof(int), cudaMemcpyDeviceToHost);
	if (err != 0) return err;
	err = cudaMemcpy(&max, result.second, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
#endif // CPU
}

DLLEXP
ERROR_RETURN intMax(const int* v, const size_t N, int& max)
{
	const int* result = thrust::max_element(THRUST_PAR, v, v + N);
#ifdef CPU
	max = *result;
#else
	cudaError err = cudaMemcpy(&max, result, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
#endif // CPU
}

DLLEXP
int vecSortBy(const Datatype::DataType keyType, const Datatype::DataType valType, void* keys, void* vals, const size_t N, const unsigned int strideKey, const unsigned int strideVal)
{
	AUTO_REALTYPE_FUNC(vectorSortBy, keyType, int, valType, keys, vals, N, strideKey, strideVal);
}

template<typename T>
inline int vectorFind(const void* vec, const size_t N, const unsigned int stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		index = thrust::find(THRUST_PAR, v, v + N, f) - v;
		if (index == N)
			index = -1;
	}
	else
	{
		auto stridedVec = make_strided_range(v, N, stride);
		index = thrust::find(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
		if (index == N)
			index = -1;
	}
	return 0;
}
template<typename T>
inline int vectorSortedFind(const void* vec, const size_t N, const unsigned int stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		bool found = thrust::binary_search(THRUST_PAR, v, v + N, f);
		index = thrust::upper_bound(THRUST_PAR, v, v + N, f) - v;
		if (!found)
			index = ~index;
	}
	else
	{
		auto stridedVec = make_strided_range(v, N, stride);
		bool found = thrust::binary_search(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f);
		index = thrust::upper_bound(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
		if (!found)
			index = ~index;
	}
	return 0;
}

DLLEXP
int vecFind(const Datatype::DataType type, const bool sorted, const void* v, const size_t N, const unsigned int stride, const void* toFind, ptrdiff_t& index)
{
	if (sorted)
	{
		AUTO_REALTYPE_FUNC(vectorSortedFind, type, int, v, N, stride, toFind, index);
	}
	else
	{
		AUTO_REALTYPE_FUNC(vectorFind, type, int, v, N, stride, toFind, index);
	}
}


template<typename T>
inline int vectorFillRange(void* vec, const size_t N, const unsigned int stride, const void* start, const void* step)
{
	T* v = (T*)vec;
	const T s = *(const T*)start, d = *(const T*)step;
	if (stride == 1)
	{
		thrust::sequence(THRUST_PAR, v, v + N, s, d);
	}
	else
	{
		auto stridedVec = make_strided_range(v, N, stride);
		thrust::sequence(THRUST_PAR, stridedVec.begin(), stridedVec.end(), s, d);
	}
	return 0;
}

DLLEXP
int vecFill(const Datatype::DataType type, void* v, const size_t N, const unsigned int stride, const void* start, const void* step)
{
	AUTO_ALLTYPE_FUNC(vectorFillRange, type, int, v, N, stride, start, step);
}


template<typename T>
inline int vectorUpperBound(const void* vec, const size_t N, const unsigned int stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		index = thrust::upper_bound(THRUST_PAR, v, v + N, f) - v;
	}
	else
	{
		auto stridedVec = make_strided_range(v, N, stride);
		index = thrust::upper_bound(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
	}
	return 0;
}
template<typename T>
inline int vectorLowerBound(const void* vec, const size_t N, const unsigned int stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		index = thrust::lower_bound(THRUST_PAR, v, v + N, f) - v;
	}
	else
	{
		auto stridedVec = make_strided_range(v, N, stride);
		index = thrust::lower_bound(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
	}
	return 0;
}

DLLEXP
int vecBound(const Datatype::DataType type, const bool lower, const void* v, const size_t N, const unsigned int stride, const void* find, ptrdiff_t& index)
{
	if (lower)
	{
		AUTO_REALTYPE_FUNC(vectorLowerBound, type, int, v, N, stride, find, index);
	}
	else
	{
		AUTO_REALTYPE_FUNC(vectorUpperBound, type, int, v, N, stride, find, index);
	}
}
#pragma endregion