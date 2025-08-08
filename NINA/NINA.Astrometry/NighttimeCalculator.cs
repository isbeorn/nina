#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Interfaces;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;

namespace NINA.Astrometry {

    public class NighttimeCalculator : BaseINPC, INighttimeCalculator {
        private readonly IProfileService profileService;
        private Timer ReferenceDateTimer = null;
        private DateTime LastReferenceDate;

        public NighttimeCalculator(IProfileService profile) {
            profileService = profile;
            Cache = new Dictionary<string, NighttimeData>();

            LastReferenceDate = GetReferenceDate(DateTime.Now);
            ReferenceDateTimer = new Timer(10 * 60 * 1000);
            ReferenceDateTimer.Elapsed += OnTimedEvent;
            ReferenceDateTimer.AutoReset = true;
            ReferenceDateTimer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e) {
            DateTime referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
            if (LastReferenceDate != referenceDate) {
                OnReferenceDayChanged?.Invoke(this, null);
            }
        }

        private IDictionary<string, NighttimeData> Cache;

        private object lockObj = new object();

        public event EventHandler OnReferenceDayChanged;

        public NighttimeData Calculate(DateTime? date = null) {
            lock (lockObj) {
                var selectedDate = date.HasValue ? date.Value : DateTime.Now;
                var referenceDate = GetReferenceDate(selectedDate);
                var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
                var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
                var elevation = profileService.ActiveProfile.AstrometrySettings.Elevation;

                var key = $"{referenceDate:yyyy-MM-dd-HH-mm-ss}_{latitude.ToString("0.000000", CultureInfo.InvariantCulture)}_{longitude.ToString("0.000000", CultureInfo.InvariantCulture)}";

                if (Cache.TryGetValue(key, out var nighttimeData)) {
                    return nighttimeData;
                } else {
                    var twilightRiseAndSet = AstroUtil.GetNightTimes(referenceDate, latitude, longitude, elevation);
                    var civilTwilightRiseAndSet = AstroUtil.GetCivilNightTimes(referenceDate, latitude, longitude, elevation);
                    var nauticalTwilightRiseAndSet = AstroUtil.GetNauticalNightTimes(referenceDate, latitude, longitude, elevation);
                    var moonRiseAndSet = AstroUtil.GetMoonRiseAndSet(referenceDate, latitude, longitude, elevation);
                    var sunRiseAndSet = AstroUtil.GetSunRiseAndSet(referenceDate, latitude, longitude, elevation);
                    var moonPhase = AstroUtil.GetMoonPhase(referenceDate, new ObserverInfo() { Latitude = latitude, Longitude = longitude, Elevation = elevation });
                    var illumination = AstroUtil.GetMoonIllumination(referenceDate, new ObserverInfo() { Latitude = latitude, Longitude = longitude, Elevation = elevation });

                    var data = new NighttimeData(date: selectedDate, referenceDate: referenceDate, moonPhase: moonPhase, moonIllumination: illumination, twilightRiseAndSet: twilightRiseAndSet, nauticalTwilightRiseAndSet: nauticalTwilightRiseAndSet,
                        sunRiseAndSet: sunRiseAndSet, moonRiseAndSet: moonRiseAndSet, civilTwilightRiseAndSet: civilTwilightRiseAndSet);
                    Cache[key] = data;
                    return data;
                }
            }
        }

        public static DateTime GetReferenceDate(DateTime reference) {
            DateTime d = reference;
            if (d.Hour > 12 || (d.Hour == 12 && d.Minute >= 0)) {
                d = new DateTime(d.Year, d.Month, d.Day, 12, 0, 0, reference.Kind);
            } else {
                var tmp = d.AddDays(-1);
                d = new DateTime(tmp.Year, tmp.Month, tmp.Day, 12, 0, 0, reference.Kind);
            }
            return d;
        }
    }
}