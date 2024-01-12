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
using NINA.Image.ImageData;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.WindowService;
using NINA.ViewModel;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
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
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyCamera;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.WPF.Base.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Interfaces;
using NINA.Astrometry.Body;

namespace NINA.Test.Sequencer.Trigger.Autofocus {

    [TestFixture]
    public class AutofocusAfterTimeTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<IImageHistoryVM> historyMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IAutoFocusVMFactory> autoFocusVMFactoryMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            historyMock = new Mock<IImageHistoryVM>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            autoFocusVMFactoryMock = new Mock<IAutoFocusVMFactory>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = true });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo { Connected = true });

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(4);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(1);
        }

        [Test]
        public void CloneTest() {
            var initial = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (AutofocusAfterTimeTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        [TestCase(30, 10, true)]
        [TestCase(10, 10, true)]
        [TestCase(0, 10, false)]
        [TestCase(5, 6, false)]
        [TestCase(6, 5, true)]
        public void ShouldTrigger_LastAFAsPerInput_ReturnExpected(double lastAFTime, double afterAFTime, bool shouldTrigger) {
            var afHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            var report = new AutoFocusReport() { Timestamp = DateTime.Now - TimeSpan.FromMinutes(lastAFTime) };
            var point = new ImageHistoryPoint(0, null, "LIGHT");
            point.PopulateAFPoint(report);
            afHistory.Add(point);

            var afTrigger = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            afTrigger.Amount = TimeSpan.FromMinutes(afterAFTime).TotalMinutes;

            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(afHistory);

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var should = afTrigger.ShouldTrigger(null, itemMock.Object);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(10, 15, true)]
        [TestCase(100, 5, false)]
        [TestCase(10, 10, true)]
        public async Task ShouldTrigger_NoLastAFRun(double milliseconds, double initDelay, bool shouldTrigger) {
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new AsyncObservableCollection<ImageHistoryPoint>());
            var afTrigger = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            afTrigger.Amount = TimeSpan.FromMilliseconds(milliseconds).TotalMinutes;

            afTrigger.SequenceBlockInitialize();

            await Task.Delay(TimeSpan.FromMilliseconds(initDelay));

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var should = afTrigger.ShouldTrigger(null, itemMock.Object);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var report = new AutoFocusReport();

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });

            var sut = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);

            await sut.Execute(default, default, default);

            // Todo proper assertion
            // historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var sut = new AutofocusAfterTimeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterTimeTrigger, Amount: 30m");
        }
    }
}