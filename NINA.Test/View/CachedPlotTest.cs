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
using NINA.Astrometry;
using NINA.Test.Sequencer.Editing;
using NINA.WPF.Base.View;
using OxyPlot;
using OxyPlot.Wpf;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.View {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class CachedPlotTest {
        [TestCase("00 03 06 09", "Segoe UI")]
        [TestCase("South 02:13\n45\u00b0", "Segoe UI")]
        [TestCase("\u0627\u0644\u0639\u0631\u0628\u064a\u0629", "Global User Interface")]
        [TestCase("\u5357 02:13", "Segoe UI")]
        [TestCase("e\u0301", "Global User Interface")]
        public void MeasureText_PreservesStockMetricsAndFontFallback(string text, string fontFamily) {
            var context = new TestPlot().CreateContext();
            var stock = new CanvasRenderContext(new Canvas());
            var expected = stock.MeasureText(text, fontFamily, 12, 400);

            context.MeasureText(text, fontFamily, 12, 400).Should().Be(expected);
            context.MeasureText(text, fontFamily, 12, 400).Should().Be(expected);
            context.CleanUp();
            context.MeasureText(text, fontFamily, 12, 400).Should().Be(expected);
        }

        [Test]
        public void MeasureText_UsesCurrentFontFormattingDpiAndCulture() {
            var context = new TestPlot().CreateContext();
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;
            try {
                var settings = new[] {
                    ("Segoe UI", 12.0, 400.0, TextFormattingMode.Display, 1.0, "en-US"),
                    ("Consolas", 12.0, 400.0, TextFormattingMode.Display, 1.0, "en-US"),
                    ("Segoe UI", 24.0, 400.0, TextFormattingMode.Display, 1.0, "en-US"),
                    ("Segoe UI", 12.0, 700.0, TextFormattingMode.Display, 1.0, "en-US"),
                    ("Segoe UI", 12.0, 400.0, TextFormattingMode.Ideal, 1.0, "en-US"),
                    ("Segoe UI", 12.0, 400.0, TextFormattingMode.Display, 1.5, "en-US"),
                    ("Segoe UI", 12.0, 400.0, TextFormattingMode.Display, 1.0, "ar-SA"),
                    ("Segoe UI", 12.0, 400.0, TextFormattingMode.Display, 1.0, "en-US")
                };
                foreach (var (family, size, weight, formatting, dpi, culture) in settings) {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                    context.TextFormattingMode = formatting;
                    context.DpiScale = dpi;
                    var stock = new CanvasRenderContext(new Canvas()) { TextFormattingMode = formatting, DpiScale = dpi };
                    context.MeasureText("Wi 03:15", family, size, weight).Should()
                        .Be(stock.MeasureText("Wi 03:15", family, size, weight));
                }
            } finally {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }

        [Test]
        public void RepeatedMeasurements_DoNotAllocateTextBlocksForEachLabel() {
            var context = new TestPlot().CreateContext();
            const string label = "03:15";
            for (int i = 0; i < 32; i++) context.MeasureText(label, "Segoe UI", 12, 400);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 128; i++) context.MeasureText(label, "Segoe UI", 12, 400);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            // A repeated label should reuse its size rather than construct 128 WPF controls.
            allocated.Should().BeLessThan(16 * 1024);
        }

        [Test]
        public void Measurements_DoNotRetainOldLabelsAfterCapacityOrRenderCleanup() {
            var context = new TestPlot().CreateContext();
            var firstLabel = MeasureUniqueLabel(context);
            for (int i = 0; i < 1024; i++) context.MeasureText($"label-{i}", "Segoe UI", 12, 400);

            Collect();
            firstLabel.IsAlive.Should().BeFalse("the measurement cache must be bounded");

            var lastLabel = MeasureUniqueLabel(context);
            context.CleanUp();

            Collect();
            lastLabel.IsAlive.Should().BeFalse("a completed render must release its cached labels");
            GC.KeepAlive(context);
        }

        [Test]
        public void MeasureText_PreservesMeasurementModeChanges() {
            var context = new TestPlot().CreateContext();
            foreach (var method in new[] { TextMeasurementMethod.TextBlock, TextMeasurementMethod.GlyphTypeface, TextMeasurementMethod.TextBlock }) {
                context.TextMeasurementMethod = method;
                var stock = new CanvasRenderContext(new Canvas()) { TextMeasurementMethod = method };
                context.MeasureText("Wi 03:15", "Segoe UI", 12, 400).Should()
                    .Be(stock.MeasureText("Wi 03:15", "Segoe UI", 12, 400));
            }
        }

        [Test]
        public void AltitudeChart_RendersBoundDataAndTogglesBothAxes() {
            using var scope = new CoreEditorTestScope();
            var referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
            var target = new InputTarget(Angle.ByDegree(52), Angle.ByDegree(13), null);
            target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(8), Angle.ByDegree(40), Epoch.J2000);
            var chart = new AltitudeChart {
                Width = 600, Height = 240, DataContext = target.DeepSkyObject,
                NighttimeData = new NighttimeData(referenceDate, referenceDate, AstroUtil.MoonPhase.FullMoon, 50, null, null, null, null, null)
            };
            scope.Host.Content = chart;
            scope.Host.UpdateLayout();
            Drain();
            var plot = Descendants<CachedPlot>(chart).Single();
            plot.TextMeasurementMethod.Should().Be(TextMeasurementMethod.TextBlock);
            plot.ActualModel.GetLastPlotException().Should().BeNull();
            var line = plot.ActualModel.Series.OfType<OxyPlot.Series.LineSeries>().Single(series => series.GetType() == typeof(OxyPlot.Series.LineSeries));
            AssertRenderedData(line, target.DeepSkyObject.Altitudes);
            var dateAxis = plot.ActualModel.Axes.OfType<OxyPlot.Axes.DateTimeAxis>().Single();
            (dateAxis.ActualMaximum - dateAxis.ActualMinimum).Should().BeInRange(1, 1.1);

            foreach (bool visible in new[] { false, true }) {
                chart.AnnotateAltitudeAxis = visible;
                chart.AnnotateTimeAxis = visible;
                target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(visible ? 12 : 10), Angle.ByDegree(visible ? -20 : 45), Epoch.J2000);
                Drain();
                plot.ActualModel.Axes.Should().OnlyContain(axis => axis.IsAxisVisible == visible);
                AssertRenderedData(line, target.DeepSkyObject.Altitudes);
                plot.ActualModel.GetLastPlotException().Should().BeNull();
            }

            foreach (string marker in new[] { "first plot style", "replacement plot style" }) {
                Application.Current.Resources[typeof(Plot)] = new Style(typeof(Plot)) {
                    Setters = { new Setter(FrameworkElement.TagProperty, marker) }
                };
                Drain();
                plot.Tag.Should().Be(marker);
            }
            Application.Current.Resources.Remove(typeof(Plot));
            Drain();
            plot.Tag.Should().BeNull();
        }

        private static void AssertRenderedData(OxyPlot.Series.LineSeries line, List<DataPoint> expected) {
            line.ItemsSource.Cast<DataPoint>().Should().Equal(expected);
            var peak = expected.MaxBy(point => point.Y);
            var hit = line.GetNearestPoint(line.Transform(peak), false);
            hit.Should().NotBeNull();
            hit.DataPoint.Should().Be(peak);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference MeasureUniqueLabel(CanvasRenderContext context) {
            string label = Guid.NewGuid().ToString();
            context.MeasureText(label, "Segoe UI", 12, 400);
            return new WeakReference(label);
        }

        private static void Collect() {
            Drain();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private sealed class TestPlot : CachedPlot {
            public CanvasRenderContext CreateContext() {
                plotPresenter = CreatePlotPresenter();
                return (CanvasRenderContext)CreateRenderContext();
            }
        }
    }
}
