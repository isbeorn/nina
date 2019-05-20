#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using AForge.Imaging;
using AForge.Imaging.Filters;

namespace NINA.Utility.ImageAnalysis {

    public class BayerFilter16bpp : BayerFilter {

        public BayerFilter16bpp() : base() {
            // initialize format translation dictionary
            FormatTranslations[System.Drawing.Imaging.PixelFormat.Format16bppGrayScale] = System.Drawing.Imaging.PixelFormat.Format48bppRgb;
        }

        protected override unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData) {
            // get width and height
            int width = sourceData.Width;
            int height = sourceData.Height;

            int widthM1 = width - 1;
            int heightM1 = height - 1;

            int srcStride = sourceData.Stride / 2;

            int srcOffset = (srcStride - width) / 2;
            int dstOffset = (destinationData.Stride - width * 6) / 6;

            // do the job
            ushort* src = (ushort*)sourceData.ImageData.ToPointer();
            ushort* dst = (ushort*)destinationData.ImageData.ToPointer();

            int[] rgbValues = new int[3];
            int[] rgbCounters = new int[3];

            if (!PerformDemosaicing) {
                // for each line
                for (int y = 0; y < height; y++) {
                    // for each pixel
                    for (int x = 0; x < width; x++, src++, dst += 3) {
                        dst[RGB.R] = dst[RGB.G] = dst[RGB.B] = 0;
                        dst[BayerPattern[y & 1, x & 1]] = *src;
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            } else {
                // for each line
                for (int y = 0; y < height; y++) {
                    // for each pixel
                    for (int x = 0; x < width; x++, src++, dst += 3) {
                        rgbValues[0] = rgbValues[1] = rgbValues[2] = 0;
                        rgbCounters[0] = rgbCounters[1] = rgbCounters[2] = 0;

                        int bayerIndex = BayerPattern[y & 1, x & 1];

                        rgbValues[bayerIndex] += *src;
                        rgbCounters[bayerIndex]++;

                        if (x != 0) {
                            bayerIndex = BayerPattern[y & 1, (x - 1) & 1];

                            rgbValues[bayerIndex] += src[-1];
                            rgbCounters[bayerIndex]++;
                        }

                        if (x != widthM1) {
                            bayerIndex = BayerPattern[y & 1, (x + 1) & 1];

                            rgbValues[bayerIndex] += src[1];
                            rgbCounters[bayerIndex]++;
                        }

                        if (y != 0) {
                            bayerIndex = BayerPattern[(y - 1) & 1, x & 1];

                            rgbValues[bayerIndex] += src[-srcStride];
                            rgbCounters[bayerIndex]++;

                            if (x != 0) {
                                bayerIndex = BayerPattern[(y - 1) & 1, (x - 1) & 1];

                                rgbValues[bayerIndex] += src[-srcStride - 1];
                                rgbCounters[bayerIndex]++;
                            }

                            if (x != widthM1) {
                                bayerIndex = BayerPattern[(y - 1) & 1, (x + 1) & 1];

                                rgbValues[bayerIndex] += src[-srcStride + 1];
                                rgbCounters[bayerIndex]++;
                            }
                        }

                        if (y != heightM1) {
                            bayerIndex = BayerPattern[(y + 1) & 1, x & 1];

                            rgbValues[bayerIndex] += src[srcStride];
                            rgbCounters[bayerIndex]++;

                            if (x != 0) {
                                bayerIndex = BayerPattern[(y + 1) & 1, (x - 1) & 1];

                                rgbValues[bayerIndex] += src[srcStride - 1];
                                rgbCounters[bayerIndex]++;
                            }

                            if (x != widthM1) {
                                bayerIndex = BayerPattern[(y + 1) & 1, (x + 1) & 1];
                                rgbValues[bayerIndex] += src[srcStride + 1];
                                rgbCounters[bayerIndex]++;
                            }
                        }

                        dst[RGB.R] = (ushort)(rgbValues[RGB.R] / rgbCounters[RGB.R]);
                        dst[RGB.G] = (ushort)(rgbValues[RGB.G] / rgbCounters[RGB.G]);
                        dst[RGB.B] = (ushort)(rgbValues[RGB.B] / rgbCounters[RGB.B]);
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            }
        }
    }
}