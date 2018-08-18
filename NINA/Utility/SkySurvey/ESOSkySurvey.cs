using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class ESOSkySurvey : ISkySurvey {
        private const string Url = "http://archive.eso.org/dss/dss/image?ra={0}&dec={1}&x={2}&y={3}&mime-type=download-gif&Sky-Survey=DSS2&equinox=J2000&statsmode=VO";

        public async Task<SkySurveyImage> GetImage(Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            var url = string.Format(Url, coordinates.RADegrees, coordinates.Dec, fieldOfView, fieldOfView);
            var image = await Utility.HttpClientGetImage(new Uri(url), ct, progress);
            image.Freeze();
            return new SkySurveyImage() {
                Image = image,
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 180,
                Coordinates = coordinates
            };
        }
    }
}