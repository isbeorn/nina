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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.WindowService;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.Core.Model.Equipment;
using NINA.Core.Model;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Image.ImageAnalysis;

namespace NINA.Test.Sequencer.SequenceItem.Autofocus {

    [TestFixture]
    internal class RunAutofocusTest {
        private ImageDataFactoryTestUtility dataFactoryUtility;
        private Mock<IProfileService> profileServiceMock;
        private Mock<IImageHistoryVM> historyMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IAutoFocusVMFactory> autofocusVMFactoryMock;
        private RunAutofocus sut;

        [SetUp]
        public void Setup() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
            profileServiceMock = dataFactoryUtility.ProfileServiceMock;
            historyMock = new Mock<IImageHistoryVM>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            autofocusVMFactoryMock = new Mock<IAutoFocusVMFactory>();
            sut = new RunAutofocus(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autofocusVMFactoryMock.Object);
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (RunAutofocus)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Validate_NoIssues() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true });

            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        [TestCase(true, false, 1)]
        [TestCase(false, true, 1)]
        [TestCase(false, false, 2)]
        public void Validate_NotConnected_HasIssues(bool cameraConnected, bool focuserConnected, int issues) {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = cameraConnected });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = focuserConnected });

            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(issues);
        }

        [Test]
        [TestCase(10, 5, 1, 0, 144)]
        [TestCase(10, 5, 1, 5, 204)]
        [TestCase(10, 5, 3, 0, 384)]
        [TestCase(10, 5, 3, 5, 544)]
        public void GetEstimatedDuration_WithFilterTime_ReturnsCorrectEstimate(double filterTime, int initialSteps, int framesPerPoint, int settleTime, double expectedDuration) {
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = new FilterInfo() { Position = 0 } });
            profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>() { new FilterInfo() { Position = 0, AutoFocusExposureTime = filterTime } });
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(initialSteps);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(framesPerPoint);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(settleTime);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromSeconds(expectedDuration));
        }

        [Test]
        [TestCase(10, 5, 1, 0, 144)]
        [TestCase(10, 5, 1, 5, 204)]
        [TestCase(10, 5, 3, 0, 384)]
        [TestCase(10, 5, 3, 5, 544)]
        public void GetEstimatedDuration_WithDefaultTime_ReturnsCorrectEstimate(double defaultTime, int initialSteps, int framesPerPoint, int settleTime, double expectedDuration) {
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = null });
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(defaultTime);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(initialSteps);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(framesPerPoint);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(settleTime);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromSeconds(expectedDuration));
        }

        [Test]
        [TestCase(10, 5, 1, 0, 144)]
        [TestCase(10, 5, 1, 5, 204)]
        [TestCase(10, 5, 3, 0, 384)]
        [TestCase(10, 5, 3, 5, 544)]
        public void GetEstimatedDuration_WithFilterTimeZeroFallback_ReturnsCorrectEstimate(double defaultTime, int initialSteps, int framesPerPoint, int settleTime, double expectedDuration) {
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = new FilterInfo() { Position = 0 } });
            profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>() { new FilterInfo() { Position = 0, AutoFocusExposureTime = 0 } });
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(defaultTime);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(initialSteps);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(framesPerPoint);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(settleTime);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromSeconds(expectedDuration));
        }

        [Test]
        [TestCase(10, 5, 1, 0, 2, 1, 288)]
        [TestCase(10, 5, 1, 5, 2, 1, 408)]
        [TestCase(10, 5, 3, 0, 2, 1, 768)]
        [TestCase(10, 5, 3, 5, 2, 1, 1088)]
        [TestCase(10, 5, 1, 0, 0, 2, 288)]
        [TestCase(10, 5, 1, 5, 0, 2, 408)]
        [TestCase(10, 5, 3, 0, 0, 2, 768)]
        [TestCase(10, 5, 3, 5, 0, 2, 1088)]
        public void GetEstimatedDuration_WithFilterTimeZeroFallback_MultiAttempts_ReturnsCorrectEstimate(double defaultTime, int initialSteps, int framesPerPoint, int settleTime, int instructionAttempts, int profileAttempts, double expectedDuration) {
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = new FilterInfo() { Position = 0 } });
            profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>() { new FilterInfo() { Position = 0, AutoFocusExposureTime = 0 } });
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(defaultTime);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(initialSteps);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(framesPerPoint);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(settleTime);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusTotalNumberOfAttempts).Returns(profileAttempts);

            sut.Attempts = instructionAttempts;

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromSeconds(expectedDuration));
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var windowMock = new Mock<IWindowService>();
            var windowFactoryMock = new Mock<IWindowServiceFactory>();
            windowFactoryMock.Setup(x => x.Create()).Returns(windowMock.Object);

            var report = new AutoFocusReport();
            var autofocusMock = new Mock<IAutoFocusVM>();
            autofocusMock.Setup(af => af.StartAutoFocus(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>())).Returns(Task.FromResult(report));
            autofocusVMFactoryMock.Setup(x => x.Create()).Returns(autofocusMock.Object);

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });
            var profileFilter = new FilterInfo() { Position = 0 };
            profileServiceMock.Setup(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>() { profileFilter });

            sut.WindowServiceFactory = windowFactoryMock.Object;

            await sut.Execute(default, default);

            windowFactoryMock.Verify(x => x.Create(), Times.Once);
            windowMock.Verify(x => x.Show(It.Is<IAutoFocusVM>(o => o == autofocusMock.Object), It.Is<string>(t => t == NINA.Core.Locale.Loc.Instance["LblAutoFocus"]), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            windowMock.Verify(x => x.DelayedClose(It.Is<TimeSpan>(t => t.TotalSeconds == 10)), Times.Once);

            autofocusMock.Verify(x => x.StartAutoFocus(It.Is<FilterInfo>(f => f == profileFilter), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
            historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public async Task Execute_UnSuccessfully_WithAllParametersPassedCorrectly() {
            var windowMock = new Mock<IWindowService>();
            var windowFactoryMock = new Mock<IWindowServiceFactory>();
            windowFactoryMock.Setup(x => x.Create()).Returns(windowMock.Object);

            var autofocusMock = new Mock<IAutoFocusVM>();
            autofocusMock.Setup(af => af.StartAutoFocus(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>())).Returns(Task.FromResult<AutoFocusReport>(null));
            autofocusVMFactoryMock.Setup(x => x.Create()).Returns(autofocusMock.Object);

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });
            var profileFilter = new FilterInfo() { Position = 0 };
            profileServiceMock.Setup(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>() { profileFilter });

            sut.WindowServiceFactory = windowFactoryMock.Object;

            Func<Task> act = () => sut.Execute(default, default);

            await act.Should().ThrowAsync<SequenceEntityFailedException>();

            windowFactoryMock.Verify(x => x.Create(), Times.Once);
            windowMock.Verify(x => x.Show(It.Is<IAutoFocusVM>(o => o == autofocusMock.Object), It.Is<string>(t => t == NINA.Core.Locale.Loc.Instance["LblAutoFocus"]), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()), Times.Once);
            windowMock.Verify(x => x.DelayedClose(It.Is<TimeSpan>(t => t.TotalSeconds == 10)), Times.Once);

            autofocusMock.Verify(x => x.StartAutoFocus(It.Is<FilterInfo>(f => f == profileFilter), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
            historyMock.Verify(h => h.AppendAutoFocusPoint(It.IsAny<AutoFocusReport>()), Times.Never);
        }

        [Test]
        public void AfterParentChanged_ValidateIsCalled() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = false });
            sut.AfterParentChanged();
            sut.Issues.Count.Should().BeGreaterThan(0);
        }

        [Test]
        public void ToString_FilledProperly() {
            sut.Category = "Autofocus";
            var tostring = sut.ToString();
            tostring.Should().Be("Category: Autofocus, Item: RunAutofocus");
        }
    }
}