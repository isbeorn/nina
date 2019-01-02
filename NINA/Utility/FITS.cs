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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NINA.Utility {

    public class FITS {
        /* Specification: http://archive.stsci.edu/fits/fits_standard/fits_standard.html */

        public FITS(ushort[] data, int width, int height, string imageType, double exposuretime) {
            this._imageData = data;
            AddHeaderCard("SIMPLE", true, "C# FITS");
            AddHeaderCard("BITPIX", 16, "");
            AddHeaderCard("NAXIS", 2, "Dimensionality");
            AddHeaderCard("NAXIS1", width, "");
            AddHeaderCard("NAXIS2", height, "");
            AddHeaderCard("BZERO", 32768, "");
            AddHeaderCard("EXTEND", true, "Extensions are permitted");
            AddHeaderCard("DATE-OBS", DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture), "");
            AddHeaderCard("IMAGETYP", imageType, "");
            AddHeaderCard("EXPOSURE", exposuretime, "");
        }

        public void AddHeaderCard(string keyword, string value, string comment) {
            EncodeHeader(keyword, "'" + value + "'", comment);
        }

        public void AddHeaderCard(string keyword, int value, string comment) {
            EncodeHeader(keyword, value.ToString(CultureInfo.InvariantCulture), comment);
        }

        public void AddHeaderCard(string keyword, double value, string comment) {
            EncodeHeader(keyword, Math.Round(value, 15).ToString(CultureInfo.InvariantCulture), comment);
        }

        public void AddHeaderCard(string keyword, bool value, string comment) {
            EncodeHeader(keyword, value ? "T" : "F", comment);
        }

        public void Write(Stream s) {
            /* Write header */
            foreach (byte[] b in _encodedHeader) {
                s.Write(b, 0, HEADERCARDSIZE);
            }

            /* Close header http://archive.stsci.edu/fits/fits_standard/node64.html#SECTION001221000000000000000 */
            s.Write(Encoding.ASCII.GetBytes("END".PadRight(HEADERCARDSIZE)), 0, HEADERCARDSIZE);

            /* Write blank lines for the rest of the header block */
            for (var i = _encodedHeader.Count + 1; i % (BLOCKSIZE / HEADERCARDSIZE) != 0; i++) {
                s.Write(Encoding.ASCII.GetBytes("".PadRight(HEADERCARDSIZE)), 0, HEADERCARDSIZE);
            }

            /* Write image data */
            for (int i = 0; i < this._imageData.Length; i++) {
                var val = (short)(this._imageData[i] - (short.MaxValue + 1));
                s.WriteByte((byte)(val >> 8));
                s.WriteByte((byte)val);
            }

            long remainingBlockPadding = (long)Math.Ceiling((double)s.Position / (double)BLOCKSIZE) * (long)BLOCKSIZE - s.Position;
            byte zeroByte = 0;
            //Pad remaining FITS block with zero values
            for (int i = 0; i < remainingBlockPadding; i++) {
                s.WriteByte(zeroByte);
            }
        }

        /* Blocksize specification: http://archive.stsci.edu/fits/fits_standard/node13.html#SECTION00810000000000000000 */
        private const int BLOCKSIZE = 2880;
        /* Header card size Specification: http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000 */
        private const int HEADERCARDSIZE = 80;
        /* Extended ascii encoding*/
        private Encoding ascii = Encoding.GetEncoding("iso-8859-1");

        private List<byte[]> _encodedHeader = new List<byte[]>();
        private ushort[] _imageData;

        private void EncodeHeader(string keyword, string value, string comment) {
            /* Header Specification: http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000 */
            var header = keyword.ToUpper().PadRight(8) + "=" + value.PadLeft(21) + " / " + comment.PadRight(47);
            _encodedHeader.Add(ascii.GetBytes(header));
        }
    }
}