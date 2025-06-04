using Newtonsoft.Json;
using NINA.Astrometry.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Utility.DateTimeProvider {
    [JsonObject(MemberSerialization.OptIn)]
    public class CivilDawnProvider : IDateTimeProvider {
        private INighttimeCalculator nighttimeCalculator;

        public CivilDawnProvider(INighttimeCalculator nighttimeCalculator) {
            this.nighttimeCalculator = nighttimeCalculator;
        }

        public string Name { get; } = Loc.Instance["LblCivilDawn"];
        public ICustomDateTime DateTime { get; set; } = new SystemDateTime();

        public DateTime GetDateTime(ISequenceEntity context) {
            var night = nighttimeCalculator.Calculate().CivilTwilightRiseAndSet.Rise;
            if (!night.HasValue) {
                throw new Exception("No Civil dawn");
            }
            return night.Value;
        }

        public TimeOnly GetRolloverTime(ISequenceEntity context) {
            var dusk = nighttimeCalculator.Calculate().SunRiseAndSet.Set;
            if (!dusk.HasValue) {
                return new TimeOnly(12, 0, 0);
            }
            return TimeOnly.FromDateTime(dusk.Value);
        }
    }
}
