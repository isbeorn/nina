#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class ImageFileSettings : Settings, IImageFileSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            filePath = string.Empty;
            filePattern = "$$IMAGETYPE$$\\$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$";
            fileType = FileTypeEnum.FITS;
        }

        private string filePath;

        [DataMember]
        public string FilePath {
            get {
                return filePath;
            }
            set {
                if (filePath != value) {
                    filePath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string filePattern;

        [DataMember]
        public string FilePattern {
            get {
                return filePattern;
            }
            set {
                if (filePattern != value) {
                    filePattern = value;
                    RaisePropertyChanged();
                }
            }
        }

        private FileTypeEnum fileType;

        [DataMember]
        public FileTypeEnum FileType {
            get {
                return fileType;
            }
            set {
                if (fileType != value) {
                    fileType = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}