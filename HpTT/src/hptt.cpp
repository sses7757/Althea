/**
 *   High-Performance Tensor Transposition Library
 *
 *   Copyright (C) 2017  Paul Springer (springer@aices.rwth-aachen.de)
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

#include <vector>
#include <memory>
#include <unordered_map>

#include "transpose.h"
#include "macros.h"


extern "C" {
	DLLEXP void sTensorTranspose(const int* perm, const int dim,
		const float alpha, const float* A, const int* sizeA, const int* outerSizeA,
		const float beta, float* B, const int* outerSizeB,
		const int numThreads, const int useRowMajor)
	{
		auto plan(std::make_shared<hptt::Transpose<float> >(sizeA, perm, outerSizeA, outerSizeB, dim, A, alpha, B, beta,
			hptt::ESTIMATE, numThreads, nullptr, useRowMajor));
		plan->execute();
		plan->~Transpose();
	}

	DLLEXP void dTensorTranspose(const int* perm, const int dim,
		const double alpha, const double* A, const int* sizeA, const int* outerSizeA,
		const double beta, double* B, const int* outerSizeB,
		const int numThreads, const int useRowMajor)
	{
		auto plan(std::make_shared<hptt::Transpose<double> >(sizeA, perm, outerSizeA, outerSizeB, dim, A, alpha, B, beta,
			hptt::ESTIMATE, numThreads, nullptr, useRowMajor));
		plan->execute();
	}

	DLLEXP void cTensorTranspose(const int* perm, const int dim,
		const hptt::FloatComplex alpha, bool conjA, const hptt::FloatComplex* A, const int* sizeA, const int* outerSizeA,
		const hptt::FloatComplex beta, hptt::FloatComplex* B, const int* outerSizeB,
		const int numThreads, const int useRowMajor)
	{
		auto plan(std::make_shared<hptt::Transpose<hptt::FloatComplex> >(sizeA, perm, outerSizeA, outerSizeB, dim,
			(const hptt::FloatComplex*) A, (hptt::FloatComplex) alpha, (hptt::FloatComplex*) B, (hptt::FloatComplex) beta,
			hptt::ESTIMATE, numThreads, nullptr, useRowMajor));
		plan->setConjA(conjA);
		plan->execute();
	}

	DLLEXP void zTensorTranspose(const int* perm, const int dim,
		const hptt::DoubleComplex alpha, bool conjA, const hptt::DoubleComplex* A, const int* sizeA, const int* outerSizeA,
		const hptt::DoubleComplex beta, hptt::DoubleComplex* B, const int* outerSizeB,
		const int numThreads, const int useRowMajor)
	{
		auto plan(std::make_shared<hptt::Transpose<hptt::DoubleComplex> >(sizeA, perm, outerSizeA, outerSizeB, dim,
			(const hptt::DoubleComplex*) A, (hptt::DoubleComplex) alpha, (hptt::DoubleComplex*) B, (hptt::DoubleComplex) beta,
			hptt::ESTIMATE, numThreads, nullptr, useRowMajor));
		plan->setConjA(conjA);
		plan->execute();
	}
}