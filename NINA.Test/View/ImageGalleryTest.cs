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
using NINA.Profile;
using NINA.Profile.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfImage = System.Windows.Controls.Image;

namespace NINA.Test.View {

    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable, SingleThreaded]
    public class ImageGalleryTest {
        private Window? host;
        private object? previousProfileService;
        private bool hadProfileService;
        private readonly List<ResourceDictionary> ownedDictionaries = new();

        [SetUp]
        public void PreserveApplicationResources() {
            var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            hadProfileService = app.Resources.Contains("ProfileService");
            previousProfileService = hadProfileService ? app.Resources["ProfileService"] : null;
        }

        [TearDown]
        public void CloseHost() {
            host?.Close();
            host = null;

            foreach (var dictionary in ownedDictionaries) {
                Application.Current.Resources.MergedDictionaries.Remove(dictionary);
            }

            ownedDictionaries.Clear();

            if (hadProfileService) {
                Application.Current.Resources["ProfileService"] = previousProfileService;
            } else {
                Application.Current.Resources.Remove("ProfileService");
            }
        }

        [Test]
        public void CompiledTemplate_ShowsTheWholeSelectedImageWithoutScrollbars() {
            var images = CreateImages(3);
            var gallery = CreateGallery(images);
            var image = Part<WpfImage>(gallery, "PART_Image");

            gallery.SelectedIndex.Should().Be(0);
            image.Source.Should().BeSameAs(images[0]);
            image.Stretch.Should().Be(Stretch.Uniform);
            image.ActualWidth.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(gallery.ActualWidth);
            image.ActualHeight.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(gallery.ActualHeight);
            Descendants<ScrollViewer>(gallery).Should().BeEmpty();
            Part<ItemsPresenter>(gallery, "PART_Indicators").Visibility.Should().Be(Visibility.Visible);

            SelectIndicator(gallery, 2);
            Layout(gallery);
            image.Source.Should().BeSameAs(images[2]);
            gallery.SelectedItem.Should().BeSameAs(images[2]);
        }

        [Test]
        public void ArrowButtons_WrapInBothDirectionsAndVisitEveryImage() {
            var images = CreateImages(3);
            var gallery = CreateGallery(images);
            var previous = Part<Button>(gallery, "PART_PreviousButton");
            var next = Part<Button>(gallery, "PART_NextButton");

            Invoke(previous);
            gallery.SelectedIndex.Should().Be(2);
            Invoke(next);
            gallery.SelectedIndex.Should().Be(0);
            Invoke(next);
            gallery.SelectedIndex.Should().Be(1);
            Invoke(next);
            gallery.SelectedIndex.Should().Be(2);
            Invoke(previous);
            gallery.SelectedIndex.Should().Be(1);
            AutomationProperties.GetName(previous).Should().Be(Loc.Instance["LblImageGalleryPrevious"]);
            AutomationProperties.GetName(next).Should().Be(Loc.Instance["LblImageGalleryNext"]);
        }

        [Test]
        public void Indicators_ExposeSelectionAutomationAndFilledVersusHollowShapes() {
            var gallery = CreateGallery(CreateImages(3));
            var first = Indicator(gallery, 0);
            var last = Indicator(gallery, 2);
            var firstShape = (Ellipse)first.Template.FindName("Indicator", first);
            var lastShape = (Ellipse)last.Template.FindName("Indicator", last);

            ((SolidColorBrush)firstShape.Fill).Color.Should().NotBe(Colors.Transparent);
            ((SolidColorBrush)lastShape.Fill).Color.Should().Be(Colors.Transparent);
            AutomationProperties.GetName(last).Should().Be(Loc.Instance["LblImageGalleryImage"] + " 3");
            SelectIndicator(gallery, 2);
            Layout(gallery);
            first.IsSelected.Should().BeFalse();
            last.IsSelected.Should().BeTrue();
            ((SolidColorBrush)firstShape.Fill).Color.Should().Be(Colors.Transparent);
            ((SolidColorBrush)lastShape.Fill).Color.Should().NotBe(Colors.Transparent);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void EmptyAndSingleImage_HideAllNavigation(int count) {
            var images = CreateImages(count);
            var gallery = CreateGallery(images);

            gallery.SelectedIndex.Should().Be(count - 1);
            Part<WpfImage>(gallery, "PART_Image").Source.Should().Be(count == 0 ? null : images[0]);
            Part<Button>(gallery, "PART_PreviousButton").Visibility.Should().Be(Visibility.Collapsed);
            Part<Button>(gallery, "PART_NextButton").Visibility.Should().Be(Visibility.Collapsed);
            Part<ItemsPresenter>(gallery, "PART_Indicators").Visibility.Should().Be(Visibility.Collapsed);
            NavigationCommands.PreviousPage.CanExecute(null, gallery).Should().BeFalse();
            NavigationCommands.NextPage.CanExecute(null, gallery).Should().BeFalse();
        }

        [Test]
        public void SourceReplacementAndReset_SelectTheFirstImage() {
            var images = CreateImages(3);
            var gallery = CreateGallery(images);
            gallery.SelectedIndex = 2;
            var replacement = CreateImages(4);
            gallery.ItemsSource = replacement;
            Layout(gallery);

            gallery.SelectedIndex.Should().Be(0);
            Part<WpfImage>(gallery, "PART_Image").Source.Should().BeSameAs(replacement[0]);
            replacement.Clear();
            Layout(gallery);
            gallery.SelectedIndex.Should().Be(-1);
            Part<WpfImage>(gallery, "PART_Image").Source.Should().BeNull();
            replacement.Add(images[1]);
            Layout(gallery);
            gallery.SelectedItem.Should().BeSameAs(images[1]);
        }

        [Test]
        public void CollectionChanges_PreserveTheSelectedImageAndRepairRemovedSelection() {
            var images = CreateImages(3);
            var gallery = CreateGallery(images);
            gallery.SelectedIndex = 1;
            var selected = gallery.SelectedItem;
            images.Insert(0, CreateImages(1)[0]);
            Layout(gallery);
            gallery.SelectedItem.Should().BeSameAs(selected);
            gallery.SelectedIndex.Should().Be(2);
            AutomationProperties.GetName(Indicator(gallery, 2)).Should().Be(Loc.Instance["LblImageGalleryImage"] + " 3");

            images.RemoveAt(0);
            Layout(gallery);
            gallery.SelectedItem.Should().BeSameAs(selected);
            gallery.SelectedIndex.Should().Be(1);
            images.Remove((ImageSource)selected);
            Layout(gallery);
            gallery.SelectedIndex.Should().Be(1);
            Part<WpfImage>(gallery, "PART_Image").Source.Should().BeSameAs(images[1]);
            images.RemoveAt(1);
            Layout(gallery);
            gallery.SelectedItem.Should().BeSameAs(images[0]);
            Part<ItemsPresenter>(gallery, "PART_Indicators").Visibility.Should().Be(Visibility.Collapsed);
            images.Add(CreateImages(1)[0]);
            Layout(gallery);
            Part<ItemsPresenter>(gallery, "PART_Indicators").Visibility.Should().Be(Visibility.Visible);
            Part<Button>(gallery, "PART_PreviousButton").IsEnabled.Should().BeTrue();
            Part<Button>(gallery, "PART_NextButton").IsEnabled.Should().BeTrue();
        }

        [Test]
        public void FocusedGallery_LeftAndRightKeysWrapAndShowIndicatorFocus() {
            var gallery = CreateGallery(CreateImages(3));
            host = new Window {
                Content = gallery, Width = 520, Height = 340,
                Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false
            };
            host.Show();
            Layout(gallery);
            gallery.Focus().Should().BeTrue();
            SendKey(gallery, Key.Left);
            gallery.SelectedIndex.Should().Be(2);
            var selected = Indicator(gallery, 2);
            selected.IsKeyboardFocusWithin.Should().BeTrue();
            ((Rectangle)selected.Template.FindName("FocusOutline", selected)).Visibility.Should().Be(Visibility.Visible);
            SendKey(selected, Key.Right);
            gallery.SelectedIndex.Should().Be(0);
            SendKey(Indicator(gallery, 0), Key.Right);
            gallery.SelectedIndex.Should().Be(1);
        }

        [TestCase("Light")]
        [TestCase("Dark Nebula")]
        [TestCase("High Contrast")]
        public void Navigation_TracksThemeChangesWithoutRecreatingTheGallery(string schema) {
            var profile = new NINA.Profile.Profile();
            var gallery = CreateGallery(CreateImages(3), profile);
            profile.ColorSchemaSettings.ColorSchema = profile.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == schema);
            Layout(gallery);

            foreach (var button in new[] { Part<Button>(gallery, "PART_PreviousButton"), Part<Button>(gallery, "PART_NextButton") }) {
                var path = (System.Windows.Shapes.Path)button.Content;
                ((SolidColorBrush)path.Fill).Color.Should().Be(((SolidColorBrush)button.Foreground).Color,
                    "navigation icons must follow the native button's foreground, including the Light theme");
                ((SolidColorBrush)button.Background).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.BackgroundColor);
            }

            var dot = Indicator(gallery, 0);
            var shape = (Ellipse)dot.Template.FindName("Indicator", dot);
            ((SolidColorBrush)shape.Fill).Color.Should().Be(profile.ColorSchemaSettings.ColorSchema.PrimaryColor);
        }

        [Test]
        public void ProfileSwitch_UpdatesNavigationColorsAndPreservesSelection() {
            using var original = new NINA.Profile.Profile();
            using var replacement = new NINA.Profile.Profile();
            original.ColorSchemaSettings.ColorSchema = original.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == "Light");
            replacement.ColorSchemaSettings.ColorSchema = replacement.ColorSchemaSettings.ColorSchemas.Items.Single(item => item.Name == "Dark Nebula");
            NINA.Profile.Interfaces.IProfile active = original;
            var profiles = new Mock<IProfileService>();
            var notifications = profiles.As<INotifyPropertyChanged>();
            profiles.SetupGet(service => service.ActiveProfile).Returns(() => active);
            var images = CreateImages(3);
            var gallery = CreateGallery(images, original, profiles.Object);
            gallery.SelectedIndex = 1;
            var arrow = (System.Windows.Shapes.Path)Part<Button>(gallery, "PART_NextButton").Content;
            ((SolidColorBrush)arrow.Fill).Color.Should().Be(original.ColorSchemaSettings.ColorSchema.PrimaryColor);

            active = replacement;
            notifications.Raise(service => service.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProfileService.ActiveProfile)));
            profiles.Raise(service => service.ProfileChanged += null, new ProfileChangedEventArgs(original, replacement));
            Layout(gallery);

            ((SolidColorBrush)arrow.Fill).Color.Should().Be(replacement.ColorSchemaSettings.ColorSchema.PrimaryColor);
            ((SolidColorBrush)Part<Button>(gallery, "PART_NextButton").Background).Color
                .Should().Be(replacement.ColorSchemaSettings.ColorSchema.BackgroundColor);
            Part<WpfImage>(gallery, "PART_Image").Source.Should().BeSameAs(images[1]);
            gallery.SelectedIndex.Should().Be(1);
        }

        private ImageGallery CreateGallery(ObservableCollection<ImageSource> images, NINA.Profile.Profile? profile = null, IProfileService? profiles = null) {
            var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            profile ??= new NINA.Profile.Profile();
            var gallery = new ImageGallery { ItemsSource = images, Width = 500, Height = 300 };
            var profileService = profiles ?? Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            app.Resources["ProfileService"] = profileService;
            gallery.Resources["ProfileService"] = profileService;
            string[] sources = [
                "/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/SVGDictionary.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Button.xaml",
                "/NINA.CustomControlLibrary;component/Themes/Generic.xaml"
            ];
            foreach (string source in sources) {
                var dictionary = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
                app.Resources.MergedDictionaries.Add(dictionary);
                ownedDictionaries.Add(dictionary);
            }

            gallery.Style = (Style)gallery.FindResource(typeof(ImageGallery));
            Layout(gallery);
            return gallery;
        }

        private static ObservableCollection<ImageSource> CreateImages(int count) {
            var images = new ObservableCollection<ImageSource>();
            for (int index = 0; index < count; index++) {
                var image = new DrawingImage(new GeometryDrawing(Brushes.White, null,
                    new RectangleGeometry(new Rect(0, 0, index % 2 == 0 ? 1600 : 200, index % 2 == 0 ? 400 : 1600))));
                image.Freeze();
                images.Add(image);
            }

            return images;
        }

        private static T Part<T>(ImageGallery gallery, string name) where T : FrameworkElement {
            return (T)gallery.Template.FindName(name, gallery);
        }

        private static ListBoxItem Indicator(ImageGallery gallery, int index) {
            return (ListBoxItem)gallery.ItemContainerGenerator.ContainerFromIndex(index);
        }

        private static void SelectIndicator(ImageGallery gallery, int index) {
            var peer = new ListBoxAutomationPeer(gallery);
            var child = peer.GetChildren()[index];
            ((ISelectionItemProvider)child.GetPattern(PatternInterface.SelectionItem)).Select();
        }

        private static void Invoke(Button button) {
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            button.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static void SendKey(UIElement target, Key key) {
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), 0, key) {
                RoutedEvent = Keyboard.PreviewKeyDownEvent
            });
        }

        private static void Layout(FrameworkElement element) {
            element.ApplyTemplate();
            element.Measure(new Size(500, 300));
            element.Arrange(new Rect(0, 0, 500, 300));
            element.UpdateLayout();
            element.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject {
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match) {
                    yield return match;
                }

                foreach (var descendant in Descendants<T>(child)) {
                    yield return descendant;
                }
            }
        }
    }
}
