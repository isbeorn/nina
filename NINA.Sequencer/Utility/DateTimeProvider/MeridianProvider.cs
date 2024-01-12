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
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Locale;

namespace NINA.Sequencer.Utility.DateTimeProvider {

    [JsonObject(MemberSerialization.OptIn)]
    public class MeridianProvider : IDateTimeProvider {
        private IProfileService profileService;

        public MeridianProvider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name { get; } = Loc.Instance["LblMeridian"];
        public ICustomDateTime DateTime { get; set; } = new SystemDateTime();

        public DateTime GetDateTime(ISequenceEntity context) {
            var contextCoordinates = ItemUtility.RetrieveContextCoordinates(context?.Parent);
            if (contextCoordinates != null) {
                var siderealTime = Angle.ByHours(AstroUtil.GetLocalSiderealTime(DateTime.Now, profileService.ActiveProfile.AstrometrySettings.Longitude));
                var timeToMeridian = MeridianFlip.TimeToMeridian(contextCoordinates.Coordinates, siderealTime);
                return DateTime.Now + timeToMeridian;
            }
            return DateTime.Now;
        }
        public TimeOnly GetRolloverTime(ISequenceEntity context) {
            var contextCoordinates = ItemUtility.RetrieveContextCoordinates(context?.Parent);
            if (contextCoordinates != null) {
                var siderealTime = Angle.ByHours(AstroUtil.GetLocalSiderealTime(DateTime.Now, profileService.ActiveProfile.AstrometrySettings.Longitude));
                var timeToMeridian = MeridianFlip.TimeToMeridian(contextCoordinates.Coordinates, siderealTime);
                return TimeOnly.FromDateTime(DateTime.Now + timeToMeridian + TimeSpan.FromHours(12));
            }
            return TimeOnly.FromDateTime(DateTime.Now + TimeSpan.FromHours(12));
        }
    }
}