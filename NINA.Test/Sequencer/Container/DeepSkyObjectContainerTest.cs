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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using OxyPlot;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Container {

    [TestFixture]
    public class DeepSkyObjectContainerTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;
        private Mock<INighttimeCalculator> nighttimeCalculatorMock;
        private Mock<IFramingAssistantVM> framingAssistantMock;
        private Mock<IApplicationMediator> applicationMediatorMock;
        private Mock<IPlanetariumFactory> planetariumFactoryMock;
        private Mock<IPlanetarium> planetariumMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<ISymbolBroker> symbolBrokerMock;
        private Mock<ISymbolProvider> symbolProviderMock;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profile.AstrometrySettings.Latitude = 47;
            profile.AstrometrySettings.Longitude = 11;

            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            nighttimeCalculatorMock = new Mock<INighttimeCalculator>();
            nighttimeCalculatorMock.Setup(x => x.Calculate(It.IsAny<DateTime?>())).Returns(CreateNighttimeData());

            framingAssistantMock = new Mock<IFramingAssistantVM>();
            framingAssistantMock.Setup(x => x.SetCoordinates(It.IsAny<DeepSkyObject>())).ReturnsAsync(true);

            applicationMediatorMock = new Mock<IApplicationMediator>();

            planetariumMock = new Mock<IPlanetarium>();
            planetariumMock.SetupGet(x => x.Name).Returns("TestPlanetarium");
            planetariumFactoryMock = new Mock<IPlanetariumFactory>();
            planetariumFactoryMock.Setup(x => x.GetPlanetarium()).Returns(planetariumMock.Object);

            cameraMediatorMock = new Mock<ICameraMediator>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo {
                Connected = true,
                CanSetGain = true,
                CanSetOffset = true,
                DefaultGain = 11,
                DefaultOffset = 22
            });

            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo {
                Connected = true,
                SelectedFilter = new FilterInfo { Name = "L", Position = 1 }
            });

            symbolProviderMock = new Mock<ISymbolProvider>();
            symbolBrokerMock = new Mock<ISymbolBroker>();
            symbolBrokerMock.As<ISymbolBrokerProviderApi>()
                .Setup(x => x.GetInternalProvider("NINA"))
                .Returns(symbolProviderMock.Object);
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies the Constructor Initializes Target Commands And Exposure State scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Constructor_InitializesTargetCommandsAndExposureState() {
            DeepSkyObjectContainer sut = CreateSut();

            sut.Target.Should().NotBeNull();
            sut.Target.InputCoordinates.Should().NotBeNull();
            sut.ExposureInfoList.Should().BeEmpty();
            sut.ExposureInfoSummary.Should().BeEmpty();
            sut.CoordsToFramingCommand.Should().NotBeNull();
            sut.CoordsFromPlanetariumCommand.Should().NotBeNull();
            sut.DropTargetCommand.Should().NotBeNull();
            sut.DeleteExposureInfoCommand.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies the Exposure Info Uses Current Filter Camera Defaults And Reuses Matching Entry scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ExposureInfo_UsesCurrentFilterCameraDefaultsAndReusesMatchingEntry() {
            DeepSkyObjectContainer sut = CreateSut();
            Mock<IExposureItem> exposure = CreateExposureItem(CaptureSequence.ImageTypes.LIGHT);

            ExposureInfo first = sut.GetOrCreateExposureCountForItemAndCurrentFilter(exposure.Object, roi: 0);
            ExposureInfo second = sut.GetOrCreateExposureCountForItemAndCurrentFilter(exposure.Object, roi: 0);

            second.Should().BeSameAs(first);
            first.Filter.Should().Be("L");
            first.ExposureTime.Should().Be(60);
            first.Gain.Should().Be(11);
            first.Offset.Should().Be(22);
            first.BinningX.Should().Be(1);
            first.BinningY.Should().Be(1);
            first.ROI.Should().Be(0);

            sut.IncrementExposureCountForItemAndCurrentFilter(exposure.Object, roi: 0);

            first.Count.Should().Be(1);
            sut.ExposureInfoSummary.Should().Be("L - 0:01:00");

            sut.DeleteExposureInfoCommand.Execute(first);

            sut.ExposureInfoList.Should().BeEmpty();
            sut.ExposureInfoSummary.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the Exposure Info Ignores Non Light Exposure Items scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ExposureInfo_IgnoresNonLightExposureItems() {
            DeepSkyObjectContainer sut = CreateSut();
            Mock<IExposureItem> darkExposure = CreateExposureItem(CaptureSequence.ImageTypes.DARK);

            ExposureInfo result = sut.GetOrCreateExposureCountForItemAndCurrentFilter(darkExposure.Object, roi: 1);

            result.Should().BeNull();
            sut.ExposureInfoList.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the Clone Copies Target Exposure Info And Reparents Children scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesTargetExposureInfoAndReparentsChildren() {
            DeepSkyObjectContainer sut = CreateSut();
            sut.Name = "M31";
            sut.Target.TargetName = "M31";
            sut.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(1.5), Angle.ByDegree(42), Epoch.J2000);
            sut.Target.PositionAngle = 33;
            sut.ExposureInfoListExpanded = true;
            sut.ExposureInfoList.Add(new ExposureInfo("L", 30, 11, 22, CaptureSequence.ImageTypes.LIGHT, 1, 1, 0));

            Annotation child = new Annotation();
            LoopCondition condition = new LoopCondition();
            Mock<ISequenceTrigger> trigger = new Mock<ISequenceTrigger>();
            Mock<ISequenceTrigger> triggerClone = new Mock<ISequenceTrigger>();
            trigger.Setup(x => x.Clone()).Returns(triggerClone.Object);
            sut.Add(child);
            sut.Add(condition);
            sut.Add(trigger.Object);

            DeepSkyObjectContainer clone = (DeepSkyObjectContainer)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            clone.Target.Should().NotBeSameAs(sut.Target);
            clone.Target.TargetName.Should().Be("M31");
            clone.Target.PositionAngle.Should().Be(33);
            clone.Target.InputCoordinates.Coordinates.RA.Should().Be(sut.Target.InputCoordinates.Coordinates.RA);
            clone.Target.InputCoordinates.Coordinates.Dec.Should().Be(sut.Target.InputCoordinates.Coordinates.Dec);
            clone.ExposureInfoListExpanded.Should().BeTrue();
            clone.ExposureInfoList.Should().HaveCount(1);
            clone.Items.Should().HaveCount(1);
            clone.Items[0].Parent.Should().BeSameAs(clone);
            clone.Conditions.Should().HaveCount(1);
            clone.Conditions[0].Parent.Should().BeSameAs(clone);
            clone.Triggers.Should().ContainSingle().Which.Should().BeSameAs(triggerClone.Object);
            triggerClone.Verify(x => x.AttachNewParent(clone), Times.Once);
        }

        /// <summary>
        /// Verifies the Execute Adds Target Symbols And Removes Them After Execution scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Execute_AddsTargetSymbolsAndRemovesThemAfterExecution() {
            DeepSkyObjectContainer sut = CreateSut();
            sut.Target.TargetName = "M31";
            sut.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(1.5), Angle.ByDegree(42), Epoch.J2000);
            sut.Target.PositionAngle = 33;

            await sut.Execute(default, CancellationToken.None);

            symbolBrokerMock.As<ISymbolBrokerProviderApi>().Verify(x => x.AddOrUpdateSymbol(symbolProviderMock.Object, "TargetName", "M31"), Times.Once);
            symbolBrokerMock.As<ISymbolBrokerProviderApi>().Verify(x => x.AddOrUpdateSymbol(symbolProviderMock.Object, "TargetRAJ2000", It.Is<object>(value => value != null && value.GetType() == typeof(double) && Math.Abs((double)value - 1.5) < 0.0001)), Times.Once);
            symbolBrokerMock.As<ISymbolBrokerProviderApi>().Verify(x => x.AddOrUpdateSymbol(symbolProviderMock.Object, "TargetDecJ2000", It.Is<object>(value => value != null && value.GetType() == typeof(double) && Math.Abs((double)value - 42) < 0.0001)), Times.Once);
            symbolBrokerMock.As<ISymbolBrokerProviderApi>().Verify(x => x.AddOrUpdateSymbol(symbolProviderMock.Object, "TargetPositionAngle", 33d), Times.Once);
            symbolBrokerMock.As<ISymbolBrokerProviderApi>().Verify(x => x.RemoveSymbol(symbolProviderMock.Object, It.IsAny<string>()), Times.Exactly(4));
        }

        /// <summary>
        /// Verifies the Coords To Framing Sends Current Target And Switches To Framing Tab scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task CoordsToFraming_SendsCurrentTargetAndSwitchesToFramingTab() {
            DeepSkyObjectContainer sut = CreateSut();
            sut.Target.TargetName = "M31";
            sut.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(1.5), Angle.ByDegree(42), Epoch.J2000);
            sut.Target.PositionAngle = 33;

            bool result = await InvokePrivateTask<bool>(sut, "CoordsToFraming");

            result.Should().BeTrue();
            applicationMediatorMock.Verify(x => x.ChangeTab(ApplicationTab.FRAMINGASSISTANT), Times.Once);
            framingAssistantMock.Verify(x => x.SetCoordinates(It.Is<DeepSkyObject>(d =>
                d.Name == "M31"
                && d.RotationPositionAngle == 33)), Times.Once);
        }

        /// <summary>
        /// Verifies the framing command wrapper starts the asynchronous coordinate transfer used by the UI command.
        /// </summary>
        [Test]
        public async Task CoordsToFramingCommand_InvokesAsyncFramingTransfer() {
            DeepSkyObjectContainer sut = CreateSut();
            sut.Target.TargetName = "M31";
            sut.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(1.5), Angle.ByDegree(42), Epoch.J2000);
            TaskCompletionSource<DeepSkyObject> sentTarget = new TaskCompletionSource<DeepSkyObject>();
            framingAssistantMock
                .Setup(x => x.SetCoordinates(It.IsAny<DeepSkyObject>()))
                .Callback<DeepSkyObject>(dso => sentTarget.TrySetResult(dso))
                .ReturnsAsync(true);

            sut.CoordsToFramingCommand.Execute(null);

            DeepSkyObject result = await sentTarget.Task.WaitAsync(TimeSpan.FromSeconds(1));
            result.Name.Should().Be("M31");
            applicationMediatorMock.Verify(x => x.ChangeTab(ApplicationTab.FRAMINGASSISTANT), Times.Once);
        }

        /// <summary>
        /// Verifies the Coords From Planetarium Updates Target And Uses Rotation When Available scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task CoordsFromPlanetarium_UpdatesTargetAndUsesRotationWhenAvailable() {
            DeepSkyObjectContainer sut = CreateSut();
            DeepSkyObject planetariumTarget = new DeepSkyObject(
                "M42",
                new Coordinates(Angle.ByHours(5.5), Angle.ByDegree(-5), Epoch.J2000),
                profile.AstrometrySettings.Horizon);
            planetariumMock.Setup(x => x.GetTarget()).ReturnsAsync(planetariumTarget);
            planetariumMock.SetupGet(x => x.CanGetRotationAngle).Returns(true);
            planetariumMock.Setup(x => x.GetRotationAngle()).ReturnsAsync(123);

            bool result = await InvokePrivateTask<bool>(sut, "CoordsFromPlanetarium");

            result.Should().BeTrue();
            sut.Name.Should().Be("M42");
            sut.Target.TargetName.Should().Be("M42");
            sut.Target.PositionAngle.Should().Be(123);
            sut.Target.InputCoordinates.Coordinates.RA.Should().Be(planetariumTarget.Coordinates.RA);
        }

        /// <summary>
        /// Verifies the planetarium command wrapper starts the asynchronous target import used by the UI command.
        /// </summary>
        [Test]
        public void CoordsFromPlanetariumCommand_InvokesAsyncPlanetariumImport() {
            DeepSkyObjectContainer sut = CreateSut();
            DeepSkyObject planetariumTarget = new DeepSkyObject(
                "M45",
                new Coordinates(Angle.ByHours(3.8), Angle.ByDegree(24), Epoch.J2000),
                profile.AstrometrySettings.Horizon);
            planetariumMock.Setup(x => x.GetTarget()).ReturnsAsync(planetariumTarget);

            sut.CoordsFromPlanetariumCommand.Execute(null);

            SpinWait.SpinUntil(() => sut.Target.TargetName == "M45", TimeSpan.FromSeconds(1)).Should().BeTrue();
            sut.Name.Should().Be("M45");
            sut.Target.InputCoordinates.Coordinates.RA.Should().Be(planetariumTarget.Coordinates.RA);
        }

        /// <summary>
        /// Verifies dropping a saved target into the container copies target metadata and exposure accounting from the source target container.
        /// </summary>
        [Test]
        public void DropTargetCommand_CopiesTargetAndExposureInfoFromTargetSequenceContainer() {
            DeepSkyObjectContainer sut = CreateSut();
            DeepSkyObjectContainer source = CreateSut();
            source.Target.TargetName = "M33";
            source.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(1.2), Angle.ByDegree(30), Epoch.J2000);
            source.Target.PositionAngle = 15;
            source.ExposureInfoList.Add(new ExposureInfo("Ha", 300, 100, 50, CaptureSequence.ImageTypes.LIGHT, 1, 1, 0));
            TargetSequenceContainer targetSequenceContainer = new TargetSequenceContainer(profileServiceMock.Object, source);

            sut.DropTargetCommand.Execute(new DropIntoParameters(targetSequenceContainer));

            sut.Name.Should().Be("M33");
            sut.Target.TargetName.Should().Be("M33");
            sut.Target.PositionAngle.Should().Be(15);
            sut.Target.InputCoordinates.Coordinates.RA.Should().Be(source.Target.InputCoordinates.Coordinates.RA);
            sut.ExposureInfoList.Should().ContainSingle().Which.Filter.Should().Be("Ha");
        }

        /// <summary>
        /// Verifies profile and nighttime events refresh target position, custom horizon, and cached nighttime data.
        /// </summary>
        [Test]
        public void ServiceEvents_UpdateTargetLocationHorizonAndNighttimeData() {
            DeepSkyObjectContainer sut = CreateSut();
            NighttimeData replacementNighttimeData = CreateNighttimeData();
            profile.AstrometrySettings.Latitude = 12;
            profile.AstrometrySettings.Longitude = 34;
            nighttimeCalculatorMock.Setup(x => x.Calculate(It.IsAny<DateTime?>())).Returns(replacementNighttimeData);
            sut.Target.DeepSkyObject = new DeepSkyObject("M31", new Coordinates(Angle.ByHours(1), Angle.ByDegree(1), Epoch.J2000), null);

            profileServiceMock.Raise(x => x.LocationChanged += null, EventArgs.Empty);
            profileServiceMock.Raise(x => x.HorizonChanged += null, EventArgs.Empty);
            nighttimeCalculatorMock.Raise(x => x.OnReferenceDayChanged += null, EventArgs.Empty);

            sut.NighttimeData.Should().BeSameAs(replacementNighttimeData);
            sut.Target.DeepSkyObject.Altitudes.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies re-entrant coordinate change notifications are ignored after the defensive recursion threshold is reached.
        /// </summary>
        [Test]
        public void TargetCoordinatesChanged_ReentrantDepthLimitReturnsWithoutParentRefresh() {
            DeepSkyObjectContainer sut = CreateSut();
            SetPrivateField(sut, "coordinatesChangedEventDepth", 5);

            Action act = () => InvokePrivate(sut, "Target_OnCoordinatesChanged", sut.Target, EventArgs.Empty);

            act.Should().NotThrow();
            GetPrivateField<int>(sut, "coordinatesChangedEventDepth").Should().Be(5);
        }

        /// <summary>
        /// Verifies exposure accounting substitutes default 1x1 binning when an exposure item has no binning mode assigned.
        /// </summary>
        [Test]
        public void ExposureInfo_UsesDefaultBinningWhenExposureItemHasNoBinning() {
            DeepSkyObjectContainer sut = CreateSut();
            Mock<IExposureItem> exposure = CreateExposureItem(CaptureSequence.ImageTypes.LIGHT);
            exposure.SetupProperty(x => x.Binning, null);

            ExposureInfo result = sut.GetOrCreateExposureCountForItemAndCurrentFilter(exposure.Object, roi: 0.5);

            result.BinningX.Should().Be(1);
            result.BinningY.Should().Be(1);
            result.ROI.Should().Be(0.5);
        }

        /// <summary>
        /// Verifies the string representation includes target name, coordinates, and rotation angle for diagnostics.
        /// </summary>
        [Test]
        public void ToString_IncludesTargetDetails() {
            DeepSkyObjectContainer sut = CreateSut();
            sut.Name = "DSO";
            sut.Target.TargetName = "M31";
            sut.Target.InputCoordinates.Coordinates = new Coordinates(Angle.ByHours(1), Angle.ByDegree(2), Epoch.J2000);
            sut.Target.PositionAngle = 45;

            string result = sut.ToString();

            result.Should().Contain("M31").And.Contain("45");
        }

        /// <summary>
        /// Verifies the Target Sequence Container Exposes Grouping And Collapses Clone When Profile Requests It scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void TargetSequenceContainer_ExposesGroupingAndCollapsesCloneWhenProfileRequestsIt() {
            profile.SequenceSettings.CollapseSequencerTemplatesByDefault = true;
            Mock<IDeepSkyObjectContainer> containerMock = new Mock<IDeepSkyObjectContainer>();
            Mock<IDeepSkyObjectContainer> cloneMock = new Mock<IDeepSkyObjectContainer>();
            cloneMock.SetupProperty(x => x.IsExpanded, true);
            containerMock.SetupGet(x => x.Name).Returns("M31");
            containerMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            TargetSequenceContainer sut = new TargetSequenceContainer(profileServiceMock.Object, containerMock.Object) {
                SubGroups = new[] { "Galaxy", "Spring" }
            };

            IDeepSkyObjectContainer clone = sut.Clone();

            sut.Name.Should().Be("M31");
            sut.Grouping.Should().Contain("Galaxy").And.Contain("Spring");
            sut.Parent.Should().BeNull();
            clone.Should().BeSameAs(cloneMock.Object);
            clone.IsExpanded.Should().BeFalse();
        }

        private DeepSkyObjectContainer CreateSut() {
            return new DeepSkyObjectContainer(
                profileServiceMock.Object,
                nighttimeCalculatorMock.Object,
                framingAssistantMock.Object,
                applicationMediatorMock.Object,
                planetariumFactoryMock.Object,
                cameraMediatorMock.Object,
                filterWheelMediatorMock.Object,
                symbolBrokerMock.Object);
        }

        private Mock<IExposureItem> CreateExposureItem(string imageType) {
            Mock<IExposureItem> exposure = new Mock<IExposureItem>();
            exposure.SetupProperty(x => x.ExposureTime, 60);
            exposure.SetupProperty(x => x.Gain, -1);
            exposure.SetupProperty(x => x.Offset, -1);
            exposure.SetupProperty(x => x.ImageType, imageType);
            exposure.SetupProperty(x => x.Binning, new BinningMode(1, 1));
            return exposure;
        }

        private static NighttimeData CreateNighttimeData() {
            DateTime referenceDate = new DateTime(2026, 4, 16, 12, 0, 0);
            RiseAndSetEvent sunRiseAndSet = new CustomRiseAndSet(referenceDate.AddHours(18), referenceDate.AddHours(7));

            return new NighttimeData(
                referenceDate,
                referenceDate,
                AstroUtil.MoonPhase.Unknown,
                null,
                null,
                null,
                sunRiseAndSet,
                null,
                null);
        }

        private static async Task<T> InvokePrivateTask<T>(object target, string methodName) {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            Task<T> task = (Task<T>)method.Invoke(target, Array.Empty<object>());
            return await task;
        }

        private static object InvokePrivate(object target, string methodName, params object[] args) {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            return method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value) {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.Should().NotBeNull();
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName) {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.Should().NotBeNull();
            return (T)field.GetValue(target);
        }
    }
}
