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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.Profile.Interfaces;
using NINA.Test.Plugin;
using NINA.View.Plugins;
using NINA.ViewModel.Plugins;
using NINA.WPF.Base.Utility;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class PluginNavigationDuringRefreshTest {
        private PluginViewResources resources = null!;

        [SetUp]
        public void SetUpResources() {
            resources = new PluginViewResources();
        }

        [TearDown]
        public void RestoreResources() {
            resources.Dispose();
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        public void HeaderMouseInputAndCatalogCompletion_KeepTheVisibleSectionConsistent(bool finishOnInstalled, bool completeWhileUnloaded) {
            var loader = new Mock<IPluginLoader>();
            loader.SetupGet(value => value.Plugins).Returns(new Dictionary<IPluginManifest, bool>());
            loader.Setup(value => value.Load()).Returns(Task.CompletedTask);
            var manager = new PluginsVM(loader.Object, Mock.Of<IProfileService>());
            PumpUntil(() => manager.Initialized);
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);

            var installed = Manifest("Local plugin");
            var previousCatalogOnly = Manifest("Previous catalog plugin");
            var freshCatalogOnly = Manifest("Fresh catalog plugin");
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            manager.AvailablePlugins = [new(installed), new(previousCatalogOnly)];
            var view = new PluginsView();
            view.SetBinding(FrameworkElement.DataContextProperty, new Binding(nameof(ShellContext.PluginsVM)));
            var pluginPage = new TabItem { Header = "Plugins", Content = view };
            var otherPage = new TabItem { Header = "Other", Content = new TextBlock { Text = "Other page" } };
            var shell = new TabControl { Items = { pluginPage, otherPage } };
            var host = new Window {
                Width = 1000,
                Height = 700,
                Left = -20000,
                Top = -20000,
                ShowInTaskbar = false,
                ShowActivated = false,
                DataContext = new ShellContext(manager),
                Content = shell
            };
            var responseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            string json = JsonConvert.SerializeObject(new[] { installed, freshCatalogOnly }, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new StringEnumConverter() }
            });
            using var repository = new LoopbackHttpServer(Encoding.UTF8.GetBytes(json), "application/json", responseGate: responseGate.Task);
            Task? refresh = null;
            try {
                host.Show();
                Layout(host);
                view.IsLoaded.Should().BeTrue();
                view.DataContext.Should().BeSameAs(manager);
                var navigation = (TabControl)view.FindName("PluginNavigation");
                var installedTab = (TabItem)view.FindName("InstalledPluginsTab");
                var availableTab = (TabItem)view.FindName("AvailablePluginsTab");
                var list = (ListBox)view.FindName("PluginList");
                navigation.DataContext.Should().BeSameAs(manager);
                list.DataContext.Should().BeSameAs(manager);

                ClickHeader(availableTab, host);
                AssertSection(manager, navigation, list, false, installed.Identifier, previousCatalogOnly.Identifier);
                list.SelectedItem = list.Items.Cast<PluginEntryVM>().Single(entry => entry.Identifier == previousCatalogOnly.Identifier);

                var fetch = typeof(PluginsVM).GetMethod("FetchRepositories", BindingFlags.Instance | BindingFlags.NonPublic)!;
                refresh = (Task)fetch.Invoke(manager, [new[] { repository.Url }])!;
                PumpUntil(() => repository.RequestReceived.IsCompleted);
                manager.IsFetching.Should().BeTrue();
                for (int iteration = 0; iteration < 3; iteration++) {
                    ClickHeader(installedTab, host);
                    AssertSection(manager, navigation, list, true, installed.Identifier);
                    ClickHeader(availableTab, host);
                    AssertSection(manager, navigation, list, false, installed.Identifier, previousCatalogOnly.Identifier);
                }

                ClickHeader(finishOnInstalled ? installedTab : availableTab, host);
                if (completeWhileUnloaded) {
                    ClickHeader(otherPage, host);
                    view.IsLoaded.Should().BeFalse();
                }

                responseGate.SetResult();
                PumpUntil(() => refresh.IsCompleted);
                refresh.GetAwaiter().GetResult();
                manager.IsFetching.Should().BeFalse();
                manager.HasCatalogError.Should().BeFalse();
                if (completeWhileUnloaded) {
                    ClickHeader(pluginPage, host);
                    view.IsLoaded.Should().BeTrue();
                }

                Layout(host);
                string[] expected = finishOnInstalled
                    ? [installed.Identifier]
                    : [installed.Identifier, freshCatalogOnly.Identifier];
                AssertSection(manager, navigation, list, finishOnInstalled, expected);
                ClickHeader(installedTab, host);
                AssertSection(manager, navigation, list, true, installed.Identifier);
                ClickHeader(availableTab, host);
                AssertSection(manager, navigation, list, false, installed.Identifier, freshCatalogOnly.Identifier);
            } finally {
                responseGate.TrySetResult();
                if (refresh != null) {
                    PumpUntil(() => refresh.IsCompleted);
                }

                host.Close();
            }
        }

        [TestCase(104729)]
        [TestCase(314159)]
        [TestCase(8675309)]
        public void MixedClickSequence_KeepsMainNavigationAndVisibleMembershipConsistent(int seed) {
            var loader = new Mock<IPluginLoader>();
            loader.SetupGet(value => value.Plugins).Returns(new Dictionary<IPluginManifest, bool>());
            loader.Setup(value => value.Load()).Returns(Task.CompletedTask);
            var manager = new PluginsVM(loader.Object, Mock.Of<IProfileService>());
            PumpUntil(() => manager.Initialized);
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);

            var focus = Manifest("Focus settings");
            focus.Tags = ["Focus", "Tools"];
            focus.Version = new PluginVersion("1.0.0.0");
            var sequence = Manifest("Sequence settings");
            sequence.Tags = ["Sequence"];
            var failed = Manifest("Failed tools");
            failed.Tags = ["Tools"];
            var focusOffer = Manifest(focus.Name);
            focusOffer.Identifier = focus.Identifier;
            focusOffer.Tags = focus.Tags;
            var sequenceOffer = Manifest(sequence.Name);
            sequenceOffer.Identifier = sequence.Identifier;
            sequenceOffer.Tags = sequence.Tags;
            var remoteFocus = Manifest("Remote focus");
            remoteFocus.Tags = ["Focus"];
            var remoteSequence = Manifest("Remote sequence");
            remoteSequence.Tags = ["Sequence", "Tools"];
            PluginManifest[] local = [focus, sequence, failed];
            PluginManifest[] catalog = [focusOffer, sequenceOffer, remoteFocus, remoteSequence];
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> {
                [focus] = true,
                [sequence] = true,
                [failed] = false
            };
            manager.AvailablePlugins = catalog.Select(manifest => new ExtendedPluginManifest(manifest)).ToList();

            var editor = new FrameworkElementFactory(typeof(TextBox));
            editor.SetBinding(TextBox.TextProperty, new Binding(nameof(PluginManifest.Author)) {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            var options = new DataTemplate { VisualTree = editor };
            string[] optionKeys = [focus.Name + DataTemplatePostfix.Options, sequence.Name + DataTemplatePostfix.Options];
            foreach (string key in optionKeys) {
                Application.Current.Resources[key] = options;
            }

            var view = new PluginsView();
            view.SetBinding(FrameworkElement.DataContextProperty, new Binding(nameof(ShellContext.PluginsVM)));
            var pluginPage = new TabItem { Header = "Plugins", Content = view };
            var otherPage = new TabItem { Header = "Other", Content = new TextBlock { Text = "Other page" } };
            var shell = new TabControl { Items = { pluginPage, otherPage } };
            var host = new Window {
                Width = 1000,
                Height = 700,
                Left = -20000,
                Top = -20000,
                ShowInTaskbar = false,
                ShowActivated = false,
                DataContext = new ShellContext(manager),
                Content = shell
            };
            var operations = new List<string>();
            try {
                host.Show();
                Layout(host);
                var navigation = (TabControl)view.FindName("PluginNavigation");
                var installedTab = (TabItem)view.FindName("InstalledPluginsTab");
                var availableTab = (TabItem)view.FindName("AvailablePluginsTab");
                var list = (ListBox)view.FindName("PluginList");
                var search = (TextBox)view.FindName("PluginSearch");
                var tags = (CheckComboBox)view.FindName("PluginTagFilter");
                var random = new Random(seed);
                int expectedMainSection = 0;
                for (int step = 0; step < 150; step++) {
                    int operation = random.Next(10);
                    string description = operation.ToString();
                    switch (operation) {
                        case 0:
                        case 1:
                            expectedMainSection = operation;
                            description = operation == 0 ? "Installed header" : "Available header";
                            ClickHeader(operation == 0 ? installedTab : availableTab, host);
                            break;
                        case 2:
                            if (list.Items.Count > 0) {
                                int index = random.Next(list.Items.Count);
                                var item = (PluginEntryVM)list.Items[index];
                                description = "Plugin row " + item.Name;
                                list.ScrollIntoView(item);
                                Layout(host);
                                var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(index);
                                ClickElement(FindVisual<TextBlock>(row)!, host);
                            }

                            break;
                        case 3:
                        case 4:
                            if (manager.SelectedPlugin != null) {
                                var details = (TabControl)view.FindName("DetailsNavigation");
                                var visibleTabs = details.Items.Cast<TabItem>().Where(tab => tab.Visibility == Visibility.Visible).ToArray();
                                var tab = visibleTabs[random.Next(visibleTabs.Length)];
                                description = "Detail header " + tab.Name;
                                ClickHeader(tab, host);
                            }

                            break;
                        case 5:
                            string[] queries = [string.Empty, "no matching observatory", "Focus", "Tools", "Test Observatory"];
                            search.Text = queries[random.Next(queries.Length)];
                            description = "Search '" + search.Text + "'";
                            break;
                        case 6:
                            if (tags.Items.Count > 0) {
                                tags.IsDropDownOpen = true;
                                var tag = tags.Items[random.Next(tags.Items.Count)];
                                if (tags.SelectedItems.Contains(tag)) {
                                    tags.SelectedItems.Remove(tag);
                                } else {
                                    tags.SelectedItems.Add(tag);
                                }
                                tags.IsDropDownOpen = false;
                                description = "Tags '" + string.Join(", ", tags.SelectedItems.Cast<PluginTag>().Select(tag => tag.Name)) + "'";
                            }

                            break;
                        case 7:
                            description = "Leave and return to Plugins";
                            ClickHeader(otherPage, host);
                            view.IsLoaded.Should().BeFalse();
                            ClickHeader(pluginPage, host);
                            view.IsLoaded.Should().BeTrue();
                            break;
                        case 8:
                            description = "Clear search";
                            search.Clear();
                            break;
                        case 9:
                            if (!manager.HasResults) {
                                description = "Clear filters button";
                                var clear = (Button)view.FindName("ClearPluginFiltersButton");
                                ((IInvokeProvider)new ButtonAutomationPeer(clear).GetPattern(PatternInterface.Invoke)).Invoke();
                            }

                            break;
                    }

                    operations.Add($"{step}: {description}");
                    Layout(host);
                    navigation.SelectedIndex.Should().Be(expectedMainSection, $"seed {seed}, step {step}: {description}");
                    manager.SelectedTabIndex.Should().Be(expectedMainSection);
                    manager.IsInstalledTab.Should().Be(expectedMainSection == 0);
                    var expectedRows = expectedMainSection == 0 ? local.AsEnumerable() : catalog.AsEnumerable();
                    string[] selectedTags = tags.SelectedItems.Cast<PluginTag>().Select(tag => tag.Name).ToArray();
                    string query = search.Text.Trim();
                    expectedRows = expectedRows.Where(manifest => selectedTags.Length == 0
                            || (manifest.Tags ?? []).Any(tag => selectedTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                        .Where(manifest => string.IsNullOrEmpty(query)
                            || Matches(manifest.Name, query)
                            || Matches(manifest.Author, query)
                            || Matches(manifest.Descriptions?.ShortDescription, query)
                            || (manifest.Tags ?? []).Any(tag => Matches(tag, query)));
                    string[] identifiers = expectedRows.Select(manifest => manifest.Identifier).ToArray();
                    list.Items.Cast<PluginEntryVM>().Select(entry => entry.Identifier).Should().BeEquivalentTo(identifiers);
                    manager.FilteredPlugins.Select(entry => entry.Identifier).Should().BeEquivalentTo(identifiers);
                    list.SelectedItem.Should().BeSameAs(manager.SelectedPlugin);
                    if (identifiers.Length == 0) {
                        manager.SelectedPlugin.Should().BeNull();
                    } else {
                        manager.SelectedPlugin.Should().NotBeNull($"seed {seed}, step {step}: {description}; keyboard modifiers: {Keyboard.Modifiers}");
                        identifiers.Should().Contain(manager.SelectedPlugin!.Identifier);
                    }
                }
            } catch {
                TestContext.Out.WriteLine($"Seed {seed} click sequence:\n" + string.Join(Environment.NewLine, operations));
                throw;
            } finally {
                host.Close();
                foreach (string key in optionKeys) {
                    Application.Current.Resources.Remove(key);
                }
            }
        }

        private static bool Matches(string? value, string query) => value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;

        private static void AssertSection(PluginsVM manager, TabControl navigation, ListBox list, bool installed, params string[] identifiers) {
            navigation.SelectedIndex.Should().Be(installed ? 0 : 1);
            manager.SelectedTabIndex.Should().Be(navigation.SelectedIndex);
            manager.IsInstalledTab.Should().Be(installed);
            list.Items.Cast<PluginEntryVM>().Select(entry => entry.Identifier).Should().BeEquivalentTo(identifiers);
            manager.FilteredPlugins.Select(entry => entry.Identifier).Should().BeEquivalentTo(identifiers);
            list.SelectedItem.Should().BeSameAs(manager.SelectedPlugin);
            identifiers.Should().Contain(manager.SelectedPlugin.Identifier);
        }

        private static void ClickHeader(TabItem tab, FrameworkElement host) {
            var header = FindVisual<TextBlock>(tab) ?? throw new AssertionException("The tab header did not materialize.");
            ClickElement(header, host);
            tab.IsSelected.Should().BeTrue("routed mouse input on the displayed tab header must select the tab");
        }

        private static void ClickElement(UIElement element, FrameworkElement host) {
            element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) {
                RoutedEvent = Mouse.MouseDownEvent
            });
            element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) {
                RoutedEvent = Mouse.MouseUpEvent
            });
            Layout(host);
        }

        private static PluginManifest Manifest(string name) => new() {
            Identifier = Guid.NewGuid().ToString(),
            Name = name,
            Author = "Test Observatory",
            Version = new PluginVersion("2.0.0.0"),
            MinimumApplicationVersion = new PluginVersion(CoreUtil.Version),
            Repository = "https://example.invalid/repository",
            License = "MPL-2.0",
            LicenseURL = "https://example.invalid/license",
            Descriptions = new PluginDescription { ShortDescription = "Refresh navigation fixture" },
            Installer = new PluginInstallerDetails {
                URL = "https://example.invalid/plugin.dll",
                Checksum = new string('0', 64),
                ChecksumType = InstallerChecksum.SHA256,
                Type = InstallerType.DLL
            }
        };

        private static void Layout(FrameworkElement host) {
            host.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            host.UpdateLayout();
        }

        private static void PumpUntil(Func<bool> complete) {
            var timeout = Stopwatch.StartNew();
            while (!complete()) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(15)) {
                    throw new TimeoutException("Refresh navigation operation did not complete.");
                }

                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                Thread.Sleep(1);
            }
        }

        private static T? FindVisual<T>(DependencyObject root) where T : DependencyObject {
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match) {
                    return match;
                }

                var nested = FindVisual<T>(child);
                if (nested != null) {
                    return nested;
                }
            }

            return null;
        }

        private sealed class ShellContext(PluginsVM manager) {
            public PluginsVM PluginsVM { get; } = manager;
        }
    }
}
