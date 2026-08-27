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
using NINA.View;
using NINA.View.Equipment;
using NINA.View.Equipment.Guider;
using NUnit.Framework;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class GuiderChartViewTest {

        [TestCase(typeof(GuiderGraph))]
        [TestCase(typeof(AnchorableGuiderView))]
        [TestCase(typeof(PHD2DetailView))]
        [TestCase(typeof(PHD2SetupView))]
        public void ChangedGuiderViews_ConstructAtRuntime(Type viewType) {
            EnsureApplicationResources();

            UserControl view = (UserControl)Activator.CreateInstance(viewType)!;
            using TestWindow host = new TestWindow { Content = view };

            host.Measure(new Size(1280, 800));
            host.Arrange(new Rect(0, 0, 1280, 800));
            host.UpdateLayout();
        }

        [TestCase(typeof(GuiderGraph))]
        [TestCase(typeof(AnchorableGuiderView))]
        public void GuideCharts_UseIndependentUpperHalfAxesForPHD2StarMetrics(Type viewType) {
            EnsureApplicationResources();
            UserControl view = (UserControl)Activator.CreateInstance(viewType)!;
            Plot plot = FindDescendants<Plot>(view).Single();

            AssertMetricSeries(plot, "StarMass", "StarMassAxis", "PHD2GuideChartShowStarMass");
            AssertMetricSeries(plot, "SNR", "StarSNRAxis", "PHD2GuideChartShowSNR");
        }

        [TestCase(typeof(PHD2DetailView))]
        [TestCase(typeof(PHD2SetupView))]
        public void PHD2Options_ExposeBothStarMetricToggles(Type viewType) {
            EnsureApplicationResources();
            UserControl view = (UserControl)Activator.CreateInstance(viewType)!;
            HashSet<string> paths = FindDescendants<CheckBox>(view)
                .Select(checkBox => BindingOperations.GetBinding(checkBox, ToggleButton.IsCheckedProperty)?.Path?.Path)
                .OfType<string>()
                .ToHashSet();

            paths.Should().Contain("PHD2GuideChartShowStarMass");
            paths.Should().Contain("PHD2GuideChartShowSNR");
        }

        private static void AssertMetricSeries(Plot plot, string dataFieldY, string axisKey, string settingPath) {
            LineSeries series = plot.Series.OfType<LineSeries>().Single(candidate => candidate.DataFieldY == dataFieldY);
            LinearAxis axis = plot.Axes.OfType<LinearAxis>().Single(candidate => candidate.Key == axisKey);
            Binding? visibilityBinding = BindingOperations.GetBinding(series, UIElement.VisibilityProperty);

            series.YAxisKey.Should().Be(axisKey);
            axis.Minimum.Should().Be(0);
            axis.MinimumPadding.Should().Be(0);
            axis.MaximumPadding.Should().Be(0);
            axis.StartPosition.Should().Be(0.5);
            axis.EndPosition.Should().Be(1);
            visibilityBinding.Should().NotBeNull();
            visibilityBinding!.Path.Path.Should().EndWith(settingPath);
        }

        private static IEnumerable<T> FindDescendants<T>(DependencyObject parent) where T : DependencyObject {
            foreach (object item in LogicalTreeHelper.GetChildren(parent)) {
                if (item is not DependencyObject child) {
                    continue;
                }
                if (child is T result) {
                    yield return result;
                }
                foreach (T descendant in FindDescendants<T>(child)) {
                    yield return descendant;
                }
            }
        }

        private static void EnsureApplicationResources() {
            const string resourcesLoadedMarker = "GuiderChartViewTest.ResourcesLoaded";
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

        private sealed class TestWindow : Window, IDisposable {

            public void Dispose() {
                Close();
            }
        }
    }
}
