#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Database {

    [TestFixture]
    internal class DatabaseInteractionTest {

        [Test]
        [TestCase("SHEADHEIGHT", null, "SHEADHEIGHT", "ZHADHEIGHT")]    // Returns longest on searches without a search token related to name
        [TestCase("SHEADHEIGHT", "", "SHEADHEIGHT", "ZHADHEIGHT")]    // Doesn't blow up on bad input
        [TestCase("IC 443", "IC4", "IC 443", "LBN 844", "SH2-248")]    // Starts with
        [TestCase("IC 443", "IC4", "IC 443", "IC 44", "LBN 844", "SH2-248")]    // Starts with + Length priority
        [TestCase("IC 443", "43", "IC 443", "LBN 844", "SH2-248")]    // Levenshtein
        [TestCase("SHEADHEIGHT", "ZHEAD", "SHEADHEIGHT", "ZHADHEIGHT")]    // Levenshtein + Length priority
        [TestCase("ZHADHEIGHT", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "SHEADHEIGHT", "ZHADHEIGHT")]    // Doesn't blow up on bad input
        public void testGetDisplayAliasSuccesses(string expected, string? searchString, params string[] aliases) {
            // Given a DatabaseInteraction Object
            DatabaseInteraction databaseInteraction = new DatabaseInteraction();
            // And a search term

            // And search results with aliass
            List<String> aliasList = aliases.ToList<string>();

            // When locating the closest alias
            String result = databaseInteraction.GetDisplayAlias(searchString, aliasList);

            // Then closest alias should be the expected value
            result.Should().Be(expected);
        }
    }
}