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
using System.Drawing.Imaging;

namespace System.Drawing {
    /// <summary>
    /// Base class for images
    /// </summary>
    public class Image : IDisposable {
        public virtual void Dispose() { }
        public int Width { get; set; }
        public int Height { get; set; }
        public ImageFormat RawFormat { get; set; }
    }

    /// <summary>
    /// Bitmap type that wraps OpenCV Mat
    /// </summary>
    public class Bitmap : Image {
        private Mat _mat;

        public Bitmap() {
            _mat = new Mat();
        }

        public Bitmap(int width, int height) {
            _mat = new Mat(height, width, MatType.CV_8UC3);
        }

        public Bitmap(int width, int height, PixelFormat format) {
            MatType matType;
            switch (format) {
                case PixelFormat.Format8bppIndexed:
                    matType = MatType.CV_8UC1;
                    break;
                case PixelFormat.Format16bppGrayScale:
                    matType = MatType.CV_16UC1;
                    break;
                case PixelFormat.Format24bppRgb:
                    matType = MatType.CV_8UC3;
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppRgb:
                    matType = MatType.CV_8UC4;
                    break;
                case PixelFormat.Format48bppRgb:
                    matType = MatType.CV_16UC3;
                    break;
                case PixelFormat.Format16bppRgb565:
                    matType = MatType.CV_16UC1; // Store as 16-bit single channel
                    break;
                default:
                    matType = MatType.CV_8UC3;
                    break;
            }
            _mat = new Mat(height, width, matType);
            // Initialize to zero
            _mat.SetTo(OpenCvSharp.Scalar.All(0));
        }

        public Bitmap(string filename) {
            _mat = Cv2.ImRead(filename, ImreadModes.Unchanged);
            if (_mat == null || _mat.Empty()) {
                throw new ArgumentException($"Failed to load image from: {filename}");
            }
        }

        public Bitmap(Mat mat) {
            _mat = mat;
        }

        public Bitmap(Bitmap source, int width, int height) {
            // Resize the source bitmap to the specified dimensions
            if (source == null || source._mat == null || source._mat.Empty()) {
                _mat = new Mat();
                return;
            }

            _mat = new Mat();
            Cv2.Resize(source._mat, _mat, new OpenCvSharp.Size(width, height));
        }

        public Bitmap Clone() {
            return new Bitmap(_mat?.Clone());
        }

        public int Width => _mat.Width;
        public int Height => _mat.Height;

        public PixelFormat PixelFormat {
            get {
                if (_mat == null || _mat.Empty()) return PixelFormat.Undefined;

                var matType = _mat.Type();
                if (matType == MatType.CV_8UC1) return PixelFormat.Format8bppIndexed;
                if (matType == MatType.CV_16UC1) return PixelFormat.Format16bppGrayScale;
                if (matType == MatType.CV_8UC3) return PixelFormat.Format24bppRgb;
                if (matType == MatType.CV_8UC4) return PixelFormat.Format32bppArgb;
                if (matType == MatType.CV_16UC3) return PixelFormat.Format48bppRgb;

                return PixelFormat.Undefined;
            }
        }

        public Size Size => new Size(Width, Height);

        private Imaging.ColorPalette _palette;

        /// <summary>
        /// Gets or sets the color palette used for this Bitmap
        /// </summary>
        public Imaging.ColorPalette Palette {
            get {
                if (_palette != null) {
                    return _palette;
                }
                // Create a default grayscale palette for indexed images
                if (PixelFormat == PixelFormat.Format8bppIndexed) {
                    var palette = new Imaging.ColorPalette(256);
                    for (int i = 0; i < 256; i++) {
                        palette.Entries[i] = Color.FromArgb(i, i, i);
                    }
                    return palette;
                }
                return new Imaging.ColorPalette();
            }
            set {
                _palette = value;
            }
        }

