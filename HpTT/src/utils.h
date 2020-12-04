/**
 * @author: Paul Springer (springer@aices.rwth-aachen.de)
 */

#pragma once

#include <list>
#include <vector>
#include <iostream>

#include "hptt_types.h"
#include "macros.h"

#define DoubleEps 2.2204460492503131e-16
#define SingleEps 1.192092896e-07F


namespace hptt {

	template<typename floatType>
	static floatType conj(floatType x) {
		return std::conj(x);
	}
	template<>
	float conj(float x) {
		return x;
	}
	template<>
	double conj(double x) {
		return x;
	}

	template<typename floatType> static double getZeroThreshold();
	template<> double getZeroThreshold<double>() { return DoubleEps; }
	template<> double getZeroThreshold<DoubleComplex>() { return DoubleEps; }
	template<> double getZeroThreshold<float>() { return SingleEps; }
	template<> double getZeroThreshold<FloatComplex>() { return SingleEps; }

	template<typename t> INLINE
	int hasItem(const std::vector<t>& vec, t value)
	{
		return (std::find(vec.begin(), vec.end(), value) != vec.end());
	}

	template<typename t> INLINE
		void printVector(const t* vec, const int size, const char* label) {
		std::cout << label << ": ";
		if (vec == NULL)
		{
			std::cout << "null\n";
			return;
		}
		for (int i = 0; i < size; i++)
		{
			std::cout << vec[i] << ", ";
		}
		std::cout << "\n";
	}

	template<typename t> INLINE
	void printVector(const std::vector<t>& vec, const char* label) {
		std::cout << label << ": ";
		for (auto a : vec)
			std::cout << a << ", ";
		std::cout << "\n";
	}

	template<typename t> INLINE
	void printVector(const std::list<t>& vec, const char* label) {
		std::cout << label << ": ";
		for (auto a : vec)
			std::cout << a << ", ";
		std::cout << "\n";
	}


	INLINE void getPrimeFactors(int n, std::list<int>& primeFactors)
	{
		primeFactors.clear();
		for (int i = 2; i <= n; ++i) {
			while (n % i == 0) {
				primeFactors.push_back(i);
				n /= i;
			}
		}
		if (primeFactors.size() <= 0) {
			fprintf(stderr, "[HPTT] Internal error: primefactorization for %d did not work.\n", n);
			exit(-1);
		}
	}

	template<typename t> INLINE
	static int findPos(t value, const std::vector<t>& array)
	{
		for (int i = 0; i < array.size(); ++i)
			if (array[i] == value)
				return i;
		return -1;
	}

	INLINE int findPos(int value, const int* array, int n)
	{
		for (int i = 0; i < n; ++i)
			if (array[i] == value)
				return i;
		return -1;
	}

	INLINE void accountForRowMajor(const int* sizeA, const int* outerSizeA, const int* outerSizeB, const int* perm,
		int* tmpSizeA, int* tmpOuterSizeA, int* tmpOuterSizeB, int* tmpPerm, const int dim, const bool useRowMajor)
	{
		for (int i = 0; i < dim; ++i) {
			int idx = i;
			if (useRowMajor) {
				idx = dim - 1 - i; // reverse order
				tmpPerm[i] = dim - perm[idx] - 1;
			}
			else
				tmpPerm[i] = perm[i];
			tmpSizeA[i] = sizeA[idx];

			if (outerSizeA == nullptr)
				tmpOuterSizeA[i] = sizeA[idx];
			else
				tmpOuterSizeA[i] = outerSizeA[idx];
			if (outerSizeB == nullptr)
				tmpOuterSizeB[i] = sizeA[perm[idx]];
			else
				tmpOuterSizeB[i] = outerSizeB[idx];
		}
	}
}


