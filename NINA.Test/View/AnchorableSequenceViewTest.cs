#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Equipment.Equipment.MyCamera;
using NINA.View;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class AnchorableSequenceViewTest {

        [Test]
        public void ReadoutRow_WithMultipleModes_DisplaysConfiguredSequenceModeAndTracksBoundaryChanges() {
            EnsureApplicationResources();
            CameraInfo cameraInfo = new CameraInfo {
                Connected = true,
                ReadoutModes = new List<string> { "Fast", "11M Mode" },
                ReadoutModeForNormalImages = 0
            };
            SequenceContext context = new SequenceContext(cameraInfo, 1);
            AnchorableSequenceView view = CreateView(context);
            using TestWindow host = Layout(view);

            UniformGrid readoutRow = FindReadoutRow(view);
            TextBlock value = FindReadoutValue(readoutRow);

            readoutRow.Parent.Should().BeOfType<Border>().Subject.Visibility.Should().Be(Visibility.Visible);
            value.Text.Should().Be("11M Mode");

            context.ActiveProfile.CameraSettings.ReadoutModeForNormalImages = 0;
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.DataBind);

            value.Text.Should().Be("Fast");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ReadoutRow_WithFewerThanTwoModes_IsCollapsed(int modeCount) {
            EnsureApplicationResources();
            CameraInfo cameraInfo = new CameraInfo {
                Connected = true,
                ReadoutModes = Enumerable.Range(0, modeCount).Select(index => $"Mode {index}").ToList(),
                ReadoutModeForNormalImages = 0
            };
            AnchorableSequenceView view = CreateView(new SequenceContext(cameraInfo, 0));
            using TestWindow host = Layout(view);

            FindReadoutRow(view).Parent.Should().BeOfType<Border>().Subject.Visibility.Should().Be(Visibility.Collapsed);
        }

        private static AnchorableSequenceView CreateView(SequenceContext context) {
            return new AnchorableSequenceView {
                DataContext = context
            };
        }

        private static TestWindow Layout(AnchorableSequenceView view) {
            TestWindow host = new TestWindow {
                Width = 400,
                Height = 700,
                Content = view
            };
            host.Show();
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            host.UpdateLayout();
            return host;
        }

        private static UniformGrid FindReadoutRow(AnchorableSequenceView view) {
            TextBlock label = FindLogicalDescendants<TextBlock>(view)
                .Single(textBlock => textBlock.Text == "Readout");
            return label.Parent.Should().BeOfType<UniformGrid>().Subject;
        }

        private static TextBlock FindReadoutValue(UniformGrid row) {
            return FindLogicalDescendants<TextBlock>(row)
                .Single(textBlock => textBlock.Text != "Readout");
        }

        private static IEnumerable<T> FindLogicalDescendants<T>(DependencyObject root) where T : DependencyObject {
            foreach (object childObject in LogicalTreeHelper.GetChildren(root)) {
                if (childObject is not DependencyObject child) {
                    continue;
                }
                if (child is T match) {
                    yield return match;
                }
                foreach (T descendant in FindLogicalDescendants<T>(child)) {
                    yield return descendant;
                }
            }
        }

        private static void EnsureApplicationResources() {
            const string resourcesLoadedMarker = "AnchorableSequenceViewTest.ResourcesLoaded";
            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            if (app.Resources.Contains(resourcesLoadedMarker)) {
                return;
            }

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
                "/NINA.WPF.Base;component/Resources/Styles/SplitButton.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ColorPicker.xaml",
                "/NINA;component/Resources/Styles/Window.xaml",
                "/NINA;component/Resources/Styles/AvalonDock.xaml",
                "/NINA;component/Resources/Styles/Oxyplot.xaml",
                "/NINA;component/Resources/Styles/Markdown.xaml"
            ];

            foreach (string resourceSource in resourceSources) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                    Source = new Uri(resourceSource, UriKind.Relative)
                });
            }
            app.Resources[resourcesLoadedMarker] = true;
        }

        private sealed class SequenceContext {

            public SequenceContext(CameraInfo cameraInfo, short? configuredReadoutMode) {
                CameraInfo = cameraInfo;
                ActiveProfile = new ProfileContext(configuredReadoutMode);
            }

            public CameraInfo CameraInfo { get; }
            public ProfileContext ActiveProfile { get; }
        }

        private sealed class ProfileContext {

            public ProfileContext(short? configuredReadoutMode) {
                CameraSettings = new CameraSettingsContext { ReadoutModeForNormalImages = configuredReadoutMode };
            }

            public CameraSettingsContext CameraSettings { get; }
        }

        private sealed class CameraSettingsContext : NINA.Core.Utility.BaseINPC {
            private short? readoutModeForNormalImages;

            public short? ReadoutModeForNormalImages {
                get => readoutModeForNormalImages;
                set {
                    if (readoutModeForNormalImages != value) {
                        readoutModeForNormalImages = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private sealed class TestWindow : Window, IDisposable {

            public void Dispose() {
                Close();
            }
        }
    }
}
