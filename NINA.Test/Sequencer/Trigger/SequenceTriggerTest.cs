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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Trigger {

    [TestFixture]
    public class SequenceTriggerTest {
        public Mock<SequenceTrigger> sequenceTriggerMock;
        public Mock<ISequenceContainer> sequenceContainerMock;
        public Mock<IProgress<ApplicationStatus>> applicationStatusMock;

        [SetUp]
        public void Setup() {
            sequenceTriggerMock = new Mock<SequenceTrigger>();
            sequenceContainerMock = new Mock<ISequenceContainer>();
            applicationStatusMock = new Mock<IProgress<ApplicationStatus>>();
        }

        [Test]
        public async Task Trigger_Failed_ValidationFailure_Status() {
            //Arrange
            sequenceTriggerMock.CallBase = true;

            //Act
            sequenceTriggerMock
                .Setup(x => x.Execute(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Throws(new SequenceEntityFailedValidationException());
            var sut = sequenceTriggerMock.Object;

            //Assert
            try {
                await sut.Run(default, default, default);
            } catch (SequenceEntityFailedValidationException) {
            }

            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }
    }
}