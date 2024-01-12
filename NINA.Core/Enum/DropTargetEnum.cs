#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Core.Enum {

    public enum DropTargetEnum {
        Top = 0b00000001,
        Bottom = 0b00000010,
        Center = 0b00000100,
        Left = 0b00001000,
        Right = 0b00010000,
        None = 0b00000000
    }

    public enum DragOverDisplayAnchor {
        Left,
        Right
    }
}