#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility.Enum;

namespace NINA.Utility {

    public class FileSaveInfo {
        public string FilePath { get; set; }
        public string FilePattern { get; set; }
        public FileTypeEnum FileType { get; set; } = FileTypeEnum.FITS;
        public TIFFCompressionTypeEnum TIFFCompressionType { get; set; } = TIFFCompressionTypeEnum.NONE;
        public XISFCompressionTypeEnum XISFCompressionType { get; set; } = XISFCompressionTypeEnum.NONE;
        public XISFChecksumTypeEnum XISFChecksumType { get; set; } = XISFChecksumTypeEnum.NONE;
        public bool XISFByteShuffling { get; set; } = false;

        public FileSaveInfo(IProfileService profileService = null) {
            if (profileService != null) {
                FilePath = profileService.ActiveProfile.ImageFileSettings.FilePath;
                FilePattern = profileService.ActiveProfile.ImageFileSettings.FilePattern;
                FileType = profileService.ActiveProfile.ImageFileSettings.FileType;
                TIFFCompressionType = profileService.ActiveProfile.ImageFileSettings.TIFFCompressionType;
                XISFCompressionType = profileService.ActiveProfile.ImageFileSettings.XISFCompressionType;
                XISFByteShuffling = profileService.ActiveProfile.ImageFileSettings.XISFByteShuffling;
                XISFChecksumType = profileService.ActiveProfile.ImageFileSettings.XISFChecksumType;
            }
        }
    }
}