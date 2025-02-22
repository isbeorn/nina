using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {
    using Newtonsoft.Json;
    using NINA.Astrometry;
    using NINA.Core.Enum;
    using NINA.Core.Utility;
    using NINA.Profile.Interfaces;
    using NINA.Sequencer.Conditions;
    using NINA.Sequencer.SequenceItem.Telescope;
    using NINA.Sequencer.Utility;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public abstract class LoopForAltitudeBase : CoordinatesCondition {

        private IList<string> issues = new List<string>();

        public LoopForAltitudeBase(IProfileService profileService, bool useCustomHorizon) {
            ProfileService = profileService;
            Data = new WaitLoopData(profileService, useCustomHorizon, CalculateExpectedTime, GetType().Name);
            ConditionWatchdog = new ConditionWatchdog(Interrupt, TimeSpan.FromSeconds(5)); 
        }

        //[JsonProperty]
        //public WaitLoopData Data { get; set; }
        public IProfileService ProfileService { get; set; }
 
        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            RunWatchdogIfInsideSequenceRoot();
        }

        public override void AfterParentChanged() {
            RunWatchdogIfInsideSequenceRoot();
        }

        private async Task Interrupt() {
            if (!Check(null, null)) {
                if (Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && Parent.Status == SequenceEntityStatus.RUNNING && Status != SequenceEntityStatus.DISABLED) {
                        Logger.Info(InterruptReason + " - Interrupting current Instruction Set");
                        await Parent.Interrupt();
                    }
                }
            }
        }

        public string InterruptReason { get; set; }

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

        [JsonProperty(propertyName: "Coordinates")]
        private InputCoordinates DeprecatedCoordinates { set => Data.Coordinates = value; }

        [Obsolete]
        [JsonIgnore]
        public InputCoordinates Coordinates { get; set; }
        #endregion
    }
}


