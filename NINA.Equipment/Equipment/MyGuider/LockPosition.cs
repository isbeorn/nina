#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using System;
using System.Numerics;

namespace NINA.Equipment.Equipment.MyGuider {

    public class LockPosition {

        public LockPosition(float x, float y) {
            X = x;
            Y = y;
            EventTime = DateTime.Now;
        }

        public float X { get; }
        public float Y { get; }
        public DateTime EventTime { get; }

        public static bool operator ==(LockPosition lhs, LockPosition rhs) {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(LockPosition lhs, LockPosition rhs) {
            return !Equals(lhs, rhs);
        }

        public override bool Equals(object obj) {
            return obj is LockPosition pos && pos.X == X && pos.Y == Y;
        }

        public override int GetHashCode() => (X, Y, EventTime).GetHashCode();

        public override string ToString() {
            return $"x={X} y={Y}";
        }
    }
}