using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    internal interface ISkySurvey {

        Task<SkySurveyImage> GetImage(Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress);
    }

    internal class SkySurveyImage {
        public BitmapSource Image { get; set; }
        public double FoVWidth { get; set; }
        public double FoVHeight { get; set; }
        public double Rotation { get; set; }
    }
}