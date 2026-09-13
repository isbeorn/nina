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
using NINA.Astrometry.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.CustomControlLibrary;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.View.SimpleSequencer;
using NINA.ViewModel;
using NINA.ViewModel.Sequencer.SimpleSequence;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Expression = NINA.Sequencer.Logic.Expression;

namespace NINA.Test.SimpleSequencer {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class SimpleDSOContainerViewTest {
        private Window window = null!;
        private Window? previousMainWindow;
        private Mock<IProfileService> profile = null!;
        private Mock<ICameraMediator> camera = null!;
        private Mock<ISequencerFactory> factory = null!;
        private CameraInfo cameraInfo = null!;
        private SimpleDSOContainer target = null!;
        private SimpleExposure row = null!;
        private TakeExposure exposure = null!;
        private SimpleDSOContainerView view = null!;
        private DataGrid grid = null!;
        private readonly List<Exception> dispatcherExceptions = [];

        [SetUp]
        public void SetUp() {
            SimpleSequenceViewTest.EnsureApplicationResources();
            previousMainWindow = Application.Current.MainWindow;
            Application.Current.DispatcherUnhandledException += OnDispatcherException;
            dispatcherExceptions.Clear();
            window = new Window { Width = 1600, Height = 1100, ShowInTaskbar = false };
            Application.Current.MainWindow = window;
            window.Show();

            profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profile.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
            profile.SetupGet(x => x.ActiveProfile.SequenceSettings.EstimatedDownloadTime).Returns(TimeSpan.Zero);
            profile.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.WorkDirectory);
            cameraInfo = new CameraInfo {
                Connected = true,
                CanSetGain = true,
                GainMin = 0,
                GainMax = 800,
                DefaultGain = 200,
                CanSetOffset = true,
                OffsetMin = 0,
                OffsetMax = 100,
                DefaultOffset = -1,
                Gains = []
            };
            camera = new Mock<ICameraMediator>();
            camera.Setup(x => x.GetInfo()).Returns(cameraInfo);
            factory = new Mock<ISequencerFactory>();
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            factory.Setup(x => x.GetCondition<LoopCondition>()).Returns(() => new LoopCondition { Iterations = 5 });
            factory.Setup(x => x.GetItem<SwitchFilter>()).Returns(() => new SwitchFilter(profile.Object, Mock.Of<IFilterWheelMediator>()));
            factory.Setup(x => x.GetItem<TakeExposure>()).Returns(() => new TakeExposure(
                profile.Object, camera.Object, Mock.Of<IImagingMediator>(), Mock.Of<IImageSaveMediator>(), Mock.Of<IImageHistoryVM>()));
            target = new SimpleDSOContainer(factory.Object, profile.Object, camera.Object,
                Mock.Of<INighttimeCalculator>(), Mock.Of<IFramingAssistantVM>(), Mock.Of<IApplicationMediator>(), Mock.Of<IPlanetariumFactory>());
            row = new SimpleExposure(factory.Object);
            target.Add(row);
            exposure = (TakeExposure)row.GetTakeExposure();
            exposure.ExposureTime = 300;
            exposure.Binning = new BinningMode(1, 1);
            exposure.Gain = 400;
            exposure.Offset = -1;
            exposure.Validate();
            view = new SimpleDSOContainerView { DataContext = target };
            grid = ((Grid)view.Content).Children.OfType<DataGrid>().Single();
        }

        [TearDown]
        public void TearDown() {
            window.Content = null;
            view.DataContext = null;
            window.Close();
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Application.Current.MainWindow = previousMainWindow;
            Application.Current.DispatcherUnhandledException -= OnDispatcherException;
            target.Detach();
        }

        // The simulator has no discrete Gains, so it never renders the faulty dropdown cell from issue #380.
        [TestCase(false, -1)]
        [TestCase(true, -1)]
        [TestCase(true, 20)]
        public void GainColumns_RenderTheCameraCapabilityWithoutExceptions(bool discreteGains, int offset) {
            cameraInfo.Gains = discreteGains ? [0, 200, 400, 800] : [];
            exposure.Offset = offset;
            ShowView();

            GainColumn(true).Visibility.Should().Be(discreteGains ? Visibility.Visible : Visibility.Collapsed);
            GainColumn(false).Visibility.Should().Be(discreteGains ? Visibility.Collapsed : Visibility.Visible);
            Display(GainColumn(discreteGains)).Text.Should().Be("400");
            Display(OffsetColumn()).Text.Should().Be(offset == -1 ? "(-1)" : "20");
        }

