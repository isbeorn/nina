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
using NINA.Core.Enum;
using NINA.CustomControlLibrary;
using NINA.View;
using NINA.ViewModel.FramingAssistant;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class FramingAssistantViewTest {

        [Test]
        public void Constructor_LoadsCompiledXaml() {
            EnsureApplicationResources();

            Action construct = ConstructInHost;

            construct.Should().NotThrow();
        }

        [Test]
        public void ObservationTimeEditor_IsOnlyVisibleWhenOfflineSkyMapIsLoaded() {
            EnsureApplicationResources();
            SourceContext context = new SourceContext {
                FramingAssistantSource = SkySurveySource.SKYATLAS
            };
            FramingAssistantView view = new FramingAssistantView {
                DataContext = context
            };
            Window host = new Window {
                Width = 1280,
                Height = 800,
                Content = view
            };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();

            GroupBox? editor = view.FindName("PART_ObservationTime") as GroupBox;

            editor.Should().NotBeNull();
            editor!.Visibility.Should().Be(Visibility.Collapsed);

            context.SkyMapAnnotator.DynamicFoV = true;
            DrainDispatcher();
            editor.Visibility.Should().Be(Visibility.Visible);

            context.SkyMapAnnotator.DynamicFoV = false;
            DrainDispatcher();
            editor.Visibility.Should().Be(Visibility.Collapsed);
            GC.KeepAlive(host);
        }

        [Test]
        public void ObservationEditor_UsesSeparateClockRowAndDescriptiveResetTooltip() {
            EnsureApplicationResources();
            FramingAssistantView view = new FramingAssistantView {
                DataContext = new SourceContext {
                    FramingAssistantSource = SkySurveySource.SKYATLAS,
                    SkyMapAnnotator = new SkyMapAnnotatorContext { DynamicFoV = true }
                }
            };
            Window host = new Window {
                Width = 1280,
                Height = 800,
                Content = view
            };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();
            WrapPanel? dateRow = view.FindName("PART_ObservationDateRow") as WrapPanel;
            WrapPanel? clockRow = view.FindName("PART_ObservationClockRow") as WrapPanel;
            Button? reset = view.FindName("PART_ResetObservationTime") as Button;

            dateRow.Should().NotBeNull();
            clockRow.Should().NotBeNull();
            StackPanel? rows = dateRow!.Parent as StackPanel;
            rows.Should().NotBeNull();
            clockRow!.Parent.Should().BeSameAs(rows);
            rows!.Children.IndexOf(clockRow).Should().BeGreaterThan(rows.Children.IndexOf(dateRow));
            reset.Should().NotBeNull();
            reset!.ToolTip.Should().Be("Reset observation date and time to the current time");
            GC.KeepAlive(host);
        }

        [TestCase("PART_ObservationMonth", 2, 2026, 12, 31, 20, 15, 2027, 1, 31, 20, 15)]
        [TestCase("PART_ObservationDay", 2, 2028, 2, 29, 20, 15, 2028, 3, 1, 20, 15)]
        [TestCase("PART_ObservationHour", 2, 2026, 12, 31, 23, 15, 2027, 1, 1, 0, 15)]
        [TestCase("PART_ObservationMinute", 2, 2026, 12, 31, 23, 59, 2027, 1, 1, 0, 0)]
        [TestCase("PART_ObservationMonth", 0, 2026, 1, 31, 20, 15, 2025, 12, 31, 20, 15)]
        [TestCase("PART_ObservationDay", 0, 2028, 3, 1, 20, 15, 2028, 2, 29, 20, 15)]
        [TestCase("PART_ObservationHour", 0, 2026, 1, 1, 0, 15, 2025, 12, 31, 23, 15)]
        [TestCase("PART_ObservationMinute", 0, 2026, 1, 1, 0, 0, 2025, 12, 31, 23, 59)]
        public void ObservationStepper_WrapsAcrossBoundaries(
            string stepperName,
            int buttonColumn,
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            int expectedMinute) {
            EnsureApplicationResources();
            DateTime now = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local);
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => now, startTimer: false);
            FramingAssistantView view = new FramingAssistantView {
                DataContext = new SourceContext {
                    FramingAssistantSource = SkySurveySource.SKYATLAS,
                    SkyMapAnnotator = new SkyMapAnnotatorContext { DynamicFoV = true },
                    TimeContext = timeContext
                }
            };
            Window host = new Window {
                Width = 1280,
                Height = 800,
                Content = view
            };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();
            IntStepperControl? stepper = view.FindName(stepperName) as IntStepperControl;

            stepper.Should().NotBeNull();
            stepper!.ApplyTemplate();
            Button? button = FindStepperButton(stepper, buttonColumn);
            button.Should().NotBeNull();
            button!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            DrainDispatcher();

            timeContext.SelectedDateTime.Should().Be(
                new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Local));
            GC.KeepAlive(host);
        }

        private static Button? FindStepperButton(DependencyObject parent, int column) {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is Button button && Grid.GetColumn(button) == column) {
                    return button;
                }

                Button? result = FindStepperButton(child, column);
                if (result is not null) {
                    return result;
                }
            }
            return null;
        }

        private static void ConstructInHost() {
            Window host = new Window {
                Width = 1280,
                Height = 800,
                Content = new FramingAssistantView()
            };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();
            GC.KeepAlive(host);
        }

        private static void DrainDispatcher() {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        private static void EnsureApplicationResources() {
            const string resourcesLoadedMarker = "FramingAssistantViewTest.ResourcesLoaded";
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

        private sealed class SourceContext {
            public SkySurveySource FramingAssistantSource { get; set; }
            public SkyMapAnnotatorContext SkyMapAnnotator { get; set; } = new SkyMapAnnotatorContext();
            public FramingAssistantTimeContext TimeContext { get; set; } = null!;
        }

        private sealed class SkyMapAnnotatorContext : INotifyPropertyChanged {
            private bool dynamicFoV;

            public event PropertyChangedEventHandler? PropertyChanged;

            public bool DynamicFoV {
                get => dynamicFoV;
                set {
                    if (dynamicFoV == value) {
                        return;
                    }

                    dynamicFoV = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DynamicFoV)));
                }
            }
        }
    }
}