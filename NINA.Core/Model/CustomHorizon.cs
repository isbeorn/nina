#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NINA.Core.Model {

    public class CustomHorizon {
        private static readonly JsonSerializer JSON_SERIALIZER = JsonSerializer.Create();
        private double[] azimuths;
        private double[] altitudes;

        /// <param name="horizonMap">A map containing azimuth->altitude mappings</param>
        private CustomHorizon(IDictionary<double, double> horizonMap) {
            this.azimuths = horizonMap.Keys.ToArray();
            this.altitudes = horizonMap.Values.ToArray();
        }

        public double GetAltitude(double azimuth) {
            if (azimuth < 0 || azimuth > 359) { azimuth = Utility.CoreUtil.EuclidianModulus(azimuth, 360); }
            return Accord.Math.Tools.Interpolate1D(azimuth, azimuths, altitudes, 0, 0);
        }

        public double GetMaxAltitude() {
            return this.altitudes.Max();
        }

        public double GetMinAltitude() {
            return this.altitudes.Min();
        }

        private static void GroomHorizonData(SortedDictionary<double, double> horizonMap) {
            if (horizonMap.Count < 2) {
                throw new ArgumentException("Horizon file does not contain enough entries or is invalid");
            }

            // Groom Incomplete Data
            // 1) No 0 or 360 Azimuth is detected => Find the nearest datapoint and add it to the list
            // 2) No 0 Azimuth is present but 360 Azimuth is present => Add value from 360 to 0
            // 2) No 360 Azimuth is present but 0 Azimuth is present => Add value from 0 to 360
            if (!horizonMap.ContainsKey(0) && !horizonMap.ContainsKey(360)) {
                var nearest0Azimuth = horizonMap.Keys.OrderBy(x => Math.Abs(x)).First();
                var nearest360Azimuth = horizonMap.Keys.OrderByDescending(x => Math.Abs(x)).First();

                var key = nearest0Azimuth;
                if (360 - nearest360Azimuth < nearest0Azimuth) {
                    key = nearest360Azimuth;
                }

                horizonMap[0] = horizonMap[key];
                horizonMap[360] = horizonMap[key];
            } else if (!horizonMap.ContainsKey(0) && horizonMap.ContainsKey(360)) {
                horizonMap[0] = horizonMap[360];
            } else if (horizonMap.ContainsKey(0) && !horizonMap.ContainsKey(360)) {
                horizonMap[360] = horizonMap[0];
            }
        }

        /// <summary>
        /// Creates an instance of the custom horizon object to calculate horizon altitude based on a given azimuth out of a NINA-standard file that specifies the horizon
        /// The Horizon file must consist of a list of azimuth and alitutde pairs that are separated by a space and line breaks
        /// A minimum of two points are required to approximate the horizon
        /// Lines starting with '#' character will be treated as comments and therefore ignored
        /// </summary>
        /// <example>
        /// # File Example
        /// 0 10
        /// 90 20
        /// 180 30
        /// 270 20
        /// 360 10
        /// </example>
        /// <param name="filePath">The file pointing to the horizon file</param>
        /// <returns>An instance of CustomHorizon</returns>
        public static CustomHorizon FromReader_Standard(TextReader sr) {
            var horizonMap = new SortedDictionary<double, double>();

            string line;
            while ((line = sr.ReadLine()?.Trim()) != null) {
                // Lines starting with # are comments
                if (!line.StartsWith("#") && !string.IsNullOrEmpty(line)) {
                    var columns = line.Split(new char[] { '\t', ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length == 2) {
                        if (double.TryParse(columns[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var azimuth)) {
                            if (double.TryParse(columns[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var altitude)) {
                                horizonMap[azimuth] = altitude;
                            } else {
                                Logger.Warning($"Invalid value for altitude {columns[0]}");
                            }
                        } else {
                            Logger.Warning($"Invalid value for azimuth {columns[0]}");
                        }
                    } else {
                        Logger.Warning($"Invalid line for horizon values {line}");
                    }
                }
            }

            GroomHorizonData(horizonMap);
            return new CustomHorizon(horizonMap);
        }

        public static CustomHorizon FromReader_MW4(StreamReader sr) {
            var horizonMap = new SortedDictionary<double, double>();

            using (JsonTextReader jsonReader = new JsonTextReader(sr)) {
                var deserialized = (JToken)JSON_SERIALIZER.Deserialize(jsonReader);
                if (deserialized.Type != JTokenType.Array) {
                    throw new ArgumentException($"Expected JSON array in MW4-formatted horizon file");
                }
                var arr = (JArray)deserialized;
                for (int i = 0; i < arr.Count; ++i) {
                    var point = arr[i];
                    if (point.Type != JTokenType.Array) {
                        throw new ArgumentException($"Expected JSON array for each point in MW4-formatted horizon file");
                    }

                    var coordinateArray = (JArray)point;
                    if (coordinateArray.Count != 2) {
                        throw new ArgumentException($"Expected JSON 2-element array for each point in MW4-formatted horizon file");
                    }

                    var altitude = coordinateArray[0].Value<double>();
                    if (altitude < 0 || altitude > 90.0) {
                        throw new ArgumentException($"Invalid altitude {altitude} found in MW4-formatted horizon file");
                    }

                    var azimuth = coordinateArray[1].Value<double>();
                    if (azimuth < 0 || azimuth > 360.0) {
                        throw new ArgumentException($"Invalid azimuth {azimuth} found in MW4-formatted horizon file");
                    }
                    horizonMap[azimuth] = altitude;
                }
            };

            GroomHorizonData(horizonMap);
            return new CustomHorizon(horizonMap);
        }

        /// <summary>
        /// Creates an instance of the custom horizon object to calculate horizon altitude based on a given azimuth from a file path. The file format used is either:
        ///  1. MountWizzard4 (.hpts)
        ///  2. NINA-standard (everything else)
        /// </summary>
        /// <param name="filePath">The file pointing to the horizon file</param>
        /// <returns>An instance of CustomHorizon</returns>
        public static CustomHorizon FromFilePath(string filePath) {
            if (File.Exists(filePath)) {
                var fi = new FileInfo(filePath);
                using (var fs = File.OpenRead(filePath)) {
                    using (var sr = new StreamReader(fs)) {
                        if (fi.Extension == ".hpts") {
                            return FromReader_MW4(sr);
                        } else {
                            return FromReader_Standard(sr);
                        }
                    }
                }
            } else {
                throw new FileNotFoundException("Horizon file not found", filePath);
            }
        }
    }
}