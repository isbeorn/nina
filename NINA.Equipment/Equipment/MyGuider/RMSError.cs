#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


namespace NINA.Equipment.Equipment.MyGuider {
    public class RMSError {
        public RMSError() : this(0, 0, 0, 0, 0, 1) { }
        public RMSError(double rA, double dec, double peakRA, double peakDec, double total, double scale) {
            RA = new RMSUnit(rA, rA * scale);
            Dec = new RMSUnit(dec, dec * scale);          
            PeakRA = new RMSUnit(peakRA, peakRA * scale); ;
            PeakDec = new RMSUnit(peakDec, peakDec * scale);
            Total = new RMSUnit(total, total * scale);
        }

        public RMSUnit RA { get; }

        public RMSUnit Dec { get; }

        public RMSUnit Total { get; }

        public RMSUnit PeakRA { get; }

        public RMSUnit PeakDec { get; }
    }

    public class RMSUnit {
        public RMSUnit(double pixel, double arcseconds) {
            Pixel = pixel;
            Arcseconds = arcseconds;
        }

        public double Pixel { get; }
        public double Arcseconds { get; }
    }
}