# Althea
a linear **A**lgebra **L**ibrary for **T**ensors with **H**ighly-**E**xtendable **A**PIs

It is written in C# (>= 9.0) and mainly focuses on general purposed scientific computations. It is designed to fulfill the high performance, user-friendliness as well as high extendibility at the same time.

## Features
- Cross Platform and Cross Device
  - It can be run on Windows, Linux or MacOS with CPU, GPU or even FPGA (if there were proper support)
- High Flexibility : you can determine which implementation you would like to use
  -  From the lowest level like BLAS
  -  To the highest level like general eigen-solver
- Highly Modularized : composed of several parts
  - `Althea` -- basic definitions such as the abstract runtime API class to be inherited by all APIs in all modules and the abstract storage structures and classes used across the whole library are defined in the base namespce `Althea`
  - `Althea.Storage` -- native storage related structures and classes which provide a unified and easy-to-use interface for accessing and manipulating memory blocks and local/remote files on different devices
  - `Althea.NativeTypes` -- interfaces, implementations and helper methods for native types used to communicate with interfaces of heavy computations which also provides support for possible future real and complex types 
  - `Althea.Arrays` -- interfaces for vectors, matrices and tensors and their concrete classes
    - `Vectors`
    - `Matrices`
    - `Tensors`
  - `Althea.LinearAlgebra` -- interfaces for dense and sparse vectors and matrices operations which actually handles the final computations and unified accessing points of them
    - `Dense`
    - `Sparse`
  - `Althea.TensorAlgebra` -- interfaces for dense and sparse tensors operations which actually handles the final computations and unified accessing points of them
    - `Dense`
    - `Sparse`
  - `Althea.Statistics` -- interfaces for random number generators and random distributions, etc which actually handles the final computations and unified accessing points of them
    - `RandomNumberGenerators`
    - `Distributions`
    - T.B.D.
  - `Althea.Solves` -- interfaces and several implementations for general and interface-based equation and eigen solvers and optimizers, also has unified accessing points
    - `EquationSolvers`
    - `Optimizers`
    - `EigenSolvers`
  - `Althea.Helpers` -- classes and methods to improve the accessibility of other modules, also has interfaces for device information and their unified accessing points
  - `Althea.Linq` -- `System.Linq` like extend methods for `IReadOnlyList<T>` and `Span<T>` of C#
  - `Althea.Backend.CSharp` -- default implementations of storage, linear algebra operations and random number generators using only C# language to make sure the basic functionalities of this library works in case both CUDA and MKL and other custom backends are not available
  - `Althea.Backend.Cuda` -- default implementations of storage, linear and tensor algebra operations and random number generators using CUDA, cuTENSOR (or [CUTT](https://github.com/ap-hynninen/cutt) when cuTENSOR is not available) and custom functions written in CUDA
  - `Althea.Backend.Mkl` -- default implementations of storage, linear and tensor algebra operations and random number generators using MKL, [HPTT](https://github.com/springer13/hptt) and custom functions written in OpenMP
- Fully Aspect- and Interface- Oriented : from top to bottom
  - Algorithms based on interfaces such as the Lanczos, Krylov-Schur and GMERS algorithms
  - Unified accessing points
  - Interfaces for operations and arrays
  - Implementations on different platforms and devices
  - Native codes
- High Extendability
  - **All** modules and aspects are designed to support any possible extensions in the future and all the default implementations are written in the same regulations
  - **Each** module and aspect can be changed to custom ones **individually** during **runtime**
- High Performance (with high-performance implementations such as the default ones)
  - The unified accessing points are fully cached using C# delegates so that there will be no substantial overhead
- Thread and Memory Safe

## License
This library follows the GNU GPL v3 license

*The **CUTT** follows MIT license. But this library only needs its compilation result.*

*The **HPTT** follows GNU license But this library only needs its compilation result.*

## How To Use
### Introduction
```C#
// TODO
```

### Select a Different Implementation
```C#
// TODO
```

### Writing Your Own Implementation
```C#
// TODO
```

## Remote Debugging from Visual Studio on Windows (IDE) to Ubuntu (remote host)
First, make sure `openssh-server`, `unzip` and `curl` are installed on host.

Then, find somewhere to run bash code
```bash
mkdir coredemo
cd coredemo
dotnet new web
dotnet restore
dotnet run
```
in order to start a HTTP host for receiving debugger from IDE.

Then, you can open your Visual Studio that compiled the code, click Debug-Attach to Process-select SSH. Adjust configurations and attach to something like
```bash
/usr/shared/dotnet/dotnet -XXX
```
After successfully connected, stop IDE debugging and the dotnet process on host.

Finally, compile your code and publish them to host before using
```bash
dotnet exec Your_Compiled_DLL_Name.dll
```
to run your code on host. Make sure that code like `Console.Read()` is used to wait asynchronous connections. Then do the same on Visual Studio as above, you can debug via IDE now.
