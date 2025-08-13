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
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Astrometry.Interfaces;

namespace NINA.Sequencer.Utility.DateTimeProvider {

    [JsonObject(MemberSerialization.OptIn)]
    public class NauticalDawnProvider : IDateTimeProvider {
        private INighttimeCalculator nighttimeCalculator;

        public NauticalDawnProvider(INighttimeCalculator nighttimeCalculator) {
            this.nighttimeCalculator = nighttimeCalculator;
        }

        public string Name { get; } = Loc.Instance["LblNauticalDawn"];
        public ICustomDateTime DateTime { get; set; } = new SystemDateTime();

        public DateTime GetDateTime(ISequenceEntity context) {
            var night = nighttimeCalculator.Calculate().NauticalTwilightRiseAndSet.Rise;
            if (!night.HasValue) {
                throw new TimeProviderException("No nautical dawn", Loc.Instance["Lbl_TimeProvider_NoNauticalDawn"]);
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