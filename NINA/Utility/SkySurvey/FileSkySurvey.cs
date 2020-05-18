#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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

                var targetName = string.IsNullOrWhiteSpace(arr.MetaData.Target?.Name) ? Path.GetFileNameWithoutExtension(dialog.FileName) : arr.MetaData.Target.Name;

                if (arr.MetaData.WorldCoordinateSystem != null) {
                    return new FileSkySurveyImage() {
                        Name = targetName,
                        Coordinates = arr.MetaData.WorldCoordinateSystem.GetCoordinates(renderedImage.Image.PixelWidth / 2, renderedImage.Image.PixelHeight / 2),
                        FoVHeight = Astrometry.Astrometry.ArcsecToArcmin(arr.MetaData.WorldCoordinateSystem.PixelScaleY * renderedImage.Image.PixelHeight),
                        FoVWidth = Astrometry.Astrometry.ArcsecToArcmin(arr.MetaData.WorldCoordinateSystem.PixelScaleX * renderedImage.Image.PixelWidth),
                        Image = renderedImage.Image,
                        Rotation = arr.MetaData.WorldCoordinateSystem.Rotation,
                        Source = nameof(FileSkySurvey),
                        Data = arr
                    };
                } else {
                    var pixelSize = arr.MetaData.Camera.PixelSize;
                    var focalLength = arr.MetaData.Telescope.FocalLength;
                    var arcSecPerPixel = Astrometry.Astrometry.ArcsecPerPixel(pixelSize, focalLength);

                    var referenceCoordinates = arr.MetaData.Telescope.Coordinates;
                    if (referenceCoordinates == null) {
                        referenceCoordinates = arr.MetaData.Target.Coordinates;
                    }

                    return new FileSkySurveyImage() {
                        Name = targetName,
                        Coordinates = referenceCoordinates,
                        FoVHeight = arcSecPerPixel * arr.Properties.Height,
                        FoVWidth = arcSecPerPixel * arr.Properties.Width,
                        Image = renderedImage.Image,
                        Rotation = double.NaN,
                        Source = nameof(FileSkySurvey),
                        Data = arr
                    };
                }
            } else {
                return null;
            }
        }
    }

    internal class FileSkySurveyImage : SkySurveyImage {
        public IImageData Data { get; set; }
    }
}
