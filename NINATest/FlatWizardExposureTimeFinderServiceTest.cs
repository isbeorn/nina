#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.ViewModel.FlatWizard;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using NINA.Utility.WindowService;
using FluentAssertions;
using NINA.Model.MyFilterWheel;
using System.Windows.Input;
using NINA.Locale;
using System.Windows.Threading;
using NINA.Model.ImageData;

namespace NINATest {

    [TestFixture]
    public class FlatWizardExposureTimeFinderServiceTest {
        private FlatWizardExposureTimeFinderService _sut;
        private Mock<IWindowService> windowServiceMock;
        private Mock<ILoc> localeMock;

        [SetUp]
        public void Init() {
            windowServiceMock = new Mock<IWindowService>();
            localeMock = new Mock<ILoc>();
            _sut = new FlatWizardExposureTimeFinderService();
            _sut.WindowService = windowServiceMock.Object;
            localeMock.Setup(m => m[It.IsAny<string>()]).Returns("");
            _sut.Locale = localeMock.Object;
        }

        [Test]
        public void GetNextExposureTime_WhenLessThanThreePoints_IncreaseByStepSize() {
            FlatWizardFilterSettingsWrapper wrapper = new FlatWizardFilterSettingsWrapper(new FilterInfo("Test", 0, 0), new FlatWizardFilterSettings() {
                StepSize = 5
            }, 8);

            var result = _sut.GetNextExposureTime(1, wrapper);
            result.Should().Be(6);

            result = _sut.GetNextExposureTime(result, wrapper);
            result.Should().Be(11);
        }

        [Test]
        public void GetNextExposureTime_WhenMoreThanThreePoints_IncreaseByLinearDataPlotCalculation() {
            FlatWizardFilterSettingsWrapper wrapper = new FlatWizardFilterSettingsWrapper(new FilterInfo("Test", 0, 0), new FlatWizardFilterSettings() {
                HistogramMeanTarget = 0.5,
            }, 8);

            _sut.AddDataPoint(1, 10);
            _sut.AddDataPoint(2, 20);
            _sut.AddDataPoint(3, 30);

            var result = _sut.GetNextExposureTime(1, wrapper);
            result.Should().BeApproximately(12.8, 0.000000001);
        }

        [Test]
        [TestCase(5, FlatWizardExposureTimeState.ExposureTimeWithinBounds)]
        [TestCase(31, FlatWizardExposureTimeState.ExposureTimeAboveMaxTime)]
        [TestCase(0.05, FlatWizardExposureTimeState.ExposureTimeBelowMinTime)]
        public void GetNextFlatExposureState_DifferentValues_ProperExpectedResults(double exposureTime, FlatWizardExposureTimeState state) {
            FlatWizardFilterSettingsWrapper wrapper = new FlatWizardFilterSettingsWrapper(new FilterInfo("Test", 0, 0), new FlatWizardFilterSettings() {
                MinFlatExposureTime = 0.1,
                MaxFlatExposureTime = 30
            }, 8);

            var result = _sut.GetNextFlatExposureState(exposureTime, wrapper);
            result.Should().Be(state);
        }

        [Test]
        [TestCase(0.5, 0.1, 100, FlatWizardExposureAduState.ExposureAduBelowMean)]
        [TestCase(0.5, 0.2, 100, FlatWizardExposureAduState.ExposureAduBelowMean)]
        [TestCase(0.2, 0.2, 10, FlatWizardExposureAduState.ExposureAduBelowMean)]
        [TestCase(0.5, 0.1, 102, FlatWizardExposureAduState.ExposureAduBelowMean)]
        [TestCase(0.5, 0.1, 200, FlatWizardExposureAduState.ExposureAduAboveMean)]
        [TestCase(0.5, 0.2, 200, FlatWizardExposureAduState.ExposureAduAboveMean)]
        [TestCase(0.2, 0.2, 61.5, FlatWizardExposureAduState.ExposureAduAboveMean)]
        [TestCase(0.5, 0.1, 154, FlatWizardExposureAduState.ExposureAduAboveMean)]
        [TestCase(0.5, 0.1, 115.2, FlatWizardExposureAduState.ExposureFinished)]
        [TestCase(0.5, 0.2, 102.5, FlatWizardExposureAduState.ExposureFinished)]
        [TestCase(0.5, 0.2, 150, FlatWizardExposureAduState.ExposureFinished)]
        public async Task GetFlatExposureState_DifferentValues_ProperExpectedResults(double meanTarget, double tolerance, double mean, FlatWizardExposureAduState state) {
            FlatWizardFilterSettingsWrapper wrapper = new FlatWizardFilterSettingsWrapper(new FilterInfo("Test", 0, 0), new FlatWizardFilterSettings() {
                HistogramMeanTarget = meanTarget,
                HistogramTolerance = tolerance
            }, 8);

            var test = new Mock<IFlatWizardExposureTimeFinderService>();

            var arrMock = new Mock<IImageArray>();

            var statMock = new Mock<IImageStatistics>();
            statMock.Setup(m => m.Mean).Returns(mean);
            var imageDataMock = new Mock<IImageData>();
            imageDataMock.Setup(m => m.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(statMock.Object)));

            var result = await _sut.GetFlatExposureState(imageDataMock.Object, 10, wrapper);
            result.Should().Be(state);
        }

