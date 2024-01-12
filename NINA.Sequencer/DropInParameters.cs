#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;

namespace NINA.Sequencer.DragDrop {

    public class DropIntoParameters {

        public DropIntoParameters(IDroppable source, IDroppable target, DropTargetEnum? position) {
            Source = source;
            Target = target;
            Position = position;
        }

        public DropIntoParameters(IDroppable source, IDroppable target) : this(source, target, null) {
        }

        public DropIntoParameters(IDroppable source) : this(source, null, null) {
        }

        public IDroppable Source { get; }
        public IDroppable Target { get; set; }
        public DropTargetEnum? Position { get; set; }

        public bool Duplicate { get; set; } = false;
    }
}