// platform specific export DLL
#if defined(_MSC_VER)
#define DLLEXP extern "C" __declspec(dllexport)
#elif defined(__ICC) || defined(__INTEL_COMPILER) || defined(__GNUC__) || defined(__GNUG__)
#define DLLEXP extern "C" __attribute__((visibility("default")))
#else
//  do nothing and hope for the best?
#define DLLEXP
#pragma warning Unknown dynamic link import/export semantics.
#endif


#include <stdio.h>
#include <stdint.h>



#pragma region memory
#if defined(_WIN32)
//  Microsoft 
#include <windows.h>
DLLEXP void getTotalSystemMemory(uint64_t& total, uint64_t& free)
{
	MEMORYSTATUSEX status{};
	status.dwLength = sizeof(status);
	GlobalMemoryStatusEx(&status);
	total = status.ullTotalPhys;
	free = status.ullAvailPhys;
}
#elif defined(__linux__) || defined(__linux) || defined(linux)
// Linux
#include <sys/sysinfo.h>
DLLEXP void getTotalSystemMemory(uint64_t& total, uint64_t& free)
{
	struct sysinfo info;
	sysinfo(&info);
	total = info.totalram * info.mem_unit;
	free = info.freeram * info.mem_unit;
}
#else
//  do nothing and hope for the best?
#endif


DLLEXP void host_memset(void* a, const int v, const size_t N)
{
	memset(a, v, N);
}

DLLEXP void host_memcopy(const void* src, void* dst, const size_t N)
{
	memcpy(dst, src, N);
}

DLLEXP void host_memcopy2D(const void* src, const size_t srcPitch, void* dst, const size_t dstPitch, const size_t height, const size_t width)
{
	if (srcPitch == dstPitch && srcPitch == height)
	{
		memcpy(dst, src, height * width);
		return;
	}
	const char* s = (const char*)src;
	const char* end = s + srcPitch * width;
	char* d = (char*)dst;
	for (; s < end; s += srcPitch, d += dstPitch)
	{
		memcpy(d, s, height);
	}
}
#pragma endregion


#pragma region file
#pragma warning(disable : 4996)
enum class FileErrorEnum : int
{
	FILE_SUCCESS = 0,
	OPEN_FILE_ERROR = 1,
	MEMORY_ALLOC_ERROR = 2,
	FILE_SIZE_INCONSISTENT = 3
};

// Ignore Spelling: wb rb
DLLEXP FileErrorEnum host_mem2file(const void* a, const size_t N, const char* path)
{
	FILE* pFile = fopen(path, "wb");
	if (pFile == NULL)
		return FileErrorEnum::OPEN_FILE_ERROR;
	fwrite(a, sizeof(char), N, pFile);
	fclose(pFile);
	return FileErrorEnum::FILE_SUCCESS;
}

DLLEXP FileErrorEnum host_file2mem(void* a, const size_t N, const char* path)
{
	FILE* pFile = fopen(path, "rb");
	if (pFile == NULL)
		return FileErrorEnum::OPEN_FILE_ERROR;

	// copy the file into the buffer:
	size_t result = fread(a, 1, N, pFile);
	if (result != N)
		return FileErrorEnum::FILE_SIZE_INCONSISTENT;

	// close and return
	fclose(pFile);
	return FileErrorEnum::FILE_SUCCESS;
}

DLLEXP FileErrorEnum host_fileGetSize(size_t& size, const char* path)
{
	FILE* pFile = fopen(path, "rb");
	if (pFile == NULL)
		return FileErrorEnum::OPEN_FILE_ERROR;

	// obtain file size:
	fseek(pFile, 0, SEEK_END);
	size = ftell(pFile);

	// close and return
	fclose(pFile);
	return FileErrorEnum::FILE_SUCCESS;
}
#pragma endregion


#include "host_datatype.h"

#pragma region parse complex number
const char* const str_plus = " + ";
const char* const str_minus = " - ";
const char* const str_neg = "-";

template <typename T>
bool parseT(const std::string s, T& result)
{
	if constexpr (std::is_integral_v<T>)
	{
		try
		{
			long long d = std::stoll(s);
			result = (T)d;
			return true;
		}
		catch (const std::exception&)
		{
			return false;
		}
	}
	else if constexpr (std::is_floating_point_v<T>)
	{
		try
		{
			long double d = std::stold(s);
			result = (T)d;
			return true;
		}
		catch (const std::exception&)
		{
			return false;
		}
	}
}


template <typename T>
bool parsePart(const std::string s, complex::complex<T>* result, bool& isReal)
{
	const size_t findi = s.find('i'), findI = s.find('I');
	const size_t len = s.size() - 1;
	if ((findi != std::string::npos && findI != std::string::npos) ||
		(findi != std::string::npos && findi != len) ||
		(findI != std::string::npos && findI != len))
	{
		return false;
	}
	if (findi == len || findI == len)
	{
		isReal = false;
		return parseT(s.substr(s.size() - 1), result->_imag);
	}
	else
	{
		isReal = true;
		return parseT(s.substr(s.size() - 1), result->_real);
	}
}

template <typename T>
bool parseComplex(const char* const str, void* resultv)
{
	if (!str)
		return false;

	std::string s(str);
	if (s.empty())
		return false;

	complex::complex<T>* result = (complex::complex<T>*)resultv;

	const size_t findPlus = s.find(str_plus), findMinus = s.find(str_minus);
	const bool hasPlus = findPlus != std::string::npos, hasMinus = findMinus != std::string::npos;
	if (!hasPlus && !hasMinus)
	{	// only one part
		size_t findNeg = s.find(str_neg);
		if (findNeg == 0)
		{	// has negate operator
			findNeg = s.find(str_neg, 1);
			if (findNeg != std::string::npos)
				return false; // has multiple negate operators
		}
		else if (findNeg != std::string::npos)
		{
			return false;
		}
		// real or imaginary part
		result->_real = T();
		result->_imag = T();
		bool isReal = false;
		return parsePart(s, result, isReal);
	}
	else if (hasPlus && hasMinus)
	{
		return false;
	}
	// check multiple plus or minus operators
	if (hasPlus)
	{
		const size_t find2 = s.find(str_plus, findPlus + 1);
		if (find2 != std::string::npos)
			return false;
	}
	else
	{
		const size_t find2 = s.find(str_minus, findMinus + 1);
		if (find2 != std::string::npos)
			return false;
	}

	// have both parts
	std::string firstPart = s.substr(0, min(findPlus, findMinus));
	std::string lastPart = s.substr(min(findPlus, findMinus) + 4);
	bool isRealPart = false;
	if (!parsePart(firstPart, result, isRealPart))
	{
		return false;
	}
	bool isRealPart2 = false;
	if (!parsePart(lastPart, result, isRealPart2))
	{
		return false;
	}
	if (!(isRealPart ^ isRealPart2))
	{
		return false;
	}
	return true;
}


DLLEXP bool parseComp(const DataType type, const char* const str, void* result)
{
	AUTO_COMPLEX_REAL_TYPE_FUNC(parseComplex, type, bool, str, result);
}
#pragma endregion

#pragma region MyRegion

#pragma endregion
