#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System;

namespace NINA.PlateSolving {

    public class PlateSolveParameter {
        private double pixelSize;

        public double FocalLength { get; set; }

        public double PixelSize {
            get => Math.Max(Binning, 1) * pixelSize;
            set => pixelSize = value;
        }

        public int Binning { get; set; }
        public double SearchRadius { get; set; }
        public double Regions { get; set; }
        public int DownSampleFactor { get; set; }
        public int MaxObjects { get; set; }
        private Coordinates coordinates;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                coordinates = value?.Transform(Epoch.J2000);
            }
        }

        public override string ToString() {
            var j2000 = Coordinates?.Transform(Epoch.J2000);
            var formatCoordinates = j2000 != null ? $"Reference Coordinates RA: {j2000.RAString} Dec: {j2000.DecString} Epoch: {j2000.Epoch}" : "";
            return $"FocalLength: {FocalLength}" + Environment.NewLine +
                $"PixelSize: {PixelSize}" + Environment.NewLine +
                $"SearchRadius: {SearchRadius}" + Environment.NewLine +
                $"Regions: {Regions}" + Environment.NewLine +
                $"DownSampleFactor: {DownSampleFactor}" + Environment.NewLine +
                $"MaxObjects: {MaxObjects}" + Environment.NewLine +
                $"{formatCoordinates}";
        }

        public PlateSolveParameter Clone() {
            var clone = (PlateSolveParameter)this.MemberwiseClone();
            clone.Coordinates = clone.Coordinates?.Transform(clone.Coordinates.Epoch);
            return clone;
        }
    }
}
