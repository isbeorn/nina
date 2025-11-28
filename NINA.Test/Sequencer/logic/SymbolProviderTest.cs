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
using NCalc.Handlers;
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
using System.Linq.Expressions;
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
            symbols.Length.Should().Be(2);

            broker.TryGetSymbol("Plugin1_A", out sym).Should().BeTrue();
            sym.Value.Should().Be(10);
            broker.TryGetSymbol("Plugin2_A", out sym).Should().BeTrue();
            sym.Value.Should().Be("String value");

            broker.TryGetSymbol("Plugin3_A", out sym).Should().BeFalse();
        }

        [Test]
        public void TestHiddenSymbols() {
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var sut1 = broker.RegisterSymbolProvider("Plugin1");
            sut1.Should().GetType().Should().NotBeNull();

            Symbol[] pierConstants = new Symbol[] { new Symbol("PierUnknown", -1), new Symbol("PierEast", 0), new Symbol("PierWest", 1) };

            sut1.AddOrUpdateSymbol("PierStatus", 1, pierConstants);

            Symbol sym;
            broker.TryGetSymbol("PierStatus", out sym).Should().BeTrue();
            //sym.Value.Should().Be(1);

            broker.TryGetSymbol("PierUnknown", out sym).Should().BeTrue();
            sym.Value.Should().Be(-1);
            broker.TryGetSymbol("PierEast", out sym).Should().BeTrue();
            sym.Value.Should().Be(0);
            broker.TryGetSymbol("PierWest", out sym).Should().BeTrue();
            sym.Value.Should().Be(1);
            broker.TryGetSymbol("Plugin1_PierWest", out sym).Should().BeTrue();
            sym.Value.Should().Be(1);
        }



        [Test]
        public void RegisterFunction_ThenInvoke_ShouldReturnExpectedResult() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var fn = new SymbolFunction(
                name: "const42",
                description: "",
                usageExample: "",
                implementation: args => 42,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            broker.RegisterFunction(fn);

            var args = new FunctionArgs(Guid.NewGuid(), []);

            // act
            var success = broker.TryInvokeFunction("const42", args, out var result, out var isVolatile);

            // assert
            success.Should().BeTrue("the function was registered and should be found");
            result.Should().Be(42);
            isVolatile.Should().BeFalse("this function was registered as non-volatile");
        }

        [Test]
        public void TryInvokeFunction_UnknownFunction_ShouldReturnFalseAndNullResult() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var args = new FunctionArgs(Guid.NewGuid(), []);

            // act
            var success = broker.TryInvokeFunction("doesNotExist", args, out var result, out var isVolatile);

            // assert
            success.Should().BeFalse("no function with that name was registered");
            result.Should().BeNull("unknown functions should not produce a result");
            isVolatile.Should().BeFalse("unknown functions should not mark the evaluation as volatile");
        }

        [Test]
        public void RegisterFunction_VolatileFunction_ShouldSetIsVolatileTrueOnInvoke() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var fn = new SymbolFunction(
                name: "volatileFunc",
                description: "",
                usageExample: "",
                implementation: args => "value",
                minArgs: 0,
                maxArgs: 0,
                isVolatile: true);

            broker.RegisterFunction(fn);

            var args = new FunctionArgs(Guid.NewGuid(), []);

            // act
            var success = broker.TryInvokeFunction("volatileFunc", args, out var result, out var isVolatile);

            // assert
            success.Should().BeTrue();
            result.Should().Be("value");
            isVolatile.Should().BeTrue("this function was registered as volatile");
        }

        [Test]
        public void TryInvokeFunction_WithTooFewArguments_ShouldThrowArgumentException() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var fn = new SymbolFunction(
                name: "needsOneArg",
                description: "",
                usageExample: "",
                implementation: args => args.Parameters[0].Evaluate(),
                minArgs: 1,
                maxArgs: 1,
                isVolatile: false);

            broker.RegisterFunction(fn);

            var args = new FunctionArgs(Guid.NewGuid(), []); // 0 parameters

            // act
            Action act = () => {
                broker.TryInvokeFunction("needsOneArg", args, out var _, out var _);
            };

            // assert
            act.Should()
               .Throw<ArgumentException>()
               .WithMessage("*needsOneArg*");
        }

        [Test]
        public void RegisterFunction_WithSameName_ShouldOverrideExistingFunction() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var fn1 = new SymbolFunction(
                name: "overrideMe",
                description: "",
                usageExample: "",
                implementation: args => 1,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            var fn2 = new SymbolFunction(
                name: "overrideMe",
                description: "",
                usageExample: "",
                implementation: args => 2,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            broker.RegisterFunction(fn1);
            broker.RegisterFunction(fn2); // should overwrite

            var args = new FunctionArgs(Guid.NewGuid(), []);

            // act
            var success = broker.TryInvokeFunction("overrideMe", args, out var result, out var isVolatile);

            // assert
            success.Should().BeTrue();
            result.Should().Be(2, "the second registration should override the first implementation");
        }

        private static FunctionArgs CreateArgsFromStrings(params string[] exprStrings) {
            var exprs = exprStrings
                .Select(s => new NCalc.Expression(s))
                .ToArray();

            return new FunctionArgs(Guid.NewGuid(), exprs);
        }

        [Test]
        public void VolatileFunction_WithArguments_ShouldSetIsVolatileTrue() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var fn = new SymbolFunction(
                name: "randomInRange",
                description: "",
                usageExample: "",
                implementation: args => {
                    var min = Convert.ToDouble(args.Parameters[0].Evaluate());
                    var max = Convert.ToDouble(args.Parameters[1].Evaluate());
                    return max; // simplified; real impl would use RNG
                },
                minArgs: 2,
                maxArgs: 2,
                isVolatile: true);

            broker.RegisterFunction(fn);

            var args = CreateArgsFromStrings("0.0", "1.0");

            // act
            var success = broker.TryInvokeFunction("randomInRange", args, out var result, out var isVolatile);

            // assert
            success.Should().BeTrue();
            result.Should().Be(1.0);
            isVolatile.Should().BeTrue("function was registered as volatile");
        }

        [Test]
        public void RegisterFunction_WithNumericArguments_ShouldReceiveEvaluatedValues() {
            // arrange: add(a, b) => a + b
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var fn = new SymbolFunction(
                name: "add",
                description: "",
                usageExample: "",
                implementation: args => {
                    // args.Parameters[i] are NCalc.Expression
                    var a = Convert.ToInt32(args.Parameters[0].Evaluate());
                    var b = Convert.ToInt32(args.Parameters[1].Evaluate());
                    return a + b;
                },
                minArgs: 2,
                maxArgs: 2,
                isVolatile: false);

            broker.RegisterFunction(fn);

            // add(10, 32)
            var args = CreateArgsFromStrings("10", "32");

            // act
            var success = broker.TryInvokeFunction("add", args, out var result, out var isVolatile);

            // assert
            success.Should().BeTrue();
            result.Should().Be(42);
            isVolatile.Should().BeFalse();
        }
    }
}