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
using NINA.Equipment.Equipment.MySwitch;
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Switch;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Equipment.Interfaces;

namespace NINA.Test.Sequencer.SequenceItem.Switch {

    internal class SetSwitchValueTest {
        public Mock<ISwitchMediator> switchMediatorMock;

        [SetUp]
        public void Setup() {
            switchMediatorMock = new Mock<ISwitchMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new SetSwitchValue(switchMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (SetSwitchValue)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.SwitchIndex.Should().Be(sut.SwitchIndex);
            item2.Value.Should().Be(sut.Value);
        }

        [Test]
        public void Validate_NoIssues() {
            switchMediatorMock.Setup(x => x.GetInfo()).Returns(new SwitchInfo() { Connected = true, WritableSwitches = new System.Collections.ObjectModel.ReadOnlyCollection<IWritableSwitch>(new List<IWritableSwitch>() { new Mock<IWritableSwitch>().Object }) });

            var sut = new SetSwitchValue(switchMediatorMock.Object);
            sut.SwitchIndex = 0;
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            switchMediatorMock.Setup(x => x.GetInfo()).Returns(new SwitchInfo() { Connected = false });

            var sut = new SetSwitchValue(switchMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(0, 0, 1, 1, true)]
        [TestCase(0.5, 0, 1, 0.5, true)]
        [TestCase(1, 1, 1, 1, true)]
        [TestCase(2, 1, 1, 1, false)]
        [TestCase(123, 1, 150, 1, true)]
        [TestCase(123.5, 1, 150, 0.2, true)]
        [TestCase(123.5, 1, 150, 0.5, true)]
        [TestCase(9, 1, 150, 3, true)]
        [TestCase(10, 1, 150, 3, true)]
        public void Validate_SwitchValueIssues_Test(double value, double minimum, double maximum, double stepsize, bool valid) {
            var s = new Mock<IWritableSwitch>();
            s.SetupGet(x => x.Minimum).Returns(minimum);
            s.SetupGet(x => x.Maximum).Returns(maximum);
            s.SetupGet(x => x.StepSize).Returns(stepsize);

            switchMediatorMock.Setup(x => x.GetInfo()).Returns(new SwitchInfo() {
                Connected = true,
                WritableSwitches = new System.Collections.ObjectModel.ReadOnlyCollection<IWritableSwitch>(
                    new List<IWritableSwitch>() {
                        s.Object
                    }
                )
            });

            var sut = new SetSwitchValue(switchMediatorMock.Object);
            sut.SwitchIndex = 0;
            sut.SelectedSwitch = s.Object;
            sut.Value = value;
            var validate = sut.Validate();

            validate.Should().Be(valid);

            sut.Issues.Should().HaveCount(valid ? 0 : 1);
        }

        [Test]
        [TestCase(0, 10.5)]
        [TestCase(0, -10.5)]
        [TestCase(1, 10.5)]
        [TestCase(2, 10.5)]
        public async Task Execute_NoIssues_LogicCalled(short index, double value) {
            var switch1 = new Mock<IWritableSwitch>();
            switch1.Setup(x => x.Minimum).Returns(-20);
            switch1.Setup(x => x.Maximum).Returns(20);
            var switch2 = new Mock<IWritableSwitch>();
            switch2.Setup(x => x.Minimum).Returns(0);
            switch2.Setup(x => x.Maximum).Returns(20);
            var switch3 = new Mock<IWritableSwitch>();
            switch3.Setup(x => x.Minimum).Returns(0);
            switch3.Setup(x => x.Maximum).Returns(20);

            switchMediatorMock.Setup(x => x.GetInfo()).Returns(new SwitchInfo() {
                Connected = true,
                WritableSwitches = new System.Collections.ObjectModel.ReadOnlyCollection<IWritableSwitch>(
                    new List<IWritableSwitch>() {
                        switch1.Object,
                        switch2.Object,
                        switch3.Object
                    }
                )
            });

            var sut = new SetSwitchValue(switchMediatorMock.Object);
            sut.SwitchIndex = index;
            sut.Value = value;
            await sut.Execute(default, default);

            switchMediatorMock.Verify(x => x.SetSwitchValue(It.Is<short>(idx => idx == index), It.Is<double>(t => t == value), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new SetSwitchValue(switchMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();
            duration.Should().Be(TimeSpan.Zero);
        }
    }
}