using NINA.Utility.Enum;

namespace NINA.Utility.Profile {
    public interface IImageFileSettings {
        string FilePath { get; set; }
        string FilePattern { get; set; }
        FileTypeEnum FileType { get; set; }
    }
}