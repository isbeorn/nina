#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Imaging.Filters;
using NINA.Model.ImageData;
using System;

namespace NINA.Utility.ImageAnalysis {

    public class BayerFilter16bpp : BayerFilter {

        public BayerFilter16bpp() : base() {
            // initialize format translation dictionary
            FormatTranslations[System.Drawing.Imaging.PixelFormat.Format16bppGrayScale] = System.Drawing.Imaging.PixelFormat.Format48bppRgb;
        }

        public bool SaveColorChannels { get; set; }

        public bool SaveLumChannel { get; set; }

        public LRGBArrays LRGBArrays { get; set; }

        protected override unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData) {
            // get width and height
            int width = sourceData.Width;
            int height = sourceData.Height;

            if (SaveColorChannels && SaveLumChannel) {
                LRGBArrays = new LRGBArrays(new ushort[width * height], new ushort[width * height], new ushort[width * height], new ushort[width * height]);
            } else if (!SaveColorChannels && SaveLumChannel) {
                LRGBArrays = new LRGBArrays(new ushort[width * height], new ushort[0], new ushort[0], new ushort[0]);
            } else if (SaveColorChannels && !SaveLumChannel) {
                LRGBArrays = new LRGBArrays(new ushort[0], new ushort[width * height], new ushort[width * height], new ushort[width * height]);
            }

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
                int counter = 0;
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
                        if (SaveColorChannels) {
                            LRGBArrays.Red[counter] = dst[RGB.R];
                            LRGBArrays.Green[counter] = dst[RGB.G];
                            LRGBArrays.Blue[counter] = dst[RGB.B];
                        }
                        if (SaveLumChannel) {
                            LRGBArrays.Lum[counter] = (ushort)Math.Floor((dst[RGB.R] + dst[RGB.G] + dst[RGB.B]) / 3d);
                        }
                        counter++;
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            }
        }
    }
}
