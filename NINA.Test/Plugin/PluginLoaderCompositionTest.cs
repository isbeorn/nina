#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Astrometry.Interfaces;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using System.Collections;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using ILogEventSink = Serilog.Core.ILogEventSink;

namespace NINA.Test.Plugin {

    [TestFixture]
    [NonParallelizable]
    public class PluginLoaderCompositionTest {

        /// <summary>
        /// Verifies that plugin composition imports sequence entities and assigns MEF metadata,
        /// resource-backed icons, plugin provenance text, and the shared symbol broker.
        /// </summary>
        [Test]
        public void Compose_AssignsMetadataAndPluginContextToSequenceEntity() {
            var resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());
            ISymbolBroker symbolBroker = Mock.Of<ISymbolBroker>();
            PluginLoader loader = CreateLoader(resourceDictionaryMock.Object, symbolBroker);
            InitializeCollections(loader);

            InvokeCompose(loader, new TypeCatalog(typeof(ExportedScienceItem), typeof(ExportedScienceUpgrader)), "Synthetic Science Plugin");

            ISequenceItem item = loader.Items.Should().ContainSingle().Subject;
            item.Should().BeOfType<ExportedScienceItem>();
            item.Name.Should().Be("Synthetic Focus Sweep");
            item.Category.Should().Be("Science Planning");
            item.Description.Should().Be("Evaluates focus drift across altitude bands." + Environment.NewLine + "(Synthetic Science Plugin)");
            item.Icon.Should().NotBeNull();
            item.SymbolBroker.Should().BeSameAs(symbolBroker);
            resourceDictionaryMock.Verify(x => x["FocusSweepSvg"], Times.Once);

            ISequenceEntityUpgrader upgrader = loader.Upgraders.Should().ContainSingle().Subject;
            upgrader.Should().BeOfType<ExportedScienceUpgrader>();
            upgrader.Name.Should().Be("Synthetic Science Upgrade");
        }

        /// <summary>
        /// Verifies that plugin entities already merged into core are filtered by plugin name and
        /// type identity so migrated users do not see duplicate Connector entries in the palette.
        /// </summary>
        [Test]
        public void AssignSequenceEntity_FiltersEntitiesMergedIntoCore() {
            var resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());
            PluginLoader loader = CreateLoader(resourceDictionaryMock.Object, Mock.Of<ISymbolBroker>());
            var imports = new[] {
                new Lazy<ISequenceItem, IDictionary<string, object>>(
                    () => new NINA.Plugins.Connector.Instructions.ConnectEquipment(),
                    CreateMetadata("Connector: Connect Equipment", "Connector duplicate", "ConnectSvg", "Connector"))
            };

            IList<ISequenceItem> items = InvokeAssignSequenceEntity(loader, imports, resourceDictionaryMock.Object, "Connector");

