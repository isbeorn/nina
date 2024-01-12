#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace NINA.Core.Utility {

    public static class TextEncoding {

        public static string UnicodeToAscii(string input) {
            if (string.IsNullOrWhiteSpace(input)) { return ""; }
            Encoding ascii = Encoding.ASCII;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(input);
            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);
            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            _ = ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);

            return new string(asciiChars);
        }

        public static string GreekToLatinAbbreviation(string input) {
            if (string.IsNullOrWhiteSpace(input)) { return ""; }

            foreach (DictionaryEntry greekchar in GreekToAbbreviationMap) {
                input = input.Replace(greekchar.Key.ToString(), greekchar.Value.ToString());
            }
            return input;
        }

        // Latin abbreviations for greek characters taken from SIMBAD user guide, Appendix A:
        // http://simbad.u-strasbg.fr/guide/chA.htx
        private static readonly HybridDictionary GreekToAbbreviationMap = new HybridDictionary() {
            { "Α", "alf" },
            { "α", "alf" },

            { "Β", "bet" },
            { "β", "bet" },

            { "Γ", "gam" },
            { "γ", "gam" },

            { "Δ", "del" },
            { "δ", "del" },

            { "Ε", "eps" },
            { "ε", "eps" },

            { "Ζ", "zet" },
            { "ζ", "zet" },

            { "Η", "eta" },
            { "η", "eta" },

            { "Θ", "tet" },
            { "θ", "tet" },

            { "Ι", "iot" },
            { "ι", "iot" },

            { "Κ", "kap" },
            { "κ", "kap" },

            { "Λ", "lam" },
            { "λ", "lam" },

            { "Μ", "mu." },
            { "µ", "mu." },

            { "Ν", "nu." },
            { "ν", "nu." },

            { "Ξ", "ksi" },
            { "ξ", "ksi" },

            { "Ο", "omi" },
            { "ο", "omi" },

            { "Π", "pi." },
            { "π", "pi." },

            { "Ρ", "rho" },
            { "ρ", "rho" },

            { "Σ", "sig" },
            { "σ", "sig" },

            { "Τ", "tau" },
            { "τ", "tau" },

            { "Υ", "ups" },
            { "υ", "ups" },

            { "Φ", "phi" },
            { "φ", "phi" },

            { "Χ", "khi" },
            { "χ", "khi" },

            { "Ψ", "psi" },
            { "ψ", "psi" },

            { "Ω", "ome" },
            { "ω", "ome" },
        };
    }
}