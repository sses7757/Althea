# Althea
a linear **A**lgebra **L**ibrary for **T**ensors with **H**ighly-**E**xtendable **A**PIs written in C# (>= 8.0)

## Features
- Cross Platform and Cross Device
  - It can be run on Windows, Linux or MacOS with CPU, GPU or even FPGA (if there were proper support)
- High Flexibility : you can determine which implementation you would like to use
  -  From the lowest level like BLAS
  -  To the highest level like general eigen-solver
- Highly Modularized : composed of several parts
  - `Memory` -- native storage related classes provide a unified and easy-to-use interface for accessing memory of different devices
  - `NativeTypes` -- interfaces, implementations and helper methods for native types used to communicate with interfaces of heavy computations which also provides support for possible future real and complex types 
  - `Arrays` -- interfaces for vectors, matrices and tensors and their concrete classes
  - `LinearAlgebra` -- interfaces for dense and sparse vectors and matrices operations which actually handles the final computations and unified accessing points of them
    - `Dense`
    - `Sparse`
  - `TensorAlgebra` -- interfaces for dense and sparse tensors operations which actually handles the final computations and unified accessing points of them
    - `Dense`
    - `Sparse`
  - `Statistics` -- interfaces for random number generators and random distributions, etc which actually handles the final computations and unified accessing points of them
    - `RandomNumberGenerators`
    - `Distributions`
    - T.B.D.
  - `GeneralSolves` -- interfaces and several implementations for general and interface-based equation and eigen solvers and optimizers, also has unified accessing points
    - `EquationSolvers`
    - `Optimizers`
    - `EigenSolvers`
  - `Helpers` -- classes and methods to imporve the accessibility of other modules, also has interfaces for device information and their unified accessing points
  - `Linq` -- `System.Linq` like extend methods for `IReadOnlyList<T>` and `Span<T>` of C#
  - `Cuda` -- default implementations of linear and tensor algebra operatons using CUDA, cuTENSOR (or [CUTT](https://github.com/ap-hynninen/cutt)) and custom functions written in CUDA
  - `Mkl` -- default implementations of linear and tensor algebra operatons using MKL, [HPTT](https://github.com/springer13/hptt) and custom functions written in OpenMP
- Fully Aspect- and Interface- Oriented : from top to bottom
  - Algorithms based on interfaces of arrays
  - Interfaces for arrays (vectors, matrices and tensors)
  - Unified accessing points
  - Interfaces for operations
- High Extendability
  - **All** modules and aspects are designed to support any possible extensions and all the default implementations are written in the same regulations
  - **Each** module and aspect can be changed to custom ones **individually** during **runtime**
  - The unified accessing points are fully cached using C# delegates so that no substantial overhead will be added
- High Performance (with high-performance implementations such as the default ones)
- Thread and Memory Safe

## License
This library follows the GNU GPL v3 license

*The **CUTT** follows MIT licnese whose compilation result is only used*

*The **HPTT** follows GNU licnese whose compilation result is only used*

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
in order to start a HTTP host for receiveing debugger from IDE.

Then, you can open your Visual Studio that compiled the code, click Debug-Attach to Process-select SSH. Adjust configs and attach to something like
```bash
/usr/shared/dotnet/dotnet -XXX
```
After successfully connected, stop IDE debugging and the dotnet process on host.

Finally, compile your code and publish them to host before using
```bash
dotnet exec Your_Compiled_DLL_Name.dll
```
to run your code on host. Make sure that code like `Console.Read()` is used to wait async connections. Then do the same on Visual Studio as above, you can debug via IDE now.
