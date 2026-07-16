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
using NINA.Astrometry;
using NINA.Sequencer.SequenceItem.Telescope;

namespace NINA.Test.Sequencer.SequenceItem.Telescope {

    [TestFixture]
    internal class CoordinatesInstructionTest {
        [Test]
        public void FirstDeclinationComponentEdit_UpdatesDeclinationExpression() {
            var sut = CreateInstruction();

            sut.Coordinates.DecDegrees = 45;

            sut.Coordinates.Coordinates.Dec.Should().Be(45);
            sut.RaExpression.Definition.Should().Be("5 + 5");
            sut.DecExpression.Definition.Should().Be("45");
        }

        [Test]
        public void ReplacingBothCoordinates_UpdatesBothExpressions() {
            var sut = CreateInstruction();

            sut.Coordinates.Coordinates = new Coordinates(Angle.ByHours(12), Angle.ByDegree(60), Epoch.J2000);

            sut.RaExpression.Definition.Should().Be("12");
            sut.DecExpression.Definition.Should().Be("60");
        }

        private static CoordinatesInstruction CreateInstruction() {
            var sut = new CoordinatesInstruction {
                Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.J2000))
            };
            sut.RaExpression.Definition = "5 + 5";
            sut.DecExpression.Definition = "20";
            sut.AfterParentChanged();
            return sut;
        }
    }
}