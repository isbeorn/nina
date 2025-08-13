#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Globalization;

namespace NINA.Equipment.Equipment.MyGuider.MetaGuide {

    public class MetaGuideStatusMsg : MetaGuideBaseMsg {

        private MetaGuideStatusMsg() {
        }

        public static MetaGuideStatusMsg Create(string[] args) {
            if (args.Length < 28) {
                return null;
            }
            try {
                return new MetaGuideStatusMsg() {
                    X = double.Parse(args[5], CultureInfo.InvariantCulture.NumberFormat),
                    Y = double.Parse(args[6], CultureInfo.InvariantCulture.NumberFormat),
                    EastWest = double.Parse(args[7], CultureInfo.InvariantCulture.NumberFormat),
                    NorthSouth = double.Parse(args[8], CultureInfo.InvariantCulture.NumberFormat),
                    EastWestCosine = double.Parse(args[9], CultureInfo.InvariantCulture.NumberFormat),
                    Intensity = double.Parse(args[10], CultureInfo.InvariantCulture.NumberFormat),
                    FWHM = double.Parse(args[11], CultureInfo.InvariantCulture.NumberFormat),
                    Seeing = double.Parse(args[12], CultureInfo.InvariantCulture.NumberFormat),
                    GuideMode = int.Parse(args[13], CultureInfo.InvariantCulture.NumberFormat),
                    DeltaEastArcsec = double.Parse(args[14], CultureInfo.InvariantCulture.NumberFormat),
                    DeltaNorthArcsec = double.Parse(args[15], CultureInfo.InvariantCulture.NumberFormat),
                    Locked = int.Parse(args[16], CultureInfo.InvariantCulture.NumberFormat) > 0,
                    Width = int.Parse(args[17], CultureInfo.InvariantCulture.NumberFormat),
                    Height = int.Parse(args[18], CultureInfo.InvariantCulture.NumberFormat),
                    CalibrationState = (CalibrationState)int.Parse(args[19], CultureInfo.InvariantCulture.NumberFormat),
                    WestSide = int.Parse(args[20], CultureInfo.InvariantCulture.NumberFormat),
                    RA = double.Parse(args[21], CultureInfo.InvariantCulture.NumberFormat),
                    DEC = double.Parse(args[22], CultureInfo.InvariantCulture.NumberFormat),
                    FocalLength = double.Parse(args[23], CultureInfo.InvariantCulture.NumberFormat),
                    PixelSize = double.Parse(args[24], CultureInfo.InvariantCulture.NumberFormat),
                    ArcSecPerPixel = double.Parse(args[25], CultureInfo.InvariantCulture.NumberFormat),
                    Guiding = int.Parse(args[26], CultureInfo.InvariantCulture.NumberFormat) > 0,
                    MetaGuideVersion = Version.Parse(args[27])
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double EastWest { get; private set; }
        public double NorthSouth { get; private set; }
        public double EastWestCosine { get; private set; }
        public double Intensity { get; private set; }
        public double FWHM { get; private set; }
        public double Seeing { get; private set; }
        public int GuideMode { get; private set; }
        public double DeltaEastArcsec { get; private set; }
        public double DeltaNorthArcsec { get; private set; }
        public bool Locked { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public CalibrationState CalibrationState { get; private set; }
        public int WestSide { get; private set; }
        public double RA { get; private set; }
        public double DEC { get; private set; }
        public double FocalLength { get; private set; }
        public double PixelSize { get; private set; }
        public double ArcSecPerPixel { get; private set; }
        public bool Guiding { get; private set; }
        public Version MetaGuideVersion { get; private set; }
    }
}