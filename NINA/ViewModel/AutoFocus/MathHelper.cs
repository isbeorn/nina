#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

    Hyperbolic fitting based on CCDCiel source, also under GPL3
    Copyright (C) 2018 Patrick Chevalley & Han Kleijn (author)

    http://www.ap-i.net
    h@ap-i.net

    http://www.hnsky.org
*/

#endregion "copyright"

using System;

namespace NINA.ViewModel {
    public static class MathHelper {

        // Hyperbolic Sine
        public static double HSin(double x) {
            return (Math.Exp(x) - Math.Exp(-x)) / 2;
        }

        // Hyperbolic Cosine
        public static double HCos(double x) {
            return (Math.Exp(x) + Math.Exp(-x)) / 2;
        }

        // Hyperbolic Tangent
        public static double HTan(double x) {
            return (Math.Exp(x) - Math.Exp(-x)) / (Math.Exp(x) + Math.Exp(-x));
        }

        // Inverse Hyperbolic Sine
        public static double HArcsin(double x) {
            return Math.Log(x + Math.Sqrt(x * x + 1));
        }

        // Inverse Hyperbolic Cosine
        public static double HArccos(double x) {
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }

        // Inverse Hyperbolic Tangent
        public static double HArctan(double x) {
            return Math.Log((1 + x) / (1 - x)) / 2;
        }
    }
}