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
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.WindowService;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Core.Utility;
using NINA.Core.Model.Equipment;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Interfaces;
using NINA.ViewModel.ImageHistory;

namespace NINA.Test.Sequencer.Trigger.Autofocus {

    [TestFixture]
    public class AutofocusAfterHFRIncreaseTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IAutoFocusVMFactory> autoFocusVMFactoryMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;

        private ImageHistoryVM imagehistory;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            autoFocusVMFactoryMock = new Mock<IAutoFocusVMFactory>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = true });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo { Connected = true });
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(4);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>() { new FilterInfo() { AutoFocusExposureTime = 1 } });
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected).Returns(Core.Enum.ImageHistoryEnum.HFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected).Returns(Core.Enum.ImageHistoryEnum.Stars);

            imagehistory = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);
        }

        [Test]
        public void CloneTest() {
            var initial = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();
            initial.SampleSize = 15;
            initial.Amount = 33;

            var sut = (AutofocusAfterHFRIncreaseTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
            sut.SampleSize.Should().Be(15);
            sut.Amount.Should().Be(33);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ShouldTrigger_HistoryNotLargeEnough_False(int sampleSize) {
            for (int i = 0; i < sampleSize; i++) {
                imagehistory.Add(imagehistory.GetNextImageId(), null, "LIGHT");
            }

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
            sut.Amount = 1;

            var trigger = sut.ShouldTrigger(null, null);

            trigger.Should().BeFalse();
        }

        [Test]
        [TestCase(new double[] { 3, 3, 3, 10 }, 1, true)]
        [TestCase(new double[] { 3, 3, 3, 3 }, 1, false)]
        [TestCase(new double[] { 3, 3.1, 2.9, 3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 50, false)]
        [TestCase(new double[] { 3, 2.9, 2.8, 2.7 }, 1, false)]
        [TestCase(new double[] { 3.4, 2.9, 3.1, 2.7, 3.3, 3.0, 3.5 }, 10, true)]
        [TestCase(new double[] { 2.068, 1.968, 2.016, 2.053, 2.044, 2.084, 2.060, 2.048, 2.131, 2.063 }, 8, false)]
        public void ShouldTrigger_HistoryExists_NoPreviousAFs_True(double[] hfrs, double changeAmount, bool shouldTrigger) {
            for (int i = 0; i < hfrs.Length; i++) {
                var p = new ImageHistoryPoint(i, null, "LIGHT");
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, null, "LIGHT");
                imagehistory.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = hfrs[i] }, MetaData = new ImageMetaData() { Image = new ImageParameter() { Id = id } } });
            }

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
            sut.Amount = changeAmount;

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var trigger = sut.ShouldTrigger(null, itemMock.Object);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(new double[] { 3, 99, 99, 3, 3, 3, 10 }, 1, true)]
        [TestCase(new double[] { 3, 99, 99, 3, 3, 3, 3 }, 1, false)]
        [TestCase(new double[] { 3, 99, 99, 3, 3.1, 2.9, 3 }, 1, true)]
        [TestCase(new double[] { 3, 99, 99, 3, 3.1, 3.2, 3.3 }, 1, true)]
        [TestCase(new double[] { 3, 99, 99, 3, 3.1, 3.2, 3.3 }, 50, false)]
        [TestCase(new double[] { 3, 99, 99, 3, 2.9, 2.8, 2.7 }, 1, false)]
        public void ShouldTrigger_HistoryExists_NoPreviousAFsButSampleSize_True(double[] hfrs, double changeAmount, bool shouldTrigger) {
            for (int i = 0; i < hfrs.Length; i++) {
                var p = new ImageHistoryPoint(i, null, "LIGHT");
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, null, "LIGHT");
                imagehistory.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = hfrs[i] }, MetaData = new ImageMetaData() { Image = new ImageParameter() { Id = id } } });
            }

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
            sut.SampleSize = 4;
            sut.Amount = changeAmount;

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var trigger = sut.ShouldTrigger(null, itemMock.Object);

            trigger.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(new double[] { 3, 3, 3, 10 }, 1, true)]
        [TestCase(new double[] { 3, 3, 3, 3 }, 1, false)]
        [TestCase(new double[] { 3, 3.1, 2.9, 3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 3.2, 3.3 }, 50, false)]
        [TestCase(new double[] { 3, 2.9, 2.8, 2.7 }, 1, false)]
        [TestCase(new double[] { 2.068, 1.968, 2.016, 2.053, 2.044, 2.084, 2.060, 2.048, 2.131, 2.063 }, 8, false)]
        public void ShouldTrigger_HistoryExists_PreviousAFsExists_True(double[] hfrs, double changeAmount, bool shouldTrigger) {
            imagehistory.Add(imagehistory.GetNextImageId(), null, "LIGHT");
            imagehistory.Add(imagehistory.GetNextImageId(), null, "LIGHT");
            imagehistory.Add(imagehistory.GetNextImageId(), null, "LIGHT");
            imagehistory.Add(imagehistory.GetNextImageId(), null, "LIGHT");

            imagehistory.AppendAutoFocusPoint(new AutoFocusReport() {
                InitialFocusPoint = new FocusPoint() { Position = 1000 },
                CalculatedFocusPoint = new FocusPoint() { Position = 1200 },
                Temperature = 10,
                Timestamp = DateTime.Now
            });
            for (int i = 0; i < hfrs.Length; i++) {
                var p = new ImageHistoryPoint(i, null, "LIGHT");
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, null, "LIGHT");
                imagehistory.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1 + 5, HFR = hfrs[i] }, MetaData = new ImageMetaData() { Image = new ImageParameter() { Id = id } } });
            }

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
            sut.Amount = changeAmount;

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

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);

            await sut.Execute(default, default, default);

            // Todo proper assertion
            // historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterHFRIncreaseTrigger, Amount: 5");
        }

        [Test]
        [TestCase(new double[] { 3, 3, 100, 100, 10 }, 1, true)] // index 2+3 are for a different filter
        [TestCase(new double[] { 3, 3, 100, 100, 3, 3 }, 1, false)]
        [TestCase(new double[] { 3, 3.1, 100, 100, 2.9, 3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 100, 100, 3.2, 3.3 }, 1, true)]
        [TestCase(new double[] { 3, 3.1, 100, 100, 3.2, 3.3 }, 50, false)]
        [TestCase(new double[] { 3, 2.9, 100, 100, 2.8, 2.7 }, 1, false)]
        [TestCase(new double[] { 3.4, 2.9, 100, 100, 3.1, 2.7, 3.3, 3.0, 3.5 }, 10, true)]
        [TestCase(new double[] { 2.068, 1.968, 100, 100, 2.016, 2.053, 2.044, 2.084, 2.060, 2.048, 2.131, 2.063 }, 8, false)]
        public void ShouldTrigger_HistoryExists_NoPreviousAFs_OnlyTestFilterConsidered_True(double[] hfrs, double changeAmount, bool shouldTrigger) {
            for (int i = 0; i < hfrs.Length; i++) {
                if (i > 1 && i < 4) {
                    var p = new ImageHistoryPoint(i, null, "LIGHT");
                    var id = imagehistory.GetNextImageId();
                    imagehistory.Add(id, null, "LIGHT");
                    imagehistory.AppendImageProperties(new ImageSavedEventArgs() { Filter = "OtherFilter", StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = hfrs[i] }, MetaData = new ImageMetaData() { Image = new ImageParameter() { Id = id } } });
                } else {
                    var p = new ImageHistoryPoint(i, null, "LIGHT");
                    var id = imagehistory.GetNextImageId();
                    imagehistory.Add(id, null, "LIGHT");
                    imagehistory.AppendImageProperties(new ImageSavedEventArgs() { Filter = "TestFilter", StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = hfrs[i] }, MetaData = new ImageMetaData() { Image = new ImageParameter() { Id = id } } });
                }
            }

            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true, SelectedFilter = new FilterInfo() { Name = "TestFilter" } });

            var sut = new AutofocusAfterHFRIncreaseTrigger(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
            sut.Amount = changeAmount;

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var trigger = sut.ShouldTrigger(null, itemMock.Object);

            trigger.Should().Be(shouldTrigger);
        }



        [Test]
        // stable - 0%
        [TestCase(new double[] { 2.0, 2.0, 2.0, 2.0, 2.0 }, 4, 2.00, 0.00)]
        // slight drift - +5%
        [TestCase(new double[] { 2.0, 2.02, 2.05, 2.07, 2.10 }, 4, 2.10, 5.00)]
        // linear drift - +20%
        [TestCase(new double[] { 2.0, 2.1, 2.2, 2.3, 2.4 }, 4, 2.40, 20.00)]
        // improving - 0% (min baseline)
        [TestCase(new double[] { 2.5, 2.4, 2.3, 2.2, 2.1 }, 4, 2.10, 0.00)]
        // stable due to sample size
        [TestCase(new double[] { 2.2, 2.40, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00 }, 4, 2.0, 0)]
        // Stable - no change
        [TestCase(new double[] { 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0 }, 10, 2.00, 0.00)]
        // Linear slight drift: +0.02 per frame 
        [TestCase(new double[] { 2.00, 2.02, 2.04, 2.06, 2.08, 2.10, 2.12, 2.14, 2.16, 2.18 }, 10, 2.18, 9.00)]
        // Linear stronger drift: +0.1 per frame
        [TestCase(new double[] { 2.0, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9 }, 10, 2.90, 45.00)]
        // Linear improvement: negative not possible w/ min-baseline - 0% 
        [TestCase(new double[] { 2.9, 2.8, 2.7, 2.6, 2.5, 2.4, 2.3, 2.2, 2.1, 2.0 }, 10, 2.00, 0.00)]
        // Early low outlier lowers the min-baseline - higher % at the end
        [TestCase(new double[] { 1.80, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00, 2.00 }, 10, 2.02909, 12.73)]
        // Slight linear drift from 1.95 to 2.13 (0.02 step, 10 pts)
        [TestCase(new double[] { 1.95, 1.97, 1.99, 2.01, 2.03, 2.05, 2.07, 2.09, 2.11, 2.13 }, 10, 2.13, 9.23)]
        public void HFRTrend_And_Percentage_AreComputed_FromWindowAndBaseline(
            double[] hfrs,
            int sampleSize,
            double expectedTrend,
            double expectedPct) {
            for (int i = 0; i < hfrs.Length; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, null, "LIGHT");
                imagehistory.AppendImageProperties(new ImageSavedEventArgs {
                    StarDetectionAnalysis = new StarDetectionAnalysis {
                        DetectedStars = 1,
                        HFR = hfrs[i]
                    },
                    MetaData = new ImageMetaData {
                        Image = new ImageParameter { Id = id }
                    }
                });
            }

            var sut = new AutofocusAfterHFRIncreaseTrigger(
                profileServiceMock.Object,
                imagehistory,
                cameraMediatorMock.Object,
                filterWheelMediatorMock.Object,
                focuserMediatorMock.Object,
                autoFocusVMFactoryMock.Object) {
                SampleSize = sampleSize,
                Amount = 0.0
            };

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");

            _ = sut.ShouldTrigger(null, itemMock.Object);

            sut.HFRTrend.Should().BeApproximately(expectedTrend, 0.01);
            sut.HFRTrendPercentage.Should().BeApproximately(expectedPct, 0.05);
        }
    }
}