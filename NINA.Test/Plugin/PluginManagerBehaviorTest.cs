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
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.Profile.Interfaces;
using NINA.ViewModel.Plugins;
using NUnit.Framework;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Test.Plugin {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class PluginManagerBehaviorTest {
        private Application? originalApplication;

        [SetUp]
        public void PreserveApplication() {
            originalApplication = Application.Current;
        }

        [TearDown]
        public void VerifyApplicationIsUnchanged() {
            Application.Current.Should().BeSameAs(originalApplication,
                "plugin manager behavior tests must not create process-wide WPF application state");
        }

        [Test]
        public void UpdateAll_CancelAfterOneSuccess_KeepsRestartRequiredWithRemainingUpdates() {
            var sut = CreateManager();
            var payload = Encoding.UTF8.GetBytes("plugin payload");
            using var firstServer = new LoopbackHttpServer(payload, fileName: "First.dll");
            var responseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var secondServer = new LoopbackHttpServer(payload, fileName: "Second.dll", responseGate: responseGate.Task);
            var first = Manifest("First", firstServer.Url, payload);
            var second = Manifest("Second", secondServer.Url, payload);
            string firstFolder = Path.Combine(Constants.UserExtensionsFolder, first.Name);
            string secondFolder = Path.Combine(Constants.UserExtensionsFolder, second.Name);
            Directory.CreateDirectory(firstFolder);
            Directory.CreateDirectory(secondFolder);
            File.WriteAllText(Path.Combine(firstFolder, "OldFirst.dll"), "old payload");
            File.WriteAllText(Path.Combine(secondFolder, "OldSecond.dll"), "old payload");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> {
                [Installed(first)] = true,
                [Installed(second)] = true
            };
            sut.AvailablePlugins = new List<ExtendedPluginManifest> {
                new(first) { State = PluginState.UpdateAvailable },
                new(second) { State = PluginState.UpdateAvailable }
            };

            try {
                Task operation = sut.UpdateAllPluginsCommand.ExecuteAsync(null);
                PumpUntil(() => sut.AvailablePlugins[0].State == PluginState.InstalledAndRequiresRestart);
                sut.CancelInstallPluginCommand.Execute(null);
                PumpUntil(() => operation.IsCompleted);
                operation.GetAwaiter().GetResult();

                sut.UpdateAllPluginsRequiresRestart.Should().BeTrue();
                sut.AvailablePluginUpdateCount.Should().Be(1);
                sut.AvailablePlugins[1].State.Should().Be(PluginState.UpdateAvailable);
            } finally {
                responseGate.TrySetResult();
                DeleteFolder(firstFolder);
                DeleteFolder(secondFolder);
            }
        }

        [Test]
        public void Filters_UseVisibleManifestMetadataAndKeepInstalledIdentityAndDiagnostics() {
            var sut = CreateManager();
            var catalog = Manifest("Focus Catalog", tags: ["Focus", "Automation"]);
            var support = Manifest("Support", tags: ["Focus"]);
            support.Author = "Support";
            var installed = Installed(catalog);
            installed.Name = "Local Focus";
            installed.Author = "Local Observatory";
            installed.Tags = ["Local"];
            var missing = Manifest("Missing Catalog");
            missing.Descriptions = new PluginDescription { ShortDescription = "Could not load assembly", LongDescription = "Missing dependency" };
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true, [missing] = false };
            sut.AvailablePlugins = [new(catalog), new(support)];

            var entry = sut.FilteredPlugins.Single(value => value.Identifier == catalog.Identifier);
            entry.DisplayManifest.Should().BeSameAs(entry.AvailableManifest);
            entry.SettingsManifest.Should().BeSameAs(installed);
            sut.SelectedTags.Add(sut.AvailableTags.Single(tag => tag.Name == "Focus"));
            sut.SearchText = "OBSERVATORY";
            sut.HasResults.Should().BeTrue();
            sut.SelectedPlugin.Should().BeSameAs(entry);

            sut.SelectedTabIndex = 0;
            entry.DisplayManifest.Should().BeSameAs(installed);
            sut.AvailableTags.Should().BeEmpty("local-only tags remain searchable but do not meet the catalog threshold");
            sut.SelectedTags.Should().BeEmpty();
            sut.HasResults.Should().BeTrue();
            sut.ClearFiltersCommand.Execute(null);
            sut.FilteredPlugins.Should().HaveCount(2);
            var failed = sut.FilteredPlugins.Single(value => value.Identifier == missing.Identifier);
            failed.HasLoadError.Should().BeTrue();
            failed.LoadError.Should().Be("Missing dependency");
            failed.CanShowSettings.Should().BeFalse();
            failed.SettingsManifest.Should().BeNull();
            failed.CanUninstall.Should().BeTrue();

            sut.SearchText = "does not match";
            sut.HasResults.Should().BeFalse();
            sut.SelectedPlugin.Should().BeNull();
            sut.ClearFiltersCommand.Execute(null);
            sut.FilteredPlugins.Should().HaveCount(2).And.Contain(entry);
            sut.SelectedTabIndex = 1;
            sut.FilteredPlugins.Should().ContainSingle().Which.Should().BeSameAs(entry);
            sut.ShowAvailableCommand.Execute(null);
            sut.IsInstalledTab.Should().BeFalse();
        }

        [Test]
        public void Tags_OnlyOfferTagsSharedByDistinctCompatibleCatalogPlugins() {
            var sut = CreateManager();
            var first = Manifest("Alpha", tags: ["Imaging", " Focus ", "focus", "Rare"]);
            var second = Manifest("Beta", tags: ["FOCUS"]);
            var third = Manifest("Gamma", tags: ["Imaging"]);
            var older = Manifest("Older", tags: ["Historical", "Rare"]);
            older.Identifier = first.Identifier;
            older.Version = new PluginVersion("1.0.0.0");
            var future = Manifest("Future", tags: ["Rare", "Future"]);
            future.MinimumApplicationVersion = new PluginVersion("99.0.0.0");
            using var catalog = Catalog(first, second, third, older, future);

            Complete(Refresh(sut, catalog.Url));

            sut.AvailablePlugins.Should().HaveCount(3);
            sut.AvailableTags.Select(tag => (tag.Name, tag.Count)).Should().Equal(("Focus", 2), ("Imaging", 2));
            sut.SearchText = "Rare";
            sut.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(first.Identifier);
            sut.AvailableTags.Select(tag => tag.Count).Should().Equal(new[] { 2, 2 }, "search does not change catalog counts");
            var localOnly = Manifest("Local only", tags: ["Focus", "LocalRare"]);
            var localFirst = Installed(first);
            localFirst.Tags = [" Focus ", "Rare"];
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [localFirst] = true, [localOnly] = true };
            sut.SelectedTabIndex = 0;
            sut.AvailableTags.Should().ContainSingle().Which.Should().Be(new PluginTag("Focus", 2));
            sut.SearchText = "LocalRare";
            sut.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(localOnly.Identifier);
        }

        [Test]
        public void Tags_CountTheUnfilteredActiveSectionAndRetainSelectionAcrossCountChanges() {
            var sut = CreateManager();
            var first = Manifest("First", tags: [" Web ", "web", "Rare"]);
            var second = Manifest("Second", tags: ["web"]);
            var third = Manifest("Third", tags: ["WEB"]);
            var local = Installed(first);
            var failed = Manifest("Local failed", tags: ["Web", "WEB"]);
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [local] = true };
            sut.AvailablePlugins = [new(first), new(second), new(third)];
            sut.SearchText = first.Name;
            sut.SelectedTags.Add(sut.AvailableTags.Single());
            sut.FilteredPlugins.Should().ContainSingle();
            sut.AvailableTags.Single().Should().Be(new PluginTag("Web", 3));

            sut.SelectedTabIndex = 0;

            sut.AvailableTags.Single().Should().Be(new PluginTag("Web", 1));
            sut.SelectedTags.Add(sut.AvailableTags.Single());
            sut.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(first.Identifier);
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [local] = true, [failed] = false };
            sut.SelectedTags.Single().Should().Be(new PluginTag("Web", 2));
            sut.FilteredPlugins.Should().HaveCount(2);
            sut.SearchText = first.Name;
            sut.FilteredPlugins.Should().ContainSingle();
            sut.AvailableTags.Single().Count.Should().Be(2, "search does not change the active section's tag counts");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [local] = true };
            sut.SelectedTags.Single().Should().Be(new PluginTag("Web", 1));
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool>();
            sut.AvailableTags.Should().BeEmpty();
            sut.SelectedTags.Should().BeEmpty();

            sut.SelectedTabIndex = 1;

            sut.SearchText.Should().Be(first.Name);
            sut.SelectedTags.Single().Should().Be(new PluginTag("Web", 3));
            sut.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(first.Identifier);
        }

        [Test]
        public void Tags_UseAnOrFilterIntersectSearchAndPruneWithdrawnChoicesOnRefresh() {
            var sut = CreateManager();
            var first = Manifest("Alpha", tags: ["Focus", "focus"]);
            var second = Manifest("Beta", tags: ["Tools"]);
            var shared = Manifest("Gamma", tags: ["Focus", "Tools", "Imaging"]);
            var untagged = Manifest("Untagged");
            untagged.Tags = null;
            untagged.Descriptions = null;
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [Installed(first)] = true };
            sut.AvailablePlugins = [new(first), new(second), new(shared), new(untagged)];
            sut.AvailableTags.Select(tag => tag.Name).Should().Equal("Focus", "Tools");
            sut.SelectedTags.Add(sut.AvailableTags[0]);
            sut.SelectedTags.Add(sut.AvailableTags[1]);
            sut.SelectedTags.Add(sut.AvailableTags[1]);
            sut.SelectedTags.Select(tag => tag.Name).Should().Equal("Focus", "Tools");
            sut.FilteredPlugins.Select(entry => entry.Identifier).Should().BeEquivalentTo(first.Identifier, second.Identifier, shared.Identifier);
            sut.AvailablePluginUpdateCount.Should().Be(1);
            sut.SearchText = " alpha ";
            sut.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(first.Identifier);
            sut.SearchText = "no matches";
            sut.SelectedPlugin.Should().BeNull();
            sut.SearchText = string.Empty;
            var selected = sut.FilteredPlugins.Single(entry => entry.Identifier == second.Identifier);
            sut.SelectedPlugin = selected;
            var extra = Manifest("Extra", tags: ["Tools"]);
            sut.AvailablePlugins = [new(first), new(second), new(shared), new(untagged), new(extra)];
            sut.SelectedTags.Select(tag => (tag.Name, tag.Count)).Should().Equal(("Focus", 2), ("Tools", 3));
            sut.SelectedPlugin.Should().BeSameAs(selected);

            first.Tags = ["Imaging"];
            sut.AvailablePlugins = [new(first), new(second), new(shared), new(untagged)];

            sut.SelectedTags.Select(tag => tag.Name).Should().Equal("Tools");
            sut.SelectedTags.Single().Count.Should().Be(2);
            sut.SelectedPlugin.Should().BeSameAs(selected);
            sut.FilteredPlugins.Select(entry => entry.Identifier).Should().BeEquivalentTo(second.Identifier, shared.Identifier);
            sut.SelectedTags.Add(sut.AvailableTags.Single(tag => tag.Name == "Imaging"));
            sut.FilteredPlugins.Should().HaveCount(3);
            sut.ClearFiltersCommand.Execute(null);
            sut.SelectedTags.Should().BeEmpty();
            sut.FilteredPlugins.Should().HaveCount(4, "clearing tags also includes plugins without metadata");
            sut.AvailablePlugins = [];
            sut.AvailableTags.Should().BeEmpty();
            sut.SelectedTags.Should().BeEmpty();
            sut.SelectedPlugin.Should().BeNull();
        }

        [Test]
        public void Install_CapturesSelectionAndPreservesPendingInstallAcrossCatalogRefresh() {
            var sut = CreateManager();
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var payload = Encoding.UTF8.GetBytes("captured plugin payload");
            using var download = new LoopbackHttpServer(payload, fileName: "Captured.dll", responseGate: gate.Task);
            var first = Manifest("Captured", download.Url, payload);
            var second = Manifest("Other");
            sut.AvailablePlugins = [new(first), new(second)];
            var entry = sut.FilteredPlugins.Single(value => value.Identifier == first.Identifier);
            sut.SelectedPlugin = entry;
            string folder = Path.Combine(Constants.UserExtensionsFolder, first.Name);
            try {
                Task install = sut.InstallPluginCommand.ExecuteAsync(null);
                PumpUntil(() => download.RequestReceived.IsCompleted);
                sut.SelectedPlugin = sut.FilteredPlugins.Single(value => value.Identifier == second.Identifier);
                sut.InstallPluginCommand.CanExecute(null).Should().BeFalse();
                sut.UninstallPluginCommand.CanExecute(null).Should().BeFalse();
                sut.RestartCommand.CanExecute(null).Should().BeFalse();
                gate.SetResult();
                Complete(install);

                File.ReadAllText(Path.Combine(folder, "Captured.dll")).Should().Be("captured plugin payload");
                entry.RequiresRestart.Should().BeTrue();
                entry.InstalledManifest.Should().BeNull();
                entry.SettingsManifest.Should().BeNull();
                sut.SelectedPlugin.RequiresRestart.Should().BeFalse();
                using var emptyCatalog = new LoopbackHttpServer(Encoding.UTF8.GetBytes("[]"), "application/json");
                Complete(Refresh(sut, emptyCatalog.Url));
                sut.AvailablePlugins.Should().BeEmpty();
                sut.HasPendingChanges.Should().BeTrue();
                sut.SelectedTabIndex = 0;
                sut.FilteredPlugins.Should().ContainSingle().Which.Should().BeSameAs(entry);
                entry.DisplayManifest.Should().BeSameAs(entry.PendingManifest);
                entry.CanInstall.Should().BeFalse();
            } finally {
                gate.TrySetResult();
                DeleteFolder(folder);
            }
        }

        [Test]
        public void Uninstall_KeepsLoadedManifestAndMarksRemovalUntilRestart() {
            var sut = CreateManager();
            var installed = Manifest("Remove");
            string folder = Path.Combine(Constants.UserExtensionsFolder, installed.Name);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "Remove.dll"), "installed payload");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            sut.SelectedTabIndex = 0;
            var entry = sut.SelectedPlugin;
            try {
                Complete(sut.UninstallPluginCommand.ExecuteAsync(null));

                entry.PendingChange.Should().Be(PluginPendingChange.Removal);
                entry.InstalledManifest.Should().BeSameAs(installed);
                entry.DisplayManifest.Should().BeSameAs(installed);
                entry.CanShowSettings.Should().BeFalse();
                entry.CanUninstall.Should().BeFalse();
                sut.HasPendingChanges.Should().BeTrue();
                sut.FilteredPlugins.Should().Contain(entry);
                File.Exists(Path.Combine(folder, "Remove.dll")).Should().BeFalse();
                using var emptyCatalog = new LoopbackHttpServer(Encoding.UTF8.GetBytes("[]"), "application/json");
                Complete(Refresh(sut, emptyCatalog.Url));
                entry.RequiresRestart.Should().BeTrue();
                sut.UpdateAllPluginsRequiresRestart.Should().BeTrue();
            } finally {
                DeleteFolder(folder);
            }
        }

        [Test]
        public void UpdateAll_IgnoresFiltersAndRetainsSuccessfulPendingStateWhenAnotherDownloadFails() {
            var sut = CreateManager();
            var payload = Encoding.UTF8.GetBytes("updated payload");
            using var goodServer = new LoopbackHttpServer(payload, fileName: "Updated.dll");
            using var badServer = new LoopbackHttpServer(payload, statusCode: 503);
            var first = Manifest("First Updated", goodServer.Url, payload);
            var second = Manifest("Second Failed", badServer.Url, payload);
            string firstFolder = Path.Combine(Constants.UserExtensionsFolder, first.Name);
            string secondFolder = Path.Combine(Constants.UserExtensionsFolder, second.Name);
            Directory.CreateDirectory(firstFolder);
            Directory.CreateDirectory(secondFolder);
            File.WriteAllText(Path.Combine(firstFolder, "Old.dll"), "old payload");
            File.WriteAllText(Path.Combine(secondFolder, "Old.dll"), "old payload");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [Installed(first)] = true, [Installed(second)] = true };
            sut.AvailablePlugins = [new(first), new(second)];
            sut.SearchText = first.Name;
            sut.FilteredPlugins.Should().ContainSingle();
            try {
                Complete(sut.UpdateAllPluginsCommand.ExecuteAsync(null));

                sut.OperationErrors.Should().Contain(second.Name).And.NotContain(first.Name);
                sut.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(first.Identifier);
                sut.ClearFiltersCommand.Execute(null);
                var success = sut.FilteredPlugins.Single(value => value.Identifier == first.Identifier);
                var failure = sut.FilteredPlugins.Single(value => value.Identifier == second.Identifier);
                success.PendingChange.Should().Be(PluginPendingChange.Update);
                success.HasOperationError.Should().BeFalse();
                failure.PendingChange.Should().Be(PluginPendingChange.None);
                failure.HasOperationError.Should().BeTrue();
                failure.CanUpdate.Should().BeTrue();
                sut.AvailablePluginUpdateCount.Should().Be(1);
                sut.UpdateAllPluginsRequiresRestart.Should().BeTrue();
                sut.IsMutating.Should().BeFalse();
                File.Exists(Path.Combine(secondFolder, "Old.dll")).Should().BeTrue();
                using var refreshed = Catalog(first, second);
                Complete(Refresh(sut, refreshed.Url));
                success.RequiresRestart.Should().BeTrue();
                sut.AvailablePluginUpdateCount.Should().Be(1);
                sut.UpdateAllPluginsRequiresRestart.Should().BeTrue();
                sut.OperationErrors.Should().Contain(second.Name);
            } finally {
                DeleteFolder(firstFolder);
                DeleteFolder(secondFolder);
            }
        }

        [Test]
        public void Install_CancelDownload_ReleasesMutationAndLeavesPluginEligible() {
            var sut = CreateManager();
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var payload = Encoding.UTF8.GetBytes("cancelled payload");
            using var server = new LoopbackHttpServer(payload, responseGate: gate.Task);
            var manifest = Manifest("Cancel", server.Url, payload);
            sut.AvailablePlugins = [new(manifest)];
            var entry = sut.SelectedPlugin;
            try {
                Task install = sut.InstallPluginCommand.ExecuteAsync(null);
                PumpUntil(() => server.RequestReceived.IsCompleted);
                sut.CancelInstallPluginCommand.Execute(null);
                Complete(install);

                sut.IsMutating.Should().BeFalse();
                entry.IsBusy.Should().BeFalse();
                entry.RequiresRestart.Should().BeFalse();
                entry.HasOperationError.Should().BeFalse();
                entry.CanInstall.Should().BeTrue();
                Directory.Exists(Path.Combine(Constants.UserExtensionsFolder, manifest.Name)).Should().BeFalse();
            } finally {
                gate.TrySetResult();
            }
        }

        [Test]
        public void Refresh_PartialFailureMergesCatalogAndTotalFailureKeepsUsableResults() {
            var sut = CreateManager();
            var original = Manifest("Original");
            sut.AvailablePlugins = [new(original)];
            var selected = sut.SelectedPlugin;
            var offered = Manifest("New Offered");
            using var success = Catalog(offered);
            using var failure = new LoopbackHttpServer([], statusCode: 503);

            Complete(Refresh(sut, success.Url, failure.Url));

            sut.AvailablePlugins.Should().HaveCount(2);
            sut.SelectedPlugin.Should().BeSameAs(selected);
            sut.HasCatalogError.Should().BeTrue();
            using var totalFailure = new LoopbackHttpServer([], statusCode: 503);
            Complete(Refresh(sut, totalFailure.Url));
            sut.AvailablePlugins.Should().HaveCount(2);
            sut.SelectedPlugin.Should().BeSameAs(selected);
            sut.HasCatalogError.Should().BeTrue();
            sut.IsFetching.Should().BeFalse();
        }

        [Test]
        public void Refresh_CancelAfterOneRepositorySuccess_DiscardsIncompleteRefresh() {
            var sut = CreateManager();
            var original = Manifest("Original");
            sut.AvailablePlugins = [new(original)];
            var selected = sut.SelectedPlugin;
            using var success = Catalog(Manifest("New Offered"));
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var held = new LoopbackHttpServer(Encoding.UTF8.GetBytes("[]"), "application/json", responseGate: gate.Task);
            try {
                Task refresh = Refresh(sut, success.Url, held.Url);
                PumpUntil(() => held.RequestReceived.IsCompleted);
                sut.CancelFetchPluginsCommand.Execute(null);
                Complete(refresh);

                sut.AvailablePlugins.Should().ContainSingle().Which.Identifier.Should().Be(original.Identifier);
                sut.SelectedPlugin.Should().BeSameAs(selected);
                sut.IsFetching.Should().BeFalse();
            } finally {
                gate.TrySetResult();
            }
        }

        [Test]
        public void Initialization_WithInstalledPlugins_StartsCatalogFetchWithoutConstructingView() {
            var installed = Manifest("Startup Update Check");
            int fetches = 0;

            var sut = CreateManager(new Dictionary<IPluginManifest, bool> { [installed] = true }, () => fetches++);

            fetches.Should().Be(1);
            sut.Initialized.Should().BeTrue();
            sut.IsFetching.Should().BeFalse();
            sut.HasCatalogError.Should().BeFalse();
            Complete(sut.FetchPluginsCommand.ExecuteAsync("Initial"));
            fetches.Should().Be(1);
            sut.IsFetching.Should().BeFalse();
        }

        [Test]
        public void Initialization_SelectsInstalledOnceWhenLocalPluginsExist() {
            var installed = Manifest("Startup");
            var sut = CreateManager(new Dictionary<IPluginManifest, bool> { [installed] = true }, openAvailable: false);

            sut.SelectedTabIndex.Should().Be(0);
            sut.SelectedPlugin.InstalledManifest.Should().BeSameAs(installed);
            sut.SelectedPlugin.SettingsManifest.Should().BeSameAs(installed);
            sut.SelectedTabIndex = 1;
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            sut.SelectedTabIndex.Should().Be(1);
            sut.IsFetching.Should().BeFalse();
            CreateManager(openAvailable: false).IsInstalledTab.Should().BeTrue();
        }

        [Test]
        public void Initialization_PreservesASectionChosenBeforeTheLoaderCompletes() {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = new Mock<IPluginLoader>();
            loader.SetupGet(value => value.Plugins).Returns(new Dictionary<IPluginManifest, bool> { [Manifest("Delayed startup")] = true });
            loader.Setup(value => value.Load()).Returns(gate.Task);
            var sut = new PluginsVM(loader.Object, Mock.Of<IProfileService>());
            sut.ShowAvailableCommand.Execute(null);
            bool fetchStarted = false;
            sut.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(PluginsVM.IsFetching) && sut.IsFetching) {
                    fetchStarted = true;
                    sut.CancelFetchPluginsCommand.Execute(null);
                }
            };

            gate.SetResult();
            PumpUntil(() => sut.Initialized && fetchStarted && !sut.IsFetching);

            sut.SelectedTabIndex.Should().Be(1);
            sut.IsInstalledTab.Should().BeFalse();
        }

        [Test]
        public void Installed_OrdersUpdatesFirstAndDistinguishesEmptyCatalogFromFilteredResults() {
            var sut = CreateManager();
            sut.EmptyMessage.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerNoAvailable"]);
            var current = Manifest("A Current");
            var update = Manifest("Z Update");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [current] = true, [Installed(update)] = true };
            sut.AvailablePlugins = [new(current), new(update)];
            sut.FilteredPlugins.Should().HaveCount(2);
            sut.SelectedTabIndex = 0;
            sut.FilteredPlugins.Select(value => value.Identifier).Should().Equal(update.Identifier, current.Identifier);
            sut.SearchText = "absent";
            sut.EmptyMessage.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerNoMatches"]);
            sut.ClearFiltersCommand.Execute(null);
            sut.SelectedTabIndex = 1;
            sut.FilteredPlugins.Select(value => value.Identifier).Should().Equal(current.Identifier, update.Identifier);
        }

        [Test]
        public void ClearingAvailableSelection_ClearsSharedSelectionAndDisablesActions() {
            var sut = CreateManager();
            sut.AvailablePlugins = [new(Manifest("Available Selection"))];
            NINA.Interfaces.IPluginsVM legacy = sut;
            sut.InstallPluginCommand.CanExecute(null).Should().BeTrue();

            legacy.SelectedAvailablePlugin = null!;

            sut.SelectedPlugin.Should().BeNull();
            sut.InstallPluginCommand.CanExecute(null).Should().BeFalse();
            sut.UpdatePluginCommand.CanExecute(null).Should().BeFalse();
            sut.UninstallPluginCommand.CanExecute(null).Should().BeFalse();
        }

        [Test]
        public void ClearingInstalledSelection_ClearsSharedSelectionAndDisablesActions() {
            var manifest = Manifest("Installed Selection");
            var sut = CreateManager(new Dictionary<IPluginManifest, bool> { [Installed(manifest)] = true });
            sut.AvailablePlugins = [new(manifest)];
            NINA.Interfaces.IPluginsVM legacy = sut;
            sut.UpdatePluginCommand.CanExecute(null).Should().BeTrue();
            sut.UninstallPluginCommand.CanExecute(null).Should().BeTrue();

            legacy.SelectedInstalledPlugin = default;

            sut.SelectedPlugin.Should().BeNull();
            sut.InstallPluginCommand.CanExecute(null).Should().BeFalse();
            sut.UpdatePluginCommand.CanExecute(null).Should().BeFalse();
            sut.UninstallPluginCommand.CanExecute(null).Should().BeFalse();
        }

        [Test]
        public void RepublishingInstalledDictionary_ReconcilesSameInstanceAdditionsAndRemovals() {
            var sut = CreateManager();
            NINA.Interfaces.IPluginsVM legacy = sut;
            var installed = legacy.InstalledPlugins;
            var first = Manifest("First Installed");
            var second = Manifest("Second Installed");
            int notifications = 0;
            sut.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(PluginsVM.InstalledPlugins)) {
                    notifications++;
                }
            };

            installed.Add(first, true);
            legacy.InstalledPlugins = installed;
            sut.SelectedTabIndex = 0;
            sut.FilteredPlugins.Should().ContainSingle().Which.InstalledManifest.Should().BeSameAs(first);
            installed.Add(second, false);
            legacy.InstalledPlugins = installed;
            sut.FilteredPlugins.Should().HaveCount(2);
            installed.Remove(first);
            legacy.InstalledPlugins = installed;

            sut.FilteredPlugins.Should().ContainSingle().Which.InstalledManifest.Should().BeSameAs(second);
            sut.SelectedPlugin.HasLoadError.Should().BeTrue();
            notifications.Should().Be(3);
        }

        [Test]
        public void RepublishingAvailableList_ReconcilesSameInstanceAdditionsAndRemovals() {
            var sut = CreateManager();
            NINA.Interfaces.IPluginsVM legacy = sut;
            var available = legacy.AvailablePlugins;
            var first = new ExtendedPluginManifest(Manifest("First Offered"));
            var second = new ExtendedPluginManifest(Manifest("Second Offered"));
            int notifications = 0;
            sut.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(PluginsVM.AvailablePlugins)) {
                    notifications++;
                }
            };

            available.Add(first);
            legacy.AvailablePlugins = available;
            sut.FilteredPlugins.Should().ContainSingle().Which.AvailableManifest.Should().BeSameAs(first);
            available.Add(second);
            legacy.AvailablePlugins = available;
            sut.FilteredPlugins.Should().HaveCount(2);
            available.Remove(first);
            legacy.AvailablePlugins = available;

            sut.FilteredPlugins.Should().ContainSingle().Which.AvailableManifest.Should().BeSameAs(second);
            sut.SelectedPlugin.AvailableManifest.Should().BeSameAs(second);
            notifications.Should().Be(3);
        }

        [Test]
        public void LegacySelectionSetters_NotifyWhenRepublishingAlreadyClearedValues() {
            var sut = CreateManager();
            NINA.Interfaces.IPluginsVM legacy = sut;
            var notifications = new List<string?>();
            sut.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(PluginsVM.SelectedInstalledPlugin)
                    || args.PropertyName == nameof(PluginsVM.SelectedAvailablePlugin)) {
                    notifications.Add(args.PropertyName);
                }
            };

            legacy.SelectedInstalledPlugin = default;
            legacy.SelectedAvailablePlugin = null!;

            notifications.Should().Equal(nameof(PluginsVM.SelectedInstalledPlugin), nameof(PluginsVM.SelectedAvailablePlugin));
            sut.SelectedPlugin.Should().BeNull();
            sut.InstallPluginCommand.CanExecute(null).Should().BeFalse();
            sut.UpdatePluginCommand.CanExecute(null).Should().BeFalse();
            sut.UninstallPluginCommand.CanExecute(null).Should().BeFalse();
        }
        [Test]
        public void Sections_RetainIndependentSearchTagsAndSelection() {
            var sut = CreateManager();
            var first = Manifest("Catalog Alpha", tags: ["Focus", "Imaging"]);
            var second = Manifest("Catalog Beta", tags: ["Focus", "Imaging"]);
            var localFirst = Installed(first);
            var localSecond = Installed(second);
            localFirst.Name = "Local Alpha";
            localSecond.Name = "Local Beta";
            localFirst.Tags = ["Imaging"];
            localSecond.Tags = ["Imaging"];
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [localFirst] = true, [localSecond] = true };
            sut.AvailablePlugins = [new(first), new(second)];
            sut.SearchText = "catalog";
            sut.SelectedTags.Add(sut.AvailableTags.Single(tag => tag.Name == "Focus"));
            sut.SelectedPlugin = sut.FilteredPlugins.Single(entry => entry.Identifier == second.Identifier);

            sut.SelectedTabIndex = 0;

            sut.SearchText.Should().BeEmpty();
            sut.SelectedTags.Should().BeEmpty();
            sut.SearchText = "local";
            sut.SelectedTags.Add(sut.AvailableTags.Single(tag => tag.Name == "Imaging"));
            sut.SelectedPlugin = sut.FilteredPlugins.Single(entry => entry.Identifier == first.Identifier);

            sut.SelectedTabIndex = 1;

            sut.SearchText.Should().Be("catalog");
            sut.SelectedTags.Select(tag => tag.Name).Should().Equal("Focus");
            sut.SelectedPlugin.Identifier.Should().Be(second.Identifier);
            sut.ClearFiltersCommand.Execute(null);
            sut.SelectedTabIndex = 0;
            sut.SearchText.Should().Be("local");
            sut.SelectedTags.Select(tag => tag.Name).Should().Equal("Imaging");
            sut.SelectedPlugin.Identifier.Should().Be(first.Identifier);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void History_LoadsCompatibleReleasesWhenTheCatalogHasNoCompatibleLatestRelease(bool catalogFails) {
            var installed = Manifest("Installed history");
            var sut = CreateManager();
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            sut.SelectedTabIndex = 0;
            var latest = Manifest("Future");
            latest.Identifier = installed.Identifier;
            latest.Version = new PluginVersion("4.0.0.0");
            latest.MinimumApplicationVersion = new PluginVersion($"{new Version(CoreUtil.Version).Major + 1}.0.0.0");
            var older = Manifest("Older");
            older.Identifier = installed.Identifier;
            older.Version = new PluginVersion("1.0.0.0");
            using var repository = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(latest), catalogFails ? 503 : 200),
                new(CatalogJson(latest, installed, older))
            });
            Complete(Refresh(sut, repository.Url));
            sut.AvailablePlugins.Should().BeEmpty();
            repository.Requests.Should().ContainSingle();

            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => !sut.IsLoadingVersions);

            sut.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("2.0.0.0", "1.0.0.0");
            sut.VersionEntries.Single(version => version.IsInstalled).VersionText.Should().Be("2.0.0.0");
            sut.VersionEntries.Last().StatusText.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerRollbackAvailable"]);
            sut.SelectedVersion = sut.VersionEntries.Last().Manifest;
            sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeTrue();
            sut.HasVersionHistoryError.Should().BeFalse();
            repository.Requests.Should().HaveCount(2);
            repository.Requests[1].Should().StartWith($"GET /plugins/{installed.Identifier}/versions HTTP/1.1");
        }

        [TestCase("invalid")]
        [TestCase("")]
        public void History_LoadsLazilyCachesByPluginAndRetainsReleasesAcrossRefreshFailureAndRetry(string failedResponse) {
            var sut = CreateManager();
            var latest = Manifest("Lazy history");
            latest.Version = new PluginVersion("3.0.0.0");
            var previous = Manifest("Previous");
            previous.Identifier = latest.Identifier;
            previous.Version = new PluginVersion("1.0.0.0");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [Installed(latest)] = true };
            using var server = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(latest)), new(CatalogJson(latest, previous)),
                new(CatalogJson(latest)), new(Encoding.UTF8.GetBytes(failedResponse)), new(CatalogJson(previous, latest))
            });
            Complete(Refresh(sut, server.Url));
            server.Requests.Should().ContainSingle();
            sut.CompatibleVersions.Should().ContainSingle();
            sut.AvailablePluginUpdateCount.Should().Be(1);

            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => !sut.IsLoadingVersions);
            sut.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("3.0.0.0", "1.0.0.0");
            sut.SelectedVersion = sut.CompatibleVersions[1];
            sut.IsVersionHistoryVisible = false;
            sut.IsVersionHistoryVisible = true;
            server.Requests.Should().HaveCount(2);
            sut.VersionEntries.Single(version => version.IsInstalled).VersionText.Should().Be("1.0.0.0");

            Complete(Refresh(sut, server.Url));
            PumpUntil(() => !sut.IsLoadingVersions);
            sut.HasVersionHistoryError.Should().BeTrue();
            sut.HasCatalogError.Should().BeFalse();
            sut.CompatibleVersions.Should().HaveCount(2);
            sut.SelectedVersion.Version.ToString().Should().Be("1.0.0.0");
            sut.AvailablePlugins.Should().ContainSingle().Which.Version.ToString().Should().Be("3.0.0.0");
            sut.AvailablePluginUpdateCount.Should().Be(1);

            Complete(sut.RetryVersionHistoryCommand.ExecuteAsync(null));
            sut.HasVersionHistoryError.Should().BeFalse();
            sut.CompatibleVersions.Should().HaveCount(2);
            server.Requests.Should().HaveCount(5);
        }

        [TestCase(404)]
        [TestCase(405)]
        public void History_UnsupportedRouteUsesEveryCatalogReleaseAndRefreshesTheFallback(int statusCode) {
            var sut = CreateManager();
            var latest = Manifest("Fallback");
            latest.Version = new PluginVersion("3.0.0.0");
            var installed = Manifest("Installed");
            installed.Identifier = latest.Identifier;
            installed.Name = latest.Name;
            var oldest = Manifest("Oldest");
            oldest.Identifier = latest.Identifier;
            oldest.Version = new PluginVersion("1.0.0.0");
            var refreshed = Manifest("Refreshed");
            refreshed.Identifier = latest.Identifier;
            refreshed.Version = new PluginVersion("4.0.0.0");
            var other = Manifest("Other");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            using var legacyRepository = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(oldest, latest, installed, oldest, other)), new([], statusCode),
                new(CatalogJson(installed, refreshed, latest, other)), new([], statusCode)
            });
            Complete(Refresh(sut, legacyRepository.Url));
            sut.SelectedPlugin = sut.FilteredPlugins.Single(entry => entry.Identifier == latest.Identifier);
            sut.AvailablePlugins.Should().HaveCount(2);
            sut.SelectedPlugin.AvailableManifest.Version.ToString().Should().Be("3.0.0.0");
            sut.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("3.0.0.0", "2.0.0.0", "1.0.0.0");

            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => !sut.IsLoadingVersions);

            sut.HasCatalogError.Should().BeFalse();
            sut.VersionHistoryError.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerHistoryUnsupported"]);
            sut.VersionEntries.Select(version => version.VersionText).Should().Equal("3.0.0.0", "2.0.0.0", "1.0.0.0");
            sut.VersionEntries.Single(version => version.IsInstalled).VersionText.Should().Be("2.0.0.0");
            sut.VersionEntries.Last().StatusText.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerRollbackAvailable"]);
            sut.SelectedVersion = sut.VersionEntries.Last().Manifest;
            sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeTrue();
            sut.IsVersionHistoryVisible = false;
            sut.IsVersionHistoryVisible = true;
            sut.SelectedTabIndex = 0;
            sut.SelectedTabIndex = 1;
            sut.SelectedVersion.Version.ToString().Should().Be("1.0.0.0");
            legacyRepository.Requests.Should().HaveCount(2);
            legacyRepository.Requests[0].Should().StartWith("GET /plugins/manifests HTTP/1.1");
            legacyRepository.Requests[1].Should().StartWith($"GET /plugins/{latest.Identifier}/versions HTTP/1.1");

            Complete(Refresh(sut, legacyRepository.Url));
            PumpUntil(() => !sut.IsLoadingVersions);

            sut.VersionEntries.Select(version => version.VersionText).Should().Equal("4.0.0.0", "3.0.0.0", "2.0.0.0");
            sut.SelectedVersion.Version.ToString().Should().Be("2.0.0.0");
            sut.SelectedPlugin.SettingsManifest.Should().BeSameAs(installed);
            sut.SelectedPlugin.AvailableManifest.Version.ToString().Should().Be("4.0.0.0");
            sut.AvailablePluginUpdateCount.Should().Be(1);
            legacyRepository.Requests.Should().HaveCount(4);
        }

        [TestCase(404)]
        [TestCase(405)]
        [TestCase(500)]
        public void History_PartialRepositoryFailureKeepsSuccessfulHistoryAndCatalogReleases(int statusCode) {
            var sut = CreateManager();
            var latest = Manifest("Partial history");
            var previous = Manifest("Previous");
            previous.Identifier = latest.Identifier;
            previous.Version = new PluginVersion("1.0.0.0");
            using var failed = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(latest)), new([], statusCode)
            });
            using var success = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(latest)), new(CatalogJson(previous))
            });
            Complete(Refresh(sut, failed.Url, success.Url));

            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => !sut.IsLoadingVersions);

            sut.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("2.0.0.0", "1.0.0.0");
            sut.HasVersionHistoryError.Should().BeTrue();
            sut.HasCatalogError.Should().BeFalse();
            sut.IsVersionHistoryVisible = false;
            sut.IsVersionHistoryVisible = true;
            failed.Requests.Should().HaveCount(2);
            success.Requests.Should().HaveCount(2);
        }

        [Test]
        public void History_CancelsObsoleteSelectionAndDoesNotPublishItsRowsOrErrorsToAnotherPlugin() {
            var sut = CreateManager();
            var first = Manifest("First history");
            var second = Manifest("Second history");
            var oldSecond = Manifest("Older second");
            oldSecond.Identifier = second.Identifier;
            oldSecond.Version = new PluginVersion("1.0.0.0");
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(first, second)), new(Encoding.UTF8.GetBytes("invalid"), Gate: gate.Task),
                new(CatalogJson(second, oldSecond))
            });
            Complete(Refresh(sut, server.Url));
            sut.SelectedPlugin = sut.FilteredPlugins.Single(entry => entry.Identifier == first.Identifier);
            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => server.Requests.Count == 2);
            sut.IsLoadingVersions.Should().BeTrue();
            sut.SelectedPlugin = sut.FilteredPlugins.Single(entry => entry.Identifier == second.Identifier);
            try {
                gate.SetResult();
                PumpUntil(() => !sut.IsLoadingVersions);
                sut.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("2.0.0.0", "1.0.0.0");
                sut.VersionEntries.Should().OnlyContain(version => version.Manifest.Identifier == second.Identifier);
                sut.SelectedVersion.Identifier.Should().Be(second.Identifier);
                sut.HasVersionHistoryError.Should().BeFalse();
                sut.HasCatalogError.Should().BeFalse();
            } finally {
                gate.TrySetResult();
            }
        }

        [Test]
        public void History_EmptyCatalogStillChecksHistoryAndAnEmptyHistoryKeepsOnlyTheInstalledMarker() {
            var sut = CreateManager();
            var latest = Manifest("Removed");
            var older = Manifest("Retired release");
            older.Identifier = latest.Identifier;
            older.Version = new PluginVersion("0.9.0.0");
            var installed = Installed(latest);
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            using var server = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(latest)), new(CatalogJson(older, latest)), new(CatalogJson()), new(CatalogJson())
            });
            Complete(Refresh(sut, server.Url));
            PumpUntil(() => !sut.IsLoadingVersions);
            sut.SelectedTabIndex = 0;
            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => !sut.IsLoadingVersions);
            sut.CompatibleVersions.Should().HaveCount(2);

            Complete(Refresh(sut, server.Url));
            PumpUntil(() => !sut.IsLoadingVersions);

            sut.CompatibleVersions.Should().BeEmpty();
            sut.VersionEntries.Should().ContainSingle().Which.IsInstalled.Should().BeTrue();
            sut.SelectedVersion.Should().BeSameAs(installed);
            sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeFalse();
            server.Requests.Should().HaveCount(4);
        }

        [Test]
        public void History_RefreshCancelsAnOlderResponseBeforeReloadingTheSelectedPlugin() {
            var sut = CreateManager();
            var latest = Manifest("Refreshed history");
            var stale = Manifest("Obsolete response");
            stale.Identifier = latest.Identifier;
            stale.Version = new PluginVersion("99.0.0.0");
            var refreshed = Manifest("Refreshed release");
            refreshed.Identifier = latest.Identifier;
            refreshed.Version = new PluginVersion("3.0.0.0");
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(latest)), new(CatalogJson(stale), Gate: gate.Task),
                new(CatalogJson(refreshed)), new(CatalogJson(refreshed, latest))
            });
            Complete(Refresh(sut, server.Url));
            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => server.Requests.Count == 2);
            try {
                Task refresh = Refresh(sut, server.Url);
                gate.SetResult();
                Complete(refresh);
                PumpUntil(() => !sut.IsLoadingVersions);

                sut.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("3.0.0.0", "2.0.0.0");
                sut.HasVersionHistoryError.Should().BeFalse();
                sut.AvailablePlugins.Single().Version.ToString().Should().Be("3.0.0.0");
                server.Requests.Should().HaveCount(4);
            } finally {
                gate.TrySetResult();
            }
        }

        [Test]
        public void Versions_RetainUniqueReleasesAndSelectionAcrossRefreshAndFailure() {
            var sut = CreateManager();
            var latest = Manifest("Version history");
            latest.Version = new PluginVersion("3.0.0.0");
            var previous = Manifest("Previous release");
            previous.Identifier = latest.Identifier;
            previous.Name = latest.Name;
            previous.Version = new PluginVersion("2.0.0.0");
            var oldest = Manifest("Oldest release");
            oldest.Identifier = latest.Identifier;
            oldest.Name = latest.Name;
            oldest.Version = new PluginVersion("1.0.0.0");
            byte[] releases = CatalogJson(oldest, latest, previous);
            byte[] duplicateRelease = CatalogJson(previous);
            using var repository = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(releases), new(releases)
            });
            using var duplicate = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(duplicateRelease), new(duplicateRelease)
            });

            Complete(Refresh(sut, repository.Url, duplicate.Url));

            sut.HasCatalogError.Should().BeFalse();
            sut.AvailablePlugins.Should().ContainSingle().Which.Version.ToString().Should().Be("3.0.0.0");
            sut.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("3.0.0.0", "2.0.0.0", "1.0.0.0");
            sut.SelectedVersion = sut.CompatibleVersions[1];
            Complete(Refresh(sut, repository.Url, duplicate.Url));
            sut.HasCatalogError.Should().BeFalse();
            repository.Requests.Should().HaveCount(2);
            duplicate.Requests.Should().HaveCount(2);
            repository.Requests.Should().OnlyContain(request => request.StartsWith("GET /plugins/manifests HTTP/1.1"));
            duplicate.Requests.Should().OnlyContain(request => request.StartsWith("GET /plugins/manifests HTTP/1.1"));
            sut.SelectedVersion.Version.ToString().Should().Be("2.0.0.0");
            using var failure = new LoopbackHttpServer(Encoding.UTF8.GetBytes("invalid"), "application/json");
            Complete(Refresh(sut, failure.Url));
            sut.HasCatalogError.Should().BeTrue();
            sut.CompatibleVersions.Should().HaveCount(3);
            sut.SelectedVersion.Version.ToString().Should().Be("2.0.0.0");
            using var empty = Catalog();
            Complete(Refresh(sut, empty.Url));
            sut.CompatibleVersions.Should().BeEmpty();
            sut.SelectedVersion.Should().BeNull();
            sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeFalse();
        }

        [TestCase("1.0.0.0")]
        [TestCase("3.0.0.0")]
        public void VersionChange_CapturesUpgradeOrRollbackAndKeepsLoadedSettingsUntilRestart(string targetVersion) {
            var sut = CreateManager();
            var payload = Encoding.UTF8.GetBytes("selected release " + targetVersion);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var download = new LoopbackHttpServer(payload, fileName: "Selected.dll", responseGate: gate.Task);
            var target = Manifest("Version change", download.Url, payload);
            target.Version = new PluginVersion(targetVersion);
            var current = Manifest("Current release", download.Url, payload);
            current.Identifier = target.Identifier;
            current.Name = target.Name;
            current.Version = new PluginVersion("2.0.0.0");
            var other = Manifest("Other installed plugin");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [current] = true, [other] = true };
            var offered = PluginEntryVM.CompareVersions(target.Version, current.Version) > 0 ? target : current;
            using var validRepository = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(CatalogJson(offered)), new(CatalogJson(target, current)), new(CatalogJson(offered))
            });
            Complete(Refresh(sut, validRepository.Url));
            sut.SelectedTabIndex = 0;
            sut.SelectedPlugin = sut.FilteredPlugins.Single(entry => entry.Identifier == current.Identifier);
            sut.IsVersionHistoryVisible = true;
            PumpUntil(() => !sut.IsLoadingVersions);
            var entry = sut.SelectedPlugin;
            sut.SelectedVersion = sut.CompatibleVersions.Single(manifest => manifest.Version.ToString() == "2.0.0.0");
            sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeFalse();
            sut.SelectedVersion = sut.CompatibleVersions.Single(manifest => manifest.Version.ToString() == targetVersion);
            sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeTrue();
            string folder = Path.Combine(Constants.UserExtensionsFolder, current.Name);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "Original.dll"), "original");
            try {
                Task operation = sut.InstallSelectedVersionCommand.ExecuteAsync(null);
                PumpUntil(() => download.RequestReceived.IsCompleted);
                sut.IsVersionHistoryVisible = false;
                sut.SelectedPlugin = sut.FilteredPlugins.Single(candidate => candidate.Identifier == other.Identifier);
                sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeFalse();
                gate.SetResult();
                Complete(operation);

                File.ReadAllText(Path.Combine(folder, "Selected.dll")).Should().Be("selected release " + targetVersion);
                File.Exists(Path.Combine(folder, "Original.dll")).Should().BeFalse();
                entry.InstalledManifest.Should().BeSameAs(current);
                entry.SettingsManifest.Should().BeSameAs(current);
                entry.PendingChange.Should().Be(PluginPendingChange.Update);
                entry.PendingManifest.Version.ToString().Should().Be(targetVersion);
                sut.HasPendingChanges.Should().BeTrue();
                Complete(Refresh(sut, validRepository.Url));
                sut.HasCatalogError.Should().BeFalse();
                validRepository.Requests.Should().HaveCount(3);
                validRepository.Requests[0].Should().StartWith("GET /plugins/manifests HTTP/1.1");
                validRepository.Requests[1].Should().StartWith($"GET /plugins/{current.Identifier}/versions HTTP/1.1");
                validRepository.Requests[2].Should().StartWith("GET /plugins/manifests HTTP/1.1");
                sut.SelectedPlugin = entry;
                entry.RequiresRestart.Should().BeTrue();
                sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeFalse();
                sut.VersionEntries.Single(version => version.IsInstalled).VersionText.Should().Be("2.0.0.0");
                sut.VersionEntries.Single(version => version.VersionText == targetVersion).StatusText
                    .Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerPendingRestart"]);
                sut.VersionActionText.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerPendingRestart"]);
            } finally {
                gate.TrySetResult();
                DeleteFolder(folder);
            }
        }

        [Test]
        public void Versions_KeepTheInstalledMarkerWithoutACatalogAndClearItAfterRemovalFromTheLoader() {
            var sut = CreateManager();
            var installed = Manifest("Local release");
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            sut.SelectedTabIndex = 0;

            sut.CompatibleVersions.Should().BeEmpty();
            sut.VersionEntries.Should().ContainSingle().Which.IsInstalled.Should().BeTrue();
            sut.VersionEntries[0].StatusText.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerCurrentlyInstalled"]);
            sut.SelectedVersion.Should().BeSameAs(installed);
            sut.InstallSelectedVersionCommand.CanExecute(null).Should().BeFalse();
            sut.InstalledPlugins = new Dictionary<IPluginManifest, bool>();
            sut.VersionEntries.Should().BeEmpty();
            sut.SelectedVersion.Should().BeNull();
        }

        private static LoopbackHttpServer Catalog(params PluginManifest[] manifests) {
            return new LoopbackHttpServer(CatalogJson(manifests), "application/json");
        }

        private static byte[] CatalogJson(params PluginManifest[] manifests) {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(manifests, new Newtonsoft.Json.JsonSerializerSettings {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
            });
            return Encoding.UTF8.GetBytes(json);
        }

        private static Task Refresh(PluginsVM sut, params string[] repositories) {
            var method = typeof(PluginsVM).GetMethod("FetchRepositories", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (Task)method.Invoke(sut, [repositories])!;
        }

        private static void Complete(Task task) {
            PumpUntil(() => task.IsCompleted);
            task.GetAwaiter().GetResult();
        }

        private static PluginsVM CreateManager(IDictionary<IPluginManifest, bool>? installed = null, Action? onStartupFetch = null, bool openAvailable = true) {
            var localPlugins = installed ?? new Dictionary<IPluginManifest, bool>();
            var loadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = new Mock<IPluginLoader>();
            loader.SetupGet(value => value.Plugins).Returns(localPlugins);
            loader.Setup(value => value.Load()).Returns(loadGate.Task);
            var sut = new PluginsVM(loader.Object, Mock.Of<IProfileService>());
            bool startupFetchStarted = false;
            System.ComponentModel.PropertyChangedEventHandler cancelStartup = (_, args) => {
                if (args.PropertyName == nameof(PluginsVM.IsFetching) && sut.IsFetching) {
                    startupFetchStarted = true;
                    onStartupFetch?.Invoke();
                    sut.CancelFetchPluginsCommand.Execute(null);
                }
            };
            sut.PropertyChanged += cancelStartup;
            loadGate.SetResult();
            PumpUntil(() => sut.Initialized);
            if (localPlugins.Count > 0) {
                PumpUntil(() => startupFetchStarted && !sut.IsFetching);
            }

            sut.PropertyChanged -= cancelStartup;
            if (openAvailable) {
                sut.ShowAvailableCommand.Execute(null);
            }

            return sut;
        }

        private static PluginManifest Manifest(string name, string url = "https://example.invalid", byte[]? payload = null, string[]? tags = null) {
            return new PluginManifest {
                Identifier = Guid.NewGuid().ToString(),
                Name = name + " " + Guid.NewGuid().ToString("N"),
                Author = "Test Observatory",
                Repository = "https://example.invalid/repository",
                License = "MPL-2.0",
                LicenseURL = "https://example.invalid/license",
                Version = new PluginVersion("2.0.0.0"),
                MinimumApplicationVersion = new PluginVersion(CoreUtil.Version),
                Descriptions = new PluginDescription { ShortDescription = "Plugin manager fixture" },
                Tags = tags ?? [],
                Installer = new PluginInstallerDetails {
                    URL = url + "/download",
                    Checksum = Convert.ToHexString(SHA256.HashData(payload ?? [])),
                    ChecksumType = InstallerChecksum.SHA256,
                    Type = InstallerType.DLL
                }
            };
        }

        private static PluginManifest Installed(PluginManifest manifest) {
            return new PluginManifest {
                Identifier = manifest.Identifier,
                Name = manifest.Name,
                Author = manifest.Author,
                Version = new PluginVersion("1.0.0.0"),
                Tags = manifest.Tags,
                Descriptions = manifest.Descriptions
            };
        }

        private static void PumpUntil(Func<bool> complete) {
            var timeout = Stopwatch.StartNew();
            while (!complete()) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(15)) {
                    throw new TimeoutException("Plugin manager operation did not complete.");
                }

                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                Thread.Sleep(1);
            }
        }

        private static void DeleteFolder(string path) {
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
        }
    }
}
