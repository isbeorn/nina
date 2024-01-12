#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging.Filters;
using NINA.Core.Utility;
using System;
using System.Drawing;

namespace NINA.Image.ImageAnalysis {

    public static class DetectionUtility {

        public static Rectangle GetCropRectangle(Bitmap image, double cropRatio) {
            int xcoord = (int)Math.Floor((image.Width - image.Width * cropRatio) / 2d);
            int ycoord = (int)Math.Floor((image.Height - image.Height * cropRatio) / 2d);
            int width = (int)Math.Floor(image.Width * cropRatio);
            int height = (int)Math.Floor(image.Height * cropRatio);
            return new Rectangle(xcoord, ycoord, width, height);
        }

        public static double LaplacianOfGaussianFunction(double x, double y, double sigma) {
            double result = -1 * 1 / (Math.PI * Math.Pow(sigma, 4)) * (1 - ((x * x + y * y) / (2 * sigma * sigma))) * Math.Exp(-1 * (x * x + y * y) / (2 * sigma * sigma)) * 600;
            return result;
        }

        public static int[,] LaplacianOfGaussianKernel(int size, double sigma) {
            int[,] LoGKernel = new int[size, size];
            int halfsize = size / 2;
            int sumKernel = 0;
            for (int x = -halfsize; x < halfsize + 1; x++) {
                for (int y = -halfsize; y < halfsize + 1; y++) {
                    int value = (int)Math.Round(LaplacianOfGaussianFunction(x, y, sigma));
                    LoGKernel[x + halfsize, y + halfsize] = value;
                    sumKernel += value;
                }
            }
            LoGKernel[halfsize, halfsize] = LoGKernel[halfsize, halfsize] - sumKernel;
            return LoGKernel;
        }

        public static Bitmap ResizeForDetection(Bitmap image, int maxWidth, double resizeFactor) {
            using (MyStopWatch.Measure()) {
                if (image.Width > maxWidth) {
                    var bmp = new ResizeBicubic((int)Math.Floor(image.Width * resizeFactor), (int)Math.Floor(image.Height * resizeFactor)).Apply(image);
                    image.Dispose();
                    return bmp;
                }
                return image;
            }
        }

        public static bool InROI(Size imageSize, Rectangle blob, double outerCropRatio = 1.0, double innerCropRatio = 1.0) {
            Rectangle outsideCropRect;

            // NOTE: OuterCrop internally set to 0 if we took a subframe and want a donut shape
            if (innerCropRatio >= 1.0 || outerCropRatio <= 0.0) {
                outsideCropRect = new Rectangle(0, 0, imageSize.Width, imageSize.Height);
            } else {
                // if only inner crop is set, then it is the outer boundary. Otherwise we use the outer crop
                var outsideCropRatio = outerCropRatio >= 1.0 ? innerCropRatio : outerCropRatio;
                var startFactor = (1.0 - outsideCropRatio) / 2.0;
                outsideCropRect = new Rectangle(
                    (int)Math.Floor(imageSize.Width * startFactor),
                    (int)Math.Floor(imageSize.Height * startFactor),
                    (int)(imageSize.Width * outsideCropRatio),
                    (int)(imageSize.Height * outsideCropRatio));
            }

            Rectangle insideCropRect;
            if (outerCropRatio >= 1.0) {
                // This rectangle is used to indicate no inside cropping should be done
                insideCropRect = new Rectangle(imageSize.Width / 2, imageSize.Height / 2, 0, 0);
            } else {
                var startFactor = (1.0 - innerCropRatio) / 2.0;
                insideCropRect = new Rectangle(
                    (int)Math.Floor(imageSize.Width * startFactor),
                    (int)Math.Floor(imageSize.Height * startFactor),
                    (int)(imageSize.Width * innerCropRatio),
                    (int)(imageSize.Height * innerCropRatio));
            }
            return blob.FullyInsideRect(outsideCropRect) && !blob.FullyInsideRect(insideCropRect);
        }

        public static bool FullyInsideRect(this Rectangle lhs, Rectangle rhs) {
            // Top left of first rectangle starts outside of the other rectangle
            var rhsRightX = rhs.X + rhs.Width;
            var rhsBottomY = rhs.Y + rhs.Height;
            if (lhs.X < rhs.X || lhs.Y < rhs.Y || lhs.X >= rhsRightX || lhs.Y >= rhsBottomY) {
                return false;
            }

            // Now we know the top left corner is within the rectangle, so all we need to do is test the bottom-right corner
            var lhsRightX = lhs.X + lhs.Width;
            var lhsBottomY = lhs.Y + lhs.Height;
            return lhsRightX <= rhsRightX && lhsBottomY <= rhsBottomY;
        }
    }
}