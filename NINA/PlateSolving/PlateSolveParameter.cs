#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model.ImageData;
using NINA.Profile;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace NINA.PlateSolving {

    public class PlateSolveParameter {

        private static IImmutableDictionary<string, string> _missingPropertyLabels = new Dictionary<string, string> {
            { nameof(FocalLength), "LblFocalLength" },
            { nameof(PixelSize), "LblPixelSize" }
        }.ToImmutableDictionary();

        public double? FocalLength { get; set; }
        public double? PixelSize { get; set; }
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

        public static PlateSolveParameter FromImageData(
            IImageData source,
            IProfileService profileService,
            Coordinates coordinates = null) {
            var metadata = source.MetaData;
            var parameter = new PlateSolveParameter() {
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                Coordinates = coordinates
            };
            if (!double.IsNaN(metadata.Camera.PixelSize)) {
                parameter.PixelSize = metadata.Camera.PixelSize;
            }
            if (!double.IsNaN(metadata.Telescope.FocalLength)) {
                parameter.FocalLength = metadata.Telescope.FocalLength;
            }
            return parameter;
        }

        public static string GetLabelForOptionalProperty(string name) {
            if (!_missingPropertyLabels.TryGetValue(name, out string value)) {
                throw new ArgumentException(
                    $"{name} is not an optional property. Valid values are: {string.Join(",", _missingPropertyLabels.Keys)}");
            }
            return value;
        }

        public PlateSolveParameter Clone() {
            return (PlateSolveParameter)this.MemberwiseClone();
        }

        public void Update(IReadOnlyDictionary<string, double?> propertyValues) {
            foreach (var kvp in propertyValues) {
                PropertyInfo prop = this.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) {
                    throw new ArgumentException(
                        $"{kvp.Key} is not a valid PlateSolveParaemter property");
                }
                prop.SetValue(this, kvp.Value);
            }
        }
    }
}