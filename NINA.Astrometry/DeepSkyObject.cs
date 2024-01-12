#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Media;

namespace NINA.Astrometry {

    class DeepSkyObjectDailyRefresher {
        private static readonly DeepSkyObjectDailyRefresher instance = new DeepSkyObjectDailyRefresher();

        private DeepSkyObjectDailyRefresher() {
            ReferenceDateTimer = new Timer(10 * 60 * 1000);
            ReferenceDateTimer.Elapsed += OnTimedEvent;
            ReferenceDateTimer.AutoReset = true;
            ReferenceDateTimer.Enabled = true;
        }

        public static DeepSkyObjectDailyRefresher Instance => instance;


        private List<WeakReference<DeepSkyObject>> DeepSkyObjects = new List<WeakReference<DeepSkyObject>>();
        private Timer ReferenceDateTimer = null;
        private DateTime LastReferenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);

        /// <summary>
        /// Registers a deep sky object to be scheduled for daily updates at noon
        /// </summary>
        /// <param name="dso"></param>
        public void Register(DeepSkyObject dso) {
            lock (DeepSkyObjects) {
                DeepSkyObjects.Add(new WeakReference<DeepSkyObject>(dso));
            }

        }
        private void OnTimedEvent(object source, ElapsedEventArgs e) {
            lock (DeepSkyObjects) {
                try {

                    int updated = 0;
                    int count = DeepSkyObjects.Count;

                    DateTime referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);

                    // If the current ReferenceDate hasn't changed, we're done except for cleaning up
                    if (referenceDate != LastReferenceDate) {
                        foreach (WeakReference<DeepSkyObject> wr in DeepSkyObjects) {
                            // Ignore weak references that are no longer valid
                            if (wr != null && wr.TryGetTarget(out DeepSkyObject target)) {
                                // If the target's reference date is our previous date, it's updatable.  Otherwise, it could
                                // be for future planning or historical something or other, in which case we do nothing
                                if (target.ReferenceDate == LastReferenceDate) {
                                    target.ReferenceDate = referenceDate;
                                    // Update altitude chart
                                    target.Refresh();
                                    updated++;
                                }
                            }
                        }
                        LastReferenceDate = referenceDate;
                    }

                    // Prune the list of weak references
                    DeepSkyObjects.RemoveAll(i => i == null || !i.TryGetTarget(out DeepSkyObject _));

                    if (updated != 0 || count != DeepSkyObjects.Count) {
                        Logger.Debug($"{nameof(DeepSkyObjectDailyRefresher)} -- Updated: {updated}; Pruned: {(count - DeepSkyObjects.Count)} / {count}");

                    }
                } catch(Exception ex) {
                    Logger.Error(ex);
                }
            }
        }
    }

    public class DeepSkyObject : SkyObjectBase {
        public DeepSkyObject(string id, Coordinates coords, string imageRepository, CustomHorizon customHorizon)
            : base(id, imageRepository, customHorizon) {
            _coordinates = coords;
            Moon = new MoonInfo(_coordinates);


            DeepSkyObjectDailyRefresher.Instance.Register(this);
        }
        public DateTime ReferenceDate { get => _referenceDate; set => _referenceDate = value; }

        public MoonInfo Moon { get; private set; }

        private Coordinates _coordinates;
        public override Coordinates Coordinates {
            get => _coordinates;
            set {
                _coordinates = value;
                if (_coordinates != null) {
                    Moon.Coordinates = _coordinates;
                    UpdateHorizonAndTransit();
                }
                RaisePropertyChanged();
            }
        }

        public override Coordinates CoordinatesAt(DateTime at) {
            return Coordinates;
        }

        public override SiderealShiftTrackingRate ShiftTrackingRate => SiderealShiftTrackingRate.Disabled;
        public override SiderealShiftTrackingRate ShiftTrackingRateAt(DateTime at) => SiderealShiftTrackingRate.Disabled;

        public void Refresh() {
            UpdateHorizonAndTransit();
        }

        protected override void UpdateHorizonAndTransit() {
            // It turns out that 2/3 of calls made here are during clone operations and deserialization,
            // at which time the Coordinates object is basically "empty" (RA = 0, Dec = 0, Epoch = J2000)
            // Each call to this method generates up to 1,000 or more calls to AstroUtil

            // Basically, each DSO in a template or sequence or target will call here 20 or so times, each
            // time looping 240 times getting altitude/azimuth, etc.  Only a couple of those 20 are worthwhile.

            // For 100 DSO's (not unreasonable at all), that's 2000 * 240 * 3 calls to AstroUtils, taking up
            // many actual seconds of work (which prevents the display of the target list, among other things)
            // That's over one million calls!

            // 80%+ of the remaining calls (or more) could be removed if deserialization didn't generate
            // a call here 8 or more times for each Coordinates object (RA hours, minutes, seconds; Dec hours, minutes, seconds;
            // rotation; and more)!  It's unclear how to do that, so I leave it to others

            if (Coordinates == null || (Coordinates.RA == 0 && Coordinates.Dec == 0 && Coordinates.Epoch == Epoch.J2000)) {
                return;
            }

            var start = _referenceDate;

            // Do this every time in case reference date has changed
            Moon.SetReferenceDateAndObserver(_referenceDate, new ObserverInfo { Latitude = _latitude, Longitude = _longitude });

            Altitudes.Clear();
            Horizon.Clear();

            var siderealTime = AstroUtil.GetLocalSiderealTime(start, _longitude);
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, Coordinates.RA);

            for (double angle = hourAngle; angle < hourAngle + 24; angle += 0.1) {
                var degAngle = AstroUtil.HoursToDegrees(angle);
                var altitude = AstroUtil.GetAltitude(degAngle, _latitude, Coordinates.Dec);
                var azimuth = AstroUtil.GetAzimuth(degAngle, altitude, _latitude, Coordinates.Dec);

                Altitudes.Add(new DataPoint(DateTimeAxis.ToDouble(start), altitude));

                if (customHorizon != null) {
                    var horizonAltitude = customHorizon.GetAltitude(azimuth);
                    Horizon.Add(new DataPoint(DateTimeAxis.ToDouble(start), horizonAltitude));
                }

                start = start.AddHours(0.1);
            }

            MaxAltitude = Altitudes.OrderByDescending((x) => x.Y).FirstOrDefault();

            CalculateTransit(_latitude);
        }

        private void CalculateTransit(double latitude) {
            var alt0 = AstroUtil.GetAltitude(0, latitude, Coordinates.Dec);
            var alt180 = AstroUtil.GetAltitude(180, latitude, Coordinates.Dec);
            double transit;
            if (alt0 > alt180) {
                transit = AstroUtil.GetAzimuth(0, alt0, latitude, Coordinates.Dec);
            } else {
                transit = AstroUtil.GetAzimuth(180, alt180, latitude, Coordinates.Dec);
            }
            DoesTransitSouth = !double.IsNaN(transit) && Convert.ToInt32(transit) == 180;
        }
    }
}