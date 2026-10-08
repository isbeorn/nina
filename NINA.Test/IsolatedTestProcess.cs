using FluentAssertions;
using System.Diagnostics;
using System.Xml.Linq;

namespace NINA.Test {
    internal static class IsolatedTestProcess {
        // The parent verifies the child's result; the child returns false to execute the test body.
        public static async Task<bool> RunCurrentTest() {
            const string marker = "NINA_ISOLATED_TEST";
            string testName = TestContext.CurrentContext.Test.FullName;
            if (Environment.GetEnvironmentVariable(marker) == testName) return false;

            string results = Path.Combine(Path.GetTempPath(), "NINA-isolated-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(results);
            try {
                var start = new ProcessStartInfo("dotnet") {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                start.ArgumentList.Add("vstest");
                start.ArgumentList.Add(typeof(IsolatedTestProcess).Assembly.Location);
                string escapedName = testName.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
                start.ArgumentList.Add($"/TestCaseFilter:FullyQualifiedName={escapedName}");
                start.ArgumentList.Add($"/ResultsDirectory:{results}");
                start.ArgumentList.Add("/Logger:trx;LogFileName=result.trx");
                start.Environment[marker] = testName;
                using var child = Process.Start(start)!;
                Task<string> output = child.StandardOutput.ReadToEndAsync();
                Task<string> error = child.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                try {
                    await child.WaitForExitAsync(timeout.Token);
                } finally {
                    if (!child.HasExited) {
                        child.Kill(entireProcessTree: true);
                        await child.WaitForExitAsync();
                    }
                }
                child.ExitCode.Should().Be(0, $"isolated test must pass:\n{await output}\n{await error}");
                var counters = XDocument.Load(Path.Combine(results, "result.trx")).Descendants()
                    .Single(element => element.Name.LocalName == "Counters");
                ((int?)counters.Attribute("total")).Should().Be(1, "the child must select exactly this test case");
                ((int?)counters.Attribute("passed")).Should().Be(1, "an empty or skipped child run is not a passing test");
                return true;
            } finally {
                Directory.Delete(results, recursive: true);
            }
        }
    }
}