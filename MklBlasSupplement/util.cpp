#include <memory.h>
#include <string.h>
#include <stdio.h>
#include "macro.h"

////#include <C:\Program Files (x86)\IntelSWTools\compilers_and_libraries\windows\mkl\include\mkl.h>


#pragma region mem
#if defined(_WIN32)
//  Microsoft 
#include <windows.h>
CUSTOM_EXTERN_C
DLLEXP void getTotalSystemMemory(unsigned long long& total, unsigned long long& free)
{
	MEMORYSTATUSEX status;
	status.dwLength = sizeof(status);
	GlobalMemoryStatusEx(&status);
	total = status.ullTotalPhys;
	free = status.ullAvailPhys;
}
END_CUSTOM_EXTERN_C
#elif defined(__linux__) || defined(__linux) || defined(linux)
// Linux
#include <sys/sysinfo.h>
CUSTOM_CUSTOM_EXTERN_C
DLLEXP void getTotalSystemMemory(unsigned long long& total, unsigned long long& free)
{
	struct sysinfo info;
	sysinfo(&info);
	total = info.totalram * info.mem_unit;
	free = info.freeram * info.mem_unit;
}
END_CUSTOM_CUSTOM_EXTERN_C
#else
//  do nothing and hope for the best?
#endif


CUSTOM_EXTERN_C
DLLEXP void hostmemset(void* a, const int v, const size_t N) {
	memset(a, v, N);
}

DLLEXP void hostmemcopy(const void* src, void* dst, const size_t N) {
	memcpy(dst, src, N);
}

DLLEXP void hostmemcopy2D(const void* src, const size_t srcPitch, void* dst, const size_t dstPitch, const size_t height, const size_t width) {
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
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region file
enum FileErrorEnum
{
	FILE_SUCCESS = 0,
	OPEN_FILE_ERROR = 1,
	MEMORY_ALLOC_ERROR = 2,
	FILE_SIZE_INCONSISTENT = 3
};
// Ignore Spelling: wb rb
CUSTOM_EXTERN_C
DLLEXP FileErrorEnum hostToFile(const void* a, const size_t N, const char* path) {
	FILE* pFile = fopen(path, "wb");
	if (pFile == NULL)
		return OPEN_FILE_ERROR;
	fwrite(a, sizeof(char), N, pFile);
	fclose(pFile);
	return FILE_SUCCESS;
}

DLLEXP FileErrorEnum hostFromFile(void* a, const size_t N, const char* path) {
	FILE* pFile = fopen(path, "rb");
	if (pFile == NULL)
		return OPEN_FILE_ERROR;

	// copy the file into the buffer:
	size_t result = fread(a, 1, N, pFile);
	if (result != N)
		return FILE_SIZE_INCONSISTENT;

	// close and return
	fclose(pFile);
	return FILE_SUCCESS;
}

DLLEXP FileErrorEnum hostFromFileGetSize(size_t& N, const char* path) {
	FILE* pFile = fopen(path, "rb");
	if (pFile == NULL)
		return OPEN_FILE_ERROR;

	// obtain file size:
	fseek(pFile, 0, SEEK_END);
	N = ftell(pFile);

	// close and return
	fclose(pFile);
	return FILE_SUCCESS;
}
END_CUSTOM_EXTERN_C
#pragma endregion
