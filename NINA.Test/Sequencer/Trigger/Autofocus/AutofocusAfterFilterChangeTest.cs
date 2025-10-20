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
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.WindowService;
using NINA.ViewModel;
using NINA.ViewModel.ImageHistory;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NINA.Core.Model.Equipment;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyCamera;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.Core.Utility;
using NINA.WPF.Base.Model;
using NINA.Core.Model;
using static NINA.Equipment.Equipment.MyGPS.PegasusAstro.UnityApi.DriverUranusReport;

namespace NINA.Test.Sequencer.Trigger.Autofocus {

    [TestFixture]
    public class AutofocusAfterFilterChangeTest {
        private ImageDataFactoryTestUtility dataFactoryUtility;
        private Mock<IProfileService> profileServiceMock;
        private Mock<IImageHistoryVM> historyMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IAutoFocusVMFactory> autoFocusVMFactoryMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private AutofocusAfterFilterChange sut;

        [SetUp]
        public void Setup() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
            profileServiceMock = dataFactoryUtility.ProfileServiceMock;
            historyMock = new Mock<IImageHistoryVM>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            autoFocusVMFactoryMock = new Mock<IAutoFocusVMFactory>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(4);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(1);

            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = true });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo { Connected = true });
            sut = new AutofocusAfterFilterChange(profileServiceMock.Object, historyMock.Object, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
        }

        [Test]
        public void Test_Clone() {
            var result = (AutofocusAfterFilterChange)sut.Clone();

            result.Should().NotBeSameAs(sut);
            result.Icon.Should().BeSameAs(sut.Icon);
            result.Name.Should().BeSameAs(sut.Name);
        }

        [Test]
        public void Test_Initialize() {
            var filterWheelInfo = new FilterWheelInfo();
            var filter = new FilterInfo() { Name = "Filter1" };
            filterWheelInfo.SelectedFilter = filter;
            filterWheelMediatorMock.Setup(m => m.GetInfo())
                .Returns(filterWheelInfo);

            sut.Initialize();

            sut.LastAutoFocusFilter.Should().Be(filter.Name);
        }

        [Test]
        public void Test_ShouldTrigger_WhenNoChangeNoTrigger() {
            var filterWheelInfo = new FilterWheelInfo();
            var filter = new FilterInfo() { Name = "Filter1" };
            filterWheelInfo.SelectedFilter = filter;
            filterWheelMediatorMock.Setup(m => m.GetInfo())
                .Returns(filterWheelInfo);

            sut.SequenceBlockStarted();

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var result = sut.ShouldTrigger(null, itemMock.Object);

            result.Should().BeFalse();
        }

        [Test]
        public void Test_ShouldTrigger_WhenChangeThenTrigger() {
            var filterWheelInfo = new FilterWheelInfo();
            var filterInfo = new FilterInfo() {
                Name = "Test1"
            };
            filterWheelInfo.SelectedFilter = filterInfo;

            var nextFilterWheelInfo = new FilterWheelInfo();
            var nextFilterInfo = new FilterInfo() {
                Name = "Test2"
            };
            nextFilterWheelInfo.SelectedFilter = nextFilterInfo;

            filterWheelMediatorMock.SetupSequence(m => m.GetInfo())
                .Returns(filterWheelInfo)
                .Returns(nextFilterWheelInfo);

            sut.Initialize();

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var result = sut.ShouldTrigger(null, itemMock.Object);

            result.Should().BeTrue();
        }

        [Test]
        public void Test_ShouldTrigger_DifferentFilterInHistory_WhenChangeThenTrigger() {
            var filterWheelInfo = new FilterWheelInfo();
            var filterInfo = new FilterInfo() {
                Name = "Test1"
            };
            filterWheelInfo.SelectedFilter = filterInfo;

            var nextFilterWheelInfo = new FilterWheelInfo();
            var nextFilterInfo = new FilterInfo() {
                Name = "Test2"
            };
            nextFilterWheelInfo.SelectedFilter = nextFilterInfo;

            var point = new ImageHistoryPoint(1, "LIGHT");
            point.PopulateAFPoint(new AutoFocusReport() { Filter = "Test1", InitialFocusPoint = new FocusPoint() { Position = 5000 }, CalculatedFocusPoint = new FocusPoint() { Position = 5000 }, Timestamp = DateTime.UtcNow });
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new AsyncObservableCollection<ImageHistoryPoint>() {
                point
            });

            filterWheelMediatorMock.SetupSequence(m => m.GetInfo())
                .Returns(nextFilterWheelInfo)
                .Returns(nextFilterWheelInfo);

            sut.Initialize();

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var result = sut.ShouldTrigger(null, itemMock.Object);

            result.Should().BeTrue();
        }


        [Test]
        public void Test_ShouldTrigger_DifferentFilterInHistory_ButThatsAnAFFilter_WhenChangeThenNotTrigger() {
            var filterWheelInfo = new FilterWheelInfo();
            var filterInfo = new FilterInfo() {
                Name = "Test1"
            };
            filterWheelInfo.SelectedFilter = filterInfo;

            var nextFilterWheelInfo = new FilterWheelInfo();
            var nextFilterInfo = new FilterInfo() {
                Name = "Test2"
            };
            nextFilterWheelInfo.SelectedFilter = nextFilterInfo;

            profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>() { new FilterInfo() { Name = "Test1", AutoFocusFilter = true } });
            var point = new ImageHistoryPoint(1, "LIGHT");
            point.PopulateAFPoint(new AutoFocusReport() { Filter = "Test1", InitialFocusPoint = new FocusPoint() { Position = 5000 }, CalculatedFocusPoint = new FocusPoint() { Position = 5000 }, Timestamp = DateTime.UtcNow });
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new AsyncObservableCollection<ImageHistoryPoint>() {
                point
            });

            filterWheelMediatorMock.SetupSequence(m => m.GetInfo())
                .Returns(nextFilterWheelInfo)
                .Returns(nextFilterWheelInfo);

            sut.Initialize();

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var result = sut.ShouldTrigger(null, itemMock.Object);

            result.Should().BeFalse();
        }


        [Test]
        public void Test_ShouldTrigger_SameFilterInHistory_WhenChangeThenTrigger() {
            var filterWheelInfo = new FilterWheelInfo();
            var filterInfo = new FilterInfo() {
                Name = "Test1"
            };
            filterWheelInfo.SelectedFilter = filterInfo;

            var nextFilterWheelInfo = new FilterWheelInfo();
            var nextFilterInfo = new FilterInfo() {
                Name = "Test2"
            };
            nextFilterWheelInfo.SelectedFilter = nextFilterInfo;

            var point = new ImageHistoryPoint(1, "LIGHT");
            point.PopulateAFPoint(new AutoFocusReport() { Filter = "Test2", InitialFocusPoint = new FocusPoint() { Position = 5000 }, CalculatedFocusPoint = new FocusPoint() { Position = 5000 }, Timestamp = DateTime.UtcNow });
            historyMock.SetupGet(x => x.AutoFocusPoints).Returns(new AsyncObservableCollection<ImageHistoryPoint>() {
                point
            });

            filterWheelMediatorMock.SetupSequence(m => m.GetInfo())
                .Returns(filterWheelInfo)
                .Returns(nextFilterWheelInfo);

            sut.Initialize();

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var result = sut.ShouldTrigger(null, itemMock.Object);

            result.Should().BeFalse();
        }

        [Test]
        public void Test_ShouldTrigger_WhenAlwaysNullThenNoTrigger() {
            sut.SequenceBlockStarted();

            var result = sut.ShouldTrigger(null, new Mock<IExposureItem>().Object);

            result.Should().BeFalse();
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var report = new AutoFocusReport();

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });

            await sut.Execute(default, default, default);

            // Todo proper assertion
            //historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterFilterChange");
        }
    }
}