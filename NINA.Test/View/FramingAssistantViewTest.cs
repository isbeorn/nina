#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.CustomControlLibrary;
using NINA.View;
using NINA.ViewModel.FramingAssistant;
using NINA.WPF.Base.Behaviors;
using NINA.WPF.Base.SkySurvey;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        public void OfflineMapDragSurfaces_BindCompleteInteractionLifecycle() {
            EnsureApplicationResources();
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => DateTime.Now, startTimer: false);
            SourceContext context = new SourceContext {
                FramingAssistantSource = SkySurveySource.SKYATLAS,
                TimeContext = timeContext,
                DragStartCommand = new RelayCommand(() => { }),
                DragMoveCommand = new RelayCommand(() => { }),
                DragStopCommand = new RelayCommand(() => { })
            };
            FramingAssistantView view = new FramingAssistantView { DataContext = context };
            Window host = new Window { Width = 1280, Height = 800, Content = view };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();

            NINA.WPF.Base.View.ImageView imageView = FindDescendants<NINA.WPF.Base.View.ImageView>(view).Single();
            DependencyObject imageArea = imageView.ImageAreaContent.Should().BeAssignableTo<DependencyObject>().Subject;
            FrameworkElement[] dragSurfaces = FindDescendants<FrameworkElement>(imageArea)
                .Where(element => BindingPath(element, DragCommandBehavior.DragMoveCommandProperty) == "DragMoveCommand")
                .ToArray();

            dragSurfaces.Should().HaveCount(2);
            dragSurfaces.Should().OnlyContain(element =>
                BindingPath(element, DragCommandBehavior.DragStartCommandProperty) == "DragStartCommand"
                && BindingPath(element, DragCommandBehavior.DragStopCommandProperty) == "DragStopCommand");
            System.Windows.Controls.Image skyMapImage = FindDescendants<System.Windows.Controls.Image>(imageArea)
                .Single(image => BindingPath(image, System.Windows.Controls.Image.SourceProperty) == "SkyMapAnnotator.SkyMapOverlay");
            RenderOptions.GetBitmapScalingMode(skyMapImage).Should().Be(BitmapScalingMode.NearestNeighbor);
            GC.KeepAlive(host);
        }

        [TestCase(0, true)]
        [TestCase(37, true)]
        [TestCase(323, true)]
        [TestCase(90, true)]
        [TestCase(180, true)]
        [TestCase(270, true)]
        [TestCase(0, false)]
        [TestCase(37, false)]
        [TestCase(323, false)]
        [TestCase(90, false)]
        [TestCase(180, false)]
        [TestCase(270, false)]
        public void ThreePanelMosaic_BaseAndPanelsRotateAroundTheMosaicCenter(double rotation, bool horizontal) {
            EnsureApplicationResources();
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => DateTime.Now, startTimer: false);
            double mainX = horizontal ? 120 : 300;
            double mainY = horizontal ? 200 : 20;
            double mainWidth = horizontal ? 560 : 200;
            double mainHeight = horizontal ? 200 : 560;
            FramingRectangle mainRectangle = new FramingRectangle(0, mainX, mainY, mainWidth, mainHeight) {
                Rotation = rotation
            };
            SkyMapCameraRectanglePlacement mainPlacement = new SkyMapCameraRectanglePlacement(mainRectangle);
            mainPlacement.Update(mainX, mainY, rotation);
            FramingRectangle leftRectangle = new FramingRectangle(0, 0, 0, 200, 200) { Id = 1 };
            SkyMapCameraRectanglePlacement leftPlacement = new SkyMapCameraRectanglePlacement(leftRectangle);
            UpdatePanelPlacement(leftPlacement, rotation, horizontal ? -180 : 0, horizontal ? 0 : -180);
            FramingRectangle centerRectangle = new FramingRectangle(0, 180, 0, 200, 200) { Id = 2 };
            SkyMapCameraRectanglePlacement centerPlacement = new SkyMapCameraRectanglePlacement(centerRectangle);
            UpdatePanelPlacement(centerPlacement, rotation, 0, 0);
            FramingRectangle rightRectangle = new FramingRectangle(0, 360, 0, 200, 200) { Id = 3 };
            SkyMapCameraRectanglePlacement rightPlacement = new SkyMapCameraRectanglePlacement(rightRectangle);
            UpdatePanelPlacement(rightPlacement, rotation, horizontal ? 180 : 0, horizontal ? 0 : 180);
            SourceContext context = new SourceContext {
                FramingAssistantSource = SkySurveySource.SKYATLAS,
                SkyMapAnnotator = new SkyMapAnnotatorContext { DynamicFoV = true },
                TimeContext = timeContext,
                ImageParameter = new SkySurveyImage {
                    Image = new WriteableBitmap(800, 600, 96, 96, PixelFormats.Bgra32, null)
                },
                ProjectedRectangle = mainPlacement,
                ProjectedCameraRectangles = [leftPlacement, centerPlacement, rightPlacement],
                DragStartCommand = new RelayCommand(() => { }),
                DragMoveCommand = new RelayCommand(() => { }),
                DragStopCommand = new RelayCommand(() => { })
            };
            FramingAssistantView view = new FramingAssistantView { DataContext = context };
            using TestWindow host = new TestWindow { Width = 1280, Height = 800, Content = view };
            host.Show();
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();
            host.UpdateLayout();

            NINA.WPF.Base.View.ImageView imageView = FindVisualDescendants<NINA.WPF.Base.View.ImageView>(view).Single();
            Canvas placementCanvas = FindVisualDescendants<Canvas>(imageView)
                .Single(canvas => BindingPath(canvas, FrameworkElement.WidthProperty) == "ImageParameter.Image.PixelWidth");
            Rectangle main = FindVisualDescendants<Rectangle>(imageView)
                .Single(rectangle => BindingPath(rectangle, FrameworkElement.WidthProperty) == "ProjectedRectangle.Width");
            Rectangle[] panels = FindVisualDescendants<Rectangle>(imageView)
                .Where(rectangle => rectangle.DataContext is SkyMapCameraRectanglePlacement)
                .OrderBy(rectangle => ((SkyMapCameraRectanglePlacement)rectangle.DataContext).Id)
                .ToArray();
            Border centerDot = FindVisualDescendants<Border>(imageView)
                .Single(border => border.Width == 2.5 && border.Height == 2.5);
            panels.Should().HaveCount(3);
            Point mainCenter = main.TranslatePoint(
                new Point(main.ActualWidth / 2, main.ActualHeight / 2), imageView);
            Point actualCenterDot = centerDot.TranslatePoint(
                new Point(centerDot.ActualWidth / 2, centerDot.ActualHeight / 2), imageView);
            Point leftCenter = panels[0].TranslatePoint(
                new Point(panels[0].ActualWidth / 2, panels[0].ActualHeight / 2), imageView);
            Point centerPanelCenter = panels[1].TranslatePoint(
                new Point(panels[1].ActualWidth / 2, panels[1].ActualHeight / 2), imageView);
            Point rightCenter = panels[2].TranslatePoint(
                new Point(panels[2].ActualWidth / 2, panels[2].ActualHeight / 2), imageView);

            AssertPoint(mainCenter, placementCanvas, imageView, mainPlacement.X + mainPlacement.Width / 2, mainPlacement.Y + mainPlacement.Height / 2);
            AssertPoint(actualCenterDot, placementCanvas, imageView, mainPlacement.X + mainPlacement.Width / 2, mainPlacement.Y + mainPlacement.Height / 2);
            AssertPoint(leftCenter, placementCanvas, imageView, leftPlacement.X + leftPlacement.Width / 2, leftPlacement.Y + leftPlacement.Height / 2);
            AssertPoint(centerPanelCenter, placementCanvas, imageView, centerPlacement.X + centerPlacement.Width / 2, centerPlacement.Y + centerPlacement.Height / 2);
            AssertPoint(rightCenter, placementCanvas, imageView, rightPlacement.X + rightPlacement.Width / 2, rightPlacement.Y + rightPlacement.Height / 2);
            GC.KeepAlive(host);
        }

        private static void UpdatePanelPlacement(SkyMapCameraRectanglePlacement placement, double rotation, double horizontalOffset, double verticalOffset) {
            double radians = AstroUtil.ToRadians(rotation);
            const double mosaicCenterX = 400;
            const double mosaicCenterY = 300;
            double centerX = mosaicCenterX + horizontalOffset * Math.Cos(radians) - verticalOffset * Math.Sin(radians);
            double centerY = mosaicCenterY + horizontalOffset * Math.Sin(radians) + verticalOffset * Math.Cos(radians);
            placement.Update(centerX - placement.Width / 2, centerY - placement.Height / 2, rotation);
        }

        private static void AssertPoint(Point point, Canvas placementCanvas, NINA.WPF.Base.View.ImageView imageView, double x, double y) {
            Point expected = placementCanvas.TranslatePoint(new Point(x, y), imageView);
            point.X.Should().BeApproximately(expected.X, 0.01);
            point.Y.Should().BeApproximately(expected.Y, 0.01);
        }

        [Test]
        public void ObservationTimeEditor_IsOnlyVisibleWhenOfflineSkyMapIsLoaded() {
            EnsureApplicationResources();
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => DateTime.Now, startTimer: false);
            SourceContext context = new SourceContext {
                FramingAssistantSource = SkySurveySource.SKYATLAS,
                TimeContext = timeContext
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

            GroupBox? editor = FindByBindingPath<GroupBox>(view, UIElement.VisibilityProperty, "SkyMapAnnotator.DynamicFoV");

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
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => DateTime.Now, startTimer: false);
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
            IntStepperControl? month = FindByBindingPath<IntStepperControl>(view, IntStepperControl.ValueProperty, "TimeContext.Month");
            IntStepperControl? hour = FindByBindingPath<IntStepperControl>(view, IntStepperControl.ValueProperty, "TimeContext.Hour");
            WrapPanel? dateRow = month?.Parent is StackPanel monthContainer ? monthContainer.Parent as WrapPanel : null;
            WrapPanel? clockRow = hour?.Parent is StackPanel hourContainer ? hourContainer.Parent as WrapPanel : null;
            Button? reset = FindDescendants<Button>(view).SingleOrDefault(button =>
                Equals(button.ToolTip, "Reset observation date and time to the current time"));

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

        [TestCase("TimeContext.Month", 2, 2026, 12, 31, 20, 15, 2027, 1, 31, 20, 15)]
        [TestCase("TimeContext.Day", 2, 2028, 2, 29, 20, 15, 2028, 3, 1, 20, 15)]
        [TestCase("TimeContext.Hour", 2, 2026, 12, 31, 23, 15, 2027, 1, 1, 0, 15)]
        [TestCase("TimeContext.Minute", 2, 2026, 12, 31, 23, 59, 2027, 1, 1, 0, 0)]
        [TestCase("TimeContext.Month", 0, 2026, 1, 31, 20, 15, 2025, 12, 31, 20, 15)]
        [TestCase("TimeContext.Day", 0, 2028, 3, 1, 20, 15, 2028, 2, 29, 20, 15)]
        [TestCase("TimeContext.Hour", 0, 2026, 1, 1, 0, 15, 2025, 12, 31, 23, 15)]
        [TestCase("TimeContext.Minute", 0, 2026, 1, 1, 0, 0, 2025, 12, 31, 23, 59)]
        public void ObservationStepper_CarriesAndBorrowsAcrossBoundaries(
            string valueBindingPath,
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
            IntStepperControl? stepper = FindByBindingPath<IntStepperControl>(view, IntStepperControl.ValueProperty, valueBindingPath);

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

        [Test]
        public void ProjectionSelector_TracksStaticOfflineStaticTransitions() {
            EnsureApplicationResources();
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => DateTime.Now, startTimer: false);
            SourceContext context = new SourceContext {
                FramingAssistantSource = SkySurveySource.SKYATLAS,
                TimeContext = timeContext,
                SkyMapAnnotator = new SkyMapAnnotatorContext {
                    DynamicFoV = false,
                    ProjectionMode = SkyMapProjectionMode.AltAz
                }
            };
            FramingAssistantView view = new FramingAssistantView { DataContext = context };
            Window host = new Window { Width = 1280, Height = 800, Content = view };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();
            NINA.WPF.Base.View.ImageView? imageView = FindDescendants<NINA.WPF.Base.View.ImageView>(view).SingleOrDefault();
            FrameworkElement? header = imageView?.ButtonHeaderContent as FrameworkElement;
            if (header is not null) {
                header.DataContext = context;
                DrainDispatcher();
            }
            ComboBox? selector = header is not null
                ? FindByBindingPath<ComboBox>(header, UIElement.VisibilityProperty, "SkyMapAnnotator.DynamicFoV")
                : null;

            selector.Should().NotBeNull();
            selector!.Visibility.Should().Be(Visibility.Collapsed);
            context.SkyMapAnnotator.DynamicFoV = true;
            DrainDispatcher();
            selector.Visibility.Should().Be(Visibility.Visible);
            selector.SelectedItem.Should().Be(SkyMapProjectionMode.AltAz);
            context.SkyMapAnnotator.DynamicFoV = false;
            DrainDispatcher();
            selector.Visibility.Should().Be(Visibility.Collapsed);
            context.SkyMapAnnotator.ProjectionMode.Should().Be(SkyMapProjectionMode.AltAz);
            GC.KeepAlive(host);
        }

        [Test]
        public void ZoomButtons_AdjustFieldOfViewWhenOfflineSkyMapIsLoaded() {
            EnsureApplicationResources();
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => DateTime.Now, startTimer: false);
            SourceContext context = new SourceContext {
                FramingAssistantSource = SkySurveySource.SKYATLAS,
                SkyMapAnnotator = new SkyMapAnnotatorContext { DynamicFoV = true },
                TimeContext = timeContext
            };
            int zoomInCount = 0;
            int zoomOutCount = 0;
            context.ZoomInCommand = new RelayCommand(() => zoomInCount++, () => context.SkyMapAnnotator.DynamicFoV);
            context.ZoomOutCommand = new RelayCommand(() => zoomOutCount++, () => context.SkyMapAnnotator.DynamicFoV);
            FramingAssistantView view = new FramingAssistantView { DataContext = context };
            Window host = new Window { Width = 1280, Height = 800, Content = view };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();
            NINA.WPF.Base.View.ImageView imageView = FindDescendants<NINA.WPF.Base.View.ImageView>(view).Single();
            Button zoomIn = FindImageViewButton(imageView, 0)!;
            Button zoomOut = FindImageViewButton(imageView, 1)!;
            TextBlock scale = (TextBlock)imageView.FindName("PART_TextblockScale");
            string initialScale = scale.Text;

            zoomIn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            zoomOut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            zoomInCount.Should().Be(1);
            zoomOutCount.Should().Be(1);
            scale.Text.Should().Be(initialScale);
            GC.KeepAlive(host);
        }

        [Test]
        public void ZoomButtons_RetainImageScalingWhenOfflineSkyMapIsNotLoaded() {
            EnsureApplicationResources();
            using FramingAssistantTimeContext timeContext = new FramingAssistantTimeContext(() => DateTime.Now, startTimer: false);
            SourceContext context = new SourceContext {
                FramingAssistantSource = SkySurveySource.NASA,
                SkyMapAnnotator = new SkyMapAnnotatorContext { DynamicFoV = false },
                TimeContext = timeContext
            };
            int commandCount = 0;
            context.ZoomInCommand = new RelayCommand(() => commandCount++, () => context.SkyMapAnnotator.DynamicFoV);
            context.ZoomOutCommand = new RelayCommand(() => commandCount++, () => context.SkyMapAnnotator.DynamicFoV);
            FramingAssistantView view = new FramingAssistantView { DataContext = context };
            Window host = new Window { Width = 1280, Height = 800, Content = view };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();
            DrainDispatcher();
            NINA.WPF.Base.View.ImageView imageView = FindDescendants<NINA.WPF.Base.View.ImageView>(view).Single();
            Button zoomIn = FindImageViewButton(imageView, 0)!;
            TextBlock scale = (TextBlock)imageView.FindName("PART_TextblockScale");
            string initialScale = scale.Text;

            zoomIn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            commandCount.Should().Be(0);
            scale.Text.Should().NotBe(initialScale);
            GC.KeepAlive(host);
        }

        [TestCase(10, 2, 0, 20, 12)]
        [TestCase(10, 0, 0, 20, 8)]
        [TestCase(20, 2, 0, 20, 20)]
        [TestCase(0, 0, 0, 20, 0)]
        public void IntStepper_UnhandledRequestsRetainBoundedBehavior(
            int initialValue,
            int buttonColumn,
            int minimum,
            int maximum,
            int expected) {
            EnsureApplicationResources();
            IntStepperControl stepper = new IntStepperControl {
                MinValue = minimum,
                MaxValue = maximum,
                StepSize = 2,
                Value = initialValue
            };
            Window host = new Window { Content = stepper };
            host.Measure(new Size(200, 100));
            host.Arrange(new Rect(0, 0, 200, 100));
            host.UpdateLayout();
            stepper.ApplyTemplate();

            Button? button = FindStepperButton(stepper, buttonColumn);
            button.Should().NotBeNull();
            button!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            stepper.Value.Should().Be(expected);
            GC.KeepAlive(host);
        }

        private static T? FindByBindingPath<T>(DependencyObject parent, DependencyProperty property, string path) where T : DependencyObject {
            return FindDescendants<T>(parent).SingleOrDefault(element =>
                BindingOperations.GetBinding(element, property)?.Path?.Path == path);
        }

        private static System.Collections.Generic.IEnumerable<T> FindDescendants<T>(DependencyObject parent) where T : DependencyObject {
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

        private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject parent) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) {
                    yield return result;
                }
                foreach (T descendant in FindVisualDescendants<T>(child)) {
                    yield return descendant;
                }
            }
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

        private static Button? FindImageViewButton(DependencyObject parent, int column) {
            return FindDescendants<Button>(parent).FirstOrDefault(button => Grid.GetColumn(button) == column);
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
                "/NINA.WPF.Base;component/Resources/Styles/Hyperlink.xaml",
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

        private static string BindingPath(DependencyObject target, DependencyProperty property) {
            return BindingOperations.GetBinding(target, property)?.Path?.Path;
        }

        private sealed class SourceContext {
            public SkySurveySource FramingAssistantSource { get; set; }
            public SkyMapAnnotatorContext SkyMapAnnotator { get; set; } = new SkyMapAnnotatorContext();
            public FramingAssistantTimeContext TimeContext { get; set; } = null!;
            public ICommand ZoomInCommand { get; set; } = null!;
            public ICommand ZoomOutCommand { get; set; } = null!;
            public ICommand DragStartCommand { get; set; } = null!;
            public ICommand DragMoveCommand { get; set; } = null!;
            public ICommand DragStopCommand { get; set; } = null!;
            public SkySurveyImage ImageParameter { get; set; } = null!;
            public SkyMapCameraRectanglePlacement ProjectedRectangle { get; set; } = null!;
            public IReadOnlyList<SkyMapCameraRectanglePlacement> ProjectedCameraRectangles { get; set; } = [];
            public int FontSize { get; set; } = 20;
            public double Opacity { get; set; } = 0.5;
            public double InverseRectangleRotation { get; set; }
        }

        private sealed class TestWindow : Window, IDisposable {

            public void Dispose() {
                Close();
            }
        }

        private sealed class SkyMapAnnotatorContext : INotifyPropertyChanged {
            private bool dynamicFoV;
            private SkyMapProjectionMode projectionMode;

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

            public SkyMapProjectionMode ProjectionMode {
                get => projectionMode;
                set {
                    if (projectionMode == value) {
                        return;
                    }

                    projectionMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProjectionMode)));
                }
            }
        }
    }
}
