#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Image.Interfaces;
using System;

namespace NINA.Image.RawConverter {

    public class RawConverterFactory {

        public static IRawConverter CreateInstance(IImageDataFactory imageDataFactory) {
            return new LibRawConverter(imageDataFactory);
        }

#pragma warning disable CS0618
        [Obsolete("RAW converter selection is obsolete. Use CreateInstance(IImageDataFactory).")]
        public static IRawConverter CreateInstance(RawConverterEnum converter, IImageDataFactory imageDataFactory) {
            return CreateInstance(imageDataFactory);
        }
#pragma warning restore CS0618
    }
}
