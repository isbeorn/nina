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
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NUnit.Framework;
using System;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class LoopWhileTest {

        /// <summary>
        /// Verifies the Clone Copies Predicate Expression Independently scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesPredicateExpressionIndependently() {
            LoopWhile sut = new LoopWhile();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.PredicateDefinition = "2 + 3";

            LoopWhile clone = (LoopWhile)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.Predicate.Should().Be(5);
            clone.PredicateExpression.Should().NotBeSameAs(sut.PredicateExpression);
            clone.PredicateExpression.Definition.Should().Be("2 + 3");

            clone.PredicateDefinition = "1";

            sut.PredicateExpression.Definition.Should().Be("2 + 3");
            sut.Predicate.Should().Be(5);
        }

        /// <summary>
        /// Verifies the Check Evaluates Predicate Expression Truthiness scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [TestCase("1", true)]
        [TestCase("0", false)]
        [TestCase("2 - 2", false)]
        [TestCase("3 > 2", true)]
        public void Check_EvaluatesPredicateExpressionTruthiness(string predicateDefinition, bool expectedResult) {
            LoopWhile sut = new LoopWhile();
            AttachToRoot(sut);
            sut.PredicateDefinition = predicateDefinition;

            sut.Check(null, null).Should().Be(expectedResult);
        }

        /// <summary>
        /// Verifies the Check Empty Predicate Throws Sequence Entity Failure scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Check_EmptyPredicate_ThrowsSequenceEntityFailure() {
            LoopWhile sut = new LoopWhile();
            AttachToRoot(sut);

            Action act = () => sut.Check(null, null);

            act.Should().Throw<SequenceEntityFailedException>();
        }

        /// <summary>
        /// Verifies the Check Syntax Error Throws Sequence Entity Failure scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Check_SyntaxError_ThrowsSequenceEntityFailure() {
            LoopWhile sut = new LoopWhile();
            AttachToRoot(sut);
            sut.PredicateDefinition = "1 +";

            Action act = () => sut.Check(null, null);

            act.Should().Throw<SequenceEntityFailedException>();
        }

        /// <summary>
        /// Verifies the Validate Syntax Error Returns Issue scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_SyntaxError_ReturnsIssue() {
            LoopWhile sut = new LoopWhile {
                PredicateDefinition = "1 +"
            };

            bool valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();
        }

        private static void AttachToRoot(LoopWhile condition) {
            SequenceRootContainer root = new SequenceRootContainer();
            root.Add(condition);
        }
    }
}
