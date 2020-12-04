# CudaCSharp
The C# library of linear algebra of matrices and tensors based on NVIDIA CUDA and Intel MKL

## Library Denpendency
> https://github.com/mathnet/mathnet-numerics

> https://www.newtonsoft.com/json

> https://github.com/ap-hynninen/cutt

> https://github.com/springer13/hptt

## License
This library follows the GNU GPL v3 license

*The denpendencies **cutt** and **Newtonsoft.Json** follows MIT licnese*

*The denpendency **hptt** follows GNU licnese*


## Remote Debugging from Visual Studio on Windows (IDE) to Ubuntu (remote host)
First, make sure `openssh-server`, `unzip` and `curl` are installed on host.

Then, find somewhere to run bash code
```
mkdir coredemo
cd coredemo
dotnet new web
dotnet restore
dotnet run
```
in order to start a HTTP host for receiveing debugger from IDE.

Then, you can open your Visual Studio that compiled the code, click Debug-Attach to Process-select SSH. Adjust configs and attach to something like
```
/usr/shared/dotnet/dotnet -XXX
```
After successfully connected, stop IDE debugging and the dotnet process on host.

Finally, compile your code and publish them to host before using
```
dotnet exec Your_Compiled_DLL_Name.dll
```
to run your code on host. Make sure that code like `Console.Read();` is used for waiting. Then do the same on Visual Studio as above, you can debug via IDE now.
