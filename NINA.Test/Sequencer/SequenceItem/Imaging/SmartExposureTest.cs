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
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Trigger.Guider;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Test.Sequencer.SequenceItem.Imaging {

    [TestFixture]
    public class SmartExposureTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IImageHistoryVM> imageHistoryMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Mock<IProfile> profileMock;
        private Mock<IImageFileSettings> imageFileSettingsMock;
        private Mock<IFilterWheelSettings> filterWheelSettingsMock;
        private Mock<IGuiderSettings> guiderSettingsMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            imageHistoryMock = new Mock<IImageHistoryVM>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            profileMock = new Mock<IProfile>();
            imageFileSettingsMock = new Mock<IImageFileSettings>();
            filterWheelSettingsMock = new Mock<IFilterWheelSettings>();
            guiderSettingsMock = new Mock<IGuiderSettings>();

            Core.Utility.ObserveAllCollection<FilterInfo> filters = new Core.Utility.ObserveAllCollection<FilterInfo> {
                new FilterInfo("Red", 0, 1)
            };

            imageFileSettingsMock.SetupGet(x => x.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            filterWheelSettingsMock.SetupGet(x => x.FilterWheelFilters).Returns(filters);
            guiderSettingsMock.SetupGet(x => x.SettleTimeout).Returns(0);
            profileMock.SetupGet(x => x.ImageFileSettings).Returns(imageFileSettingsMock.Object);
            profileMock.SetupGet(x => x.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);
            profileMock.SetupGet(x => x.GuiderSettings).Returns(guiderSettingsMock.Object);
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profileMock.Object);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = true });
            imageHistoryMock.SetupGet(x => x.ImageHistory).Returns(new List<ImageHistoryPoint>());
        }

        /// <summary>
        /// Verifies the Constructor Creates Expected Immutable Sequence Pieces scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Constructor_CreatesExpectedImmutableSequencePieces() {
            SmartExposure sut = CreateSut();

            sut.GetSwitchFilter().Should().NotBeNull();
            sut.GetTakeExposure().Should().NotBeNull();
            sut.GetLoopCondition().Should().NotBeNull();
            sut.GetDitherAfterExposures().Should().NotBeNull();
            sut.Items.Should().HaveCount(2);
            sut.Conditions.Should().HaveCount(1);
            sut.Triggers.Should().HaveCount(1);
        }

        /// <summary>
        /// Verifies the Iterations Definition Updates Internal Loop Condition scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void IterationsDefinition_UpdatesInternalLoopCondition() {
            SmartExposure sut = CreateSut();

            sut.IterationsDefinition = "2 + 3";

            sut.Iterations.Should().Be(5);
            sut.GetLoopCondition().Iterations.Should().Be(5);
        }

        /// <summary>
        /// Verifies the Clone Copies Expression And Child Items Independently scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesExpressionAndChildItemsIndependently() {
            SmartExposure sut = CreateSut();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.IterationsDefinition = "2 + 3";
            sut.GetSwitchFilter().ComboBoxText = "Red";
            sut.GetTakeExposure().ExposureTimeDefinition = "30 + 15";
            sut.GetDitherAfterExposures().AfterExposuresDefinition = "1 + 1";

            SmartExposure clone = (SmartExposure)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.Iterations.Should().Be(5);
            clone.IterationsExpression.Should().NotBeSameAs(sut.IterationsExpression);
            clone.GetLoopCondition().Should().NotBeSameAs(sut.GetLoopCondition());
            clone.GetLoopCondition().Iterations.Should().Be(5);
            clone.GetSwitchFilter().Should().NotBeSameAs(sut.GetSwitchFilter());
            clone.GetTakeExposure().Should().NotBeSameAs(sut.GetTakeExposure());
            clone.GetDitherAfterExposures().Should().NotBeSameAs(sut.GetDitherAfterExposures());
            clone.GetTakeExposure().ExposureTime.Should().Be(45);
            clone.GetDitherAfterExposures().AfterExposures.Should().Be(2);

            clone.IterationsDefinition = "1";

            sut.IterationsExpression.Definition.Should().Be("2 + 3");
            sut.GetLoopCondition().Iterations.Should().Be(5);
        }

        /// <summary>
        /// Verifies the Validate Expression Syntax Error Returns Issue scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_ExpressionSyntaxError_ReturnsIssue() {
            SmartExposure sut = CreateSut();
            sut.IterationsDefinition = "0";

            bool valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();
        }

        [Test]
        public void ExposureTimeDefinition_TimeOnlyBrokerSymbol_EvaluatesOnSmartExposureChildItem() {
            SmartExposure sut = CreateSut();
            TimeOnly timeOnly = new TimeOnly(0, 1, 30);
            Mock<ISymbolBroker> symbolBrokerMock = new Mock<ISymbolBroker>();

            object timeOnlyValue = timeOnly;
            symbolBrokerMock
                .Setup(x => x.TryGetValue("TemporalTest_TimeOnly", out timeOnlyValue))
                .Returns(true);

            sut.GetTakeExposure().SymbolBroker = symbolBrokerMock.Object;
            sut.GetTakeExposure().ExposureTimeExpression.SymbolBroker = symbolBrokerMock.Object;
            sut.GetTakeExposure().ExposureTimeDefinition = "TemporalTest_TimeOnly";

            sut.GetTakeExposure().ExposureTime.Should().Be(90);
            sut.Validate().Should().BeTrue();
        }

        private SmartExposure CreateSut() {
            return new SmartExposure(
                profileServiceMock.Object,
                cameraMediatorMock.Object,
                imagingMediatorMock.Object,
                imageSaveMediatorMock.Object,
                imageHistoryMock.Object,
                filterWheelMediatorMock.Object,
                guiderMediatorMock.Object,
                safetyMonitorMediatorMock.Object);
        }
    }
}
