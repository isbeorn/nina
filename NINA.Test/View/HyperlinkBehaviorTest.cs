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
using MdXaml;
using NINA.View;
using NINA.View.About;
using NINA.View.Plugins;
using NINA.WPF.Base.Behaviors;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class HyperlinkBehaviorTest {

        [Test]
        public void CopyAction_PrefersNavigateUriAndCopiesItsOriginalString() {
            Hyperlink hyperlink = new Hyperlink {
                NavigateUri = new Uri("https://nighttime-imaging.eu/release%20notes?channel=nightly"),
                CommandParameter = "https://fallback.example/"
            };
            HyperlinkBehavior.SetIsEnabled(hyperlink, true);

            WithRestoredClipboard(() => {
                MenuItem copyMenuItem = GetCopyMenuItem(hyperlink);
                copyMenuItem.Header.Should().Be("Copy URL");
                copyMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

                Clipboard.GetText().Should().Be("https://nighttime-imaging.eu/release%20notes?channel=nightly");
            });
        }

        [TestCase("https://nighttime-imaging.eu/docs/")]
        [TestCase("mailto:isbeorn86+NINA@googlemail.com")]
        public void CopyAction_UsesAbsoluteCommandParameterWhenNavigateUriIsMissing(string target) {
            Hyperlink hyperlink = new Hyperlink { CommandParameter = target };
            HyperlinkBehavior.SetIsEnabled(hyperlink, true);

            WithRestoredClipboard(() => {
                GetCopyMenuItem(hyperlink).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

                Clipboard.GetText().Should().Be(target);
            });
        }

        [Test]
        public void ContextMenuOpening_SuppressesOwnedMenuWithoutUrlAndPreservesExistingMenu() {
            Hyperlink ownedMenuHyperlink = new Hyperlink { CommandParameter = "not a URL" };
            HyperlinkBehavior.SetIsEnabled(ownedMenuHyperlink, true);

            ContextMenuEventArgs ownedMenuArgs = RaiseContextMenuOpening(ownedMenuHyperlink);

            ownedMenuArgs.Handled.Should().BeTrue();
            GetCopyMenuItem(ownedMenuHyperlink).Visibility.Should().Be(Visibility.Collapsed);

            MenuItem existingItem = new MenuItem { Header = "Existing" };
            ContextMenu existingMenu = new ContextMenu { Items = { existingItem } };
            Hyperlink existingMenuHyperlink = new Hyperlink {
                CommandParameter = "not a URL",
                ContextMenu = existingMenu
            };
            HyperlinkBehavior.SetIsEnabled(existingMenuHyperlink, true);

            ContextMenuEventArgs existingMenuArgs = RaiseContextMenuOpening(existingMenuHyperlink);

            existingMenuArgs.Handled.Should().BeFalse();
            existingMenuHyperlink.ContextMenu.Should().BeSameAs(existingMenu);
            existingMenu.Items.Cast<object>().Should().Contain(existingItem);
            GetCopyMenuItem(existingMenuHyperlink).Visibility.Should().Be(Visibility.Collapsed);
        }

        [Test]
        public void EnableDisable_RestoresContextMenuAndDoesNotDuplicateOwnedItems() {
            MenuItem existingItem = new MenuItem { Header = "Existing" };
            ContextMenu existingMenu = new ContextMenu { Items = { existingItem } };
            Hyperlink hyperlink = new Hyperlink {
                NavigateUri = new Uri("https://nighttime-imaging.eu/"),
                ContextMenu = existingMenu
            };

            HyperlinkBehavior.SetIsEnabled(hyperlink, true);
            existingMenu.Items.Cast<object>().Should().HaveCount(3);

            HyperlinkBehavior.SetIsEnabled(hyperlink, false);
            hyperlink.ContextMenu.Should().BeSameAs(existingMenu);
            existingMenu.Items.Cast<object>().Should().Equal(existingItem);

            HyperlinkBehavior.SetIsEnabled(hyperlink, true);
            existingMenu.Items.Cast<object>().Should().HaveCount(3);

            HyperlinkBehavior.SetIsEnabled(hyperlink, false);
            existingMenu.Items.Cast<object>().Should().Equal(existingItem);

            Hyperlink ownedMenuHyperlink = new Hyperlink { NavigateUri = new Uri("https://nighttime-imaging.eu/") };
            HyperlinkBehavior.SetIsEnabled(ownedMenuHyperlink, true);
            ownedMenuHyperlink.ContextMenu.Should().NotBeNull();

            HyperlinkBehavior.SetIsEnabled(ownedMenuHyperlink, false);
            ownedMenuHyperlink.ContextMenu.Should().BeNull();
        }

        [Test]
        public void NavigationRequest_UsesConfiguredCommandAndFollowsEnableDisableSymmetry() {
            Uri target = new Uri("https://nighttime-imaging.eu/docs/");
            RecordingCommand command = new RecordingCommand();
            Hyperlink hyperlink = new Hyperlink { NavigateUri = target };
            HyperlinkBehavior.SetNavigationCommand(hyperlink, command);

            HyperlinkBehavior.SetIsEnabled(hyperlink, true);
            RequestNavigateEventArgs firstRequest = RaiseRequestNavigate(hyperlink, target);

            firstRequest.Handled.Should().BeTrue();
            command.Executions.Should().Equal(target);

            HyperlinkBehavior.SetIsEnabled(hyperlink, false);
            RequestNavigateEventArgs disabledRequest = RaiseRequestNavigate(hyperlink, target);

            disabledRequest.Handled.Should().BeFalse();
            command.Executions.Should().Equal(target);

            HyperlinkBehavior.SetIsEnabled(hyperlink, true);
            RequestNavigateEventArgs secondRequest = RaiseRequestNavigate(hyperlink, target);

            secondRequest.Handled.Should().BeTrue();
            command.Executions.Should().Equal(target, target);
        }

        [Test]
        public void NavigationRequest_RemainsUnhandledWhenConfiguredCommandCannotExecute() {
            Uri target = new Uri("https://nighttime-imaging.eu/docs/");
            RecordingCommand command = new RecordingCommand(canExecute: false);
            Hyperlink hyperlink = new Hyperlink { NavigateUri = target };
            HyperlinkBehavior.SetNavigationCommand(hyperlink, command);
            HyperlinkBehavior.SetIsEnabled(hyperlink, true);

            RequestNavigateEventArgs request = RaiseRequestNavigate(hyperlink, target);

            request.Handled.Should().BeFalse();
            command.Executions.Should().BeEmpty();
        }

        [Test]
        public void DefaultNavigationCommand_AcceptsOnlyAbsoluteUris() {
            ICommand command = HyperlinkBehavior.GetNavigationCommand(new Hyperlink());

            command.CanExecute(new Uri("https://nighttime-imaging.eu/")).Should().BeTrue();
            command.CanExecute(new Uri("mailto:isbeorn86+NINA@googlemail.com")).Should().BeTrue();
            command.CanExecute(new Uri("docs/index.html", UriKind.Relative)).Should().BeFalse();
            command.CanExecute(null).Should().BeFalse();
        }

        [Test]
        public void CompiledStyles_EnableStandardAndMarkdownHyperlinks() {
            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            string[] resourceSources = [
                "/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/SVGDictionary.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Converters.xaml",
                "/NINA;component/Resources/StaticResources/DataTemplates.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Button.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Path.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBlock.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TabControl.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/CheckBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/DataGrid.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ListView.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/GroupBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/RepeatButton.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ToggleButton.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Slider.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Expander.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ScrollViewer.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ComboBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/GridSplitter.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ProgressBar.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Tooltip.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/CancellableButton.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/DatePicker.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/StepperControl.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ContextMenu.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Hyperlink.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/SplitButton.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ColorPicker.xaml",
                "/NINA;component/Resources/Styles/Window.xaml",
                "/NINA;component/Resources/Styles/AvalonDock.xaml",
                "/NINA;component/Resources/Styles/Oxyplot.xaml",
                "/NINA;component/Resources/Styles/Markdown.xaml"
            ];
            ResourceDictionary hyperlinkResources = null;
            ResourceDictionary markdownResources = null;
            foreach (string resourceSource in resourceSources) {
                ResourceDictionary resources = LoadResources(resourceSource);
                application.Resources.MergedDictionaries.Add(resources);
                if (resourceSource.EndsWith("/Hyperlink.xaml")) {
                    hyperlinkResources = resources;
                } else if (resourceSource.EndsWith("/Markdown.xaml")) {
                    markdownResources = resources;
                }
            }

            hyperlinkResources.Should().NotBeNull();
            markdownResources.Should().NotBeNull();

            Style applicationStyle = hyperlinkResources["ApplicationHyperlinkStyle"].Should().BeOfType<Style>().Subject;
            Style implicitStyle = hyperlinkResources[typeof(Hyperlink)].Should().BeOfType<Style>().Subject;
            implicitStyle.BasedOn.Should().BeSameAs(applicationStyle);

            Hyperlink standardHyperlink = new Hyperlink {
                NavigateUri = new Uri("https://nighttime-imaging.eu/"),
                Style = implicitStyle
            };
            HyperlinkBehavior.GetIsEnabled(standardHyperlink).Should().BeTrue();
            standardHyperlink.ContextMenu.Should().NotBeNull();

            UserControl[] compiledViews = [
                new AboutNINAView(),
                new IconsView(),
                new ThirdPartyLicensesView(),
                new FramingAssistantView(),
                new AvailablePluginsView(),
                new PluginsView(),
                new SkyAtlasView()
            ];
            foreach (UserControl view in compiledViews) {
                Window host = new Window { Width = 800, Height = 600, Content = view };
                host.Measure(new Size(host.Width, host.Height));
                host.Arrange(new Rect(0, 0, host.Width, host.Height));
                host.UpdateLayout();
            }

            Hyperlink[] compiledViewHyperlinks = compiledViews
                .SelectMany(FindLogicalDescendants<Hyperlink>)
                .ToArray();
            compiledViewHyperlinks.Should().HaveCount(140);
            compiledViewHyperlinks.Should().OnlyContain(hyperlink =>
                HyperlinkBehavior.GetIsEnabled(hyperlink) && hyperlink.ContextMenu != null);

            MarkdownScrollViewer viewer = new MarkdownScrollViewer {
                MarkdownStyle = markdownResources["MarkdownStyle"].Should().BeOfType<Style>().Subject,
                Markdown = "[N.I.N.A.](https://nighttime-imaging.eu/)"
            };
            Hyperlink markdownHyperlink = viewer.Document.Blocks
                .OfType<Paragraph>()
                .SelectMany(paragraph => paragraph.Inlines.OfType<Hyperlink>())
                .Single();

            HyperlinkBehavior.GetIsEnabled(markdownHyperlink).Should().BeTrue();
            markdownHyperlink.ContextMenu.Should().NotBeNull();
            WithRestoredClipboard(() => {
                GetCopyMenuItem(markdownHyperlink).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

                Clipboard.GetText().Should().Be("https://nighttime-imaging.eu/");
            });
            GC.KeepAlive(compiledViews);
        }

        private static MenuItem GetCopyMenuItem(Hyperlink hyperlink) {
            hyperlink.ContextMenu.Should().NotBeNull();
            return hyperlink.ContextMenu!.Items
                .OfType<MenuItem>()
                .Last();
        }

        private static ContextMenuEventArgs RaiseContextMenuOpening(Hyperlink hyperlink) {
            ConstructorInfo constructor = typeof(ContextMenuEventArgs)
                .GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    [typeof(object), typeof(bool)],
                    modifiers: null);
            constructor.Should().NotBeNull();
            ContextMenuEventArgs args = (ContextMenuEventArgs)constructor.Invoke([hyperlink, true]);
            args.RoutedEvent = ContextMenuService.ContextMenuOpeningEvent;
            hyperlink.RaiseEvent(args);
            return args;
        }

        private static RequestNavigateEventArgs RaiseRequestNavigate(Hyperlink hyperlink, Uri target) {
            RequestNavigateEventArgs args = new RequestNavigateEventArgs(target, string.Empty) {
                RoutedEvent = Hyperlink.RequestNavigateEvent
            };
            hyperlink.RaiseEvent(args);
            return args;
        }

        private static ResourceDictionary LoadResources(string source) {
            return new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
        }

        private static IEnumerable<T> FindLogicalDescendants<T>(DependencyObject root) where T : DependencyObject {
            foreach (object child in LogicalTreeHelper.GetChildren(root)) {
                if (child is T match) {
                    yield return match;
                }

                if (child is DependencyObject dependencyObject) {
                    foreach (T descendant in FindLogicalDescendants<T>(dependencyObject)) {
                        yield return descendant;
                    }
                }
            }
        }

        private static void WithRestoredClipboard(Action action) {
            IDataObject previousClipboard = Clipboard.GetDataObject();
            try {
                action();
            } finally {
                if (previousClipboard != null) {
                    Clipboard.SetDataObject(previousClipboard, true);
                }
            }
        }

        private sealed class RecordingCommand : ICommand {

            private readonly bool canExecute;

            public RecordingCommand(bool canExecute = true) {
                this.canExecute = canExecute;
            }

            public List<object> Executions { get; } = new List<object>();

            public event EventHandler CanExecuteChanged {
                add { }
                remove { }
            }

            public bool CanExecute(object parameter) {
                return canExecute;
            }

            public void Execute(object parameter) {
                Executions.Add(parameter);
            }
        }
    }
}