        /// <summary>
        /// Locks a bitmap into system memory
        /// </summary>
        public BitmapData LockBits(Rectangle rect, ImageLockMode mode, PixelFormat format) {
            // For OpenCV Mat, the data is already accessible
            // Return BitmapData pointing to the Mat's data
            return new BitmapData {
                Width = _mat.Width,
                Height = _mat.Height,
                Stride = (int)_mat.Step(),
                Scan0 = _mat.Data,
                PixelFormat = this.PixelFormat
            };
        }

        /// <summary>
        /// Unlocks a bitmap from system memory
        /// </summary>
        public void UnlockBits(BitmapData bitmapData) {
            // For OpenCV Mat, no action needed as data is always accessible
            // This is a no-op for compatibility
        }

        /// <summary>
        /// Saves the bitmap to a file
        /// </summary>
        public void Save(string filename) {
            if (_mat == null || _mat.Empty()) {
                throw new InvalidOperationException("Cannot save an empty bitmap");
            }
            Cv2.ImWrite(filename, _mat);
        }

        /// <summary>
        /// Saves the bitmap to a file with specified format
        /// </summary>
        public void Save(string filename, Imaging.ImageFormat format) {
            if (_mat == null || _mat.Empty()) {
                throw new InvalidOperationException("Cannot save an empty bitmap");
            }
            // OpenCV's ImWrite determines format from filename extension
            // Format parameter is for compatibility but not used
            Cv2.ImWrite(filename, _mat);
        }

        /// <summary>
        /// Saves the bitmap to a stream with specified format
        /// </summary>
        public void Save(System.IO.Stream stream, Imaging.ImageFormat format) {
            if (_mat == null || _mat.Empty()) {
                throw new InvalidOperationException("Cannot save an empty bitmap");
            }
            
            // Determine file extension from format
            string ext = ".png";
            if (format == Imaging.ImageFormat.Jpeg) ext = ".jpg";
            else if (format == Imaging.ImageFormat.Bmp) ext = ".bmp";
            else if (format == Imaging.ImageFormat.Tiff) ext = ".tiff";
            else if (format == Imaging.ImageFormat.Gif) ext = ".gif";
            
            // Encode to memory buffer
            Cv2.ImEncode(ext, _mat, out byte[] buffer);
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Saves the bitmap to a stream with specified encoder and parameters
        /// </summary>
        public void Save(System.IO.Stream stream, Imaging.ImageCodecInfo encoder, Imaging.EncoderParameters encoderParams) {
            if (_mat == null || _mat.Empty()) {
                throw new InvalidOperationException("Cannot save an empty bitmap");
            }
            
            // Determine file extension from codec
            string ext = ".png";
            if (encoder != null && encoder.FormatDescription.Contains("JPEG")) {
                ext = ".jpg";
            } else if (encoder != null && encoder.FormatDescription.Contains("PNG")) {
                ext = ".png";
            }
            
            // For JPEG with quality parameter, extract quality value
            int quality = 95;
            if (encoderParams != null && encoderParams.Param != null && encoderParams.Param.Length > 0) {
                var qualityParam = encoderParams.Param[0];
                if (qualityParam != null) {
                    quality = (int)qualityParam.Value;
                }
            }
            
            // Encode to memory buffer with quality parameter
            var encodeParams = new int[] { (int)ImwriteFlags.JpegQuality, quality };
            Cv2.ImEncode(ext, _mat, out byte[] buffer, encodeParams);
            stream.Write(buffer, 0, buffer.Length);
        }

        public void Dispose() => _mat?.Dispose();

        // Implicit conversions
        // NOTE: Returns internal Mat without cloning for performance
        // Caller must ensure Bitmap is not disposed while Mat is in use
        public static implicit operator Mat(Bitmap bmp) => bmp._mat;
        public static implicit operator Bitmap(Mat mat) => new Bitmap(mat);

        // Explicit method to get a cloned Mat
        public Mat GetMat() => _mat?.Clone();
    }

    /// <summary>
    /// Extension methods for System.Drawing types
    /// </summary>
    public static class DrawingExtensions {
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
