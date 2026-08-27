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
using NINA.View.About;
using NINA.WPF.Base.Behaviors;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class HyperlinkCopyBehaviorTest {

        [Test]
        public void CopyAction_PrefersNavigateUriAndCopiesItsOriginalString() {
            Hyperlink hyperlink = new Hyperlink {
                NavigateUri = new Uri("https://nighttime-imaging.eu/release%20notes?channel=nightly"),
                CommandParameter = "https://fallback.example/"
            };
            HyperlinkCopyBehavior.SetIsEnabled(hyperlink, true);

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
            HyperlinkCopyBehavior.SetIsEnabled(hyperlink, true);

            WithRestoredClipboard(() => {
                GetCopyMenuItem(hyperlink).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

                Clipboard.GetText().Should().Be(target);
            });
        }

        [Test]
        public void ContextMenuOpening_SuppressesOwnedMenuWithoutUrlAndPreservesExistingMenu() {
            Hyperlink ownedMenuHyperlink = new Hyperlink { CommandParameter = "not a URL" };
            HyperlinkCopyBehavior.SetIsEnabled(ownedMenuHyperlink, true);

            ContextMenuEventArgs ownedMenuArgs = RaiseContextMenuOpening(ownedMenuHyperlink);

            ownedMenuArgs.Handled.Should().BeTrue();
            GetCopyMenuItem(ownedMenuHyperlink).Visibility.Should().Be(Visibility.Collapsed);

            MenuItem existingItem = new MenuItem { Header = "Existing" };
            ContextMenu existingMenu = new ContextMenu { Items = { existingItem } };
            Hyperlink existingMenuHyperlink = new Hyperlink {
                CommandParameter = "not a URL",
                ContextMenu = existingMenu
            };
            HyperlinkCopyBehavior.SetIsEnabled(existingMenuHyperlink, true);

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

            HyperlinkCopyBehavior.SetIsEnabled(hyperlink, true);
            existingMenu.Items.Cast<object>().Should().HaveCount(3);

            HyperlinkCopyBehavior.SetIsEnabled(hyperlink, false);
            hyperlink.ContextMenu.Should().BeSameAs(existingMenu);
            existingMenu.Items.Cast<object>().Should().Equal(existingItem);

            HyperlinkCopyBehavior.SetIsEnabled(hyperlink, true);
            existingMenu.Items.Cast<object>().Should().HaveCount(3);

            HyperlinkCopyBehavior.SetIsEnabled(hyperlink, false);
            existingMenu.Items.Cast<object>().Should().Equal(existingItem);

            Hyperlink ownedMenuHyperlink = new Hyperlink { NavigateUri = new Uri("https://nighttime-imaging.eu/") };
            HyperlinkCopyBehavior.SetIsEnabled(ownedMenuHyperlink, true);
            ownedMenuHyperlink.ContextMenu.Should().NotBeNull();

            HyperlinkCopyBehavior.SetIsEnabled(ownedMenuHyperlink, false);
            ownedMenuHyperlink.ContextMenu.Should().BeNull();
        }

        [Test]
        public void CompiledStyles_EnableStandardAndMarkdownHyperlinks() {
            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            ResourceDictionary profileResources = LoadResources("/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml");
            application.Resources.MergedDictionaries.Add(profileResources);
            ResourceDictionary hyperlinkResources = LoadResources("/NINA.WPF.Base;component/Resources/Styles/Hyperlink.xaml");
            application.Resources.MergedDictionaries.Add(hyperlinkResources);
            ResourceDictionary brushResources = LoadResources("/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml");
            application.Resources.MergedDictionaries.Add(brushResources);
            ResourceDictionary markdownResources = LoadResources("/NINA;component/Resources/Styles/Markdown.xaml");

            Style copyableStyle = hyperlinkResources["CopyableHyperlinkStyle"].Should().BeOfType<Style>().Subject;
            Style implicitStyle = hyperlinkResources[typeof(Hyperlink)].Should().BeOfType<Style>().Subject;
            implicitStyle.BasedOn.Should().BeSameAs(copyableStyle);

            Hyperlink standardHyperlink = new Hyperlink {
                NavigateUri = new Uri("https://nighttime-imaging.eu/"),
                Style = implicitStyle
            };
            HyperlinkCopyBehavior.GetIsEnabled(standardHyperlink).Should().BeTrue();
            standardHyperlink.ContextMenu.Should().NotBeNull();

            AboutNINAView aboutView = new AboutNINAView();
            Window host = new Window { Width = 800, Height = 600, Content = aboutView };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            Hyperlink[] compiledViewHyperlinks = FindLogicalDescendants<Hyperlink>(aboutView).ToArray();
            compiledViewHyperlinks.Should().HaveCount(8);
            compiledViewHyperlinks.Should().OnlyContain(hyperlink =>
                HyperlinkCopyBehavior.GetIsEnabled(hyperlink) && hyperlink.ContextMenu != null);

            MarkdownScrollViewer viewer = new MarkdownScrollViewer {
                MarkdownStyle = markdownResources["MarkdownStyle"].Should().BeOfType<Style>().Subject,
                Markdown = "[N.I.N.A.](https://nighttime-imaging.eu/)"
            };
            Hyperlink markdownHyperlink = viewer.Document.Blocks
                .OfType<Paragraph>()
                .SelectMany(paragraph => paragraph.Inlines.OfType<Hyperlink>())
                .Single();

            HyperlinkCopyBehavior.GetIsEnabled(markdownHyperlink).Should().BeTrue();
            markdownHyperlink.ContextMenu.Should().NotBeNull();
            WithRestoredClipboard(() => {
                GetCopyMenuItem(markdownHyperlink).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

                Clipboard.GetText().Should().Be("https://nighttime-imaging.eu/");
            });
            GC.KeepAlive(host);
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
    }
}