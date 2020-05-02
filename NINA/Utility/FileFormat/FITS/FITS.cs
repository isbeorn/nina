#region "copyright"

/*
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

/*
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using Namotion.Reflection;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using nom.tam.fits;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.FileFormat.FITS {

    /// <summary>
    /// Specification:
    /// https://fits.gsfc.nasa.gov/fits_standard.html
    /// http://archive.stsci.edu/fits/fits_standard/fits_standard.html
    /// </summary>
    public class FITS {

        public FITS(ushort[] data, int width, int height) {
            this.Header = new FITSHeader(width, height);
            this.Data = new FITSData(data);
        }

        public static Task<IImageData> Load(Uri filePath, bool isBayered) {
            return Task.Run<IImageData>(() => {
                Fits f = new Fits(filePath);
                ImageHDU hdu = (ImageHDU)f.ReadHDU();
                Array[] arr = (Array[])hdu.Data.DataArray;

                var width = hdu.Header.GetIntValue("NAXIS1");
                var height = hdu.Header.GetIntValue("NAXIS2");
                var bitPix = hdu.Header.GetIntValue("BITPIX");

                ushort[] pixels = new ushort[width * height];
                var i = 0;
                foreach (var row in arr) {
                    foreach (object val in row) {
                        switch (bitPix) {
                            case BITPIX_BYTE:
                                pixels[i++] = (ushort)(((byte)val / (double)byte.MaxValue) * ushort.MaxValue);
                                break;

                            case BITPIX_SHORT:
                                pixels[i++] = (ushort)(((short)val / (double)short.MaxValue) * ushort.MaxValue);
                                break;

                            case BITPIX_INT:
                                pixels[i++] = (ushort)(((int)val / (double)int.MaxValue) * ushort.MaxValue);
                                break;

                            case BITPIX_LONG:
                                pixels[i++] = (ushort)(((long)val / (double)long.MaxValue) * ushort.MaxValue);
                                break;

                            case BITPIX_FLOAT:
                                pixels[i++] = (ushort)((float)val * ushort.MaxValue);
                                break;

                            case BITPIX_DOUBLE:
                                pixels[i++] = (ushort)((double)val * ushort.MaxValue);
                                break;
                        }
                    }
                }

                //Translate nom.tam.fits into N.I.N.A. FITSHeader
                FITSHeader header = new FITSHeader(width, height);
                var iterator = hdu.Header.GetCursor();
                while (iterator.MoveNext()) {
                    HeaderCard card = (HeaderCard)((DictionaryEntry)iterator.Current).Value;
                    if (card.Value != null) {
                        if (card.IsStringValue) {
                            header.Add(card.Key, card.Value, card.Comment);
                        } else {
                            if (card.Value == "T") {
                                header.Add(card.Key, true, card.Comment);
                            } else if (card.Value.Contains(".")) {
                                header.Add(card.Key, double.Parse(card.Value, CultureInfo.InvariantCulture), card.Comment);
                            } else {
                                header.Add(card.Key, int.Parse(card.Value, CultureInfo.InvariantCulture), card.Comment);
                            }
                        }
                    }
                }

                var metaData = header.ExtractMetaData();
                return new Model.ImageData.ImageData(pixels, width, height, 16, isBayered, metaData);
            });
        }

        public FITSHeader Header { get; }

        public FITSData Data { get; }

        /// <summary>
        /// Fills FITS Header Cards using all available ImageMetaData information
        /// </summary>
        /// <param name="metaData"></param>
        public void PopulateHeaderCards(ImageMetaData metaData) {
            this.Header.PopulateFromMetaData(metaData);
        }

        public void Write(Stream s) {
            this.Header.Write(s);

            this.Data.Write(s);

            long remainingBlockPadding = (long)Math.Ceiling((double)s.Position / (double)BLOCKSIZE) * (long)BLOCKSIZE - s.Position;
            byte zeroByte = 0;
            //Pad remaining FITS block with zero values
            for (int i = 0; i < remainingBlockPadding; i++) {
                s.WriteByte(zeroByte);
            }
        }

        /* Header card size Specification: http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000 */
        public const int HEADERCARDSIZE = 80;
        /* Blocksize specification: http://archive.stsci.edu/fits/fits_standard/node13.html#SECTION00810000000000000000 */
        public const int BLOCKSIZE = 2880;
        public const int BITPIX_BYTE = 8;
        public const int BITPIX_SHORT = 16;
        public const int BITPIX_INT = 32;
        public const int BITPIX_LONG = 64;
        public const int BITPIX_FLOAT = -32;
        public const int BITPIX_DOUBLE = -64;
    }
}