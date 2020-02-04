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
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
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
            tiffCompressionType = TIFFCompressionTypeEnum.NONE;
            xisfCompressionType = XISFCompressionTypeEnum.NONE;
            xisfChecksumType = XISFChecksumTypeEnum.NONE;
            xisfByteShuffling = false;
        }

        private string filePath;

        [DataMember]
        public string FilePath {
            get => filePath;
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
            get => filePattern;
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
                /*
                 * The TIFF_LZW and TIFF_ZIP file types are obsoleted and
                 * the compression options are specified separately now. This block
                 * will catch any old profiles that have old file types set and
                 * correct them to adhere to the new scheme.
                 */
#pragma warning disable CS0612 // Type or member is obsolete
                switch (fileType) {
                    case FileTypeEnum.TIFF_LZW:
                        FileType = FileTypeEnum.TIFF;
                        TIFFCompressionType = TIFFCompressionTypeEnum.LZW;
                        break;

                    case FileTypeEnum.TIFF_ZIP:
                        FileType = FileTypeEnum.TIFF;
                        TIFFCompressionType = TIFFCompressionTypeEnum.ZIP;
                        break;
                }
#pragma warning restore CS0612 // Type or member is obsolete

                return fileType;
            }
            set {
                if (fileType != value) {
                    fileType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private TIFFCompressionTypeEnum tiffCompressionType;

        [DataMember]
        public TIFFCompressionTypeEnum TIFFCompressionType {
            get => tiffCompressionType;
            set {
                if (tiffCompressionType != value) {
                    tiffCompressionType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private XISFCompressionTypeEnum xisfCompressionType;

        [DataMember]
        public XISFCompressionTypeEnum XISFCompressionType {
            get => xisfCompressionType;
            set {
                if (xisfCompressionType != value) {
                    xisfCompressionType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private XISFChecksumTypeEnum xisfChecksumType;

        [DataMember]
        public XISFChecksumTypeEnum XISFChecksumType {
            get => xisfChecksumType;
            set {
                if (xisfChecksumType != value) {
                    xisfChecksumType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool xisfByteShuffling;

        [DataMember]
        public bool XISFByteShuffling {
            get => xisfByteShuffling;
            set {
                if (xisfByteShuffling != value) {
                    xisfByteShuffling = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}