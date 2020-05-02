#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Utility.Astrometry;
using NINA.Utility.ImageAnalysis;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    internal class FileSkySurvey : ISkySurvey {

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblLoadImage"];
            dialog.FileName = "";
            dialog.DefaultExt = ".tif";
            dialog.Multiselect = false;
            dialog.Filter = "Image files|*.tif;*.tiff;*.jpeg;*.jpg;*.png;*.cr2;*.nef;*.fit;*.fits;*.xisf|TIFF files|*.tif;*.tiff;|JPEG files|*.jpeg;*.jpg|PNG Files|*.png|RAW Files|*.cr2;*.nef|XISF Files|*.xisf|FITS Files|*.fit;*.fits";

            if (dialog.ShowDialog() == true) {
                var arr = await ImageData.FromFile(dialog.FileName, 16, false, Enum.RawConverterEnum.FREEIMAGE, ct);
                var renderedImage = arr.RenderImage();
                renderedImage = await renderedImage.Stretch(factor: 0.2, blackClipping: -2.8, unlinked: false);

                var pixelSize = arr.MetaData.Camera.PixelSize;
                var focalLength = arr.MetaData.Telescope.FocalLength;
                var arcSecPerPixel = Astrometry.Astrometry.ArcsecPerPixel(pixelSize, focalLength);

                var referenceCoordinates = arr.MetaData.Telescope.Coordinates;
                if (referenceCoordinates == null) {
                    referenceCoordinates = arr.MetaData.Target.Coordinates;
                }

                // TODO: Try and extract properties from image if available
                return new SkySurveyImage() {
                    Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                    Coordinates = referenceCoordinates,
                    FoVHeight = arcSecPerPixel * arr.Properties.Height,
                    FoVWidth = arcSecPerPixel * arr.Properties.Width,
                    Image = renderedImage.Image,
                    Rotation = double.NaN,
                    Source = nameof(FileSkySurvey)
                };
            } else {
                return null;
            }
        }
    }
}