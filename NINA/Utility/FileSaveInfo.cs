#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * Copyright 2020 Dale Ghent <daleg@elemental.org>
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