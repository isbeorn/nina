#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Astrometry;
using System;
using System.Drawing;
using System.IO;

namespace NINA.WPF.Base.SkySurvey {

    public class CacheImage : IDisposable {

        public CacheImage(double ra, double dec, double fovW, double fovH, double rotation, string path) {
            if (Math.Round(dec, 4) == 0) { dec = 0; }
            this.Coordinates = new Coordinates(Angle.ByHours(ra), Angle.ByDegree(dec), Epoch.J2000);
            this.FoVW = fovW;
            this.FoVH = fovH;
            this.Rotation = rotation;
            this.ImagePath = path;
        }

        public Coordinates Coordinates { get; }
        public double FoVW { get; }
        public double FoVH { get; }
        public double Rotation { get; }
        public string ImagePath { get; }

        public const int SmallThumbnailSize = 75;
        public const int MediumThumbnailSize = 150;
        public const int BigThumbnailSize = 500;

        public static string GetImagePathForThumbnail(string imagePath, int size) {
            return Path.Combine(Path.GetDirectoryName(imagePath), $"{Path.GetFileNameWithoutExtension(imagePath)}_{size}px{Path.GetExtension(imagePath)}");
        }

        public Bitmap GetImageForScale(double totalFieldOfViewDeg, double totalWidth) {
            var estimatedPlatePixelWidth = (AstroUtil.ArcminToDegree(FoVW) / totalFieldOfViewDeg) * totalWidth;
            if (estimatedPlatePixelWidth <= 150) {
                if (cachedImage == null || previousPixelWidth > SmallThumbnailSize * 2) {
                    if (cachedImage != null) { cachedImage.Dispose(); }
                    var file = GetImagePathForThumbnail(ImagePath, SmallThumbnailSize);
                    if (!File.Exists(file)) {
                        var tmp = BitmapWithoutLock(ImagePath);
                        cachedImage = new Bitmap(tmp, SmallThumbnailSize, SmallThumbnailSize);
                        cachedImage.Save(file);
                        tmp.Dispose();
                    } else {
                        cachedImage = BitmapWithoutLock(file);
                    }
                }
            } else if (estimatedPlatePixelWidth <= (MediumThumbnailSize * 2)) {
                if (cachedImage == null || previousPixelWidth > (MediumThumbnailSize * 2) || previousPixelWidth <= (SmallThumbnailSize * 2)) {
                    if (cachedImage != null) { cachedImage.Dispose(); }
                    var file = GetImagePathForThumbnail(ImagePath, MediumThumbnailSize);
                    if (!File.Exists(file)) {
                        var tmp = BitmapWithoutLock(ImagePath);
                        cachedImage = new Bitmap(tmp, MediumThumbnailSize, MediumThumbnailSize);
                        cachedImage.Save(file);
                        tmp.Dispose();
                    } else {
                        cachedImage = BitmapWithoutLock(file);
                    }
                }
            } else if (estimatedPlatePixelWidth <= (BigThumbnailSize * 2)) {
                if (cachedImage == null || previousPixelWidth > (BigThumbnailSize * 2) || previousPixelWidth <= (MediumThumbnailSize * 2)) {
                    if (cachedImage != null) { cachedImage.Dispose(); }
                    var file = GetImagePathForThumbnail(ImagePath, BigThumbnailSize);
                    if (!File.Exists(file)) {
                        var tmp = BitmapWithoutLock(ImagePath);
                        cachedImage = new Bitmap(tmp, BigThumbnailSize, BigThumbnailSize);
                        cachedImage.Save(file);
                        tmp.Dispose();
                    } else {
                        cachedImage = BitmapWithoutLock(file);
                    }
                }
            } else {
                if (cachedImage == null || previousPixelWidth <= (BigThumbnailSize * 2)) {
                    if (cachedImage != null) { cachedImage.Dispose(); }
                    cachedImage = BitmapWithoutLock(ImagePath);
                }
            }

            previousPixelWidth = cachedImage.Width;
            return cachedImage;
        }

        private Bitmap BitmapWithoutLock(string path) {
            Bitmap img;
            using (var bmpTemp = new Bitmap(path)) {
                img = new Bitmap(bmpTemp);
            }
            return img;
        }

        private Bitmap cachedImage;
        private double previousPixelWidth;

        private bool isDisposed;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing) {
            if (isDisposed) return;

            if (disposing) {
                if (cachedImage != null) {
                    cachedImage.Dispose();
                }
            }
            isDisposed = true;
        }
    }
}