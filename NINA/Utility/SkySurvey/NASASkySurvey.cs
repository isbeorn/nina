using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class NASASkySurvey : ISkySurvey {
        private const string Url = "https://skyview.gsfc.nasa.gov/current/cgi/runquery.pl?Survey=dss2r&Position={0},{1}&Size={2}&Pixels={3}&Return=JPG";

        public async Task<SkySurveyImage> GetImage(Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 0.5;
            var pixels = Math.Min(Astrometry.Astrometry.ArcminToArcsec(fieldOfView) * arcSecPerPixel, 5000);
            var url = string.Format(Url, coordinates.RADegrees, coordinates.Dec, Astrometry.Astrometry.ArcminToDegree(fieldOfView), pixels);
            var image = await Utility.HttpClientGetImage(new Uri(url), ct, progress);
            return new SkySurveyImage() {
                Image = image,
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 180
            };
        }
    }
}