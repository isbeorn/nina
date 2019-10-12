# FLI SDK Installation Instructions

The FLI SDK is available from the Support download section of FLI's website: <http://flicamera.com/software/index.html>

1. Under the section "Software (except for Kepler)", Download the "Windows Binaries (`version`)" zip file.
2. Inside the `libfli-<version>-winbin.zip` file you will see both 32 and 64bit DLLs in the base folder of the archive. We want to copy the following DLL files into the `NINA/External/FLI/(x86|x64)` folders:

| File          | Destination Folder                 |
|---------------|:---------------------------------- |
|`libfli.dll`   | `NINA/External/x86/FLI/libfli.dll` |
|`libfli64.dll` | `NINA/External/x64/FLI/libfli.dll` |

Any `*.lib` or `*.exp` files can be ignored as they are relevant only to linking in C++ apps, which NINA is not.