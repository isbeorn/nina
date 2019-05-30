using NINA.Utility.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public interface IImageData {
        IImageArray Data { get; }
        BitmapSource Image { get; }
        IImageStatistics Statistics { get; set; }
        ImageMetaData MetaData { get; set; }

        Task CalculateStatistics();

        void Debayer();

        Task DetectStars(bool annotate, CancellationToken ct = default, IProgress<ApplicationStatus> progress = null);

        void RenderImage();

        Task Stretch(double factor, double blackClipping, bool unlinked);

        Task<string> SaveToDisk(string path, string pattern, FileTypeEnum fileType, CancellationToken token = default);
    }
}