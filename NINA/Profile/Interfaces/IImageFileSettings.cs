#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Enum;

namespace NINA.Profile {

    public interface IImageFileSettings : ISettings {
        string FilePath { get; set; }
        string FilePattern { get; set; }
        FileTypeEnum FileType { get; set; }
        TIFFCompressionTypeEnum TIFFCompressionType { get; set; }
        XISFCompressionTypeEnum XISFCompressionType { get; set; }
        XISFChecksumTypeEnum XISFChecksumType { get; set; }
        bool XISFByteShuffling { get; set; }
    }
}