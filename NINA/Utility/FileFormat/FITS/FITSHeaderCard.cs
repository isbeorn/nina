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

using System;
using System.Globalization;
using System.Text;

namespace NINA.Utility.FileFormat.FITS {

    public class FITSHeaderCard {
        /* Extended ascii encoding*/
        private static Encoding ascii = Encoding.GetEncoding("iso-8859-1");

        public FITSHeaderCard(string key, string value, string comment) {
            /*
             * FITS Standard 4.0, Section 4.2.1:
             * A single quote is represented within a string as two successive single quotes
             */
            this.Key = key;
            if (value.Length > 18) {
                value = value.Substring(0, 18);
            }
            this.Value = $"'{value.Replace(@"'", @"''")}'".PadRight(20);

            if (comment?.Length > 45) {
                comment = comment.Substring(0, 45);
            } else {
                comment = string.Empty;
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, bool value, string comment) {
            this.Key = key;
            this.Value = value ? "T" : "F";
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, double value, string comment) {
            this.Key = key;
            this.Value = Math.Round(value, 15).ToString(CultureInfo.InvariantCulture);
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, DateTime value, string comment) {
            this.Key = key;
            this.Value = @"'" + value.ToString(@"yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + @"'";
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, int value, string comment) {
            this.Key = key;
            this.Value = value.ToString(CultureInfo.InvariantCulture);
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public string Key { get; }
        public string Value { get; }
        public string Comment { get; }

        public string GetHeaderString() {
            var encodedKeyword = Key.ToUpper().PadRight(8);
            var encodedValue = Value.PadLeft(20);

            var header = $"{encodedKeyword}= {encodedValue} / ";
            var encodedComment = Comment.PadRight(80 - header.Length);
            header += encodedComment;
            return header;
        }

        /// <summary>
        /// Encodes a FITS header according to FITS specifications to be exactly 80 characters long
        /// value + comment length must not exceed 67 characters
        /// </summary>
        /// <param name="keyword">FITS Keyword. Max length 8 chars</param>
        /// <param name="value">Keyword Value</param>
        /// <param name="comment">Description of Keyword</param>
        /// <remarks>
        /// Header Specification:
        /// http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000
        /// More in depth: https://fits.gsfc.nasa.gov/fits_standard.html
        /// </remarks>
        public byte[] Encode() {
            return ascii.GetBytes(GetHeaderString());
        }

        public override string ToString() {
            return $"{Key} - {Value} - {Comment}";
        }
    }
}