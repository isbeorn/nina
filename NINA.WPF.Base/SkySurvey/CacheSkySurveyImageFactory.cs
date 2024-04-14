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
using System.Collections.Generic;
using System.Linq;
using NINA.Core.Utility;
using System.IO;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using NINA.Image.ImageAnalysis;
using System.Windows.Media;

namespace NINA.WPF.Base.SkySurvey {
    public class CacheSkySurveyImageFactory {
        public CacheSkySurveyImageFactory(int width, int height, CacheSkySurvey cache) {
            this.width = width;
            this.height = height;
            this.cache = cache;
        }

        public ViewportFoV ViewportFoV { get; private set; }

        private readonly int width;
        private readonly int height;
        private CacheSkySurvey cache;

        private object lockObj = new object();

        public BitmapSource Render(Coordinates centerCoordinates, double vFoVDegrees, double imageRotation) {
            lock (lockObj) {
                ViewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, width, height, imageRotation);

                using var dsoImageBuffer = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var dsoImageGraphics = Graphics.FromImage(dsoImageBuffer);
                dsoImageGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                dsoImageGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                dsoImageGraphics.Clear(System.Drawing.Color.Transparent);
                DrawBufferedDSOImages(dsoImageGraphics);

                var source = ImageUtility.ConvertBitmap(dsoImageBuffer, PixelFormats.Bgra32);
                source.Freeze();
                return source;
            }
        }

        private void DrawBufferedDSOImages(Graphics dsoImageGraphics) {
            try {
                var relevantImages = GetCacheImagesForViewport();
                foreach (var cacheImage in relevantImages) {
                    if (File.Exists(cacheImage.ImagePath)) {
                        var image = cacheImage.GetImageForScale(ViewportFoV.HFoV, 400);
                        var sourceR = new RectangleF(0, 0, image.Width, image.Height);

                        var imageResW = AstroUtil.ArcminToArcsec(cacheImage.FoVW) / image.Width;
                        var imageResH = AstroUtil.ArcminToArcsec(cacheImage.FoVH) / image.Height;
                        var conversionW = imageResW / ViewportFoV.ArcSecWidth;
                        var conversionH = imageResH / ViewportFoV.ArcSecHeight;
                        var dest = new RectangleF(-(float)(image.Width * conversionW / 2f), -(float)(image.Height * conversionH / 2f), (float)(image.Width * conversionW), (float)(image.Height * conversionH));

                        var center = cacheImage.Coordinates.XYProjection(ViewportFoV);

                        var panelDeltaX = center.X - ViewportFoV.ViewPortCenterPoint.X;
                        var panelDeltaY = center.Y - ViewportFoV.ViewPortCenterPoint.Y;
                        var referenceCenter = ViewportFoV.CenterCoordinates.Shift(panelDeltaX < 1E-10 ? 1 : 0, panelDeltaY, ViewportFoV.Rotation, ViewportFoV.ArcSecWidth, ViewportFoV.ArcSecHeight);

                        var rotation = -(90 - ((float)AstroUtil.CalculatePositionAngle(referenceCenter.RADegrees, cacheImage.Coordinates.RADegrees, referenceCenter.Dec, cacheImage.Coordinates.Dec)));
                        if (panelDeltaX < 0) {
                            rotation += 180;
                        }
                        if (cacheImage.Coordinates.Dec < 0 || (referenceCenter.Dec < 0 && cacheImage.Coordinates.Dec >= 0)) {
                            rotation += 180;
                        }

                        rotation += (float)cacheImage.Rotation;

                        dsoImageGraphics.TranslateTransform((float)center.X, (float)center.Y);
                        dsoImageGraphics.RotateTransform(rotation);
                        dsoImageGraphics.DrawImage(image, dest, sourceR, GraphicsUnit.Pixel);
                        dsoImageGraphics.ResetTransform();
                    }
                }
            } catch (Exception) {
            } finally {
            }
        }

        private List<CacheImage> GetCacheImagesForViewport() {
            using (MyStopWatch.Measure()) {
                double minSize = 6;
                double maxSize = 600;

                var l = new List<CacheImage>();
                foreach (var entry in cache.Cache.Elements("Image")) {
                    double fovW = double.Parse(entry.Attribute("FoVW").Value, CultureInfo.InvariantCulture);
                    double fovH = double.Parse(entry.Attribute("FoVH").Value, CultureInfo.InvariantCulture);

                    if (fovW < minSize || fovW > maxSize) {
                        continue;
                    }

                    double ra = double.Parse(entry.Attribute("RA").Value, CultureInfo.InvariantCulture);
                    double dec = double.Parse(entry.Attribute("Dec").Value, CultureInfo.InvariantCulture);
                    double rotation = double.Parse(entry.Attribute("Rotation").Value, CultureInfo.InvariantCulture);
                    string path = Path.Combine(cache.framingAssistantCachePath, entry.Attribute("FileName").Value);

                    if (AstroUtil.ArcminToArcsec(fovW) > minSize && AstroUtil.ArcminToArcsec(fovH) > minSize) {
                        var image = new CacheImage(ra, dec, fovW, fovH, rotation, path);
                        l.Add(image);
                    }
                }

                l = l.Where(x => {
                        var distance = x.Coordinates - ViewportFoV.CenterCoordinates;
                        return distance.Distance.Degree < Math.Max(ViewportFoV.HFoV, ViewportFoV.VFoV) + AstroUtil.ArcminToDegree(Math.Max(x.FoVH, x.FoVW));
                    })
                    //Order in descending order so that smallest field of view is drawn on top, as it most likely contains most details
                    .OrderByDescending(x => x.FoVW)
                    .ToList();

                return l;
            }

        }
    }
}