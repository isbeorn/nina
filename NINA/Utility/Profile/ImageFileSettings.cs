#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

#endregion "copyright"

using NINA.Utility.Enum;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class ImageFileSettings : Settings, IImageFileSettings {
        private string filePath = string.Empty;

        [DataMember]
        public string FilePath {
            get {
                return filePath;
            }
            set {
                filePath = value;
                RaisePropertyChanged();
            }
        }

        private string filePattern = "$$IMAGETYPE$$\\$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$";

        [DataMember]
        public string FilePattern {
            get {
                return filePattern;
            }
            set {
                filePattern = value;
                RaisePropertyChanged();
            }
        }

        private FileTypeEnum fileType = FileTypeEnum.FITS;

        [DataMember]
        public FileTypeEnum FileType {
            get {
                return fileType;
            }
            set {
                fileType = value;
                RaisePropertyChanged();
            }
        }
    }
}