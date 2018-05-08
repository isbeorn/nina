using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [XmlRoot(nameof(Profile))]
    public class ImageFileSettings {
        private string filePath = string.Empty;

        [XmlElement(nameof(FilePath))]
        public string FilePath {
            get {
                return filePath;
            }
            set {
                filePath = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string filePattern = "$$IMAGETYPE$$\\$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$";

        [XmlElement(nameof(FilePattern))]
        public string FilePattern {
            get {
                return filePattern;
            }
            set {
                filePattern = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private FileTypeEnum fileType = FileTypeEnum.FITS;

        [XmlElement(nameof(FileType))]
        public FileTypeEnum FileType {
            get {
                return fileType;
            }
            set {
                fileType = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}