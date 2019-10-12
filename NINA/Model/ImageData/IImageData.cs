using NINA.Utility.Enum;
using NINA.Utility.ImageAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public interface IImageData {
        IImageArray Data { get; }
        LRGBArrays DebayeredData { get; }
        BitmapSource Image { get; }
        IImageStatistics Statistics { get; set; }
        ImageMetaData MetaData { get; set; }

        Task CalculateStatistics();

        void Debayer(bool saveColorChannels = false, bool saveLumChannel = false);

        Task DetectStars(bool annotate, StarSensitivityEnum sensitivity, NoiseReductionEnum noiseReduction, CancellationToken ct = default, IProgress<ApplicationStatus> progress = null);

        void RenderImage();

        Task Stretch(double factor, double blackClipping, bool unlinked);

        Task<string> SaveToDisk(string path, string pattern, FileTypeEnum fileType, CancellationToken token = default);

        Task<string> PrepareSave(string path, FileTypeEnum fileType, CancellationToken token = default);

        string FinalizeSave(string file, string pattern);
    }
}