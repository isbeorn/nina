using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Text.Json;
using System.Windows;

// Loaded only by the external startup benchmark through DOTNET_STARTUP_HOOKS.
public static class StartupHook {
    private static string resultPath = string.Empty;
    private static long startTimestamp;
    private static bool attached;

    public static void Initialize() {
        resultPath = Environment.GetEnvironmentVariable("NINA_STARTUP_RESULT") ?? string.Empty;
        if (string.IsNullOrEmpty(resultPath)) { return; }

        try {
            startTimestamp = long.Parse(Environment.GetEnvironmentVariable("NINA_STARTUP_TIMESTAMP")!);
            var stateDirectory = Environment.GetEnvironmentVariable("NINA_STARTUP_STATE")!;
            Directory.CreateDirectory(stateDirectory);
            AppDomain.CurrentDomain.UnhandledException += (_, args) => Fail(args.ExceptionObject.ToString()!);

            // Reuse the existing storage seam without changing production startup code.
            var core = Assembly.Load("NINA.Core");
            core.GetType("NINA.Core.Utility.CoreUtil", true)!
                .GetField("APPLICATIONTEMPPATH")!.SetValue(null, stateDirectory);

            var app = Assembly.Load("NINA");
            var settings = (ApplicationSettingsBase)app.GetType("NINA.Properties.Settings", true)!
                .GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
            var provider = new InMemorySettingsProvider();
            provider.Initialize("StartupBenchmark", new NameValueCollection());
            settings.Providers.Clear();
            settings.Providers.Add(provider);
            foreach (SettingsProperty property in settings.Properties) { property.Provider = provider; }
            settings.Reload();
            settings["UpdateSettings"] = false;
            settings["UseSavedProfileSelection"] = true;
            settings["DatabaseLocation"] = Path.Combine(stateDirectory, "NINA.sqlite");

            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(WindowLoaded), true);
        } catch (Exception exception) {
            Fail(exception.ToString());
        }
    }

    private static void WindowLoaded(object sender, RoutedEventArgs args) {
        if (attached || sender is not Window window || window.GetType().FullName != "NINA.MainWindow") { return; }
        attached = true;
        window.ShowInTaskbar = false;
        window.Left = -10000;
        window.Top = -10000;
        window.Width = 1280;
        window.Height = 800;

        // Exclude the remote update service from this local startup comparison.
        var versionCheck = window.DataContext.GetType().GetProperty("VersionCheckVM")!.GetValue(window.DataContext)!;
        versionCheck.GetType().GetProperty("CheckUpdateCommand")!.SetValue(versionCheck, null);
        window.ContentRendered += (_, _) => Complete();
    }

    private static void Complete() {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        using var process = Process.GetCurrentProcess();
        var result = new {
            StartupMilliseconds = elapsed.TotalMilliseconds,
            CpuMilliseconds = process.TotalProcessorTime.TotalMilliseconds,
            WorkingSetBytes = process.WorkingSet64,
            PrivateBytes = process.PrivateMemorySize64,
            JitMilliseconds = JitInfo.GetCompilationTime().TotalMilliseconds,
            JitMethods = JitInfo.GetCompiledMethodCount(),
            JitILBytes = JitInfo.GetCompiledILBytes()
        };
        WriteResult(result);
        // Avoid shutdown persistence and device teardown in the timed process.
        Environment.Exit(0);
    }

    private static void Fail(string error) {
        WriteResult(new { Error = error });
        Environment.Exit(1);
    }

    private static void WriteResult(object result) {
        // The runner may terminate native SDK shutdown once the complete result is visible.
        var temporaryPath = resultPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(result));
        File.Move(temporaryPath, resultPath);
    }

    private sealed class InMemorySettingsProvider : SettingsProvider, IApplicationSettingsProvider {
        public override string ApplicationName { get; set; } = "StartupBenchmark";

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties) {
            var values = new SettingsPropertyValueCollection();
            foreach (SettingsProperty property in properties) {
                values.Add(new SettingsPropertyValue(property) { SerializedValue = property.DefaultValue, IsDirty = false });
            }
            return values;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values) { }
        public SettingsPropertyValue? GetPreviousVersion(SettingsContext context, SettingsProperty property) => null;
        public void Reset(SettingsContext context) { }
        public void Upgrade(SettingsContext context, SettingsPropertyCollection properties) { }
    }
}
