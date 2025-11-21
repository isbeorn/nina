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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Logic;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Logic {

    [TestFixture]


 //   public SymbolBroker(IProfileService profileService, ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
 //           IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IRotatorMediator rotatorMediator, ISafetyMonitorMediator safetyMonitorMediator,
 //           IFocuserMediator focuserMediator, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator, IImagingMediator imagingMediator) : base(profileService) {



    public class SymbolProviderTest {

        private Mock<IProfileService> profileServiceMock;
        private Mock<ISwitchMediator> switchMediatorMock;
        private Mock<IWeatherDataMediator> weatherDataMediatorMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IFlatDeviceMediator> flatDeviceMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IRotatorMediator> rotatorMediatorMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            switchMediatorMock = new Mock<ISwitchMediator>();
            weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            rotatorMediatorMock = new Mock<IRotatorMediator>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
        }

        [Test]
        public void RegisterSymbolProvider() {
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var sut = broker.RegisterSymbolProvider("Plugin");
            sut.Should().GetType().Should().NotBeNull();
            sut.GetProviderName().Should().Be("Plugin");

            // Make sure duplicates aren't allowed
            Action test = () => broker.RegisterSymbolProvider("Plugin");
            test.Should().Throw<ArgumentException>();

            // Make sure built in provider isn't allowed
            test = () => broker.RegisterSymbolProvider("NINA");
            test.Should().Throw<ArgumentException>();
        }
        [Test]
        public void TestAddRemoveSymbols() {
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var sut = broker.RegisterSymbolProvider("Plugin");
            sut.Should().GetType().Should().NotBeNull();

            sut.AddOrUpdateSymbol("A", 10);
            sut.AddOrUpdateSymbol("B", "String value");
            sut.AddOrUpdateSymbol("C", 22.77);

            Symbol sym;
            broker.TryGetSymbol("A", out sym);
            sym.Should().NotBeNull();
            sym.Value.Should().Be(10);
            broker.TryGetSymbol("B", out sym).Should().BeTrue();
            sym.Should().NotBeNull();
            sym.Value.Should().Be("String value");
            broker.TryGetSymbol("C", out sym);
            sym.Should().NotBeNull();
            sym.Value.Should().Be(22.77);

            sut.RemoveSymbol("B").Should().BeTrue();
            broker.TryGetSymbol("B", out sym).Should().BeFalse();
        }
        
        [Test]
        public void TestAmbiguousSymbols() {
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var sut1 = broker.RegisterSymbolProvider("Plugin1");
            var sut2 = broker.RegisterSymbolProvider("Plugin2");
            sut1.Should().GetType().Should().NotBeNull();

            sut1.AddOrUpdateSymbol("A", 10);
            sut2.AddOrUpdateSymbol("A", "String value");

            Symbol sym;
            broker.TryGetSymbol("A", out sym);
            sym.Should().NotBeNull();
            sym.GetType().Should().Be(typeof(AmbiguousSymbol));
            Symbol[] symbols = sym.Constants;
            symbols.Count().Should().Be(2);

            broker.TryGetSymbol("Plugin1_A", out sym).Should().BeTrue();
            sym.Value.Should().Be(10);
            broker.TryGetSymbol("Plugin2_A", out sym).Should().BeTrue();
            sym.Value.Should().Be("String value");

            broker.TryGetSymbol("Plugin3_A", out sym).Should().BeFalse();
        }
    }
}