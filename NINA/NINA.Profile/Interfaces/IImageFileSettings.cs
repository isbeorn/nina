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

namespace NINA.Profile.Interfaces {

    public interface IImageFileSettings : ISettings {
        string FilePath { get; set; }
        string FilePattern { get; set; }
        string FilePatternDARK { get; set; }
        string FilePatternBIAS { get; set; }
        string FilePatternFLAT { get; set; }
        FileTypeEnum FileType { get; set; }
        TIFFCompressionTypeEnum TIFFCompressionType { get; set; }
        XISFCompressionTypeEnum XISFCompressionType { get; set; }
        XISFChecksumTypeEnum XISFChecksumType { get; set; }
        bool XISFByteShuffling { get; set; }
        FITSCompressionTypeEnum FITSCompressionType { get; set; }
        bool FITSAddFzExtension { get; set; }
        bool FITSUseLegacyWriter { get; set; }
        string GetFilePattern(string imageType);
    }
}