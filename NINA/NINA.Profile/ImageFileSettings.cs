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
using System;
using System.IO;
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
            filePath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A");
            filePattern = "$$DATEMINUS12$$\\$$IMAGETYPE$$\\$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$";
            filePatternDARK = "";
            filePatternBIAS = "";
            filePatternFLAT = "";
            fileType = FileTypeEnum.FITS;
            tiffCompressionType = TIFFCompressionTypeEnum.NONE;
            xisfCompressionType = XISFCompressionTypeEnum.NONE;
            xisfChecksumType = XISFChecksumTypeEnum.SHA256;
            xisfByteShuffling = false;
            fitsCompressionType = FITSCompressionTypeEnum.NONE;
            fitsAddFzExtension = true;
            fitsUseLegacyWriter = true;
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

        private string filePatternDARK;

        [DataMember]
        public string FilePatternDARK {
            get => filePatternDARK;
            set {
                if (filePatternDARK != value) {
                    filePatternDARK = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string filePatternFLAT;

        [DataMember]
        public string FilePatternFLAT {
            get => filePatternFLAT;
            set {
                if (filePatternFLAT != value) {
                    filePatternFLAT = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string filePatternBIAS;

        [DataMember]
        public string FilePatternBIAS {
            get => filePatternBIAS;
            set {
                if (filePatternBIAS != value) {
                    filePatternBIAS = value;
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

        private FITSCompressionTypeEnum fitsCompressionType;

        [DataMember]
        public FITSCompressionTypeEnum FITSCompressionType {
            get => fitsCompressionType;
            set {
                if (fitsCompressionType != value) {
                    fitsCompressionType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool fitsAddFzExtension;

        [DataMember]
        public bool FITSAddFzExtension {
            get => fitsAddFzExtension;
            set {
                if (fitsAddFzExtension != value) {
                    fitsAddFzExtension = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool fitsUseLegacyWriter;
        [DataMember]
        public bool FITSUseLegacyWriter {
            get => fitsUseLegacyWriter;
            set {
                if(fitsUseLegacyWriter != value) {
                    fitsUseLegacyWriter = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string GetFilePattern(string imageType) {
            var pattern = FilePattern;

            if(imageType == "DARK" && !string.IsNullOrWhiteSpace(FilePatternDARK)) {
                pattern = FilePatternDARK;
            }
            if (imageType == "FLAT" && !string.IsNullOrWhiteSpace(FilePatternFLAT)) {
                pattern = FilePatternFLAT;
            }
            if (imageType == "BIAS" && !string.IsNullOrWhiteSpace(FilePatternBIAS)) {
                pattern = FilePatternBIAS;
            }

            return pattern;
        }
    }
}