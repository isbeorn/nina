using NINA.Utility.Enum;
using NINA.Utility.Mediator;
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