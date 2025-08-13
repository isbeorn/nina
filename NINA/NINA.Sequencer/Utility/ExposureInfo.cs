using Newtonsoft.Json;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Utility {
    [JsonObject(MemberSerialization.OptOut)]
    public class ExposureInfo : BaseINPC {
        private int count;

        [JsonConstructor]
        public ExposureInfo(int count, string filter, double exposureTime, int gain, int offset, string imageType, short binningX, short binningY, double roi) {
            Count = count;
            Filter = filter;
            ExposureTime = exposureTime;
            Gain = gain;
            Offset = offset;
            ImageType = imageType;
            BinningX = binningX;
            BinningY = binningY;
            ROI = roi;
        }

        public ExposureInfo(string filter, double exposureTime, int gain, int offset, string imageType, short binningX, short binningY, double roi) {
            Filter = filter;
            ExposureTime = exposureTime;
            Gain = gain;
            Offset = offset;
            ImageType = imageType;
            BinningX = binningX;
            BinningY = binningY;
            ROI = roi;
            Count = 0;
        }

        [JsonIgnore]
        public string TotalTime {
            get {
                var total = TimeSpan.FromSeconds(ExposureTime * Count);
                return $"{Math.Truncate(total.TotalHours)}:{total.Minutes:D2}:{total.Seconds:D2}";
            }
        }
        public int Count {
            get => count;
            set {
                if (count != value) {
                    count = value;
                    RaisePropertyChanged(nameof(Count));
                    RaisePropertyChanged(nameof(TotalTime));
                }                
            }
        }
        public string Filter { get; }
        public double ExposureTime { get; }
        public int Gain { get; }
        public int Offset { get; }
        public string ImageType { get; }
        public short BinningX { get; }
        public short BinningY { get; }
        public double ROI { get; }

        public void Increment() {
            Count++;
        }
    }
}
