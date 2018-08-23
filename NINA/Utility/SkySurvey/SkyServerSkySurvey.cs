using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class SkyServerSkySurvey : ISkySurvey {
        private const string Url = "http://skyserver.sdss.org/dr12/SkyserverWS/ImgCutout/getjpeg?ra={0}&dec={1}&width={2}&height={3}&scale={4}";

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 0.4;
            var targetFoVInArcSec = Astrometry.Astrometry.ArcminToArcsec(fieldOfView);
            var pixels = Math.Min(targetFoVInArcSec / arcSecPerPixel, 2048);
            if (pixels == 2048) {
                arcSecPerPixel = targetFoVInArcSec / 2048;
            }

            var url = string.Format(
                Url,
                coordinates.RADegrees,
                coordinates.Dec,
                pixels,
                pixels,
                arcSecPerPixel
            );
            var image = await Utility.HttpClientGetImage(new Uri(url), ct, progress);
            image.Freeze();
            return new SkySurveyImage() {
                Name = nameof(SkyServerSkySurvey) + name,
                Image = image,
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 180,
                Coordinates = coordinates
            };
        }
    }
}