using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class ESOSkySurvey : MosaicSkySurvey, ISkySurvey {

        public ESOSkySurvey() {
            MaxFoVPerImage = 120;
        }

        private const string Url = "http://archive.eso.org/dss/dss/image?ra={0}&dec={1}&x={2}&y={3}&mime-type=download-gif&Sky-Survey=DSS2&equinox=J2000&statsmode=VO";

        protected override Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct) {
            var url = string.Format(Url, coordinates.RADegrees, coordinates.Dec, fovW, fovH);
            return Utility.HttpClientGetImage(new Uri(url), ct);
        }
    }
}