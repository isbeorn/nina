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

            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(10);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(20);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Elevation).Returns(30);
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
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
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
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
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
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
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
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("Plugin");
            var fn = new SymbolFunction(
                key: "const42",
                category: "Plugin",
                description: "",
                usageExample: "",
                implementation: args => 42,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            provider.RegisterFunction(fn);

            var args = new FunctionArgs(Guid.NewGuid(), []);

            // act
            broker.InvokeFunction("const42", args, out var result, out var isVolatile);

            // assert
            result.Should().Be(42);
            isVolatile.Should().BeFalse("this function was registered as non-volatile");
        }

        [Test]
        public void InvokeFunction_UnknownFunction_ShouldReturnFalseAndNullResult() {
            // arrange
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var args = new FunctionArgs(Guid.NewGuid(), []);

            // act
            Action fn = () => broker.InvokeFunction("doesNotExist", args, out var result, out var isVolatile);

            // assert
            fn.Should().Throw<ArgumentException>("no function with that name was registered");
        }

        [Test]
        public void RegisterFunction_VolatileFunction_ShouldSetIsVolatileTrueOnInvoke() {
            // arrange
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("Plugin");
            var fn = new SymbolFunction(
                key: "volatileFunc",
                category: "Plugin",
                description: "",
                usageExample: "",
                implementation: args => "value",
                minArgs: 0,
                maxArgs: 0,
                isVolatile: true);

            provider.RegisterFunction(fn);

            var args = new FunctionArgs(Guid.NewGuid(), []);

            // act
            broker.InvokeFunction("volatileFunc", args, out var result, out var isVolatile);

            // assert
            result.Should().Be("value");
            isVolatile.Should().BeTrue("this function was registered as volatile");
        }

        [Test]
        public void InvokeFunction_WithTooFewArguments_ShouldThrowArgumentException() {
            // arrange
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("Plugin");
            var fn = new SymbolFunction(
                key: "needsOneArg",
                category: "Plugin",
                description: "",
                usageExample: "",
                implementation: args => args.Parameters[0].Evaluate(),
                minArgs: 1,
                maxArgs: 1,
                isVolatile: false);

            provider.RegisterFunction(fn);

            var args = new FunctionArgs(Guid.NewGuid(), []); // 0 parameters

            // act
            Action act = () => {
                broker.InvokeFunction("needsOneArg", args, out var _, out var _);
            };

            // assert
            act.Should()
               .Throw<ArgumentException>()
               .WithMessage("*needsOneArg*");
        }

        [Test]
        public void RegisterFunction_WithSameName_ShouldNotOverrideExistingFunction() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("Plugin");
            var fn1 = new SymbolFunction(
                key: "overrideMe",
                category: "Plugin",
                description: "",
                usageExample: "",
                implementation: args => 1,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            var fn2 = new SymbolFunction(
                key: "overrideMe",
                category: "Plugin",
                description: "",
                usageExample: "",
                implementation: args => 2,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            provider.RegisterFunction(fn1);
            Action overwrite = () => provider.RegisterFunction(fn2);
            overwrite.Should().Throw<ArgumentException>("overwriting an existing function should not be allowed");
        }

        [Test]
        public void RegisterFunction_WithSameName_DifferentNamespace_ShouldNotOverrideExistingFunction() {
            // arrange
            var broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider1 = broker.RegisterSymbolProvider("Plugin1");
            var fn1 = new SymbolFunction(
                key: "overrideMe",
                category: "Plugin1",
                description: "",
                usageExample: "",
                implementation: args => 1,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            var provider2 = broker.RegisterSymbolProvider("Plugin2");
            var fn2 = new SymbolFunction(
                key: "overrideMe",
                category: "Plugin2",
                description: "",
                usageExample: "",
                implementation: args => 2,
                minArgs: 0,
                maxArgs: 0,
                isVolatile: false);

            provider1.RegisterFunction(fn1);
            Action overwrite = () => provider2.RegisterFunction(fn2);
            overwrite.Should().NotThrow<ArgumentException>("overwriting an existing function in a different category should be allowed");
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
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("Plugin");
            var fn = new SymbolFunction(
                key: "randomInRange",
                category: "Plugin",
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

            provider.RegisterFunction(fn);

            var args = CreateArgsFromStrings("0.0", "1.0");

            // act
            broker.InvokeFunction("randomInRange", args, out var result, out var isVolatile);

            // assert
            result.Should().Be(1.0);
            isVolatile.Should().BeTrue("function was registered as volatile");
        }

        [Test]
        public void RegisterFunction_WithNumericArguments_ShouldReceiveEvaluatedValues() {
            // arrange: add(a, b) => a + b
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("Plugin");
            var fn = new SymbolFunction(
                key: "add",
                category: "Plugin",
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

            provider.RegisterFunction(fn);

            // add(10, 32)
            var args = CreateArgsFromStrings("10", "32");

            // act
            broker.InvokeFunction("add", args, out var result, out var isVolatile);

            // assert
            result.Should().Be(42);
            isVolatile.Should().BeFalse();
        }

        [Test]
        public void RegisterFunction_WithNumericArguments_WithNamespace_ShouldReceiveEvaluatedValues() {
            // arrange: add(a, b) => a + b
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("Plugin");
            var fn = new SymbolFunction(
                key: "add",
                category: "Plugin",
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

            provider.RegisterFunction(fn);

            // add(10, 32)
            var args = CreateArgsFromStrings("10", "32");

            // act
            broker.InvokeFunction("Plugin_add", args, out var result, out var isVolatile);

            // assert
            result.Should().Be(42);
            isVolatile.Should().BeFalse();
        }

        [Test]
        public void RegisterMultipleFunctions_WithNumericArguments_WithNamespace_ShouldReceiveEvaluatedValues() {
            // arrange: add(a, b) => a + b
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider1 = broker.RegisterSymbolProvider("Plugin1");
            var fn1 = new SymbolFunction(
                key: "add",
                category: "Plugin1",
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

            provider1.RegisterFunction(fn1);

            var provider2 = broker.RegisterSymbolProvider("Plugin2");
            var fn2 = new SymbolFunction(
                key: "add",
                category: "Plugin2",
                description: "",
                usageExample: "",
                implementation: args => {
                    // args.Parameters[i] are NCalc.Expression
                    var a = Convert.ToInt32(args.Parameters[0].Evaluate());
                    var b = Convert.ToInt32(args.Parameters[1].Evaluate());
                    var c = Convert.ToInt32(args.Parameters[2].Evaluate());
                    return a + b + c;
                },
                minArgs: 3,
                maxArgs: 3,
                isVolatile: true);

            provider2.RegisterFunction(fn2);

            // add(10, 32)
            var args1 = CreateArgsFromStrings("10", "32");
            // add(10, 32, 40)
            var args2 = CreateArgsFromStrings("10", "32", "40");

            // act
            broker.InvokeFunction("Plugin1_add", args1, out var result1, out var isVolatile1);
            broker.InvokeFunction("Plugin2_add", args2, out var result2, out var isVolatile2);

            // assert
            result1.Should().Be(42);
            isVolatile1.Should().BeFalse();
            result2.Should().Be(82);
            isVolatile2.Should().BeTrue();
        }

        [Test]
        public void RegisterMultipleFunctions_WithNumericArguments_WithoutNamespace_IsAmbiguousAndThrows() {
            // arrange: add(a, b) => a + b
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
               flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
               telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider1 = broker.RegisterSymbolProvider("Plugin1");
            var fn1 = new SymbolFunction(
                key: "add",
                category: "Plugin1",
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

            provider1.RegisterFunction(fn1);

            var provider2 = broker.RegisterSymbolProvider("Plugin2");
            var fn2 = new SymbolFunction(
                key: "add",
                category: "Plugin2",
                description: "",
                usageExample: "",
                implementation: args => {
                    // args.Parameters[i] are NCalc.Expression
                    var a = Convert.ToInt32(args.Parameters[0].Evaluate());
                    var b = Convert.ToInt32(args.Parameters[1].Evaluate());
                    var c = Convert.ToInt32(args.Parameters[2].Evaluate());
                    return a + b + c;
                },
                minArgs: 3,
                maxArgs: 3,
                isVolatile: true);

            provider2.RegisterFunction(fn2);

            // add(10, 32)
            var args1 = CreateArgsFromStrings("10", "32");
            // add(10, 32, 40)
            var args2 = CreateArgsFromStrings("10", "32", "40");

            // act
            Action act = () => broker.InvokeFunction("add", args1, out var result1, out var isVolatile1);

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        [TestCase("")]
        [TestCase("123InvalidStart")]
        [TestCase("Invalid Symbol")]
        [TestCase("Invalid-Symbol!")]
        [TestCase("Symbol@Name")]
        public void RegisterSymbolProvider_WithInvalidName_ShouldThrowArgumentException(string invalidName) {
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);

            Action act = () => broker.RegisterSymbolProvider(invalidName);

            act.Should().Throw<ArgumentException>()
                .WithMessage("SymbolProvider name must be an alphanumeric word.");
        }

        [Test]
        [TestCase("")]
        [TestCase("123InvalidStart")]
        [TestCase("Invalid Symbol")]
        [TestCase("Invalid-Symbol")]
        [TestCase("Invalid+Symbol")]
        [TestCase("Symbol@Name")]
        public void AddOrUpdateSymbol_WithInvalidToken_ShouldThrowArgumentException(string invalidToken) {
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("ValidProvider");

            Action act = () => provider.AddOrUpdateSymbol(invalidToken, 42);

            act.Should().Throw<ArgumentException>()
                .WithMessage($"Invalid Symbol - {invalidToken}");
        }

        [Test]
        [TestCase("")]
        [TestCase("123InvalidStart")]
        [TestCase("Invalid Symbol")]
        [TestCase("Invalid-Symbol")]
        [TestCase("Invalid+Symbol")]
        [TestCase("Symbol@Name")]
        public void AddOrUpdateSymbol_WithConstantsAndInvalidToken_ShouldThrowArgumentException(string invalidToken) {
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("ValidProvider");
            Symbol[] constants = new Symbol[] { new Symbol("Constant1", 1), new Symbol("Constant2", 2) };

            Action act = () => provider.AddOrUpdateSymbol(invalidToken, 1, constants);

            act.Should().Throw<ArgumentException>()
                .WithMessage($"Invalid Symbol - {invalidToken}");
        }

        [Test]
        [TestCase("")]
        [TestCase("123InvalidStart")]
        [TestCase("Invalid Symbol")]
        [TestCase("Invalid-Symbol")]
        [TestCase("Invalid+Symbol")]
        [TestCase("Symbol@Name")]
        public void AddOrUpdateHiddenSymbol_WithInvalidToken_ShouldThrowArgumentException(string invalidToken) {
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("ValidProvider");
            Symbol[] constants = new Symbol[] { new Symbol("Constant1", 1), new Symbol("Constant2", 2) };

            Action act = () => provider.AddOrUpdateHiddenSymbol(invalidToken, 1, constants);

            act.Should().Throw<ArgumentException>()
                .WithMessage($"Invalid Symbol - {invalidToken}");
        }

        [Test]
        [TestCase("ValidSymbol")]
        [TestCase("Valid123")]
        [TestCase("Valid_Symbol")]
        [TestCase("_privateSymbol")]
        [TestCase("_")]
        [TestCase("a")]
        [TestCase("A1B2C3")]
        public void AddOrUpdateSymbol_WithValidToken_ShouldSucceed(string validToken) {
            ISymbolBroker broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
            var provider = broker.RegisterSymbolProvider("ValidProvider");

            Action act = () => provider.AddOrUpdateSymbol(validToken, 42);

            act.Should().NotThrow();
        }

        [Test]
        public void SymbolProvider_ValidSymbolRegex_IsPrecompiled() {
            // Assert - verify the regex is precompiled for performance
            SymbolProvider.ValidSymbolRegex.Should().NotBeNull();
            SymbolProvider.ValidSymbolRegex.Options.Should().HaveFlag(System.Text.RegularExpressions.RegexOptions.Compiled,
                "precompiled regex provides better performance for frequently used patterns");
        }

        [Test]
        public void UserSymbol_ValidSymbolRegex_IsPrecompiled() {
            // Assert - verify the regex is precompiled for performance
            UserSymbol.ValidSymbolRegex.Should().NotBeNull();
            UserSymbol.ValidSymbolRegex.Options.Should().HaveFlag(System.Text.RegularExpressions.RegexOptions.Compiled,
                "precompiled regex provides better performance for frequently used patterns");
        }
    }
}