        [Test]
        public void GainColumns_SwitchBothWaysWhenCameraCapabilitiesChange() {
            ShowView();
            GainColumn(true).Visibility.Should().Be(Visibility.Collapsed);
            GainColumn(false).Visibility.Should().Be(Visibility.Visible);

            cameraInfo.Gains = [0, 200, 400, 800];
            Render();
            GainColumn(true).Visibility.Should().Be(Visibility.Visible);
            GainColumn(false).Visibility.Should().Be(Visibility.Collapsed);
            Display(GainColumn(true)).Text.Should().Be("400");

            cameraInfo.Gains = [];
            Render();
            GainColumn(true).Visibility.Should().Be(Visibility.Collapsed);
            GainColumn(false).Visibility.Should().Be(Visibility.Visible);
            Display(GainColumn(false)).Text.Should().Be("400");
        }

        [Test]
        public void DiscreteGainEditor_SelectsNumbersAndCameraDefaultWithoutReplacingExpression() {
            cameraInfo.Gains = [0, 200, 400, 800];
            // Load the actual editing template independently so a broken display cannot hide a broken editor.
            ComboBox editor = (ComboBox)GainColumn(true).CellEditingTemplate.LoadContent();
            editor.DataContext = row;
            window.Content = editor;
            Render();
            Expression originalExpression = exposure.GainExpression;
            editor.SelectedValue.Should().Be("400");

            foreach (int gain in new[] { 800, 0, 400, -1, 200, -1, 800, 0, -1 }) {
                editor.SetCurrentValue(Selector.SelectedValueProperty, gain == -1 ? "(200)" : gain.ToString());
                Render();
                exposure.Gain.Should().Be(gain == -1 ? 200 : gain);
                exposure.GainExpression.Should().BeSameAs(originalExpression);
                // The existing Gain setter treats the current default value and -1 as camera-default mode.
                exposure.GainExpression.Definition.Should().Be(gain is -1 or 200 ? string.Empty : gain.ToString());
                cameraInfo.DefaultGain.Should().Be(200);
            }

            editor.SetCurrentValue(Selector.SelectedValueProperty, null);
            Render();
            exposure.GainExpression.Should().BeSameAs(originalExpression);
            exposure.GainExpression.Definition.Should().BeEmpty();
        }

        [Test]
        public void DiscreteGainDisplay_TracksValuesDefaultsAndExpressionReplacement() {
            cameraInfo.Gains = [0, 200, 400, 800];
            ShowView();
            TextBlock display = Display(GainColumn(true));
            foreach (int gain in new[] { 800, 0, 400 }) {
                exposure.Gain = gain;
                Render();
                display.Text.Should().Be(gain.ToString());
            }

            // Equal numeric values still need a new label when switching between an explicit value and automatic gain.
            exposure.GainExpression.Default = 400;
            exposure.Gain = -1;
            Render();
            display.Text.Should().Be("(400)");
            cameraInfo.Connected = false;
            exposure.Validate();
            Render();
            display.Text.Should().Be($"({Loc.Instance["LblCamera"]})");
            cameraInfo.Connected = true;
            cameraInfo.DefaultGain = 400;
            exposure.Validate();
            Render();
            display.Text.Should().Be("(400)");
            exposure.GainExpression.Definition = "400";
            Render();
            display.Text.Should().Be("400");

            Expression replacement = new Expression(exposure.GainExpression, exposure) { Definition = "800" };
            exposure.GainExpression = replacement;
            Render();
            display.Text.Should().Be("800");
            replacement.Definition = "100 + 300";
            exposure.Gain.Should().Be(400);
            Render();
            display.Text.Should().Be("400");
        }

        [TestCase(true, 200)]
        [TestCase(true, -1)]
        [TestCase(false, -1)]
        public void DiscreteGainDisplay_FormatsKnownAndUnresolvedCameraDefaults(bool connected, int defaultGain) {
            cameraInfo.Gains = [0, 200, 400, 800];
            cameraInfo.Connected = connected;
            cameraInfo.DefaultGain = defaultGain;
            exposure.Gain = -1;
            exposure.Validate();
            ShowView();

            Display(GainColumn(true)).Text.Should().Be(connected ? $"({defaultGain})" : $"({Loc.Instance["LblCamera"]})");
        }

        [Test]
        public void NumericGainAndOffsetEditors_KeepTheirExistingRoundTripBehavior() {
            cameraInfo.DefaultOffset = 10;
            exposure.Validate();
            ShowView();
            foreach ((DataGridTemplateColumn column, bool gain) in new[] { (GainColumn(false), true), (OffsetColumn(), false) }) {
                BeginEdit(column);
                HintTextBox editor = CellContent<HintTextBox>(column);
                foreach (string text in new[] { "20", "0", "" }) {
                    editor.SetCurrentValue(TextBox.TextProperty, text);
                    editor.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                    Render();
                    int expected = text.Length == 0 ? (gain ? 200 : 10) : int.Parse(text);
                    (gain ? exposure.Gain : exposure.Offset).Should().Be(expected);
                }
                grid.CommitEdit(DataGridEditingUnit.Cell, true).Should().BeTrue();
                Render();
                Display(column).Text.Should().Be(gain ? "(200)" : "(10)");
            }
        }

