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

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 0.5;
            fieldOfView = Math.Round(fieldOfView, 2);
            var pixels = Math.Min(Astrometry.Astrometry.ArcminToArcsec(fieldOfView) * arcSecPerPixel, 5000);

            var request = new Http.HttpDownloadImageRequest(
               Url,
               coordinates.RADegrees,
               coordinates.Dec,
               Astrometry.Astrometry.ArcminToDegree(fieldOfView),
               pixels
           );
            var image = await request.Request(ct, progress);
            image.Freeze();

            return new SkySurveyImage() {
                Image = image,
                Name = name,
                Source = nameof(NASASkySurvey),
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
    }
}