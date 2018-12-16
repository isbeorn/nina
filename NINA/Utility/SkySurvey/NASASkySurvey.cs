#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

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