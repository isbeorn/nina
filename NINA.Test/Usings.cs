global using NUnit.Framework;
using NINA.Core.Utility;

[SetUpFixture]
public class Init {
    [OneTimeSetUp]
    public void LoadAllNativeDlls() {
        Console.WriteLine("Preloading Native DLLs.");
        Console.WriteLine(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x64", Path.Combine("SOFA", "SOFAlib.dll")));
        DllLoader.LoadDll(Path.Combine("SOFA", "SOFAlib.dll"));
        Console.WriteLine(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x64", Path.Combine("NOVAS", "NOVAS31lib.dll")));
        DllLoader.LoadDll(Path.Combine("NOVAS", "NOVAS31lib.dll"));
        Console.WriteLine("Native DLLs preloaded.");
    }
}