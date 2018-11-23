using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NINA.Utility.Astrometry;
using NINA.Utility.Http;

namespace NINA.Utility.SkySurvey {

    internal class StsciSkySurvey : MosaicSkySurvey, ISkySurvey {
        private const string Url = "https://archive.stsci.edu/cgi-bin/dss_search?format=GIF&r={0}&d={1}&e=J2000&w={2}&h={3}&v=1";

        protected override Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct) {
            var request = new HttpDownloadImageRequest(Url, coordinates.RADegrees, coordinates.Dec, fovW, fovH);
            return request.Request(ct);
        }
    }
}