#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.Text;

namespace NINA.Image.FileFormat.FITS {

    public class FITSHeaderCard {
        /* Extended ascii encoding*/
        private static Encoding ascii = Encoding.GetEncoding("iso-8859-1");

        public FITSHeaderCard(string key, string value, string comment) {
            this.Key = key;
            if (value == null) { value = string.Empty; }
            
            /*
             * FITS Standard 4.0, Section 4.2.1:
             * A single quote is represented within a string as two successive single quotes
             */
            value = value.Replace(@"'", @"''");

            // Header total is 80 - Keyword = 8, Keyword Separator = 2, String Quotes = 2,
            var totalLength = 68;
            var valueMaxLength = totalLength;
            var commentSeparatorLenght = 3;
            var valueMaxLengthWithComment = totalLength - commentSeparatorLenght - 1;
            if (!string.IsNullOrWhiteSpace(comment) && value.Length <= valueMaxLengthWithComment) {
                // with a comment the total length is reduced by 4 for the " / " separator
                // but skip the comment if value is too long
                // otherwise make sure the comment gets at least one character
                valueMaxLength = valueMaxLengthWithComment;
            }

            if (value.Length > valueMaxLength) {
                value = value.Substring(0, valueMaxLength);
            }

            if (comment == null) { comment = string.Empty; }
            var commentLength = Math.Min(47, Math.Max(0, totalLength - value.Length - commentSeparatorLenght));
            if (comment?.Length > commentLength) {
                comment = comment.Substring(0, commentLength);
            }

            
            this.Value = $"'{value}'".PadRight(totalLength - commentLength - commentSeparatorLenght + 2);
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, bool value, string comment) {
            this.Key = key;
            this.Value = value ? "T" : "F";
            if (comment?.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, double value, string comment) {
            this.Key = key;
            this.Value = (value.ToString("0.0##############", CultureInfo.InvariantCulture));
            if (comment?.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, DateTime value, string comment) {
            this.Key = key;
            this.Value = @"'" + value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + @"'";
            if (comment?.Length > 40) {
                comment = comment.Substring(0, 40);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, int value, string comment) {
            this.Key = key;
            this.Value = value.ToString(CultureInfo.InvariantCulture);
            if (comment?.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public string Key { get; }
        public string Value { get; }
        public string Comment { get; }

        public string OriginalValue {
            get {
                if (string.IsNullOrWhiteSpace(Value)) {
                    return string.Empty;
                } else if (Value.StartsWith("'")) {
                    var trimmedValue = Value.Trim();
                    return trimmedValue.Remove(trimmedValue.Length - 1, 1).Remove(0, 1).Replace(@"''", @"'");
                } else {
                    return Value;
                }
            }
        }

        public string GetHeaderString() {
            var encodedKeyword = Key.ToUpper().PadRight(8);
            var encodedValue = Value.PadLeft(20);

            var header = $"{encodedKeyword}= {encodedValue}";
            if (!string.IsNullOrWhiteSpace(Comment)) {
                header += " / ";
            }
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