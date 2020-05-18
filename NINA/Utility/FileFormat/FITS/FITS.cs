#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 
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

                var dimensions = hdu.Header.GetIntValue("NAXIS");
                if (dimensions > 2) {
                    //Debayered Images are not supported. Take the first dimension instead to get at least a monochrome image
                    arr = (Array[])arr[0];
                }

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
                                if (double.TryParse(card.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) {
                                    header.Add(card.Key, value, card.Comment);
                                }
                            } else {
                                if (int.TryParse(card.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                                    header.Add(card.Key, value, card.Comment);
                            }
                        }
                    }
                }

                var metaData = new ImageMetaData();
                try {
                    metaData = header.ExtractMetaData();
                } catch (Exception ex) {
                    Logger.Error(ex.Message);
                }
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
