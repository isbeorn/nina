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
using NINA.Profile.Interfaces;

namespace NINA.Image.FileFormat {

    public class FileSaveInfo {
        public string FilePath { get; set; }
        public string FilePattern { get; set; }
        public string ForceExtension { get; set; }
        public FileTypeEnum FileType { get; set; } = FileTypeEnum.FITS;
        public TIFFCompressionTypeEnum TIFFCompressionType { get; set; } = TIFFCompressionTypeEnum.NONE;
        public XISFCompressionTypeEnum XISFCompressionType { get; set; } = XISFCompressionTypeEnum.NONE;
        public XISFChecksumTypeEnum XISFChecksumType { get; set; } = XISFChecksumTypeEnum.NONE;
        public bool XISFByteShuffling { get; set; } = false;
        public FITSCompressionTypeEnum FITSCompressionType { get; set; } = FITSCompressionTypeEnum.NONE;
        public bool FITSAddFzExtension { get; set; } = false;
        public bool FITSUseLegacyWriter { get; set; } = true;

        public FileSaveInfo(IProfileService profileService = null) {
            if (profileService != null) {
                FilePath = profileService.ActiveProfile.ImageFileSettings.FilePath;
                FilePattern = profileService.ActiveProfile.ImageFileSettings.FilePattern;
                FileType = profileService.ActiveProfile.ImageFileSettings.FileType;
                TIFFCompressionType = profileService.ActiveProfile.ImageFileSettings.TIFFCompressionType;
                XISFCompressionType = profileService.ActiveProfile.ImageFileSettings.XISFCompressionType;
                XISFByteShuffling = profileService.ActiveProfile.ImageFileSettings.XISFByteShuffling;
                XISFChecksumType = profileService.ActiveProfile.ImageFileSettings.XISFChecksumType;
                FITSCompressionType = profileService.ActiveProfile.ImageFileSettings.FITSCompressionType;
                FITSAddFzExtension = profileService.ActiveProfile.ImageFileSettings.FITSAddFzExtension;
                FITSUseLegacyWriter = profileService.ActiveProfile.ImageFileSettings.FITSUseLegacyWriter;
            }
        }

        public string GetExtension(string defaultExtension) {
            return string.IsNullOrEmpty(ForceExtension) ? defaultExtension : ForceExtension;
        }
    }
}