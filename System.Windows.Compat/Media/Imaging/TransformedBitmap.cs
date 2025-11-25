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
using System.Windows.Media;

namespace System.Windows.Media.Imaging {
    /// <summary>
    /// TransformedBitmap - applies a transform to a bitmap using OpenCV
    /// </summary>
    public class TransformedBitmap : BitmapSource {
        private BitmapSource _source;
        private Transform _transform;

        public TransformedBitmap() : base() {
        }

        public TransformedBitmap(BitmapSource source, Transform transform) : base() {
            _source = source;
            _transform = transform;
            ApplyTransform();
        }

        public BitmapSource Source {
            get => _source;
            set => _source = value;
        }

        public Transform Transform {
            get => _transform;
            set => _transform = value;
        }

        public void BeginInit() {
            // Initialization started
        }

        public void EndInit() {
            // Initialization complete - apply the transform
            ApplyTransform();
        }

        private void ApplyTransform() {
            if (_source == null || _transform == null) {
                return;
            }

            Mat sourceMat = _source;
            Mat result = new Mat();

            // Apply the transform
            if (_transform is ScaleTransform scaleTransform) {
                double scaleX = scaleTransform.ScaleX;
                double scaleY = scaleTransform.ScaleY;

                // Reject invalid scale values (NaN, Infinity) and no-op zero scales
                if (double.IsNaN(scaleX) || double.IsInfinity(scaleX) || double.IsNaN(scaleY) || double.IsInfinity(scaleY)) {
                    // Do not attempt to resize with invalid factors; copy the source
                    sourceMat.CopyTo(result);
                    _mat = result;
                    return;
                }

                if (scaleX == 0.0 || scaleY == 0.0) {
                    // A scale of zero is effectively an invalid resize; copy the source
                    sourceMat.CopyTo(result);
                    _mat = result;
                    return;
                }

                // Determine new dimensions; ensure at least 1 pixel in each dimension
                int newWidth = Math.Max(1, (int)Math.Round(sourceMat.Width * Math.Abs(scaleX)));
                int newHeight = Math.Max(1, (int)Math.Round(sourceMat.Height * Math.Abs(scaleY)));

                // Resize first (using absolute scale), then flip if needed for negative scales
                using var resized = new Mat();
                Cv2.Resize(sourceMat, resized, new Size(newWidth, newHeight), 0, 0, InterpolationFlags.Linear);

                if (scaleX < 0 || scaleY < 0) {
                    FlipMode flipMode = FlipMode.X; // Flip horizontally by default
                    if (scaleX < 0 && scaleY < 0) {
                        flipMode = FlipMode.XY; // Flip both
                    } else if (scaleY < 0) {
                        flipMode = FlipMode.Y; // Flip vertically
                    }
                    Cv2.Flip(resized, result, flipMode);
                } else {
                    resized.CopyTo(result);
                }
            } else {
                // For other transforms, just copy the source
                sourceMat.CopyTo(result);
            }

            _mat = result;
        }
    }
}
