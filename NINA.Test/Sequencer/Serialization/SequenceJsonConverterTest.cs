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
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Serialization {
    //[TestFixture]
    //public class SequenceJsonConverterTest {
    //    private Mock<IProfileService> profileServiceMock;
    //    private Mock<ICameraMediator> cameraMediatorMock;
    //    private Mock<ITelescopeMediator> telescopeMediatorMock;
    //    private Mock<IFocuserMediator> focuserMediatorMock;
    //    private Mock<IFilterWheelMediator> filterWheelMediatorMock;
    //    private Mock<IGuiderMediator> guiderMediatorMock;
    //    private Mock<IRotatorMediator> rotatorMediatorMock;
    //    private Mock<IWeatherDataMediator> weatherDataMediatorMock;
    //    private Mock<IImagingMediator> imagingMediatorMock;
    //    private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
    //    private Mock<IFlatDeviceMediator> flatDeviceMediatorMock;
    //    private Mock<INighttimeCalculator> nighttimeCalculatorMock;
    //    private Mock<IPlanetariumFactory> planetariumFactoryMock;
    //    private Mock<IImageHistoryVM> imageHistoryVMMock;
    //    private Mock<IDeepSkyObjectSearchVM> deepSkyObjectSearchVMMock;
    //    private Mock<IDomeMediator> domeMediatorMock;
    //    private Mock<IImageSaveMediator> imageSaveMediatorMock;
    //    private Mock<IApplicationResourceDictionary> resourceMock;
    //    private Mock<ISwitchMediator> switchMediatorMock;
    //    private Mock<ISafetyMonitorMediator> safetyMonitorMock;
    //    private Mock<IApplicationMediator> applicationMediatorMock;
    //    private Mock<IFramingAssistantVM> framingAssistantVMMock;

    //    [Test]
    //    public void AllAvailableItems_Serialize_Deserialize_Successfully() {
    //        profileServiceMock = new Mock<IProfileService>();
    //        cameraMediatorMock = new Mock<ICameraMediator>();
    //        telescopeMediatorMock = new Mock<ITelescopeMediator>();
    //        focuserMediatorMock = new Mock<IFocuserMediator>();
    //        filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
    //        guiderMediatorMock = new Mock<IGuiderMediator>();
    //        rotatorMediatorMock = new Mock<IRotatorMediator>();
    //        flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
    //        weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
    //        imagingMediatorMock = new Mock<IImagingMediator>();
    //        applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
    //        nighttimeCalculatorMock = new Mock<INighttimeCalculator>();
    //        planetariumFactoryMock = new Mock<IPlanetariumFactory>();
    //        imageHistoryVMMock = new Mock<IImageHistoryVM>();
    //        deepSkyObjectSearchVMMock = new Mock<IDeepSkyObjectSearchVM>();
    //        domeMediatorMock = new Mock<IDomeMediator>();
    //        imageSaveMediatorMock = new Mock<IImageSaveMediator>();
    //        resourceMock = new Mock<IApplicationResourceDictionary>();
    //        switchMediatorMock = new Mock<ISwitchMediator>();
    //        safetyMonitorMock = new Mock<ISafetyMonitorMediator>();
    //        applicationMediatorMock = new Mock<IApplicationMediator>();
    //        framingAssistantVMMock = new Mock<IFramingAssistantVM>();

    //        profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(10);
    //        profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(10);
    //        profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.AutoFocusAfterFlip).Returns(false);
    //        profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings).Returns(new Mock<IFilterWheelSettings>().Object);
    //        cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyCamera.CameraInfo() { Connected = false });
    //        telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyTelescope.TelescopeInfo() { Connected = false });
    //        focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyFocuser.FocuserInfo() { Connected = false });
    //        guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyGuider.GuiderInfo() { Connected = false });
    //        rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyRotator.RotatorInfo() { Connected = false });
    //        flatDeviceMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyFlatDevice.FlatDeviceInfo() { Connected = false });
    //        weatherDataMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyWeatherData.WeatherDataInfo() { Connected = false });
    //        switchMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MySwitch.SwitchInfo() { Connected = false });
    //        filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyFilterWheel.FilterWheelInfo() { Connected = false });
    //        domeMediatorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MyDome.DomeInfo() { Connected = false });
    //        safetyMonitorMock.Setup(x => x.GetInfo()).Returns(new NINA.Model.MySafetyMonitor.SafetyMonitorInfo() { Connected = false });

    //        var factory = new SequencerFactory(
    //                profileServiceMock.Object,
    //                cameraMediatorMock.Object,
    //                telescopeMediatorMock.Object,
    //                focuserMediatorMock.Object,
    //                filterWheelMediatorMock.Object,
    //                guiderMediatorMock.Object,
    //                rotatorMediatorMock.Object,
    //                flatDeviceMediatorMock.Object,
    //                weatherDataMediatorMock.Object,
    //                imagingMediatorMock.Object,
    //                applicationStatusMediatorMock.Object,
    //                nighttimeCalculatorMock.Object,
    //                planetariumFactoryMock.Object,
    //                imageHistoryVMMock.Object,
    //                deepSkyObjectSearchVMMock.Object,
    //                domeMediatorMock.Object,
    //                imageSaveMediatorMock.Object,
    //                switchMediatorMock.Object,
    //                safetyMonitorMock.Object,
    //                resourceMock.Object,
    //                applicationMediatorMock.Object,
    //                framingAssistantVMMock.Object

    //                );

    //        var testContainer = factory.GetContainer<SequenceRootContainer>();

    //        foreach (var container in factory.Container) {
    //            var c = (ISequenceContainer)container.Clone();
    //            if (!(c is SequenceRootContainer)) {
    //                foreach (var item in factory.Items) {
    //                    c.Add((ISequenceItem)item.Clone());
    //                }
    //                if (c is IConditionable) {
    //                    var cond = c as IConditionable;
    //                    foreach (var condition in factory.Conditions) {
    //                        cond.Add((ISequenceCondition)condition.Clone());
    //                    }
    //                }

    //                if (c is ITriggerable) {
    //                    var t = c as ITriggerable;
    //                    foreach (var trigger in factory.Triggers) {
    //                        t.Add((ISequenceTrigger)trigger.Clone());
    //                    }
    //                }
    //            }
    //            testContainer.Add(c);
    //        }

    //        var sut = new SequenceJsonConverter(factory);
    //        var json = sut.Serialize(testContainer);
    //        var newContainer = sut.Deserialize(json);

    //        // Might add more validations that items are properly inside later
    //        newContainer.Should().NotBeNull();
    //    }
    //}
}