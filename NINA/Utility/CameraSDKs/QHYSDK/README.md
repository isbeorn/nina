# QHYCCD SDK Installation Instructions

The QHYCCD SDK is available from the Developer download section of QHYCCD's website: <https://www.qhyccd.com/>

Inside the Windows SDK zip file you will see both 32 and 64bit DLLs in the base folder of the archive. We want to copy the following DLL files into the `NINA/External/QHYCCD/(x86|x64)` folders:

| File          | Destination Folder                   |
|---------------|:------------------------------------ |
|`tbb.dll`      | `NINA/External/x86/QHYCCD/tbb.dll`   |
|`tbb_x64.dll`  | `NINA/External/x64/QHYCCD/tbb.dll`   |
|`ftd2xx.dll`   | `NINA/External/x86/QHYCCD/ftd2xx.dll`|
|`ftd2xx64.dll` | `NINA/External/x64/QHYCCD/ftd2xx.dll`|
|`DLLwithDebugOutput/qhyccd.dll` | `NINA/External/x86/QHYCCD/qhyccd.dll`|
|`DLLwithDebugOutput/qhyccd_x64.dll` | `NINA/External/x64/QHYCCD/qhyccd.dll`|

Do not copy the `qhyccd*.dll` file in the root folder as they are not compatible with debugging and will crash upon being loaded in a Debug build of NINA. Instead, there should be a folder named `DLLwithDebugOutput` which contains SDK DLLs which do have debugging symbols enabled.

Any `*.lib` or `*.exp` files can be ignored as they are relevant only to linking in C++ apps, which NINA is not.