#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.WPF.Base.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Test.Sequencer.SequenceItem.Platesolving {

    [TestFixture]
    public class CenterAndRotateTest {
        private static Angle TOLERANCE_EPSILON = Angle.ByDegree(AstroUtil.ArcsecToDegree(0.01));
        private Mock<IProfileService> profileServiceMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IRotatorMediator> rotatorMediatorMock;
        private Mock<IPlateSolverFactory> plateSolverFactoryMock;
        private Mock<IWindowServiceFactory> windowServiceFactoryMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IDomeFollower> domeFollowerMock;

        private CenterAndRotate sut;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            profileServiceMock = new Mock<IProfileService>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            rotatorMediatorMock = new Mock<IRotatorMediator>();
            plateSolverFactoryMock = new Mock<IPlateSolverFactory>();
            windowServiceFactoryMock = new Mock<IWindowServiceFactory>();
            domeMediatorMock = new Mock<IDomeMediator>();
            domeMediatorMock.Setup(m => m.GetInfo()).Returns(new NINA.Equipment.Equipment.MyDome.DomeInfo() { Connected = false });
            domeFollowerMock = new Mock<IDomeFollower>();
        }

        [SetUp]
        public void Setup() {
            profileServiceMock.Reset();
            telescopeMediatorMock.Reset();
            imagingMediatorMock.Reset();
            filterWheelMediatorMock.Reset();
            guiderMediatorMock.Reset();
            rotatorMediatorMock.Reset();
            plateSolverFactoryMock.Reset();
            windowServiceFactoryMock.Reset();

            profileServiceMock.SetupGet(x => x.ActiveProfile.PlateSolveSettings).Returns(new Mock<IPlateSolveSettings>().Object);

            sut = new CenterAndRotate(profileServiceMock.Object, telescopeMediatorMock.Object, imagingMediatorMock.Object, rotatorMediatorMock.Object, filterWheelMediatorMock.Object, guiderMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object, plateSolverFactoryMock.Object, windowServiceFactoryMock.Object);
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (CenterAndRotate)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Validate_TelescopeNotConnected_RotatorNotConnected_TwoIssues() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false });
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = false });
            var valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Count.Should().Be(2);
        }

        [Test]
        public void Validate_TelescopeNotConnected_RotatorConnected_TwoIssues() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false });
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = true });
            var valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Count.Should().Be(1);
        }

        [Test]
        public void Validate_TelescopeConnected_RotatorNotConnected_TwoIssues() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true });
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = false });
            var valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Count.Should().Be(1);
        }

        [Test]
        public void Validate_TelescopeConnected_RotatorConnected_NoIssues() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true });
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = true });
            var valid = sut.Validate();

            valid.Should().BeTrue();
            sut.Issues.Count.Should().Be(0);
        }

        [Test]
        public void ToString_Migration_Test() {
            sut.Category = "TestCategory";
            sut.Coordinates.Coordinates = new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.J2000);
            sut.DeprecatedRotation = 100;
            sut.ToString().Should().Be("Category: TestCategory, Item: CenterAndRotate, Coordinates RA: 10:00:00; Dec: 20° 00' 00\"; Epoch: J2000, Position Angle: 260°");
        }

        [Test]
        public void ToString_Test() {
            sut.Category = "TestCategory";
            sut.Coordinates.Coordinates = new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.J2000);
            sut.PositionAngle = 260;
            sut.ToString().Should().Be("Category: TestCategory, Item: CenterAndRotate, Coordinates RA: 10:00:00; Dec: 20° 00' 00\"; Epoch: J2000, Position Angle: 260°");
        }

        [Test]
        public void AfterParentChanged_NotInDSOSet_NoCoordinatesAvailable() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false });
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = false });

            sut.AfterParentChanged();
            sut.Inherited.Should().BeFalse();
        }

        [Test]
        public void AfterParentChanged_InDSOSet_CoordinatesAvailable() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false });
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = false });

            var mock = new Mock<IDeepSkyObjectContainer>();
            var target = new InputTarget(Angle.Zero, Angle.Zero, default);
            target.InputCoordinates = new InputCoordinates(new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000));
            mock.SetupGet(x => x.Target).Returns(target);
            var dsoset = mock.Object;

            sut.AttachNewParent(dsoset);

            sut.AfterParentChanged();

            sut.Inherited.Should().BeTrue();
            sut.Coordinates.Coordinates.RADegrees.Should().BeApproximately(10, TOLERANCE_EPSILON.Degree);
            sut.Coordinates.Coordinates.Dec.Should().BeApproximately(20, TOLERANCE_EPSILON.Degree);
        }

        [Test]
        public async Task Execute_PlateSolveFailed_ThrowFailedException() {
            var service = new Mock<IWindowService>();
            var captureSolver = new Mock<ICaptureSolver>();
            captureSolver.Setup(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = false });
            var centeringSolver = new Mock<ICenteringSolver>();
            centeringSolver.Setup(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = false });

            windowServiceFactoryMock.Setup(x => x.Create()).Returns(service.Object);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, AtPark = false });

            profileServiceMock.SetupGet(x => x.ActiveProfile.PlateSolveSettings).Returns(new Mock<IPlateSolveSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings).Returns(new Mock<ITelescopeSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings).Returns(new Mock<ICameraSettings>().Object);

            plateSolverFactoryMock.Setup(x => x.GetPlateSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetBlindSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetCaptureSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<IFilterWheelMediator>())).Returns(captureSolver.Object);
            plateSolverFactoryMock.Setup(x => x.GetCenteringSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<ITelescopeMediator>(), It.IsAny<IFilterWheelMediator>(), It.IsAny<IDomeMediator>(), It.IsAny<IDomeFollower>())).Returns(centeringSolver.Object);

            Func<Task> act = () => sut.Execute(default, default);

            await act.Should().ThrowAsync<Exception>().WithMessage(Loc.Instance["LblPlatesolveFailed"]);

            service.Verify(x => x.Show(It.Is<PlateSolvingStatusVM>(s => s == sut.PlateSolveStatusVM), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            service.Verify(x => x.DelayedClose(It.IsAny<TimeSpan>()), Times.Once);

            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);

            captureSolver.Verify(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_RotationFailsAfterMaxAttempts_ThrowFailedException() {
            var service = new Mock<IWindowService>();
            var captureSolver = new Mock<ICaptureSolver>();
            var coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000);
            captureSolver.Setup(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates, PositionAngle = 10 });

            var centeringSolver = new Mock<ICenteringSolver>();
            centeringSolver.Setup(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates });

            windowServiceFactoryMock.Setup(x => x.Create()).Returns(service.Object);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, AtPark = false });

            profileServiceMock.SetupGet(x => x.ActiveProfile.PlateSolveSettings).Returns(new Mock<IPlateSolveSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings).Returns(new Mock<ITelescopeSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings).Returns(new Mock<ICameraSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.RotatorSettings.RangeType).Returns(RotatorRangeTypeEnum.FULL);

            plateSolverFactoryMock.Setup(x => x.GetPlateSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetBlindSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetCaptureSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<IFilterWheelMediator>())).Returns(captureSolver.Object);
            plateSolverFactoryMock.Setup(x => x.GetCenteringSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<ITelescopeMediator>(), It.IsAny<IFilterWheelMediator>(), It.IsAny<IDomeMediator>(), It.IsAny<IDomeFollower>())).Returns(centeringSolver.Object);

            Func<Task> act = () => sut.Execute(default, default);

            await act.Should().ThrowAsync<Exception>().WithMessage(string.Format(Loc.Instance["Lbl_SequenceItem_Platesolving_CenterAndRotate_FailedAfterMaxAttempts"], 10));

            service.Verify(x => x.Show(It.Is<PlateSolvingStatusVM>(s => s == sut.PlateSolveStatusVM), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            service.Verify(x => x.DelayedClose(It.IsAny<TimeSpan>()), Times.Once);

            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);

            captureSolver.Verify(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_FullRotatorRange_PlateSolveSuccess_AlreadyCorrectlyRotated_NoException() {
            var service = new Mock<IWindowService>();
            var coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000);

            var captureSolver = new Mock<ICaptureSolver>();
            captureSolver.Setup(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates, PositionAngle = 260 });

            var centeringSolver = new Mock<ICenteringSolver>();
            centeringSolver.Setup(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates });

            windowServiceFactoryMock.Setup(x => x.Create()).Returns(service.Object);

            profileServiceMock.SetupGet(x => x.ActiveProfile.RotatorSettings.RangeType).Returns(RotatorRangeTypeEnum.FULL);
            rotatorMediatorMock.Setup(x => x.GetTargetPosition(It.IsAny<float>())).Returns(260);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, AtPark = false });

            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            profileServiceMock.SetupGet(x => x.ActiveProfile.PlateSolveSettings).Returns(new Mock<IPlateSolveSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings).Returns(new Mock<ITelescopeSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings).Returns(new Mock<ICameraSettings>().Object);

            plateSolverFactoryMock.Setup(x => x.GetPlateSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetBlindSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetCaptureSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<IFilterWheelMediator>())).Returns(captureSolver.Object);
            plateSolverFactoryMock.Setup(x => x.GetCenteringSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<ITelescopeMediator>(), It.IsAny<IFilterWheelMediator>(), It.IsAny<IDomeMediator>(), It.IsAny<IDomeFollower>())).Returns(centeringSolver.Object);

            sut.PositionAngle = 260;
            var cts = new CancellationTokenSource();
            await sut.Execute(default, cts.Token);

            service.Verify(x => x.Show(It.Is<PlateSolvingStatusVM>(s => s == sut.PlateSolveStatusVM), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            service.Verify(x => x.DelayedClose(It.IsAny<TimeSpan>()), Times.Once);
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            rotatorMediatorMock.Verify(x => x.Sync(It.Is<float>(r => r == 260)), Times.Once);
            rotatorMediatorMock.Verify(x => x.MoveRelative(It.IsAny<float>(), It.IsAny<CancellationToken>()), Times.Never);

            captureSolver.Verify(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(160, 260, -80)]
        [TestCase(160, 170, 10)]
        public async Task Execute_FullRotatorRange_PlateSolveSuccess_RotationOffOneTime_NoException(double first, double second, double movement) {
            var service = new Mock<IWindowService>();
            var coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, AtPark = false });

            var captureSolver = new Mock<ICaptureSolver>();
            captureSolver.SetupSequence(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates, PositionAngle = first })
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates, PositionAngle = second });

            var centeringSolver = new Mock<ICenteringSolver>();
            centeringSolver.Setup(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates });

            windowServiceFactoryMock.Setup(x => x.Create()).Returns(service.Object);

            profileServiceMock.SetupGet(x => x.ActiveProfile.RotatorSettings.RangeType).Returns(RotatorRangeTypeEnum.FULL);
            rotatorMediatorMock
                .SetupSequence(x => x.GetTargetPosition(It.IsAny<float>()))
                .Returns((float)(second))
                .Returns((float)(second));

            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            profileServiceMock.SetupGet(x => x.ActiveProfile.PlateSolveSettings).Returns(new Mock<IPlateSolveSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings).Returns(new Mock<ITelescopeSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings).Returns(new Mock<ICameraSettings>().Object);

            plateSolverFactoryMock.Setup(x => x.GetPlateSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetBlindSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetCaptureSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<IFilterWheelMediator>())).Returns(captureSolver.Object);
            plateSolverFactoryMock.Setup(x => x.GetCenteringSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<ITelescopeMediator>(), It.IsAny<IFilterWheelMediator>(), It.IsAny<IDomeMediator>(), It.IsAny<IDomeFollower>())).Returns(centeringSolver.Object);

            sut.PositionAngle = second;
            var cts = new CancellationTokenSource();
            await sut.Execute(default, cts.Token);

            service.Verify(x => x.Show(It.Is<PlateSolvingStatusVM>(s => s == sut.PlateSolveStatusVM), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            service.Verify(x => x.DelayedClose(It.IsAny<TimeSpan>()), Times.Once);
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            rotatorMediatorMock.Verify(x => x.Sync(It.Is<float>(r => r == first)), Times.Once);
            rotatorMediatorMock.Verify(x => x.Sync(It.Is<float>(r => r == second)), Times.Once);
            rotatorMediatorMock.Verify(x => x.MoveRelative(It.Is<float>(r => r == movement), cts.Token), Times.Once);

            captureSolver.Verify(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_FullRotatorRange_PlateSolveSuccess_AlreadyCorrectlyRotated_ButCenterFails() {
            var service = new Mock<IWindowService>();
            var coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000);

            var captureSolver = new Mock<ICaptureSolver>();
            captureSolver.Setup(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates, PositionAngle = 260 });

            var centeringSolver = new Mock<ICenteringSolver>();
            centeringSolver.Setup(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = false });

            windowServiceFactoryMock.Setup(x => x.Create()).Returns(service.Object);

            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, AtPark = false });

            profileServiceMock.SetupGet(x => x.ActiveProfile.RotatorSettings.RangeType).Returns(RotatorRangeTypeEnum.FULL);
            rotatorMediatorMock.Setup(x => x.GetTargetPosition(It.IsAny<float>())).Returns(260);

            profileServiceMock.SetupGet(x => x.ActiveProfile.PlateSolveSettings).Returns(new Mock<IPlateSolveSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings).Returns(new Mock<ITelescopeSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings).Returns(new Mock<ICameraSettings>().Object);

            plateSolverFactoryMock.Setup(x => x.GetPlateSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetBlindSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetCaptureSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<IFilterWheelMediator>())).Returns(captureSolver.Object);
            plateSolverFactoryMock.Setup(x => x.GetCenteringSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<ITelescopeMediator>(), It.IsAny<IFilterWheelMediator>(), It.IsAny<IDomeMediator>(), It.IsAny<IDomeFollower>())).Returns(centeringSolver.Object);

            sut.PositionAngle = 260;
            var cts = new CancellationTokenSource();
            Func<Task> act = () => sut.Execute(default, cts.Token);

            await act.Should().ThrowAsync<Exception>().WithMessage(Loc.Instance["LblPlatesolveFailed"]);

            service.Verify(x => x.Show(It.Is<PlateSolvingStatusVM>(s => s == sut.PlateSolveStatusVM), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            service.Verify(x => x.DelayedClose(It.IsAny<TimeSpan>()), Times.Once);
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            rotatorMediatorMock.Verify(x => x.Sync(It.Is<float>(r => r == 260)), Times.Once);
            rotatorMediatorMock.Verify(x => x.MoveRelative(It.IsAny<float>(), It.IsAny<CancellationToken>()), Times.Never);

            captureSolver.Verify(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(160, 260, 100)]
        [TestCase(160, 170, 10)]
        [TestCase(173, 355, 182)]
        public async Task Execute_HalfRotatorRange_PlateSolveSuccess_RotationOffOneTime_NoException(double first, double second, double movement) {
            var service = new Mock<IWindowService>();
            var coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000);

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, AtPark = false });

            var captureSolver = new Mock<ICaptureSolver>();
            captureSolver.SetupSequence(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates, PositionAngle = first })
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates, PositionAngle = second });

            var centeringSolver = new Mock<ICenteringSolver>();
            centeringSolver.Setup(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = coordinates });

            windowServiceFactoryMock.Setup(x => x.Create()).Returns(service.Object);

            profileServiceMock.SetupGet(x => x.ActiveProfile.RotatorSettings.RangeType).Returns(RotatorRangeTypeEnum.HALF);
            rotatorMediatorMock
                .SetupSequence(x => x.GetTargetPosition(It.IsAny<float>()))
                .Returns((float)(second))
                .Returns((float)(second));

            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var settingsMock = new Mock<IPlateSolveSettings>();
            settingsMock.SetupGet(x => x.RotationTolerance).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.PlateSolveSettings).Returns(settingsMock.Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings).Returns(new Mock<ITelescopeSettings>().Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings).Returns(new Mock<ICameraSettings>().Object);

            plateSolverFactoryMock.Setup(x => x.GetPlateSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetBlindSolver(It.IsAny<IPlateSolveSettings>())).Returns(new Mock<IPlateSolver>().Object);
            plateSolverFactoryMock.Setup(x => x.GetCaptureSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<IFilterWheelMediator>())).Returns(captureSolver.Object);
            plateSolverFactoryMock.Setup(x => x.GetCenteringSolver(It.IsAny<IPlateSolver>(), It.IsAny<IPlateSolver>(), It.IsAny<IImagingMediator>(), It.IsAny<ITelescopeMediator>(), It.IsAny<IFilterWheelMediator>(), It.IsAny<IDomeMediator>(), It.IsAny<IDomeFollower>())).Returns(centeringSolver.Object);

            sut.PositionAngle = second;
            var cts = new CancellationTokenSource();
            await sut.Execute(default, cts.Token);

            service.Verify(x => x.Show(It.Is<PlateSolvingStatusVM>(s => s == sut.PlateSolveStatusVM), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            service.Verify(x => x.DelayedClose(It.IsAny<TimeSpan>()), Times.Once);
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            rotatorMediatorMock.Verify(x => x.Sync(It.Is<float>(r => r == first)), Times.Once);
            rotatorMediatorMock.Verify(x => x.Sync(It.Is<float>(r => r == second)), Times.Once);
            rotatorMediatorMock.Verify(x => x.MoveRelative(It.Is<float>(r => r == movement), cts.Token), Times.Once);

            captureSolver.Verify(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            centeringSolver.Verify(x => x.Center(It.IsAny<CaptureSequence>(), It.IsAny<CenterSolveParameter>(), It.Is<IProgress<PlateSolveProgress>>(p => p == sut.PlateSolveStatusVM.Progress), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}