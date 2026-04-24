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
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger.SafetyMonitor;
using NINA.Sequencer.Trigger.Utility;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Test.Sequencer.SequenceItem {

    [TestFixture]
    public class CoordinatesInstructionInheritanceTest {
        private const double Tolerance = 1e-10;
        private const string CenterInstruction = nameof(Center);
        private const string CenterAndRotateInstruction = nameof(CenterAndRotate);
        private const string SlewScopeToRaDecInstruction = nameof(SlewScopeToRaDec);
        private const string WaitForAltitudeInstruction = nameof(WaitForAltitude);
        private const string WaitUntilAboveHorizonInstruction = nameof(WaitUntilAboveHorizon);

        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profile.AstrometrySettings.Latitude = 47;
            profile.AstrometrySettings.Longitude = 11;

            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies a coordinate-inheriting instruction remains attached to its target context and updates when that target's coordinates change.
        /// </summary>
        /// <param name="instructionName">The sequence instruction type to verify.</param>
        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void TargetCoordinatesChanged_UpdatesInheritedInstructionCoordinates(string instructionName) {
            DeepSkyObjectContainer target = CreateTargetContainer();
            Coordinates initialCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 10),
                dec: Angle.ByDegree(degree: 20),
                epoch: Epoch.J2000);
            Coordinates updatedCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 30),
                dec: Angle.ByDegree(degree: 40),
                epoch: Epoch.J2000);
            CoordinatesInstruction instruction = CreateInstruction(instructionName: instructionName);

            SetTarget(target: target, coordinates: initialCoordinates, positionAngle: 15);
            target.Add(item: instruction);

            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: initialCoordinates,
                expectedRaDegrees: 10,
                expectedDecDegrees: 20,
                expectedPositionAngle: 15);

            SetTarget(target: target, coordinates: updatedCoordinates, positionAngle: 25);

            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: updatedCoordinates,
                expectedRaDegrees: 30,
                expectedDecDegrees: 40,
                expectedPositionAngle: 25);
        }

        /// <summary>
        /// Verifies moving a coordinate-inheriting instruction from one target container to another refreshes it to the new target and stops following the old one.
        /// </summary>
        /// <param name="instructionName">The sequence instruction type to verify.</param>
        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void MoveBetweenTargetContainers_UpdatesInheritedInstructionCoordinatesToNewTarget(string instructionName) {
            DeepSkyObjectContainer firstTarget = CreateTargetContainer();
            DeepSkyObjectContainer secondTarget = CreateTargetContainer();
            Coordinates firstCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 10),
                dec: Angle.ByDegree(degree: 20),
                epoch: Epoch.J2000);
            Coordinates firstUpdatedCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 30),
                dec: Angle.ByDegree(degree: 40),
                epoch: Epoch.J2000);
            Coordinates secondCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 50),
                dec: Angle.ByDegree(degree: 60),
                epoch: Epoch.J2000);
            Coordinates secondUpdatedCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 70),
                dec: Angle.ByDegree(degree: 80),
                epoch: Epoch.J2000);
            CoordinatesInstruction instruction = CreateInstruction(instructionName: instructionName);

            SetTarget(target: firstTarget, coordinates: firstCoordinates, positionAngle: 15);
            SetTarget(target: secondTarget, coordinates: secondCoordinates, positionAngle: 25);
            firstTarget.Add(item: instruction);

            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: firstCoordinates,
                expectedRaDegrees: 10,
                expectedDecDegrees: 20,
                expectedPositionAngle: 15);

            secondTarget.Add(item: instruction);

            firstTarget.Items.Should().NotContain(instruction);
            secondTarget.Items.Should().Contain(instruction);
            instruction.Parent.Should().BeSameAs(secondTarget);
            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: secondCoordinates,
                expectedRaDegrees: 50,
                expectedDecDegrees: 60,
                expectedPositionAngle: 25);

            SetTarget(target: firstTarget, coordinates: firstUpdatedCoordinates, positionAngle: 35);

            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: secondCoordinates,
                expectedRaDegrees: 50,
                expectedDecDegrees: 60,
                expectedPositionAngle: 25);

            SetTarget(target: secondTarget, coordinates: secondUpdatedCoordinates, positionAngle: 45);

            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: secondUpdatedCoordinates,
                expectedRaDegrees: 70,
                expectedDecDegrees: 80,
                expectedPositionAngle: 45);
        }

        /// <summary>
        /// Verifies a coordinate-inheriting instruction inside a custom trigger still inherits and refreshes deep-sky target coordinates.
        /// </summary>
        /// <param name="instructionName">The custom-trigger instruction type to verify.</param>
        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void CustomTrigger_TargetCoordinatesChanged_UpdatesInheritedInstructionCoordinates(string instructionName) {
            DeepSkyObjectContainer target = CreateTargetContainer();
            Coordinates initialCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 10),
                dec: Angle.ByDegree(degree: 20),
                epoch: Epoch.J2000);
            Coordinates updatedCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 30),
                dec: Angle.ByDegree(degree: 40),
                epoch: Epoch.J2000);
            CoordinatesInstruction instruction = CreateInstruction(instructionName: instructionName);
            CustomTrigger customTrigger = CreateCustomTrigger();

            customTrigger.TriggerRunner.Add(instruction);
            SetTarget(target: target, coordinates: initialCoordinates, positionAngle: 15);
            target.Add(customTrigger);

            instruction.Parent.Should().BeSameAs(customTrigger.TriggerRunner);
            AssertTriggerRunnerContext(
                customTrigger: customTrigger,
                sourceCoordinates: initialCoordinates,
                expectedPositionAngle: 15);
            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: initialCoordinates,
                expectedRaDegrees: 10,
                expectedDecDegrees: 20,
                expectedPositionAngle: 15);

            SetTarget(target: target, coordinates: updatedCoordinates, positionAngle: 25);

            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: updatedCoordinates,
                expectedRaDegrees: 30,
                expectedDecDegrees: 40,
                expectedPositionAngle: 25);
        }

        /// <summary>
        /// Verifies runtime execution of a custom trigger keeps deep-sky target coordinates available to nested slew instructions.
        /// </summary>
        [Test]
        public async Task CustomTrigger_Execute_PreservesInheritedCoordinatesForSlewInstruction() {
            DeepSkyObjectContainer target = CreateTargetContainer();
            Coordinates targetCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 10),
                dec: Angle.ByDegree(degree: 20),
                epoch: Epoch.J2000);
            (SlewScopeToRaDec instruction, Mock<ITelescopeMediator> telescopeMediatorMock) = CreateExecutableSlewScopeToRaDec();
            CustomTrigger customTrigger = CreateCustomTrigger();
            Coordinates slewedCoordinates = null;

            telescopeMediatorMock
                .Setup(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
                .Callback<Coordinates, CancellationToken>((coordinates, _) => slewedCoordinates = coordinates)
                .ReturnsAsync(true);

            customTrigger.TriggerRunner.Add(instruction);
            SetTarget(target: target, coordinates: targetCoordinates, positionAngle: 15);
            target.Add(customTrigger);

            await customTrigger.Execute(target, progress: new Progress<ApplicationStatus>(), token: CancellationToken.None);

            instruction.Inherited.Should().BeTrue();
            slewedCoordinates.Should().NotBeNull();
            slewedCoordinates.RADegrees.Should().BeApproximately(targetCoordinates.RADegrees, Tolerance);
            slewedCoordinates.Dec.Should().BeApproximately(targetCoordinates.Dec, Tolerance);
            AssertTriggerRunnerContext(
                customTrigger: customTrigger,
                sourceCoordinates: targetCoordinates,
                expectedPositionAngle: 15);
        }

        /// <summary>
        /// Verifies TriggerOnUnsafe before/after instruction sets inherit and refresh deep-sky target coordinates.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void TriggerOnUnsafe_TargetCoordinatesChanged_UpdatesInheritedInstructionCoordinates(bool useBeforeWaitForSafe) {
            DeepSkyObjectContainer target = CreateTargetContainer();
            Coordinates initialCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 10),
                dec: Angle.ByDegree(degree: 20),
                epoch: Epoch.J2000);
            Coordinates updatedCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 30),
                dec: Angle.ByDegree(degree: 40),
                epoch: Epoch.J2000);
            SlewScopeToRaDec instruction = CreateSlewScopeToRaDec();
            TriggerOnUnsafe triggerOnUnsafe = CreateTriggerOnUnsafe();
            SequentialContainer instructionSet = useBeforeWaitForSafe ? triggerOnUnsafe.BeforeWaitForSafe : triggerOnUnsafe.AfterWaitForSafe;

            instructionSet.Add(instruction);
            SetTarget(target: target, coordinates: initialCoordinates, positionAngle: 15);
            target.Add(triggerOnUnsafe);

            AssertTriggerRunnerContext(
                container: instructionSet,
                sourceCoordinates: initialCoordinates,
                expectedPositionAngle: 15);
            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: initialCoordinates,
                expectedRaDegrees: 10,
                expectedDecDegrees: 20,
                expectedPositionAngle: 15);

            SetTarget(target: target, coordinates: updatedCoordinates, positionAngle: 25);

            AssertTriggerRunnerContext(
                container: instructionSet,
                sourceCoordinates: updatedCoordinates,
                expectedPositionAngle: 25);
            AssertInheritedCoordinates(
                instruction: instruction,
                sourceCoordinates: updatedCoordinates,
                expectedRaDegrees: 30,
                expectedDecDegrees: 40,
                expectedPositionAngle: 25);
        }

        /// <summary>
        /// Verifies TriggerOnUnsafe runtime execution preserves inherited coordinates for before/after instruction sets.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public async Task TriggerOnUnsafe_Execute_PreservesInheritedCoordinatesForInstructionSet(bool useBeforeWaitForSafe) {
            DeepSkyObjectContainer target = CreateTargetContainer();
            Coordinates targetCoordinates = new Coordinates(
                ra: Angle.ByDegree(degree: 10),
                dec: Angle.ByDegree(degree: 20),
                epoch: Epoch.J2000);
            (SlewScopeToRaDec instruction, Mock<ITelescopeMediator> telescopeMediatorMock) = CreateExecutableSlewScopeToRaDec();
            TriggerOnUnsafe triggerOnUnsafe = CreateTriggerOnUnsafe();
            SequentialContainer instructionSet = useBeforeWaitForSafe ? triggerOnUnsafe.BeforeWaitForSafe : triggerOnUnsafe.AfterWaitForSafe;
            Coordinates slewedCoordinates = null;

            telescopeMediatorMock
                .Setup(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
                .Callback<Coordinates, CancellationToken>((coordinates, _) => slewedCoordinates = coordinates)
                .ReturnsAsync(true);

            instructionSet.Add(instruction);
            SetTarget(target: target, coordinates: targetCoordinates, positionAngle: 15);
            target.Add(triggerOnUnsafe);

            await triggerOnUnsafe.Execute(target, progress: new Progress<ApplicationStatus>(), token: CancellationToken.None);

            instruction.Inherited.Should().BeTrue();
            slewedCoordinates.Should().NotBeNull();
            slewedCoordinates.RADegrees.Should().BeApproximately(targetCoordinates.RADegrees, Tolerance);
            slewedCoordinates.Dec.Should().BeApproximately(targetCoordinates.Dec, Tolerance);
            AssertTriggerRunnerContext(
                container: instructionSet,
                sourceCoordinates: targetCoordinates,
                expectedPositionAngle: 15);
        }

        private CoordinatesInstruction CreateInstruction(string instructionName) {
            switch (instructionName) {
                case CenterInstruction:
                    return CreateCenter();
                case CenterAndRotateInstruction:
                    return CreateCenterAndRotate();
                case SlewScopeToRaDecInstruction:
                    return CreateSlewScopeToRaDec();
                case WaitForAltitudeInstruction:
                    return new WaitForAltitude(profileService: profileServiceMock.Object);
                case WaitUntilAboveHorizonInstruction:
                    return new WaitUntilAboveHorizon(profileService: profileServiceMock.Object);
                default:
                    throw new ArgumentOutOfRangeException(paramName: nameof(instructionName), actualValue: instructionName, message: null);
            }
        }

        private Center CreateCenter() {
            Mock<ITelescopeMediator> telescopeMediatorMock = CreateConnectedTelescopeMediator();
            return new Center(
                profileService: profileServiceMock.Object,
                telescopeMediator: telescopeMediatorMock.Object,
                imagingMediator: new Mock<IImagingMediator>().Object,
                filterWheelMediator: new Mock<IFilterWheelMediator>().Object,
                guiderMediator: new Mock<IGuiderMediator>().Object,
                domeMediator: new Mock<IDomeMediator>().Object,
                domeFollower: new Mock<IDomeFollower>().Object,
                plateSolverFactory: new Mock<IPlateSolverFactory>().Object,
                windowServiceFactory: new Mock<IWindowServiceFactory>().Object);
        }

        private CenterAndRotate CreateCenterAndRotate() {
            Mock<ITelescopeMediator> telescopeMediatorMock = CreateConnectedTelescopeMediator();
            Mock<IRotatorMediator> rotatorMediatorMock = new Mock<IRotatorMediator>();
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = true });

            return new CenterAndRotate(
                profileService: profileServiceMock.Object,
                telescopeMediator: telescopeMediatorMock.Object,
                imagingMediator: new Mock<IImagingMediator>().Object,
                rotatorMediator: rotatorMediatorMock.Object,
                filterWheelMediator: new Mock<IFilterWheelMediator>().Object,
                guiderMediator: new Mock<IGuiderMediator>().Object,
                domeMediator: new Mock<IDomeMediator>().Object,
                domeFollower: new Mock<IDomeFollower>().Object,
                plateSolverFactory: new Mock<IPlateSolverFactory>().Object,
                windowServiceFactory: new Mock<IWindowServiceFactory>().Object);
        }

        private SlewScopeToRaDec CreateSlewScopeToRaDec() {
            Mock<ITelescopeMediator> telescopeMediatorMock = CreateConnectedTelescopeMediator();
            return new SlewScopeToRaDec(telescopeMediator: telescopeMediatorMock.Object, guiderMediator: new Mock<IGuiderMediator>().Object);
        }

        private (SlewScopeToRaDec Instruction, Mock<ITelescopeMediator> TelescopeMediator) CreateExecutableSlewScopeToRaDec() {
            Mock<ITelescopeMediator> telescopeMediatorMock = CreateConnectedTelescopeMediator();
            Mock<IGuiderMediator> guiderMediatorMock = new Mock<IGuiderMediator>();
            guiderMediatorMock
                .Setup(x => x.StopGuiding(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            return (
                Instruction: new SlewScopeToRaDec(telescopeMediator: telescopeMediatorMock.Object, guiderMediator: guiderMediatorMock.Object),
                TelescopeMediator: telescopeMediatorMock);
        }

        private static Mock<ITelescopeMediator> CreateConnectedTelescopeMediator() {
            Mock<ITelescopeMediator> telescopeMediatorMock = new Mock<ITelescopeMediator>();
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true });
            return telescopeMediatorMock;
        }

        private static CustomTrigger CreateCustomTrigger() {
            Mock<IApplicationResourceDictionary> resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());
            return new CustomTrigger(resourceDictionaryMock.Object);
        }

        private static TriggerOnUnsafe CreateTriggerOnUnsafe() {
            Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            safetyMonitorMediatorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo { Connected = true, IsSafe = true });

            Mock<IApplicationResourceDictionary> resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            resourceDictionaryMock.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());
            return new TriggerOnUnsafe(safetyMonitorMediator: safetyMonitorMediatorMock.Object, resourceDictionary: resourceDictionaryMock.Object);
        }

        private DeepSkyObjectContainer CreateTargetContainer() {
            Mock<INighttimeCalculator> nighttimeCalculatorMock = new Mock<INighttimeCalculator>();
            nighttimeCalculatorMock.Setup(x => x.Calculate(It.IsAny<DateTime?>())).Returns(CreateNighttimeData());

            return new DeepSkyObjectContainer(
                profileService: profileServiceMock.Object,
                nighttimeCalculator: nighttimeCalculatorMock.Object,
                framingAssistantVM: new Mock<IFramingAssistantVM>().Object,
                applicationMediator: new Mock<IApplicationMediator>().Object,
                planetariumFactory: new Mock<IPlanetariumFactory>().Object,
                cameraMediator: new Mock<ICameraMediator>().Object,
                filterWheelMediator: new Mock<IFilterWheelMediator>().Object,
                symbolBroker: new Mock<ISymbolBroker>().Object);
        }

        private static void SetTarget(DeepSkyObjectContainer target, Coordinates coordinates, double positionAngle) {
            target.Target.PositionAngle = positionAngle;
            target.Target.InputCoordinates.Coordinates = coordinates;
        }

        private static void AssertInheritedCoordinates(CoordinatesInstruction instruction, Coordinates sourceCoordinates, double expectedRaDegrees, double expectedDecDegrees, double expectedPositionAngle) {
            instruction.Inherited.Should().BeTrue();
            instruction.PositionAngle.Should().BeApproximately(expectedPositionAngle, Tolerance);
            AssertInputCoordinates(
                inputCoordinates: instruction.Coordinates,
                sourceCoordinates: sourceCoordinates,
                expectedRaDegrees: expectedRaDegrees,
                expectedDecDegrees: expectedDecDegrees);
            AssertInputCoordinates(
                inputCoordinates: GetEffectiveCoordinates(instruction: instruction),
                sourceCoordinates: sourceCoordinates,
                expectedRaDegrees: expectedRaDegrees,
                expectedDecDegrees: expectedDecDegrees);
        }

        private static InputCoordinates GetEffectiveCoordinates(CoordinatesInstruction instruction) {
            if (instruction is WaitForAltitude waitForAltitude) {
                return waitForAltitude.Data.Coordinates;
            }

            if (instruction is WaitUntilAboveHorizon waitUntilAboveHorizon) {
                return waitUntilAboveHorizon.Data.Coordinates;
            }

            return instruction.Coordinates;
        }

        private static void AssertTriggerRunnerContext(CustomTrigger customTrigger, Coordinates sourceCoordinates, double expectedPositionAngle) {
            customTrigger.TriggerRunner.Parent.Should().NotBeNull();
            AssertTriggerRunnerContext(customTrigger.TriggerRunner, sourceCoordinates, expectedPositionAngle);
        }

        private static void AssertTriggerRunnerContext(ISequenceContainer container, Coordinates sourceCoordinates, double expectedPositionAngle) {
            container.Parent.Should().NotBeNull();
            ContextCoordinates contextCoordinates = ItemUtility.RetrieveContextCoordinates(container);
            contextCoordinates.Should().NotBeNull();
            contextCoordinates.PositionAngle.Should().BeApproximately(expectedPositionAngle, Tolerance);
            contextCoordinates.Coordinates.RADegrees.Should().BeApproximately(sourceCoordinates.RADegrees, Tolerance);
            contextCoordinates.Coordinates.Dec.Should().BeApproximately(sourceCoordinates.Dec, Tolerance);
        }

        private static void AssertInputCoordinates(InputCoordinates inputCoordinates, Coordinates sourceCoordinates, double expectedRaDegrees, double expectedDecDegrees) {
            inputCoordinates.Coordinates.RADegrees.Should().BeApproximately(expectedRaDegrees, Tolerance);
            inputCoordinates.Coordinates.Dec.Should().BeApproximately(expectedDecDegrees, Tolerance);
            inputCoordinates.Coordinates.Should().NotBeSameAs(sourceCoordinates);
        }

        private static NighttimeData CreateNighttimeData() {
            DateTime referenceDate = new DateTime(year: 2026, month: 4, day: 16, hour: 12, minute: 0, second: 0);
            RiseAndSetEvent sunRiseAndSet = new CustomRiseAndSet(rise: referenceDate.AddHours(18), set: referenceDate.AddHours(7));

            return new NighttimeData(
                date: referenceDate,
                referenceDate: referenceDate,
                moonPhase: AstroUtil.MoonPhase.Unknown,
                moonIllumination: null,
                twilightRiseAndSet: null,
                nauticalTwilightRiseAndSet: null,
                sunRiseAndSet: sunRiseAndSet,
                moonRiseAndSet: null,
                civilTwilightRiseAndSet: null);
        }
    }
}