        [Test]
        public void DiscreteGainCell_RemainsUsableAcrossSequenceStartAndStop() {
            cameraInfo.Gains = [0, 200, 400, 800];
            SequenceRootContainer root = new SequenceRootContainer();
            TargetAreaContainer targets = new TargetAreaContainer();
            targets.Add(target);
            root.Add(new SequentialContainer());
            root.Add(targets);
            root.Add(new SequentialContainer());
            TaskCompletionSource completion = new TaskCompletionSource();
            CancellationToken sequenceToken = default;
            Mock<ISequencer> sequencer = new Mock<ISequencer>();
            sequencer.SetupGet(x => x.MainContainer).Returns(root);
            sequencer.Setup(x => x.Start(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback<IProgress<ApplicationStatus>, CancellationToken>((_, token) => sequenceToken = token)
                .Returns(completion.Task);
            SimpleSequenceVM vm = new SimpleSequenceVM(profile.Object, Mock.Of<ISequenceMediator>(), camera.Object,
                Mock.Of<IApplicationStatusMediator>(), Mock.Of<INighttimeCalculator>(), Mock.Of<IPlanetariumFactory>(),
                Mock.Of<IFramingAssistantVM>(), Mock.Of<IApplicationMediator>(), factory.Object);
            typeof(SimpleSequenceVM).GetProperty(nameof(SimpleSequenceVM.Sequencer))!.SetValue(vm, sequencer.Object);
            vm.FlipTrigger = new MeridianFlipTrigger(profile.Object, camera.Object, Mock.Of<ITelescopeMediator>(),
                Mock.Of<IFocuserMediator>(), Mock.Of<IApplicationStatusMediator>(), Mock.Of<IMeridianFlipVMFactory>(),
                Mock.Of<ISafetyMonitorMediator>());
            vm.SelectedTarget = target;
            SimpleSequenceView sequenceView = new SimpleSequenceView { DataContext = vm };
            window.Content = sequenceView;
            Render();
            grid = Descendants(sequenceView).OfType<DataGrid>().Single();

            SynchronizationContext? previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            try {
                Task<bool> running = vm.StartSequence(null);
                Render();
                running.IsFaulted.Should().BeFalse(running.Exception?.ToString());
                vm.IsRunning.Should().BeTrue();
                Display(GainColumn(true)).Text.Should().Be("400");
                vm.CancelSequenceCommand.Execute(null);
                sequenceToken.IsCancellationRequested.Should().BeTrue();
                completion.SetResult();
                Render();
                running.IsCompletedSuccessfully.Should().BeTrue();
                vm.IsRunning.Should().BeFalse();
                camera.Verify(x => x.RegisterCaptureBlock(vm), Times.Once);
                camera.Verify(x => x.ReleaseCaptureBlock(vm), Times.Once);

                BeginEdit(GainColumn(true));
                ComboBox editor = CellContent<ComboBox>(GainColumn(true));
                editor.SetCurrentValue(Selector.SelectedValueProperty, "800");
                grid.CommitEdit(DataGridEditingUnit.Cell, true).Should().BeTrue();
                Render();
                Display(GainColumn(true)).Text.Should().Be("800");
            } finally {
                completion.TrySetResult();
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        private void ShowView() {
            window.Content = view;
            Render();
        }

        private void Render() {
            window.UpdateLayout();
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            dispatcherExceptions.Should().BeEmpty();
        }

        private void BeginEdit(DataGridTemplateColumn column) {
            grid.CurrentCell = new DataGridCellInfo(row, column);
            grid.BeginEdit().Should().BeTrue();
            Render();
        }

        private DataGridTemplateColumn GainColumn(bool discrete) => grid.Columns.OfType<DataGridTemplateColumn>()
            .Where(column => Equals(column.Header, Loc.Instance["LblGain"])).ElementAt(discrete ? 0 : 1);

        private DataGridTemplateColumn OffsetColumn() => grid.Columns.OfType<DataGridTemplateColumn>()
            .Single(column => Equals(column.Header, Loc.Instance["LblOffset"]));

        private TextBlock Display(DataGridTemplateColumn column) => CellContent<TextBlock>(column);

        private T CellContent<T>(DataGridTemplateColumn column) where T : DependencyObject =>
            Descendants(column.GetCellContent(row)).OfType<T>().Single();

        private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            dispatcherExceptions.Add(e.Exception);
            e.Handled = true;
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject parent) {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (DependencyObject descendant in Descendants(child)) {
                    yield return descendant;
                }
            }
        }
    }
}