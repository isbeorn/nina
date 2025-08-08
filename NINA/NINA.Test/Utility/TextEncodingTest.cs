#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NUnit.Framework.Legacy;

namespace NINA.Test.Utility {

    [TestFixture]
    public class TextEncodingTest {

        [Test]
        public void UnicodeToAsciiNullTest() {
            // Arrange
            string unicode = null;
            string ascii = "";

            // Act
            string result = TextEncoding.UnicodeToAscii(unicode);

            // Assert
            ClassicAssert.AreEqual(ascii, result);
        }

        [Test]
        public void UnicodeToAsciiEmptyTest() {
            // Arrange
            string unicode = "";
            string ascii = "";

            // Act
            string result = TextEncoding.UnicodeToAscii(unicode);

            // Assert
            ClassicAssert.AreEqual(ascii, result);
        }

        [Test]
        public void UnicodeToAsciiTest() {
            // Arrange
            string unicode = "TĤis is a ștring with unicódè characterȿ.";
            string ascii = "T?is is a ?tring with unic?d? character?.";

            // Act
            string result = TextEncoding.UnicodeToAscii(unicode);

            // Assert
            ClassicAssert.AreEqual(ascii, result);
        }

        [Test]
        public void GreekToLatinAbbreviationNullTest() {
            // Arrange
            string hasGreek = null;
            string inLatin = "";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasGreek);

            // Assert
            ClassicAssert.AreEqual(inLatin, result);
        }

        [Test]
        public void GreekToLatinAbbreviationEmptyTest() {
            // Arrange
            string hasGreek = "";
            string inLatin = "";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasGreek);

            // Assert
            ClassicAssert.AreEqual(inLatin, result);
        }

        [Test]
        public void GreekToLatinAbbreviationTest() {
            // Arrange
            string hasGreek = "Σ 2103, π Hercules";
            string inLatin = "sig 2103, pi. Hercules";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasGreek);

            // Assert
            ClassicAssert.AreEqual(inLatin, result);
        }

        [Test]
        public void LatinToLatinTest() {
            // Arrange
            string hasOnylLatin = "abcdefghijklmnopqrstuvwxyzäöüABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜ1234567890!\"§$%&/()=?`*'_:;><-.,#+@²³{[]}\\~|'";
            string inLatin = "abcdefghijklmnopqrstuvwxyzäöüABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜ1234567890!\"§$%&/()=?`*'_:;><-.,#+@²³{[]}\\~|'";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasOnylLatin);

            // Assert
            ClassicAssert.AreEqual(inLatin, result);
        }
    }
}