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
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyCamera;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Interfaces;

namespace NINA.Test.Sequencer.Trigger.Autofocus {

    [TestFixture]
    public class AutofocusAfterTemperatureChangeTriggerTest {
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
            var initial = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (AutofocusAfterTemperatureChangeTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        [TestCase(40, 1, 40, false)]
        [TestCase(40, 1, 41, true)]
        [TestCase(40, 1, 39, true)]
        [TestCase(50, 10, 40, true)]
        [TestCase(50, 5, 52, false)]
        [TestCase(50, 5, 48, false)]
        public void ShouldTrigger_LastAFExists(double initialTemp, double tempAmount, double changedTemp, bool shouldTrigger) {
            var afHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            var report = new AutoFocusReport() { Temperature = initialTemp };
            var point = new ImageHistoryPoint(0, null, "LIGHT");
            point.PopulateAFPoint(report);
            afHistory.Add(point);
            historyMock.SetupGet(x => x.ImageHistory).Returns(afHistory.ToList());
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(afHistory);

            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Temperature = changedTemp });

            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            sut.Amount = tempAmount;

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var trigger = sut.ShouldTrigger(null, itemMock.Object);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(40, 1, 40, false)]
        [TestCase(40, 1, 41, false)]
        [TestCase(40, 1, 39, false)]
        [TestCase(50, 10, 40, false)]
        [TestCase(50, 5, 52, false)]
        [TestCase(50, 5, 48, false)]
        public void ShouldTrigger_LastAFDoesNotExists_NoHistoryExists_NeverFocus(double initialTemp, double tempAmount, double changedTemp, bool shouldTrigger) {
            historyMock.SetupGet(x => x.ImageHistory).Returns(new List<ImageHistoryPoint>());
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new AsyncObservableCollection<ImageHistoryPoint>());

            focuserMediatorMock.SetupSequence(x => x.GetInfo())
                .Returns(new FocuserInfo() { Connected = true, Temperature = initialTemp })
                .Returns(new FocuserInfo() { Temperature = initialTemp })
                .Returns(new FocuserInfo() { Temperature = changedTemp });

            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            sut.Initialize();
            sut.Amount = tempAmount;

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var trigger = sut.ShouldTrigger(null, itemMock.Object);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(40, 1, 40, false)]
        [TestCase(40, 1, 41, true)]
        [TestCase(40, 1, 39, true)]
        [TestCase(50, 10, 40, true)]
        [TestCase(50, 5, 52, false)]
        [TestCase(50, 5, 48, false)]
        public void ShouldTrigger_LastAFDoesNotExists_ButHistoryExists_(double initialTemp, double tempAmount, double changedTemp, bool shouldTrigger) {
            historyMock.SetupGet(x => x.ImageHistory).Returns(new List<ImageHistoryPoint>() { new ImageHistoryPoint(1, null, "LIGHT") });
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new AsyncObservableCollection<ImageHistoryPoint>());

            focuserMediatorMock.SetupSequence(x => x.GetInfo())
                .Returns(new FocuserInfo() { Connected = true, Temperature = initialTemp })
                .Returns(new FocuserInfo() { Temperature = initialTemp })
                .Returns(new FocuserInfo() { Temperature = changedTemp });

            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            sut.Initialize();
            sut.Amount = tempAmount;

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var trigger = sut.ShouldTrigger(null, itemMock.Object);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var report = new AutoFocusReport();

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });

            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);

            await sut.Execute(default, default, default);

            // Todo proper assertion
            // historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var sut = new AutofocusAfterTemperatureChangeTrigger(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            sut = (AutofocusAfterTemperatureChangeTrigger)sut.Clone();
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterTemperatureChangeTrigger, Amount: 5°");
        }
    }
}