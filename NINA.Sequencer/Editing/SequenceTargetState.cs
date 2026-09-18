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
using NINA.Sequencer.Container;

namespace NINA.Sequencer.Editing {
    internal sealed record SequenceTargetState(string TargetName, SequenceCoordinateValue Coordinates, double PositionAngle) {
        public static SequenceTargetState Capture(InputTarget target) => target == null ? null
            : new(target.TargetName, SequenceCoordinateValue.Capture(target.InputCoordinates), target.PositionAngle);
        public static SequenceTargetState Capture(LinkedTemplateTargetOverride target) => target == null ? null
            : new(target.TargetName, SequenceCoordinateValue.Capture(target.InputCoordinates), target.PositionAngle);
        public void Restore(InputTarget target) {
            target.TargetName = TargetName;
            target.InputCoordinates ??= new InputCoordinates();
            Coordinates?.Restore(target.InputCoordinates);
            target.PositionAngle = PositionAngle;
        }
        public LinkedTemplateTargetOverride ToOverride() {
            var target = new LinkedTemplateTargetOverride { TargetName = TargetName, PositionAngle = PositionAngle };
            Coordinates?.Restore(target.InputCoordinates);
            return target;
        }
    }
}