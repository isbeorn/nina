#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using System.Windows.Media.Imaging;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class ESOSkySurvey : MosaicSkySurvey, ISkySurvey {

        public ESOSkySurvey() {
            MaxFoVPerImage = 120;
        }

        private const string Url = "http://archive.eso.org/dss/dss/image?ra={0}&dec={1}&x={2}&y={3}&mime-type=download-gif&Sky-Survey=DSS2&equinox=J2000&statsmode=VO";

        protected override Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct) {
            var request = new Http.HttpDownloadImageRequest(
                Url,
                coordinates.RADegrees,
                coordinates.Dec,
                fovW,
                fovH
            );
            return request.Request(ct);
        }
    }
}