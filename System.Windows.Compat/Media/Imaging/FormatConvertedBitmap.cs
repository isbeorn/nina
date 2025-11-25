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

namespace System.Windows.Media.Imaging {
    /// <summary>
    /// Converts a BitmapSource to a different pixel format using OpenCV
    /// </summary>
    public class FormatConvertedBitmap : BitmapSource {
        private BitmapSource _source;
        private Media.PixelFormat _destinationFormat;

        public FormatConvertedBitmap() {
        }

        public FormatConvertedBitmap(BitmapSource source, Media.PixelFormat destinationFormat, BitmapPalette palette, double alphaThreshold) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
            _destinationFormat = destinationFormat;
            Initialize();
        }

        public BitmapSource Source {
            get => _source;
            set => _source = value;
        }

        public Media.PixelFormat DestinationFormat {
            get => _destinationFormat;
            set => _destinationFormat = value;
        }

        public void BeginInit() {
            // Initialization started
        }

        public void EndInit() {
            // Initialization complete, now perform the conversion
            Initialize();
        }

        private void Initialize() {
            if (_source == null) {
                _mat = new Mat();
                return;
            }

            Mat sourceMat = _source;
            if (sourceMat.Empty()) {
                _mat = new Mat();
                return;
            }

            // Convert based on destination format
            _mat = ConvertFormat(sourceMat, _source.Format, _destinationFormat);
        }

        private Mat ConvertFormat(Mat source, Media.PixelFormat sourceFormat, Media.PixelFormat destFormat) {
            // Determine target OpenCV type
            MatType targetType;
            ColorConversionCodes? conversionCode = null;

            // Map destination format to MatType
            if (destFormat == Media.PixelFormats.Gray8) {
                targetType = MatType.CV_8UC1;

                // Determine conversion code based on source format
                if (sourceFormat == Media.PixelFormats.Bgr24 || sourceFormat == Media.PixelFormats.Bgr32) {
                    conversionCode = ColorConversionCodes.BGR2GRAY;
                } else if (sourceFormat == Media.PixelFormats.Bgra32 || sourceFormat == Media.PixelFormats.Pbgra32) {
                    conversionCode = ColorConversionCodes.BGRA2GRAY;
                } else if (sourceFormat == Media.PixelFormats.Gray16) {
                    // Convert from 16-bit to 8-bit
                    Mat result = new Mat();
                    source.ConvertTo(result, MatType.CV_8UC1, 1.0 / 257.0); // 257 = 65535/255
                    return result;
                } else if (sourceFormat == Media.PixelFormats.Gray8 || sourceFormat == Media.PixelFormats.Indexed8) {
                    // Already grayscale
                    return source.Clone();
                }
            } else if (destFormat == Media.PixelFormats.Gray16) {
                targetType = MatType.CV_16UC1;

                if (sourceFormat == Media.PixelFormats.Gray16) {
                    // Already 16-bit grayscale
                    return source.Clone();
                } else if (sourceFormat == Media.PixelFormats.Gray8 || sourceFormat == Media.PixelFormats.Indexed8) {
                    // Convert from 8-bit to 16-bit
                    Mat result = new Mat();
                    source.ConvertTo(result, MatType.CV_16UC1, 257.0); // 257 = 65535/255
                    return result;
                }
            } else if (destFormat == Media.PixelFormats.Bgr24) {
                targetType = MatType.CV_8UC3;

                if (sourceFormat == Media.PixelFormats.Gray8 || sourceFormat == Media.PixelFormats.Indexed8) {
                    conversionCode = ColorConversionCodes.GRAY2BGR;
                } else if (sourceFormat == Media.PixelFormats.Bgra32) {
                    conversionCode = ColorConversionCodes.BGRA2BGR;
                }
            } else if (destFormat == Media.PixelFormats.Bgra32) {
                targetType = MatType.CV_8UC4;

                if (sourceFormat == Media.PixelFormats.Gray8 || sourceFormat == Media.PixelFormats.Indexed8) {
                    conversionCode = ColorConversionCodes.GRAY2BGRA;
                } else if (sourceFormat == Media.PixelFormats.Bgr24) {
                    conversionCode = ColorConversionCodes.BGR2BGRA;
                }
            } else {
                // Unsupported conversion, return clone
                return source.Clone();
            }

            // Perform conversion
            if (conversionCode.HasValue) {
                Mat result = new Mat();
                Cv2.CvtColor(source, result, conversionCode.Value);
                return result;
            }

            return source.Clone();
        }
    }
}
