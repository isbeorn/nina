using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NINA.Core.Utility;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System.ComponentModel.Composition;
using System.Reflection;

namespace NINA.Test.Plugin {

    public partial class PluginLoaderCompositionTest {

        [TearDown]
        public void ResetPlateSolverProviders() {
            PlateSolverFactory.RegisterProviders(Array.Empty<IPlateSolverProvider>());
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Load_RealProviderExportsAppearOnlyAfterSuccessfulManifestInitialization(bool failInitialization) {
            ResetPluginRuntimeFolders();
            string plugin = CreateSolverPlugin("Only", "test.composed-solver", failInitialization, true);
            var loader = CreateLoaderWithInitializedCollections();
            int changes = 0;
            EventHandler handler = (_, _) => changes++;
            PlateSolverFactory.ProvidersChanged += handler;
            try {
                await InvokeLoadPlugin(loader, plugin, 1d);
                PlateSolverFactory.Providers.Should().BeEmpty("composition and initialization must only stage providers");
                loader.Plugins.Should().ContainSingle().Which.Value.Should().Be(!failInitialization);
                await loader.Load();

                changes.Should().Be(1);
                PlateSolverFactory.Providers.Count.Should().Be(failInitialization ? 0 : 1,
                    "broken provider constructors and invalid exports must not poison the valid provider or admit failed plugins");
                if (!failInitialization) {
                    PlateSolverFactory.Providers[0].Id.Should().Be("test.composed-solver");
                }
            } finally {
                PlateSolverFactory.ProvidersChanged -= handler;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Load_DuplicateIdsFromRealPluginsAreAllExcludedInBothOrders(bool reverse) {
            ResetPluginRuntimeFolders();
            string first = CreateSolverPlugin("First", reverse ? "unique" : "conflict", false, false);
            string second = CreateSolverPlugin("Second", "conflict", false, false);
            string third = CreateSolverPlugin("Third", reverse ? "conflict" : "unique", false, false);
            var loader = CreateLoaderWithInitializedCollections();

            await InvokeLoadPlugin(loader, first, 0.3d);
            await InvokeLoadPlugin(loader, second, 0.6d);
            await InvokeLoadPlugin(loader, third, 1d);
            loader.Plugins.Should().HaveCount(3).And.OnlyContain(p => p.Value);
            await loader.Load();

            PlateSolverFactory.Providers.Should().ContainSingle().Which.Id.Should().Be("unique");
        }

        [Test]
        public async Task Load_FailedInitializationDoesNotRemoveOtherPluginsSolvers() {
            ResetPluginRuntimeFolders();
            var loader = CreateLoaderWithInitializedCollections();
            await InvokeLoadPlugin(loader, CreateSolverPlugin("First", "first", false, false), 0.3d);
            await InvokeLoadPlugin(loader, CreateSolverPlugin("Failed", "failed", true, false), 0.6d);
            await InvokeLoadPlugin(loader, CreateSolverPlugin("Last", "last", false, false), 1d);

            await loader.Load();

            PlateSolverFactory.Providers.Select(provider => provider.Id).Should().BeEquivalentTo("first", "last");
        }

        private static string CreateSolverPlugin(string name, string id, bool failInitialization, bool invalidExports) {
            string assemblyName = "SolverPlugin_" + name + "_" + Guid.NewGuid().ToString("N");
            // Loaded plugin contexts live for the process lifetime and retain their DLL files.
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "SolverPluginAssemblies", Guid.NewGuid().ToString("N"), name);
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, assemblyName + ".dll");
            string initialization = failInitialization
                ? """Task.FromException(new InvalidOperationException("initialization failed"))"""
                : "Task.CompletedTask";
            string source = $$"""
                using System;
                using System.Reflection;
                using System.ComponentModel.Composition;
                using System.Threading.Tasks;
                using NINA.Plugin.Interfaces;
                using NINA.Plugin.ManifestDefinition;
                using NINA.PlateSolving.Interfaces;
                using NINA.Profile.Interfaces;
                [assembly: AssemblyTitle("{{name}}")]
                [assembly: AssemblyCompany("NINA.Test")]
                [assembly: AssemblyVersion("1.0.0.0")]
                [assembly: AssemblyFileVersion("1.0.0.0")]
                [Export(typeof(IPluginManifest))]
                public class Manifest : PluginManifest, IPluginManifest {
                    public Manifest() {
                        Name = "{{name}}";
                        Identifier = "{{Guid.NewGuid()}}";
                        Author = "NINA.Test";
                        Version = new PluginVersion("1.0.0.0");
                        MinimumApplicationVersion = new PluginVersion("3.0.0.0");
                    }

                    public new Task Initialize() => {{initialization}};
                }

                [Export(typeof(IPlateSolverProvider))]
                public class Provider : IPlateSolverProvider {
                    public string Id => "{{id}}";

                    public string Name => "{{name}} solver";

                    public bool SupportsPrimary => true;

                    public bool SupportsBlind => true;

                    public IPlateSolver CreateSolver(IPlateSolveSettings settings) => null;
                }
                """;
            if (invalidExports) {
                source += """
                    [Export(typeof(IPlateSolverProvider))]
                    public class BrokenProvider : Provider {
                        public BrokenProvider() {
                            throw new InvalidOperationException("broken provider constructor");
                        }
                    }

                    [Export(typeof(IPlateSolverProvider))]
                    public class InvalidProvider : IPlateSolverProvider {
                        public string Id => " ";

                        public string Name => "Invalid";

                        public bool SupportsPrimary => false;

                        public bool SupportsBlind => false;

                        public IPlateSolver CreateSolver(IPlateSolveSettings settings) => null;
                    }
                    """;
            }
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Concat(new[] { typeof(PluginLoader).Assembly.Location, typeof(IPlateSolverProvider).Assembly.Location,
                    typeof(IPlateSolveSettings).Assembly.Location, typeof(ExportAttribute).Assembly.Location })
                .Distinct().Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create(assemblyName, new[] { CSharpSyntaxTree.ParseText(source) }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var result = compilation.Emit(file);
            result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
            return file;
        }
    }
}
