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


#pragma region MyRegion

#pragma endregion
