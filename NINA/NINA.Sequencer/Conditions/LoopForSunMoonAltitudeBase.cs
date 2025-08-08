using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {
    using Newtonsoft.Json;
    using NINA.Astrometry;
    using NINA.Core.Enum;
    using NINA.Core.Locale;
    using NINA.Core.Model;
    using NINA.Core.Utility;
    using NINA.Profile.Interfaces;
    using NINA.Sequencer.Conditions;
    using NINA.Sequencer.Utility;
    using NINA.Sequencer.Validations;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using static NINA.Sequencer.Utility.ItemUtility;

    public abstract class LoopForSunMoonAltitudeBase : LoopForAltitudeBase {

        public LoopForSunMoonAltitudeBase(IProfileService profileService, bool useCustomHorizon) : base(profileService, useCustomHorizon) {
            Data.Offset = 0d;
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            return Check(previousItem, nextItem, false);
        }

        public bool Check(ISequenceItem previousItem, ISequenceItem nextItem, bool test) {
            if (!test) CalculateExpectedTime();

            var check = true;
            switch (Data.Comparator) {

                case ComparisonOperatorEnum.GREATER_THAN:
                case ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL:
                    if (Data.CurrentAltitude > GetDataOffset()) { check = false; }
                    break;

                default:
                    if (Data.CurrentAltitude <= GetDataOffset()) { check = false; }
                    break;
            }

            if (!check && IsActive()) {
                Logger.Info($"{nameof(LoopForSunMoonAltitudeBase)} finished. Current {Data.Comparator} Target: {Data.CurrentAltitude}° / {Data.Offset}°");
            }
            return check;
        }

        protected abstract double GetDataOffset();

        public override string ToString() {
            return $"Condition: {GetType().Name}, " +
                $"CurrentAltitude: {Data.CurrentAltitude}, Comparator: {Data.Comparator}, TargetAltitude: {Data.TargetAltitude}";
        }
    }
}


