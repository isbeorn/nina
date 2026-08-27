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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Trigger.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Trigger.Platesolving {

    [TestFixture]
    public class CenterAfterDriftTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IFilterWheelMediator> filterMediatorMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IDomeFollower> domeFollowerMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Func<object, BeforeImageSavedEventArgs, Task> imageSavedHandlers;

        [SetUp]
        public void Setup() {
            imageSavedHandlers = null;
            profileServiceMock = new Mock<IProfileService>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            filterMediatorMock = new Mock<IFilterWheelMediator>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            domeFollowerMock = new Mock<IDomeFollower>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo());
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo());
            imageSaveMediatorMock
                .SetupAdd(x => x.BeforeImageSaved += It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>())
                .Callback<Func<object, BeforeImageSavedEventArgs, Task>>(handler => imageSavedHandlers += handler);
            imageSaveMediatorMock
                .SetupRemove(x => x.BeforeImageSaved -= It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>())
                .Callback<Func<object, BeforeImageSavedEventArgs, Task>>(handler => imageSavedHandlers -= handler);
        }

        [Test]
        [TestCase(3.8, 400, 1, 30)]
        [TestCase(3.8, 800, 1, 61)]
        [TestCase(3.8, 1600, 1, 122)]
        public void DistanceArcMinutes_CorrectlyCalculates_DistancePixels(double pixelsize, int focallength, double arcmin, double expectedPixels) {
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.PixelSize).Returns(pixelsize);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings.FocalLength).Returns(focallength);

            var sut = new CenterAfterDriftTrigger(
                profileServiceMock.Object,
                telescopeMediatorMock.Object,
                filterMediatorMock.Object,
                guiderMediatorMock.Object,
                imagingMediatorMock.Object,
                cameraMediatorMock.Object,
                domeMediatorMock.Object,
                domeFollowerMock.Object,
                imageSaveMediatorMock.Object,
                applicationStatusMediatorMock.Object,
                safetyMonitorMediatorMock.Object);

            sut.DistanceArcMinutes = arcmin;

            sut.DistancePixels.Should().BeApproximately(expectedPixels, 1);
        }

        /// <summary>
        /// Verifies that CenterAfterDrift evaluates its arc-minute distance expression and converts it into pixel distance.
        /// </summary>
        [Test]
        public void DistanceArcMinutesExpression_CorrectlyCalculates_DistancePixels() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.PixelSize).Returns(3.8);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings.FocalLength).Returns(400);

            var sut = CreateSut();
            sut.DistanceArcMinutesDefinition = "0.5 + 0.5";

            sut.DistanceArcMinutes.Should().Be(1);
            sut.DistancePixels.Should().BeApproximately(30, 1);
        }

        /// <summary>
        /// Verifies that CenterAfterDrift uses the evaluated distance expression when deciding whether accumulated drift should trigger recentering.
        /// </summary>
        [Test]
        public void ShouldTrigger_UsesEvaluatedDistanceArcMinutesExpression() {
            var sut = CreateSut();
            sut.DistanceArcMinutesDefinition = "2 + 3";
            SetLastDistanceArcMinutes(sut, 5);

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.Zero);

            sut.ShouldTrigger(null, itemMock.Object).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that a stale Center After Drift follower from a completed target cannot consume images from the next target.
        /// </summary>
        [Test]
        public async Task ImageSaveEvent_InactiveTriggerDoesNotConsumeImageFromNextContainer() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer firstTarget = new SequentialContainer();
            SequentialContainer secondTarget = new SequentialContainer();
            CenterAfterDriftTrigger firstTrigger = CreateSut();
            CenterAfterDriftTrigger secondTrigger = CreateSut();
            firstTrigger.AfterExposures = 3;
            secondTrigger.AfterExposures = 3;
            root.Add(firstTarget);
            root.Add(secondTarget);
            firstTarget.Add(firstTrigger);
            secondTarget.Add(secondTrigger);

            try {
                root.Status = SequenceEntityStatus.RUNNING;
                firstTarget.Status = SequenceEntityStatus.RUNNING;
                firstTrigger.SequenceBlockInitialize();

                firstTarget.Status = SequenceEntityStatus.FINISHED;
                secondTarget.Status = SequenceEntityStatus.RUNNING;
                secondTrigger.SequenceBlockInitialize();

                imageSavedHandlers.Should().NotBeNull();
                await imageSavedHandlers.Invoke(this, CreateImageSavedArgs("LIGHT"));

                firstTrigger.ProgressExposures.Should().Be(0);
                secondTrigger.ProgressExposures.Should().Be(1);
            } finally {
                firstTrigger.SequenceBlockTeardown();
                secondTrigger.SequenceBlockTeardown();
            }
        }

        /// <summary>
        /// Verifies that a late plate-solve result from a completed target cannot change that target's drift state.
        /// </summary>
        [Test]
        public void PlateSolveResult_InactiveTriggerDoesNotPublishDrift() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer target = new SequentialContainer();
            CenterAfterDriftTrigger sut = CreateSut();
            root.Add(target);
            target.Add(sut);
            sut.Coordinates.Coordinates = new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.J2000);

            try {
                root.Status = SequenceEntityStatus.RUNNING;
                target.Status = SequenceEntityStatus.RUNNING;
                sut.SequenceBlockInitialize();
                PlatesolvingImageFollower follower = GetImageFollower(sut);

                target.Status = SequenceEntityStatus.FINISHED;
                follower.LastCoordinates = new Coordinates(Angle.ByHours(11), Angle.ByDegree(20), Epoch.J2000);

                sut.LastDistanceArcMinutes.Should().Be(0);
            } finally {
                sut.SequenceBlockTeardown();
            }
        }

        /// <summary>
        /// Verifies that each Center After Drift trigger in a sequence owns exactly one subscription and releases it at teardown.
        /// </summary>
        [Test]
        public void SequenceBlockLifecycle_MultipleContainersReleaseTheirFollowers() {
            SequenceRootContainer root = new SequenceRootContainer();
            CenterAfterDriftTrigger[] triggers = Enumerable.Range(0, 3)
                .Select(_ => CreateSut())
                .ToArray();
            SequentialContainer[] targets = triggers
                .Select(trigger => {
                    SequentialContainer target = new SequentialContainer();
                    root.Add(target);
                    target.Add(trigger);
                    return target;
                })
                .ToArray();
            root.Status = SequenceEntityStatus.RUNNING;

            for (int i = 0; i < triggers.Length; i++) {
                targets[i].Status = SequenceEntityStatus.RUNNING;
                triggers[i].SequenceBlockInitialize();
                imageSavedHandlers.GetInvocationList().Should().HaveCount(1);

                triggers[i].SequenceBlockInitialize();
                imageSavedHandlers.GetInvocationList().Should().HaveCount(1);

                triggers[i].SequenceBlockTeardown();
                imageSavedHandlers.Should().BeNull();
                targets[i].Status = SequenceEntityStatus.FINISHED;
            }

            imageSaveMediatorMock.VerifyAdd(
                x => x.BeforeImageSaved += It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>(),
                Times.Exactly(6));
            imageSaveMediatorMock.VerifyRemove(
                x => x.BeforeImageSaved -= It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>(),
                Times.Exactly(6));
        }

        /// <summary>
        /// Verifies that followers reject images for every non-running parent state.
        /// </summary>
        [TestCase(SequenceEntityStatus.CREATED)]
        [TestCase(SequenceEntityStatus.FINISHED)]
        [TestCase(SequenceEntityStatus.FAILED)]
        [TestCase(SequenceEntityStatus.SKIPPED)]
        [TestCase(SequenceEntityStatus.DISABLED)]
        public async Task ImageSaveEvent_ParentNotRunningDoesNotConsumeImage(SequenceEntityStatus parentStatus) {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer target = new SequentialContainer();
            CenterAfterDriftTrigger sut = CreateSut();
            sut.AfterExposures = 3;
            root.Add(target);
            target.Add(sut);

            try {
                root.Status = SequenceEntityStatus.RUNNING;
                target.Status = parentStatus;
                sut.SequenceBlockInitialize();

                await imageSavedHandlers.Invoke(this, CreateImageSavedArgs("LIGHT"));

                sut.ProgressExposures.Should().Be(0);
            } finally {
                sut.SequenceBlockTeardown();
            }
        }

        /// <summary>
        /// Verifies that a disabled trigger rejects images even while its parent is running.
        /// </summary>
        [Test]
        public async Task ImageSaveEvent_DisabledTriggerDoesNotConsumeImage() {
            SequenceRootContainer root = new SequenceRootContainer();
            SequentialContainer target = new SequentialContainer();
            CenterAfterDriftTrigger sut = CreateSut();
            sut.AfterExposures = 3;
            root.Add(target);
            target.Add(sut);

            try {
                root.Status = SequenceEntityStatus.RUNNING;
                target.Status = SequenceEntityStatus.RUNNING;
                sut.Status = SequenceEntityStatus.DISABLED;
                sut.SequenceBlockInitialize();

                await imageSavedHandlers.Invoke(this, CreateImageSavedArgs("LIGHT"));

                sut.ProgressExposures.Should().Be(0);
            } finally {
                sut.SequenceBlockTeardown();
            }
        }

        private CenterAfterDriftTrigger CreateSut() {
            return new CenterAfterDriftTrigger(
                profileServiceMock.Object,
                telescopeMediatorMock.Object,
                filterMediatorMock.Object,
                guiderMediatorMock.Object,
                imagingMediatorMock.Object,
                cameraMediatorMock.Object,
                domeMediatorMock.Object,
                domeFollowerMock.Object,
                imageSaveMediatorMock.Object,
                applicationStatusMediatorMock.Object,
                safetyMonitorMediatorMock.Object);
        }

        private static void SetLastDistanceArcMinutes(CenterAfterDriftTrigger sut, double distanceArcMinutes) {
            typeof(CenterAfterDriftTrigger)
                .GetField("lastDistanceArcMinutes", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(sut, distanceArcMinutes);
        }

        private static PlatesolvingImageFollower GetImageFollower(CenterAfterDriftTrigger sut) {
            return (PlatesolvingImageFollower)typeof(CenterAfterDriftTrigger)
                .GetField("platesolvingImageFollower", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(sut);
        }

        private static BeforeImageSavedEventArgs CreateImageSavedArgs(string imageType) {
            Mock<IImageData> imageMock = new Mock<IImageData>();
            imageMock.SetupGet(x => x.MetaData).Returns(new ImageMetaData {
                Image = new ImageParameter { ImageType = imageType }
            });
            return new BeforeImageSavedEventArgs(imageMock.Object, Task.FromResult<IRenderedImage>(null));
        }
    }
}
