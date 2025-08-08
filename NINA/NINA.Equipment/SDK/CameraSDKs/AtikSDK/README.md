# Atik Camera SDK Installation Instructions

The Atik SDK is available from the downloads section of Atik's website: <https://www.atik-cameras.com/downloads/>

Inside the SDK zip file under AtikCamerasSDK/win you will see directories for the 32 and 64bit drivers. We want to copy all three DLL files into the respective `NINA/External/Atik/(x86|x64)` folders.

In addition, the Atik SDK requires the 32 or 64bit version of the file libiomp5md.dll to be in the same folder (x86 or x64 respectively).
