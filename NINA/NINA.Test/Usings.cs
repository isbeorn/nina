global using NUnit.Framework;
using NINA.Core.Utility;

[SetUpFixture]
public class Init {
    [OneTimeSetUp]
    public void LoadAllNativeDlls() {
        Logger.Info($"Preloading Native DLLs. The environment is x64: {Environment.Is64BitProcess}");
        Logger.Info(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x64", Path.Combine("SOFA", "SOFAlib.dll")));
        DllLoader.LoadDll(Path.Combine("SOFA", "SOFAlib.dll"));
        Logger.Info(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", "x64", Path.Combine("NOVAS", "NOVAS31lib.dll")));
        DllLoader.LoadDll(Path.Combine("NOVAS", "NOVAS31lib.dll"));
        Logger.Info("Native DLLs preloaded.");
    }
}