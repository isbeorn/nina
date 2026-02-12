#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Imaging.Filters;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {

    public class BayerFilter16bpp : BayerFilter {

        public BayerFilter16bpp() : base() {
            // initialize format translation dictionary
            FormatTranslations[System.Drawing.Imaging.PixelFormat.Format16bppGrayScale] = System.Drawing.Imaging.PixelFormat.Format48bppRgb;
        }

        public bool SaveColorChannels { get; set; }

        public bool SaveLumChannel { get; set; }

        public LRGBArrays LRGBArrays { get; private set; }

        protected override unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData) {
            InitLRGBArrays(sourceData.Width * sourceData.Height);

            ushort* srcPtr = (ushort*)sourceData.ImageData.ToPointer();
            ushort* dstPtr = (ushort*)destinationData.ImageData.ToPointer();
            // Convert byte strides to ushort strides so pointer math stays in 16-bit units.
            // Destination stride must be honored for odd widths because GDI+ pads rows.
            int srcStride = sourceData.Stride / 2;
            int dstStride = destinationData.Stride / 2;

            using (MyStopWatch.Measure("Demosaicing")) {
                if (PerformDemosaicing) {
                    Demosaic(srcPtr, dstPtr, sourceData.Width, sourceData.Height, srcStride, dstStride);
                } else {
                    CopyPatternToRgb(srcPtr, dstPtr, sourceData.Width, sourceData.Height, srcStride, dstStride);
                }
            }

            using (MyStopWatch.Measure("ExtractLrgba")) {
                if (SaveColorChannels || SaveLumChannel) {
                    ExtractLrgba(dstPtr, sourceData.Width, sourceData.Height, dstStride);
                }
            }
        }

        private void InitLRGBArrays(int pixelCount) {
            if (SaveColorChannels && SaveLumChannel) {
                LRGBArrays = new LRGBArrays(
                    new ushort[pixelCount],
                    new ushort[pixelCount],
                    new ushort[pixelCount],
                    new ushort[pixelCount]);
            } else if (SaveLumChannel) {
                LRGBArrays = new LRGBArrays(
                    new ushort[pixelCount],
                    Array.Empty<ushort>(),
                    Array.Empty<ushort>(),
                    Array.Empty<ushort>());
            } else if (SaveColorChannels) {
                LRGBArrays = new LRGBArrays(
                    Array.Empty<ushort>(),
                    new ushort[pixelCount],
                    new ushort[pixelCount],
                    new ushort[pixelCount]);
            } else {
                LRGBArrays = null;
            }
        }

        private unsafe void CopyPatternToRgb(
            ushort* srcPtr,
            ushort* dstPtr,
            int width,
            int height,
            int srcStride,
            int dstStride) {
            // Use stride values expressed in ushorts so row stepping is correct with padding.
            // This is required for odd widths where the destination row has extra alignment bytes.
            ushort* src = srcPtr;
            ushort* dst = dstPtr;

            // Compute per-row padding in ushort units for source and destination.
            // These offsets are applied after each row to skip over alignment bytes.
            int srcOffset = srcStride - width;
            int dstOffset = dstStride - (width * 3);

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++, src++, dst += 3) {
                    dst[RGB.R] = dst[RGB.G] = dst[RGB.B] = 0;
                    dst[BayerPattern[y & 1, x & 1]] = *src;
                }
                src += srcOffset;
                dst += dstOffset;
            }
        }

        private unsafe void Demosaic(
            ushort* srcPtr,
            ushort* dstPtr,
            int width,
            int height,
            int srcStride,
            int dstStride) {
            int innerStartY = 1;
            int innerEndY = height - 1;

            int p00 = BayerPattern[0, 0];
            int p01 = BayerPattern[0, 1];
            int p10 = BayerPattern[1, 0];
            int p11 = BayerPattern[1, 1];
            int[] flatPattern = { p00, p01, p10, p11 };

            // Process the interior rows in parallel; the hot path assumes valid neighbors
            // so we peel borders out to a separate pass to avoid per-pixel edge checks.
            var po = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };
            Parallel.For(innerStartY, innerEndY, po, y => ProcessInnerRow(y, width, srcPtr, dstPtr, srcStride, dstStride, flatPattern));

            ProcessBorders(width, height, srcPtr, dstPtr, srcStride, dstStride);
        }

        private struct ColumnAccum {
            public int s0, s1, s2;   // sums for channels 0,1,2
            public int c0, c1, c2;   // counts for channels 0,1,2
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Acc(ref ColumnAccum col, int ch, ushort v) {
            // ch is pat[...] value: 0,1,2
            switch (ch) {
                case 0:
                    col.s0 += v; col.c0++; break;
                case 1:
                    col.s1 += v; col.c1++; break;
                default:
                    col.s2 += v; col.c2++; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void InitColumn(
            ref ColumnAccum col,
            ushort* rowTop,
            ushort* rowMid,
            ushort* rowBot,
            int x,
            int[] pat,
            int baseTop,
            int baseMid,
            int baseBot) {
            // reset accumulators
            col.s0 = col.s1 = col.s2 = 0;
            col.c0 = col.c1 = col.c2 = 0;

            int xp = x & 1;

            // top
            ushort v = rowTop[x];
            int ch = pat[baseTop + xp];
            Acc(ref col, ch, v);

            // middle
            v = rowMid[x];
            ch = pat[baseMid + xp];
            Acc(ref col, ch, v);

            // bottom
            v = rowBot[x];
            ch = pat[baseBot + xp];
            Acc(ref col, ch, v);
        }

        // Process a single interior row using a sliding 3x3 window built from three column accumulators.
        // Text graphic (rows are top/mid/bot, columns are L/C/R at x-1/x/x+1):
        //   rowTop:  a  b  c  d ...
        //   rowMid:  e  f  g  h ...
        //   rowBot:  i  j  k  l ...
        //   cols:    L={a,e,i}  C={b,f,j}  R={c,g,k}
        //   next x:  L<-C, C<-R, R<-{d,h,l}
        // Why faster: only the new right column is recomputed per pixel (3 samples instead of 9),
        // which reduces loads and pattern lookups while keeping the hot loop cache-friendly.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ProcessInnerRow(
            int y,
            int width,
            ushort* srcBase,
            ushort* dstBase,
            int srcStride,
            int dstStride,
            int[] pat) {
            // Pointers to the three rows around y
            ushort* rowTop = srcBase + (y - 1) * srcStride;
            ushort* rowMid = srcBase + y * srcStride;
            ushort* rowBot = srcBase + (y + 1) * srcStride;

            // Row parity bases for Bayer pattern
            int baseTop = ((y - 1) & 1) << 1;   // (yTop & 1) * 2
            int baseMid = (y & 1) << 1;
            int baseBot = ((y + 1) & 1) << 1;

            // Destination pointer: first inner pixel (x = 1), using destination stride.
            // This keeps row alignment correct when GDI+ inserts padding for odd widths.
            ushort* dst = dstBase + (y * dstStride) + 3;

            // Column accumulators for x-1, x, x+1.
            // We reuse two columns each step (L<-C<-R) and recompute only the new right column at x+2.
            // This reduces per-pixel work to 3 new samples and keeps the hot loop fast and cache-friendly.
            ColumnAccum colL = default, colC = default, colR = default, tmp = default;

            // Seed the sliding 3x3 window centered at x = 1 using columns 0, 1, 2
            InitColumn(ref colL, rowTop, rowMid, rowBot, 0, pat, baseTop, baseMid, baseBot);
            InitColumn(ref colC, rowTop, rowMid, rowBot, 1, pat, baseTop, baseMid, baseBot);
            InitColumn(ref colR, rowTop, rowMid, rowBot, 2, pat, baseTop, baseMid, baseBot);

            // Temporary arrays for final sums/counts per channel (stack) to avoid heap allocations
            int* sums = stackalloc int[3];
            int* counts = stackalloc int[3];

            // Track the last inner pixel that still has a full 3x3 neighborhood.
            // We iterate only up to width-3 so newX = x+2 always stays in-bounds.
            // The final inner pixel is handled after the loop to keep the hot loop branch-free.
            // This avoids per-pixel bounds checks and keeps the pipeline predictable.
            int lastInnerX = width - 2;

            for (int x = 1; x < lastInnerX; x++, dst += 3) {
                // Sum the 3x3 window = sum of three columns
                sums[0] = colL.s0 + colC.s0 + colR.s0;
                sums[1] = colL.s1 + colC.s1 + colR.s1;
                sums[2] = colL.s2 + colC.s2 + colR.s2;

                counts[0] = colL.c0 + colC.c0 + colR.c0;
                counts[1] = colL.c1 + colC.c1 + colR.c1;
                counts[2] = colL.c2 + colC.c2 + colR.c2;

                // Use RGB enum indices, same as original code
                dst[RGB.R] = (ushort)(sums[RGB.R] / counts[RGB.R]);
                dst[RGB.G] = (ushort)(sums[RGB.G] / counts[RGB.G]);
                dst[RGB.B] = (ushort)(sums[RGB.B] / counts[RGB.B]);

                // Slide window horizontally by reusing two columns and refreshing the rightmost one
                int newX = x + 2; // new right column at x+2

                tmp = colL;
                colL = colC;
                colC = colR;

                InitColumn(ref tmp, rowTop, rowMid, rowBot, newX, pat, baseTop, baseMid, baseBot);
                colR = tmp;
            }

            // Process the final inner pixel at x = lastInnerX
            sums[0] = colL.s0 + colC.s0 + colR.s0;
            sums[1] = colL.s1 + colC.s1 + colR.s1;
            sums[2] = colL.s2 + colC.s2 + colR.s2;

            counts[0] = colL.c0 + colC.c0 + colR.c0;
            counts[1] = colL.c1 + colC.c1 + colR.c1;
            counts[2] = colL.c2 + colC.c2 + colR.c2;

            dst[RGB.R] = (ushort)(sums[RGB.R] / counts[RGB.R]);
            dst[RGB.G] = (ushort)(sums[RGB.G] / counts[RGB.G]);
            dst[RGB.B] = (ushort)(sums[RGB.B] / counts[RGB.B]);
        }

        private unsafe void ProcessBorders(
            int width,
            int height,
            ushort* srcBase,
            ushort* dstBase,
            int srcStride,
            int dstStride) {

            // Handles edge pixels that do not have a full 3x3 neighborhood; kept out of the main loop.
            void ProcessPixel(int x, int y) {
                ushort* src = srcBase + y * srcStride + x;
                // Use destination stride so borders are written to the correct padded row.
                ushort* dst = dstBase + (y * dstStride) + (x * 3);

                // Stack-allocate the small accumulators to avoid per-pixel GC pressure.
                // Explicitly clear them so each pixel starts from a known state.
                int* vals = stackalloc int[3];
                int* cnts = stackalloc int[3];
                vals[0] = vals[1] = vals[2] = 0;
                cnts[0] = cnts[1] = cnts[2] = 0;

                for (int dy = -1; dy <= 1; dy++) {
                    int ny = y + dy;
                    if (ny < 0 || ny >= height) continue;

                    for (int dx = -1; dx <= 1; dx++) {
                        int nx = x + dx;
                        if (nx < 0 || nx >= width) continue;

                        int bIdx = BayerPattern[ny & 1, nx & 1];
                        vals[bIdx] += src[dy * srcStride + dx];
                        cnts[bIdx]++;
                    }
                }

                dst[RGB.R] = (ushort)(vals[RGB.R] / cnts[RGB.R]);
                dst[RGB.G] = (ushort)(vals[RGB.G] / cnts[RGB.G]);
                dst[RGB.B] = (ushort)(vals[RGB.B] / cnts[RGB.B]);
            }

            // top/bottom
            for (int x = 0; x < width; x++) {
                ProcessPixel(x, 0);
                ProcessPixel(x, height - 1);
            }

            // left/right (excluding corners)
            for (int y = 1; y < height - 1; y++) {
                ProcessPixel(0, y);
                ProcessPixel(width - 1, y);
            }
        }

        private unsafe void ExtractLrgba(ushort* rgbSrcPtr, int width, int height, int rgbSrcStride) {
            Debug.Assert(LRGBArrays != null);

            // Pin all arrays once so the GC cannot relocate them during parallel access.
            // The fixed block avoids per-row pinning and keeps pointer arithmetic safe.
            fixed (ushort* redPtr = LRGBArrays.Red)
            fixed (ushort* greenPtr = LRGBArrays.Green)
            fixed (ushort* bluePtr = LRGBArrays.Blue)
            fixed (ushort* lumPtr = LRGBArrays.Lum) {
                // Convert pinned pointers to IntPtr so the lambda can use them legally.
                // The conversion is done once, outside the parallel loop.
                IntPtr redBase = (IntPtr)redPtr;
                IntPtr greenBase = (IntPtr)greenPtr;
                IntPtr blueBase = (IntPtr)bluePtr;
                IntPtr lumBase = (IntPtr)lumPtr;

                // Snapshot the flags once so row-level branches are stable during parallel execution.
                // This keeps the control flow predictable while avoiding per-pixel conditionals.
                bool saveColor = SaveColorChannels;
                bool saveLum = SaveLumChannel;

                Parallel.For(0, height, y => {
                    // Start from the beginning of the source RGB row using the padded stride.
                    // This keeps odd-width rows aligned with GDI+ padding rules.
                    ushort* rowStart = rgbSrcPtr + (y * rgbSrcStride);

                    // Compute the packed output offset once so array writes are contiguous.
                    // Each LRGBArrays plane is stored without padding between rows.
                    int offset = y * width;

                    if (saveColor) {
                        // Build row pointers for the color planes using the packed row offset.
                        // Using row pointers keeps the inner loop simple and bounds-check free.
                        ushort* redRow = (ushort*)redBase + offset;
                        ushort* greenRow = (ushort*)greenBase + offset;
                        ushort* blueRow = (ushort*)blueBase + offset;

                        // Walk the source RGB row and write the three color planes.
                        // The mapping preserves the legacy Red=B, Green=G, Blue=R layout.
                        ushort* src = rowStart;
                        for (int x = 0; x < width; x++) {
                            ushort b = src[RGB.B];
                            ushort g = src[RGB.G];
                            ushort r = src[RGB.R];
                            src += 3;

                            redRow[x] = b;
                            greenRow[x] = g;
                            blueRow[x] = r;
                        }
                    }

                    if (saveLum) {
                        // Build the luminance row pointer using the packed row offset.
                        // This keeps the luminance buffer tightly packed and contiguous.
                        ushort* lumRow = (ushort*)lumBase + offset;

                        // Walk the source RGB row and compute luminance only.
                        // The floating divide keeps luminance bit-exact with existing behavior.
                        ushort* src = rowStart;
                        for (int x = 0; x < width; x++) {
                            ushort b = src[RGB.B];
                            ushort g = src[RGB.G];
                            ushort r = src[RGB.R];
                            src += 3;

                            lumRow[x] = (ushort)((r + g + b) / 3.0);
                        }
                    }
                });
            }
        }
    }
}
