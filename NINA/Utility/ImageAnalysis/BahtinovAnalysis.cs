#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;

namespace NINA.Utility.ImageAnalysis {

    internal class BahtinovAnalysis {

        public BahtinovAnalysis(BitmapSource source, System.Windows.Media.Color backgroundColor) {
            originalSource = source;
            this.backgroundColor = backgroundColor;
        }

        private System.Windows.Media.Color backgroundColor;

        private BitmapSource originalSource;

        public BahtinovImage GrabBahtinov() {
            var bahtinovImage = new BahtinovImage();
            Bitmap convertedSource;

            if (originalSource.Format != System.Windows.Media.PixelFormats.Gray8) {
                if (originalSource.Format != System.Windows.Media.PixelFormats.Gray16) {
                    using (var imgToConvert = ImageUtility.BitmapFromSource(originalSource, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                        convertedSource = new Grayscale(0.2125, 0.7154, 0.0721).Apply(imgToConvert);
                    }
                    convertedSource = ImageUtility.Convert16BppTo8Bpp(ImageUtility.ConvertBitmap(convertedSource, System.Windows.Media.PixelFormats.Gray16));
                } else {
                    convertedSource = ImageUtility.Convert16BppTo8Bpp(originalSource);
                }
            } else {
                convertedSource = ImageUtility.BitmapFromSource(originalSource, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                convertedSource.Palette = ImageUtility.GetGrayScalePalette();
            }

            using (var focusEllipsePen = new System.Drawing.Pen(System.Drawing.Brushes.Green, 1)) {
                using (var intersectEllipsePen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 1)) {
                    var mediaColor = backgroundColor;
                    var drawingColor = System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
                    using (var linePen = new System.Drawing.Pen(drawingColor, 1)) {
                        using (var bahtinovedBitmap = new Bitmap(convertedSource.Width, convertedSource.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)) {
                            Graphics graphics = Graphics.FromImage(bahtinovedBitmap);
                            graphics.DrawImage(convertedSource, 0, 0);

                            /* Apply filters and detection*/
                            CannyEdgeDetector filter = new CannyEdgeDetector();
                            filter.GaussianSize = 10;
                            filter.ApplyInPlace(convertedSource);

                            HoughLineTransformation lineTransform = new HoughLineTransformation();
                            lineTransform.ProcessImage(convertedSource);

                            HoughLine[] lines = lineTransform.GetMostIntensiveLines(6);

                            List<Line> bahtinovLines = new List<Line>();
                            foreach (HoughLine line in lines) {
                                var k = TranslateHughLineToLine(line, bahtinovedBitmap.Width, bahtinovedBitmap.Height);
                                bahtinovLines.Add(k);
                            }

                            float x1, x2, y1, y2;

                            if (bahtinovLines.Count == 6) {
                                var orderedPoints = bahtinovLines.OrderBy(x => 1.0d / x.Slope).ToList();
                                var threeLines = new List<Line>();

                                for (var i = 0; i < orderedPoints.Count(); i += 2) {
                                    var l1 = orderedPoints[i];
                                    var l2 = orderedPoints[i + 1];

                                    var inter = (l1.Intercept + l2.Intercept) / 2.0f;
                                    var slope = (l1.Slope + l2.Slope) / 2.0f;
                                    var centerLine = Line.FromSlopeIntercept(slope, inter);
                                    threeLines.Add(centerLine);

                                    x1 = 0;
                                    x2 = convertedSource.Width;
                                    y1 = double.IsInfinity(centerLine.Slope) ? centerLine.Intercept : centerLine.Slope + centerLine.Intercept;
                                    y2 = double.IsInfinity(centerLine.Slope) ? centerLine.Intercept : (centerLine.Slope * (convertedSource.Width) + centerLine.Intercept);

                                    graphics.DrawLine(
                                        linePen,
                                        new PointF(x1, y1),
                                        new PointF(x2, y2));
                                }

                                /* Intersect outer bahtinov lines */
                                var intersection = threeLines[0].GetIntersectionWith(threeLines[2]);
                                if (intersection.HasValue) {
                                    /* get orthogonale to center line through intersection */
                                    var centerBahtinovLine = threeLines[1];
                                    var orthogonalSlope = -1.0f / centerBahtinovLine.Slope;
                                    var orthogonalIntercept = intersection.Value.Y - orthogonalSlope * intersection.Value.X;

                                    var orthogonalCenter = Line.FromSlopeIntercept(orthogonalSlope, orthogonalIntercept);
                                    var intersection2 = centerBahtinovLine.GetIntersectionWith(orthogonalCenter);
                                    if (intersection2.HasValue && !double.IsInfinity(intersection2.Value.X)) {
                                        x1 = intersection.Value.X;
                                        y1 = intersection.Value.Y;
                                        x2 = intersection2.Value.X;
                                        y2 = intersection2.Value.Y;

                                        bahtinovImage.Distance = intersection.Value.DistanceTo(intersection2.Value);

                                        var t = bahtinovImage.Distance * 4 / bahtinovImage.Distance;
                                        var x3 = (float)((1 - t) * x1 + t * x2);
                                        var y3 = (float)((1 - t) * y1 + t * y2);

                                        var r = 10;
                                        graphics.DrawEllipse(
                                            intersectEllipsePen,
                                            new RectangleF(x3 - r, y3 - r, 2 * r, 2 * r));
                                        graphics.DrawEllipse(
                                            focusEllipsePen,
                                            new RectangleF(x2 - r, y2 - r, 2 * r, 2 * r));

                                        graphics.DrawLine(
                                            intersectEllipsePen,
                                            new PointF(x3, y3),
                                            new PointF(x2, y2));
                                    }
                                }
                            }

                            var img = ImageUtility.ConvertBitmap(bahtinovedBitmap, System.Windows.Media.PixelFormats.Bgr24);
                            convertedSource.Dispose();
                            img.Freeze();
                            bahtinovImage.Image = img;
                            return bahtinovImage;
                        }
                    }
                }
            }
        }

        private Line TranslateHughLineToLine(HoughLine line, int width, int height) {
            // get line's radius and theta values
            int r = line.Radius;
            double t = line.Theta;

            // check if line is in lower part of the image
            if (r < 0) {
                t += 180;
                r = -r;
            }

            // convert degrees to radians
            t = (t / 180) * Math.PI;

            // get image centers (all coordinate are measured relative to center)
            int w2 = width / 2;
            int h2 = height / 2;

            double x0 = 0, x1 = 0, y0 = 0, y1 = 0;

            if (line.Theta != 0) {
                // none-vertical line
                x0 = -w2; // most left point
                x1 = w2;  // most right point

                // calculate corresponding y values
                y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);
            } else {
                // vertical line
                x0 = line.Radius;
                x1 = line.Radius;

                y0 = h2;
                y1 = -h2;
            }

            return
                Line.FromPoints(
                    new IntPoint((int)x0 + w2, h2 - (int)y0),
                    new IntPoint((int)x1 + w2, h2 - (int)y1)
                );
        }
    }
}
