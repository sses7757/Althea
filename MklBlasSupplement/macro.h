#pragma once

#if defined(_MSC_VER)
#define INLINE __forceinline
#define DLLEXP __declspec(dllexport)
#elif defined(__ICC) || defined(__INTEL_COMPILER) || defined(__GNUC__) || defined(__GNUG__)
#define INLINE __attribute__((always_inline)) inline
#define DLLEXP __attribute__((visibility("default")))
#else
#define INLINE
//  do nothing and hope for the best?
#define DLLEXP
#pragma warning Unknown inline semantics.
#pragma warning Unknown dynamic link import/export semantics.
#endif

#define CUSTOM_EXTERN_C extern "C" {
#define END_CUSTOM_EXTERN_C }
