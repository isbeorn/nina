#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OpenCvSharp;
using System.Drawing;

namespace Accord.Imaging.Filters {
    /// <summary>
    /// Grayscale filter - converts color image to grayscale using weighted RGB coefficients
    /// </summary>
    public class Grayscale {
        private double redCoefficient;
        private double greenCoefficient;
        private double blueCoefficient;

        public Grayscale(double cr, double cg, double cb) {
            redCoefficient = cr;
            greenCoefficient = cg;
            blueCoefficient = cb;
        }

        public Bitmap Apply(Bitmap source) {
            Mat sourceMat = source;
            Mat result = new Mat();

            // If already grayscale, just clone
            if (sourceMat.Channels() == 1) {
                sourceMat.CopyTo(result);
                return result;
            }

            // Convert to grayscale using custom weights
            // OpenCV's cvtColor uses different default weights, so we need to do it manually
            Mat[] channels = Cv2.Split(sourceMat);

            // Create weighted sum: gray = cr*R + cg*G + cb*B
            // Note: OpenCV uses BGR order, so channels[0]=B, [1]=G, [2]=R
            result = new Mat();
            Cv2.AddWeighted(channels[2], redCoefficient, channels[1], greenCoefficient, 0.0, result);

            Mat temp = new Mat();
            Cv2.AddWeighted(result, 1.0, channels[0], blueCoefficient, 0.0, temp);

            // Convert to 8-bit if needed with proper scaling
            result = new Mat();
            if (temp.Depth() == MatType.CV_16U) {
                // 16-bit to 8-bit conversion with scaling
                temp.ConvertTo(result, MatType.CV_8UC1, 1.0 / 256.0);
            } else {
                // Already 8-bit or other depth
                temp.ConvertTo(result, MatType.CV_8UC1);
            }

            // Cleanup
            foreach (var ch in channels) ch.Dispose();
            temp.Dispose();

            return result;
        }

        public void ApplyInPlace(Bitmap image) {
            var result = Apply(image);
            Mat resultMat = result;
            Mat imageMat = image;
            resultMat.CopyTo(imageMat);
            result.Dispose();
        }
    }

    /// <summary>
    /// Crop filter - crops a rectangular region from an image using OpenCV
    /// </summary>
    public class Crop {
        private Rectangle cropRectangle;

        public Crop(Rectangle rect) {
            cropRectangle = rect;
        }

        public Bitmap Apply(Bitmap source) {
            Mat sourceMat = source;

            // Create OpenCV Rect from System.Drawing.Rectangle
            Rect cvRect = new Rect(cropRectangle.X, cropRectangle.Y, cropRectangle.Width, cropRectangle.Height);

            // Ensure the crop rectangle is within bounds
            cvRect = cvRect.Intersect(new Rect(0, 0, sourceMat.Width, sourceMat.Height));

            // Crop the Mat
            Mat croppedMat = new Mat(sourceMat, cvRect);

            return croppedMat;
        }
    }

    /// <summary>
    /// Median filter - applies median blur using OpenCV
    /// </summary>
    public class Median {
        public int KernelSize { get; set; } = 3;

        public Median() { }

        public Median(int kernelSize) {
            KernelSize = kernelSize | 1; // Ensure odd number
        }

        public void ApplyInPlace(Bitmap image) {
            Mat mat = image;
            Cv2.MedianBlur(mat, mat, KernelSize);
        }

        public Bitmap Apply(Bitmap source) {
            Mat sourceMat = source;
            Mat result = new Mat();
            Cv2.MedianBlur(sourceMat, result, KernelSize);
            return result;
        }
    }

    /// <summary>
    /// Convolution filter - applies custom convolution kernel using OpenCV
    /// </summary>
    public class Convolution {
        private Mat kernel;

