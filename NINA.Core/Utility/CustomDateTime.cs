#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Core.Utility {

    public interface ICustomDateTime {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }

    public class SystemDateTime : ICustomDateTime {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime MinValue => DateTime.MinValue;
        public DateTime MaxValue => DateTime.MaxValue;
    }
}