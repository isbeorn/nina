#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model.MyGuider;
using NINA.Utility;
using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NINA.Model {

    public class GuideStepsHistory : BaseINPC {

        public GuideStepsHistory(int historySize, GuiderScaleEnum scale, double maxY) {
            RMS = new RMS();
            PixelScale = 1;
            HistorySize = historySize;
            MaxY = maxY;
            Scale = scale;
            GuideSteps = new AsyncObservableLimitedSizedStack<HistoryStep>(historySize);
            MaxDurationY = 1;
        }

        private readonly object lockObj = new object();

        private RMS rMS;

        public RMS RMS {
            get {
                return rMS;
            }
            private set {
                rMS = value;
                RaisePropertyChanged();
            }
        }

        public double Interval {
            get {
                return MaxY / 4;
            }
        }

        private double _maxY;

        public double MaxY {
            get {
                return _maxY;
            }

            set {
                _maxY = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MinY));
                RaisePropertyChanged(nameof(Interval));
            }
        }

        public double MinY {
            get {
                return -MaxY;
            }
        }

        private double _maxDurationY;

        public double MaxDurationY {
            get {
                return _maxDurationY;
            }

            set {
                _maxDurationY = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MinDurationY));
            }
        }

        public double MinDurationY {
            get {
                return -MaxDurationY;
            }
        }

        private LinkedList<HistoryStep> overallGuideSteps = new LinkedList<HistoryStep>();

        private AsyncObservableLimitedSizedStack<HistoryStep> guideSteps;

        public AsyncObservableLimitedSizedStack<HistoryStep> GuideSteps {
            get {
                return guideSteps;
            }
            set {
                guideSteps = value;
                RaisePropertyChanged();
            }
        }

        private int historySize;

        public int HistorySize {
            get {
                return historySize;
            }
            set {
                historySize = value;
                RebuildGuideHistoryList();
                RaisePropertyChanged();
            }
        }

        private void RebuildGuideHistoryList() {
            lock (lockObj) {
                if (overallGuideSteps.Count > 0) {
                    var collection = new LinkedList<HistoryStep>();
                    var startIndex = overallGuideSteps.Count - historySize;
                    if (startIndex < 0) startIndex = 0;
                    RMS.Clear();
                    for (int i = startIndex; i < overallGuideSteps.Count; i++) {
                        var p = overallGuideSteps.ElementAt(i);
                        RMS.AddDataPoint(p.RADistanceRaw, p.DECDistanceRaw);
                        if (Scale == GuiderScaleEnum.ARCSECONDS) {
                            p = p.AdjustPixelScale(PixelScale);
                        } else {
                            p = p.AdjustPixelScale(1);
                        }
                        collection.AddLast(p);
                    }

                    GuideSteps = new AsyncObservableLimitedSizedStack<HistoryStep>(historySize, collection);
                    CalculateMaximumDurationY();
                }
            }
        }

        private void CalculateMaximumDurationY() {
            MaxDurationY = Math.Abs(GuideSteps.Max((x) => Math.Max(Math.Abs(x.RADuration), Math.Abs(x.DECDuration))));
        }

        private double pixelScale;

        public double PixelScale {
            get {
                return pixelScale;
            }
            set {
                pixelScale = value;
                RMS.SetScale(pixelScale);
                RaisePropertyChanged();
            }
        }

        private GuiderScaleEnum scale;

        public GuiderScaleEnum Scale {
            get {
                return scale;
            }
            set {
                scale = value;
                RebuildGuideHistoryList();
                RaisePropertyChanged();
            }
        }

        public void Clear() {
            lock (lockObj) {
                overallGuideSteps.Clear();
                GuideSteps.Clear();
                RMS.Clear();
                MaxDurationY = 1;
                HistoryStep.ResetIdProvider();
            }
        }

        public void AddGuideStep(IGuideStep step) {
            lock (lockObj) {
                var historyStep = HistoryStep.FromGuideStep(step, Scale == GuiderScaleEnum.PIXELS ? 1 : PixelScale);
                overallGuideSteps.AddLast(historyStep);

                if (GuideSteps.Count == HistorySize) {
                    var elementIdx = overallGuideSteps.Count - HistorySize;
                    if (elementIdx >= 0) {
                        var stepToRemove = overallGuideSteps.ElementAt(elementIdx);
                        RMS.RemoveDataPoint(stepToRemove.RADistanceRaw, stepToRemove.DECDistanceRaw);
                    }
                }

                RMS.AddDataPoint(step.RADistanceRaw, step.DECDistanceRaw);

                GuideSteps.Add(historyStep);
                CalculateMaximumDurationY();
            }
        }

        internal void AddDitherIndicator() {
            lock (lockObj) {
                var dither = HistoryStep.GenerateDitherStep();
                overallGuideSteps.AddLast(dither);

                GuideSteps.Add(dither);
            }
        }

        public class HistoryStep {
            private static int idProvider = 1;
            public int Id { get; private set; }
            public double IdOffsetLeft { get => Id - 0.15; }
            public double IdOffsetRight { get => Id + 0.15; }
            public double RADistanceRaw { get; private set; }
            public double RADistanceRawDisplay { get; private set; }
            public double RADuration { get; private set; }
            public double DECDistanceRaw { get; private set; }
            public double DECDistanceRawDisplay { get; private set; }
            public double DECDuration { get; private set; }
            public double Dither { get; private set; } = double.NaN;

            public static HistoryStep FromGuideStep(IGuideStep guideStep, double pixelScale) {
                var id = idProvider++;
                return new HistoryStep() {
                    Id = id,
                    RADistanceRaw = guideStep.RADistanceRaw,
                    RADistanceRawDisplay = guideStep.RADistanceRaw * pixelScale,
                    RADuration = guideStep.RADuration,
                    DECDistanceRaw = guideStep.DECDistanceRaw,
                    DECDistanceRawDisplay = guideStep.DECDistanceRaw * pixelScale,
                    DECDuration = guideStep.DECDuration
                };
            }

            public static HistoryStep GenerateDitherStep() {
                var id = idProvider++;
                return new HistoryStep() {
                    Id = id,
                    RADistanceRaw = 0,
                    RADistanceRawDisplay = 0,
                    RADuration = 0,
                    DECDistanceRaw = 0,
                    DECDistanceRawDisplay = 0,
                    DECDuration = 0,
                    Dither = 0.01
                };
            }

            public HistoryStep AdjustPixelScale(double pixelScale) {
                return new HistoryStep() {
                    Id = this.Id,
                    RADistanceRaw = this.RADistanceRaw,
                    RADistanceRawDisplay = this.RADistanceRaw * pixelScale,
                    RADuration = this.RADuration,
                    DECDistanceRaw = this.DECDistanceRaw,
                    DECDistanceRawDisplay = this.DECDistanceRaw * pixelScale,
                    DECDuration = this.DECDuration,
                    Dither = this.Dither
                };
            }

            public static void ResetIdProvider() {
                idProvider = 0;
            }
        }
    }
}