        [Test]
        public async Task EvaluateUserPromptResult_GetResponseWithoutResetAndContinue_ShouldReturnProperResponseAsync() {
            FlatWizardFilterSettingsWrapper wrapper = new FlatWizardFilterSettingsWrapper(new FilterInfo("Test", 0, 0), new FlatWizardFilterSettings() {
                HistogramMeanTarget = 0.5,
                HistogramTolerance = 0.1
            }, 8);

            DispatcherFrame frame = new DispatcherFrame();

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<FlatWizardUserPromptVM>(), It.IsAny<string>(), System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow,
                It.IsAny<ICommand>()))
                .Returns(Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>((f) => { ((DispatcherFrame)f).Continue = false; }), frame))
                .Callback<FlatWizardUserPromptVM, object, object, object, object>((f, f1, f2, f3, f4) => {
                    f.Continue = true;
                    f.Reset = false;
                });

            var statMock = new Mock<IImageStatistics>();
            statMock.Setup(m => m.Mean).Returns(500);
            var imageDataMock = new Mock<IImageData>();
            imageDataMock.Setup(m => m.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(statMock.Object)));

            var resultTask = _sut.EvaluateUserPromptResultAsync(imageDataMock.Object, 10, "", wrapper);
            Dispatcher.PushFrame(frame);
            var result = await resultTask;

            result.Continue.Should().BeTrue();
            result.NextExposureTime.Should().Be(10);
        }

        [Test]
        public async Task EvaluateUserPromptResult_GetResponseWithResetAndContinue_ShouldReturnProperResponseAsync() {
            FlatWizardFilterSettingsWrapper wrapper = new FlatWizardFilterSettingsWrapper(new FilterInfo("Test", 0, 0), new FlatWizardFilterSettings() {
                HistogramMeanTarget = 0.5,
                HistogramTolerance = 0.1,
                MinFlatExposureTime = 10
            }, 8);

            DispatcherFrame frame = new DispatcherFrame();

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<FlatWizardUserPromptVM>(), It.IsAny<string>(), System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow,
                It.IsAny<ICommand>()))
                .Returns(Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>((f) => { ((DispatcherFrame)f).Continue = false; }), frame))
                .Callback<FlatWizardUserPromptVM, object, object, object, object>((f, f1, f2, f3, f4) => {
                    f.Continue = true;
                    f.Reset = true;
                });

            var statMock = new Mock<IImageStatistics>();
            statMock.Setup(m => m.Mean).Returns(500);
            var imageDataMock = new Mock<IImageData>();
            imageDataMock.Setup(m => m.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(statMock.Object)));

            var resultTask = _sut.EvaluateUserPromptResultAsync(imageDataMock.Object, 5, "", wrapper);
            Dispatcher.PushFrame(frame);
            var result = await resultTask;

            result.Continue.Should().BeTrue();
            result.NextExposureTime.Should().Be(10);
        }

        [Test]
        public async Task EvaluateUserPromptResult_GetResponseWithCancel_ShouldReturnProperResponseAsync() {
            FlatWizardFilterSettingsWrapper wrapper = new FlatWizardFilterSettingsWrapper(new FilterInfo("Test", 0, 0), new FlatWizardFilterSettings() {
                HistogramMeanTarget = 0.5,
                HistogramTolerance = 0.1,
                MinFlatExposureTime = 10
            }, 8);

            DispatcherFrame frame = new DispatcherFrame();

            windowServiceMock.Setup(m => m.ShowDialog(It.IsAny<FlatWizardUserPromptVM>(), It.IsAny<string>(), System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow,
                It.IsAny<ICommand>()))
                .Returns(Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>((f) => { ((DispatcherFrame)f).Continue = false; }), frame))
                .Callback<FlatWizardUserPromptVM, object, object, object, object>((f, f1, f2, f3, f4) => {
                    f.Continue = false;
                    f.Reset = true;
                });

            var statMock = new Mock<IImageStatistics>();
            statMock.Setup(m => m.Mean).Returns(500);
            var imageDataMock = new Mock<IImageData>();
            imageDataMock.Setup(m => m.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(statMock.Object)));

            var resultTask = _sut.EvaluateUserPromptResultAsync(imageDataMock.Object, 5, "", wrapper);
            Dispatcher.PushFrame(frame);
            var result = await resultTask;

            result.Continue.Should().BeFalse();
            result.NextExposureTime.Should().Be(10);
        }

        [Test]
        [TestCase(2, 4)]
        [TestCase(4, 16)]
        [TestCase(8, 256)]
        [TestCase(16, 65536)]
        public void CameraBitDepthToAdu_WhenUsingDifferentValues_CalculateProperly(double bitDepth, double expectedResultInAdu) {
            FlatWizardExposureTimeFinderService.CameraBitDepthToAdu(bitDepth).Should().Be(expectedResultInAdu);
        }

        [Test]
        [TestCase(0.5, 8, 128)]
        [TestCase(0.2, 8, 51.2)]
        [TestCase(0.5, 16, 32768)]
        [TestCase(0.3, 16, 19660.8)]
        public void HistogramMeanAndCameraBitDepthToAdu_WhenUsingDifferentValues_CalculateProperly(double histogramMean, double cameraBitDepth, double expectedResultinAdu) {
            FlatWizardExposureTimeFinderService.HistogramMeanAndCameraBitDepthToAdu(histogramMean, cameraBitDepth).Should().Be(expectedResultinAdu);
        }
    }
}