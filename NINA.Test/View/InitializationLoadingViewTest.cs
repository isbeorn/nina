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
using NINA.Core.Locale;
using NINA.CustomControlLibrary;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Utility;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class InitializationLoadingViewTest {
        private NINA.Profile.Profile profile = null!;
        private ResourceDictionary[] originalDictionaries = [];
        private Dictionary<Uri, WeakReference> originalSharedDictionaries = [];
        private object? originalProfileService;
        private Window? originalMainWindow;
        private bool hadProfileService;

        [OneTimeSetUp]
        public void SetUp() {
            var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            originalDictionaries = app.Resources.MergedDictionaries.ToArray();
            originalSharedDictionaries = SharedResourceDictionary.SharedDictionaries.ToDictionary(entry => entry.Key, entry => entry.Value);
            hadProfileService = app.Resources.Keys.Cast<object>().Contains("ProfileService");
            originalProfileService = hadProfileService ? app.Resources["ProfileService"] : null;
            originalMainWindow = app.MainWindow;
            profile = new NINA.Profile.Profile();
            EnsureApplicationResources();
        }

        [OneTimeTearDown]
        public void TearDown() {
            var resources = Application.Current.Resources;
            Application.Current.MainWindow = originalMainWindow;
            resources.MergedDictionaries.Clear();
            foreach (var dictionary in originalDictionaries) {
                resources.MergedDictionaries.Add(dictionary);
            }

            if (hadProfileService) {
                resources["ProfileService"] = originalProfileService;
            } else {
                resources.Remove("ProfileService");
            }

            SharedResourceDictionary.SharedDictionaries.Clear();
            foreach (var entry in originalSharedDictionaries) {
                SharedResourceDictionary.SharedDictionaries.Add(entry.Key, entry.Value);
            }

            profile.Dispose();
        }

        [TestCase("Imaging", "Persian Faint")]
        [TestCase("Sequencer", "Persian Faint")]
        [TestCase("Plugins", "Persian Faint")]
        public void Initialization_UsesTheSharedHorizontalIndicatorAndTracksCompletion(string page, string schema) {
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            UserControl view = page switch {
                "Imaging" => new NINA.View.OverView(),
                "Sequencer" => new NINA.View.SequenceView(),
                _ => new NINA.View.Plugins.PluginsView()
            };
            string caption = Loc.Instance[page switch {
                "Imaging" => "LblInitializingLayout",
                "Sequencer" => "LblInitializingSequencer",
                _ => "LblInitializingPlugins"
            }];
            string path = page == "Imaging" ? "LoadAvalonDockLayoutCommand.Execution.IsCompleted" : "Initialized";
            var root = (Grid)view.Content;
            var overlay = root.Children.OfType<FrameworkElement>().Single(element => {
                var binding = BindingOperations.GetBinding(element, UIElement.VisibilityProperty);
                return binding?.Path.Path == path
                    && Equals(binding.Converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture), Visibility.Visible);
            });
            var state = new InitializationState();
            view.DataContext = state;
            var host = new Window {
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false,
                Background = (Brush)view.FindResource("BackgroundBrush"), Content = view
            };
            try {
                host.Show();
                Layout(host, 800, 600);
                overlay.IsVisible.Should().BeTrue();
                FindVisuals<LoadingControl>(overlay).Should().BeEmpty();
                var presenter = FindVisuals<ContentControl>(overlay).Single(control => Equals(control.Content, caption));
                presenter.ContentTemplate.Should().BeSameAs(view.FindResource("InitializationLoadingTemplate"));
                var label = FindVisuals<TextBlock>(presenter).Single(text => text.Text == caption);
                var bar = FindVisuals<ProgressBar>(overlay).Single();
                bar.Style.Should().BeSameAs(view.FindResource("LoadingProgressBar"));
                bar.IsIndeterminate.Should().BeTrue();
                bar.Orientation.Should().Be(Orientation.Horizontal);
                bar.ActualHeight.Should().BeGreaterThan(0).And.BeLessThan(bar.ActualWidth);
                bar.ActualWidth.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(view.ActualWidth);
                ((SolidColorBrush)label.Foreground).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.PrimaryColor);
                double originalCaptionHeight = label.ActualHeight;
                presenter.Content = "Initializing this application section with an intentionally long translated loading caption that must wrap within the available space";
                Layout(host, 800, 600);
                label.ActualHeight.Should().BeGreaterThan(originalCaptionHeight);
                var labelBounds = label.TransformToAncestor(view).TransformBounds(new Rect(label.RenderSize));
                labelBounds.Left.Should().BeGreaterThanOrEqualTo(0);
                labelBounds.Right.Should().BeLessThanOrEqualTo(view.ActualWidth);
                labelBounds.Bottom.Should().BeLessThanOrEqualTo(view.ActualHeight);
                view.LayoutTransform = new ScaleTransform(2, 2);
                Layout(host, 1600, 1200);
                state.Initialized = true;
                Layout(host, 1600, 1200);
                overlay.Visibility.Should().Be(Visibility.Collapsed);
                bar.IsVisible.Should().BeFalse();
                bar.IsIndeterminate.Should().BeFalse();
                state.Initialized = false;
                Layout(host, 1600, 1200);
                overlay.IsVisible.Should().BeTrue();
                bar.IsVisible.Should().BeTrue();
                bar.IsIndeterminate.Should().BeTrue();
            } finally {
                host.Close();
            }
        }

        private static void Layout(Window host, double width, double height) {
            host.Width = width;
            host.Height = height;
            host.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
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

        private sealed class InitializationState : INotifyPropertyChanged {
            private bool initialized;

            public InitializationState() {
                LoadAvalonDockLayoutCommand = new { Execution = this };
            }

            public object LoadAvalonDockLayoutCommand { get; }

            public bool IsCompleted => initialized;

            public bool Initialized {
                get => initialized;
                set {
                    initialized = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Initialized)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private void EnsureApplicationResources() {
            var app = Application.Current;
            var profiles = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            app.Resources["ProfileService"] = profiles;
            var profileResources = new ResourceDictionary { ["ProfileService"] = profiles };
            app.Resources.MergedDictionaries.Add(profileResources);
            SharedResourceDictionary.SharedDictionaries[new Uri("/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml", UriKind.Relative)] = new WeakReference(profileResources);
            string[] sources = [
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
                "/NINA;component/Resources/Styles/Window.xaml",
                "/NINA;component/Resources/Styles/AvalonDock.xaml",
                "/NINA;component/Resources/Styles/Oxyplot.xaml",
                "/NINA;component/Resources/Styles/Markdown.xaml"
            ];
            foreach (string source in sources) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
            }

        }
    }
}
