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
using NINA.CustomControlLibrary;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.View.Plugins;
using NINA.ViewModel.Plugins;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.WPF.Base.Utility;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Reflection;
using System.Security.Cryptography;
using NINA.Test.Plugin;
using System.Text;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class PluginsViewTest {
        private readonly List<Window> changelogHosts = new();
        private PluginViewResources resources = null!;

        [OneTimeSetUp]
        public void SetUpResources() {
            resources = new PluginViewResources();
        }

        [TearDown]
        public void RestoreResources() {
            foreach (var host in changelogHosts) {
                host.Close();
            }

            changelogHosts.Clear();
            resources.Reset();
        }

        [OneTimeTearDown]
        public void DisposeResources() {
            resources.Dispose();
        }

        [TestCase("Persian Faint")]
        [TestCase("Light")]
        [TestCase("Dark Nebula")]
        [TestCase("High Contrast")]
        public void MarkdownLists_UseSquareBulletsAndPreserveNumbering(string schema) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            const string text = "### Dashes\n\n- Dash item\n    - Nested item\n\n### Asterisks\n\n* Asterisk item\n\n### Plus\n\n+ Plus item\n\n### Numbered\n\n7. Numbered item\n8. Another numbered item";
            var manager = CreateManager();
            manager.AvailablePlugins = [new(new PluginManifest {
                Identifier = "lists", Name = "Markdown list fixture",
                Descriptions = new PluginDescription {
                    LongDescription = text
                }
            })];
            var view = new PluginsView { DataContext = manager };
            Layout(view, 800, 600);
            var viewer = FindVisual<MdXaml.MarkdownScrollViewer>((ScrollViewer)view.FindName("DescriptionContent"))!;
            var document = viewer.Document.Should().BeOfType<System.Windows.Documents.FlowDocument>().Subject;
            var lists = document.Blocks.OfType<System.Windows.Documents.List>().ToArray();
            var numbered = lists.Should().ContainSingle(list => list.MarkerStyle == TextMarkerStyle.Decimal).Which;
            var parsedNumbered = new MdXaml.Markdown().Transform(text).Blocks.OfType<System.Windows.Documents.List>()
                .Single(list => list.MarkerStyle == TextMarkerStyle.Decimal);
            numbered.StartIndex.Should().Be(parsedNumbered.StartIndex);
            numbered.ListItems.Count.Should().Be(2);
            var unordered = lists.Where(list => list != numbered)
                .Concat(lists.SelectMany(list => list.ListItems.SelectMany(item => item.Blocks.OfType<System.Windows.Documents.List>())))
                .ToArray();
            unordered.Should().HaveCount(4);
            unordered.Should().OnlyContain(list => list.MarkerStyle == TextMarkerStyle.Box, "unordered lists use filled square bullets");

            manager.AvailablePlugins = [new(new PluginManifest {
                Identifier = "replacement", Name = "Replacement list fixture",
                Descriptions = new PluginDescription { LongDescription = "- Replacement item" }
            })];
            Layout(view, 800, 600);
            viewer.Document.Should().BeOfType<System.Windows.Documents.FlowDocument>().Subject.Blocks
                .OfType<System.Windows.Documents.List>().Should().ContainSingle().Which.MarkerStyle.Should().Be(TextMarkerStyle.Box);
        }

        [Test]
        public void ChangelogDetachedView_DoesNotFetchUntilItIsShown() {
            WithChangelogCache(_ => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes("# Visible notes"), "text/markdown", responseGate: gate.Task);
                var manager = CreateManager();
                manager.AvailablePlugins = [new(new PluginManifest {
                    Identifier = "detached", Name = "Detached fixture", ChangelogURL = server.Url + "/notes.md"
                })];
                var view = new PluginsView { DataContext = manager };
                try {
                    Layout(view);
                    ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                    Layout(view);
                    var state = (PluginChangelogVM)((FrameworkElement)view.FindName("ChangelogContent")).DataContext;
                    view.IsLoaded.Should().BeFalse();
                    state.IsLoading.Should().BeFalse("detached views must not start a download");
                    server.Requests.Should().BeEmpty();

                    ShowChangelogView(view);
                    PumpUntil(() => server.RequestReceived.IsCompleted);
                    gate.TrySetResult();
                    var markdown = (MdXaml.MarkdownScrollViewer)view.FindName("ChangelogMarkdown");
                    PumpUntil(() => markdown.Markdown == "# Visible notes");
                } finally {
                    view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                    gate.TrySetResult();
                }
            });
        }

        [Test]
        public void ChangelogTab_LoadsMarkdownOnlyWhenOpenedAndRetainsBrowserLink() {
            WithChangelogCache(_ => {
                using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes("# Release notes\n\nFixed the fixture. [More](../guide.md)\n\n- Dash bullet"), "text/markdown");
                var manifest = new PluginManifest {
                    Identifier = "changelog", Name = "Changelog fixture", Version = new PluginVersion("1.0.0.0"),
                    ChangelogURL = server.Url + "/docs/CHANGELOG.md"
                };
                var manager = CreateManager();
                manager.AvailablePlugins = [new(manifest)];
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                var tab = view.FindName("ChangelogTab").Should().BeOfType<TabItem>().Subject;
                var tabs = (TabControl)view.FindName("DetailsNavigation");
                tabs.Items.IndexOf(tab).Should().Be(tabs.Items.IndexOf(view.FindName("DescriptionTab")) + 1);
                tab.Visibility.Should().Be(Visibility.Visible);
                server.Requests.Should().BeEmpty();

                tab.IsSelected = true;
                Layout(view);
                var markdown = view.FindName("ChangelogMarkdown").Should().BeOfType<MdXaml.MarkdownScrollViewer>().Subject;
                PumpUntil(() => markdown.Markdown?.Contains("Fixed the fixture.") == true);
                markdown.MarkdownStyle.Should().BeSameAs(view.FindResource("MarkdownStyle"));
                markdown.AssetPathRoot.Should().Be(server.Url + "/docs/");
                var document = markdown.Document.Should().BeOfType<System.Windows.Documents.FlowDocument>().Subject;
                document.Blocks.OfType<System.Windows.Documents.List>().Should().ContainSingle().Which.MarkerStyle.Should().Be(TextMarkerStyle.Box);
                var relativeLink = document.Blocks.OfType<System.Windows.Documents.Paragraph>()
                    .SelectMany(paragraph => paragraph.Inlines.OfType<System.Windows.Documents.Hyperlink>()).Single();
                relativeLink.CommandParameter.Should().Be(server.Url + "/guide.md");
                new System.Windows.Documents.TextRange(document.ContentStart, document.ContentEnd)
                    .Text.Should().Contain("Release notes").And.Contain("Fixed the fixture.");
                Layout(view);
                var browser = (TextBlock)view.FindName("ChangelogBrowserLink");
                browser.Inlines.OfType<System.Windows.Documents.Hyperlink>().Single().NavigateUri.AbsoluteUri.Should().Be(manifest.ChangelogURL);
                ((FrameworkElement)view.FindName("ChangelogContent")).Visibility.Should().Be(Visibility.Visible);
                ((TabItem)view.FindName("DescriptionTab")).IsSelected = true;
                Layout(view);
                ((FrameworkElement)view.FindName("ChangelogContent")).Visibility.Should().Be(Visibility.Collapsed);

                manager.AvailablePlugins = [new(new PluginManifest { Identifier = "no-changelog", Name = "No changelog" })];
                Layout(view);
                tab.Visibility.Should().Be(Visibility.Collapsed);
            });
        }

        [Test]
        public void ChangelogRetry_UsesTheBoundButtonAfterAFailedRequest() {
            WithChangelogCache(_ => {
                using var server = new LoopbackHttpServer(responses: [
                    new([], 503), new(Encoding.UTF8.GetBytes("# Recovered notes"))
                ], "text/markdown");
                var manager = CreateManager();
                manager.AvailablePlugins = [new(new PluginManifest {
                    Identifier = "retry", Name = "Retry fixture", ChangelogURL = server.Url + "/notes.md"
                })];
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                var content = (FrameworkElement)view.FindName("ChangelogContent");
                var state = (PluginChangelogVM)content.DataContext;
                var retry = (Button)view.FindName("RetryChangelogButton");
                PumpUntil(() => state.CanRetry);
                Layout(view);
                state.Notice.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerChangelogFailed"]);
                retry.Visibility.Should().Be(Visibility.Visible);
                retry.Style.Should().BeSameAs(view.FindResource("StandardButton"));
                ((IInvokeProvider)new ButtonAutomationPeer(retry).GetPattern(PatternInterface.Invoke)).Invoke();
                var markdown = (MdXaml.MarkdownScrollViewer)view.FindName("ChangelogMarkdown");
                PumpUntil(() => markdown.Markdown == "# Recovered notes");
                state.Notice.Should().BeEmpty();
                state.CanRetry.Should().BeFalse();
                server.Requests.Should().HaveCount(2);
            });
        }

        [Test]
        public void ChangelogHtml_ShowsLocalizedBrowserFallbackWithoutRenderingHtml() {
            WithChangelogCache(root => {
                using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes("<html><body>Website notes</body></html>"), "text/html");
                var manager = CreateManager();
                manager.AvailablePlugins = [new(new PluginManifest {
                    Identifier = "html", Name = "HTML fixture", ChangelogURL = server.Url + "/releases"
                })];
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                var state = (PluginChangelogVM)((FrameworkElement)view.FindName("ChangelogContent")).DataContext;
                PumpUntil(() => state.CanRetry);
                Layout(view);
                state.Notice.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerChangelogUnsupported"]);
                ((MdXaml.MarkdownScrollViewer)view.FindName("ChangelogMarkdown")).Markdown.Should().BeEmpty();
                ((TextBlock)view.FindName("ChangelogBrowserLink")).Inlines.OfType<System.Windows.Documents.Hyperlink>()
                    .Single().NavigateUri.AbsoluteUri.Should().Be(server.Url + "/releases");
                Directory.Exists(Path.Combine(root, "Cache", "PluginChangelogs")).Should().BeFalse();
            });
        }

        [Test]
        public void ChangelogExpiredCache_ShowsSavedNotesDuringRefreshAndRetainsThemForRetry() {
            WithChangelogCache(root => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var server = new LoopbackHttpServer(responses: [
                    new(Encoding.UTF8.GetBytes("# Saved notes")), new([], 503, gate.Task),
                    new(Encoding.UTF8.GetBytes("# Updated notes"))
                ], "text/markdown");
                try {
                    string url = server.Url + "/notes.md";
                    var uri = new Uri(url);
                    new PluginContentCache().FetchChangelogAsync(uri, "1.0.0.0", CancellationToken.None).GetAwaiter().GetResult();
                    string path = Directory.GetFiles(Path.Combine(root, "Cache", "PluginChangelogs"), "*.json").Single();
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-8));
                    DateTime timestamp = File.GetLastWriteTimeUtc(path);
                    var manager = CreateManager();
                    manager.AvailablePlugins = [new(new PluginManifest {
                        Identifier = "stale", Name = "Stale fixture", Version = new PluginVersion("1.0.0.0"), ChangelogURL = url
                    })];
                    var view = new PluginsView { DataContext = manager };
                    ShowChangelogView(view);
                    ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                    Layout(view);
                    var state = (PluginChangelogVM)((FrameworkElement)view.FindName("ChangelogContent")).DataContext;
                    var markdown = (MdXaml.MarkdownScrollViewer)view.FindName("ChangelogMarkdown");
                    markdown.Markdown.Should().Be("# Saved notes");
                    state.IsLoading.Should().BeTrue();
                    PumpUntil(() => server.Requests.Count == 2);
                    gate.TrySetResult();
                    PumpUntil(() => state.CanRetry);
                    state.Notice.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerChangelogStale"]);
                    markdown.Markdown.Should().Be("# Saved notes");
                    File.GetLastWriteTimeUtc(path).Should().Be(timestamp);
                    ((Button)view.FindName("RetryChangelogButton")).Command.Execute(null);
                    PumpUntil(() => markdown.Markdown == "# Updated notes");

                    var reopened = new PluginsView { DataContext = manager };
                    ShowChangelogView(reopened);
                    ((TabItem)reopened.FindName("ChangelogTab")).IsSelected = true;
                    Layout(reopened);
                    ((MdXaml.MarkdownScrollViewer)reopened.FindName("ChangelogMarkdown")).Markdown.Should().Be("# Updated notes");
                    server.Requests.Should().HaveCount(3, "a fresh persisted document avoids another request in a new view");
                } finally {
                    gate.TrySetResult();
                }
            });
        }

        [TestCase("selection")]
        [TestCase("version")]
        [TestCase("url")]
        public void ChangelogObsoleteDownload_DoesNotReachAnotherDisplayedManifest(string change) {
            WithChangelogCache(_ => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var oldServer = new LoopbackHttpServer(responses: [
                    new(Encoding.UTF8.GetBytes("# Obsolete notes"), Gate: gate.Task),
                    new(Encoding.UTF8.GetBytes("# Current notes"))
                ], "text/markdown");
                using var newServer = new LoopbackHttpServer(Encoding.UTF8.GetBytes("# Current notes"), "text/markdown");
                try {
                    var manager = CreateManager();
                    var original = new PluginManifest {
                        Identifier = "original", Name = "Original fixture", Version = new PluginVersion("1.0.0.0"),
                        ChangelogURL = oldServer.Url + "/notes.md"
                    };
                    manager.AvailablePlugins = [new(original)];
                    var view = new PluginsView { DataContext = manager };
                    ShowChangelogView(view);
                    ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                    PumpUntil(() => oldServer.RequestReceived.IsCompleted);

                    var replacement = new PluginManifest {
                        Identifier = change == "selection" ? "replacement" : original.Identifier,
                        Name = original.Name, Version = new PluginVersion(change == "version" ? "2.0.0.0" : "1.0.0.0"),
                        ChangelogURL = (change == "version" ? oldServer.Url : newServer.Url) + "/notes.md"
                    };
                    manager.AvailablePlugins = [new(replacement)];
                    Layout(view);
                    ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                    var markdown = (MdXaml.MarkdownScrollViewer)view.FindName("ChangelogMarkdown");
                    if (change == "version") {
                        markdown.Markdown.Should().BeEmpty();
                        gate.TrySetResult();
                    }

                    PumpUntil(() => markdown.Markdown == "# Current notes");
                    gate.TrySetResult();
                    oldServer.Dispose();
                    Layout(view);
                    markdown.Markdown.Should().Be("# Current notes");
                } finally {
                    gate.TrySetResult();
                }
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ChangelogTabHideAndViewUnload_CancelPendingDisplay(bool unload) {
            WithChangelogCache(root => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes("# Obsolete notes"), "text/markdown", responseGate: gate.Task);
                try {
                    var manager = CreateManager();
                    manager.AvailablePlugins = [new(new PluginManifest {
                        Identifier = "cancel", Name = "Cancel fixture", ChangelogURL = server.Url + "/notes.md"
                    })];
                    var view = new PluginsView { DataContext = manager };
                    var host = ShowChangelogView(view);
                    ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                    PumpUntil(() => server.RequestReceived.IsCompleted);
                    var state = (PluginChangelogVM)((FrameworkElement)view.FindName("ChangelogContent")).DataContext;
                    if (unload) {
                        host.Content = null;
                        PumpUntil(() => !view.IsLoaded);
                    } else {
                        ((TabItem)view.FindName("DescriptionTab")).IsSelected = true;
                    }

                    state.IsLoading.Should().BeFalse();
                    gate.TrySetResult();
                    server.Dispose();
                    Layout(view);
                    state.Markdown.Should().BeEmpty();
                    Directory.Exists(Path.Combine(root, "Cache", "PluginChangelogs")).Should().BeFalse();
                } finally {
                    gate.TrySetResult();
                }
            });
        }

        [Test]
        public void Changelog_UsesEachSectionsDisplayedManifestAndCachedVersion() {
            WithChangelogCache(_ => {
                using var server = new LoopbackHttpServer(responses: [
                    new(Encoding.UTF8.GetBytes("# Installed notes")), new(Encoding.UTF8.GetBytes("# Offered notes"))
                ], "text/markdown");
                var installed = new PluginManifest {
                    Identifier = "displayed", Name = "Displayed fixture", Version = new PluginVersion("1.0.0.0"),
                    ChangelogURL = server.Url + "/notes.md"
                };
                var offered = new PluginManifest {
                    Identifier = installed.Identifier, Name = installed.Name, Version = new PluginVersion("2.0.0.0"),
                    ChangelogURL = installed.ChangelogURL
                };
                var manager = CreateManager();
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
                manager.AvailablePlugins = [new(offered)];
                manager.SelectedTabIndex = 0;
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                var tab = (TabItem)view.FindName("ChangelogTab");
                var markdown = (MdXaml.MarkdownScrollViewer)view.FindName("ChangelogMarkdown");
                tab.IsSelected = true;
                PumpUntil(() => markdown.Markdown == "# Installed notes");
                manager.SelectedTabIndex = 1;
                Layout(view);
                tab.IsSelected = true;
                PumpUntil(() => markdown.Markdown == "# Offered notes");
                manager.SelectedTabIndex = 0;
                Layout(view);
                tab.IsSelected = true;
                Layout(view);
                markdown.Markdown.Should().Be("# Installed notes");
                server.Requests.Should().HaveCount(2);
            });
        }

        [Test]
        public void ChangelogHiddenView_RemainsCancelledAfterEntryNotificationsAndResumesWhenShown() {
            WithChangelogCache(_ => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var server = new LoopbackHttpServer(responses: [
                    new(Encoding.UTF8.GetBytes("# Obsolete notes"), Gate: gate.Task),
                    new(Encoding.UTF8.GetBytes("# Visible notes"))
                ], "text/markdown");
                var manager = CreateManager();
                typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
                manager.AvailablePlugins = [new(new PluginManifest {
                    Identifier = "visibility", Name = "Visibility fixture", ChangelogURL = server.Url + "/notes.md"
                })];
                var view = new PluginsView { DataContext = manager };
                var host = new Window {
                    Width = 1000, Height = 700, Left = -20000, Top = -20000,
                    ShowInTaskbar = false, ShowActivated = false, Content = view
                };
                try {
                    host.Show();
                    Layout(view);
                    ((TabItem)view.FindName("ChangelogTab")).IsSelected = true;
                    PumpUntil(() => server.RequestReceived.IsCompleted);
                    var state = (PluginChangelogVM)((FrameworkElement)view.FindName("ChangelogContent")).DataContext;
                    host.Hide();
                    manager.SelectedPlugin.OperationError = "Hidden notification fixture";
                    Layout(view);
                    state.IsLoading.Should().BeFalse();
                    server.Requests.Should().ContainSingle();
                    gate.TrySetResult();
                    state.Markdown.Should().BeEmpty();

                    host.Show();
                    Layout(view);
                    var markdown = (MdXaml.MarkdownScrollViewer)view.FindName("ChangelogMarkdown");
                    PumpUntil(() => markdown.Markdown == "# Visible notes");
                    server.Requests.Should().HaveCount(2);
                } finally {
                    gate.TrySetResult();
                    host.Close();
                }
            });
        }

        [Test]
        public void Constructor_ProvidesSharedSearchAndSidebarNavigation() {
            var view = new PluginsView();

            var search = view.FindName("PluginSearch").Should().BeOfType<HintTextBox>().Subject;
            search.HintText.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerSearch"]);
            view.FindName("PluginList").Should().BeOfType<ListBox>();
            var navigation = view.FindName("PluginNavigation").Should().BeOfType<TabControl>().Subject;
            navigation.TabStripPlacement.Should().Be(Dock.Left);
            navigation.Style.Should().BeSameAs(view.FindResource("SecondLevelTabControl"));
            navigation.Items.Count.Should().Be(2);
            ((TabItem)navigation.Items[0]).GetValue(System.Windows.Automation.AutomationProperties.NameProperty)
                .Should().Be(NINA.Core.Locale.Loc.Instance["LblInstalled"]);
        }

        [TestCase("Persian Faint")]
        [TestCase("Light")]
        [TestCase("Dark Nebula")]
        [TestCase("High Contrast")]
        public void BrowserRows_AlignFiltersAndShowCompactStateMarkers(string schema) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            var manager = CreateManager();
            var installed = new PluginManifest { Identifier = "installed", Name = "Installed fixture", Version = new PluginVersion("1.0.0.0") };
            var offered = new PluginManifest { Identifier = installed.Identifier, Name = installed.Name, Version = new PluginVersion("2.0.0.0"), Tags = ["Imaging", "Focus"] };
            var remote = new PluginManifest { Identifier = "remote", Name = "Catalog fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Tools"] };
            var current = new PluginManifest { Identifier = "current", Name = "Current fixture", Version = new PluginVersion("1.0.0.0") };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true, [current] = true };
            manager.AvailablePlugins = [new(offered), new(remote), new(current)];
            var view = new PluginsView { DataContext = manager };
            Layout(view, 720, 560);
            var list = (ListBox)view.FindName("PluginList");
            var listBounds = list.TransformToAncestor(view).TransformBounds(new Rect(list.RenderSize));
            var refresh = (Button)view.FindName("RefreshPluginsButton");
            var refreshBounds = refresh.TransformToAncestor(view).TransformBounds(new Rect(refresh.RenderSize));
            foreach (string name in new[] { "PluginSearch", "PluginTagFilter" }) {
                var filter = (FrameworkElement)view.FindName(name);
                var bounds = filter.TransformToAncestor(view).TransformBounds(new Rect(filter.RenderSize));
                bounds.Left.Should().BeApproximately(listBounds.Left, 0.1);
                bounds.Right.Should().BeApproximately(name == "PluginSearch" ? refreshBounds.Left - 5 : refreshBounds.Right, 0.1);
            }

            var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(manager.FilteredPlugins.Single(entry => entry.Identifier == installed.Identifier));
            var installedMarker = FindVisuals<TextBlock>(row).Single(label => label.Text == NINA.Core.Locale.Loc.Instance["LblInstalled"]);
            var updateMarker = FindVisuals<TextBlock>(row).Single(label => label.Text == NINA.Core.Locale.Loc.Instance["LblUpdate"]);
            installedMarker.Visibility.Should().Be(Visibility.Collapsed, "Update takes priority over Installed");
            updateMarker.Visibility.Should().Be(Visibility.Visible);
            installedMarker.FontSize.Should().BeLessThan(FindVisual<TextBlock>(row)!.FontSize);
            installedMarker.Text.Should().NotContain("(").And.NotContain(")");
            updateMarker.Text.Should().NotContain("(").And.NotContain(")");
            var currentRow = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(manager.FilteredPlugins.Single(entry => entry.Identifier == current.Identifier));
            var currentMarker = FindVisuals<TextBlock>(currentRow).Single(label => label.Text == installedMarker.Text);
            var mouseOverKey = (DependencyPropertyKey)typeof(UIElement)
                .GetField("IsMouseOverPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            foreach (var (markerRow, marker, color) in new[] {
                (row, updateMarker, profile.ColorSchemaSettings.ColorSchema.ButtonBackgroundSelectedColor),
                (currentRow, currentMarker, profile.ColorSchemaSettings.ColorSchema.SecondaryBackgroundColor)
            }) {
                foreach (bool selected in new[] { false, true }) {
                    markerRow.IsSelected = selected;
                    foreach (bool hovered in new[] { false, true }) {
                        markerRow.SetValue(mouseOverKey, hovered);
                        Layout(view, 720, 560);
                        ((SolidColorBrush)marker.Background).Color.Should().Be(color, "status colors come from the active theme");
                        AssertBrushColor(marker.Foreground, view.FindResource(marker == updateMarker ? "ButtonForegroundBrush" : "PrimaryBrush"));
                    }
                }

                markerRow.SetValue(mouseOverKey, false);
            }

            foreach (var entry in manager.FilteredPlugins.Where(entry => entry.Identifier != installed.Identifier)) {
                var other = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(entry);
                FindVisuals<TextBlock>(other).Single(label => label.Text == NINA.Core.Locale.Loc.Instance["LblInstalled"])
                    .Visibility.Should().Be(entry.Identifier == current.Identifier ? Visibility.Visible : Visibility.Collapsed);
                FindVisuals<TextBlock>(other).Single(label => label.Text == NINA.Core.Locale.Loc.Instance["LblUpdate"])
                    .Visibility.Should().Be(Visibility.Collapsed);
            }

            manager.SelectedTabIndex = 0;
            Layout(view, 720, 560);
            row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            FindVisuals<TextBlock>(row).Single(label => label.Text == NINA.Core.Locale.Loc.Instance["LblInstalled"])
                .Visibility.Should().Be(Visibility.Collapsed);
            FindVisuals<TextBlock>(row).Single(label => label.Text == NINA.Core.Locale.Loc.Instance["LblUpdate"])
                .Visibility.Should().Be(Visibility.Visible);
            manager.SelectedTabIndex = 1;
            manager.AvailablePlugins = [new(installed), new(remote), new(current)];
            Layout(view, 720, 560);
            row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(manager.FilteredPlugins.Single(entry => entry.Identifier == installed.Identifier));
            FindVisuals<TextBlock>(row).Single(label => label.Text == installedMarker.Text).Visibility.Should().Be(Visibility.Visible);
            FindVisuals<TextBlock>(row).Single(label => label.Text == updateMarker.Text).Visibility.Should().Be(Visibility.Collapsed);
            manager.AvailablePlugins = [new(offered), new(remote), new(current)];
            Layout(view, 720, 560);
            row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(manager.FilteredPlugins.Single(entry => entry.Identifier == installed.Identifier));
            FindVisuals<TextBlock>(row).Single(label => label.Text == installedMarker.Text).Visibility.Should().Be(Visibility.Collapsed);
            FindVisuals<TextBlock>(row).Single(label => label.Text == updateMarker.Text).Visibility.Should().Be(Visibility.Visible);
        }

        [TestCase("Persian Faint")]
        [TestCase("Light")]
        [TestCase("Dark Nebula")]
        [TestCase("High Contrast")]
        public void Tags_CheckSeveralValuesAndRetainThemAcrossSectionChanges(string schema) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            var manager = CreateManager();
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            var focus = new PluginManifest { Identifier = "focus", Name = "Focus fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Focus"] };
            var imaging = new PluginManifest { Identifier = "imaging", Name = "Imaging fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Imaging"] };
            var tools = new PluginManifest { Identifier = "tools", Name = "Tools fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Tools"] };
            var shared = new PluginManifest { Identifier = "shared", Name = "Shared tags fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Focus", "Imaging"] };
            var extra = new PluginManifest { Identifier = "extra", Name = "Count fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Imaging"] };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [focus] = true, [imaging] = true };
            manager.AvailablePlugins = [new(focus), new(imaging), new(shared), new(tools)];
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            try {
                host.Show();
                LayoutWindow(host, 1000, 700);
                var tags = view.FindName("PluginTagFilter").Should().BeOfType<CheckComboBox>().Subject;
                tags.Items.Cast<PluginTag>().Select(tag => (tag.Name, tag.Count)).Should().Equal(("Focus", 2), ("Imaging", 2));
                tags.IsDropDownOpen = true;
                LayoutWindow(host, 1000, 700);
                foreach (string tag in new[] { "Focus", "Imaging" }) {
                    var choice = tags.Items.Cast<PluginTag>().Single(item => item.Name == tag);
                    var item = (SelectorItem)tags.ItemContainerGenerator.ContainerFromItem(choice);
                    var check = FindVisual<CheckBox>(item)!;
                    check.Content.Should().Be(choice.Label);
                    ((IToggleProvider)new CheckBoxAutomationPeer(check).GetPattern(PatternInterface.Toggle)).Toggle();
                    LayoutWindow(host, 1000, 700);
                    tags.IsDropDownOpen.Should().BeTrue("checking a tag must leave the other choices accessible");
                }

                tags.SelectedItems.Cast<PluginTag>().Select(tag => tag.Name).Should().BeEquivalentTo("Focus", "Imaging");
                manager.FilteredPlugins.Select(entry => entry.Identifier).Should().BeEquivalentTo(focus.Identifier, imaging.Identifier, shared.Identifier);
                manager.AvailablePlugins = [new(focus), new(imaging), new(shared), new(tools), new(extra)];
                LayoutWindow(host, 1000, 700);
                var imagingChoice = tags.Items.Cast<PluginTag>().Single(tag => tag.Name == "Imaging");
                imagingChoice.Count.Should().Be(3);
                tags.SelectedItems.Cast<PluginTag>().Should().Contain(imagingChoice, "refreshing a count keeps that tag selected");
                var imagingCheck = FindVisual<CheckBox>((SelectorItem)tags.ItemContainerGenerator.ContainerFromItem(imagingChoice))!;
                imagingCheck.Content.Should().Be(imagingChoice.Label);
                imagingCheck.IsChecked.Should().BeTrue();
                var popup = (System.Windows.Controls.Primitives.Popup)tags.Template.FindName("PART_Popup", tags);
                var popupContent = (Border)popup.Child;
                ((SolidColorBrush)popupContent.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.BackgroundColor);
                manager.SearchText = "Imaging fixture";
                LayoutWindow(host, 1000, 700);
                manager.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(imaging.Identifier);
                var escapeTarget = imagingCheck;
                var escape = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(escapeTarget), Environment.TickCount, Key.Escape) {
                    RoutedEvent = Keyboard.PreviewKeyDownEvent
                };
                escapeTarget.RaiseEvent(escape);
                if (!escape.Handled) {
                    escape.RoutedEvent = Keyboard.KeyDownEvent;
                    escapeTarget.RaiseEvent(escape);
                }

                tags.IsDropDownOpen.Should().BeFalse("Escape must close the multi-select popup without clearing its choices");
                tags.SelectedItems.Cast<PluginTag>().Select(tag => tag.Name).Should().BeEquivalentTo("Focus", "Imaging");
                manager.SelectedTabIndex = 0;
                LayoutWindow(host, 1000, 700);
                tags.SelectedItems.Cast<PluginTag>().Should().BeEmpty();
                tags.Items.Cast<PluginTag>().Select(tag => (tag.Name, tag.Count)).Should().Equal(("Focus", 1), ("Imaging", 1));
                tags.IsDropDownOpen = true;
                LayoutWindow(host, 1000, 700);
                var installedFocus = tags.Items.Cast<PluginTag>().Single(tag => tag.Name == "Focus");
                var installedCheck = FindVisual<CheckBox>((SelectorItem)tags.ItemContainerGenerator.ContainerFromItem(installedFocus))!;
                installedCheck.Content.Should().Be(installedFocus.Label);
                ((IToggleProvider)new CheckBoxAutomationPeer(installedCheck).GetPattern(PatternInterface.Toggle)).Toggle();
                LayoutWindow(host, 1000, 700);
                manager.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(focus.Identifier);
                tags.IsDropDownOpen = false;
                manager.SelectedTabIndex = 1;
                LayoutWindow(host, 1000, 700);
                tags.SelectedItems.Cast<PluginTag>().Select(tag => tag.Name).Should().BeEquivalentTo("Focus", "Imaging");
                tags.SelectedItems.Cast<PluginTag>().Select(tag => (tag.Name, tag.Count)).Should().BeEquivalentTo(new[] { ("Focus", 2), ("Imaging", 3) });
                manager.ClearFiltersCommand.Execute(null);
                LayoutWindow(host, 1000, 700);
                tags.SelectedItems.Cast<PluginTag>().Should().BeEmpty();
                manager.FilteredPlugins.Should().HaveCount(5);
                manager.SelectedTabIndex = 0;
                LayoutWindow(host, 1000, 700);
                tags.SelectedItems.Cast<PluginTag>().Select(tag => tag.Name).Should().Equal("Focus");
                manager.SelectedTabIndex = 1;
                LayoutWindow(host, 1000, 700);
                tags.SelectedItems.Add(tags.Items.Cast<PluginTag>().Single(tag => tag.Name == "Focus"));
                tags.SelectedItems.Add(tags.Items.Cast<PluginTag>().Single(tag => tag.Name == "Imaging"));
                manager.AvailablePlugins = [
                    new(new PluginManifest { Identifier = focus.Identifier, Name = focus.Name, Version = focus.Version, Tags = ["Tools"] }),
                    new(imaging), new(shared), new(tools)
                ];
                LayoutWindow(host, 1000, 700);
                tags.SelectedItems.Cast<PluginTag>().Select(tag => tag.Name).Should().Equal(new[] { "Imaging" }, "a refresh must retain a valid choice and remove the withdrawn one");
                tags.SelectedItems.Cast<PluginTag>().Single().Count.Should().Be(2);
                manager.FilteredPlugins.Select(entry => entry.Identifier).Should().BeEquivalentTo(imaging.Identifier, shared.Identifier);
                tags.IsDropDownOpen = true;
                LayoutWindow(host, 1000, 700);
                var remainingItem = (SelectorItem)tags.ItemContainerGenerator.ContainerFromItem(tags.Items.Cast<PluginTag>().Single(tag => tag.Name == "Imaging"));
                var remainingCheck = FindVisual<CheckBox>(remainingItem)!;
                ((IToggleProvider)new CheckBoxAutomationPeer(remainingCheck).GetPattern(PatternInterface.Toggle)).Toggle();
                LayoutWindow(host, 1000, 700);
                tags.SelectedItems.Cast<PluginTag>().Should().BeEmpty();
                manager.FilteredPlugins.Should().HaveCount(4);
            } finally {
                host.Close();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tags_OmittedFromThePickerRemainSearchableAndVisibleInDetails(bool installed) {
            var manager = CreateManager();
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            var first = new PluginManifest {
                Identifier = "rare-tag", Name = "Tagged fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Focus", "Rare"]
            };
            var second = new PluginManifest {
                Identifier = "shared-tag", Name = "Other fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Focus"]
            };
            var third = new PluginManifest {
                Identifier = "another-shared-tag", Name = "Third fixture", Version = new PluginVersion("1.0.0.0"), Tags = ["Focus"]
            };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [first] = true };
            manager.AvailablePlugins = [new(first), new(second), new(third)];
            manager.SelectedTabIndex = installed ? 0 : 1;
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            try {
                host.Show();
                LayoutWindow(host, 1000, 700);
                var tags = (CheckComboBox)view.FindName("PluginTagFilter");
                tags.Items.Cast<PluginTag>().Should().ContainSingle().Which.Should().Be(new PluginTag("Focus", installed ? 1 : 3));
                ((TextBox)view.FindName("PluginSearch")).Text = "rare";
                ((TabItem)view.FindName("InfoTab")).IsSelected = true;
                LayoutWindow(host, 1000, 700);
                manager.FilteredPlugins.Should().ContainSingle().Which.Identifier.Should().Be(first.Identifier);
                manager.AvailableTags.Single().Count.Should().Be(installed ? 1 : 3);
                var info = (ScrollViewer)view.FindName("InfoContent");
                FindVisuals<TextBlock>(info).Single(label => label.Text == "Focus, Rare").IsVisible.Should().BeTrue();
            } finally {
                host.Close();
            }
        }

        [Test]
        public void Controls_UseSharedNinaButtonAndTabStyles() {
            var manager = CreateManager();
            manager.AvailablePlugins = [new(new PluginManifest {
                Identifier = "native-controls", Name = "Native controls fixture", Version = new PluginVersion("1.0.0.0")
            })];
            var view = new PluginsView { DataContext = manager };
            ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
            Layout(view);
            var standardButton = (Style)view.FindResource("StandardButton");
            var backgroundButton = (Style)view.FindResource("BackgroundButton");
            var buttons = FindVisuals<Button>(view).ToArray();
            buttons.Should().NotBeEmpty();
            foreach (var button in buttons) {
                button.Style.Should().BeOneOf(standardButton, backgroundButton);
                button.Template.Should().BeSameAs(button.Style.Setters.OfType<Setter>()
                    .Single(setter => setter.Property == Control.TemplateProperty).Value);
                if (button.Name == "RefreshPluginsButton") {
                    FindVisual<System.Windows.Shapes.Path>(button)!.Data.Should().BeSameAs(view.FindResource("LoopSVG"));
                    button.GetValue(System.Windows.Automation.AutomationProperties.NameProperty).Should().NotBeNull();
                } else {
                    var caption = FindVisual<TextBlock>(button)!;
                    caption.Text.Should().NotBeNullOrWhiteSpace();
                    caption.Style.Should().BeSameAs(view.FindResource("StandardTextBlock"));
                    AssertBrushColor(caption.Foreground, button.Style == backgroundButton ? button.Foreground : view.FindResource("ButtonForegroundBrush"));
                }
            }

            foreach (string name in new[] { "PluginNavigation", "DetailsNavigation" }) {
                var tabs = (TabControl)view.FindName(name);
                bool sidebar = name == "PluginNavigation";
                var itemStyle = (Style)view.FindResource(sidebar ? "LeftTabItem" : "TopTabItem");
                tabs.ItemContainerStyle.Should().BeSameAs(itemStyle);
                foreach (var tab in tabs.Items.Cast<TabItem>().Where(item => item.Visibility == Visibility.Visible)) {
                    tab.Style.Should().BeSameAs(itemStyle);
                    FindVisual<TextBlock>(tab)!.Style.Should().BeSameAs(view.FindResource(sidebar ? "SideBarTextBlock" : "TabItemTextBox"));
                }
            }
        }

        [Test]
        public void Installed_ShowsAllLocalPluginsWithUpdatesFirstAndOnlySearchAndTagFilters() {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == "Persian Faint");
            var manager = CreateManager();
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            var installed = new PluginManifest { Identifier = "has-update", Name = "Update fixture", Version = new PluginVersion("1.0.0.0") };
            var current = new PluginManifest { Identifier = "current", Name = "Current fixture", Version = new PluginVersion("1.0.0.0") };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true, [current] = true };
            manager.AvailablePlugins = [
                new(new PluginManifest { Identifier = installed.Identifier, Name = installed.Name, Version = new PluginVersion("2.0.0.0") }),
                new(current)
            ];
            manager.SelectedTabIndex = 0;
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            try {
                host.Show();
                LayoutWindow(host, 720, 560);
                view.FindName("UpdatesOnlyFilter").Should().BeNull();
                view.FindName("UpdatesOnlyLabel").Should().BeNull();
                var list = (ListBox)view.FindName("PluginList");
                list.Items.Cast<PluginEntryVM>().Select(entry => entry.Identifier).Should().Equal(installed.Identifier, current.Identifier);
                var tags = (CheckComboBox)view.FindName("PluginTagFilter");
                var tagBounds = tags.TransformToAncestor(view).TransformBounds(new Rect(tags.RenderSize));
                var listBounds = list.TransformToAncestor(view).TransformBounds(new Rect(list.RenderSize));
                var refresh = (Button)view.FindName("RefreshPluginsButton");
                refresh.Command.Should().BeSameAs(manager.FetchPluginsCommand);
                refresh.ToolTip.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerRefresh"]);
                refresh.GetValue(System.Windows.Automation.AutomationProperties.NameProperty).Should().Be(refresh.ToolTip);
                var refreshBounds = refresh.TransformToAncestor(view).TransformBounds(new Rect(refresh.RenderSize));
                var search = (HintTextBox)view.FindName("PluginSearch");
                var searchBounds = search.TransformToAncestor(view).TransformBounds(new Rect(search.RenderSize));
                var hint = (TextBlock)search.Template.FindName("PART_HintText", search);
                FindVisuals<TextBlock>(view).Single(label => label.Text == search.HintText).Should().BeSameAs(hint);
                hint.IsVisible.Should().BeTrue();
                search.Text.Should().BeEmpty();
                refreshBounds.Right.Should().BeApproximately(tagBounds.Right, 0.1);
                refreshBounds.Top.Should().BeApproximately(searchBounds.Top, 0.1);
                refreshBounds.Height.Should().BeApproximately(searchBounds.Height, 0.1);
                refreshBounds.Left.Should().BeApproximately(searchBounds.Right + 5, 0.1);
                FindVisuals<TextBlock>(view).Should().NotContain(label => label.Text == NINA.Core.Locale.Loc.Instance["LblPlugins"]);
                var details = (FrameworkElement)view.FindName("PluginDetails");
                var detailsBounds = details.TransformToAncestor(view).TransformBounds(new Rect(details.RenderSize));
                detailsBounds.Top.Should().BeApproximately(8, 0.1);
                var updateAll = (Button)view.FindName("UpdateAllPluginsButton");
                var updateAllBounds = updateAll.TransformToAncestor(view).TransformBounds(new Rect(updateAll.RenderSize));
                updateAll.Visibility.Should().Be(Visibility.Visible);
                updateAllBounds.Top.Should().BeApproximately(tagBounds.Bottom + 8, 0.1);
                updateAllBounds.Right.Should().BeApproximately(tagBounds.Right, 0.1);
                listBounds.Top.Should().BeApproximately(updateAllBounds.Bottom + 8, 0.1);
                updateAll.Command.Should().BeSameAs(manager.UpdateAllPluginsCommand);
                Keyboard.Focus(tags).Should().BeSameAs(tags);
                tags.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)).Should().BeTrue();
                updateAll.IsKeyboardFocusWithin.Should().BeTrue("Update all follows the tag filter on Installed");
                updateAll.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)).Should().BeTrue();
                list.IsKeyboardFocusWithin.Should().BeTrue();
                search.Text = "Current";
                LayoutWindow(host, 720, 560);
                list.Items.Cast<PluginEntryVM>().Should().ContainSingle().Which.Identifier.Should().Be(current.Identifier);
                manager.AvailablePluginUpdateCount.Should().Be(1);
                ((TextBlock)updateAll.Content).Text.Should().Be(manager.UpdateAllButtonText);
                updateAll.IsVisible.Should().BeTrue("filtering must not hide updates outside the visible results");
                manager.ClearFiltersCommand.Execute(null);
                LayoutWindow(host, 720, 560);
                list.Items.Count.Should().Be(2);
                manager.ShowAvailableCommand.Execute(null);
                LayoutWindow(host, 720, 560);
                list.Items.Count.Should().Be(2);
                updateAll.IsVisible.Should().BeFalse("Update all belongs to Installed");
                tagBounds = tags.TransformToAncestor(view).TransformBounds(new Rect(tags.RenderSize));
                listBounds = list.TransformToAncestor(view).TransformBounds(new Rect(list.RenderSize));
                listBounds.Top.Should().BeApproximately(tagBounds.Bottom + 8, 0.1);

                manager.SelectedTabIndex = 0;
                LayoutWindow(host, 720, 560);
                updateAll.IsVisible.Should().BeTrue();
                manager.AvailablePlugins = [new(installed), new(current)];
                LayoutWindow(host, 720, 560);
                manager.AvailablePluginUpdateCount.Should().Be(0);
                updateAll.IsVisible.Should().BeFalse();
                tagBounds = tags.TransformToAncestor(view).TransformBounds(new Rect(tags.RenderSize));
                listBounds = list.TransformToAncestor(view).TransformBounds(new Rect(list.RenderSize));
                listBounds.Top.Should().BeApproximately(tagBounds.Bottom + 8, 0.1, "no empty action row remains without updates");
                Keyboard.Focus(tags).Should().BeSameAs(tags);
                tags.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)).Should().BeTrue();
                list.IsKeyboardFocusWithin.Should().BeTrue();
            } finally {
                host.Close();
            }
        }

        [TestCase("Persian Faint")]
        [TestCase("Light")]
        [TestCase("Dark Nebula")]
        [TestCase("High Contrast")]
        public void Lists_UseNinaSelectionAndHoverColors(string schema) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            var manager = CreateManager();
            manager.AvailablePlugins = [new(new PluginManifest {
                Identifier = "list-colors", Name = "List colors fixture", Version = new PluginVersion("2.0.0.0")
            })];
            var view = new PluginsView { DataContext = manager };
            Layout(view);
            ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
            Layout(view);
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            Layout(view);
            var mouseOverKey = (DependencyPropertyKey)typeof(UIElement)
                .GetField("IsMouseOverPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

            foreach (string name in new[] { "PluginList", "PluginVersions" }) {
                var list = (ListBox)view.FindName(name);
                var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem);
                var border = (Border)row.Template.FindName("RowBorder", row);
                ((SolidColorBrush)border.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.SecondaryColor,
                    name + " must use NINA's list selection color");
                row.SetValue(mouseOverKey, true);
                try {
                    ((SolidColorBrush)border.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.SecondaryBackgroundColor,
                        name + " must use NINA's list hover color, including a selected row");
                    AssertBrushColor(FindVisual<TextBlock>(row)!.Foreground, view.FindResource("PrimaryBrush"));
                    row.IsSelected = false;
                    ((SolidColorBrush)border.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.SecondaryBackgroundColor);
                } finally {
                    row.SetValue(mouseOverKey, false);
                }

                ((SolidColorBrush)border.Background).Color.A.Should().Be(0, "an ordinary row has no selection or hover fill");
                row.IsSelected = true;
                ((SolidColorBrush)border.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.SecondaryColor);
            }
        }

        [TestCase("Description")]
        [TestCase("Settings")]
        public void DetailPages_BubbleWheelInputPastNestedScrollers(string page) {
            var manifest = new PluginManifest {
                Identifier = "wheel-input", Name = "Wheel input fixture", Version = new PluginVersion("1.0.0.0"),
                Descriptions = new PluginDescription { LongDescription = string.Join("\n\n", Enumerable.Repeat("Description paragraph for scrolling verification.", 100)) }
            };
            string templateKey = manifest.Name + DataTemplatePostfix.Options;
            var nestedScroller = new FrameworkElementFactory(typeof(ScrollViewer));
            nestedScroller.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            var content = new FrameworkElementFactory(typeof(Border));
            content.SetValue(FrameworkElement.HeightProperty, 2000.0);
            nestedScroller.AppendChild(content);
            Application.Current.Resources[templateKey] = new DataTemplate { VisualTree = nestedScroller };
            var manager = CreateManager();
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [manifest] = true };
            manager.SelectedTabIndex = 0;
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            try {
                host.Show();
                ((TabItem)view.FindName(page + "Tab")).IsSelected = true;
                LayoutWindow(host, 1000, 700);
                var outer = (ScrollViewer)view.FindName(page + "Content");
                var inner = FindVisual<ScrollViewer>(outer)!;
                inner.Should().NotBeNull();
                outer.ScrollableHeight.Should().BeGreaterThan(0);
                RaiseWheel(inner, -120);
                LayoutWindow(host, 1000, 700);
                outer.VerticalOffset.Should().BeGreaterThan(0, "the page must scroll when the pointer is over its nested viewer");
                inner.VerticalOffset.Should().Be(0);
                RaiseWheel(inner, 120);
                LayoutWindow(host, 1000, 700);
                outer.VerticalOffset.Should().Be(0);
                outer.ScrollToBottom();
                LayoutWindow(host, 1000, 700);
                double bottom = outer.VerticalOffset;
                RaiseWheel(inner, -120);
                LayoutWindow(host, 1000, 700);
                outer.VerticalOffset.Should().Be(bottom);
                RaiseWheel(inner, 120);
                LayoutWindow(host, 1000, 700);
                outer.VerticalOffset.Should().BeLessThan(bottom);
            } finally {
                host.Close();
                Application.Current.Resources.Remove(templateKey);
            }
        }

        [TestCase(200)]
        [TestCase(404)]
        [TestCase(405)]
        public void Versions_UsesHistoryRouteOrCatalogFallbackAndReusesTheResult(int historyStatusCode) {
            var manager = CreateManager();
            var latest = new PluginManifest {
                Identifier = Guid.NewGuid().ToString(), Name = "Release history", Author = "Test Observatory",
                Version = new PluginVersion("3.0.0.0"), MinimumApplicationVersion = new PluginVersion(NINA.Core.Utility.CoreUtil.Version),
                Repository = "https://example.invalid", License = "MPL-2.0", LicenseURL = "https://example.invalid/license",
                Descriptions = new PluginDescription { ShortDescription = "History fixture" },
                Installer = new PluginInstallerDetails { URL = "https://example.invalid/plugin.dll", Checksum = new string('0', 64), ChecksumType = InstallerChecksum.SHA256, Type = InstallerType.DLL }
            };
            var previous = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginManifest>(Newtonsoft.Json.JsonConvert.SerializeObject(latest))!;
            previous.Version = new PluginVersion("2.0.0.0");
            byte[] Json(params PluginManifest[] values) => Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(values,
                new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() } }));
            using var server = new LoopbackHttpServer(new LoopbackHttpServer.Response[] {
                new(historyStatusCode == 200 ? Json(latest) : Json(previous, latest, previous)),
                new(Json(previous, latest), historyStatusCode)
            });
            var refresh = (Task)typeof(PluginsVM).GetMethod("FetchRepositories", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(manager, [new[] { server.Url }])!;
            PumpUntil(() => refresh.IsCompleted);
            refresh.GetAwaiter().GetResult();
            var view = new PluginsView { DataContext = manager };
            Layout(view);
            server.Requests.Should().ContainSingle();
            manager.CompatibleVersions.Should().HaveCount(historyStatusCode == 200 ? 1 : 2);

            ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
            PumpUntil(() => !manager.IsLoadingVersions);
            Layout(view);

            manager.CompatibleVersions.Select(manifest => manifest.Version.ToString()).Should().Equal("3.0.0.0", "2.0.0.0");
            var versions = (ListBox)view.FindName("PluginVersions");
            versions.Items.Cast<PluginVersionVM>().Select(version => version.VersionText).Should().Equal("3.0.0.0", "2.0.0.0");
            versions.SelectedValue = manager.CompatibleVersions[1];
            Layout(view);
            manager.SelectedVersion.Version.ToString().Should().Be("2.0.0.0");
            manager.InstallSelectedVersionCommand.CanExecute(null).Should().BeTrue();
            FindVisual<TextBlock>(FindVisual<Button>((ListBoxItem)versions.ItemContainerGenerator.ContainerFromItem(versions.SelectedItem))!)!.Text.Should().Be("Install 2.0.0.0");
            manager.HasCatalogError.Should().BeFalse();
            manager.HasVersionHistoryError.Should().Be(historyStatusCode != 200);
            server.Requests.Should().HaveCount(2);
            server.Requests[1].Should().StartWith($"GET /plugins/{latest.Identifier}/versions HTTP/1.1");
            server.Requests[1].Should().Contain("User-Agent: " + NINA.Core.Utility.CoreUtil.UserAgent);
            ((TabItem)view.FindName("DescriptionTab")).IsSelected = true;
            ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
            Layout(view);
            server.Requests.Should().HaveCount(2);
        }

        [Test]
        public void Navigation_InstalledAvailableInstalled_KeepsOnlyInstalledRowsWithReadOnlyLoadedMetadata() {
            var installed = new ReadOnlyLoadedManifest();
            var catalogOnly = new PluginManifest { Identifier = "catalog-only", Name = "Catalog only plugin" };
            var offered = new PluginManifest {
                Identifier = installed.Identifier, Name = installed.Name, License = installed.License,
                LicenseURL = installed.LicenseURL, Installer = new PluginInstallerDetails { URL = installed.Installer.URL }
            };
            var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = new Mock<IPluginLoader>();
            loader.SetupGet(value => value.Plugins).Returns(new Dictionary<IPluginManifest, bool> { [installed] = true });
            loader.Setup(value => value.Load()).Returns(loaded.Task);
            var manager = new PluginsVM(loader.Object, Mock.Of<IProfileService>());
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            manager.AvailablePlugins = [new(offered), new(catalogOnly)];
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                Width = 1000, Height = 700, Left = -20000, Top = -20000,
                ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            var errors = new BindingErrorListener();
            PresentationTraceSources.DataBindingSource.Listeners.Add(errors);
            try {
                host.Show();
                Layout(view);
                loaded.SetResult();
                PumpUntil(() => manager.Initialized);
                Layout(view);
                var navigation = (TabControl)view.FindName("PluginNavigation");
                var availableTab = (TabItem)view.FindName("AvailablePluginsTab");
                var installedTab = (TabItem)view.FindName("InstalledPluginsTab");
                var list = (ListBox)view.FindName("PluginList");
                installedTab.IsSelected.Should().BeTrue();

                ((ISelectionItemProvider)new TabItemAutomationPeer(availableTab, new TabControlAutomationPeer(navigation)).GetPattern(PatternInterface.SelectionItem)).Select();
                Layout(view);
                list.Items.Cast<PluginEntryVM>().Should().HaveCount(2);

                ((ISelectionItemProvider)new TabItemAutomationPeer(installedTab, new TabControlAutomationPeer(navigation)).GetPattern(PatternInterface.SelectionItem)).Select();
                Layout(view);

                installedTab.IsSelected.Should().BeTrue();
                manager.IsInstalledTab.Should().BeTrue();
                list.Items.Cast<PluginEntryVM>().Select(entry => entry.Identifier).Should().Equal(new[] { installed.Identifier },
                    "the visible Installed list must exclude catalog-only rows even with read-only plugin metadata; binding errors: " + string.Join(Environment.NewLine, errors.Messages));
                manager.SelectedPlugin.InstalledManifest.Should().BeSameAs(installed);
                manager.HasCatalogError.Should().BeFalse(manager.CatalogError);
                errors.Messages.Should().BeEmpty();
            } finally {
                loaded.TrySetResult();
                PresentationTraceSources.DataBindingSource.Listeners.Remove(errors);
                host.Close();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Navigation_UsesTheVisibleTabToSeparateInstalledAndCatalogPlugins(bool collapsed) {
            bool previousCollapsed = NINA.Properties.Settings.Default.CollapsedSidebar;
            NINA.Properties.Settings.Default.CollapsedSidebar = collapsed;
            var manager = CreateManager();
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            var installed = new PluginManifest { Identifier = "installed", Name = "Installed plugin" };
            var catalogOnly = new PluginManifest { Identifier = "catalog-only", Name = "Catalog only plugin" };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            manager.AvailablePlugins = [new(installed), new(catalogOnly)];
            manager.SelectedTabIndex = 0;
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                Width = 1000, Height = 700, Left = -20000, Top = -20000,
                ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            try {
                host.Show();
                Layout(view);
                var navigation = (TabControl)view.FindName("PluginNavigation");
                var installedTab = navigation.Items.Cast<TabItem>().Single(tab =>
                    tab.Header.ToString() == NINA.Core.Locale.Loc.Instance["LblInstalled"]);
                var availableTab = navigation.Items.Cast<TabItem>().Single(tab =>
                    tab.Header.ToString() == NINA.Core.Locale.Loc.Instance["LblAvailable"]);
                var list = (ListBox)view.FindName("PluginList");
                navigation.ActualWidth.Should().Be(collapsed ? 40 : 65);
                foreach (var tab in new[] { installedTab, availableTab }) {
                    tab.ToolTip.Should().Be(tab.Header);
                    tab.GetValue(System.Windows.Automation.AutomationProperties.NameProperty).Should().Be(tab.Header);
                    tab.FocusVisualStyle.Should().NotBeNull();
                    FindVisuals<System.Windows.Shapes.Path>(tab).Count(path => path.IsVisible).Should().Be(1);
                    FindVisual<TextBlock>(tab)!.IsVisible.Should().Be(!collapsed);
                }

                var navigationBounds = navigation.TransformToAncestor(view).TransformBounds(new Rect(navigation.RenderSize));
                var listBounds = list.TransformToAncestor(view).TransformBounds(new Rect(list.RenderSize));
                navigationBounds.Top.Should().Be(0);
                navigationBounds.Bottom.Should().BeApproximately(view.ActualHeight, 0.1);
                listBounds.Left.Should().BeGreaterThanOrEqualTo(navigationBounds.Right);

                installedTab.IsSelected.Should().BeTrue();
                list.Items.Cast<PluginEntryVM>().Select(entry => entry.Identifier).Should().Equal(installed.Identifier);
                for (int iteration = 0; iteration < 3; iteration++) {
                    ((ISelectionItemProvider)new TabItemAutomationPeer(availableTab, new TabControlAutomationPeer(navigation)).GetPattern(PatternInterface.SelectionItem)).Select();
                    Layout(view);
                    manager.IsInstalledTab.Should().BeFalse();
                    list.Items.Cast<PluginEntryVM>().Should().HaveCount(2);
                    list.SelectedItem = manager.FilteredPlugins.Single(entry => entry.Identifier == catalogOnly.Identifier);

                    ((ISelectionItemProvider)new TabItemAutomationPeer(installedTab, new TabControlAutomationPeer(navigation)).GetPattern(PatternInterface.SelectionItem)).Select();
                    Layout(view);
                    manager.IsInstalledTab.Should().BeTrue();
                    list.Items.Cast<PluginEntryVM>().Select(entry => entry.Identifier).Should().Equal(installed.Identifier);
                    manager.SelectedPlugin.Identifier.Should().Be(installed.Identifier);
                }

                Keyboard.Focus(installedTab).Should().BeSameAs(installedTab);
                foreach (var direction in new[] { FocusNavigationDirection.Down, FocusNavigationDirection.Up }) {
                    var focusedTab = (TabItem)navigation.SelectedItem;
                    focusedTab.MoveFocus(new TraversalRequest(direction)).Should().BeTrue();
                    Layout(view);
                    navigation.SelectedIndex.Should().Be(direction == FocusNavigationDirection.Down ? 1 : 0);
                    manager.IsInstalledTab.Should().Be(direction == FocusNavigationDirection.Up);
                }

                installedTab.Header = "Installed plugins with a deliberately long translated label";
                Layout(view);
                navigation.ActualWidth.Should().Be(collapsed ? 40 : 65);
                FindVisual<TextBlock>(installedTab)!.Text.Should().Be(installedTab.Header.ToString());
                NINA.Properties.Settings.Default.CollapsedSidebar = !collapsed;
                Layout(view);
                navigation.ActualWidth.Should().Be(collapsed ? 65 : 40);
                manager.IsInstalledTab.Should().BeTrue();
            } finally {
                host.Close();
                NINA.Properties.Settings.Default.CollapsedSidebar = previousCollapsed;
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Navigation_SwitchDuringListRebinding_UsesTheNewSection(bool switchToInstalled) {
            var manager = CreateManager();
            var installed = new PluginManifest { Identifier = "local", Name = "Local plugin" };
            var catalogOnly = new PluginManifest { Identifier = "remote", Name = "Remote plugin" };
            var rememberedLocal = new PluginManifest { Identifier = "remembered-local", Name = "Second local plugin" };
            var rememberedRemote = new PluginManifest { Identifier = "remembered-remote", Name = "Second remote plugin" };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true, [rememberedLocal] = true };
            manager.AvailablePlugins = switchToInstalled
                ? [new(new PluginManifest { Identifier = "departing", Name = "Removed from catalog" })]
                : [new(catalogOnly), new(rememberedRemote)];
            if (switchToInstalled) {
                manager.SelectedTabIndex = 0;
                manager.SelectedPlugin = manager.FilteredPlugins.Single(entry => entry.Identifier == rememberedLocal.Identifier);
                manager.SelectedTabIndex = 1;
            } else {
                manager.SelectedPlugin = manager.FilteredPlugins.Single(entry => entry.Identifier == rememberedRemote.Identifier);
                manager.SelectedTabIndex = 0;
            }

            var view = new PluginsView { DataContext = manager };
            Layout(view);
            var list = (ListBox)view.FindName("PluginList");
            var targetTab = (TabItem)view.FindName(switchToInstalled ? "InstalledPluginsTab" : "AvailablePluginsTab");
            bool switched = false;
            list.SelectionChanged += (_, _) => {
                if (!switched) {
                    switched = true;
                    targetTab.IsSelected = true;
                }
            };

            if (switchToInstalled) {
                manager.AvailablePlugins = [new(installed), new(catalogOnly)];
            } else {
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> {
                    [new PluginManifest { Identifier = "replacement", Name = "Replacement local plugin" }] = true
                };
            }

            Layout(view);

            switched.Should().BeTrue();
            targetTab.IsSelected.Should().BeTrue();
            manager.IsInstalledTab.Should().Be(switchToInstalled);
            string[] expectedIdentifiers = switchToInstalled
                ? [installed.Identifier, rememberedLocal.Identifier]
                : [catalogOnly.Identifier, rememberedRemote.Identifier];
            list.Items.Cast<PluginEntryVM>().Select(entry => entry.Identifier).Should().BeEquivalentTo(expectedIdentifiers);
            manager.SelectedPlugin.Identifier.Should().Be(switchToInstalled ? rememberedLocal.Identifier : rememberedRemote.Identifier);
        }

        [Test]
        public void CatalogRefresh_PublishesResultsToTheBoundListOnTheUiThread() {
            var manager = CreateManager();
            var view = new PluginsView { DataContext = manager };
            Layout(view);
            var manifest = new PluginManifest {
                Identifier = Guid.NewGuid().ToString(), Name = "Offered plugin", Author = "Test Observatory",
                Version = new PluginVersion("1.0.0.0"), MinimumApplicationVersion = new PluginVersion(CoreUtil.Version),
                Repository = "https://example.invalid", License = "MPL-2.0", LicenseURL = "https://example.invalid/license",
                Descriptions = new PluginDescription { ShortDescription = "Catalog refresh fixture" },
                Installer = new PluginInstallerDetails {
                    URL = "https://example.invalid/plugin.dll", Checksum = new string('0', 64),
                    ChecksumType = InstallerChecksum.SHA256, Type = InstallerType.DLL
                }
            };
            byte[] json = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(new[] { manifest },
                new Newtonsoft.Json.JsonSerializerSettings {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
                }));
            using var success = new LoopbackHttpServer(json, "application/json");
            using var failure = new LoopbackHttpServer([], statusCode: 503);

            int ownerThread = Environment.CurrentManagedThreadId;
            var changedThreads = new List<int>();
            manager.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(PluginsVM.AvailablePlugins)) {
                    changedThreads.Add(Environment.CurrentManagedThreadId);
                }
            };

            var fetch = typeof(PluginsVM).GetMethod("FetchRepositories", BindingFlags.Instance | BindingFlags.NonPublic)!;

            var refresh = (Task)fetch.Invoke(manager, [new[] { success.Url, failure.Url }])!;
            PumpUntil(() => refresh.IsCompleted);
            refresh.GetAwaiter().GetResult();
            Layout(view);

            changedThreads.Should().NotBeEmpty().And.OnlyContain(thread => thread == ownerThread);
            var list = (ListBox)view.FindName("PluginList");
            list.Items.Cast<PluginEntryVM>().Should().ContainSingle().Which.Identifier.Should().Be(manifest.Identifier);
            manager.HasCatalogError.Should().BeTrue();
        }

        [TestCase("complete", false)]
        [TestCase("cancel", false)]
        [TestCase("failure", false)]
        [TestCase("complete", true)]
        [TestCase("cancel", true)]
        [TestCase("failure", true)]
        public void CatalogProgress_UsesTheBottomStatusAreaAndRetainsRestartNotice(string outcome, bool pendingRestart) {
            WithChangelogCache(_ => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var repository = new LoopbackHttpServer(Encoding.UTF8.GetBytes("[]"), "application/json",
                    responseGate: gate.Task, statusCode: outcome == "failure" ? 503 : 200);
                var manifest = new PluginManifest {
                    Identifier = "refresh-footer", Name = "Refresh footer fixture", Version = new PluginVersion("1.0.0.0")
                };
                var manager = CreateManager();
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [manifest] = true };
                manager.AvailablePlugins = [new(manifest)];
                manager.SelectedPlugin.PendingChange = pendingRestart ? PluginPendingChange.Update : PluginPendingChange.None;
                manager.SelectedTabIndex = 0;
                var view = new PluginsView { DataContext = manager };
                var host = ShowChangelogView(view);
                host.WindowStyle = WindowStyle.None;
                host.ResizeMode = ResizeMode.NoResize;
                LayoutWindow(host, 800, 600);
                var restart = (Button)view.FindName("RestartPluginsButton");
                var restartNotice = (FrameworkElement)restart.Parent;
                var footer = ((Grid)view.Content).Children.OfType<Border>().Single(border => border.IsAncestorOf(restart));
                var browser = (FrameworkElement)view.FindName("BrowserGrid");
                double browserTop = browser.TransformToAncestor(view).Transform(new Point()).Y;
                var cancel = FindVisuals<Button>(view).Single(button => button.Command == manager.CancelFetchPluginsCommand);
                var progress = (FrameworkElement)cancel.Parent;
                footer.Visibility.Should().Be(pendingRestart ? Visibility.Visible : Visibility.Collapsed);
                var fetch = typeof(PluginsVM).GetMethod("FetchRepositories", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var refresh = (Task)fetch.Invoke(manager, [new[] { repository.Url }])!;
                try {
                    PumpUntil(() => repository.RequestReceived.IsCompleted);
                    LayoutWindow(host, 800, 600);
                    footer.Visibility.Should().Be(Visibility.Visible, "catalog loading belongs to the shared bottom status area");
                    footer.IsAncestorOf(progress).Should().BeTrue();
                    progress.Visibility.Should().Be(Visibility.Visible);
                    FindVisuals<TextBlock>(progress).Should().Contain(label => label.Text == NINA.Core.Locale.Loc.Instance["LblPluginManagerLoadingCatalog"]);
                    FindVisual<ProgressBar>(progress)!.IsIndeterminate.Should().BeTrue();
                    cancel.Style.Should().BeSameAs(view.FindResource("BackgroundButton"));
                    browser.TransformToAncestor(view).Transform(new Point()).Y.Should().BeApproximately(browserTop, 0.1);
                    progress.TransformToAncestor(view).TransformBounds(new Rect(progress.RenderSize)).Top.Should()
                        .BeGreaterThanOrEqualTo(browser.TransformToAncestor(view).TransformBounds(new Rect(browser.RenderSize)).Bottom);
                    restartNotice.Visibility.Should().Be(pendingRestart ? Visibility.Visible : Visibility.Collapsed);
                    manager.SelectedTabIndex = 1;
                    manager.SearchText = "No matching fixture";
                    manager.SelectedTabIndex = 0;
                    LayoutWindow(host, 800, 600);
                    progress.Visibility.Should().Be(Visibility.Visible);

                    if (outcome == "cancel") {
                        ((IInvokeProvider)new ButtonAutomationPeer(cancel).GetPattern(PatternInterface.Invoke)).Invoke();
                    } else {
                        gate.TrySetResult();
                    }

                    PumpUntil(() => refresh.IsCompleted);
                    refresh.GetAwaiter().GetResult();
                    LayoutWindow(host, 800, 600);
                    manager.IsFetching.Should().BeFalse();
                    manager.HasCatalogError.Should().Be(outcome == "failure");
                    progress.Visibility.Should().Be(Visibility.Collapsed);
                    footer.Visibility.Should().Be(pendingRestart ? Visibility.Visible : Visibility.Collapsed);
                    restartNotice.Visibility.Should().Be(pendingRestart ? Visibility.Visible : Visibility.Collapsed);
                } finally {
                    manager.CancelFetchPluginsCommand.Execute(null);
                    gate.TrySetResult();
                    PumpUntil(() => refresh.IsCompleted);
                    refresh.GetAwaiter().GetResult();
                }
            });
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void MutationProgress_UsesTheBottomStatusAreaAndTransitionsToRestart(bool update, bool cancel) {
            WithChangelogCache(_ => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                byte[] payload = Encoding.UTF8.GetBytes("Status area fixture");
                string name = "Status fixture " + Guid.NewGuid().ToString("N");
                using var download = new LoopbackHttpServer(payload, fileName: name + ".dll", responseGate: gate.Task);
                var offered = new PluginManifest {
                    Identifier = Guid.NewGuid().ToString(), Name = name, Version = new PluginVersion("2.0.0.0"),
                    MinimumApplicationVersion = new PluginVersion(CoreUtil.Version),
                    Installer = new PluginInstallerDetails {
                        Type = InstallerType.DLL, URL = download.Url + "/download",
                        Checksum = Convert.ToHexString(SHA256.HashData(payload)), ChecksumType = InstallerChecksum.SHA256
                    }
                };
                var other = new PluginManifest { Identifier = "other-status", Name = "Other status fixture" };
                var manager = CreateManager();
                if (update) {
                    var installed = new PluginManifest { Identifier = offered.Identifier, Name = name, Version = new PluginVersion("1.0.0.0") };
                    manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
                    string folder = Path.Combine(NINA.Plugin.Constants.UserExtensionsFolder, name);
                    Directory.CreateDirectory(folder);
                    File.WriteAllText(Path.Combine(folder, "Old.dll"), "old fixture");
                }

                manager.AvailablePlugins = [new(offered), new(other)];
                manager.SelectedPlugin = manager.FilteredPlugins.Single(entry => entry.Identifier == offered.Identifier);
                var view = new PluginsView { DataContext = manager };
                var host = ShowChangelogView(view);
                host.WindowStyle = WindowStyle.None;
                host.ResizeMode = ResizeMode.NoResize;
                LayoutWindow(host, 800, 600);
                var restart = (Button)view.FindName("RestartPluginsButton");
                var restartNotice = (FrameworkElement)restart.Parent;
                var footer = ((Grid)view.Content).Children.OfType<Border>().Single(border => border.IsAncestorOf(restart));
                var cancelButton = FindVisuals<Button>(view).Single(button => button.Command == manager.CancelInstallPluginCommand);
                var progress = (FrameworkElement)cancelButton.Parent;
                footer.Visibility.Should().Be(Visibility.Collapsed);
                Task operation = update ? manager.UpdatePluginCommand.ExecuteAsync(null) : manager.InstallPluginCommand.ExecuteAsync(null);
                try {
                    PumpUntil(() => download.RequestReceived.IsCompleted);
                    Layout(view, 800, 600);
                    footer.Visibility.Should().Be(Visibility.Visible, "active changes belong to the shared bottom status area");
                    footer.IsAncestorOf(progress).Should().BeTrue();
                    progress.Visibility.Should().Be(Visibility.Visible);
                    restartNotice.Visibility.Should().Be(Visibility.Collapsed);
                    FindVisuals<TextBlock>(progress).Should().Contain(label => label.Text == NINA.Core.Locale.Loc.Instance[update ? "LblPluginManagerUpdating" : "LblPluginManagerInstalling"]);
                    var browser = (FrameworkElement)view.FindName("BrowserGrid");
                    progress.TransformToAncestor(view).TransformBounds(new Rect(progress.RenderSize)).Top.Should()
                        .BeGreaterThanOrEqualTo(browser.TransformToAncestor(view).TransformBounds(new Rect(browser.RenderSize)).Bottom);
                    FindVisual<ProgressBar>(progress)!.IsIndeterminate.Should().BeTrue();
                    cancelButton.Style.Should().BeSameAs(view.FindResource("BackgroundButton"));

                    var previousSuccess = manager.FilteredPlugins.Single(entry => entry.Identifier == other.Identifier);
                    previousSuccess.PendingChange = PluginPendingChange.Install;
                    manager.SearchText = other.Name;
                    Layout(view, 800, 600);
                    restartNotice.Visibility.Should().Be(Visibility.Visible, "earlier successful changes retain their restart notice");
                    manager.RestartCommand.CanExecute(null).Should().BeFalse();
                    previousSuccess.PendingChange = PluginPendingChange.None;
                    manager.SearchText = string.Empty;
                    manager.SelectedTabIndex = 0;
                    Layout(view, 800, 600);
                    progress.Visibility.Should().Be(Visibility.Visible, "activity is independent of selection, filters and the current section");

                    if (cancel) {
                        ((IInvokeProvider)new ButtonAutomationPeer(cancelButton).GetPattern(PatternInterface.Invoke)).Invoke();
                    } else {
                        gate.TrySetResult();
                    }

                    PumpUntil(() => operation.IsCompleted);
                    operation.GetAwaiter().GetResult();
                    Layout(view, 800, 600);
                    progress.Visibility.Should().Be(Visibility.Collapsed);
                    manager.IsMutating.Should().BeFalse();
                    footer.Visibility.Should().Be(cancel ? Visibility.Collapsed : Visibility.Visible);
                    restartNotice.Visibility.Should().Be(cancel ? Visibility.Collapsed : Visibility.Visible);
                } finally {
                    manager.CancelInstallPluginCommand.Execute(null);
                    gate.TrySetResult();
                    PumpUntil(() => operation.IsCompleted);
                    operation.GetAwaiter().GetResult();
                }
            });
        }

        [Test]
        public void OperationErrors_StayVisibleAcrossFiltersSectionsAndSelectionChanges() {
            var manager = CreateManager();
            var failed = new PluginManifest { Identifier = "failed", Name = "Failed plugin", Version = new PluginVersion("1.0.0.0") };
            var other = new PluginManifest { Identifier = "other", Name = "Other plugin", Version = new PluginVersion("1.0.0.0") };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [failed] = true, [other] = true };
            manager.AvailablePlugins = [new(failed), new(other)];
            var entry = manager.FilteredPlugins.Single(plugin => plugin.Identifier == failed.Identifier);
            var view = new PluginsView { DataContext = manager };
            Layout(view);
            var errors = (Border)view.FindName("PluginOperationErrors");
            errors.Visibility.Should().Be(Visibility.Collapsed);

            entry.OperationError = "Fixture download failed";
            manager.SearchText = other.Name;
            Layout(view);
            manager.SelectedPlugin.Identifier.Should().Be(other.Identifier);
            errors.Visibility.Should().Be(Visibility.Visible);
            FindVisual<TextBlock>(errors)!.Text.Should().Contain(failed.Name).And.Contain(entry.OperationError);
            manager.SelectedTabIndex = 0;
            Layout(view);
            errors.Visibility.Should().Be(Visibility.Visible);
            manager.SelectedPlugin = manager.FilteredPlugins.Single(plugin => plugin.Identifier == other.Identifier);
            manager.AvailablePlugins = [new(other)];
            Layout(view);
            FindVisual<TextBlock>(errors)!.Text.Should().Contain(failed.Name);

            entry.OperationError = string.Empty;
            manager.AvailablePlugins = [new(other)];
            Layout(view);
            errors.Visibility.Should().Be(Visibility.Collapsed);
        }

        [Test]
        public void Settings_KeepApplicationTextAndHyperlinkStyles() {
            var manifest = new SettingsManifest { Identifier = "settings-styles", Name = "Settings style fixture" };
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetValue(TextBlock.TextProperty, "Plugin settings label");
            string optionsKey = manifest.Name + DataTemplatePostfix.Options;
            Application.Current.Resources[optionsKey] = new DataTemplate { VisualTree = factory };
            try {
                var manager = CreateManager();
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [manifest] = true };
                manager.SelectedTabIndex = 0;
                var view = new PluginsView { DataContext = manager };
                Layout(view);
                var label = FindVisual<TextBlock>((ContentControl)view.FindName("SettingsPresenter"))!;
                label.Style.Should().BeSameAs(Application.Current.FindResource(typeof(TextBlock)));
                label.TextWrapping.Should().Be(TextWrapping.NoWrap);
                label.FindResource(typeof(System.Windows.Documents.Hyperlink)).Should()
                    .BeSameAs(Application.Current.FindResource(typeof(System.Windows.Documents.Hyperlink)));
            } finally {
                Application.Current.Resources.Remove(optionsKey);
            }
        }

        [Test]
        public void ListWidth_UsesTheDeclaredMaximumAndPreservesTheDetailMinimum() {
            var view = new PluginsView { DataContext = CreateManager() };
            var browser = (Grid)view.FindName("BrowserGrid");
            var column = browser.ColumnDefinitions[0];
            double declaredMaximum = column.MaxWidth;
            column.Width = new GridLength(declaredMaximum);
            Layout(view, 1600, 900);
            column.ActualWidth.Should().BeApproximately(declaredMaximum, 0.1);
            Layout(view, 720, 560);
            browser.ColumnDefinitions[2].ActualWidth.Should().BeGreaterThanOrEqualTo(320);
            Layout(view, 1600, 900);
            column.ActualWidth.Should().BeApproximately(declaredMaximum, 0.1);
        }

        [TestCase(180)]
        [TestCase(1000)]
        public void Settings_StarSizedPluginRowsStartAtTheTopAndScrollWhenNeeded(int formHeight) {
            var manifest = new SettingsManifest { Identifier = "settings-layout", Name = "Settings layout fixture" };
            var template = new DataTemplate { VisualTree = new FrameworkElementFactory(typeof(Grid)) };
            string optionsKey = manifest.Name + DataTemplatePostfix.Options;
            Application.Current.Resources[optionsKey] = template;

            try {
                var manager = CreateManager();
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [manifest] = true };
                manager.SelectedTabIndex = 0;
                var view = new PluginsView { DataContext = manager };
                Layout(view);
                var presenter = (ContentControl)view.FindName("SettingsPresenter");
                var scroll = (ScrollViewer)view.FindName("SettingsContent");
                var pluginRoot = FindVisual<Grid>(presenter)!;
                for (int row = 0; row < 3; row++) {
                    pluginRoot.RowDefinitions.Add(new RowDefinition());
                }

                var notice = new TextBlock { Height = 32, Text = "Plugin settings fixture notice" };
                var form = new Border { Height = formHeight, Child = new TextBox { Text = "Settings fixture value" } };
                Grid.SetRow(notice, 1);
                Grid.SetRow(form, 2);
                pluginRoot.Children.Add(notice);
                pluginRoot.Children.Add(form);
                Layout(view);

                double noticeTop = notice.TransformToAncestor(scroll).Transform(new Point()).Y;
                TestContext.Out.WriteLine(
                    $"Viewport {scroll.ViewportHeight}, presenter {presenter.ActualHeight}, " +
                    $"plugin desired {pluginRoot.DesiredSize.Height}, plugin actual {pluginRoot.ActualHeight}, " +
                    $"first row {pluginRoot.RowDefinitions[0].ActualHeight}, notice top {noticeTop}");
                noticeTop.Should().BeApproximately(0, 0.1, "unused star rows must not expand above the plugin settings");
                presenter.Content.Should().BeSameAs(manifest);
                if (formHeight > scroll.ViewportHeight) {
                    scroll.ScrollableHeight.Should().BeGreaterThan(0);
                    scroll.ScrollToBottom();
                    Layout(view);
                    scroll.VerticalOffset.Should().BeGreaterThan(0);
                    scroll.ScrollToTop();
                    Layout(view);
                    notice.TransformToAncestor(scroll).Transform(new Point()).Y.Should().BeApproximately(0, 0.1);
                } else {
                    scroll.ScrollableHeight.Should().Be(0);
                }

            } finally {
                Application.Current.Resources.Remove(optionsKey);
            }
        }

        [Test]
        public void Settings_UsesLoadedManifestAndRetainsItsViewAcrossDetailTabsAndRefresh() {
            var original = new SettingsManifest { Identifier = "settings", Name = "Settings test", OptionsValue = "Original", ChangelogURL = "invalid-changelog" };
            var second = new SettingsManifest { Identifier = "second", Name = "Second test", OptionsValue = "Second" };
            var missing = new PluginManifest { Identifier = "missing", Name = "No settings" };
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetBinding(TextBox.TextProperty, new Binding(nameof(SettingsManifest.OptionsValue)) {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            var template = new DataTemplate { VisualTree = factory };
            string firstKey = original.Name + DataTemplatePostfix.Options;
            string secondKey = second.Name + DataTemplatePostfix.Options;
            Application.Current.Resources[firstKey] = template;
            Application.Current.Resources[secondKey] = template;

            try {
                var manager = CreateManager();
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> {
                    [original] = true, [second] = true, [missing] = true
                };
                manager.SelectedTabIndex = 0;
                manager.SelectedInstalledPlugin = new KeyValuePair<IPluginManifest, bool>(original, true);
                var view = new PluginsView { DataContext = manager };
                Layout(view);
                var tabs = (TabControl)view.FindName("DetailsNavigation");
                var settingsTab = (TabItem)view.FindName("SettingsTab");
                settingsTab.Visibility.Should().Be(Visibility.Visible);
                settingsTab.IsSelected.Should().BeTrue("Installed is the place to configure the plugin");
                Layout(view);

                var presenter = (ContentControl)view.FindName("SettingsPresenter");
                presenter.Content.Should().BeSameAs(original);
                var editor = FindVisual<TextBox>(presenter)!;
                editor.Text.Should().Be("Original");
                editor.Text = "Edited through the plugin view";
                original.OptionsValue.Should().Be(editor.Text);

                foreach (string name in new[] { "Description", "Changelog", "Showcase", "Versions", "Info" }) {
                    ((TabItem)view.FindName(name + "Tab")).IsSelected = true;
                    Layout(view);
                    ((FrameworkElement)view.FindName(name + "Content")).Visibility.Should().Be(Visibility.Visible);
                    ((ScrollViewer)view.FindName("SettingsContent")).Visibility.Should().Be(Visibility.Collapsed);
                    settingsTab.IsSelected = true;
                    Layout(view);
                    FindVisual<TextBox>(presenter).Should().BeSameAs(editor);
                }
                manager.AvailablePlugins = new List<ExtendedPluginManifest> {
                    new(new PluginManifest { Identifier = original.Identifier, Name = "Catalog metadata" })
                };
                Layout(view);
                presenter.Content.Should().BeSameAs(original);
                FindVisual<TextBox>(presenter).Should().BeSameAs(editor);

                manager.SelectedInstalledPlugin = new KeyValuePair<IPluginManifest, bool>(second, true);
                Layout(view);
                settingsTab.IsSelected.Should().BeTrue();
                presenter.Content.Should().BeSameAs(second);
                FindVisual<TextBox>(presenter)!.Text.Should().Be("Second");

                manager.SelectedTabIndex = 1;
                Layout(view);
                settingsTab.Visibility.Should().Be(Visibility.Collapsed, "Available is for exploring plugin descriptions");
                tabs.Visibility.Should().Be(Visibility.Visible);
                ((ScrollViewer)view.FindName("DescriptionContent")).Visibility.Should().Be(Visibility.Visible);
                ((Button)view.FindName("UninstallPluginButton")).Visibility.Should().Be(Visibility.Collapsed);
                manager.SelectedTabIndex = 0;
                Layout(view);
                settingsTab.IsSelected.Should().BeTrue();
                presenter.Content.Should().BeSameAs(second);

                manager.SelectedInstalledPlugin = new KeyValuePair<IPluginManifest, bool>(missing, true);
                Layout(view);
                settingsTab.Visibility.Should().Be(Visibility.Collapsed);
                presenter.Content.Should().BeNull();
                ((TextBlock)view.FindName("NoSettingsHint")).Visibility.Should().Be(Visibility.Visible);
            } finally {
                Application.Current.Resources.Remove(firstKey);
                Application.Current.Resources.Remove(secondKey);
            }
        }

        [Test]
        public void DetailTabs_SeparateDescriptionScreenshotsVersionsAndPublisherInformation() {
            var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, new byte[16], 8);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var buffer = new MemoryStream();
            encoder.Save(buffer);
            using var images = new NINA.Test.Plugin.LoopbackHttpServer(buffer.ToArray(), "image/png");
            var manifest = new PluginManifest {
                Identifier = "detail-tabs", Name = "Detail tabs fixture", Version = new PluginVersion("1.0.0.0"),
                MinimumApplicationVersion = new PluginVersion("3.2.0.0"),
                Homepage = "https://example.invalid/home", ChangelogURL = "https://example.invalid/changes",
                Repository = "https://example.invalid/source", License = "MPL-2.0", LicenseURL = "https://example.invalid/license",
                Descriptions = new PluginDescription { LongDescription = "**Description fixture**", ScreenshotURL = images.Url + "/showcase.png" }
            };
            var manager = CreateManager();
            manager.AvailablePlugins = [new(manifest)];
            string originalApplicationTempPath = CoreUtil.APPLICATIONTEMPPATH;
            string imageTestRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PluginArtwork", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(imageTestRoot);
            CoreUtil.APPLICATIONTEMPPATH = imageTestRoot;
            Window? host = null;
            try {
                var view = new PluginsView { DataContext = manager };
                Layout(view);
                ((TabItem)view.FindName("DescriptionTab")).IsSelected.Should().BeTrue();

                foreach (string name in new[] { "Description", "Showcase", "Versions", "Info" }) {
                    ((TabItem)view.FindName(name + "Tab")).IsSelected = true;
                    Layout(view);
                    foreach (string contentName in new[] { "Description", "Showcase", "Versions", "Info" }) {
                        ((FrameworkElement)view.FindName(contentName + "Content")).Visibility.Should().Be(
                            name == contentName ? Visibility.Visible : Visibility.Collapsed);
                    }
                }

                host = ShowChangelogView(view);
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                var gallery = (ImageGallery)view.FindName("ScreenshotGallery");
                PumpUntil(() => gallery.Items.Count == 1);
                var versions = (ListBox)view.FindName("PluginVersions");
                versions.Items.Cast<object>().Should().ContainSingle();
                versions.SelectedValue.Should().BeSameAs(manager.SelectedVersion);
                FindVisual<Button>((ListBoxItem)versions.ItemContainerGenerator.ContainerFromItem(versions.SelectedItem))!.Command.Should().BeSameAs(manager.InstallSelectedVersionCommand);
                var info = (StackPanel)((ScrollViewer)view.FindName("InfoContent")).Content;
                var links = info.Children.OfType<WrapPanel>().Single().Children.OfType<TextBlock>()
                    .SelectMany(block => block.Inlines.OfType<System.Windows.Documents.Hyperlink>())
                    .Select(link => link.NavigateUri.AbsoluteUri);
                links.Should().BeEquivalentTo(manifest.Homepage, manifest.ChangelogURL, manifest.Repository, manifest.LicenseURL);
            } finally {
                host?.Close();
                changelogHosts.Remove(host!);
                CoreUtil.APPLICATIONTEMPPATH = originalApplicationTempPath;
                Directory.Delete(imageTestRoot, true);
            }
        }

        [Test]
        public void Showcase_DisplaysDownloadedImagesWithinTheDetailPane() {
            WithChangelogCache(_ => {
                using var landscape = new LoopbackHttpServer(CreateScreenshot(120, 60, Colors.SteelBlue), "image/png");
                using var portrait = new LoopbackHttpServer(CreateScreenshot(60, 120, Colors.DarkGoldenrod), "image/png");
                var manager = CreateManager();
                manager.AvailablePlugins = [new(new PluginManifest {
                    Identifier = "gallery", Name = "Screenshot gallery fixture", Version = new PluginVersion("1.0.0.0"),
                    Descriptions = new PluginDescription {
                        ScreenshotURL = landscape.Url + "/landscape.png", AltScreenshotURL = portrait.Url + "/portrait.png"
                    }
                })];
                var view = new PluginsView { DataContext = manager };
                var host = ShowChangelogView(view);
                LayoutWindow(host, 800, 600);
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                var content = (Grid)view.FindName("ShowcaseContent");
                var gallery = (ImageGallery)view.FindName("ScreenshotGallery");
                PumpUntil(() => gallery.Items.Count == 2);
                Layout(view, 800, 600);

                content.IsVisible.Should().BeTrue();
                FindVisuals<ScrollViewer>(content).Should().BeEmpty();
                gallery.ActualHeight.Should().BeApproximately(content.ActualHeight, 1,
                    "the gallery uses the available showcase pane rather than a fixed screenshot viewport");
                var image = (System.Windows.Controls.Image)gallery.Template.FindName("PART_Image", gallery);
                foreach (var (index, ratio) in new[] { (0, 2.0), (1, 0.5) }) {
                    gallery.SelectedIndex = index;
                    Layout(view, 800, 600);
                    image.Source.Should().BeSameAs(gallery.SelectedItem);
                    image.ActualHeight.Should().BeGreaterThan(0);
                    (image.ActualWidth / image.ActualHeight).Should().BeApproximately(ratio, 0.01);
                    var bounds = image.TransformToAncestor(content).TransformBounds(new Rect(image.RenderSize));
                    bounds.Left.Should().BeGreaterThanOrEqualTo(0);
                    bounds.Top.Should().BeGreaterThanOrEqualTo(0);
                    bounds.Right.Should().BeLessThanOrEqualTo(content.ActualWidth + 1);
                    bounds.Bottom.Should().BeLessThanOrEqualTo(content.ActualHeight + 1);
                }

                LayoutWindow(host, 800, 450);
                double smallerPaneHeight = content.ActualHeight;
                double smallerImageHeight = image.ActualHeight;
                LayoutWindow(host, 800, 600);
                content.ActualHeight.Should().BeGreaterThan(smallerPaneHeight);
                gallery.ActualHeight.Should().BeApproximately(content.ActualHeight, 1);
                image.ActualHeight.Should().BeGreaterThan(smallerImageHeight,
                    "the portrait image must use the additional space when the showcase pane grows");
            });
        }

        [TestCase("primary", 1)]
        [TestCase("alternate", 1)]
        [TestCase("failed-primary", 1)]
        [TestCase("duplicate", 1)]
        [TestCase("failed-both", 0)]
        [TestCase("missing", 0)]
        public void Showcase_OmitsMissingAndFailedImagesAndHidesSingleImageNavigation(string scenario, int count) {
            WithChangelogCache(_ => {
                using var valid = new LoopbackHttpServer(CreateScreenshot(120, 60, Colors.SteelBlue), "image/png");
                using var failure = new LoopbackHttpServer([], statusCode: 503);
                string? primary = scenario switch {
                    "primary" or "duplicate" => valid.Url + "/image.png",
                    "failed-primary" or "failed-both" => failure.Url + "/broken.png",
                    _ => null
                };
                string? alternate = scenario is "alternate" or "failed-primary" or "duplicate" ? valid.Url + "/image.png" : null;
                var manager = CreateManager();
                manager.AvailablePlugins = [new(new PluginManifest {
                    Identifier = "partial-gallery", Name = "Partial gallery fixture", Version = new PluginVersion("1.0.0.0"),
                    Descriptions = new PluginDescription { ScreenshotURL = primary, AltScreenshotURL = alternate }
                })];
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                valid.Requests.Should().BeEmpty("screenshots load when Showcase opens");
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                var content = (FrameworkElement)view.FindName("ShowcaseContent");
                var state = (PluginShowcaseVM)content.DataContext;
                PumpUntil(() => !state.IsLoading);
                Layout(view, 800, 600);
                var gallery = (ImageGallery)view.FindName("ScreenshotGallery");
                gallery.Items.Count.Should().Be(count);
                state.IsEmpty.Should().Be(count == 0);
                foreach (string name in new[] { "PART_PreviousButton", "PART_NextButton", "PART_Indicators" }) {
                    ((FrameworkElement)gallery.Template.FindName(name, gallery)).Visibility.Should().Be(Visibility.Collapsed);
                }

                if (count == 1) {
                    var image = (System.Windows.Controls.Image)gallery.Template.FindName("PART_Image", gallery);
                    image.ActualWidth.Should().BeApproximately(gallery.ActualWidth, 0.1, "a single landscape image uses the pane width");
                    gallery.SelectedIndex.Should().Be(0);
                    valid.Requests.Should().ContainSingle();
                }
            });
        }

        [Test]
        public void Showcase_IgnoresObsoleteDownloadsAndResetsSelectionForAnotherPlugin() {
            WithChangelogCache(root => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var slow = new LoopbackHttpServer(CreateScreenshot(100, 100, Colors.Red), "image/png", responseGate: gate.Task);
                using var first = new LoopbackHttpServer(CreateScreenshot(200, 100, Colors.Blue), "image/png");
                using var second = new LoopbackHttpServer(CreateScreenshot(100, 200, Colors.Green), "image/png");
                var manager = CreateManager();
                manager.AvailablePlugins = [
                    new(new PluginManifest {
                        Identifier = "old", Name = "A slow plugin", Version = new PluginVersion("1.0.0.0"),
                        Descriptions = new PluginDescription { ScreenshotURL = slow.Url + "/old.png" }
                    }),
                    new(new PluginManifest {
                        Identifier = "new", Name = "B current plugin", Version = new PluginVersion("1.0.0.0"),
                        Descriptions = new PluginDescription { ScreenshotURL = first.Url + "/first.png", AltScreenshotURL = second.Url + "/second.png" }
                    })
                ];
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                var gallery = (ImageGallery)view.FindName("ScreenshotGallery");
                var state = (PluginShowcaseVM)((FrameworkElement)view.FindName("ShowcaseContent")).DataContext;
                try {
                    PumpUntil(() => slow.RequestReceived.IsCompleted);
                    state.IsLoading.Should().BeTrue();
                    state.IsEmpty.Should().BeFalse();
                    manager.SelectedPlugin = manager.FilteredPlugins.Single(entry => entry.Identifier == "new");
                    PumpUntil(() => gallery.Items.Count == 2 && !state.IsLoading);
                    Layout(view);
                    gallery.SelectedIndex = 1;
                    gate.TrySetResult();
                    PumpUntil(() => Directory.GetFiles(Path.Combine(root, "Cache", "PluginIcons"), "*.png").Length == 3);
                    Layout(view);
                    gallery.Items.Count.Should().Be(2);
                    gallery.SelectedIndex.Should().Be(1);
                    gallery.Items.Cast<BitmapSource>().Select(image => image.PixelWidth).Should().Equal(200, 100);

                    ((TabItem)view.FindName("DescriptionTab")).IsSelected = true;
                    ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                    gallery.SelectedIndex.Should().Be(1, "switching detail tabs keeps the current screenshot");
                    manager.SelectedPlugin = manager.FilteredPlugins.Single(entry => entry.Identifier == "old");
                    PumpUntil(() => gallery.Items.Count == 1 && !state.IsLoading);
                    gallery.SelectedIndex.Should().Be(0);
                    manager.SelectedPlugin = manager.FilteredPlugins.Single(entry => entry.Identifier == "new");
                    PumpUntil(() => gallery.Items.Count == 2 && !state.IsLoading);
                    gallery.SelectedIndex.Should().Be(0, "a newly selected plugin starts with its first screenshot");
                    first.Requests.Should().ContainSingle("returning to a plugin reuses its cached artwork");
                    second.Requests.Should().ContainSingle();
                } finally {
                    gate.TrySetResult();
                }
            });
        }

        [Test]
        public void Showcase_PreservesManifestOrderWhenTheAlternateDownloadsFirst() {
            WithChangelogCache(root => {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var primary = new LoopbackHttpServer(CreateScreenshot(200, 100, Colors.Blue), "image/png", responseGate: gate.Task);
                using var alternate = new LoopbackHttpServer(CreateScreenshot(100, 200, Colors.Green), "image/png");
                var manager = CreateManager();
                manager.AvailablePlugins = [new(new PluginManifest {
                    Identifier = "order", Name = "Ordered gallery fixture", Version = new PluginVersion("1.0.0.0"),
                    Descriptions = new PluginDescription { ScreenshotURL = primary.Url + "/first.png", AltScreenshotURL = alternate.Url + "/second.png" }
                })];
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                var gallery = (ImageGallery)view.FindName("ScreenshotGallery");
                var state = (PluginShowcaseVM)((FrameworkElement)view.FindName("ShowcaseContent")).DataContext;
                try {
                    string cacheDirectory = Path.Combine(root, "Cache", "PluginIcons");
                    PumpUntil(() => Directory.Exists(cacheDirectory) && Directory.GetFiles(cacheDirectory, "*.png").Length == 1);
                    state.IsLoading.Should().BeTrue();
                    gate.TrySetResult();
                    PumpUntil(() => !state.IsLoading);
                    gallery.Items.Cast<BitmapSource>().Select(image => image.PixelWidth).Should().Equal(200, 100);
                    gallery.SelectedIndex.Should().Be(0);
                } finally {
                    gate.TrySetResult();
                }
            });
        }

        [Test]
        public void Showcase_UsesTheDisplayedVersionCacheAcrossSectionsAndRefresh() {
            WithChangelogCache(root => {
                using var server = new LoopbackHttpServer(responses: [
                    new(CreateScreenshot(100, 100, Colors.Red)), new(CreateScreenshot(200, 100, Colors.Blue)),
                    new(CreateScreenshot(300, 100, Colors.Green))
                ], "image/png");
                PluginManifest Manifest(string version) => new() {
                    Identifier = "versioned-gallery", Name = "Versioned gallery fixture", Version = new PluginVersion(version),
                    Descriptions = new PluginDescription { ScreenshotURL = server.Url + "/screenshot.png" }
                };

                var manager = CreateManager();
                var installed = Manifest("1.0.0.0");
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
                manager.AvailablePlugins = [new(Manifest("2.0.0.0"))];
                manager.SelectedTabIndex = 0;
                var view = new PluginsView { DataContext = manager };
                ShowChangelogView(view);
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                var gallery = (ImageGallery)view.FindName("ScreenshotGallery");
                var state = (PluginShowcaseVM)((FrameworkElement)view.FindName("ShowcaseContent")).DataContext;
                void CheckImage(int width) {
                    PumpUntil(() => !state.IsLoading && gallery.Items.Count == 1);
                    ((BitmapSource)gallery.SelectedItem).PixelWidth.Should().Be(width);
                }

                CheckImage(100);
                manager.SelectedTabIndex = 1;
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                CheckImage(200);
                manager.AvailablePlugins = [new(Manifest("3.0.0.0"))];
                CheckImage(300);
                manager.SelectedTabIndex = 0;
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                CheckImage(100);
                server.Requests.Should().HaveCount(3);
                Directory.GetFiles(Path.Combine(root, "Cache", "PluginIcons"), "*.png").Should().HaveCount(3);
            });
        }

        private static byte[] CreateScreenshot(int width, int height, Color color) {
            byte[] pixels = new byte[width * height * 4];
            for (int index = 0; index < pixels.Length; index += 4) {
                pixels[index] = color.B;
                pixels[index + 1] = color.G;
                pixels[index + 2] = color.R;
                pixels[index + 3] = color.A;
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4)));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        [Test]
        public void ArtworkCacheVersion_TracksTheDisplayedManifestAcrossSectionsAndRefresh() {
            var manager = CreateManager();
            var installed = new PluginManifest {
                Identifier = "artwork", Name = "Artwork fixture", Version = new PluginVersion("1.0.0.0")
            };
            var offered = new PluginManifest {
                Identifier = installed.Identifier, Name = installed.Name, Version = new PluginVersion("2.0.0.0")
            };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            manager.AvailablePlugins = [new(offered)];
            var view = new PluginsView { DataContext = manager };
            foreach (int section in new[] { 0, 1 }) {
                manager.SelectedTabIndex = section;
                ((TabItem)view.FindName("ShowcaseTab")).IsSelected = true;
                Layout(view);
                var images = FindVisuals<PluginImage>(view).ToArray();
                images.Should().HaveCount(2, "the list and header use the displayed version; Showcase loads its own cached images");
                foreach (var image in images) {
                    image.CacheVersion.Should().NotBeNull($"the version binding is {BindingOperations.GetBindingExpression(image, PluginImage.CacheVersionProperty)?.Status}");
                    image.CacheVersion.Should().Be(section == 0 ? "1.0.0.0" : "2.0.0.0");
                }
            }

            manager.AvailablePlugins = [new(new PluginManifest {
                Identifier = installed.Identifier, Name = installed.Name, Version = new PluginVersion("3.0.0.0")
            })];
            Layout(view);
            FindVisuals<PluginImage>(view).Should().OnlyContain(image => image.CacheVersion == "3.0.0.0");
            manager.SelectedTabIndex = 0;
            Layout(view);
            FindVisuals<PluginImage>(view).Should().OnlyContain(image => image.CacheVersion == "1.0.0.0");
        }

        [TestCase("1.0.0.0", "LblPluginManagerRollbackAvailable", "LblPluginManagerRollBackTo")]
        [TestCase("3.0.0.0", "LblPluginManagerUpgradeAvailable", "LblPluginManagerUpgradeTo")]
        public void VersionRows_MarkTheInstalledReleaseAndExplainTheSelectedChange(string offeredVersion, string statusKey, string actionKey) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == "Persian Faint");
            var manager = CreateManager();
            var installed = new PluginManifest { Identifier = "version-state", Name = "Version state fixture", Version = new PluginVersion("2.0.0.0") };
            var offered = new PluginManifest {
                Identifier = installed.Identifier, Name = installed.Name, Version = new PluginVersion(offeredVersion),
                MinimumApplicationVersion = new PluginVersion("3.2.0.0")
            };
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
            manager.AvailablePlugins = [new(offered)];
            var view = new PluginsView { DataContext = manager };
            ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
            Layout(view, 720, 560);
            var list = (ListBox)view.FindName("PluginVersions");
            list.Items.Count.Should().Be(2, "the installed release remains visible even when the repository no longer supplies it");
            var rows = list.Items.Cast<object>().Select(item => (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(item)).ToArray();
            var currentRow = rows.Single(row => FindVisual<TextBlock>(row)!.Text == "2.0.0.0");
            var currentMarker = FindVisuals<Border>(currentRow).Single(border => border.Child is TextBlock label
                && label.Text == NINA.Core.Locale.Loc.Instance["LblPluginManagerCurrentlyInstalled"]);
            currentMarker.Visibility.Should().Be(Visibility.Visible);
            FindVisuals<TextBlock>(currentRow).Single(label => label.Text.StartsWith(NINA.Core.Locale.Loc.Instance["LblPluginManagerRequiresNina"]))
                .Visibility.Should().Be(Visibility.Collapsed, "the local manifest does not supply a minimum NINA version");
            list.SelectedItem = currentRow.Content;
            Layout(view, 720, 560);
            var action = FindVisual<Button>(currentRow)!;
            action.Visibility.Should().Be(Visibility.Collapsed);
            action.IsEnabled.Should().BeFalse();
            ((TextBlock)action.Content).Text.Should().Be(NINA.Core.Locale.Loc.Instance["LblPluginManagerCurrentlyInstalled"]);

            var offeredRow = rows.Single(row => FindVisual<TextBlock>(row)!.Text == offeredVersion);
            FindVisuals<TextBlock>(offeredRow).Should().Contain(label => label.Text == NINA.Core.Locale.Loc.Instance[statusKey]);
            list.SelectedItem = offeredRow.Content;
            Layout(view, 720, 560);
            action = FindVisual<Button>(offeredRow)!;
            action.Visibility.Should().Be(Visibility.Visible);
            currentMarker.Visibility.Should().Be(Visibility.Visible);
            action.IsEnabled.Should().BeTrue();
            FindVisual<TextBlock>(action)!.Text.Should().Be(string.Format(NINA.Core.Locale.Loc.Instance[actionKey], offeredVersion));
        }

        [TestCase("Persian Faint")]
        [TestCase("Light")]
        [TestCase("Dark Nebula")]
        [TestCase("High Contrast")]
        public void Versions_FillThePaneAndShowTheActionBesideTheSelectedRelease(string schema) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            var manager = CreateManager();
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            var identifier = Guid.NewGuid().ToString();
            var releases = Enumerable.Range(1, 20).Select(major => new PluginManifest {
                Identifier = identifier, Name = "Version layout fixture", Author = "Test Observatory",
                Version = new PluginVersion($"{major}.0.0.0"), MinimumApplicationVersion = new PluginVersion(NINA.Core.Utility.CoreUtil.Version),
                Repository = "https://example.invalid", License = "MPL-2.0", LicenseURL = "https://example.invalid/license",
                Descriptions = new PluginDescription { ShortDescription = "Illustrative release list for layout verification" },
                Installer = new PluginInstallerDetails { URL = "https://example.invalid/plugin.dll", Checksum = new string('0', 64), ChecksumType = InstallerChecksum.SHA256, Type = InstallerType.DLL }
            }).ToArray();
            manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [releases[9]] = true };
            var json = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(releases,
                new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() } }));
            using var repository = new LoopbackHttpServer(new LoopbackHttpServer.Response[] { new(json), new(json) });
            var refresh = (Task)typeof(PluginsVM).GetMethod("FetchRepositories", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(manager, [new[] { repository.Url }])!;
            PumpUntil(() => refresh.IsCompleted);
            refresh.GetAwaiter().GetResult();
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                Width = 1000, Height = 900, Left = -20000, Top = -20000,
                MaxHeight = 900,
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            try {
                host.Show();
                LayoutWindow(host, 1000, 900);
                ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
                PumpUntil(() => !manager.IsLoadingVersions);
                LayoutWindow(host, 1000, 900);
                var list = (ListBox)view.FindName("PluginVersions");
                var pane = (Grid)view.FindName("VersionsContent");
                double remainingHeight = pane.ActualHeight - list.TranslatePoint(new Point(), pane).Y;
                list.ActualHeight.Should().BeApproximately(remainingHeight, 1,
                    "the release list must fill the available pane even when Windows limits the host size");
                LayoutWindow(host, 1000, 600);
                remainingHeight = pane.ActualHeight - list.TranslatePoint(new Point(), pane).Y;
                list.ActualHeight.Should().BeApproximately(remainingHeight, 1);
                LayoutWindow(host, 1000, 900);

                var installed = list.Items.Cast<PluginVersionVM>().Single(version => version.IsInstalled);
                manager.SelectedVersion.Version.ToString().Should().Be(installed.VersionText);
                var installedRow = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(installed);
                installedRow.Should().NotBeNull("opening Versions must bring the installed release into view");
                var older = list.Items.Cast<PluginVersionVM>().Single(version => version.VersionText == "9.0.0.0");
                list.SelectedItem = older;
                LayoutWindow(host, 1000, 900);
                var selectedRow = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(older);
                var action = FindVisual<Button>(selectedRow)!;
                action.Visibility.Should().Be(Visibility.Visible);
                action.Command.Should().BeSameAs(manager.InstallSelectedVersionCommand);
                FindVisual<TextBlock>(action)!.Text.Should().Be(string.Format(NINA.Core.Locale.Loc.Instance["LblPluginManagerRollBackTo"], "9.0.0.0"));
                action.IsEnabled.Should().BeTrue();
                var bounds = action.TransformToAncestor(selectedRow).TransformBounds(new Rect(action.RenderSize));
                bounds.Right.Should().BeGreaterThan(selectedRow.ActualWidth - 24);
                bounds.Right.Should().BeLessThanOrEqualTo(selectedRow.ActualWidth);
                bounds = action.TransformToAncestor(list).TransformBounds(new Rect(action.RenderSize));
                bounds.Top.Should().BeGreaterThanOrEqualTo(0);
                bounds.Bottom.Should().BeLessThanOrEqualTo(list.ActualHeight);
                FindVisual<Button>(installedRow)!.Visibility.Should().Be(Visibility.Collapsed);
                var currentBorder = (Border)installedRow.Template.FindName("RowBorder", installedRow);
                currentBorder.BorderThickness.Left.Should().BeGreaterThanOrEqualTo(2);
                currentBorder.BorderThickness.Top.Should().BeGreaterThan(0);
                AssertBrushColor(installedRow.Foreground, view.FindResource("PrimaryBrush"));
                AssertBrushColor(FindVisual<TextBlock>(selectedRow)!.Foreground, view.FindResource("PrimaryBrush"));
                AssertBrushColor(FindVisual<TextBlock>(action)!.Foreground, view.FindResource("ButtonForegroundBrush"));

                FindVisual<TextBlock>(action)!.Text = "Roll back to this release using a deliberately long translated label";
                LayoutWindow(host, 720, 560);
                bounds = action.TransformToAncestor(list).TransformBounds(new Rect(action.RenderSize));
                bounds.Right.Should().BeLessThanOrEqualTo(list.ActualWidth);
                bounds.Left.Should().BeGreaterThanOrEqualTo(0);
                list.ActualHeight.Should().BeGreaterThan(50);
                view.LayoutTransform = new ScaleTransform(2, 2);
                LayoutWindow(host, 1440, 1120);
                list.ScrollIntoView(older);
                LayoutWindow(host, 1440, 1120);
                selectedRow = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(older);
                action = FindVisual<Button>(selectedRow)!;
                FindVisual<TextBlock>(action)!.Text = "Roll back to this release using a deliberately long translated label";
                LayoutWindow(host, 1440, 1120);
                bounds = action.TransformToAncestor(list).TransformBounds(new Rect(action.RenderSize));
                bounds.Left.Should().BeGreaterThanOrEqualTo(0);
                bounds.Right.Should().BeLessThanOrEqualTo(list.ActualWidth);

                var viewport = FindVisual<ScrollViewer>(list)!;
                viewport.ScrollToTop();
                LayoutWindow(host, 1440, 1120);
                RaiseWheel(viewport, 120);
                LayoutWindow(host, 1440, 1120);
                viewport.VerticalOffset.Should().Be(0);
                RaiseWheel(viewport, -120);
                LayoutWindow(host, 1440, 1120);
                viewport.VerticalOffset.Should().BeGreaterThan(0, "the version list must retain its own wheel scrolling");
                viewport.ScrollToBottom();
                LayoutWindow(host, 1440, 1120);
                double bottom = viewport.VerticalOffset;
                RaiseWheel(viewport, -120);
                LayoutWindow(host, 1440, 1120);
                viewport.VerticalOffset.Should().Be(bottom);
                RaiseWheel(viewport, 120);
                LayoutWindow(host, 1440, 1120);
                viewport.VerticalOffset.Should().BeLessThan(bottom);
            } finally {
                host.Close();
            }
        }

        [TestCase("Persian Faint")]
        [TestCase("Light")]
        [TestCase("Dark Nebula")]
        [TestCase("High Contrast")]
        public void Versions_ShowsThePluginVersionSeparatelyFromTheRequiredNinaVersion(string schema) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            var manager = CreateManager();
            manager.AvailablePlugins = [new(new PluginManifest {
                Identifier = "version-label", Name = "Version label fixture", Version = new PluginVersion("2.5.12.42"),
                MinimumApplicationVersion = new PluginVersion("3.2.0.0")
            })];
            var view = new PluginsView { DataContext = manager };
            ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
            Layout(view, 720, 560);

            var versions = (ListBox)view.FindName("PluginVersions");
            var row = (ListBoxItem)versions.ItemContainerGenerator.ContainerFromIndex(0);
            var label = FindVisual<TextBlock>(row)!;
            label.Text.Should().Be("2.5.12.42");
            var requirement = FindVisuals<TextBlock>(row).Single(block => block.Text.StartsWith(NINA.Core.Locale.Loc.Instance["LblPluginManagerRequiresNina"]));
            requirement.Text.Should().EndWith("3.2.0.0");
            label.ActualHeight.Should().BeGreaterThan(0);
            var border = (Border)row.Template.FindName("RowBorder", row);
            AssertBrushColor(label.Foreground, view.FindResource("PrimaryBrush"));
            AssertBrushColor(requirement.Foreground, view.FindResource("PrimaryBrush"));
            var content = (FrameworkElement)view.FindName("VersionsContent");
            var bounds = label.TransformToAncestor(content).TransformBounds(new Rect(label.RenderSize));
            bounds.Top.Should().BeGreaterThanOrEqualTo(0);
            bounds.Bottom.Should().BeLessThanOrEqualTo(content.ActualHeight);
        }

        [TestCase("Persian Faint", 720, 560)]
        [TestCase("Light", 720, 560)]
        [TestCase("Dark Nebula", 1000, 700)]
        [TestCase("High Contrast", 720, 560)]
        public void ThemeAndNarrowLayout_KeepActionsInsideThePluginArea(string schema, int width, int height) {
            var profile = ((IProfileService)Application.Current.Resources["ProfileService"]).ActiveProfile;
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            var manager = CreateManager();
            manager.AvailablePlugins = new List<ExtendedPluginManifest> {
                new(new PluginManifest {
                    Identifier = "preview",
                    Name = "A plugin with a deliberately long translated title",
                    Author = "Plugin author",
                    Version = new PluginVersion("2.1.0.0"),
                    MinimumApplicationVersion = new PluginVersion("3.2.0.0"),
                    Tags = new[] { "Imaging", "Sequencer" },
                    License = "MPL-2.0",
                    LicenseURL = "https://example.invalid/license",
                    Descriptions = new PluginDescription {
                        ShortDescription = "A long plugin summary that wraps within the available detail pane.",
                        LongDescription = "A **formatted** description with [documentation](https://example.invalid/docs)."
                    }
                })
            };
            var errors = new BindingErrorListener();
            PresentationTraceSources.DataBindingSource.Listeners.Add(errors);
            try {
                var view = new PluginsView { DataContext = manager };
                Layout(view, width, height);
                ((SolidColorBrush)view.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.BackgroundColor);
                var search = (HintTextBox)view.FindName("PluginSearch");
                var hint = (TextBlock)search.Template.FindName("PART_HintText", search);
                hint.Visibility.Should().Be(Visibility.Visible);
                search.Text = "NO MATCH";
                Layout(view, width, height);
                manager.HasResults.Should().BeFalse();
                hint.Visibility.Should().Be(Visibility.Collapsed);
                manager.ClearFiltersCommand.Execute(null);
                Layout(view, width, height);
                manager.HasResults.Should().BeTrue();
                hint.Visibility.Should().Be(Visibility.Visible);

                foreach (string name in new[] { "RefreshPluginsButton", "InstallPluginButton", "PluginSearch", "PluginDetails" }) {
                    var element = (FrameworkElement)view.FindName(name);
                    var bounds = element.TransformToAncestor(view).TransformBounds(new Rect(element.RenderSize));
                    bounds.Left.Should().BeGreaterThanOrEqualTo(0, name);
                    bounds.Right.Should().BeLessThanOrEqualTo(width + 0.1, name);
                    bounds.Bottom.Should().BeLessThanOrEqualTo(height + 0.1, name);
                }

                var install = (Button)view.FindName("InstallPluginButton");
                var details = (FrameworkElement)view.FindName("PluginDetails");
                var installBounds = install.TransformToAncestor(details).TransformBounds(new Rect(install.RenderSize));
                installBounds.Right.Should().BeApproximately(details.ActualWidth, 0.1, "detail actions align with the right edge");
                installBounds.Top.Should().BeApproximately(0, 0.1, "detail actions occupy the top-right corner");
                var label = FindVisual<TextBlock>(install)!;
                label.Style.Should().BeSameAs(view.FindResource("StandardTextBlock"));
                AssertBrushColor(label.Foreground, view.FindResource("ButtonForegroundBrush"));
                var mouseOverKey = (DependencyPropertyKey)typeof(UIElement)
                    .GetField("IsMouseOverPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                var refresh = (Button)view.FindName("RefreshPluginsButton");
                AssertBrushColor(FindVisual<System.Windows.Shapes.Path>(refresh)!.Fill, refresh.Foreground);
                foreach (var button in new[] { install, (Button)view.FindName("RefreshPluginsButton") }) {
                    button.SetValue(mouseOverKey, true);
                    try {
                        Layout(view, width, height);
                        ((SolidColorBrush)button.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.ButtonBackgroundSelectedColor);
                        var foreground = button == install
                            ? FindVisual<TextBlock>(button)!.Foreground
                            : FindVisual<System.Windows.Shapes.Path>(button)!.Fill;
                        AssertBrushColor(foreground, view.FindResource("ButtonForegroundBrush"));
                    } finally {
                        button.SetValue(mouseOverKey, false);
                    }
                }

                var list = (ListBox)view.FindName("PluginList");
                var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
                var border = (Border)row.Template.FindName("RowBorder", row);
                var rowLabel = FindVisual<TextBlock>(row)!;
                AssertBrushColor(rowLabel.Foreground, view.FindResource("PrimaryBrush"));
                var navigation = (TabControl)view.FindName("DetailsNavigation");
                var tabLabel = FindVisual<TextBlock>((TabItem)navigation.SelectedItem)!;
                tabLabel.Style.Should().BeSameAs(view.FindResource("TabItemTextBox"));
                AssertBrushColor(tabLabel.Foreground, view.FindResource("ButtonForegroundBrush"));
                var tab = (TabItem)navigation.SelectedItem;
                var panel = (Grid)tab.Template.FindName("PART_Panel", tab);
                tab.SetValue(mouseOverKey, true);
                panel.SetValue(mouseOverKey, true);
                try {
                    Layout(view, width, height);
                    ((SolidColorBrush)panel.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.ButtonBackgroundSelectedColor);
                    AssertBrushColor(tabLabel.Foreground, view.FindResource("ButtonForegroundBrush"));
                } finally {
                    panel.SetValue(mouseOverKey, false);
                    tab.SetValue(mouseOverKey, false);
                }

                Layout(view, width, height);

                var sidebar = (TabControl)view.FindName("PluginNavigation");
                foreach (var sidebarTab in sidebar.Items.Cast<TabItem>()) {
                    var sidebarLabel = FindVisual<TextBlock>(sidebarTab)!;
                    foreach (bool hovered in new[] { false, true }) {
                        sidebarTab.SetValue(mouseOverKey, hovered);
                        Layout(view, width, height);
                        var foreground = view.FindResource(hovered || sidebarTab.IsSelected ? "ButtonForegroundBrush" : "ButtonForegroundDisabledBrush");
                        AssertBrushColor(sidebarLabel.Foreground, foreground);
                        foreach (var icon in FindVisuals<System.Windows.Shapes.Path>(sidebarTab)) {
                            AssertBrushColor(icon.Fill, foreground);
                        }
                    }

                    sidebarTab.SetValue(mouseOverKey, false);
                }

                errors.Messages.Should().BeEmpty();
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(view);
                var searchBounds = search.TransformToAncestor(view).TransformBounds(new Rect(search.RenderSize));
                var backgroundPixel = new byte[4];
                bitmap.CopyPixels(new Int32Rect((int)searchBounds.Right - 10, (int)(searchBounds.Top + searchBounds.Height / 2), 1, 1), backgroundPixel, 4, 0);
                var backgroundColor = Color.FromRgb(backgroundPixel[2], backgroundPixel[1], backgroundPixel[0]);
                var hintBrush = (SolidColorBrush)hint.Foreground;
                double hintOpacity = hint.Opacity * hintBrush.Opacity * hintBrush.Color.A / byte.MaxValue;
                var hintColor = Color.FromRgb(
                    (byte)Math.Round(backgroundColor.R + (hintBrush.Color.R - backgroundColor.R) * hintOpacity),
                    (byte)Math.Round(backgroundColor.G + (hintBrush.Color.G - backgroundColor.G) * hintOpacity),
                    (byte)Math.Round(backgroundColor.B + (hintBrush.Color.B - backgroundColor.B) * hintOpacity));
                Contrast(new SolidColorBrush(hintColor), new SolidColorBrush(backgroundColor))
                    .Should().BeGreaterThanOrEqualTo(4.5, "the search hint must remain readable in " + schema);

                var offered = manager.AvailablePlugins.Single();
                var installed = new PluginManifest {
                    Identifier = offered.Identifier, Name = offered.Name, Author = offered.Author,
                    Version = new PluginVersion("1.0.0.0"), Descriptions = offered.Descriptions
                };
                manager.InstalledPlugins = new Dictionary<IPluginManifest, bool> { [installed] = true };
                manager.SelectedTabIndex = 0;
                Layout(view, width, height);
                var updateAll = (Button)view.FindName("UpdateAllPluginsButton");
                var tagFilter = (FrameworkElement)view.FindName("PluginTagFilter");
                var updateAllBounds = updateAll.TransformToAncestor(view).TransformBounds(new Rect(updateAll.RenderSize));
                var tagBounds = tagFilter.TransformToAncestor(view).TransformBounds(new Rect(tagFilter.RenderSize));
                var listBounds = list.TransformToAncestor(view).TransformBounds(new Rect(list.RenderSize));
                updateAll.Visibility.Should().Be(Visibility.Visible);
                ((FrameworkElement)updateAll.Parent).Visibility.Should().Be(Visibility.Visible);
                updateAllBounds.Top.Should().BeApproximately(tagBounds.Bottom + 8, 0.1);
                updateAllBounds.Right.Should().BeApproximately(tagBounds.Right, 0.1);
                var update = (Button)view.FindName("UpdatePluginButton");
                var uninstall = (Button)view.FindName("UninstallPluginButton");
                update.Visibility.Should().Be(Visibility.Visible);
                uninstall.Visibility.Should().Be(Visibility.Visible);
                update.Command.Should().BeSameAs(manager.UpdatePluginCommand);
                uninstall.Command.Should().BeSameAs(manager.UninstallPluginCommand);
                update.Style.Should().BeSameAs(view.FindResource("StandardButton"));
                uninstall.Style.Should().BeSameAs(view.FindResource("StandardButton"));
                ((TextBlock)uninstall.Content).Style.Should().BeSameAs(view.FindResource("StandardTextBlock"));
                foreach (bool hovered in new[] { false, true }) {
                    uninstall.SetValue(mouseOverKey, hovered);
                    Layout(view, width, height);
                    ((SolidColorBrush)uninstall.Background).Color.Should().Be(hovered
                        ? profile.ColorSchemaSettings.ColorSchema.ButtonBackgroundSelectedColor
                        : profile.ColorSchemaSettings.ColorSchema.ButtonBackgroundColor);
                    AssertBrushColor(((TextBlock)uninstall.Content).Foreground, view.FindResource("ButtonForegroundBrush"));
                }

                uninstall.SetValue(mouseOverKey, false);
                Layout(view, width, height);
                var updateBounds = update.TransformToAncestor(details).TransformBounds(new Rect(update.RenderSize));
                var uninstallBounds = uninstall.TransformToAncestor(details).TransformBounds(new Rect(uninstall.RenderSize));
                uninstallBounds.Right.Should().BeApproximately(details.ActualWidth, 0.1);
                updateBounds.Right.Should().BeLessThan(uninstallBounds.Left);
                updateBounds.Top.Should().BeApproximately(uninstallBounds.Top, 0.1);
                updateBounds.Top.Should().BeApproximately(0, 0.1);
                ((TextBlock)uninstall.Content).ActualHeight.Should().BeLessThan(25, "the normal action caption fits on one line");
                var heading = FindVisuals<TextBlock>(details).Single(text => text.Text == installed.Name);
                heading.ToolTip.Should().Be(installed.Name);
                heading.ActualWidth.Should().BeGreaterThan(0);
                var headingBounds = heading.TransformToAncestor(details).TransformBounds(new Rect(heading.RenderSize));
                headingBounds.Right.Should().BeLessThanOrEqualTo(updateBounds.Left);

                manager.SelectedTabIndex = 1;
                Layout(view, width, height);
                uninstall.Visibility.Should().Be(Visibility.Collapsed);
                ((FrameworkElement)updateAll.Parent).Visibility.Should().Be(Visibility.Collapsed);
                updateBounds = update.TransformToAncestor(details).TransformBounds(new Rect(update.RenderSize));
                updateBounds.Right.Should().BeApproximately(details.ActualWidth, 0.1);
                updateBounds.Top.Should().BeApproximately(0, 0.1);

                manager.SelectedTabIndex = 0;
                ((TextBlock)update.Content).Text = "Update this plugin using a deliberately long translated label";
                ((TextBlock)uninstall.Content).Text = "Uninstall this plugin using a deliberately long translated label";
                Layout(view, width, height);
                foreach (var button in new[] { update, uninstall }) {
                    var bounds = button.TransformToAncestor(details).TransformBounds(new Rect(button.RenderSize));
                    bounds.Left.Should().BeGreaterThanOrEqualTo(0);
                    bounds.Right.Should().BeLessThanOrEqualTo(details.ActualWidth + 0.1);
                    bounds.Bottom.Should().BeLessThanOrEqualTo(details.ActualHeight);
                    bounds.Top.Should().BeApproximately(0, 0.1);
                    ((TextBlock)button.Content).ActualHeight.Should().BeGreaterThan(20);
                }

                errors.Messages.Should().BeEmpty();
            } finally {
                PresentationTraceSources.DataBindingSource.Listeners.Remove(errors);
            }
        }

        [Test]
        public void Images_UseFallbackForMissingUrlsAndOmitMissingScreenshots() {
            var image = new PluginImage { ImageUrl = " " };
            image.Source.Should().NotBeNull();
            image.HasImage.Should().BeTrue();
            image.UseFallback = false;
            image.Source.Should().BeNull();
            image.HasImage.Should().BeFalse();
        }

        [Test]
        public void KeyboardAndFilterControls_UseTheBoundManager() {
            var manager = CreateManager();
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            manager.AvailablePlugins = new List<ExtendedPluginManifest> {
                new(new PluginManifest { Identifier = "keyboard", Name = "Keyboard plugin", Tags = new[] { "Imaging" }, Version = new PluginVersion("1.0.0.0") }),
                new(new PluginManifest { Identifier = "other", Name = "Other plugin", Tags = new[] { "Imaging" }, Version = new PluginVersion("1.0.0.0") })
            };
            var view = new PluginsView { DataContext = manager };
            var host = new Window {
                Width = 1000, Height = 700, Left = -20000, Top = -20000,
                ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            try {
                host.Show();
                Layout(view);
                var search = (HintTextBox)view.FindName("PluginSearch");
                var editor = (TextBox)search.Template.FindName("PART_TextBox", search);
                var hint = (TextBlock)search.Template.FindName("PART_HintText", search);
                hint.IsVisible.Should().BeTrue();
                editor.Text = "No matches";
                Layout(view);
                manager.SearchText.Should().Be(editor.Text);
                hint.IsVisible.Should().BeFalse();
                ApplicationCommands.Find.Execute(null, view);
                editor.IsKeyboardFocusWithin.Should().BeTrue();
                editor.SelectionLength.Should().Be(editor.Text.Length);
                AssertBrushColor(editor.BorderBrush, view.FindResource("ButtonBackgroundBrush"));
                editor.GetValue(System.Windows.Automation.AutomationProperties.NameProperty).Should().Be(search.HintText);
                var refresh = (Button)view.FindName("RefreshPluginsButton");
                var tags = (CheckComboBox)view.FindName("PluginTagFilter");
                editor.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)).Should().BeTrue();
                refresh.IsKeyboardFocusWithin.Should().BeTrue();
                AssertBrushColor(editor.BorderBrush, view.FindResource("BorderBrush"));
                refresh.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)).Should().BeTrue();
                tags.IsKeyboardFocusWithin.Should().BeTrue();
                tags.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)).Should().BeTrue();
                refresh.IsKeyboardFocusWithin.Should().BeTrue();
                refresh.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)).Should().BeTrue();
                editor.IsKeyboardFocusWithin.Should().BeTrue();
                var key = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(editor), 0, Key.Escape) {
                    RoutedEvent = Keyboard.KeyDownEvent
                };
                editor.RaiseEvent(key);
                Layout(view);
                manager.SearchText.Should().BeEmpty();
                editor.Text.Should().BeEmpty();
                hint.IsVisible.Should().BeTrue();
                key.Handled.Should().BeTrue();

                tags.IsDropDownOpen = true;
                tags.SelectedItems.Add(tags.Items.Cast<PluginTag>().Single(tag => tag.Name == "Imaging"));
                tags.IsDropDownOpen = false;
                manager.SelectedTags.Select(tag => tag.Name).Should().Equal("Imaging");
                var navigation = (TabControl)view.FindName("PluginNavigation");
                navigation.SelectedIndex = 0;
                Layout(view);
                manager.IsInstalledTab.Should().BeTrue();
                manager.ShowAvailableCommand.Execute(null);
                Layout(view);
                manager.SelectedTabIndex.Should().Be(1);
                search.Text = "No matches";
                Layout(view);
                var clear = (Button)view.FindName("ClearPluginFiltersButton");
                ((IInvokeProvider)new ButtonAutomationPeer(clear).GetPattern(PatternInterface.Invoke)).Invoke();
                Layout(view);
                manager.SearchText.Should().BeEmpty();
                manager.HasResults.Should().BeTrue();
                ((TabItem)view.FindName("VersionsTab")).IsSelected = true;
                Layout(view);
                var versions = (ListBox)view.FindName("PluginVersions");
                var row = (ListBoxItem)versions.ItemContainerGenerator.ContainerFromItem(versions.SelectedItem);
                var versionAction = FindVisual<Button>(row)!;
                Keyboard.Focus(row).Should().BeSameAs(row);
                row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)).Should().BeTrue();
                versionAction.IsKeyboardFocusWithin.Should().BeTrue("the selected version's inline action must be reachable with Tab");
            } finally {
                host.Close();
            }
        }

        [Test]
        public void ResizeAndDoubleScale_PreserveTheDetailMinimum() {
            var manager = CreateManager();
            var view = new PluginsView { DataContext = manager };
            Layout(view);
            var browser = (Grid)view.FindName("BrowserGrid");
            browser.ColumnDefinitions[0].Width = new GridLength(420);
            Layout(view, 720, 560);
            browser.ColumnDefinitions[0].ActualWidth.Should().BeInRange(240, 420);
            browser.ColumnDefinitions[2].ActualWidth.Should().BeGreaterThanOrEqualTo(320);
            browser.ActualWidth.Should().BeLessThanOrEqualTo(720);
            view.LayoutTransform = new ScaleTransform(2, 2);
            Layout(view, 1440, 1120);
            browser.ColumnDefinitions[2].ActualWidth.Should().BeGreaterThanOrEqualTo(320);
        }

        private void WithChangelogCache(Action<string> verify) {
            string originalPath = CoreUtil.APPLICATIONTEMPPATH;
            var originalContext = SynchronizationContext.Current;
            string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PluginChangelogView", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            CoreUtil.APPLICATIONTEMPPATH = root;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            try {
                verify(root);
            } finally {
                foreach (var host in changelogHosts) {
                    host.Close();
                }

                changelogHosts.Clear();
                SynchronizationContext.SetSynchronizationContext(originalContext);
                CoreUtil.APPLICATIONTEMPPATH = originalPath;
                Directory.Delete(root, true);
            }
        }

        private Window ShowChangelogView(PluginsView view) {
            var manager = (PluginsVM)view.DataContext;
            typeof(PluginsVM).GetField("firstTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, false);
            var host = new Window {
                Width = 1000, Height = 700, Left = -20000, Top = -20000,
                ShowInTaskbar = false, ShowActivated = false, Content = view
            };
            changelogHosts.Add(host);
            host.Show();
            Layout(view);
            return host;
        }

        private static PluginsVM CreateManager() {
            var loader = new Mock<IPluginLoader>();
            loader.SetupGet(value => value.Plugins).Returns(new Dictionary<IPluginManifest, bool>());
            loader.Setup(value => value.Load()).Returns(Task.CompletedTask);
            var manager = new PluginsVM(loader.Object, Mock.Of<IProfileService>());
            PumpUntil(() => manager.Initialized);
            manager.ShowAvailableCommand.Execute(null);
            return manager;
        }

        private static void Layout(FrameworkElement view, int width = 1000, int height = 700) {
            view.Measure(new Size(width, height));
            view.Arrange(new Rect(0, 0, width, height));
            view.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            view.UpdateLayout();
        }

        private static void LayoutWindow(Window host, int width, int height) {
            host.Width = width;
            host.Height = height;
            host.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            host.UpdateLayout();
        }

        private static void RaiseWheel(UIElement target, int delta) {
            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta) {
                RoutedEvent = Mouse.PreviewMouseWheelEvent
            };
            target.RaiseEvent(args);
            if (!args.Handled) {
                args.RoutedEvent = Mouse.MouseWheelEvent;
                target.RaiseEvent(args);
            }
        }

        private static void PumpUntil(Func<bool> complete) {
            var timeout = Stopwatch.StartNew();
            while (!complete()) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(10)) {
                    throw new TimeoutException("Plugin view operation did not complete.");
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

        private static IEnumerable<T> FindVisuals<T>(DependencyObject root) where T : DependencyObject {
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match) {
                    yield return match;
                }

                foreach (var descendant in FindVisuals<T>(child)) {
                    yield return descendant;
                }
            }
        }

        private sealed class ReadOnlyLoadedManifest : IPluginManifest {
            public string Identifier => "read-only-loaded";

            public string Name => "Read-only loaded plugin";

            public string License => "MPL-2.0";

            public string LicenseURL => "https://example.invalid/license";

            public string Author => "Test Observatory";

            public string Homepage => "https://example.invalid";

            public string Repository => "https://example.invalid/source";

            public string ChangelogURL => "https://example.invalid/changelog";

            public string[] Tags { get; } = [];

            public IPluginVersion Version { get; } = new PluginVersion("1.0.0.0");

            public IPluginVersion MinimumApplicationVersion { get; } = new PluginVersion("3.0.0.0");

            public IPluginInstallerDetails Installer { get; } = new ReadOnlyInstaller();

            public IPluginDescription Descriptions { get; } = new PluginDescription { ShortDescription = "Read-only metadata fixture" };

            public Task Initialize() => Task.CompletedTask;

            public Task Teardown() => Task.CompletedTask;
        }

        private sealed class ReadOnlyInstaller : IPluginInstallerDetails {
            public string URL => "https://example.invalid/plugin.dll";

            public InstallerType Type => InstallerType.DLL;

            public string Checksum => new string('0', 64);

            public InstallerChecksum ChecksumType => InstallerChecksum.SHA256;
        }

        private sealed class SettingsManifest : PluginManifest {
            public string OptionsValue { get; set; } = string.Empty;
        }

        private static void AssertBrushColor(Brush actual, object expected) {
            actual.Should().BeOfType<SolidColorBrush>().Subject.Color.Should().Be(((SolidColorBrush)expected).Color);
        }

        private static double Contrast(SolidColorBrush foreground, SolidColorBrush background) {
            static double Luminance(Color color) {
                static double Linear(byte channel) {
                    double value = channel / 255.0;
                    return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
                }

                return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
            }

            var backgroundColor = background.Color;
            if (backgroundColor.A < byte.MaxValue) {
                var page = ((SolidColorBrush)Application.Current.Resources["BackgroundBrush"]).Color;
                double opacity = backgroundColor.A / 255.0;
                backgroundColor = Color.FromRgb(
                    (byte)Math.Round(backgroundColor.R * opacity + page.R * (1 - opacity)),
                    (byte)Math.Round(backgroundColor.G * opacity + page.G * (1 - opacity)),
                    (byte)Math.Round(backgroundColor.B * opacity + page.B * (1 - opacity)));
            }

            double first = Luminance(foreground.Color);
            double second = Luminance(backgroundColor);
            return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
        }

        private sealed class BindingErrorListener : TraceListener {
            public List<string> Messages { get; } = new();

            public override void Write(string? message) {
                if (!string.IsNullOrEmpty(message)) {
                    Messages.Add(message);
                }
            }

            public override void WriteLine(string? message) => Write(message);
        }

    }
}
