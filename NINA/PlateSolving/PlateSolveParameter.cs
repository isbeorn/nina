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
        public Coordinates Coordinates { get; set; }

        public override string ToString() {
            var formatCoordinates = Coordinates != null ? $"Reference Coordinates RA: {Coordinates.RAString} Dec: {Coordinates.DecString} Epoch: {Coordinates.Epoch}" : "";
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