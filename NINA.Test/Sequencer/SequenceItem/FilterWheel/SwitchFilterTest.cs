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
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;

namespace NINA.Test.Sequencer.SequenceItem.FilterWheel {

    [TestFixture]
    internal class SwitchFilterTest {
        public Mock<IFilterWheelMediator> fwMediatorMock;
        public Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            fwMediatorMock = new Mock<IFilterWheelMediator>();
            profileServiceMock = new Mock<IProfileService>();
        }

        private delegate void TryGetValueCallback(string key, out object value);

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (SwitchFilter)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Filter.Should().BeSameAs(sut.Filter);
        }

        [Test]
        public void Validate_NoIssues() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_NoFilterSelected_NoIssue() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().HaveCount(0);
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            sut.Filter = new FilterInfo();
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        //[Test]
        //public async Task Execute_NoIssues_LogicCalled() {
        //    var filter = new FilterInfo();
        //    fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });

        //    var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
        //    sut.Filter = filter;
        //    await sut.Execute(default, default);

        //    fwMediatorMock.Verify(x => x.ChangeFilter(It.Is<FilterInfo>(f => f == filter), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
        //}

        [Test]
        public Task Execute_NoFilterSelected_Skipped() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            Func<Task> act = () => { return sut.Execute(default, default); };

            fwMediatorMock.Verify(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Never);
            return act.Should().ThrowAsync<SequenceItemSkippedException>();
        }

        [Test]
        public Task Execute_HasIssues_LogicNotCalled() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            Func<Task> act = () => { return sut.Execute(default, default); };

            fwMediatorMock.Verify(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Never);
            return act.Should().ThrowAsync<SequenceItemSkippedException>(string.Join(",", sut.Issues));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(30, 30)]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate(int minutes, int expected) {
            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void Validate_WithMockedFilterWheel_FiltersRetrievedCorrectly() {
            var filters = new Core.Utility.ObserveAllCollection<FilterInfo> {
                new FilterInfo("Red", 0, 1),
                new FilterInfo("Green", 0, 2),
                new FilterInfo("Blue", 0, 3),
                new FilterInfo("Luminance", 0, 4)
            };

            var profileMock = new Mock<IProfile>();
            var filterWheelSettingsMock = new Mock<NINA.Profile.Interfaces.IFilterWheelSettings>();
            filterWheelSettingsMock.Setup(x => x.FilterWheelFilters).Returns(filters);
            profileMock.Setup(x => x.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);
            profileServiceMock.Setup(x => x.ActiveProfile).Returns(profileMock.Object);

            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            sut.Validate();

            sut.FilterNames.Should().HaveCount(4);
            sut.FilterNames.Should().Contain("Red");
            sut.FilterNames.Should().Contain("Green");
            sut.FilterNames.Should().Contain("Blue");
            sut.FilterNames.Should().Contain("Luminance");
        }

        [Test]
        public async Task Execute_SwitchToSpecificFilter_With_ComboBox_Expression() {
            var redFilter = new FilterInfo("Red", 0, 1);
            var greenFilter = new FilterInfo("Green", 0, 2);
            var blueFilter = new FilterInfo("Blue", 0, 3);
            var luminanceFilter = new FilterInfo("Luminance", 0, 4);

            var filters = new Core.Utility.ObserveAllCollection<FilterInfo> {
                redFilter,
                greenFilter,
                blueFilter,
                luminanceFilter
            };

            var profileMock = new Mock<IProfile>();
            var filterWheelSettingsMock = new Mock<NINA.Profile.Interfaces.IFilterWheelSettings>();
            filterWheelSettingsMock.Setup(x => x.FilterWheelFilters).Returns(filters);
            profileMock.Setup(x => x.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);

            var localProfileServiceMock = new Mock<IProfileService>();
            localProfileServiceMock.Setup(x => x.ActiveProfile).Returns(profileMock.Object);

            var symbolBrokerMock = new Mock<NINA.Sequencer.Logic.ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.TryGetValue("Red", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 1.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Green", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 2.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Blue", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 3.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Luminance", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 4.0; }))
                .Returns(true);

            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });
            fwMediatorMock.Setup(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync(greenFilter);

            var sut = new SwitchFilter(localProfileServiceMock.Object, fwMediatorMock.Object);
            sut.SymbolBroker = symbolBrokerMock.Object;

            // Force XfilterExpression initialization and ensure it has the SymbolBroker
            var expr = sut.XfilterExpression;
            expr.SymbolBroker = symbolBrokerMock.Object;

            sut.ComboBoxText = "Green";

            await sut.Execute(default, default);

            sut.Filter.Should().NotBeNull();
            sut.Filter.Name.Should().Be("Green");

            fwMediatorMock.Verify(x => x.ChangeFilter(
                It.Is<FilterInfo>(f => f.Name == "Green" && f.Position == 2),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
        }

        [Test]
        public async Task Execute_SwitchToSpecificFilter_FromComboBox_WithConstantExpression() {
            NINA.Sequencer.Logic.UserSymbol.SymbolCache.Clear();
            NINA.Sequencer.Logic.UserSymbol.ClearUserSymbols();

            var redFilter = new FilterInfo("Red", 0, 1);
            var greenFilter = new FilterInfo("Green", 0, 2);
            var blueFilter = new FilterInfo("Blue", 0, 3);
            var luminanceFilter = new FilterInfo("Luminance", 0, 4);

            var filters = new Core.Utility.ObserveAllCollection<FilterInfo> {
                redFilter,
                greenFilter,
                blueFilter,
                luminanceFilter
            };

            var profileMock = new Mock<IProfile>();
            var filterWheelSettingsMock = new Mock<NINA.Profile.Interfaces.IFilterWheelSettings>();
            filterWheelSettingsMock.Setup(x => x.FilterWheelFilters).Returns(filters);
            profileMock.Setup(x => x.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);

            var localProfileServiceMock = new Mock<IProfileService>();
            localProfileServiceMock.Setup(x => x.ActiveProfile).Returns(profileMock.Object);

            var symbolBrokerMock = new Mock<NINA.Sequencer.Logic.ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.TryGetValue("Red", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 1.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Green", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 2.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Blue", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 3.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Luminance", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 4.0; }))
                .Returns(true);

            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });
            fwMediatorMock.Setup(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync(greenFilter);

            var container = new NINA.Sequencer.Container.SequentialContainer {
                SymbolBroker = symbolBrokerMock.Object
            };

            var constant = new NINA.Sequencer.SequenceItem.Expressions.Constant {
                SymbolBroker = symbolBrokerMock.Object
            };
            constant.Expr = new NINA.Sequencer.Logic.Expression("2", constant) {
                SymbolBroker = symbolBrokerMock.Object
            };
            container.Add(constant);
            constant.Identifier = "f";

            var sut = new SwitchFilter(localProfileServiceMock.Object, fwMediatorMock.Object);
            container.Add(sut);
            sut.SymbolBroker = symbolBrokerMock.Object;

            sut.ComboBoxText = "f";

            await sut.Execute(default, default);

            sut.Filter.Should().NotBeNull();
            sut.Filter.Name.Should().Be("Green");
            sut.Filter.Position.Should().Be(2);

            fwMediatorMock.Verify(x => x.ChangeFilter(
                It.Is<FilterInfo>(f => f.Name == "Green" && f.Position == 2),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
        }

        [Test]
        public async Task Execute_SwitchToSpecificFilter_With_ComboBox_Selection() {
            var redFilter = new FilterInfo("Red", 0, 1);
            var greenFilter = new FilterInfo("Green", 0, 2);
            var blueFilter = new FilterInfo("Blue", 0, 3);
            var luminanceFilter = new FilterInfo("Luminance", 0, 4);

            var filters = new Core.Utility.ObserveAllCollection<FilterInfo> {
                redFilter,
                greenFilter,
                blueFilter,
                luminanceFilter
            };

            var profileMock = new Mock<IProfile>();
            var filterWheelSettingsMock = new Mock<NINA.Profile.Interfaces.IFilterWheelSettings>();
            filterWheelSettingsMock.Setup(x => x.FilterWheelFilters).Returns(filters);
            profileMock.Setup(x => x.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);

            var localProfileServiceMock = new Mock<IProfileService>();
            localProfileServiceMock.Setup(x => x.ActiveProfile).Returns(profileMock.Object);

            var symbolBrokerMock = new Mock<NINA.Sequencer.Logic.ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.TryGetValue("Red", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 1.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Green", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 2.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Blue", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 3.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Luminance", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 4.0; }))
                .Returns(true);

            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });
            fwMediatorMock.Setup(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync(greenFilter);

            var sut = new SwitchFilter(localProfileServiceMock.Object, fwMediatorMock.Object);
            sut.SymbolBroker = symbolBrokerMock.Object;

            // Force XfilterExpression initialization and ensure it has the SymbolBroker
            var expr = sut.XfilterExpression;
            expr.SymbolBroker = symbolBrokerMock.Object;

            sut.SelectedFilter = 2; // This simulates the user selecting "Green" from the ComboBox (index 2 corresponds to position 2, which is "Green")

            await sut.Execute(default, default);

            sut.Filter.Should().NotBeNull();
            sut.Filter.Name.Should().Be("Green");

            fwMediatorMock.Verify(x => x.ChangeFilter(
                It.Is<FilterInfo>(f => f.Name == "Green" && f.Position == 2),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
        }

        [Test]
        public async Task Execute_UpgradeFrom32_FilterDirectlySet() {
            var redFilter = new FilterInfo("Red", 0, 1);
            var greenFilter = new FilterInfo("Green", 0, 2);
            var blueFilter = new FilterInfo("Blue", 0, 3);
            var luminanceFilter = new FilterInfo("Luminance", 0, 4);

            var filters = new Core.Utility.ObserveAllCollection<FilterInfo> {
                redFilter,
                greenFilter,
                blueFilter,
                luminanceFilter
            };

            var profileMock = new Mock<IProfile>();
            var filterWheelSettingsMock = new Mock<NINA.Profile.Interfaces.IFilterWheelSettings>();
            filterWheelSettingsMock.Setup(x => x.FilterWheelFilters).Returns(filters);
            profileMock.Setup(x => x.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);

            var localProfileServiceMock = new Mock<IProfileService>();
            localProfileServiceMock.Setup(x => x.ActiveProfile).Returns(profileMock.Object);

            var symbolBrokerMock = new Mock<NINA.Sequencer.Logic.ISymbolBroker>();
            symbolBrokerMock.Setup(x => x.TryGetValue("Red", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 1.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Green", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 2.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Blue", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 3.0; }))
                .Returns(true);
            symbolBrokerMock.Setup(x => x.TryGetValue("Luminance", out It.Ref<object>.IsAny))
                .Callback(new TryGetValueCallback((string key, out object value) => { value = 4.0; }))
                .Returns(true);

            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });
            fwMediatorMock.Setup(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync(greenFilter);

            var sut = new SwitchFilter(localProfileServiceMock.Object, fwMediatorMock.Object);
            sut.SymbolBroker = symbolBrokerMock.Object;

            // Initialize XfilterExpression before setting Filter for upgrade scenario
            var expr = sut.XfilterExpression;
            expr.SymbolBroker = symbolBrokerMock.Object;

            // Simulate upgrade from 3.2 where Filter property was directly set during deserialization
            sut.Filter = greenFilter;

            await sut.Execute(default, default);

            sut.Filter.Should().NotBeNull();
            sut.Filter.Name.Should().Be("Green");
            sut.Filter.Position.Should().Be(2);
            sut.ComboBoxText.Should().Be("Green");

            fwMediatorMock.Verify(x => x.ChangeFilter(
                It.Is<FilterInfo>(f => f.Name == "Green" && f.Position == 2),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
        }
    }
}