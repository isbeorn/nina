using NINA.Model.ImageData;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    public interface IExposureData {
        int BitDepth { get; }
        ImageMetaData MetaData { get; }

        Task<IImageData> ToImageData(CancellationToken cancelToken = default);
    }
}