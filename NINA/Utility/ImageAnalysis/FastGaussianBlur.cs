#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NINA.Utility.ImageAnalysis {

    internal class FastGaussianBlur {
        private readonly byte[] gray;
        private readonly ColorPalette palette;

        private readonly int _width;
        private readonly int _height;

        private readonly ParallelOptions _pOptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        public FastGaussianBlur(Bitmap image) {
            _width = image.Width;
            _height = image.Height;
            palette = image.Palette;
            var rct = new Rectangle(0, 0, _width, _height);
            var bits = image.LockBits(rct, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int bytes = Math.Abs(bits.Stride) * bits.Height;
            var source = new byte[rct.Width * rct.Height];
            Marshal.Copy(bits.Scan0, source, 0, source.Length);
            image.UnlockBits(bits);

            gray = new byte[_width * _height];
            gray = source;
        }

        public Bitmap Process(int radial) {
            var dest = new byte[_width * _height];

            gaussBlur_4(gray, dest, radial);

            var image = new Bitmap(_width, _height, PixelFormat.Format8bppIndexed);
            image.Palette = palette;
            var rct = new Rectangle(0, 0, image.Width, image.Height);
            var bits2 = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            Marshal.Copy(dest, 0, bits2.Scan0, dest.Length);
            image.UnlockBits(bits2);
            return image;
        }

        private void gaussBlur_4(byte[] source, byte[] dest, int r) {
            var bxs = boxesForGauss(r, 3);
            boxBlur_4(source, dest, _width, _height, (bxs[0] - 1) / 2);
            boxBlur_4(dest, source, _width, _height, (bxs[1] - 1) / 2);
            boxBlur_4(source, dest, _width, _height, (bxs[2] - 1) / 2);
        }

        private int[] boxesForGauss(int sigma, int n) {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
            var wl = (int)Math.Floor(wIdeal);
            if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);

            var sizes = new List<int>();
            for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
            return sizes.ToArray();
        }

        private void boxBlur_4(byte[] source, byte[] dest, int w, int h, int r) {
            for (var i = 0; i < source.Length; i++) dest[i] = source[i];
            boxBlurH_4(dest, source, w, h, r);
            boxBlurT_4(source, dest, w, h, r);
        }

        private void boxBlurH_4(byte[] source, byte[] dest, int w, int h, int r) {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, h, _pOptions, i => {
                var ti = i * w;
                var li = ti;
                var ri = ti + r;
                var fv = source[ti];
                var lv = source[ti + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j];
                for (var j = 0; j <= r; j++) {
                    val += source[ri++] - fv;
                    dest[ti++] = (byte)Math.Floor(val * iar);
                }
                for (var j = r + 1; j < w - r; j++) {
                    val += source[ri++] - dest[li++];
                    dest[ti++] = (byte)Math.Floor(val * iar);
                }
                for (var j = w - r; j < w; j++) {
                    val += lv - source[li++];
                    dest[ti++] = (byte)Math.Floor(val * iar);
                }
            });
        }

        private void boxBlurT_4(byte[] source, byte[] dest, int w, int h, int r) {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, w, _pOptions, i => {
                var ti = i;
                var li = ti;
                var ri = ti + r * w;
                var fv = source[ti];
                var lv = source[ti + w * (h - 1)];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j * w];
                for (var j = 0; j <= r; j++) {
                    val += source[ri] - fv;
                    dest[ti] = (byte)Math.Floor(val * iar);
                    ri += w;
                    ti += w;
                }
                for (var j = r + 1; j < h - r; j++) {
                    val += source[ri] - source[li];
                    dest[ti] = (byte)Math.Floor(val * iar);
                    li += w;
                    ri += w;
                    ti += w;
                }
                for (var j = h - r; j < h; j++) {
                    val += lv - source[li];
                    dest[ti] = (byte)Math.Floor(val * iar);
                    li += w;
                    ti += w;
                }
            });
        }
    }
}