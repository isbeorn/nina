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
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.FlatDevice {

    [TestFixture]
    public class TrainedFlatExposureTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IImageHistoryVM> imageHistoryMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFlatDeviceMediator> flatDeviceMediatorMock;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;
            profile.CameraSettings.Gain = -1;
            profile.CameraSettings.Offset = -1;
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

            imagingMediatorMock = new Mock<IImagingMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            imageHistoryMock = new Mock<IImageHistoryVM>();

            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo {
                Connected = true,
                SelectedFilter = new FilterInfo { Name = "L", Position = 1 }
            });

            flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            flatDeviceMediatorMock.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo {
                Connected = true,
                SupportsOnOff = true,
                SupportsOpenClose = true,
                MinBrightness = 0,
                MaxBrightness = 255
            });
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies the Trained Flat Exposure Constructor Creates Expected Immutable Children scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void TrainedFlatExposure_Constructor_CreatesExpectedImmutableChildren() {
            TrainedFlatExposure sut = CreateFlatSut();

            sut.Items.Should().HaveCount(7);
            sut.GetCloseCoverItem().Should().NotBeNull();
            sut.GetToggleLightOnItem().OnOff.Should().BeTrue();
            sut.GetSwitchFilterItem().Should().BeOfType<SwitchFilter>();
            sut.GetSetBrightnessItem().Should().NotBeNull();
            sut.GetImagingContainer().Should().BeOfType<SequentialContainer>();
            sut.GetExposureItem().Should().BeOfType<TakeExposure>();
            sut.GetExposureItem().ImageType.Should().Be(CaptureSequence.ImageTypes.FLAT);
            sut.GetIterations().Iterations.Should().Be(1);
            sut.GetToggleLightOffItem().OnOff.Should().BeFalse();
            sut.GetOpenCoverItem().Should().NotBeNull();
        }

        /// <summary>
        /// Verifies the Trained Dark Flat Exposure Constructor Creates Expected Immutable Children scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void TrainedDarkFlatExposure_Constructor_CreatesExpectedImmutableChildren() {
            TrainedDarkFlatExposure sut = CreateDarkFlatSut();

            sut.Items.Should().HaveCount(6);
            sut.GetCloseCoverItem().Should().NotBeNull();
            sut.GetToggleLightOffItem().OnOff.Should().BeFalse();
            sut.GetSwitchFilterItem().Should().BeOfType<SwitchFilter>();
            sut.GetSetBrightnessItem().Should().NotBeNull();
            sut.GetImagingContainer().Should().BeOfType<SequentialContainer>();
            sut.GetExposureItem().Should().BeOfType<TakeExposure>();
            sut.GetExposureItem().ImageType.Should().Be(CaptureSequence.ImageTypes.DARK);
            sut.GetIterations().Iterations.Should().Be(1);
            sut.GetOpenCoverItem().Should().NotBeNull();
        }

        /// <summary>
        /// Verifies the Error Behavior And Attempts Propagate To Immutable Children scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ErrorBehaviorAndAttempts_PropagateToImmutableChildren() {
            TrainedFlatExposure flat = CreateFlatSut();
            TrainedDarkFlatExposure darkFlat = CreateDarkFlatSut();

            flat.ErrorBehavior = InstructionErrorBehavior.AbortOnError;
            flat.Attempts = 3;
            darkFlat.ErrorBehavior = InstructionErrorBehavior.AbortOnError;
            darkFlat.Attempts = 4;

            flat.Items.Should().OnlyContain(i => i.ErrorBehavior == InstructionErrorBehavior.AbortOnError);
            flat.Items.Should().OnlyContain(i => i.Attempts == 3);
            darkFlat.Items.Should().OnlyContain(i => i.ErrorBehavior == InstructionErrorBehavior.AbortOnError);
            darkFlat.Items.Should().OnlyContain(i => i.Attempts == 4);
        }

        /// <summary>
        /// Verifies the Clone Copies Scalar Settings And Clones Mutable Children scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesScalarSettingsAndClonesMutableChildren() {
            TrainedFlatExposure flat = CreateFlatSut();
            flat.KeepPanelClosed = true;
            TrainedDarkFlatExposure darkFlat = CreateDarkFlatSut();
            darkFlat.KeepPanelClosed = true;

            TrainedFlatExposure flatClone = (TrainedFlatExposure)flat.Clone();
            TrainedDarkFlatExposure darkFlatClone = (TrainedDarkFlatExposure)darkFlat.Clone();

            flatClone.KeepPanelClosed.Should().BeTrue();
            flatClone.GetExposureItem().Should().NotBeSameAs(flat.GetExposureItem());
            flatClone.GetIterations().Should().NotBeSameAs(flat.GetIterations());
            flatClone.GetSetBrightnessItem().Should().NotBeSameAs(flat.GetSetBrightnessItem());

            darkFlatClone.KeepPanelClosed.Should().BeTrue();
            darkFlatClone.GetExposureItem().Should().NotBeSameAs(darkFlat.GetExposureItem());
            darkFlatClone.GetIterations().Should().NotBeSameAs(darkFlat.GetIterations());
            darkFlatClone.GetSetBrightnessItem().Should().NotBeSameAs(darkFlat.GetSetBrightnessItem());
        }

        /// <summary>
        /// Verifies the Validate Returns True When Matching Trained Exposure Exists scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_ReturnsTrueWhenMatchingTrainedExposureExists() {
            AddTrainedSetting(brightness: 80, exposureTime: 2.5);
            TrainedFlatExposure flat = CreateFlatSut();
            TrainedDarkFlatExposure darkFlat = CreateDarkFlatSut();

            flat.Validate().Should().BeTrue();
            flat.Issues.Should().BeEmpty();

            darkFlat.Validate().Should().BeTrue();
            darkFlat.Issues.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the Validate Returns Issue When No Matching Trained Exposure Exists scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_ReturnsIssueWhenNoMatchingTrainedExposureExists() {
            TrainedFlatExposure flat = CreateFlatSut();
            TrainedDarkFlatExposure darkFlat = CreateDarkFlatSut();

            flat.Validate().Should().BeFalse();
            flat.Issues.Should().NotBeEmpty();

            darkFlat.Validate().Should().BeFalse();
            darkFlat.Issues.Should().NotBeEmpty();
        }

        /// <summary>
        /// Verifies the Execute Skips When Iteration Progress Is Already Complete scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Execute_SkipsWhenIterationProgressIsAlreadyComplete() {
            TrainedFlatExposure flat = CreateFlatSut();
            flat.GetIterations().CompletedIterations = 1;
            flat.GetIterations().Iterations = 1;
            TrainedDarkFlatExposure darkFlat = CreateDarkFlatSut();
            darkFlat.GetIterations().CompletedIterations = 1;
            darkFlat.GetIterations().Iterations = 1;

            Func<Task> flatAct = () => flat.Execute(default, CancellationToken.None);
            Func<Task> darkFlatAct = () => darkFlat.Execute(default, CancellationToken.None);

            await flatAct.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*progress is already complete*");
            await darkFlatAct.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*progress is already complete*");
        }

        private void AddTrainedSetting(int brightness, double exposureTime) {
            profile.FlatDeviceSettings.AddTrainedFlatExposureSetting(
                filterPosition: null,
                binning: new BinningMode(1, 1),
                gain: -1,
                offset: -1,
                brightness: brightness,
                exposureTime: exposureTime);
        }

        private TrainedFlatExposure CreateFlatSut() {
            return new TrainedFlatExposure(
                profileServiceMock.Object,
                cameraMediatorMock.Object,
                imagingMediatorMock.Object,
                imageSaveMediatorMock.Object,
                imageHistoryMock.Object,
                filterWheelMediatorMock.Object,
                flatDeviceMediatorMock.Object);
        }

        private TrainedDarkFlatExposure CreateDarkFlatSut() {
            return new TrainedDarkFlatExposure(
                profileServiceMock.Object,
                cameraMediatorMock.Object,
                imagingMediatorMock.Object,
                imageSaveMediatorMock.Object,
                imageHistoryMock.Object,
                filterWheelMediatorMock.Object,
                flatDeviceMediatorMock.Object);
        }
    }
}
