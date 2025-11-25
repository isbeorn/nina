#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.IO;
using OpenCvSharp;

namespace System.Windows.Media.Imaging {
    /// <summary>
    /// TIFF compression options
    /// </summary>
    public enum TiffCompressOption {
        None,
        Lzw,
        Zip
    }

    /// <summary>
    /// Base class for bitmap encoders
    /// </summary>
    public abstract class BitmapEncoder {
        public BitmapFrameCollection Frames { get; } = new BitmapFrameCollection();

        public abstract void Save(Stream stream);
    }

    /// <summary>
    /// TIFF bitmap encoder
    /// </summary>
    public class TiffBitmapEncoder : BitmapEncoder {
        public TiffCompressOption Compression { get; set; } = TiffCompressOption.None;

        public override void Save(Stream stream) {
            if (Frames.Count == 0) {
                throw new InvalidOperationException("No frames to encode");
            }

            // Use the first frame's bitmap source
            var frame = Frames[0];
            Mat mat = frame; // Use implicit conversion from BitmapSource to Mat

            // Encode as TIFF using OpenCV
            Cv2.ImEncode(".tif", mat, out byte[] buffer);
            stream.Write(buffer, 0, buffer.Length);
        }
    }

    /// <summary>
    /// PNG bitmap encoder
    /// </summary>
    public class PngBitmapEncoder : BitmapEncoder {
        public override void Save(Stream stream) {
            if (Frames.Count == 0) {
                throw new InvalidOperationException("No frames to encode");
            }

            // Use the first frame's bitmap source
            var frame = Frames[0];

            Mat mat = frame; // Use implicit conversion from BitmapSource to Mat

            if (mat == null) {
                throw new InvalidOperationException("Mat is null");
            }

            if (mat.Empty()) {
                throw new InvalidOperationException("Mat is empty");
            }

            // Check if the Mat's data pointer is valid
            if (mat.Data == IntPtr.Zero) {
                throw new InvalidOperationException("Mat data pointer is null");
            }

            try {
                // Workaround: Use ImWrite directly with the original mat
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"opencv_temp_{Guid.NewGuid()}.png");

                try {
                    var encodingParams = new ImageEncodingParam[] {
                        new ImageEncodingParam(ImwriteFlags.PngCompression, 3)
                    };

                    // Write to file using the original mat directly
                    Cv2.ImWrite(tempFile, mat, encodingParams);

                    // Read back and write to stream
                    byte[] buffer = System.IO.File.ReadAllBytes(tempFile);
                    stream.Write(buffer, 0, buffer.Length);
                } finally {
                    // Clean up temp file
                    if (System.IO.File.Exists(tempFile)) {
                        System.IO.File.Delete(tempFile);
                    }
                }
            } catch (Exception ex) {
                throw new InvalidOperationException($"PNG encoding failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// JPEG bitmap encoder
    /// </summary>
    public class JpegBitmapEncoder : BitmapEncoder {
        public int QualityLevel { get; set; } = 90;

        public override void Save(Stream stream) {
            if (Frames.Count == 0) {
                throw new InvalidOperationException("No frames to encode");
            }

            // Use the first frame's bitmap source
            var frame = Frames[0];
            Mat mat = frame; // Use implicit conversion from BitmapSource to Mat

            // JPEG only supports 8-bit, so convert if necessary
            Mat matToEncode = mat;
            bool needsCleanup = false;

            if (mat.Depth() == MatType.CV_16U) {
                // Convert 16-bit to 8-bit for JPEG
                matToEncode = new Mat();
                mat.ConvertTo(matToEncode, MatType.CV_8U, 1.0 / 256.0);
                needsCleanup = true;
            }

            try {
                // Set JPEG quality
                var encodingParams = new ImageEncodingParam[] {
                    new ImageEncodingParam(ImwriteFlags.JpegQuality, QualityLevel)
                };

                // Encode as JPEG using OpenCV
                Cv2.ImEncode(".jpg", matToEncode, out byte[] buffer, encodingParams);
                stream.Write(buffer, 0, buffer.Length);
            } finally {
                if (needsCleanup) {
                    matToEncode.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Collection of bitmap frames for encoding
    /// </summary>
    public class BitmapFrameCollection : System.Collections.Generic.List<BitmapFrame> {
    }
}