        public Convolution(int[,] customKernel) {
            int rows = customKernel.GetLength(0);
            int cols = customKernel.GetLength(1);

            kernel = new Mat(rows, cols, MatType.CV_32F);

            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < cols; j++) {
                    kernel.Set(i, j, (float)customKernel[i, j]);
                }
            }
        }

        public void ApplyInPlace(Bitmap image) {
            Mat mat = image;
            Mat temp = new Mat();
            Cv2.Filter2D(mat, temp, mat.Depth(), kernel);
            temp.CopyTo(mat);
            temp.Dispose();
        }

        public Bitmap Apply(Bitmap source) {
            Mat sourceMat = source;
            Mat result = new Mat();
            Cv2.Filter2D(sourceMat, result, sourceMat.Depth(), kernel);
            return result;
        }
    }

    /// <summary>
    /// SIS Threshold - Simple Image Statistics threshold using OpenCV
    /// Adaptive thresholding algorithm
    /// </summary>
    public class SISThreshold {
        public void ApplyInPlace(Bitmap image) {
            Mat mat = image;

            // SIS threshold uses adaptive thresholding
            // Using Gaussian adaptive threshold which is similar to SIS
            Cv2.AdaptiveThreshold(mat, mat, 255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.Binary,
                11, 2);
        }

        public Bitmap Apply(Bitmap source) {
            Mat sourceMat = source;
            Mat result = new Mat();

            Cv2.AdaptiveThreshold(sourceMat, result, 255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.Binary,
                11, 2);

            return result;
        }
    }

    /// <summary>
    /// Binary Dilation 3x3 - morphological dilation with 3x3 kernel using OpenCV
    /// </summary>
    public class BinaryDilation3x3 {
        public void ApplyInPlace(Bitmap image) {
            Mat mat = image;
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            Cv2.Dilate(mat, mat, kernel);
            kernel.Dispose();
        }

        public Bitmap Apply(Bitmap source) {
            Mat sourceMat = source;
            Mat result = new Mat();
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            Cv2.Dilate(sourceMat, result, kernel);
            kernel.Dispose();
            return result;
        }
    }

    /// <summary>
    /// Fast Gaussian Blur - applies Gaussian blur using optimized OpenCV implementation
    /// </summary>
    public class FastGaussianBlur {
        private Bitmap sourceBitmap;

        public FastGaussianBlur(Bitmap source) {
            sourceBitmap = source;
        }

        public Bitmap Process(int sigma) {
            Mat sourceMat = sourceBitmap;
            Mat result = new Mat();

            // Calculate kernel size based on sigma
            int kernelSize = (sigma * 2) * 2 + 1;
            if (kernelSize < 3) kernelSize = 3;
            if (kernelSize % 2 == 0) kernelSize++; // Ensure odd

            Cv2.GaussianBlur(sourceMat, result, new OpenCvSharp.Size(kernelSize, kernelSize), sigma);
            return result;
        }
    }

    /// <summary>
    /// Base class for color remapping filters
    /// </summary>
    public abstract class ColorRemapping {
        /// <summary>
        /// Format translations dictionary
        /// </summary>
        protected System.Collections.Generic.Dictionary<System.Drawing.Imaging.PixelFormat, System.Drawing.Imaging.PixelFormat> FormatTranslations { get; set; }

        protected ColorRemapping() {
            FormatTranslations = new System.Collections.Generic.Dictionary<System.Drawing.Imaging.PixelFormat, System.Drawing.Imaging.PixelFormat>();
        }

        /// <summary>
        /// Process the filter on the specified image
        /// </summary>
        protected abstract void ProcessFilter(UnmanagedImage image, Rectangle rect);

        /// <summary>
        /// Apply filter to an image
        /// </summary>
        public Bitmap Apply(Bitmap image) {
            // Create output bitmap
            var output = new Bitmap(image.Width, image.Height, image.PixelFormat);
            
            // Lock both bitmaps
            var sourceData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                image.PixelFormat);
            
            var destData = output.LockBits(
                new Rectangle(0, 0, output.Width, output.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                output.PixelFormat);

            try {
                // Copy source to destination first
                unsafe {
                    byte* src = (byte*)sourceData.Scan0;
                    byte* dst = (byte*)destData.Scan0;
                    int bytes = sourceData.Stride * sourceData.Height;
                    for (int i = 0; i < bytes; i++) {
                        dst[i] = src[i];
                    }
                }

                // Create unmanaged image wrapper for destination
                var unmanagedImage = new UnmanagedImage(destData);
                
                // Process the filter
                ProcessFilter(unmanagedImage, new Rectangle(0, 0, image.Width, image.Height));
            } finally {
                image.UnlockBits(sourceData);
                output.UnlockBits(destData);
            }

            return output;
        }

        /// <summary>
        /// Apply filter to an image in place (modifies the original image)
        /// </summary>
        public void ApplyInPlace(Bitmap image) {
            var bitmapData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                image.PixelFormat);

            try {
                // Create unmanaged image wrapper
                var unmanagedImage = new UnmanagedImage(bitmapData);
                
                // Process the filter
                ProcessFilter(unmanagedImage, new Rectangle(0, 0, image.Width, image.Height));
            } finally {
                image.UnlockBits(bitmapData);
            }
        }
    }

    /// <summary>
    /// Base class for Bayer filter demosaicing
    /// </summary>
    public abstract class BayerFilter {
        /// <summary>
        /// Format translations dictionary
        /// </summary>
        protected System.Collections.Generic.Dictionary<System.Drawing.Imaging.PixelFormat, System.Drawing.Imaging.PixelFormat> FormatTranslations { get; set; }

        /// <summary>
        /// Bayer pattern matrix (2x2)
        /// </summary>
        public int[,] BayerPattern { get; set; }

        /// <summary>
        /// Whether to perform full demosaicing or simple extraction
        /// </summary>
        public bool PerformDemosaicing { get; set; } = true;

        protected BayerFilter() {
            FormatTranslations = new System.Collections.Generic.Dictionary<System.Drawing.Imaging.PixelFormat, System.Drawing.Imaging.PixelFormat>();
        }

        /// <summary>
        /// Process the filter on the specified images
        /// </summary>
        protected abstract unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData);

        /// <summary>
        /// Apply filter to create a new processed bitmap
        /// </summary>
        public virtual Bitmap Apply(Bitmap source) {
            // Get the target pixel format
            var sourceFormat = source.PixelFormat;
            var destFormat = FormatTranslations.ContainsKey(sourceFormat) 
                ? FormatTranslations[sourceFormat] 
                : sourceFormat;

            // Create destination bitmap
            var destination = new Bitmap(source.Width, source.Height, destFormat);

            // Lock both bitmaps
            var sourceData = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                sourceFormat);

            var destData = destination.LockBits(
                new Rectangle(0, 0, destination.Width, destination.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                destFormat);

            try {
                var unmanagedSource = new UnmanagedImage(sourceData);
                var unmanagedDest = new UnmanagedImage(destData);

                ProcessFilter(unmanagedSource, unmanagedDest);
            } finally {
                source.UnlockBits(sourceData);
                destination.UnlockBits(destData);
            }

            return destination;
        }
    }
}
