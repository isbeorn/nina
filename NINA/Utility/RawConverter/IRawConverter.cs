#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Utility.Enum;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.RawConverter {

    public class RawConverter {

        public static IRawConverter CreateInstance(RawConverterEnum converter) {
            switch (converter) {
                case RawConverterEnum.DCRAW:
                    return new DCRaw();

                case RawConverterEnum.FREEIMAGE:
                    return new FreeImageConverter();

                default:
                    return new FreeImageConverter();
            }
        }
    }

    public interface IRawConverter {

        Task<IImageData> Convert(
            MemoryStream s,
            int bitDepth,
            string rawType,
            ImageMetaData metaData,
            CancellationToken token = default);
    }
}