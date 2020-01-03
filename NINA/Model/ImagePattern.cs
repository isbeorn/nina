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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NINA.Model {

    public class ImagePatterns {

        public ReadOnlyCollection<ImagePattern> Items {
            get {
                return patterns.Values.OrderBy(x => x.Key).ToList().AsReadOnly();
            }
        }

        private Dictionary<string, ImagePattern> patterns;

        public ImagePatterns() {
            patterns = new Dictionary<string, ImagePattern>();
            var p = new ImagePattern(ImagePatternKeys.Filter, Locale.Loc.Instance["LblFilternameDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.ApplicationStartDate, Locale.Loc.Instance["LblApplicationStartDateDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.Date, Locale.Loc.Instance["LblDateFormatDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.DateMinus12, Locale.Loc.Instance["LblDateFormatDescription2"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.DateTime, Locale.Loc.Instance["LblDateTimeFormatDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.Time, Locale.Loc.Instance["LblTimeFormatDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.FrameNr, Locale.Loc.Instance["LblFrameNrDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.ImageType, Locale.Loc.Instance["LblImageTypeDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.Binning, Locale.Loc.Instance["LblBinningDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.SensorTemp, Locale.Loc.Instance["LblTemperatureDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.ExposureTime, Locale.Loc.Instance["LblExposureTimeDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.TargetName, Locale.Loc.Instance["LblTargetNameDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.Gain, Locale.Loc.Instance["LblGainDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.Offset, Locale.Loc.Instance["LblOffsetDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.RMS, Locale.Loc.Instance["LblGuidingRMSDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.RMSArcSec, Locale.Loc.Instance["LblGuidingRMSArcSecDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.FocuserPosition, Locale.Loc.Instance["LblFocuserPositionDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.FocuserTemp, Locale.Loc.Instance["LblFocuserTempDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.HFR, Locale.Loc.Instance["LblHFRPatternDescription"]);
            patterns.Add(p.Key, p);
        }

        public bool Set(string key, string value) {
            if (patterns.ContainsKey(key) && value != null) {
                patterns[key].Value = value.Trim();
                return true;
            }

            return false;
        }

        public bool Set(string key, double value) {
            if (!double.IsNaN(value)) {
                return this.Set(key, string.Format("{0:0.00}", value));
            }
            return false;
        }

        public bool Set(string key, int value) {
            return this.Set(key, string.Format("{0:0}", value));
        }

        /// <summary>
        /// Replaces makros from Settings.ImageFilePattern into actual values based on input e.g.:
        /// $$Filter$$ -&gt; "Red"
        /// </summary>
        /// <param name="patterns">KeyValue Collection of Makro -&gt; Makrovalue</param>
        /// <returns></returns>
        internal string GetImageFileString(string filePatternMacro) {
            string s = filePatternMacro;
            foreach (ImagePattern p in patterns.Values) {
                s = s.Replace(p.Key, p.Value);
            }
            var path = s.Split(Utility.Utility.PATHSEPARATORS, StringSplitOptions.RemoveEmptyEntries);

            var imageFileString = string.Empty;
            for (int i = 0; i < path.Length; i++) {
                imageFileString = Path.Combine(imageFileString, ReplaceInvalidChars(path[i]));
            }

            return imageFileString;
        }

        /// <summary>
        /// Replace invalid characters from filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string ReplaceInvalidChars(string filename) {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        internal static ImagePatterns CreateExample() {
            var p = new ImagePatterns();

            p.Set(ImagePatternKeys.Filter, "L");
            p.Set(ImagePatternKeys.Date, "2016-01-01");
            p.Set(ImagePatternKeys.DateMinus12, "2015-12-31");
            p.Set(ImagePatternKeys.DateTime, "2016-01-01_12-00-00");
            p.Set(ImagePatternKeys.Time, "12-00-00");
            p.Set(ImagePatternKeys.FrameNr, "0001");
            p.Set(ImagePatternKeys.ImageType, "Light");
            p.Set(ImagePatternKeys.Binning, "1x1");
            p.Set(ImagePatternKeys.SensorTemp, "-15");
            p.Set(ImagePatternKeys.ExposureTime, 10.21234);
            p.Set(ImagePatternKeys.TargetName, "M33");
            p.Set(ImagePatternKeys.Gain, "1600");
            p.Set(ImagePatternKeys.Offset, "10");
            p.Set(ImagePatternKeys.RMS, 0.35);
            p.Set(ImagePatternKeys.RMSArcSec, 0.65);
            p.Set(ImagePatternKeys.FocuserPosition, 12542);
            p.Set(ImagePatternKeys.FocuserTemp, "3.94");
            p.Set(ImagePatternKeys.ApplicationStartDate, Utility.Utility.ApplicationStartDate.ToString("yyyy-MM-dd"));
            p.Set(ImagePatternKeys.HFR, 3.25);
            return p;
        }
    }

    public sealed class ImagePatternKeys {

        private ImagePatternKeys() {
        }

        public static readonly string Filter = "$$FILTER$$";
        public static readonly string Date = "$$DATE$$";
        public static readonly string DateMinus12 = "$$DATEMINUS12$$";
        public static readonly string DateTime = "$$DATETIME$$";
        public static readonly string Time = "$$TIME$$";
        public static readonly string FrameNr = "$$FRAMENR$$";
        public static readonly string ImageType = "$$IMAGETYPE$$";
        public static readonly string Binning = "$$BINNING$$";
        public static readonly string SensorTemp = "$$SENSORTEMP$$";
        public static readonly string ExposureTime = "$$EXPOSURETIME$$";
        public static readonly string TargetName = "$$TARGETNAME$$";
        public static readonly string Gain = "$$GAIN$$";
        public static readonly string Offset = "$$OFFSET$$";
        public static readonly string RMS = "$$RMS$$";
        public static readonly string RMSArcSec = "$$RMSARCSEC$$";
        public static readonly string FocuserPosition = "$$FOCUSERPOSITION$$";
        public static readonly string FocuserTemp = "$$FOCUSERTEMP$$";
        public static readonly string ApplicationStartDate = "$$APPLICATIONSTARTDATE$$";
        public static readonly string HFR = "$$HFR$$";
    }

    public class ImagePattern {

        public ImagePattern(string k, string d) {
            Key = k;
            Description = d;
        }

        public string Value { get; set; }

        public string Key { get; set; }

        public string Description { get; set; }
    }
}