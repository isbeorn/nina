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

namespace NINA.Utility.SkySurvey {

    internal class StsciSkySurvey : MosaicSkySurvey, ISkySurvey {
        private const string Url = "https://archive.stsci.edu/cgi-bin/dss_search?format=GIF&r={0}&d={1}&e=J2000&w={2}&h={3}&v=1";

        private string GetUrl(Coordinates coordinates, double fovW, double fovH) {
            return string.Format(Url, coordinates.RADegrees, coordinates.Dec, fovW, fovH);
        }

        protected override async Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct) {
            var url = GetUrl(coordinates, fovW, fovH);
            return await Utility.HttpGetImage(ct, url);
        }
    }
}