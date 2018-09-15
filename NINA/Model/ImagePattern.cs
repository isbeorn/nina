using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model {

    public class ImagePatterns {

        public ReadOnlyCollection<ImagePattern> Items {
            get {
                return patterns.Values.ToList().AsReadOnly();
            }
        }

        private Dictionary<string, ImagePattern> patterns;

        public ImagePatterns() {
            patterns = new Dictionary<string, ImagePattern>();
            var p = new ImagePattern(ImagePatternKeys.Filter, Locale.Loc.Instance["LblFilternameDescription"]);
            patterns.Add(p.Key, p);

            p = new ImagePattern(ImagePatternKeys.Date, Locale.Loc.Instance["LblDateFormatDescription"]);
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

            p = new ImagePattern(ImagePatternKeys.RMS, Locale.Loc.Instance["LblGuidingRMSDescription"]);
            patterns.Add(p.Key, p);
        }

        public bool Set(string key, string value) {
            if (patterns.ContainsKey(key)) {
                patterns[key].Value = value;
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

        /// <summary>
        /// Replaces makros from Settings.ImageFilePattern into actual values based on input e.g.:
        /// $$Filter$$ -&gt; "Red"
        /// </summary>
        /// <param name="patterns">KeyValue Collection of Makro -&gt; Makrovalue</param>
        /// <returns></returns>
        internal string GetImageFileString(string filePatternMacro) {
            string s = filePatternMacro;
            var nonEmptyPatterns = patterns.Where((x) => !string.IsNullOrWhiteSpace(x.Value.Value)).Select((x) => x.Value);
            foreach (ImagePattern p in nonEmptyPatterns) {
                s = s.Replace(p.Key, p.Value);
            }
            s = Path.Combine(s.Split(Utility.Utility.PATHSEPARATORS, StringSplitOptions.RemoveEmptyEntries));
            return s;
        }

        internal static ImagePatterns CreateExample() {
            var p = new ImagePatterns();

            p.Set(ImagePatternKeys.Filter, "L");
            p.Set(ImagePatternKeys.Date, "2016-01-01");
            p.Set(ImagePatternKeys.DateTime, "2016-01-01_12-00-00");
            p.Set(ImagePatternKeys.Time, "12-00-00");
            p.Set(ImagePatternKeys.FrameNr, "0001");
            p.Set(ImagePatternKeys.ImageType, "Light");
            p.Set(ImagePatternKeys.Binning, "1x1");
            p.Set(ImagePatternKeys.SensorTemp, "-15");
            p.Set(ImagePatternKeys.ExposureTime, 10.21234);
            p.Set(ImagePatternKeys.TargetName, "M33");
            p.Set(ImagePatternKeys.Gain, "1600");
            p.Set(ImagePatternKeys.RMS, 0.35);
            return p;
        }
    }

    public sealed class ImagePatternKeys {

        private ImagePatternKeys() {
        }

        public static readonly string Filter = "$$FILTER$$";
        public static readonly string Date = "$$DATE$$";
        public static readonly string DateTime = "$$DATETIME$$";
        public static readonly string Time = "$$TIME$$";
        public static readonly string FrameNr = "$$FRAMENR$$";
        public static readonly string ImageType = "$$IMAGETYPE$$";
        public static readonly string Binning = "$$BINNING$$";
        public static readonly string SensorTemp = "$$SENSORTEMP$$";
        public static readonly string ExposureTime = "$$EXPOSURETIME$$";
        public static readonly string TargetName = "$$TARGETNAME$$";
        public static readonly string Gain = "$$GAIN$$";
        public static readonly string RMS = "$$RMS$$";
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