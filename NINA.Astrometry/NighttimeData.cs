#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.RiseAndSet;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NINA.Astrometry {

    public class NighttimeData {

        public NighttimeData(
            DateTime date,
            DateTime referenceDate,
            AstroUtil.MoonPhase moonPhase,
            double? moonIllumination,
            RiseAndSetEvent twilightRiseAndSet,
            RiseAndSetEvent nauticalTwilightRiseAndSet,
            RiseAndSetEvent sunRiseAndSet,
            RiseAndSetEvent moonRiseAndSet,
            RiseAndSetEvent civilTwilightRiseAndSet) {
            this.Date = date;
            this.ReferenceDate = referenceDate;
            this.MoonPhase = moonPhase;
            this.Illumination = moonIllumination;
            this.TwilightRiseAndSet = twilightRiseAndSet;
            this.NauticalTwilightRiseAndSet = nauticalTwilightRiseAndSet;
            this.CivilTwilightRiseAndSet = civilTwilightRiseAndSet;
            this.SunRiseAndSet = sunRiseAndSet;
            this.MoonRiseAndSet = moonRiseAndSet;
            this.NightDuration = new AsyncObservableCollection<DataPoint>(CalculateNightDuration(twilightRiseAndSet));
            this.TwilightDuration = new AsyncObservableCollection<DataPoint>(CalculateTwilightDuration(twilightRiseAndSet, sunRiseAndSet));
            this.NauticalTwilightDuration = new AsyncObservableCollection<DataPoint>(CalculateNauticalTwilightDuration(nauticalTwilightRiseAndSet, sunRiseAndSet));
            this.CivilTwilightDuration = new AsyncObservableCollection<DataPoint>(CalculateNauticalTwilightDuration(civilTwilightRiseAndSet, sunRiseAndSet));
            this.Ticker = new Ticker(TimeSpan.FromSeconds(30));
        }

        public Ticker Ticker { get; }

        private static IList<DataPoint> CalculateTwilightDuration(RiseAndSetEvent twilightRiseAndSet, RiseAndSetEvent sunRiseAndSet) {
            if (twilightRiseAndSet != null && twilightRiseAndSet.Rise.HasValue && twilightRiseAndSet.Set.HasValue) {
                var twilightRise = twilightRiseAndSet.Rise;
                var twilightSet = twilightRiseAndSet.Set;
                if (twilightSet.Value > twilightRise.Value) {
                    twilightSet = twilightSet?.AddDays(-1);
                }
                var dataPointsBuilder = ImmutableList.CreateBuilder<DataPoint>();
                dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(twilightSet), 90));
                if (sunRiseAndSet != null) {
                    var rise = sunRiseAndSet.Rise;
                    var set = sunRiseAndSet.Set;
                    if (rise.HasValue && set.HasValue) {
                        if (set.Value > rise.Value) {
                            set = set?.AddDays(-1);
                        }
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(set), 90));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(set), 0));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(rise), 0));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(rise), 90));
                    }
                }
                dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(twilightRise), 90));
                return dataPointsBuilder.ToImmutable();
            }
            return ImmutableList.Create<DataPoint>();
        }

        private static IList<DataPoint> CalculateNauticalTwilightDuration(RiseAndSetEvent nauticalTwilightRiseAndSet, RiseAndSetEvent sunRiseAndSet) {
            if (nauticalTwilightRiseAndSet != null && nauticalTwilightRiseAndSet.Rise.HasValue && nauticalTwilightRiseAndSet.Set.HasValue) {
                var nauticalTwilightRise = nauticalTwilightRiseAndSet.Rise;
                var nauticalTwilightSet = nauticalTwilightRiseAndSet.Set;
                if (nauticalTwilightSet.Value > nauticalTwilightRise.Value) {
                    nauticalTwilightSet = nauticalTwilightSet?.AddDays(-1);
                }
                var dataPointsBuilder = ImmutableList.CreateBuilder<DataPoint>();
                dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(nauticalTwilightSet), 90));
                if (sunRiseAndSet != null) {
                    var rise = sunRiseAndSet.Rise;
                    var set = sunRiseAndSet.Set;
                    if (rise.HasValue && set.HasValue) {
                        if (set.Value > rise.Value) {
                            set = set?.AddDays(-1);
                        }
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(set), 90));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(set), 0));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(rise), 0));
                        dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(rise), 90));
                    }
                }
                dataPointsBuilder.Add(new DataPoint(Axis.ToDouble(nauticalTwilightRise), 90));
                return dataPointsBuilder.ToImmutable();
            }
            return ImmutableList.Create<DataPoint>();
        }

        private static IList<DataPoint> CalculateNightDuration(RiseAndSetEvent twilightRiseAndSet) {
            if (twilightRiseAndSet != null && twilightRiseAndSet.Rise.HasValue && twilightRiseAndSet.Set.HasValue) {
                var rise = twilightRiseAndSet.Rise;
                var set = twilightRiseAndSet.Set;
                if (set.Value > rise.Value) {
                    set = set?.AddDays(-1);
                }
                return ImmutableList.Create(
                    new DataPoint(Axis.ToDouble(rise), 90),
                    new DataPoint(Axis.ToDouble(set), 90));
            }

            return ImmutableList.Create<DataPoint>();
        }

        public DateTime Date { get; set; }
        public DateTime ReferenceDate { get; set; }
        public AstroUtil.MoonPhase MoonPhase { get; set; }
        public double? Illumination { get; set; }
        public RiseAndSetEvent TwilightRiseAndSet { get; set; }
        public RiseAndSetEvent NauticalTwilightRiseAndSet { get; set; }
        public RiseAndSetEvent CivilTwilightRiseAndSet { get; set; }
        public RiseAndSetEvent SunRiseAndSet { get; set; }
        public RiseAndSetEvent MoonRiseAndSet { get; set; }
        public AsyncObservableCollection<DataPoint> TwilightDuration { get; set; }
        public AsyncObservableCollection<DataPoint> NauticalTwilightDuration { get; set; }
        public AsyncObservableCollection<DataPoint> CivilTwilightDuration { get; set; }
        public AsyncObservableCollection<DataPoint> NightDuration { get; set; }
    }
}