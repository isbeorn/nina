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
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static NINA.Sequencer.SequenceItem.FlatDevice.SkyFlat;

namespace NINA.Test.Sequencer.SequenceItem.FlatDevice {

    [TestFixture]
    public class SkyFlatTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IImageHistoryVM> imageHistoryMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<ITwilightCalculator> twilightCalculatorMock;
        private Mock<ISymbolBroker> symbolBrokerMock;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;
            profile.GuiderSettings.DitherPixels = 8;
            profile.GuiderSettings.SettleTime = 12;
            profile.AstrometrySettings.Latitude = 47.1;
            profile.AstrometrySettings.Longitude = 11.3;
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo { Name = "L", Position = 1 });

            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            cameraMediatorMock = new Mock<ICameraMediator>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo {
                Connected = true,
                CanSetGain = true,
                CanSetOffset = true,
                DefaultGain = -1,
                DefaultOffset = -1,
                GainMin = 0,
                GainMax = 100,
                OffsetMin = 0,
                OffsetMax = 100
            });

            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo {
                Connected = true,
                CanPulseGuide = true,
                AtPark = false
            });

            imagingMediatorMock = new Mock<IImagingMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            imageHistoryMock = new Mock<IImageHistoryVM>();

            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo {
                Connected = true,
                SelectedFilter = new FilterInfo { Name = "L", Position = 1 }
            });

            twilightCalculatorMock = new Mock<ITwilightCalculator>();
            symbolBrokerMock = new Mock<ISymbolBroker>();
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies the Constructor Creates Expected Immutable Children And Defaults scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Constructor_CreatesExpectedImmutableChildrenAndDefaults() {
            SkyFlat sut = CreateSut();

            sut.Items.Should().HaveCount(5);
            sut.GetSwitchFilterItem().Should().BeOfType<SwitchFilter>();
            sut.GetImagingContainer().Should().BeOfType<SequentialContainer>();
            sut.GetExposureItem().Should().BeOfType<TakeExposure>();
            sut.GetIterations().Iterations.Should().Be(1);
            sut.GetExposureItem().ImageType.Should().Be(CaptureSequence.ImageTypes.FLAT);
            sut.HistogramTargetPercentage.Should().Be(0.5);
            sut.HistogramTolerancePercentage.Should().Be(0.1);
            sut.MinExposure.Should().Be(0);
            sut.MaxExposure.Should().Be(10);
            sut.ShouldDither.Should().BeFalse();
            sut.DitherPixels.Should().Be(profile.GuiderSettings.DitherPixels);
            sut.DitherSettleTime.Should().Be(profile.GuiderSettings.SettleTime);
        }

        /// <summary>
        /// Verifies the Error Behavior And Attempts Propagate To Immutable Children scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ErrorBehaviorAndAttempts_PropagateToImmutableChildren() {
            SkyFlat sut = CreateSut();

            sut.ErrorBehavior = InstructionErrorBehavior.AbortOnError;
            sut.Attempts = 3;

            sut.Items.Should().OnlyContain(i => i.ErrorBehavior == InstructionErrorBehavior.AbortOnError);
            sut.Items.Should().OnlyContain(i => i.Attempts == 3);
        }

        /// <summary>
        /// Verifies the Clone Copies Scalar Settings And Clones Mutable Children scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesScalarSettingsAndClonesMutableChildren() {
            SkyFlat sut = CreateSut();
            sut.MinExposure = 1;
            sut.MaxExposure = 20;
            sut.HistogramTargetPercentage = 0.7;
            sut.HistogramTolerancePercentage = 0.2;
            sut.ShouldDither = true;
            sut.DitherPixels = 3;
            sut.DitherSettleTime = 4;

            SkyFlat clone = (SkyFlat)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.MinExposure.Should().Be(sut.MinExposure);
            clone.MaxExposure.Should().Be(sut.MaxExposure);
            clone.HistogramTargetPercentage.Should().Be(sut.HistogramTargetPercentage);
            clone.HistogramTolerancePercentage.Should().Be(sut.HistogramTolerancePercentage);
            clone.ShouldDither.Should().BeTrue();
            clone.DitherPixels.Should().Be(sut.DitherPixels);
            clone.DitherSettleTime.Should().Be(sut.DitherSettleTime);
            clone.GetSwitchFilterItem().Should().NotBeSameAs(sut.GetSwitchFilterItem());
            clone.GetExposureItem().Should().NotBeSameAs(sut.GetExposureItem());
            clone.GetIterations().Should().NotBeSameAs(sut.GetIterations());
        }

        /// <summary>
        /// Verifies the Histogram Percentage Setters Clamp To Valid Range scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void HistogramPercentageSetters_ClampToValidRange() {
            SkyFlat sut = CreateSut();

            sut.HistogramTargetPercentage = -1;
            sut.HistogramTargetPercentage.Should().Be(0);
            sut.HistogramTargetPercentage = 2;
            sut.HistogramTargetPercentage.Should().Be(1);

            sut.HistogramTolerancePercentage = -1;
            sut.HistogramTolerancePercentage.Should().Be(0);
            sut.HistogramTolerancePercentage = 2;
            sut.HistogramTolerancePercentage.Should().Be(1);
        }

        /// <summary>
        /// Verifies the Validate Reports Exposure Range And Dither Mount Issues scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_ReportsExposureRangeAndDitherMountIssues() {
            SkyFlat sut = CreateSut();
            sut.MinExposure = 20;
            sut.MaxExposure = 10;
            sut.ShouldDither = true;
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo {
                Connected = false
            });

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().HaveCount(2);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo {
                Connected = true,
                CanPulseGuide = false,
                AtPark = true
            });

            sut.MinExposure = 1;
            sut.MaxExposure = 10;

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().HaveCount(2);
        }

        /// <summary>
        /// Verifies the Validate Dither Enabled With Guidable Unparked Mount Has No Dither Issue scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_DitherEnabledWithGuidableUnparkedMountHasNoDitherIssue() {
            SkyFlat sut = CreateSut();
            sut.ShouldDither = true;

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the Execute Skips When Iteration Progress Is Already Complete scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Execute_SkipsWhenIterationProgressIsAlreadyComplete() {
            SkyFlat sut = CreateSut();
            sut.GetIterations().CompletedIterations = 1;
            sut.GetIterations().Iterations = 1;

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*progress is already complete*");
        }

        /// <summary>
        /// Verifies that sky-flat execution determines a valid exposure, saves that calibration frame, and then captures the remaining requested flats.
        /// </summary>
        [Test]
        public async Task Execute_DeterminesExposureAndCapturesRemainingSkyFlats() {
            SkyFlat sut = CreateSut();
            sut.MinExposure = 0;
            sut.MaxExposure = 10;
            sut.HistogramTargetPercentage = 0.5;
            sut.HistogramTolerancePercentage = 0.1;
            sut.GetIterations().Iterations = 3;
            twilightCalculatorMock.Setup(x => x.GetTwilightDuration(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(TimeSpan.FromMinutes(30));
            SetupImageCaptureMeans(32768, 32768, 32768);

            await sut.Execute(default, CancellationToken.None);

            sut.GetExposureItem().ExposureTime.Should().BeApproximately(5, 0.0001);
            sut.GetIterations().CompletedIterations.Should().Be(3);
            imagingMediatorMock.Verify(x => x.CaptureImage(
                It.IsAny<CaptureSequence>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<ApplicationStatus>>(),
                It.IsAny<string>()), Times.Exactly(3));
            imageSaveMediatorMock.Verify(x => x.Enqueue(
                It.IsAny<IImageData>(),
                It.IsAny<Task<IRenderedImage>>(),
                It.IsAny<IProgress<ApplicationStatus>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        /// <summary>
        /// Verifies the Test Linearity Detects Linear And Non Linear Exposure Response scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void TestLinearity_DetectsLinearAndNonLinearExposureResponse() {
            SkyFlat sut = CreateSut();
            MethodInfo method = typeof(SkyFlat).GetMethod("TestLinearity", BindingFlags.Instance | BindingFlags.NonPublic);

            bool linear = (bool)method.Invoke(sut, new object[] {
                new List<(double exposure, double adu)> {
                    (1, 100),
                    (2, 200),
                    (3, 300)
                }
            });

            bool nonLinear = (bool)method.Invoke(sut, new object[] {
                new List<(double exposure, double adu)> {
                    (1, 100),
                    (2, 150),
                    (3, 900)
                }
            });

            linear.Should().BeTrue();
            nonLinear.Should().BeFalse();
        }

        /// <summary>
        /// Verifies the Sky Flat Exposure Determination Uses Adu Calibration And History For Next Exposure scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SkyFlatExposureDetermination_UsesAduCalibrationAndHistoryForNextExposure() {
            SkyFlatExposureDetermination sut = new SkyFlatExposureDetermination(
                Stopwatch.StartNew(),
                10,
                springTwilight: 1000,
                todayTwilight: 1000,
                dateTime: new IncrementingDateTime());
            sut.TargetADU = 10000;
            sut.LastADU = 5000;

            sut.GetNextExposureTimeByADU().Should().Be(20);

            sut.LastADU = 10000;
            double second = sut.GetNextExposureTimeByADU();
            sut.LastADU = 10000;
            double third = sut.GetNextExposureTimeByADU();

            second.Should().BeGreaterThan(0);
            third.Should().BeGreaterThan(0);
            third.Should().NotBe(double.NaN);

            sut.CameraIsLinear = false;
            sut.GetNextExposureTime().Should().BeGreaterThan(0);
        }

        private SkyFlat CreateSut() {
            return new SkyFlat(
                profileServiceMock.Object,
                cameraMediatorMock.Object,
                telescopeMediatorMock.Object,
                imagingMediatorMock.Object,
                imageSaveMediatorMock.Object,
                imageHistoryMock.Object,
                filterWheelMediatorMock.Object,
                twilightCalculatorMock.Object,
                symbolBrokerMock.Object);
        }

        private void SetupImageCaptureMeans(params double[] means) {
            Queue<double> remainingMeans = new Queue<double>(means);
            imagingMediatorMock.Setup(x => x.CaptureImage(
                    It.IsAny<CaptureSequence>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IProgress<ApplicationStatus>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(() => CreateExposureData(remainingMeans.Dequeue()));
            imagingMediatorMock.Setup(x => x.PrepareImage(
                    It.IsAny<IImageData>(),
                    It.IsAny<PrepareImageParameters>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Mock.Of<IRenderedImage>()));
            imageSaveMediatorMock.Setup(x => x.Enqueue(
                    It.IsAny<IImageData>(),
                    It.IsAny<Task<IRenderedImage>>(),
                    It.IsAny<IProgress<ApplicationStatus>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private static IExposureData CreateExposureData(double mean) {
            Mock<IExposureData> exposureDataMock = new Mock<IExposureData>();
            Mock<IImageData> imageDataMock = new Mock<IImageData>();
            Mock<IImageStatistics> statisticsMock = new Mock<IImageStatistics>();
            ImageProperties imageProperties = new ImageProperties(100, 100, 16, true, 100, 0);

            exposureDataMock.SetupGet(x => x.BitDepth).Returns(16);
            statisticsMock.SetupGet(x => x.Mean).Returns(mean);
            imageDataMock.SetupGet(x => x.Properties).Returns(imageProperties);
            imageDataMock.SetupGet(x => x.MetaData).Returns(new ImageMetaData());
            imageDataMock.SetupGet(x => x.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(statisticsMock.Object)));
            exposureDataMock.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(imageDataMock.Object);

            return exposureDataMock.Object;
        }

        private class IncrementingDateTime : ICustomDateTime {
            private DateTime now = new DateTime(2026, 4, 16, 20, 0, 0);

            public DateTime Now {
                get {
                    now = now.AddSeconds(30);
                    return now;
                }
            }

            public DateTime UtcNow => Now.ToUniversalTime();
        }
    }
}
