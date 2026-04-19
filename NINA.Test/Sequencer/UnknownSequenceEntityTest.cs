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
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer {

    [TestFixture]
    public class UnknownSequenceEntityTest {

        /// <summary>
        /// Verifies the Unknown Sequence Item Validates False And Skips Execution scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task UnknownSequenceItem_ValidatesFalseAndSkipsExecution() {
            UnknownSequenceItem sut = new UnknownSequenceItem("MissingItem");

            sut.Name.Should().Contain("MissingItem");
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();

            UnknownSequenceItem clone = (UnknownSequenceItem)sut.Clone();
            clone.Name.Should().Contain("MissingItem");

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*Unknown instruction*");
        }

        /// <summary>
        /// Verifies the Unknown Sequence Condition Validates False And Never Checks True scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void UnknownSequenceCondition_ValidatesFalseAndNeverChecksTrue() {
            UnknownSequenceCondition sut = new UnknownSequenceCondition("MissingCondition");

            sut.Name.Should().Contain("MissingCondition");
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();
            sut.Check(MockSequenceItem(), MockSequenceItem()).Should().BeFalse();

            UnknownSequenceCondition clone = (UnknownSequenceCondition)sut.Clone();
            clone.Name.Should().Contain("MissingCondition");
        }

        /// <summary>
        /// Verifies the Unknown Sequence Trigger Validates False Never Triggers And Skips Execution scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task UnknownSequenceTrigger_ValidatesFalseNeverTriggersAndSkipsExecution() {
            UnknownSequenceTrigger sut = new UnknownSequenceTrigger("MissingTrigger");

            sut.Name.Should().Contain("MissingTrigger");
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();
            sut.ShouldTrigger(MockSequenceItem(), MockSequenceItem()).Should().BeFalse();

            UnknownSequenceTrigger clone = (UnknownSequenceTrigger)sut.Clone();
            clone.Name.Should().Contain("MissingTrigger");

            Func<Task> act = () => sut.Execute(new SequentialContainer(), default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*Unknown trigger*");
        }

        /// <summary>
        /// Verifies the Unknown Sequence Container Validates False And Skips Execution scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task UnknownSequenceContainer_ValidatesFalseAndSkipsExecution() {
            UnknownSequenceContainer sut = new UnknownSequenceContainer("MissingContainer");

            sut.Name.Should().Contain("MissingContainer");
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();

            UnknownSequenceContainer clone = (UnknownSequenceContainer)sut.Clone();
            clone.Name.Should().Contain("MissingContainer");

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*Unknown instruction set*");
        }

        private static ISequenceItem MockSequenceItem() {
            return new UnknownSequenceItem("Other");
        }
    }
}
