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

        Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress);
    }

    internal class SkySurveyImage {
        public Guid Id { get; set; } = Guid.NewGuid();
        public BitmapSource Image { get; set; }
        public double FoVWidth { get; set; }
        public double FoVHeight { get; set; }
        public double Rotation { get; set; }
        public Coordinates Coordinates { get; set; }
        public string Name { get; internal set; }
    }
}