            items.Should().BeEmpty();
            resourceDictionaryMock.Verify(x => x[It.IsAny<string>()], Times.Never);
        }

        /// <summary>
        /// Verifies that DLLs in the plugin folder which do not reference NINA.Plugin are ignored
        /// instead of being reported as failed plugins.
        /// </summary>
        [Test]
        public async Task LoadPlugin_IgnoresManagedDllWithoutPluginReference() {
            PluginLoader loader = CreateLoaderWithInitializedCollections();
            string assemblyPath = typeof(CoreUtil).Assembly.Location;

            await InvokeLoadPlugin(loader, assemblyPath, 1d);

            loader.Plugins.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that a helper assembly which references NINA.Plugin but does not export a
        /// manifest is ignored instead of being reported as a failed plugin.
        /// </summary>
        [Test]
        public async Task LoadPlugin_IgnoresManagedDllWithoutPluginManifestExport() {
            PluginLoader loader = CreateLoaderWithInitializedCollections();
            string assemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "NINA.dll");
            PluginAssemblyReader.GrabAssemblyReferences(assemblyPath).Should().Contain("NINA.Plugin");
            var sink = new CollectingLogEventSink();
            _ = NINA.Core.Utility.Logger.IsEnabled(NINA.Core.Enum.LogLevelEnum.ERROR);
            ILogger originalLogger = Log.Logger;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(sink)
                .CreateLogger();

            try {
                await InvokeLoadPlugin(loader, assemblyPath, 1d);

                loader.Plugins.Should().BeEmpty();
                sink.Events.Should().NotContain(x =>
                    x.Level == LogEventLevel.Error
                    && x.RenderMessage().Contains(assemblyPath, StringComparison.Ordinal));
            } finally {
                (Log.Logger as IDisposable)?.Dispose();
                Log.Logger = originalLogger;
            }
        }

        /// <summary>
        /// Verifies that non-.NET files found beside plugins are treated like native dependencies
        /// and skipped without adding failed plugin manifests.
        /// </summary>
        [Test]
        public async Task LoadPlugin_IgnoresNonManagedDependencyFile() {
            PluginLoader loader = CreateLoaderWithInitializedCollections();
            string tempFile = Path.Combine(Path.GetTempPath(), "NINA.PluginLoader.NativeDependency." + Guid.NewGuid().ToString("N") + ".dll");
            try {
                File.WriteAllText(tempFile, "not a managed assembly");

                await InvokeLoadPlugin(loader, tempFile, 1d);

                loader.Plugins.Should().BeEmpty();
            } finally {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that a confirmed plugin assembly with an invalid file version is registered as
        /// failed and produces a durable error log instead of falling through the native-DLL path.
        /// </summary>
        [Test]
        public async Task LoadPlugin_InvalidAssemblyFileVersion_RegistersFailureAndLogsError() {
            PluginLoader loader = CreateLoaderWithInitializedCollections();
            string tempDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PluginLoaderCompositionTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            string invalidVersion;
            string pluginPath = CreateAssemblyWithInvalidFileVersion(tempDirectory, out invalidVersion);
            var sink = new CollectingLogEventSink();
            _ = NINA.Core.Utility.Logger.IsEnabled(NINA.Core.Enum.LogLevelEnum.ERROR);
            ILogger originalLogger = Log.Logger;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(sink)
                .CreateLogger();

            try {
                await InvokeLoadPlugin(loader, pluginPath, 1d);

                KeyValuePair<IPluginManifest, bool> failedPlugin = loader.Plugins.Should().ContainSingle().Subject;
                failedPlugin.Value.Should().BeFalse();
                failedPlugin.Key.Name.Should().Be("NINA.Test");
                failedPlugin.Key.Identifier.Should().Be("102e88d0-82e9-4da5-919a-94fc68e32f3e");
                failedPlugin.Key.Author.Should().Be("NINA.Test");
                failedPlugin.Key.Version.ToString().Should().Be(FileVersionInfo.GetVersionInfo(pluginPath).FileVersion);
                LogEvent errorEvent = sink.Events.Should().ContainSingle(x =>
                    x.Level == LogEventLevel.Error
                    && x.RenderMessage().Contains(pluginPath, StringComparison.Ordinal)
                    && x.RenderMessage().Contains(invalidVersion, StringComparison.Ordinal)
                    && x.Exception != null).Subject;
                errorEvent.Exception.Should().BeOfType<CompositionException>();
            } finally {
                (Log.Logger as IDisposable)?.Dispose();
                Log.Logger = originalLogger;
            }
        }

        /// <summary>
        /// Verifies that every assembly attribute used by the failed-plugin manifest has the same
        /// safe fallback when it is missing or malformed.
        /// </summary>
        [TestCase(nameof(AssemblyTitleAttribute), null, "BrokenPlugin", "Metadata Company", "58da11c2-738f-4628-a207-82644bedb449", "2.3.4.5")]
        [TestCase(nameof(AssemblyCompanyAttribute), null, "Metadata Name", "", "58da11c2-738f-4628-a207-82644bedb449", "2.3.4.5")]
        [TestCase(nameof(GuidAttribute), null, "Metadata Name", "Metadata Company", "C:\\Plugins\\BrokenPlugin.dll", "2.3.4.5")]
        [TestCase(nameof(AssemblyFileVersionAttribute), null, "Metadata Name", "Metadata Company", "58da11c2-738f-4628-a207-82644bedb449", "6.7.8.9")]
        [TestCase(nameof(AssemblyTitleAttribute), " ", "BrokenPlugin", "Metadata Company", "58da11c2-738f-4628-a207-82644bedb449", "2.3.4.5")]
        [TestCase(nameof(AssemblyCompanyAttribute), " ", "Metadata Name", "", "58da11c2-738f-4628-a207-82644bedb449", "2.3.4.5")]
        [TestCase(nameof(GuidAttribute), "not-a-guid", "Metadata Name", "Metadata Company", "C:\\Plugins\\BrokenPlugin.dll", "2.3.4.5")]
        [TestCase(nameof(AssemblyFileVersionAttribute), "1.2.bad.4", "Metadata Name", "Metadata Company", "58da11c2-738f-4628-a207-82644bedb449", "6.7.8.9")]
        public void CreateFailedPluginManifest_InvalidMetadata_UsesSafeFallback(
            string attribute,
            string? invalidValue,
            string expectedName,
            string expectedAuthor,
            string expectedIdentifier,
            string expectedVersion) {
            const string pluginPath = "C:\\Plugins\\BrokenPlugin.dll";
            var metadata = new Dictionary<string, string> {
                [nameof(AssemblyTitleAttribute)] = "Metadata Name",
                [nameof(AssemblyCompanyAttribute)] = "Metadata Company",
                [nameof(GuidAttribute)] = "58da11c2-738f-4628-a207-82644bedb449",
                [nameof(AssemblyFileVersionAttribute)] = "2.3.4.5"
            };
            if (invalidValue == null) {
                metadata.Remove(attribute);
            } else {
                metadata[attribute] = invalidValue;
            }

            PluginManifest manifest = InvokeCreateFailedPluginManifest(
                pluginPath,
                metadata,
                "6.7.8.9",
                "Manifest failed");

            manifest.Name.Should().Be(expectedName);
            manifest.Author.Should().Be(expectedAuthor);
            manifest.Identifier.Should().Be(expectedIdentifier);
            manifest.Version.ToString().Should().Be(expectedVersion);
            manifest.Descriptions.ShortDescription.Should().Be($"Failed to load {pluginPath}");
            manifest.Descriptions.LongDescription.Should().Be("Manifest failed");
        }

        /// <summary>
        /// Verifies strict four-part version parsing, including the minimum and maximum supported
        /// component values and the zero-version fallback for malformed candidates.
        /// </summary>
        [TestCase("0.0.0.0", "9.8.7.6", "0.0.0.0")]
        [TestCase("2147483647.2147483647.2147483647.2147483647", "9.8.7.6", "2147483647.2147483647.2147483647.2147483647")]
        [TestCase("1.2.3", "4.5.6", "0.0.0.0")]
        [TestCase("1.2.bad.4", "5.6.bad.8", "0.0.0.0")]
        [TestCase(null, null, "0.0.0.0")]
        public void CreateFailedPluginManifest_VersionCandidates_RequireFourNumericParts(
            string? metadataVersion,
            string? fileVersion,
            string expectedVersion) {
            var metadata = new Dictionary<string, string>();
            if (metadataVersion != null) {
                metadata[nameof(AssemblyFileVersionAttribute)] = metadataVersion;
            }

            PluginManifest manifest = InvokeCreateFailedPluginManifest(
                "C:\\Plugins\\BrokenPlugin.dll",
                metadata,
                fileVersion,
                "Manifest failed");

            manifest.Version.ToString().Should().Be(expectedVersion);
        }

        /// <summary>
        /// Verifies that a full loader startup against an isolated runtime root initializes plugin
        /// collections and completes without installed plugins, matching a clean first-run profile.
        /// </summary>
        [Test]
        public async Task Load_WithEmptyPluginRuntimeRoot_InitializesCollectionsWithoutInstalledPlugins() {
            PluginLoader loader = CreateLoaderWithInitializedResourceDictionary();

            await loader.Load();

            loader.Plugins.Should().NotBeNull();
            loader.Plugins.Should().BeEmpty();
            loader.Items.Should().NotBeNull();
            loader.Conditions.Should().NotBeNull();
            loader.Triggers.Should().NotBeNull();
            loader.Container.Should().NotBeNull();
            loader.Upgraders.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that startup migrates plugin files from the highest older versioned plugin
        /// folder into the current plugin API folder before scanning installed plugins.
        /// </summary>
        [Test]
        public async Task Load_MigratesHighestOlderVersionedPluginFolder() {
            ResetPluginRuntimeFolders();
            string previousVersionFolder = Path.Combine(Constants.BaseUserExtensionsFolder, "1.0.0");
            string previousPluginFile = Path.Combine(previousVersionFolder, "NativeDependency.dll");
            Directory.CreateDirectory(previousVersionFolder);
            File.WriteAllText(previousPluginFile, "native dependency placeholder");
            PluginLoader loader = CreateLoaderWithInitializedResourceDictionary();

            await loader.Load();

            string migratedPluginFile = Path.Combine(Constants.UserExtensionsFolder, "NativeDependency.dll");
            File.Exists(migratedPluginFile).Should().BeTrue();
            File.ReadAllText(migratedPluginFile).Should().Be("native dependency placeholder");
        }

        /// <summary>
        /// Verifies that the loader's AppDomain fallback resolver can return assemblies already
        /// loaded for earlier plugins when a later plugin asks for the same dependency by name.
        /// </summary>
        [Test]
        public void CurrentDomainAssemblyResolve_ReturnsAssemblyBySimpleName() {
            PluginLoader loader = CreateLoaderWithInitializedCollections();
            Assembly expectedAssembly = typeof(PluginLoader).Assembly;
            loader.Assemblies.Add(expectedAssembly);
            var args = new ResolveEventArgs(expectedAssembly.GetName().FullName);

            MethodInfo method = typeof(PluginLoader).GetMethod("CurrentDomain_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assembly resolvedAssembly = (Assembly)method.Invoke(loader, new object[] { this, args })!;

            resolvedAssembly.Should().BeSameAs(expectedAssembly);
        }

        /// <summary>
        /// Verifies that the plugin assembly load context can resolve a dependency from the plugin
        /// directory by simple assembly name.
        /// </summary>
        [Test]
        public void PluginAssemblyLoadContext_ResolvesDependencyFromPluginDirectory() {
            string fixtureAssembly = typeof(PluginLoader).Assembly.Location;
            Type contextType = typeof(PluginLoader).GetNestedType("PluginAssemblyLoadContext", BindingFlags.NonPublic)!;
            var context = (AssemblyLoadContext)Activator.CreateInstance(contextType, "plugin-resolver", fixtureAssembly, true)!;
            try {
                MethodInfo method = contextType.GetMethod("PluginAssemblyLoadContext_Resolving", BindingFlags.Instance | BindingFlags.NonPublic)!;

                Assembly resolvedAssembly = (Assembly)method.Invoke(context, new object[] { context, new AssemblyName("NINA.Plugin") })!;

                resolvedAssembly.GetName().Name.Should().Be("NINA.Plugin");
            } finally {
                context.Unload();
            }
        }

        /// <summary>
        /// Verifies that the plugin assembly load context returns null for dependencies that do not
        /// exist beside the plugin assembly.
        /// </summary>
        [Test]
        public void PluginAssemblyLoadContext_ReturnsNullForMissingDependency() {
            string fixtureAssembly = typeof(PluginLoader).Assembly.Location;
            Type contextType = typeof(PluginLoader).GetNestedType("PluginAssemblyLoadContext", BindingFlags.NonPublic)!;
            var context = (AssemblyLoadContext)Activator.CreateInstance(contextType, "plugin-missing-resolver", fixtureAssembly, true)!;
            try {
                MethodInfo method = contextType.GetMethod("PluginAssemblyLoadContext_Resolving", BindingFlags.Instance | BindingFlags.NonPublic)!;

                Assembly resolvedAssembly = (Assembly)method.Invoke(context, new object[] { context, new AssemblyName("Definitely.Missing.Plugin.Dependency") })!;

                resolvedAssembly.Should().BeNull();
            } finally {
                context.Unload();
            }
        }

        /// <summary>
        /// Verifies that repeated load calls are idempotent once initialization is complete and do
        /// not recompose plugin state.
        /// </summary>
        [Test]
        public async Task Load_ReturnsWithoutWorkWhenAlreadyInitialized() {
            PluginLoader loader = CreateLoaderWithInitializedCollections();
            FieldInfo initializedField = typeof(PluginLoader).GetField("initialized", BindingFlags.Instance | BindingFlags.NonPublic)!;
            initializedField.SetValue(loader, true);

            await loader.Load();

            loader.Plugins.Should().BeEmpty();
            loader.Items.Should().BeEmpty();
        }

        private static IDictionary<string, object> CreateMetadata(string name, string description, string icon, string category) {
            return new Dictionary<string, object> {
                ["Name"] = name,
                ["Description"] = description,
                ["Icon"] = icon,
                ["Category"] = category
            };
        }

        private static string CreateAssemblyWithInvalidFileVersion(string destinationDirectory, out string invalidVersion) {
            string sourcePath = typeof(PluginLoaderCompositionTest).Assembly.Location;
            Dictionary<string, string> metadata = PluginAssemblyReader.GrabPluginMetaData(sourcePath);
            string validVersion = metadata[nameof(AssemblyFileVersionAttribute)];
            invalidVersion = validVersion.Substring(0, validVersion.Length - 1) + "x";
            byte[] assemblyBytes = File.ReadAllBytes(sourcePath);
            byte[] validVersionBytes = Encoding.UTF8.GetBytes(validVersion);
            byte[] invalidVersionBytes = Encoding.UTF8.GetBytes(invalidVersion);
            validVersionBytes.Length.Should().BeLessThan(128, "the test fixture uses the single-byte serialized string length form");
            invalidVersionBytes.Length.Should().Be(validVersionBytes.Length);

            byte[] attributeBlob = new byte[validVersionBytes.Length + 5];
            attributeBlob[0] = 0x01;
            attributeBlob[1] = 0x00;
            attributeBlob[2] = (byte)validVersionBytes.Length;
            Array.Copy(validVersionBytes, 0, attributeBlob, 3, validVersionBytes.Length);
            int attributeOffset = FindUniqueSequence(assemblyBytes, attributeBlob);
            Array.Copy(invalidVersionBytes, 0, assemblyBytes, attributeOffset + 3, invalidVersionBytes.Length);

            string destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
            File.WriteAllBytes(destinationPath, assemblyBytes);
            PluginAssemblyReader.GrabPluginMetaData(destinationPath)[nameof(AssemblyFileVersionAttribute)].Should().Be(invalidVersion);
            return destinationPath;
        }

        private static int FindUniqueSequence(byte[] source, byte[] sequence) {
            var matches = new List<int>();
            for (int i = 0; i <= source.Length - sequence.Length; i++) {
                bool match = true;
                for (int j = 0; j < sequence.Length; j++) {
                    if (source[i + j] != sequence[j]) {
                        match = false;
                        break;
                    }
                }

                if (match) {
                    matches.Add(i);
                }
            }

            return matches.Should().ContainSingle().Subject;
        }

        private static void InvokeCompose(PluginLoader loader, ComposablePartCatalog catalog, string pluginName) {
            MethodInfo method = typeof(PluginLoader).GetMethod("Compose", BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(loader, new object[] { catalog, pluginName });
        }

        private static async Task InvokeLoadPlugin(PluginLoader loader, string file, double progress) {
            MethodInfo method = typeof(PluginLoader).GetMethod("LoadPlugin", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var task = (Task)method.Invoke(loader, new object[] { file, progress })!;
            await task;
        }

        private static PluginManifest InvokeCreateFailedPluginManifest(
            string file,
            IReadOnlyDictionary<string, string> metadata,
            string fileVersion,
            string message) {
            MethodInfo method = typeof(PluginLoader).GetMethod("CreateFailedPluginManifest", BindingFlags.Static | BindingFlags.NonPublic)!;
            return (PluginManifest)method.Invoke(null, new object[] { file, metadata, fileVersion, message })!;
        }

        private static IList<ISequenceItem> InvokeAssignSequenceEntity(
            PluginLoader loader,
            IEnumerable<Lazy<ISequenceItem, IDictionary<string, object>>> imports,
            IApplicationResourceDictionary resourceDictionary,
            string pluginName) {
            MethodInfo method = typeof(PluginLoader).GetMethod("AssignSequenceEntity", BindingFlags.Instance | BindingFlags.NonPublic)!;
            MethodInfo genericMethod = method.MakeGenericMethod(typeof(ISequenceItem));
            IEnumerable result = (IEnumerable)genericMethod.Invoke(loader, new object[] { imports, resourceDictionary, pluginName })!;
            return result.Cast<ISequenceItem>().ToList();
        }

        private static void InitializeCollections(PluginLoader loader) {
            SetProperty(loader, nameof(PluginLoader.Items), new List<ISequenceItem>());
            SetProperty(loader, nameof(PluginLoader.Conditions), new List<NINA.Sequencer.Conditions.ISequenceCondition>());
            SetProperty(loader, nameof(PluginLoader.Triggers), new List<NINA.Sequencer.Trigger.ISequenceTrigger>());
            SetProperty(loader, nameof(PluginLoader.Container), new List<NINA.Sequencer.Container.ISequenceContainer>());
            SetProperty(loader, nameof(PluginLoader.DockableVMs), new List<IDockableVM>());
            SetProperty(loader, nameof(PluginLoader.PluggableBehaviors), new List<IPluggableBehavior>());
            SetProperty(loader, nameof(PluginLoader.DeviceProviders), new List<IEquipmentProvider>());
            SetProperty(loader, nameof(PluginLoader.Plugins), new Dictionary<IPluginManifest, bool>());
            SetProperty(loader, nameof(PluginLoader.Upgraders), new List<ISequenceEntityUpgrader>());
        }

        private static void SetProperty(PluginLoader loader, string propertyName, object value) {
            PropertyInfo property = typeof(PluginLoader).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!;
            property.SetValue(loader, value);
        }

        private static void ResetPluginRuntimeFolders() {
            DeleteDirectoryIfExists(Constants.BaseUserExtensionsFolder);
            DeleteDirectoryIfExists(Constants.BaseStagingFolder);
            DeleteDirectoryIfExists(Constants.BaseDeletionFolder);
        }

        private static void DeleteDirectoryIfExists(string directory) {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, true);
            }
        }

        private static PluginLoader CreateLoaderWithInitializedCollections() {
            PluginLoader loader = CreateLoaderWithInitializedResourceDictionary();
            InitializeCollections(loader);
            return loader;
        }

        private static PluginLoader CreateLoaderWithInitializedResourceDictionary() {
            var resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());
            return CreateLoader(resourceDictionaryMock.Object, Mock.Of<ISymbolBroker>());
        }

        private static PluginLoader CreateLoader(IApplicationResourceDictionary resourceDictionary, ISymbolBroker symbolBroker) {
            var applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            applicationStatusMediatorMock
                .Setup(x => x.StatusUpdate(It.IsAny<ApplicationStatus>()))
                .Verifiable();

            return new PluginLoader(
                Mock.Of<IProfileService>(),
                Mock.Of<ICameraMediator>(),
                Mock.Of<ITelescopeMediator>(),
                Mock.Of<IFocuserMediator>(),
                Mock.Of<IFilterWheelMediator>(),
                Mock.Of<IGuiderMediator>(),
                Mock.Of<IRotatorMediator>(),
                Mock.Of<IFlatDeviceMediator>(),
                Mock.Of<IWeatherDataMediator>(),
                Mock.Of<IImagingMediator>(),
                applicationStatusMediatorMock.Object,
                Mock.Of<INighttimeCalculator>(),
                Mock.Of<IPlanetariumFactory>(),
                Mock.Of<IImageHistoryVM>(),
                Mock.Of<IDeepSkyObjectSearchVM>(),
                Mock.Of<IDomeMediator>(),
                Mock.Of<IImageSaveMediator>(),
                Mock.Of<ISwitchMediator>(),
                Mock.Of<ISafetyMonitorMediator>(),
                resourceDictionary,
                Mock.Of<IApplicationMediator>(),
                Mock.Of<IFramingAssistantVM>(),
                Mock.Of<IPlateSolverFactory>(),
                Mock.Of<IWindowServiceFactory>(),
                Mock.Of<IDomeFollower>(),
                Mock.Of<IPluggableBehaviorSelector<IStarDetection>>(),
                Mock.Of<IPluggableBehaviorSelector<IStarAnnotator>>(),
                Mock.Of<IImageDataFactory>(),
                Mock.Of<IMeridianFlipVMFactory>(),
                Mock.Of<IAutoFocusVMFactory>(),
                Mock.Of<IImageControlVM>(),
                Mock.Of<IImageStatisticsVM>(),
                Mock.Of<IDomeSynchronization>(),
                Mock.Of<ISequenceMediator>(),
                Mock.Of<IOptionsVM>(),
                Mock.Of<IExposureDataFactory>(),
                Mock.Of<ITwilightCalculator>(),
                Mock.Of<IMessageBroker>(),
                symbolBroker,
                Mock.Of<ITemplateLinkResolver>());
        }

        private sealed class CollectingLogEventSink : ILogEventSink {
            public IList<LogEvent> Events { get; } = new List<LogEvent>();

            public void Emit(LogEvent logEvent) {
                Events.Add(logEvent);
            }
        }

        [Export(typeof(IPluginManifest))]
        public class ExportedTestPluginManifest : PluginManifest {
            public ExportedTestPluginManifest() {
                string version = GetType().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
                Version = new PluginVersion(version);
            }
        }

        [ExportMetadata("Name", "Synthetic Focus Sweep")]
        [ExportMetadata("Description", "Evaluates focus drift across altitude bands.")]
        [ExportMetadata("Icon", "FocusSweepSvg")]
        [ExportMetadata("Category", "Science Planning")]
        [Export(typeof(ISequenceItem))]
        public class ExportedScienceItem : SequenceItem {
            public override object Clone() {
                return new ExportedScienceItem();
            }

            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                return Task.CompletedTask;
            }
        }

        [ExportMetadata("Name", "Synthetic Science Upgrade")]
        [Export(typeof(ISequenceEntityUpgrader))]
        public class ExportedScienceUpgrader : ISequenceEntityUpgrader {
            public string Name { get; set; } = string.Empty;
            public SequenceUpgradeStage Stages => SequenceUpgradeStage.AfterPopulate;

            public object Upgrade(SequenceUpgradeContext context, SequenceUpgradeStage stage, object? current) {
                return current;
            }
        }
    }
}

namespace NINA.Plugins.Connector.Instructions {
    using NINA.Core.Model;
    using NINA.Sequencer.SequenceItem;

    public class ConnectEquipment : SequenceItem {
        public override object Clone() {
            return new ConnectEquipment();
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.CompletedTask;
        }
    }
}
