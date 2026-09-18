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
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using Expression = NINA.Sequencer.Logic.Expression;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NUnit.Framework;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SmartExposureHistoryTest {
        private ResourceDictionary previousResources = null!;
        private Window previousWindow = null!;
        private Window window = null!;
        private NINA.Profile.Profile profile = null!;
        private SmartExposure item = null!;
        private CameraInfo cameraInfo = null!;
        private IProfileService profiles = null!;
        private SequenceEditHistory history = null!;
        private SequenceEditBehavior behavior = null!;
        private FrameworkElement view = null!;
        private NINA.Profile.ProfileService sharedProfiles = null!;
        private IProfile previousSharedProfile = null!;

        [SetUp]
        public void SetUp() {
            var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            previousResources = app.Resources;
            previousWindow = app.MainWindow;
            app.Resources = new ResourceDictionary();
            profile = new NINA.Profile.Profile();
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("Red", 0, 0));
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("Green", 0, 1));
            profiles = Mock.Of<IProfileService>(x => x.ActiveProfile == profile);
            app.Resources["ProfileService"] = profiles;
            var shared = new NINA.WPF.Base.Utility.SharedResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml", UriKind.Relative) };
            sharedProfiles = (NINA.Profile.ProfileService)shared["ProfileService"];
            previousSharedProfile = sharedProfiles.ActiveProfile;
            typeof(NINA.Profile.ProfileService).GetProperty(nameof(IProfileService.ActiveProfile))!.SetValue(sharedProfiles, profile);
            foreach (string resource in new[] { "SVGDictionary", "Brushes", "Converters" }) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/NINA.WPF.Base;component/Resources/StaticResources/{resource}.xaml", UriKind.Relative) });
            }
            window = new Window { Width = 1400, Height = 300, Left = -32000, Top = -32000, ShowActivated = false, ShowInTaskbar = false };
            app.MainWindow = window;
            NameScope.SetNameScope(window, new NameScope());
            window.RegisterName("RootGrid", new Grid());
            cameraInfo = new CameraInfo {
                Connected = true, CanSetGain = true, GainMin = 0, GainMax = 300,
                DefaultGain = 100, Gains = new List<int> { 100, 200 }
            };
            item = new SmartExposure(profiles,
                Mock.Of<ICameraMediator>(x => x.GetInfo() == cameraInfo),
                Mock.Of<IImagingMediator>(), Mock.Of<IImageSaveMediator>(),
                Mock.Of<IImageHistoryVM>(x => x.ImageHistory == new List<ImageHistoryPoint>()),
                Mock.Of<IFilterWheelMediator>(x => x.GetInfo() == new FilterWheelInfo { Connected = true }),
                Mock.Of<IGuiderMediator>(x => x.GetInfo() == new GuiderInfo { Connected = true }),
                Mock.Of<ISafetyMonitorMediator>());
            item.GetSwitchFilter().ComboBoxText = "Red";
            var root = new SequenceRootContainer();
            root.Add(item);
            history = new SequenceEditHistory(root);
            var templates = new NINA.Sequencer.SequenceItem.Imaging.Datatemplates();
            app.Resources.MergedDictionaries.Add(templates);
            view = new ContentControl { Content = item, ContentTemplate = (DataTemplate)templates[new DataTemplateKey(typeof(SmartExposure))] };
            view.DataContext = item;
            behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(view);
            window.Content = view;
            window.Show();
            Drain();
        }

        [TearDown]
        public void TearDown() {
            behavior?.Detach();
            history?.Dispose();
            window?.Close();
            Application.Current.MainWindow = previousWindow;
            typeof(NINA.Profile.ProfileService).GetProperty(nameof(IProfileService.ActiveProfile))!.SetValue(sharedProfiles, previousSharedProfile);
            Application.Current.Resources = previousResources;
            profile?.Dispose();
        }

        [TestCase("Dither")]
        [TestCase("Iterations")]
        [TestCase("Exposure")]
        public void ExpressionInSmartExposure_RecordsOneEditAndReplaysBothWays(string field) {
            Expression expression = field switch {
                "Dither" => item.GetDitherAfterExposures().AfterExposuresExpression,
                "Iterations" => item.IterationsExpression,
                _ => item.GetTakeExposure().ExposureTimeExpression
            };
            string before = expression.Definition;
            var box = Descendants<TextBox>(view).Single(x => ReferenceEquals(x.GetBindingExpression(TextBox.TextProperty)?.ResolvedSource, expression));
            box.RaiseEvent(new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice,
                new TextComposition(InputManager.Current, box, "7")) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
            box.Text = "7";
            box.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            behavior.Commit();
            expression.Definition.Should().Be("7");
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            expression.Definition.Should().Be(before);
            box.Text.Should().Be(before);
            history.Redo().Should().BeTrue();
            expression.Definition.Should().Be("7");
            box.Text.Should().Be("7");
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void FilterDropdown_RecordsSelectionAndReplaysBothWays(bool standalone, bool keyboard) {
            if (standalone) ShowStandaloneFilter();
            var filter = item.GetSwitchFilter();
            var combo = Descendants<ComboBox>(view).Single(x => ReferenceEquals(x.GetBindingExpression(ComboBox.TextProperty)?.ResolvedSource, filter));
            combo.Items.Count.Should().Be(3);
            if (keyboard) {
                var textBox = (TextBox)combo.Template.FindName("PART_EditableTextBox", combo);
                RaiseKey(textBox, Key.Down, Keyboard.PreviewKeyDownEvent);
            } else {
                combo.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
            }
            combo.IsDropDownOpen = true;
            combo.SelectedIndex = 2;
            combo.IsDropDownOpen = false;
            combo.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            Drain();
            filter.ComboBoxText.Should().Be("Green");
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            filter.ComboBoxText.Should().Be("Red");
            filter.XfilterExpression.Definition.Should().Be("Red");
            history.Redo().Should().BeTrue();
            filter.ComboBoxText.Should().Be("Green");
            filter.XfilterExpression.Definition.Should().Be("Green");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EditableFilterText_GroupsTypingUntilCommit(bool standalone) {
            if (standalone) ShowStandaloneFilter();
            var filter = item.GetSwitchFilter();
            var combo = Descendants<ComboBox>(view).Single(x => ReferenceEquals(x.GetBindingExpression(ComboBox.TextProperty)?.ResolvedSource, filter));
            var box = (TextBox)combo.Template.FindName("PART_EditableTextBox", combo);
            foreach (string text in new[] { "G", "Green" }) {
                box.RaiseEvent(new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice,
                    new TextComposition(InputManager.Current, box, text)) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
                box.Text = text;
                RaiseKey(box, Key.E, Keyboard.PreviewKeyUpEvent);
                Drain();
                history.Position.Should().Be(0, "typing stays in one editing session until commit");
            }
            behavior.Commit();
            filter.ComboBoxText.Should().Be("Green");
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            filter.ComboBoxText.Should().Be("Red");
            history.Redo().Should().BeTrue();
            filter.ComboBoxText.Should().Be("Green");
        }

        [Test]
        public void BackgroundUpdatesWhileFilterHasFocus_DoNotCreateHistory() {
            var filter = item.GetSwitchFilter();
            var combo = Descendants<ComboBox>(view).Single(x => ReferenceEquals(x.GetBindingExpression(ComboBox.TextProperty)?.ResolvedSource, filter));
            combo.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, view, combo) { RoutedEvent = Keyboard.GotKeyboardFocusEvent });
            filter.ComboBoxText = "Green";
            item.GetDitherAfterExposures().AfterExposuresDefinition = "9";
            item.GetLoopCondition().CompletedIterations = 3;
            Drain();
            behavior.Commit();
            history.Position.Should().Be(0);
        }

        [TestCase("Smart", "", false)]
        [TestCase("Smart", "50+50", true)]
        [TestCase("Single", "", true)]
        [TestCase("Single", "50+50", false)]
        [TestCase("Flat", "", false)]
        [TestCase("Flat", "50+50", true)]
        public void GainDropdown_RecordsOneEditAndRestoresDefinitionAndCameraDefault(string instruction, string before, bool keyboard) {
            TakeExposure exposure = ShowGainInstruction(instruction);
            exposure.GainExpression.Definition = before;
            Drain();
            var combo = Descendants<ComboBox>(view).Single(x =>
                BindingOperations.GetMultiBindingExpression(x, Selector.SelectedValueProperty)?.BindingExpressions
                    .OfType<BindingExpression>().Any(b => ReferenceEquals(b.ResolvedSource, exposure) && b.ResolvedSourcePropertyName == nameof(TakeExposure.Gain)) == true);
            combo.Items.Count.Should().Be(3);
            combo.IsVisible.Should().BeTrue();
            SelectGain(combo, 2, keyboard);
            exposure.Gain.Should().Be(200);
            exposure.GainExpression.Definition.Should().Be("200");
            history.Position.Should().Be(1);

            cameraInfo.DefaultGain = 200;
            exposure.Validate();
            Drain();
            history.Position.Should().Be(1, "camera refreshes are not sequence edits");
            history.Undo().Should().BeTrue();
            exposure.GainExpression.Definition.Should().Be(before);
            exposure.Gain.Should().Be(before.Length == 0 ? cameraInfo.DefaultGain : 100);
            combo.SelectedValue.Should().Be(before.Length == 0 ? "200" : "100");
            history.Redo().Should().BeTrue();
            exposure.Gain.Should().Be(200);
            exposure.GainExpression.Definition.Should().Be("200");
            combo.SelectedValue.Should().Be("200");

            SelectGain(combo, 0, keyboard);
            exposure.GainExpression.Definition.Should().BeEmpty();
            exposure.Gain.Should().Be(cameraInfo.DefaultGain);
            history.Position.Should().Be(2);
            history.Undo().Should().BeTrue();
            exposure.Gain.Should().Be(200);
            exposure.GainExpression.Definition.Should().Be("200");
            combo.SelectedValue.Should().Be("200");
            history.Redo().Should().BeTrue();
            exposure.GainExpression.Definition.Should().BeEmpty();
            exposure.Gain.Should().Be(cameraInfo.DefaultGain);
            combo.SelectedValue.Should().Be("200");
            cameraInfo.DefaultGain.Should().Be(200);
        }

        private TakeExposure ShowGainInstruction(string instruction) {
            if (instruction == "Smart") return item.GetTakeExposure();
            if (instruction == "Single") {
                var host = (ContentControl)view;
                host.ContentTemplate = (DataTemplate)Application.Current.Resources[new DataTemplateKey(typeof(TakeExposure))];
                host.Content = item.GetTakeExposure();
                Drain();
                return item.GetTakeExposure();
            }
            var flat = new AutoExposureFlat(profiles, Mock.Of<ICameraMediator>(x => x.GetInfo() == cameraInfo),
                Mock.Of<IImagingMediator>(), Mock.Of<IImageSaveMediator>(),
                Mock.Of<IImageHistoryVM>(x => x.ImageHistory == new List<ImageHistoryPoint>()),
                Mock.Of<IFilterWheelMediator>(x => x.GetInfo() == new FilterWheelInfo { Connected = true }),
                Mock.Of<IFlatDeviceMediator>(x => x.GetInfo() == new FlatDeviceInfo { Connected = true }));
            history.Root.Add(flat);
            var templates = new NINA.Sequencer.SequenceItem.FlatDevice.Datatemplates();
            Application.Current.Resources.MergedDictionaries.Add(templates);
            var flatHost = (ContentControl)view;
            flatHost.ContentTemplate = (DataTemplate)templates[new DataTemplateKey(typeof(AutoExposureFlat))];
            flatHost.Content = flat;
            Drain();
            history.Position.Should().Be(0);
            return flat.GetExposureItem();
        }

        private static void SelectGain(ComboBox combo, int index, bool keyboard) {
            if (keyboard) RaiseKey(combo, index == 0 ? Key.Up : Key.Down, Keyboard.PreviewKeyDownEvent);
            else combo.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
            combo.IsDropDownOpen = true;
            combo.SelectedIndex = index;
            combo.IsDropDownOpen = false;
            Drain();
        }

        private void ShowStandaloneFilter() {
            var templates = new NINA.Sequencer.SequenceItem.FilterWheel.Datatemplates();
            Application.Current.Resources.MergedDictionaries.Add(templates);
            var host = (ContentControl)view;
            host.ContentTemplate = (DataTemplate)templates[new DataTemplateKey(item.GetSwitchFilter().GetType())];
            host.Content = item.GetSwitchFilter();
            Drain();
        }

        private static void RaiseKey(UIElement element, Key key, RoutedEvent routedEvent) => element.RaiseEvent(
            new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(element), 0, key) { RoutedEvent = routedEvent });

        private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject {
            if (parent is T match) yield return match;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                foreach (T child in Descendants<T>(VisualTreeHelper.GetChild(parent, i))) yield return child;
            }
        }

        private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}