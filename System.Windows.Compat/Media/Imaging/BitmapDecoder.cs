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
using System;
using System.Collections.Generic;
using System.IO;

namespace System.Windows.Media.Imaging {
    /// <summary>
    /// Color context for color management
    /// </summary>
    public class ColorContext {
    }

    /// <summary>
    /// BitmapCreateOptions - options for creating bitmaps
    /// </summary>
    [Flags]
    public enum BitmapCreateOptions {
        None = 0,
        PreservePixelFormat = 1,
        DelayCreation = 2,
        IgnoreColorProfile = 4,
        IgnoreImageCache = 8
    }

    /// <summary>
    /// TiffBitmapDecoder - decodes TIFF images using OpenCV
    /// </summary>
    public class TiffBitmapDecoder : BitmapDecoder {
        public TiffBitmapDecoder(Stream stream, BitmapCreateOptions createOptions, BitmapCacheOption cacheOption) {
            LoadFromStream(stream);
        }

        public TiffBitmapDecoder(Uri uri, BitmapCreateOptions createOptions, BitmapCacheOption cacheOption) {
            if (uri.IsFile) {
                LoadFromFile(uri.LocalPath);
            }
        }

        private void LoadFromStream(Stream stream) {
            // Read stream into memory
            byte[] buffer;
            if (stream is MemoryStream ms) {
                buffer = ms.ToArray();
            } else {
                using (var memStream = new MemoryStream()) {
                    stream.CopyTo(memStream);
                    buffer = memStream.ToArray();
                }
            }

            // Decode using OpenCV
            Mat mat = Cv2.ImDecode(buffer, ImreadModes.Unchanged);
            if (mat != null && !mat.Empty()) {
                var frame = new BitmapFrame(mat);
                Frames.Add(frame);
            }
        }

        private void LoadFromFile(string filePath) {
            Mat mat = Cv2.ImRead(filePath, ImreadModes.Unchanged);
            if (mat != null && !mat.Empty()) {
                var frame = new BitmapFrame(mat);
                Frames.Add(frame);
            }
        }
    }

    /// <summary>
    /// GifBitmapDecoder - decodes GIF images using OpenCV
    /// </summary>
    public class GifBitmapDecoder : BitmapDecoder {
        public GifBitmapDecoder(Uri uri, BitmapCreateOptions createOptions, BitmapCacheOption cacheOption) {
            if (uri.IsFile) {
                Mat mat = Cv2.ImRead(uri.LocalPath, ImreadModes.Unchanged);
                if (mat != null && !mat.Empty()) {
                    var frame = new BitmapFrame(mat);
                    Frames.Add(frame);
                }
            }
        }
    }

    /// <summary>
    /// JpegBitmapDecoder - decodes JPEG images using OpenCV
    /// </summary>
    public class JpegBitmapDecoder : BitmapDecoder {
        public JpegBitmapDecoder(Uri uri, BitmapCreateOptions createOptions, BitmapCacheOption cacheOption) {
            if (uri.IsFile) {
                Mat mat = Cv2.ImRead(uri.LocalPath, ImreadModes.Unchanged);
                if (mat != null && !mat.Empty()) {
                    var frame = new BitmapFrame(mat);
                    Frames.Add(frame);
                }
            }
        }
    }

    /// <summary>
    /// PngBitmapDecoder - decodes PNG images using OpenCV
    /// </summary>
    public class PngBitmapDecoder : BitmapDecoder {
        public PngBitmapDecoder(Uri uri, BitmapCreateOptions createOptions, BitmapCacheOption cacheOption) {
            if (uri.IsFile) {
                Mat mat = Cv2.ImRead(uri.LocalPath, ImreadModes.Unchanged);
                if (mat != null && !mat.Empty()) {
                    var frame = new BitmapFrame(mat);
                    Frames.Add(frame);
                }
            }
        }
    }

    /// <summary>
    /// BitmapFrame - represents a frame in a decoded bitmap
    /// </summary>
    public class BitmapFrame : BitmapSource {
        public BitmapFrame(Mat mat) : base(mat) { }

        /// <summary>
        /// Metadata associated with the frame
        /// </summary>
        public BitmapMetadata Metadata { get; set; }

        public static BitmapFrame Create(BitmapSource source) {
            Mat mat = source;
            return new BitmapFrame(mat);
        }

        public static BitmapFrame Create(BitmapSource source, BitmapSource thumbnail, BitmapMetadata metadata, System.Collections.ObjectModel.ReadOnlyCollection<ColorContext> colorContexts) {
            var frame = new BitmapFrame((Mat)source);
            frame.Metadata = metadata;
            return frame;
        }
    }

    /// <summary>
    /// Base class for bitmap decoders
    /// </summary>
    public abstract class BitmapDecoder {
        public List<BitmapFrame> Frames { get; protected set; } = new List<BitmapFrame>();

        public BitmapFrame Preview => Frames.Count > 0 ? Frames[0] : null;

        public BitmapFrame Thumbnail => Frames.Count > 0 ? Frames[0] : null;
    }
}
