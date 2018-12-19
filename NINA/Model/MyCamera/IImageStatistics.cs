using System.Collections.Generic;
using System.Threading.Tasks;
using OxyPlot;

namespace NINA.Model.MyCamera {

    public interface IImageStatistics {
        int BitDepth { get; }
        int DetectedStars { get; set; }
        double ExposureTime { get; set; }
        int Height { get; }
        double HFR { get; set; }
        List<DataPoint> Histogram { get; }
        int Id { get; set; }
        bool IsBayered { get; }
        int Max { get; }
        long MaxOccurrences { get; }
        double Mean { get; }
        int Min { get; }
        long MinOccurrences { get; }
        double StDev { get; }
        int Width { get; }

        Task Calculate(ushort[] array);
    }
}