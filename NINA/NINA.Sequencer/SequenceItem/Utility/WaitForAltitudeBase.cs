using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {
    using Newtonsoft.Json;
    using NINA.Astrometry;
    using NINA.Core.Enum;
    using NINA.Profile.Interfaces;
    using System.Collections.Generic;

    public abstract class WaitForAltitudeBase : SequenceItem {

        private IList<string> issues = new List<string>();

        public WaitForAltitudeBase(IProfileService profileService, bool useCustomHorizon) {
            ProfileService = profileService;
            Data = new WaitLoopData(profileService, useCustomHorizon, CalculateExpectedTime, GetType().Name);
        }

        public IProfileService ProfileService { get; set; }

        [JsonProperty]
        public WaitLoopData Data { get; set; }

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public abstract void CalculateExpectedTime();

        #region Obsolete Migration Properties

        [JsonProperty(propertyName: "Comparator")]
        private ComparisonOperatorEnum DeprecatedComparator {
            set {
                switch (value) {
                    case ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL:
                        value = ComparisonOperatorEnum.GREATER_THAN;
                        break;
                    case ComparisonOperatorEnum.LESS_THAN_OR_EQUAL:
                        value = ComparisonOperatorEnum.LESS_THAN;
                        break;
                }
                Data.Comparator = value;
            }
        }
        [Obsolete]
        [JsonIgnore]
        public ComparisonOperatorEnum Comparator { get; set; }

        [JsonProperty(propertyName: "UserMoonAltitude")]
        private double DeprecatedUserMoonAltitude { set => Data.Offset = value; }
        [Obsolete]
        [JsonIgnore]
        public double UserMoonAltitude { get; set; }

        [JsonProperty(propertyName: "UserSunAltitude")]
        private double DeprecatedUserSunAltitude { set => Data.Offset = value; }
        [Obsolete]
        [JsonIgnore]
        public double UserSunAltitude { get; set; }

        [JsonProperty(propertyName: "AltitudeOffset")]
        private double DeprecatedAltitudeOffset { set => Data.Offset = value; }

        [Obsolete]
        [JsonIgnore]
        public double AltitudeOffset { get; set; }

        [JsonProperty(propertyName: "Altitude")]
        private double DeprecatedAltitude { set => Data.Offset = value; }

        [Obsolete]
        [JsonIgnore]
        public double Altitude { get; set; }

        [JsonProperty(propertyName: "Coordinates")]
        private InputCoordinates DeprecatedCoordinates { set => Data.Coordinates = value; }

        [Obsolete]
        [JsonIgnore]
        public InputCoordinates Coordinates { get; set; }
        #endregion
    }
}


