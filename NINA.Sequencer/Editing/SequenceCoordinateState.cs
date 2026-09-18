#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Editing {
    // Captures explicit configuration. Inherited values and expression results remain live.
    internal sealed record SequenceCoordinateState(Coordinates Value, bool NegativeDec, string RaDefinition, string DecDefinition,
        string RotationDefinition, bool RaExpression, bool DecExpression, bool Inherited) {
        public static SequenceCoordinateState Capture(InputCoordinates coordinates, Expression ra, Expression dec, Expression rotation, bool inherited) =>
            new(coordinates.Coordinates.Clone(), coordinates.NegativeDec, ra.Definition, dec.Definition, rotation.Definition,
                ra.IsExpression, dec.IsExpression, inherited);

        internal static void RestoreCoordinates(InputCoordinates coordinates, Expression ra, Expression dec, Coordinates value,
            bool negativeDec, string raDefinition, string decDefinition, ref bool protect, ref double lastRA, ref double lastDec) {
            bool previous = protect;
            try {
                ra.Definition = raDefinition;
                dec.Definition = decDefinition;
                Coordinates restored = value.Clone();
                if (ra.IsExpression && ra.IsValid) restored.RA = ra.Value;
                if (dec.IsExpression && dec.IsValid) restored.Dec = dec.Value;
                protect = true;
                coordinates.Coordinates = restored;
                coordinates.NegativeDec = restored.Dec == 0 ? negativeDec : restored.Dec < 0;
                lastRA = restored.RA;
                lastDec = restored.Dec;
            } finally { protect = previous; }
        }

        public static bool Equal(SequenceCoordinateState left, SequenceCoordinateState right) =>
            left.Inherited == right.Inherited && (left.Inherited || (
            left.RaDefinition == right.RaDefinition && left.DecDefinition == right.DecDefinition
            && left.RotationDefinition == right.RotationDefinition && left.Value.Epoch == right.Value.Epoch
            && (left.RaExpression || left.Value.RA == right.Value.RA)
            && (left.DecExpression || (left.Value.Dec == right.Value.Dec && left.NegativeDec == right.NegativeDec))));
    }
}